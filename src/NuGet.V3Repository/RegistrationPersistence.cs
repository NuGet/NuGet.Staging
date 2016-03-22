﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Services.Metadata.Catalog;
using NuGet.Services.Metadata.Catalog.Helpers;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.Metadata.Catalog.Registration;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Datasets;

namespace NuGet.V3Repository
{
    public interface IRegistrationPersistence
    {
        Task<IDictionary<RegistrationEntryKey, RegistrationCatalogEntry>> Load(CancellationToken cancellationToken);
        Task Save(IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> registration, CancellationToken cancellationToken);
    }

    public class RegistrationPersistence : IRegistrationPersistence
    {
        Uri _registrationUri;
        int _packageCountThreshold;
        int _partitionSize;
        RecordingStorage _storage;
        Uri _registrationBaseAddress;
        Uri _contentBaseAddress;

        public RegistrationPersistence(StorageFactory storageFactory, RegistrationKey registrationKey, int partitionSize, int packageCountThreshold, Uri contentBaseAddress)
        {
            _storage = new RecordingStorage(storageFactory.Create(registrationKey.ToString()));
            _registrationUri = _storage.ResolveUri("index.json");
            _packageCountThreshold = packageCountThreshold;
            _partitionSize = partitionSize;
            _registrationBaseAddress = storageFactory.BaseAddress;
            _contentBaseAddress = contentBaseAddress;
        }

        public Task<IDictionary<RegistrationEntryKey, RegistrationCatalogEntry>> Load(CancellationToken cancellationToken)
        {
            return Load(_storage, _registrationUri, cancellationToken);
        }

        public async Task Save(IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> registration, CancellationToken cancellationToken)
        {
            await Save(_storage, _registrationBaseAddress, registration, _partitionSize, _packageCountThreshold, _contentBaseAddress, cancellationToken);

            await Cleanup(_storage, cancellationToken);
        }

        //  Load implementation

        static async Task<IDictionary<RegistrationEntryKey, RegistrationCatalogEntry>> Load(IStorage storage, Uri resourceUri, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.Load: resourceUri = {0}", resourceUri);

            IGraph graph = await LoadCatalog(storage, resourceUri, cancellationToken);

            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> resources = GetResources(graph);

            Trace.TraceInformation("RegistrationPersistence.Load: resources = {0}", resources.Count);

            return resources;
        }

        static IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> GetResources(IGraph graph)
        {
            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> resources = new Dictionary<RegistrationEntryKey, RegistrationCatalogEntry>();

            TripleStore store = new TripleStore();
            store.Add(graph);

            IList<Uri> existingItems = ListExistingItems(store);

            foreach (Uri existingItem in existingItems)
            {
                AddExistingItem(resources, store, existingItem);
            }

            return resources;
        }

        static IList<Uri> ListExistingItems(TripleStore store)
        {
            string sparql = Utils.GetResource("sparql.SelectInlinePackage.rq");

            SparqlResultSet resultSet = SparqlHelpers.Select(store, sparql);

            IList<Uri> results = new List<Uri>();
            foreach (SparqlResult result in resultSet)
            {
                IUriNode item = (IUriNode)result["catalogPackage"];
                results.Add(item.Uri);
            }

            Trace.TraceInformation("RegistrationPersistence.ListExistingItems results = {0}", results.Count);

            return results;
        }

        static void AddExistingItem(IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> resources, TripleStore store, Uri catalogEntry)
        {
            Trace.TraceInformation("RegistrationPersistence.AddExistingItem: catalogEntry = {0}", catalogEntry);

            SparqlParameterizedString sparql = new SparqlParameterizedString();
            sparql.CommandText = Utils.GetResource("sparql.ConstructCatalogEntryGraph.rq");
            sparql.SetUri("catalogEntry", catalogEntry);

            IGraph graph = SparqlHelpers.Construct(store, sparql.ToString());

            resources.Add(RegistrationCatalogEntry.Promote(catalogEntry.AbsoluteUri, graph));
        }

        static async Task<IGraph> LoadCatalog(IStorage storage, Uri resourceUri, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.LoadCatalog: resourceUri = {0}", resourceUri);

            string json = await storage.LoadString(resourceUri, cancellationToken);

            IGraph graph = Utils.CreateGraph(json);

            if (graph == null)
            {
                return new Graph();
            }

            IEnumerable<Triple> pages = graph.GetTriplesWithPredicateObject(graph.CreateUriNode(Schema.Predicates.Type), graph.CreateUriNode(Schema.DataTypes.CatalogPage));

            IList<Task<IGraph>> tasks = new List<Task<IGraph>>();

            foreach (Triple page in pages)
            {
                Uri pageUri = ((IUriNode)page.Subject).Uri;

                //  note that this is explicit Uri comparison and deliberately ignores differences in the fragment

                if (pageUri != resourceUri)
                {
                    tasks.Add(LoadCatalogPage(storage, pageUri, cancellationToken));
                }
            }

            await Task.WhenAll(tasks.ToArray());

            foreach (Task<IGraph> task in tasks)
            {
                graph.Merge(task.Result, false);
            }

            await LoadCatalogItems(storage, graph, cancellationToken);

            return graph;
        }

        static async Task<IGraph> LoadCatalogPage(IStorage storage, Uri pageUri, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.LoadCatalogPage: pageUri = {0}", pageUri);
            string json = await storage.LoadString(pageUri, cancellationToken);
            IGraph graph = Utils.CreateGraph(json);
            return graph;
        }

        static async Task LoadCatalogItems(IStorage storage, IGraph graph, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.LoadCatalogItems");

            IList<Uri> itemUris = new List<Uri>();

            IEnumerable<Triple> pages = graph.GetTriplesWithPredicateObject(graph.CreateUriNode(Schema.Predicates.Type), graph.CreateUriNode(Schema.DataTypes.CatalogPage));

            foreach (Triple page in pages)
            {
                IEnumerable<Triple> items = graph.GetTriplesWithSubjectPredicate(page.Subject, graph.CreateUriNode(Schema.Predicates.CatalogItem));

                foreach (Triple item in items)
                {
                    itemUris.Add(((IUriNode)item.Object).Uri);
                }
            }

            IList<Task<IGraph>> tasks = new List<Task<IGraph>>();

            foreach (Uri itemUri in itemUris)
            {
                tasks.Add(LoadCatalogItem(storage, itemUri, cancellationToken));
            }

            await Task.WhenAll(tasks.ToArray());

            //TODO: if we have details at the package level and not inlined on a page we will merge them in here
        }

        static async Task<IGraph> LoadCatalogItem(IStorage storage, Uri itemUri, CancellationToken cancellationToken)
        {
            string json = await storage.LoadString(itemUri, cancellationToken);
            IGraph graph = Utils.CreateGraph(json);
            return graph;
        }

        //  Save implementation

        static async Task Save(IStorage storage, Uri registrationBaseAddress, IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> registration, int partitionSize, int packageCountThreshold, Uri contentBaseAddress, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.Save");

            IDictionary<string, IGraph> items = new Dictionary<string, IGraph>();

            foreach (RegistrationCatalogEntry value in registration.Values)
            {
                if (value != null)
                {
                    items.Add(value.ResourceUri, value.Graph);
                }
            }

            if (items.Count == 0)
            {
                return;
            }

            if (items.Count < packageCountThreshold)
            {
                await SaveSmallRegistration(storage, registrationBaseAddress, items, partitionSize, contentBaseAddress, cancellationToken);
            }
            else
            {
                await SaveLargeRegistration(storage, registrationBaseAddress, items, partitionSize, contentBaseAddress, cancellationToken);
            }
        }

        static async Task SaveSmallRegistration(IStorage storage, Uri registrationBaseAddress, IDictionary<string, IGraph> items, int partitionSize, Uri contentBaseAddress, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.SaveSmallRegistration");

            SingleGraphPersistence graphPersistence = new SingleGraphPersistence(storage);

            //await graphPersistence.Initialize();

            await SaveRegistration(storage, registrationBaseAddress, items, null, graphPersistence, partitionSize, contentBaseAddress, cancellationToken);

            // now the commit has happened the graphPersistence.Graph should contain all the data

            JObject frame = (new CatalogContext()).GetJsonLdContext("context.Registration.json", graphPersistence.TypeUri);
            StorageContent content = new JTokenStorageContent(Utils.CreateJson(graphPersistence.Graph, frame), "application/json", "no-store");
            await storage.Save(graphPersistence.ResourceUri, content, cancellationToken);
        }

        static async Task SaveLargeRegistration(IStorage storage, Uri registrationBaseAddress, IDictionary<string, IGraph> items, int partitionSize, Uri contentBaseAddress, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.SaveLargeRegistration: registrationBaseAddress = {0} items: {1}", registrationBaseAddress, items.Count);

            IList<Uri> cleanUpList = new List<Uri>();

            await SaveRegistration(storage, registrationBaseAddress, items, cleanUpList, null, partitionSize, contentBaseAddress, cancellationToken);
        }

        static async Task SaveRegistration(IStorage storage, Uri registrationBaseAddress, IDictionary<string, IGraph> items, IList<Uri> cleanUpList, SingleGraphPersistence graphPersistence, int partitionSize, Uri contentBaseAddress, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.SaveRegistration: registrationBaseAddress = {0} items: {1}", registrationBaseAddress, items.Count);

            using (RegistrationMakerCatalogWriter writer = new RegistrationMakerCatalogWriter(storage, partitionSize, cleanUpList, graphPersistence))
            {
                foreach (KeyValuePair<string, IGraph> item in items)
                {
                    writer.Add(new RegistrationMakerCatalogItem(new Uri(item.Key), item.Value, registrationBaseAddress, contentBaseAddress));
                }
                await writer.Commit(DateTime.UtcNow, null, cancellationToken);
            }
        }

        static async Task Cleanup(RecordingStorage storage, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationPersistence.Cleanup");

            IList<Task> tasks = new List<Task>();
            foreach (Uri loaded in storage.Loaded)
            {
                if (!storage.Saved.Contains(loaded))
                {
                    tasks.Add(storage.Delete(loaded, cancellationToken));
                }
            }
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks.ToArray());
            }
        }
    }

    public class SparqlHelpers
    {
        public static IGraph Construct(TripleStore store, string sparql)
        {
            return (IGraph)Execute(store, sparql);
        }

        public static SparqlResultSet Select(TripleStore store, string sparql)
        {
            return (SparqlResultSet)Execute(store, sparql);
        }

        static object Execute(TripleStore store, string sparql)
        {
            InMemoryDataset ds = new InMemoryDataset(store);
            ISparqlQueryProcessor processor = new LeviathanQueryProcessor(ds);
            SparqlQueryParser sparqlparser = new SparqlQueryParser();
            SparqlQuery query = sparqlparser.ParseFromString(sparql);
            return processor.ProcessQuery(query);
        }
    }
}

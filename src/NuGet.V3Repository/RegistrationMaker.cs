﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Services.Metadata.Catalog.Persistence;
using NuGet.Services.Metadata.Catalog.Registration;
using VDS.RDF;

namespace NuGet.V3Repository
{
    public static class RegistrationMaker
    {
        public static async Task Process(RegistrationKey registrationKey, IDictionary<string, IGraph> newItems, StorageFactory storageFactory, Uri contentBaseAddress, int partitionSize, int packageCountThreshold, bool unlistShouldDelete, CancellationToken cancellationToken)
        {
            Trace.TraceInformation("RegistrationMaker.Process: registrationKey = {0} newItems: {1}", registrationKey, newItems.Count);

            IRegistrationPersistence registration = new RegistrationPersistence(storageFactory, registrationKey, partitionSize, packageCountThreshold, contentBaseAddress);

            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> existing = await registration.Load(cancellationToken);

            Trace.TraceInformation("RegistrationMaker.Process: existing = {0}", existing.Count);

            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> delta = PromoteRegistrationKey(newItems, unlistShouldDelete);

            Trace.TraceInformation("RegistrationMaker.Process: delta = {0}", delta.Count);

            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> resulting = Apply(existing, delta);

            Trace.TraceInformation("RegistrationMaker.Process: resulting = {0}", resulting.Count);

            await registration.Save(resulting, cancellationToken);
        }

        static IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> PromoteRegistrationKey(IDictionary<string, IGraph> newItems, bool unlistShouldDelete)
        {
            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> promoted = new Dictionary<RegistrationEntryKey, RegistrationCatalogEntry>();
            foreach (var newItem in newItems)
            {
                promoted.Add(RegistrationCatalogEntry.Promote(newItem.Key, newItem.Value, unlistShouldDelete));
            }
            return promoted;
        }

        static IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> Apply(
            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> existing,
            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> delta)
        {
            IDictionary<RegistrationEntryKey, RegistrationCatalogEntry> resulting = new Dictionary<RegistrationEntryKey, RegistrationCatalogEntry>();

            foreach (var item in existing)
            {
                if (delta.ContainsKey(item.Key))
                {
                    resulting.Add(item.Key, delta[item.Key]);
                    delta.Remove(item.Key);
                }
                else
                {
                    resulting.Add(item);
                }
            }

            foreach (var item in delta)
            {
                resulting.Add(item);
            }

            return resulting;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class TopicMessageProviderOptions
    {
        public string ServiceBusConnectionString { get; set; }
        public string TopicName { get; set; }
        public string SubscriptionName { get; set; }
        public int ProcessingConcurrency { get; set; }
    }

    public class TopicMessageProvider<T> : IMessageProvider<T> where T : class
    {
        private readonly SubscriptionClient _subscriptionClient;
        private readonly TopicMessageProviderOptions _options;
        private readonly ILogger<TopicMessageProvider<T>> _logger;

        public bool IsActive { get; private set; }

        public TopicMessageProvider(IOptions<TopicMessageProviderOptions> options, ILogger<TopicMessageProvider<T>> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _options = options.Value;
            _logger = logger;

            var factory = MessagingFactory.CreateFromConnectionString(_options.ServiceBusConnectionString);
            _subscriptionClient = factory.CreateSubscriptionClient(_options.TopicName, _options.SubscriptionName, ReceiveMode.PeekLock);
        }

        public void Start(Func<T, Task> messageHandler)
        {
            var onMessageOptions = new OnMessageOptions
            {
                AutoComplete = false,
                AutoRenewTimeout = TimeSpan.FromHours(1),
                MaxConcurrentCalls = _options.ProcessingConcurrency,
            };

            onMessageOptions.ExceptionReceived += (sender, args) =>
            {
                if (args != null && args.Exception != null)
                {
                    _logger.LogError(string.Format("Service bus exception: Action: {0}, Error: {1}",
                        args == null ? "None" : args.Action,
                        args == null || args.Exception == null ? "None" : args.Exception.ToString()));
                }
            };

            _subscriptionClient.OnMessageAsync(async message =>
            {
                T innerMessage = null;

                try
                {
                    var stream = message.GetBody<Stream>();
                    var reader = new StreamReader(stream);
                    innerMessage = JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
                }
                catch (JsonException e)
                {
                    // The reason to stop the client when serialization fails, is that if we can't deserialize properly, it means
                    // the contract changed. We need to wait for deployment.
                    // TODO: monitor this case. It should have an alert
                    _logger.LogCritical("Failed to deserialize message. Stopping subscription. Exception: {0}", e);
                    await _subscriptionClient.CloseAsync();

                    IsActive = false;
                    throw;
                }

                try
                {
                    await messageHandler(innerMessage);
                    await message.CompleteAsync();
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Caught handler exception: " + e);
                    await message.AbandonAsync();
                }

            }, onMessageOptions);

            IsActive = true;
        }

        public async Task Stop()
        {
            await _subscriptionClient.CloseAsync();
        }
    }
}

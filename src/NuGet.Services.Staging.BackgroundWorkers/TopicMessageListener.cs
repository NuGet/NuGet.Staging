// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class TopicMessageListener<T> : IMessageListener<T> where T : class
    {
        private readonly SubscriptionClient _subscriptionClient;
        private readonly TopicMessageListenerOptions _options;
        private readonly ILogger<TopicMessageListener<T>> _logger;

        private ConcurrentDictionary<string, BrokeredMessage> _activeTaskCollection { get; set; }
        private bool _stopRequested = false;
        private readonly TimeSpan _waitForExecutionCompletionTimeout = TimeSpan.FromMinutes(10);
        private readonly TimeSpan _waitForExecutionCompletionSleepBetweenIterations = TimeSpan.FromSeconds(10);

        public bool IsActive { get; private set; }

        public TopicMessageListener(IOptions<TopicMessageListenerOptions> options, ILogger<TopicMessageListener<T>> logger)
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

            _activeTaskCollection = new ConcurrentDictionary<string, BrokeredMessage>();
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
                if (_stopRequested)
                {
                    // Block processing of new messages. We want to wait for old messages to complete and exit.
                    _logger.LogInformation(
                        $"Stop requested but new message {message.MessageId} processing began. Blocking until all processing completes.");

                    await Task.Delay(_waitForExecutionCompletionTimeout);

                    _logger.LogError( $"Wait for execution completion done for message {message.MessageId}. Exiting.");
                }
                else
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
                        _logger.LogCritical("Failed to deserialize message. Stopping subscription.", e);
                        await Stop();
                        return;
                    }

                    try
                    {
                        // Track executing messages
                        _activeTaskCollection[message.MessageId] = message;

                        await messageHandler(innerMessage);
                        await message.CompleteAsync();
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning("Caught handler exception. Message will be retried. Exception: " + e);
                    }
                    finally
                    {
                        BrokeredMessage savedMessage;

                        if (!_activeTaskCollection.TryRemove(message.MessageId, out savedMessage))
                        {
                            _logger.LogWarning("Attempt to remove message id {0} failed.", savedMessage.MessageId);
                        }
                    }
                }

            }, onMessageOptions);

            IsActive = true;
        }

        public async Task Stop()
        {
            _stopRequested = true;

            _logger.LogInformation("Waiting for messages processing to complete. Timeout: {0} minutes", _waitForExecutionCompletionTimeout.TotalMinutes);

            DateTime startWaitTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startWaitTime < _waitForExecutionCompletionTimeout && _activeTaskCollection.Count > 0)
            {
                _logger.LogInformation("Found {0} active message processing tasks. Waiting..", _activeTaskCollection.Count);
                await Task.Delay(_waitForExecutionCompletionSleepBetweenIterations);
            }

            if (_activeTaskCollection.Count == 0)
            {
                _logger.LogInformation("Wait completed successfully. No processing is running.");
            }
            else
            {
                _logger.LogError("Wait timed out. There are {0} running message processing tasks. Ids: {1}",
                    _activeTaskCollection.Count, string.Join(",", _activeTaskCollection.Keys));
            }

            await _subscriptionClient.CloseAsync();
            IsActive = false;
        }
    }
}

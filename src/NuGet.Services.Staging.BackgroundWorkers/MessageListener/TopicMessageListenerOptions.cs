// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace NuGet.Services.Staging.BackgroundWorkers
{
    public class TopicMessageListenerOptions
    {
        public string ServiceBusUri { get; set; }
        public string SharedAccessPolicyName { get; set; }
        public string SharedAccessPolicyKey { get; set; }
        public string TopicName { get; set; }
        public string SubscriptionName { get; set; }
        public int ProcessingConcurrency { get; set; }
        public int MaxDeliveryCount { get; set; }
    }
}
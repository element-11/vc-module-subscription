﻿using System.Linq;
using Hangfire;
using VirtoCommerce.SubscriptionModule.Core.Model;
using VirtoCommerce.SubscriptionModule.Core.Model.Search;
using VirtoCommerce.SubscriptionModule.Core.Services;

namespace VirtoCommerce.SubscriptionModule.Web.BackgroundJobs
{
    public class ProcessSubscriptionJob
    {
        private readonly ISubscriptionBuilder _subscriptionBuilder;
        private readonly ISubscriptionSearchService _subscriptionSearchService;
        private readonly ISubscriptionService _subscriptionService;
        public ProcessSubscriptionJob(ISubscriptionBuilder subscriptionBuilder, ISubscriptionSearchService subscriptionSearchService, 
                                      ISubscriptionService subscriptionService)
        {
            _subscriptionBuilder = subscriptionBuilder;
            _subscriptionSearchService = subscriptionSearchService;
            _subscriptionService = subscriptionService;
        }

        [DisableConcurrentExecution(60 * 60 * 24)]
        public void Process()
        {
            var criteria = new SubscriptionSearchCriteria
            {
                Statuses = new[] { SubscriptionStatus.Active, SubscriptionStatus.PastDue, SubscriptionStatus.Trialing, SubscriptionStatus.Unpaid }.Select(x=> x.ToString()).ToArray(),
            };
            var result = _subscriptionSearchService.SearchSubscriptions(criteria);
            var batchSize = 20;
            for(int i = 0; i < result.TotalCount; i += batchSize)
            {
                var subscriptions = _subscriptionService.GetByIds(result.Results.Skip(i).Take(batchSize).Select(x => x.Id).ToArray());
                foreach(var subscription in subscriptions)
                {
                    _subscriptionBuilder.TakeSubscription(subscription).Actualize();
                }
                _subscriptionService.SaveSubscriptions(subscriptions);
            }
        }

    }
}

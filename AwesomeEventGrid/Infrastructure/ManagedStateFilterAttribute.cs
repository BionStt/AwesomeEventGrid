﻿using AwesomeEventGrid.Abstractions;
using AwesomeEventGrid.Abstractions.Models;
using AwesomeEventGrid.Entities;
using Hangfire.Common;
using Hangfire.States;
using System.Collections.Generic;

namespace AwesomeEventGrid.Infrastructure
{
    public class ManagedStateFilterAttribute : JobFilterAttribute, IElectStateFilter
    {
        private readonly HangfireActivator activator;

        public ManagedStateFilterAttribute(HangfireActivator activator)
        {
            this.activator = activator;
        }

        public void OnStateElection(ElectStateContext context)
        {
            var failedState = context.CandidateState as FailedState;
            if (failedState != null)
            {
                int retryCount = context.GetJobParameter<int>("RetryCount");

                if (retryCount == 3)
                {
                    var eventGridEventHandler = (IEventGridEventHandler)activator.ActivateJob(typeof(IEventGridEventHandler));
                    SubscriptionModel subscription = (SubscriptionModel)context.BackgroundJob.Job.Args[0];
                    EventModel @event = (EventModel)context.BackgroundJob.Job.Args[1];


                    var errorEvent = new EventModel
                    {
                        Data = @event,
                        EventType = "events.deliveryFailed",
                        Source = @event.Source,
                        Extensions = new Dictionary<string, object>
                        {
                            { nameof(subscription), subscription }
                        }
                    };

                   eventGridEventHandler.HandleAsync(subscription.Topic, errorEvent).Wait();
                }


                //job has failed, stop application here
            }
        }

        
    }



}

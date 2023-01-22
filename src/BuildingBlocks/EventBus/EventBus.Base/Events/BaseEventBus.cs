using EventBus.Base.Abstraction;
using EventBus.Base.SubManager;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Base.Events
{
    public abstract class BaseEventBus : IEventBus
    {
        public readonly IServiceProvider ServiceProvider;
        public readonly IEventBusSubscriptionManager SubsManager;
        private EventBusConfig _eventBusConfig;

        protected BaseEventBus(IServiceProvider serviceProvider, EventBusConfig eventBusConfig)
        {
            ServiceProvider = serviceProvider;
            SubsManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
            _eventBusConfig = eventBusConfig;
            
        }

        public virtual string ProcessEventName(string eventName)
        {
            if (_eventBusConfig.DeleteEventPrefix)
            {
                eventName = eventName.TrimStart(_eventBusConfig.EventNamePrefix.ToArray());
            }
            if (_eventBusConfig.DeleteEventSuffix)
            {
                eventName = eventName.TrimEnd(_eventBusConfig.EventNameSuffix.ToArray());
            }
            return eventName;
        }
        public virtual string GetSubName(string eventName)
        {
            return $"{_eventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
        }

        public virtual void Dispose()
        {
            _eventBusConfig = null;
        }

        public async Task<bool> ProccesEvent(string eventName,string message)
        {
            eventName = ProcessEventName(eventName);

            var procesed = false;

            if (SubsManager.HasSubscriptionsForEvent(eventName))
            {
                var subscriptions = SubsManager.GetHandlersForEvent(eventName);
                using (var scope = ServiceProvider.CreateScope())
                {
                    foreach (var subscription in subscriptions)
                    {
                        var handler = ServiceProvider.GetService(subscription.HandlerType);

                        if (handler == null) continue;

                        var eventType = SubsManager.GetEventTypeByName($"{_eventBusConfig.EventNamePrefix}{eventName}{_eventBusConfig.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        var concrateType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concrateType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
                    }
                }
                procesed = true;
            }

            return procesed;
        }

        public void Publish(IntegrationEvent @event)
        {
            throw new NotImplementedException();
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            throw new NotImplementedException();
        }

        public void UnSubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            throw new NotImplementedException();
        }
    }
}

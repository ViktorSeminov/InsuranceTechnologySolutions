using Claims.Application.Common.Events;

namespace Claims.Infrastructure.Events
{
    public class InMemoryEventDispatcher: IEventDispatcher
    {
        private readonly IServiceProvider _serviceProvider;

        public InMemoryEventDispatcher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task DispatchAsync<TEvent>(TEvent domainEvent)
        {
            var handlers = _serviceProvider
                .GetServices<IEventHandler<TEvent>>();

            foreach (var handler in handlers)
            {
                await handler.HandleAsync(domainEvent);
            }
        }
    }
}


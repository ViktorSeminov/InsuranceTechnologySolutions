namespace Claims.Application.Common.Events
{
    public interface IEventDispatcher
    {
        Task DispatchAsync<TEvent>(TEvent domainEvent);
    }
}

namespace Claims.Application.Common.Events
{
    public interface IEventHandler<in TEvent>
    {
        Task HandleAsync(TEvent domainEvent);
    }
}

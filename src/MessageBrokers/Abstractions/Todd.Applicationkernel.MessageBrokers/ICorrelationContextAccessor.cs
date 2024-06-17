namespace Todd.Applicationkernel.MessageBrokers.MessageBrokers;

public interface ICorrelationContextAccessor
{
    object CorrelationContext { get; set; }
}
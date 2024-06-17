namespace Todd.Applicationkernel.MessageBrokers.MessageBrokers;

public interface IMessagePropertiesAccessor
{
    IMessageProperties MessageProperties { get; set; }
}
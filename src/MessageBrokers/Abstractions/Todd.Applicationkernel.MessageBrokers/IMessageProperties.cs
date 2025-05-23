using System.Collections.Generic;

namespace Todd.Applicationkernel.MessageBrokers;

public interface IMessageProperties
{
    string MessageId { get; }
    string CorrelationId { get; }
    long Timestamp { get; }
    IDictionary<string, object> Headers { get; }
}
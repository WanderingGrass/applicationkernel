using System.Collections.Generic;
using System.Threading.Tasks;

namespace Todd.Applicationkernel.MessageBrokers.MessageBrokers;

public interface IBusPublisher
{
    Task PublishAsync<T>(T message, string messageId = null, string correlationId = null, string spanContext = null,
        object messageContext = null, IDictionary<string, object> headers = null) where T : class;
}

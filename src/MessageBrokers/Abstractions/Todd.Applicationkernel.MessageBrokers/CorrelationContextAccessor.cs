using System.Threading;

namespace Todd.Applicationkernel.MessageBrokers.MessageBrokers;

public class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<CorrelationContextHolder>
        Holder = new();

    public object CorrelationContext
    {
        get => Holder.Value?.Context;
        set
        {
            var holder = Holder.Value;
            if (holder != null)
            {
                holder.Context = null;
            }

            if (value != null)
            {
                Holder.Value = new CorrelationContextHolder {Context = value};
            }
        }
    }

    private class CorrelationContextHolder
    {
        public object Context;
    }
}

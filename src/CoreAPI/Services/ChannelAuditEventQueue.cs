using System.Threading.Channels;
using CoreAPI.Models;

namespace CoreAPI.Services
{
    public class ChannelAuditEventQueue : IAuditEventQueue
    {
        private readonly Channel<AuditEventRequest> _queue;

        public ChannelAuditEventQueue()
        {
            var options = new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };

            // Channel<T> is the in-process queue for the producer/consumer pattern.
            // Producers write audit events quickly; one background consumer reads them.
            _queue = Channel.CreateBounded<AuditEventRequest>(options);
        }

        public ValueTask QueueAsync(AuditEventRequest auditEvent, CancellationToken cancellationToken = default)
        {
            return _queue.Writer.WriteAsync(auditEvent, cancellationToken);
        }

        public ValueTask<AuditEventRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}

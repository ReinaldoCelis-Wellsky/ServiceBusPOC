using Microsoft.Azure.ServiceBus;

namespace ServiceBusPOC.Processor
{
    public interface IFormProcessor
    {
        public Task StartProcessingAsync();
        public Task ProcessMessageAsync(Message message, CancellationToken token);
    }
}
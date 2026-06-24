using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;

namespace ChineseAuction.Api.Services
{
    public class KafkaProducerService : IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;

        public KafkaProducerService(IConfiguration configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? throw new ArgumentException("Kafka:BootstrapServers configuration is missing");
            _topic = configuration["Kafka:LotteryTopic"] ?? throw new ArgumentException("Kafka:LotteryTopic configuration is missing");

            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task SendLotteryEventAsync(object lotteryData)
        {
            if (lotteryData == null) throw new ArgumentNullException(nameof(lotteryData));

            var json = JsonSerializer.Serialize(lotteryData);
            var message = new Message<Null, string> { Value = json };

            await _producer.ProduceAsync(_topic, message).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _producer?.Flush(TimeSpan.FromSeconds(5));
            _producer?.Dispose();
        }
    }
}

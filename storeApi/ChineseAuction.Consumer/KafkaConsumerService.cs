using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChineseAuction.Consumer
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<KafkaConsumerService> _logger;

        public KafkaConsumerService(IConfiguration configuration, ILogger<KafkaConsumerService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => ConsumeLoop(stoppingToken), stoppingToken);
        }

        private void ConsumeLoop(CancellationToken stoppingToken)
        {
            var bootstrap = _configuration["KafkaSettings:BootstrapServers"];
            var topic = _configuration["KafkaSettings:Topic"];
            var groupId = _configuration["KafkaSettings:GroupId"];

            var config = new ConsumerConfig
            {
                BootstrapServers = bootstrap,
                GroupId = groupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            consumer.Subscribe(topic);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var cr = consumer.Consume(stoppingToken);
                        if (cr?.Message != null)
                        {
                            // Log received JSON string
                            _logger.LogInformation("Received message: {Message}", cr.Message.Value);

                            // Manually commit the offset
                            try
                            {
                                consumer.Commit(cr);
                            }
                            catch (Exception commitEx)
                            {
                                _logger.LogError(commitEx, "Failed to commit offset");
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Consume error");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error while consuming");
                    }
                }
            }
            finally
            {
                try
                {
                    consumer.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error closing consumer");
                }
            }
        }
    }
}

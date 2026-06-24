using ChineseAuction.Consumer;

var builder = Host.CreateApplicationBuilder(args);
// builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<KafkaConsumerService>();

var host = builder.Build();
host.Run();

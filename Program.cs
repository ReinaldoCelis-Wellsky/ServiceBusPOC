using Amazon.DynamoDBv2;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceBusPOC.Processor;

var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

var serviceProvider = new ServiceCollection()
                .AddSingleton(configuration)
                .AddSingleton<ISubscriptionClient>(new SubscriptionClient(configuration["ServiceBusConnectionString"], configuration["ServiceBusTopicName"], configuration["ServiceBusSubscriptionName"]))
                .AddSingleton<IAmazonDynamoDB>(new AmazonDynamoDBClient(configuration["AWSAccessKey"], configuration["AWSSecretKey"], Amazon.RegionEndpoint.GetBySystemName(configuration["AWSRegion"])))
                .AddSingleton<FormProcessor>()
                .BuildServiceProvider();

var formProcessor = serviceProvider.GetService<FormProcessor>();
await formProcessor.StartProcessingAsync();

Console.WriteLine("Press any key to exit...");
Console.ReadKey();
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Azure.ServiceBus;

namespace ServiceBusPOC.Processor
{
    public class FormProcessor : IFormProcessor
    {
        private readonly ISubscriptionClient _subscriptionClient;
        private readonly IAmazonDynamoDB _dynamoDbClient;

        public FormProcessor(ISubscriptionClient subscriptionClient, IAmazonDynamoDB dynamoDbClient)
        {
            _subscriptionClient = subscriptionClient;
            _dynamoDbClient = dynamoDbClient;
        }

        public async Task StartProcessingAsync()
        {
            // Register the message handler and receive messages in a loop
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false
            };

            _subscriptionClient.RegisterMessageHandler(ProcessMessageAsync, messageHandlerOptions);

            Console.WriteLine("Waiting for messages...");
            await Task.Delay(Timeout.Infinite);
        }

        public async Task ProcessMessageAsync(Message message, CancellationToken token)
        {
            try
            {
                // Convert the message body to a string
                var messageBody = Encoding.UTF8.GetString(message.Body);

                Console.WriteLine($"Received message: {messageBody}");

                // Get the record from the DynamoDB table
                var response = await _dynamoDbClient.GetItemAsync(new GetItemRequest
                {
                    TableName = "custom_forms",
                    Key = new Dictionary<string, AttributeValue>
                    {
                        { "id", new AttributeValue { S = messageBody } }
                    }
                });

                if (response.Item != null)
                {
                    Console.WriteLine($"Found record in DynamoDB: {response.Item["id"].S}");

                    // Add your custom logic here to process the record


                    // For example, you could publish the record to another Azure Service Bus topic
                    var connectionString = "<YourAzureServiceBusConnectionString>";
                    var topicName = "<YourAzureServiceBusTopicName>";
                    var messageBodyBytes = Encoding.UTF8.GetBytes(messageBody);
                    var topicClient = new TopicClient(connectionString, topicName);
                    var topicMessage = new Message(messageBodyBytes);
                    await topicClient.SendAsync(topicMessage);

                    Console.WriteLine($"Published message to topic: {topicName}");
                }
                else
                {
                    Console.WriteLine($"Record not found in DynamoDB: {messageBody}");
                }

                // Mark the message as completed
                await _subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                await _subscriptionClient.AbandonAsync(message.SystemProperties.LockToken);
            }
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs args)
        {
            Console.WriteLine($"Message handler encountered an exception: {args.Exception.Message}");
            return Task.CompletedTask;
        }
    }
}

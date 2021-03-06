using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using System.IO;


namespace StreamingProcessor
{
    public static class Test0
    {
        /*
         * Cosmos DB output using Azure Function binding
         */
        [FunctionName("Test0")]
        public static async Task RunAsync(
            [KafkaTrigger("%KafkaBrokers%", "%EventHubName%",
            ConsumerGroup = "%ConsumerGroup%",
            EventHubConnectionString = "EventHubsConnectionString", 
            Protocol = BrokerProtocol.SaslSsl, 
            AuthenticationMode = BrokerAuthenticationMode.Plain,
            SslCaLocation =  "D:\\home\\site\\wwwroot\\cacert.pem", // TODO: come with a better way to specify the path in KafkaTrigger
            Username = "$ConnectionString", 
            Password = "EventHubsConnectionString")] KafkaEventData<string>[] kafkaEvents, 
            [CosmosDB(databaseName: "%CosmosDBDatabaseName%", collectionName: "%CosmosDBCollectionName%", ConnectionStringSetting = "CosmosDBConnectionString")] IAsyncCollector<JObject> cosmosMessage,
            ILogger log)
        {
            var tasks = new List<Task>();
            long len = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            foreach (var data in kafkaEvents)
            {
                try
                {
                    string message = data.Value;
                    len += message.Length;


                    var document = JObject.Parse(message);
                    document["id"] = document["eventId"];
                    document["enqueuedAt"] = data.Timestamp;
                    document["processedAt"] = DateTime.UtcNow;

                    tasks.Add(cosmosMessage.AddAsync(document));
                }
                catch (Exception ex)
                {
                    log.LogError($"{ex} - {ex.Message}");
                }
            }

            await Task.WhenAll(tasks);

            sw.Stop();

            string logMessage = $"[Test0] T:{len} doc - E:{sw.ElapsedMilliseconds} msec";
            if (len > 0)
            {
                logMessage += Environment.NewLine + $"AVG:{(sw.ElapsedMilliseconds / len):N3} msec";
            }

            log.LogInformation(logMessage);
        }
    }
}

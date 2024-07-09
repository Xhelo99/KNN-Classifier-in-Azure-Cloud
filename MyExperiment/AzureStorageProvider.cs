using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCloudProject.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyExperiment
{
    public class AzureStorageProvider : IStorageProvider
    {
        private MyConfig _config;
        private ILogger logger;

        public AzureStorageProvider(IConfigurationSection configSection)
        {
            _config = new MyConfig();
            configSection.Bind(_config);

        }

        public async Task CommitRequestAsync(IExerimentRequest request)
        {
            // Delete the message from the 
            var queueClient = new QueueClient(_config.StorageConnectionString, this._config.Queue);
            await queueClient.DeleteMessageAsync(request.MessageId, request.MessageReceipt);
        }
      

        // Download the dataset from my blob storage
        public async Task<string> DownloadInputAsync(string fileName)
        {
            BlobContainerClient container = new BlobContainerClient(_config.StorageConnectionString, _config.TrainingContainer);
            await container.CreateIfNotExistsAsync();

            // Geting a reference to a blob by its name.
            BlobClient blob = container.GetBlobClient(fileName);

            // Downloading the blob to the specified local file.
            await blob.DownloadToAsync(fileName);

            return fileName;


        }

        public async Task<IExerimentRequest> ReceiveExperimentRequestAsync(CancellationToken token)
        {
            // Initialize a QueueClient for processing messages from a queue
            QueueClient queueClient = new QueueClient(this._config.StorageConnectionString, this._config.Queue);

           
                // Receive a message from the queue
                QueueMessage message = await queueClient.ReceiveMessageAsync();

                if (message != null)
                {
                    try
                    {
                        // Processing of the the received message
                        string msgTxt = Encoding.UTF8.GetString(message.Body.ToArray());
                        logger?.LogInformation($"Received the message {msgTxt}");
                        ExerimentRequestMessage request = JsonSerializer.Deserialize<ExerimentRequestMessage>(msgTxt);
                        request.MessageId = message.MessageId;
                        request.MessageReceipt = message.PopReceipt;
                        return request;

                    }
                    catch (JsonException jsonEx)
                    {
                        logger?.LogError(jsonEx, "JSON deserialization failed for the message");
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Something went wrong while running the experiment");
                    }
                }
                else
                {
                    this.logger?.LogInformation("The message is null");
                }
            

            return null;
        }


        // Uploading experiment results to Azure Table Storage
        public Task UploadExperimentResult(IExperimentResult result)
        {
            throw new NotImplementedException();
        }

        public async Task UploadResultAsync(string experimentName, IExperimentResult result)
        {
            try
            {
                // New instance of the TableClient class
                TableServiceClient tableServiceClient = new TableServiceClient(this._config.StorageConnectionString);
                TableClient tableClient = tableServiceClient.GetTableClient(tableName: this._config.ResultTable);
                await tableClient.CreateIfNotExistsAsync();

                // Creating a table entity from the result
                var entity = new TableEntity(experimentName, "KnnClassifier")
             {
                { "StartTimeUtc", result.StartTimeUtc },
                { "EndTimeUtc", result.EndTimeUtc },
                { "ExperimentId", result.ExperimentId },
                { "DurationSec", result.DurationSec },
                { "InputFileUrl", result.InputFileUrl },
                { "Accuracy", result.Accuracy },
                //{ "OutputFiles", string.Join(",", result.OutputFiles) }
            };
              
                // Adding the newly created entity to the Azure Table.
                await tableClient.AddEntityAsync(entity);
               
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to upload to Table Storage: " + ex.ToString());
            }
        }
    }


}

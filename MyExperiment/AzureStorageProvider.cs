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
        private QueueClient _queueClient;
        private ILogger logger;

        public AzureStorageProvider(IConfigurationSection configSection)
        {
            _config = new MyConfig();
            configSection.Bind(_config);

            _queueClient = new QueueClient(_config.StorageConnectionString, _config.Queue);
        }

        public Task CommitRequestAsync(IExerimentRequest request)
        {
            throw new NotImplementedException();
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

            while (token.IsCancellationRequested == false)
            {
                // Receive a message from the queue
                QueueMessage message = await queueClient.ReceiveMessageAsync();

                if (message != null)
                {
                    try
                    {
                        // Processing of the the received message
                        string msgTxt = Encoding.UTF8.GetString(message.Body.ToArray());
                        logger?.LogInformation($"Received the message {msgTxt}");
                        IExerimentRequest request = JsonSerializer.Deserialize<IExerimentRequest>(msgTxt);

                        // Download input file, run the experiment, and upload results
                        var inputFile = await DownloadInputAsync(request.InputFile);

                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Something went wrong while running the experiment");
                    }
                }
                else
                {
                    await Task.Delay(500);
                    logger?.LogTrace("Queue empty...");
                }
            }
            this.logger?.LogInformation("Cancel pressed. Exiting the listener loop.");
        }


        // Uploading experiment results to Azure Table Storage
        public Task UploadExperimentResult(IExperimentResult result)
        {
            throw new NotImplementedException();
        }

        public Task UploadResultAsync(string experimentName, IExperimentResult result)
        {
            throw new NotImplementedException();
        }
    }


}

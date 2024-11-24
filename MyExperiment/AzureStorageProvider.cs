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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MyExperiment
{
    public class AzureStorageProvider : IStorageProvider
    {
        private MyConfig _config;
        private ILogger logger;

        /// <summary>
        /// Initializes a new instance of the AzureStorageProvider class.
        /// </summary>
        /// <param name="configSection">The configuration section.</param>
        public AzureStorageProvider(IConfigurationSection configSection)
        {
            _config = new MyConfig();
            configSection.Bind(_config);
        }

        /// <summary>
        /// Deletes the message from the queue.
        /// </summary>
        /// <param name="request">The experiment request.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task CommitRequestAsync(IExerimentRequest request)
        {
            var queueClient = new QueueClient(_config.StorageConnectionString, this._config.Queue);
            await queueClient.DeleteMessageAsync(request.MessageId, request.MessageReceipt);
        }

        /// <summary>
        /// Downloads the dataset from the blob storage.
        /// </summary>
        /// <param name="fileName">The name of the file to download.</param>
        /// <returns>The path to the downloaded file.</returns>
        public async Task<string> DownloadInputAsync(string fileName)
        {
            BlobContainerClient container = new BlobContainerClient(_config.StorageConnectionString, _config.TrainingContainer);
            await container.CreateIfNotExistsAsync();

            // Get a reference to the blob by its name.
            BlobClient blob = container.GetBlobClient(fileName);

            // Download the blob to the specified local file.
            await blob.DownloadToAsync(fileName);

            return fileName;
        }

        /// <summary>
        /// Awaits and processes the queue message.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The experiment request, or null if no message was received.</returns>
        public async Task<IExerimentRequest> ReceiveExperimentRequestAsync(CancellationToken token)
        {
            QueueClient queueClient = new QueueClient(this._config.StorageConnectionString, this._config.Queue);

            // Receive a message from the queue.
            QueueMessage message = await queueClient.ReceiveMessageAsync();

            if (message != null)
            {
                try
                {
                    // Process the received message.
                    string msgTxt = Encoding.UTF8.GetString(message.Body.ToArray());
                    ExerimentRequestMessage request = JsonSerializer.Deserialize<ExerimentRequestMessage>(msgTxt);
                    request.MessageId = message.MessageId;
                    request.MessageReceipt = message.PopReceipt;
                    return request;
                }
                catch (JsonException jsonEx)
                {
                    logger?.LogError(jsonEx, "JSON deserialization failed for the message");
                    Console.Error.WriteLine("The message sent is not correctly formatted. Please send another message.");
                }
            }
            else
            {
                this.logger?.LogInformation("The message is null");
            }

            return null;
        }

        /// <summary>
        /// Uploads the experiment results to Azure Table Storage.
        /// </summary>
        /// <param name="result">The experiment result to upload.</param>
        public async Task UploadExperimentResult(IExperimentResult result)
        {
            try
            {
                // Create a new instance of the TableClient class.
                TableServiceClient tableServiceClient = new TableServiceClient(this._config.StorageConnectionString);
                TableClient tableClient = tableServiceClient.GetTableClient(tableName: this._config.ResultTable);
                await tableClient.CreateIfNotExistsAsync();

                // Generate a unique RowKey.
                string uniqueRowKey = Guid.NewGuid().ToString();

                // Create a table entity from the result.
                var entity = new TableEntity(this._config.ResultTable, uniqueRowKey)
                {
                    { "ExperimentId", result.ExperimentId },
                    { "Name", result.Name },
                    { "Decription", result.Description },
                    { "StartTimeUtc", result.StartTimeUtc },
                    { "EndTimeUtc", result.EndTimeUtc },
                    { "DurationSec", result.DurationSec },
                    { "InputFileUrl", result.InputFileUrl },
                    { "OutputFileUrl", result.OutputFileUrl },
                    { "Accuracy", result.Accuracy },
                };

                // Add the newly created entity to Azure Table.
                await tableClient.AddEntityAsync(entity);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to upload to Table Storage: ", ex.ToString());
            }
        }

        /// <summary>
        /// Uploads the experiment output file to the blob container.
        /// </summary>
        /// <param name="experimentName">The name of the experiment.</param>
        /// <param name="result">The experiment result containing the output file.</param>
        public async Task UploadResultAsync(string experimentName, IExperimentResult result)
        {
            string outputFile = result.OutputFiles[0];
            result.OutputFileUrl = experimentName;

            // Initialize the BlobServiceClient and BlobContainerClient.
            BlobServiceClient blobServiceClient = new BlobServiceClient(this._config.StorageConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(this._config.ResultContainer);

            await containerClient.CreateIfNotExistsAsync();

            BlobClient blobClient = containerClient.GetBlobClient($"{Path.GetFileName(outputFile)}");

            await blobClient.UploadAsync(outputFile, overwrite: true);
        }
    }
}

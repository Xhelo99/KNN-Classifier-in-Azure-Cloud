
﻿using MyCloudProject.Common;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Threading;
using MyExperiment;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using Azure.Storage.Queues;
using System.Text.Json;
using System.Text;
using NeoCortexApi.Entities;

namespace MyCloudProject
{
    class Program
    {
        /// <summary>
        /// Your project ID from the last semester.
        /// </summary>
        private static string _projectName = "ML 22/23-2 Investigate and Implement KNN Classifier";

        //string test;

        static async Task Main(string[] args)
        {
            CancellationTokenSource tokeSrc = new CancellationTokenSource();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                tokeSrc.Cancel();
            };

            Console.WriteLine($"Started experiment: {_projectName}");

            // Init configuration
            var cfgRoot = Common.InitHelpers.InitConfiguration(args);

            var cfgSec = cfgRoot.GetSection("MyConfig");

            // InitLogging
            var logFactory = InitHelpers.InitLogging(cfgRoot);
          
            var logger = logFactory.CreateLogger("Train.Console");

            logger?.LogInformation($"{DateTime.Now} -  Started experiment: {_projectName}");

            IStorageProvider storageProvider = new AzureStorageProvider(cfgSec);
            IExperiment experiment = new Experiment(cfgSec, storageProvider, logger);

            // Implements the step 3 in the architecture picture.
            while (tokeSrc.Token.IsCancellationRequested == false)
            {
                // Wait for the queue message
                IExerimentRequest request = await storageProvider.ReceiveExperimentRequestAsync(tokeSrc.Token);
             
                if (request != null)
                {
                    try
                    {
                        // Method to set the messaga parameters to Experiment class
                        experiment.setExperimentDetails(request.ExperimentId, request.Name, request.Description);

                        logger?.LogInformation($"The message with Id: {request.ExperimentId} is received. " +
                            $"The dataset will be downloaded from Blob storage.");

                        // Download dataset from Blob storage
                        var localFileWithInputArgs = await storageProvider.DownloadInputAsync(request.InputFile);
          
                        logger?.LogInformation($"The dataset {localFileWithInputArgs} has successfully been downloaded. " +
                            $"The SE project will start.");

                        logger?.LogInformation("Hello NeoCortexApi! Multisequence Experiment started...");

                        // Run SE Project code
                        IExperimentResult result = await experiment.RunAsync(localFileWithInputArgs);

                        logger?.LogInformation($"The experiment has finished and the experiment output will be uploaded " +
                            $"to the container.");

                        // Upload the experiment output to blob container. 
                        await storageProvider.UploadResultAsync("outputfile", result);

                        logger?.LogInformation("The result is uploading to the Table Storage.");

                        // Upload the results to the table
                        await storageProvider.UploadExperimentResult(result);

                        logger?.LogInformation("Uploaded to Table Storage successfully");

                        // Delete the message from the queue
                        await storageProvider.CommitRequestAsync(request);

                        logger?.LogInformation("The message has been deleted from the queue and the program is waiting for another message.");
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

            logger?.LogInformation($"{DateTime.Now} -  Experiment exit: {_projectName}");
        }


    }
}

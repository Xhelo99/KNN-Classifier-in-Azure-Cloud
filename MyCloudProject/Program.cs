using MyCloudProject.Common;
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

namespace MyCloudProject
{
    class Program
    {
        /// <summary>
        /// Your project ID from the last semester.
        /// </summary>
        private static string _projectName = "ML 22/23-2";

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

            IExperiment experiment = new Experiment(cfgSec, storageProvider, logger, _projectName);

            //
            // Implements the step 3 in the architecture picture.
            while (tokeSrc.Token.IsCancellationRequested == false)
            {
                // Step 3
                IExerimentRequest request = await storageProvider.ReceiveExperimentRequestAsync(tokeSrc.Token);

                if (request != null)
                {
                    try
                    {
                        
                        logger?.LogInformation($"The message with Id: {request.ExperimentId} is received. and" +
                            $"the dataset will be downloaded from Blob storage.");

                        //Download dataset from Blob storage
                        var localFileWithInputArgs = await storageProvider.DownloadInputAsync(request.InputFile);
          
                        logger?.LogInformation($"The dataset {localFileWithInputArgs} has successfully been downloaded.");

                        // Run SE Project code
                        IExperimentResult result = await experiment.RunAsync(localFileWithInputArgs);

                        logger?.LogInformation($"The experiment has finished and now the results will be uploaded to table.");

                        // Upload the results to the table
                        await storageProvider.UploadResultAsync("outputfile", result);

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

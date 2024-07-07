using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyCloudProject.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace MyExperiment
{
    /// <summary>
    /// This class implements the ML experiment that will run in the cloud. This is refactored code from my SE project.
    /// </summary>
    public class Experiment : IExperiment
    {
        private IStorageProvider storageProvider;

        private ILogger logger;

        private MyConfig config;

        private string projectName;

        public Experiment(IConfigurationSection configSection, IStorageProvider storageProvider, ILogger log, string projectName)
        {
            this.storageProvider = storageProvider;
            this.logger = log;

            config = new MyConfig();
            configSection.Bind(config);

            this.projectName = projectName;
        }


        public Task<IExperimentResult> RunAsync(string inputData)
        {
            // Create output file 
            var outputFile = "output.txt";

            // Read  inputData file
            var text = File.ReadAllText(inputData, Encoding.UTF8);
            var sequences = JsonSerializer.Deserialize<Test>(text);
            

            //  This creates an instance of MultiSequenceLearning and run the method
            MultiSequenceLearning experiment = new MultiSequenceLearning();

            ExperimentResult res = new ExperimentResult(this.config.GroupId, "1");

            res.StartTimeUtc = DateTime.UtcNow;

            experiment.Run(sequences.Train);

            // logging, logging and logging

            res.Timestamp = DateTime.UtcNow;
            res.EndTimeUtc = DateTime.UtcNow;
            res.ExperimentId = projectName;
            var elapsedTime = res.EndTimeUtc - res.StartTimeUtc;
            res.DurationSec = (long)elapsedTime.GetValueOrDefault().TotalSeconds;
            res.InputFileUrl = inputData;
            res.Accuracy = experiment.accuracy;
            res.OutputFiles = new string[] { outputFile };

            return Task.FromResult<IExperimentResult>(res); // TODO...
        }
    }
}

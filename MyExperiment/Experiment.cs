
﻿using Azure.Storage.Queues;
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
using System.Diagnostics;
using NeoCortexApi;
using System.Linq;

namespace MyExperiment
{
    /// <summary>
    /// This class implements a machine learning experiment that will run in the cloud.
    /// It is refactored from a previous software engineering project.
    /// </summary>
    public class Experiment : IExperiment
    {
        private IStorageProvider storageProvider;

        private ILogger logger;

        private MyConfig config;

        private string experimentId;

        private string experimentName;

        private string experimentDescription;
        public Experiment(IConfigurationSection configSection, IStorageProvider storageProvider, ILogger log)
        {
            this.storageProvider = storageProvider;
            this.logger = log;

            config = new MyConfig();
            configSection.Bind(config);               
        }

        /// <summary>
        /// Sets the experiment details such as ID, name, and description.
        /// </summary>
        /// <param name="experimentId">The unique identifier for the experiment.</param>
        /// <param name="experimentName">The name of the experiment.</param>
        /// <param name="experimentDescription">A brief description of the experiment.</param>
        public void setExperimentDetails(string experimentId, string experimentName, string experimentDescription) 
        {
            this.experimentId = experimentId;
            this.experimentName = experimentName;
            this.experimentDescription = experimentDescription;
        }

        /// <summary>
        /// Runs the experiment asynchronously and returns the experiment results.
        /// </summary>
        /// <param name="inputData">The path to the input data file.</param>
        /// <returns>A task representing the asynchronous operation, containing the experiment results.</returns>
        public Task<IExperimentResult> RunAsync(string inputData)
        {
            // Create output file 
            var outputFile = "output.txt";

            // Read  inputData file
            var text = File.ReadAllText(inputData, Encoding.UTF8);
            var sequences = JsonSerializer.Deserialize<Test>(text);
            

            //  This creates an instance of MultiSequenceLearning and run the method
            MultiSequenceLearning experiment = new MultiSequenceLearning();

            // These list are used to see how the prediction works.
            // Predictor is traversing the list element by element. 
            // By providing more elements to the prediction, the predictor delivers more precise result.
            var list1 = new double[] { 1.0, 2.0, 3.0, 4.0, 2.0, 5.0 };
            var list2 = new double[] { 2.0, 3.0, 4.0 };
            var list3 = new double[] { 8.0, 1.0, 2.0 };


            ExperimentResult res = new ExperimentResult(this.config.GroupId, "1");

            res.StartTimeUtc = DateTime.UtcNow;

            // Console output to outpufile
            using (StreamWriter writer = new StreamWriter(outputFile))
            {
                Console.SetOut(writer);
                var predictor = experiment.Run(sequences.Train);

                predictor.Reset();
                PredictNextElement(predictor, list1);

                predictor.Reset();
                PredictNextElement(predictor, list2);

                predictor.Reset();
                PredictNextElement(predictor, list3);
                writer.Flush();
            }

            void PredictNextElement(Predictor predictor, double[] list)
            {
                Console.WriteLine("------------------------------");

                foreach (var item in list)
                {
                    var res = predictor.Predict(item);

                    if (res.Count > 0)
                    {
                        foreach (var pred in res)
                        {
                            Console.WriteLine($"{pred.PredictedInput} - {pred.Similarity}");
                        }

                        var tokens = res.First().PredictedInput.Split('_');
                        var tokens2 = res.First().PredictedInput.Split('-');
                        Console.WriteLine($"Predicted Sequence: {tokens[0]}, predicted next element {tokens2.Last()}");
                    }
                    else
                        Console.WriteLine("Nothing predicted :(");
                }

                Console.WriteLine("------------------------------");
            }

            res.Timestamp = DateTime.UtcNow;
            res.EndTimeUtc = DateTime.UtcNow;
            res.ExperimentId = experimentId;
            res.Name = experimentName;
            res.Description = experimentDescription;
            var elapsedTime = res.EndTimeUtc - res.StartTimeUtc;
            res.DurationSec = (long)elapsedTime.GetValueOrDefault().TotalSeconds;
            res.InputFileUrl = inputData;
            res.OutputFileUrl = outputFile;
            res.Accuracy = experiment.accuracy;
            res.OutputFiles = new string[] { outputFile };
            

            return Task.FromResult<IExperimentResult>(res); 
        }
    }
}

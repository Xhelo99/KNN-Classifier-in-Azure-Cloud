
﻿using NeoCortexApi.Classifiers;
using NeoCortexApi.Entities;
using NeoCortexApi.Network;
using NeoCortexApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyExperiment
{
    public class Predictor
    {
        private Connections connections { get; set; }

        private CortexLayer<object, object> layer { get; set; }

        private KnnClassifier<string, ComputeCycle> classifier { get; set; }

        /// <summary>
        /// Initializes the predictor functionality.
        /// </summary>
        /// <param name="layer">The HTM Layer.</param>
        /// <param name="connections">The HTM memory in the learned state.</param>
        /// <param name="classifier">The classifier that contains the state of learned sequences.</param>

        public Predictor(CortexLayer<object, object> layer, Connections connections, KnnClassifier<string, ComputeCycle> classifier)
        {
            this.connections = connections;
            this.layer = layer;
            this.classifier = classifier;
        }

        /// <summary>
        /// Starts predicting of the next subsequences.
        /// </summary>
        public void Reset()
        {
            var tm = this.layer.HtmModules.FirstOrDefault(m => m.Value is TemporalMemory);
            ((TemporalMemory)tm.Value).Reset(this.connections);
        }


        /// <summary>
        /// Predicts the list of next expected elements.
        /// </summary>
        /// <param name="input">The element that will cause the next expected element.</param>
        /// <returns>The list of expected (predicting) elements.</returns>
        public List<ClassifierResult<string>> Predict(double input)
        {
            var lyrOut = this.layer.Compute(input, false) as ComputeCycle;

            List<ClassifierResult<string>> predictedInputValues = this.classifier.GetPredictedInputValues(lyrOut.PredictiveCells.ToArray(), 3);

            return predictedInputValues;
        }

        public void Serialize(object obj, string name, StreamWriter sw)
        {
            this.connections.Serialize(obj, name, sw);
        }

        public static object Deserialize<T>(StreamReader sr, string name)
        {
            throw new NotImplementedException();
        }
    }
}

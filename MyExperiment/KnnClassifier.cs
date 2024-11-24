
﻿using NeoCortexApi.Classifiers;
using NeoCortexApi.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyExperiment
{
    /// <summary>
    /// Defines a generic K-nearest neighbors (KNN) classifier class.
    /// </summary>
    public class KnnClassifier<TInput, TOutput> : IClassifier<string, ComputeCycle>
    {
        // Dictionary to store training data.
        private Dictionary<string, List<Cell[]>> trainingData;
        private short k;  // Parameter for KNN.

        /// <summary>
        /// Constructor to initialize the training data dictionary and k value.
        /// </summary>
        /// <param name="k">The number of nearest neighbors to consider.</param>
        public KnnClassifier(short k = 3)
        {
            trainingData = new Dictionary<string, List<Cell[]>>();
            this.k = k;
        }

        /// <summary>
        /// Adds new training data.
        /// </summary>
        /// <param name="input">The input label.</param>
        /// <param name="output">The corresponding output (cell array).</param>
        public void Learn(string input, Cell[] output)
        {
            // If the input label is not present in the training data, add a new entry.
            if (!trainingData.ContainsKey(input))
            {
                trainingData[input] = new List<Cell[]>();
            }

            // Add the output (cell array) corresponding to the input label.
            trainingData[input].Add(output);
        }

        /// <summary>
        /// Gets the predicted input value based on the KNN algorithm with the default parameter k=1.
        /// </summary>
        /// <param name="predictiveCells">The predictive cells.</param>
        /// <returns>The predicted input value.</returns>
        public string GetPredictedInputValue(Cell[] predictiveCells)
        {
            return Classify(predictiveCells, 1);
        }

        /// <summary>
        /// Gets predicted input values based on the KNN algorithm.
        /// </summary>
        /// <param name="cellIndices">The indices of the cells to predict.</param>
        /// <param name="howMany">The number of nearest neighbors to consider.</param>
        /// <returns>A list of predicted input values and their similarities.</returns>
        public List<ClassifierResult<string>> GetPredictedInputValues(int[] cellIndices, short howMany = 1)
        {
            // Convert cell indices to an array of Cells (this is needed for compatibility with your existing methods).
            Cell[] predictiveCells = cellIndices.Select(index => new Cell { Index = index }).ToArray();

            return GetPredictedInputValues(predictiveCells, howMany);
        }

        /// <summary>
        /// Gets predicted input values based on the KNN algorithm with the specified parameter k.
        /// </summary>
        /// <param name="predictiveCells">The predictive cells.</param>
        /// <param name="k">The number of nearest neighbors to consider.</param>
        /// <returns>A list of predicted input values and their similarities.</returns>
        public List<ClassifierResult<string>> GetPredictedInputValues(Cell[] predictiveCells, short k)
        {
            // Dictionary to store distances between training samples and predictive cells.
            var distances = new Dictionary<string, double>();

            // Calculate the distance between predictive cells and each training sample.
            foreach (var entry in trainingData)
            {
                double distance = CalculateDistance(predictiveCells, entry.Value);
                distances.Add(entry.Key, distance);
            }

            // Sort distances in ascending order.
            var sortedDistances = distances.OrderBy(x => x.Value);

            // Get the top k distances.
            var topDistances = sortedDistances.Take(k);

            // List to store predicted input values and their similarities.
            var results = new List<ClassifierResult<string>>();

            // Iterate through the top k distances and add predicted input values to results.
            foreach (var kvp in topDistances)
            {
                // Add predicted input value with its similarity (inverse of distance).
                results.Add(new ClassifierResult<string> { PredictedInput = kvp.Key, Similarity = 1.0 - kvp.Value });
            }

            return results;
        }

        /// <summary>
        /// Classifies based on the KNN algorithm with the specified parameter k.
        /// </summary>
        /// <param name="predictiveCells">The predictive cells.</param>
        /// <param name="k">The number of nearest neighbors to consider (default is 1).</param>
        /// <returns>The classified input value.</returns>
        public string Classify(Cell[] predictiveCells, short k = 1)
        {
            // Get predicted input values using KNN with the specified parameter k.
            var predictedInputs = GetPredictedInputValues(predictiveCells, k);
            // Return the first predicted input value.
            return predictedInputs.FirstOrDefault()?.PredictedInput;
        }

        /// <summary>
        /// Performs majority voting among KNN predictions with the specified parameter k.
        /// </summary>
        /// <param name="predictiveCells">The predictive cells.</param>
        /// <param name="k">The number of nearest neighbors to consider (default is 1).</param>
        /// <returns>The input value with the highest number of votes.</returns>
        public string Vote(Cell[] predictiveCells, short k = 1)
        {
            // Get predicted input values using KNN with the specified parameter k.
            var predictedInputs = GetPredictedInputValues(predictiveCells, k);
            // Dictionary to store votes for each predicted input value.
            var votes = new Dictionary<string, int>();

            // Count votes for each predicted input value.
            foreach (var result in predictedInputs)
            {
                if (!votes.ContainsKey(result.PredictedInput))
                    votes[result.PredictedInput] = 0;

                votes[result.PredictedInput]++;
            }

            // Return the predicted input value with the highest number of votes.
            return votes.OrderByDescending(v => v.Value).FirstOrDefault().Key;
        }

        /// <summary>
        /// Calculates the distance between the given input cells and the stored training data.
        /// </summary>
        /// <param name="input1">The first set of cells.</param>
        /// <param name="input2">The list of training data cell arrays.</param>
        /// <returns>The minimum distance.</returns>
        private double CalculateDistance(Cell[] input1, List<Cell[]> input2)
        {
            double minDistance = double.MaxValue;

            foreach (var cellArray in input2)
            {
                double distance = 0.0;
                int minLength = Math.Min(input1.Length, cellArray.Length);

                for (int i = 0; i < minLength; i++)
                {
                    distance += input1[i].Index == cellArray[i].Index ? 0 : 1;
                }

                // Handle the extra elements in the longer array.
                distance += Math.Abs(input1.Length - cellArray.Length);

                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }

        /// <summary>
        /// Clears the state of the classifier (clears the training data).
        /// </summary>
        public void ClearState() => trainingData.Clear();
    }
}

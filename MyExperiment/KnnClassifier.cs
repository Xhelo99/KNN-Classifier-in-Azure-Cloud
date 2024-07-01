using NeoCortexApi.Classifiers;
using NeoCortexApi.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyExperiment
{
    // Define a generic K-nearest neighbors (KNN) classifier class
    public class KnnClassifier<TInput, TOutput> : IClassifierKnn<string, ComputeCycle>
    {
        // Dictionary to store training data
        private Dictionary<string, List<Cell[]>> trainingData;
        private short k;  // Parameter for KNN

        // Constructor to initialize training data dictionary and k value
        public KnnClassifier(short k = 3)
        {
            trainingData = new Dictionary<string, List<Cell[]>>();
            this.k = k;
        }

        // Method to add new training data
        public void Learn(string input, Cell[] output)
        {
            // If input label is not present in training data, add a new entry
            if (!trainingData.ContainsKey(input))
            {
                trainingData[input] = new List<Cell[]>();
            }

            // Add the output (cell array) corresponding to the input label
            trainingData[input].Add(output);
        }

        // Method to get predicted input values based on KNN algorithm with parameter k
        public List<ClassifierResult<string>> GetPredictedInputValues(Cell[] predictiveCells, short k)
        {
            // Dictionary to store distances between training samples and predictive cells
            var distances = new Dictionary<string, double>();

            // Calculate distance between predictive cells and each training sample
            foreach (var entry in trainingData)
            {
                double distance = CalculateDistance(predictiveCells, entry.Value);
                distances.Add(entry.Key, distance);
            }

            // Sort distances in ascending order
            var sortedDistances = distances.OrderBy(x => x.Value);

            // Get top k distances
            var topDistances = sortedDistances.Take(k);

            // List to store predicted input values and their similarities
            var results = new List<ClassifierResult<string>>();

            // Iterate through top k distances and add predicted input values to results
            foreach (var kvp in topDistances)
            {
                // Add predicted input value with its similarity (inverse of distance)
                results.Add(new ClassifierResult<string> { PredictedInput = kvp.Key, Similarity = 1.0 - kvp.Value });
            }

            return results;
        }

        // Method to classify based on KNN algorithm with parameter k
        public string Classify(Cell[] predictiveCells, short k = 1)
        {
            // Get predicted input values using KNN with parameter k
            var predictedInputs = GetPredictedInputValues(predictiveCells, k);
            // Return the first predicted input value
            return predictedInputs.FirstOrDefault()?.PredictedInput;
        }

        // Method to perform majority voting among KNN predictions with parameter k
        public string Vote(Cell[] predictiveCells, short k = 1)
        {
            // Get predicted input values using KNN with parameter k
            var predictedInputs = GetPredictedInputValues(predictiveCells, k);
            // Dictionary to store votes for each predicted input value
            var votes = new Dictionary<string, int>();

            // Count votes for each predicted input value
            foreach (var result in predictedInputs)
            {
                if (!votes.ContainsKey(result.PredictedInput))
                    votes[result.PredictedInput] = 0;

                votes[result.PredictedInput]++;
            }

            // Return the predicted input value with the highest number of votes
            return votes.OrderByDescending(v => v.Value).FirstOrDefault().Key;
        }

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

                // Handle the extra elements in the longer array
                distance += Math.Abs(input1.Length - cellArray.Length);

                if (distance < minDistance)
                {
                    minDistance = distance;
                }
            }

            return minDistance;
        }

        // Method to clear the state of the classifier (clear training data)
        public void ClearState() => trainingData.Clear();
    }
}

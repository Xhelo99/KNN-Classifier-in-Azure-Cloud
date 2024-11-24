# ML22/23-2	Investigate and Implement KNN Classifier

## Feature overview

*   [x] **Microsoft Azure** 
*   [x] **Docker in Azure**
*   [x] **Azure Storage and Blob Storage**
*   [x] **KNN Classifier integrated with HTM and tested with a large number of sequences**
## Contents

*   [What is this?](#what-is-this)
*   [Project Architecture](#project-architecture)
*   [Project Implementation](#project-implementation)
       * [Receive the message from Queue](#receive-the-message-from-queue)
       * [Result Table](#result-table)
       * [Download the dataset from Blob storage](#download-the-dataset-from-blob-storage)
       * [Run the SE experiment](#run-the-se-experiment)
       * [Upload the experiment output to Blob storage](#upload-the-experiment-output-to-blob-storage)
       * [Upload the experiment results to the table](#upload-the-experiment-results-to-the-table)
       * [Delete the message from the Queue](#delete-the-message-from-the-queue)
*  [Azure Deployment](#azure-deployment)
*  [Model Evaluation](#model-evaluation)
*  [Results](#results)
      * [Visual Representation](#visual-representation)
*  [Conclusion](#conclusion)
*  [Sources](#sources)

 ## What is this?
 The SE Project integrates a KNN classifier with the Neocortex API. The SE project used a sequence of values with preassigned labels to train the model.
  Once the model is trained, users can provide an unclassified/test sequence that needs to be labeled.
  For example, in the SE Project, we used two sequences to train the model:

- `("S1", new List<double>(new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0 }))`
- `("S2", new List<double>(new double[] { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0 }))`

We will implement this project in a cloud environment, using thousands of sequences to train and evaluate the model. Some additional conditions need to be fulfilled,
which will be explained further in this README.

## Project Architecture
In this section, we explore the foundational structure of our project, focusing on key components essential for effective design and operation. Our primary emphasis is seamlessly integrating and deploying the K-Nearest Neighbors (KNN) classifier within the Azure cloud ecosystem.

<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/ProjectArchitektur.png" alt="ProjectArchitektur">
  <br>
  <em>Figure 1: <i>Project Architecture of ML22/23-2 Investigate and Implement KNN Classifier - Azure Cloud Implementation</i></em>
</p>


## Project Implementation
This section discusses the classes implemented for running the project in a cloud environment and provides detailed answers to questions about their implementation. **KnnClassifier** is implemented in the current `NeoCortexAPI version 1.1.5`.

First, you have to get the cloud project from [MyCloudProjectSample](https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/tree/main/Source/MyCloudProjectSample). Then we start to modify the project step by step. 
### Receive the message from Queue

We implemented the method in the `AzureStorageProvider` class to receive a message from the queue.

```csharp
public async Task<IExerimentRequest> ReceiveExperimentRequestAsync(CancellationToken token)
{
    QueueClient queueClient = new QueueClient(this._config.StorageConnectionString, this._config.Queue);

    // Receive a message from the queue
    QueueMessage message = await queueClient.ReceiveMessageAsync();

    if (message != null)
    {
        try
        {
            // Processing of the received message
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
```
When the following JSON message is given to the Queue:

```bash
{
   "ExperimentId": "1",
   "Name": "KNN Classifier",
   "Description": "A description of choice",
   "InputFile": "TestSequence.txt"
}
```
This method receives it and processes it. The name of the dataset (InputFile) is passed as a parameter to the next method which downloads the dataset from Blob storage. 
Three dataset files can be downloaded. The test dataset consists of only one sequence `TestSequence.txt`, `sequence500.txt` with 500 sequences, and the other `sequence3000.txt` with 3000 sequences. The test dataset is used to quickly test the functionality of the project requirement because the other dataset requires a lot of processing time.
### Result Table

| Number of Sequence | Duration Sec | InputFileUrl |  Training Accuracy | OutputFileUrl |
|----------|----------|----------|----------|----------|
| 1 sequence | 37s | TestSequence.txt | 88.88888888888889 | outputfile |
| 500 sequence | 19501s | sequence500.txt | 88.88888888888889 | outputfile |
| 3000 sequence | 173108s | sequence3000.txt | 88.88888888888889 | outputfile |

### Result Graph of Training for Three Input Sequences

<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/sequences_duration_accuracy_chart.png" alt="ProjectArchitektur">
  <br>
  <em>Figure 2: <i>Training Accuracy for Different Input Sequences</i></em>
</p>

### Download the dataset from Blob storage
The next step is to implement the method that downloads the dataset from the blob: 

```csharp
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
```
The dataset file is returned and next, we are ready to run the SE experiment.
### Run the SE experiment
To run the SE project we modify the `Experiments`class inside the `MyExperiment` project. We have 
Implemented the method that runs the experiment as follows: 

```csharp
  public Task<IExperimentResult> RunAsync(string inputData)
  {
      
      var outputFile = "output.txt";

      // Read  inputData file
      var text = File.ReadAllText(inputData, Encoding.UTF8);
      var sequences = JsonSerializer.Deserialize<Test>(text);
      

      //  This creates an instance of MultiSequenceLearning and run the method
      MultiSequenceLearning experiment = new MultiSequenceLearning();

      ExperimentResult res = new ExperimentResult(this.config.GroupId, "1");

      res.StartTimeUtc = DateTime.UtcNow;

      // Console output to outpufile
      using (StreamWriter writer = new StreamWriter(outputFile))
      {
          Console.SetOut(writer);
          experiment.Run(sequences.Train);
          writer.Flush();
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
```
This code runs the `MultiSequenceLearning` of the `NeoCortexApi`, which is implemented to use our Knn classifier. The results of the experiment are returned. Some parameters 
are taken from the initial message we send from the Queue message and `Accuracy` is taken from experiment result accuracy. The experiment's output console is saved as `output.txt`
and is given the next method to upload that to the output blob container. 
### Upload the experiment output to Blob storage
Then the method that uploads the experiment output to the Blob storage is implemented: 
```csharp
 public async Task UploadResultAsync(string experimentName, IExperimentResult result)
 {
     string outputFile = result.OutputFiles[0];
     result.OutputFileUrl = experimentName;

     // Initialize the BlobServiceClient and BlobContainerClient
     BlobServiceClient blobServiceClient = new BlobServiceClient(this._config.StorageConnectionString);
     BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(this._config.ResultContainer);   

     await containerClient.CreateIfNotExistsAsync();

     BlobClient blobClient = containerClient.GetBlobClient($"{Path.GetFileName(outputFile)}");
  
     await blobClient.UploadAsync(outputFile, overwrite: true);


 }
```

### Upload the experiment results to the table 
The experiment result which was returned is now uploaded to the table storage, by creating a table entity from the result. 
```csharp
 public async Task UploadExperimentResult(IExperimentResult result)
 {
     try
     {
         // New instance of the TableClient class
         TableServiceClient tableServiceClient = new TableServiceClient(this._config.StorageConnectionString);
         TableClient tableClient = tableServiceClient.GetTableClient(tableName: this._config.ResultTable);
         await tableClient.CreateIfNotExistsAsync();

         // Generate a unique RowKey
         string uniqueRowKey = Guid.NewGuid().ToString();

         // Creating a table entity from the result
         var entity = new TableEntity( this._config.ResultTable, uniqueRowKey)
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

         // Adding the newly created entity to the Azure Table.
         await tableClient.AddEntityAsync(entity);

     }
     catch (Exception ex)
     {
         Console.Error.WriteLine("Failed to upload to Table Storage: ", ex.ToString());
     }
     
 }
```
Now there is only one method that needs to be implemented and that's to delete the message from the Queue. 

### Delete the message from the Queue 
```csharp
        public async Task CommitRequestAsync(IExerimentRequest request)
        {
            
            var queueClient = new QueueClient(_config.StorageConnectionString, this._config.Queue);
            await queueClient.DeleteMessageAsync(request.MessageId, request.MessageReceipt);
        }
```
Here we take the `MessageId` and the `MessageReceipt` from the message that was sent from the Queue. After completing these methods, we only added logging in to the `Program.cs` tested the program for runtime errors, and improved it. 

## Azure Deployment
This section describes the procedure for deploying the project on Azure. A detailed step-by-step guide will be provided to outline every specification clearly.

**1. First are onfigured all necessary components in Azure, including Resource Group, Storage Container, Queue, Table, Container Registry, and Container Instances**. 
All components in Azure are created manually through the Azure portal. Once the configurations are completed, all the details can be viewed under the resource group, as illustrated in the reference image below.


<details>
<summary>Click here to see the configuration details</summary>

```Docker: 
  Resource Group:RG-Team_Kejdjon_Mariglen_CC

  Azure Container Registry: knnclassifier1

  Azure Container Instances: knnci

  Azure Storage: knnclassifier1

  Containers :  1. inputfile
                2. outputfile
 
  Queue: knnmessage

  Table: resultstable

  ConnectionString: "DefaultEndpointsProtocol=https;AccountName=knnclassifier1;AccountKey=EJGD0xEbXXppbOVCTm+9GZaCvbAL59tXcbFOm7tdjmXS2FWGGMDXIyb/WnT0YvLtIKrFj/EwGLUb+AStwKYJWw==;EndpointSuffix=core.windows.net"
 
```
</details>
<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/ResourceGroup.png" alt="ResourceGroup">
  <br>
  <em>Figure 3: <i>Resource Group</i></em>
</p>

**2. After setting up all required configurations in Azure, the Docker image is deployed to the Azure Container Registry.**

<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/ContainerRegistry.png" alt="Azure Container Registry">
  <br>
  <em>Figure 4: <i>Azure Container Registry</i></em>
</p>

**3. Creation of Azure Contanier Instances.**
<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/AzureContanierInstance.PNG" alt="Azure Container Instance">
  <br>
  <em>Figure 5: <i>Azure Container Instance</i></em>
</p>

## Steps to run the project

In this section, we will explain in detail how to run the project in the Azure environment.
<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/Image..png" alt="Execution Instructions">
  <br>
  <em>Figure 6: <i>Execution Diagram</i></em>
</p>

### Navigate to the Azure Container Instances.
After completing all necessary configurations, navigate to the Azure Container Instance and click the 'Start' button to initiate the project.
<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/StartAzureContainerInstance.PNG" alt="Start of Azure Contanier Instance">
  <br>
  <em>Figure 7: <i>Start of Azure Contanier Instance</i></em>
</p>


### Upload InputFile
Next, go to Azure Storage Container and upload the input file. You can start by testing with a single sequence to ensure everything is working correctly. Once confirmed, you can use the larger dataset containing multiple sequences for extended running in Azure. You can use the following JSON template to prepare your input file and include as many test sequences as needed.
```json
{
"Train":
  {"S1": [0, 1, 2, 3, 4, 8, 9, 10, 13]}
}
```
### Send the queue message
When the experiment begins, it will be on hold for a queue message. You can either insert the message directly into the queue storage or utilize Azure Storage Explorer with the provided connection string.

Here is a JSON template that you can use as a message for the queue storage:
```json
{
   "ExperimentId": "Test",
   "Name": "KNN Classifier ",
   "Description": "Test",
   "InputFile": "TestSequence.txt"
}
```
Here’s how you can send the message:
<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/QueueMessage.png" alt="Queue Message">
  <br>
  <em>Figure 8: <i>Queue Message </i></em>
</p>

### Monitoring the process in azure contanier instances
To verify that everything is proceeding correctly, you can monitor the logs in Azure Container Instances during the execution process. Here’s how to do it

<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/AzureLogs.png" alt="Monitoring">
  <br>
  <em>Figure 9: <i>Azure logs</i></em>
</p>

### Output file and Output table
Once the experiment is complete, you can navigate to the Azure Storage Account or use Microsoft Azure Storage Explorer to verify that everything is correct and check that the output file and output table have been created with the respective results. 
<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/OutputFile.png" alt="Outputfile">
  <br>
  <em>Figure 10: <i>Outputfile</i></em>
</p>

Next, navigate to Microsoft Azure Storage Explorer to verify all necessary information after the project execution is complete. 

<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/OutputTable.png" alt="Outputtable">
  <br>
  <em>Figure 11: <i>Outputtable</i></em>
</p>

The largest dataset tested consisted of 3000 sequences, which took approximately 3 days to process on Azure. The specifications of this experiment were then uploaded to table storage. 
### Model Evaluation
In this project, two different models were evaluated using the same dataset **sequence500.txt**. The first model was built using the HTM (Hierarchical Temporal Memory) Classifier, while the second employed the KNN (K-Nearest Neighbors) Classifier. Both models were executed in a cloud environment, where their performance was analyzed and compared based on the output images generated by each classifier.
- To test with the **HTM Classifier**, uncomment the following line in `MultiSequenceLearning.cs`:
     ```csharp
     HtmClassifier<string, ComputeCycle> cls = new HtmClassifier<string, ComputeCycle>();
     ```
   - Then, comment out the line for the **KNN Classifier**:
     ```csharp
     //var cls = new KnnClassifier<string, ComputeCycle>();
     ```
   - This ensures that the HTM classifier is used during the model execution.

We tested our sequence prediction using the KNN classifier and the HTM classifier on three different lists:

```csharp
// Lists used for prediction to evaluate accuracy.
var list1 = new double[] { 1.0, 2.0, 3.0, 4.0, 2.0, 5.0 };
var list2 = new double[] { 2.0, 3.0, 4.0 };
var list3 = new double[] { 8.0, 1.0, 2.0 };
```
The results are explained in the next section. See the outputs for the prediction of each model. 

### Results 
Below is a comparison between the KNN classifier and HTM [outputs](https://drive.google.com/drive/folders/1XxfITBlo8VWIU7oP5hCIM7TajOxmwrsh?usp=sharing).
#### HTM (Hierarchical Temporal Memory)

- **Accuracy:** The HTM model reached a stable accuracy of **88.88%** after 30 cycles, indicating robust learning and reliable sequence prediction.
- **Learning Stability:** The algorithm stabilized after 30 repeats, ensuring consistent performance across multiple sequences.
- **Predictions:** HTM accurately predicted sequences with high confidence, as demonstrated by consistent match percentages across different sequences.

The results of a Hierarchical Temporal Memory (HTM) model applied to the sequence var **list1 = new double[] { 1.0, 2.0, 3.0, 4.0, 2.0, 5.0 };**. The HTM model is designed to find the label of this sequence. Below are the step-by-step results, including the predicted sequences and the likelihood (percentage) of each prediction. For the other 2 sequences see [outputs](https://drive.google.com/drive/folders/1XxfITBlo8VWIU7oP5hCIM7TajOxmwrsh?usp=sharing). 
```csharp
S469_4-6-8-9-10-12-13-1-3 - 33.33
S473_4-8-10-11-12-14-0-1-3 - 33.33
S475_3-4-5-7-10-11-0-1-2 - 33.33
Predicted Sequence: S469, predicted next element 3
S475_4-5-7-10-11-0-1-2-3 - 100
S141_4-5-6-8-11-13-14-0-3 - 25
S239_4-5-6-9-10-14-0-2-3 - 25
S408_4-5-8-9-10-14-0-1-3 - 25
Predicted Sequence: S475, predicted next element 3
S475_5-7-10-11-0-1-2-3-4 - 100
S404_7-8-9-11-13-14-0-2-4 - 20
S407_7-9-10-12-14-0-1-2-4 - 20
S240_5-7-8-10-14-0-1-2-4 - 15
Predicted Sequence: S475, predicted next element 4
S475_7-10-11-0-1-2-3-4-5 - 100
S324_7-9-10-12-13-2-3-4-5 - 20
S326_6-9-12-13-14-2-3-4-5 - 20
S415_6-10-11-12-13-0-1-2-5 - 20
Predicted Sequence: S475, predicted next element 5
S470_4-6-8-9-11-13-14-2-3 - 60.87
S471_4-6-7-8-9-10-14-2-3 - 60.87
S468_5-8-11-13-14-0-1-2-3 - 43.48
Predicted Sequence: S470, predicted next element 3
S475_-1.0-0-1-2-3-4-5-7 - 50
S475_10-11-0-1-2-3-4-5-7 - 50
S266_-1.0-0-1-3-4-5-6-7 - 15
Predicted Sequence: S475, predicted next element 7
```
The HTM model shows how it analyzes and predicts the continuation of a sequence based on learned patterns. Across the six prediction cycles, the model adjusts its predictions based on the patterns it recognizes, with `S475` emerging as a frequent prediction, indicating a strong alignment between this sequence and the input data.

#### KNN Classifier

- **Accuracy:** The KNN classifier exhibited fluctuating accuracy, with several sequences showing negative scores, indicating lower confidence or misclassification.
- **Learning Stability:** Unlike HTM, KNN did not exhibit clear stability, with mixed results across sequences.
- **Predictions:** While KNN was able to predict some sequences accurately, it struggled with others, reflected in the varying and sometimes negative prediction scores.

The results of a KNN model applied to the sequence var **list1 = new double[] { 1.0, 2.0, 3.0, 4.0, 2.0, 5.0 };**:
```csharp
S381_4-6-8-9-12-13-14-2-3 - -56
S382_5-6-8-10-11-14-0-2-3 - -56
S386_8-9-10-12-13-14-1-2-3 - -56
Predicted Sequence: S381, predicted next element 3
S475_4-5-7-10-11-0-1-2-3 - 1
S382_5-6-8-10-11-14-0-2-3 - -14
S414_5-6-7-8-9-12-13-14-3 - -14
Predicted Sequence: S475, predicted next element 3
S475_5-7-10-11-0-1-2-3-4 - 1
S465_5-9-11-12-13-14-1-2-4 - -13
S466_5-8-9-11-12-13-1-2-4 - -13
Predicted Sequence: S475, predicted next element 4
S475_7-10-11-0-1-2-3-4-5 - 1
S239_6-9-10-14-0-2-3-4-5 - -15
S388_7-11-13-0-1-2-3-4-5 - -15
Predicted Sequence: S475, predicted next element 5
S331_5-6-7-9-14-0-1-2-3 - -30
S357_5-6-7-10-11-13-0-2-3 - -33
S380_4-5-7-8-10-13-1-2-3 - -33
Predicted Sequence: S331, predicted next element 3
S322_8-9-10-13-14-2-5-6-7 - -34
S221_9-10-13-0-1-3-4-6-7 - -35
S288_10-12-14-0-2-3-4-5-7 - -35
Predicted Sequence: S322, predicted next element 7
```
The KNN model analyzes the sequence and identifies the closest matching patterns, initially predicting sequence S381 with a next element of 3. It then consistently predicts sequence S475 across several cycles, with a gradual increase in confidence, correctly forecasting subsequent elements 3, 4, and 5. However, in later cycles, the model shifts predictions to sequences S331 and S322, reflecting a change in pattern matching, but it remains focused on accurately predicting the next elements in the sequence.

### Visual Representation
Input to test the model **list1 = new double[] { 1.0, 2.0, 3.0, 4.0, 2.0, 5.0 };**, the model will give the label for this sequence. We expect this label **S475** as output which has the following sequence [ 0, 1, 2, 3, 4, 5, 7, 10, 11 ]. **S475** is the sequence that has all values of **list1**. Below is a visual representation of HTM and KNN model predictions for the given sequence: 

<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/HTM_Sequence_Likelihood_Bar_Chart.png" alt="HTM graph">
  <br>
  <em>Figure 12: <i>HTM sequence prediction for 6 cycles. </i></em>
</p>

In 4 of 6 total cycles, HTM predicted the expected sequence **S475** label.


<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/KNN_Sequence_Likelihood_Bar_Chart.png" alt="KNN graph">
  <br>
  <em>Figure 13: <i>KNN sequence prediction for 6 cycles. </i></em>
</p>

In 3 of 6 total cycles, our KNN model predicted the expected sequence **S475** label.

Then a visual comparison accuracy graph of both models is created based on the prediction made for input sequence **list1 = new double[] { 1.0, 2.0, 3.0, 4.0, 2.0, 5.0 };** : 
<p align="center">
  <img src="https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2023-2024/blob/Team_Kejdjon_Mariglen_CC/MyCloudProject/Documentation/Images/HTM_vs_KNN_Accuracy_Comparison.png" alt="Model Accuracy Graph">
  <br>
  <em>Figure 14: <i>Accuracy comparison between KNN and HTM results</i></em>
</p>

The HTM model demonstrates superior sequence learning and prediction stability compared to the KNN classifier. This conclusion is the same for all predictions made. HTM is recommended for applications requiring high accuracy and consistent performance in sequence prediction.

### Conclusion
In conclusion, the Cloud Project successfully integrates a KNN classifier with the Neocortex API, utilizing Azure's cloud environment to scale and enhance its capabilities. Through the implementation of essential methods for receiving messages from queues, downloading datasets, running experiments, and uploading results to blob and table storage, the project demonstrates a robust and efficient architecture. The seamless deployment of this project on Azure showcases the power of cloud computing in handling complex machine learning tasks, ensuring scalability, reliability, and ease of access. This deployment paves the way for future enhancements and wider applications, reinforcing the potential of integrating advanced machine-learning models with cloud services.

### Sources
1. [Neocortex API](https://github.com/ddobric/neocortexapi)
2. [Microsoft Azure]( azure.microsoft.com​)








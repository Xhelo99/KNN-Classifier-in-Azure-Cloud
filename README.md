# ML22/23-2	Investigate and Implement KNN Classifier

## Feature overview

*   [x] **Microsoft Azure** 
*   [x] **Docker in Azure**
*   [x] **Azure Storage and Blob Storage**
*   [x] **KNN Classifier integrated with HTM and tested with large number of sequences**
## Contents

*   [What is this?](#what-is-this)
*   [Prerequisites - SE Project](#prerequisites-se-project)
*   [Project Architecture](#project-architecture)
*   [Project Implementation](#project-implementation)
     *   [Install](#install)
     *   [Install](#install)
     *   [Install](#install)  
*   [Getting started](#getting-started)
    *   [Install](#install)
*  [Integration of Classifiers with Neocortex API](#integration-of-classifiers-with-neocortex-api)
*  [Usage](#usage)
*  [Conclusion](#conclusion)
*  [Sources](#sources)

*  ## What is this?
 The SE Project integrates a KNN classifier with the Neocortex API. In SE project, a sequence of values with preassigned labels was used to train the model.
  Once the model is trained, users can provide an unclassified/test sequence that needs to be labeled.
  For example, in the SE Project, we used two sequences:

- `("S1", new List<double>(new double[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0 }))`
- `("S2", new List<double>(new double[] { 10.0, 11.0, 12.0, 13.0, 14.0, 15.0, 16.0 }))`

Now, we will implement this project in a cloud environment, using hundreds of sequences to train and evaluate the model. There are some additional conditions that need to be fulfilled,
which will be explained further in this README.
 

* ## Prerequisites - SE Project

1. [Documentation](https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2022-2023/tree/Team_Mariglen_Kejdjon/MySEProject/Documentation)
2. [README](https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2022-2023/blob/Team_Mariglen_Kejdjon/MySEProject/README.md)
3. [Link to SE Project](https://github.com/UniversityOfAppliedSciencesFrankfurt/se-cloud-2022-2023/tree/Team_Mariglen_Kejdjon/MySEProject/MyProjectSample)

*  ## Project Implementation

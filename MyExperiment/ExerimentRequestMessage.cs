
﻿using MyCloudProject.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyExperiment
{
    internal class ExerimentRequestMessage : IExerimentRequest
    {

        // Unique identifier for the experiment.
        public string ExperimentId { get; set; }

        // Name of the input file used in the experiment.
        public string InputFile { get; set; }

        // Name of the experiment.
        public string Name { get; set; }

        // A brief description of the experiment.
        public string Description { get; set; }

        // Unique identifier for the message associated with the experiment request.
        public string MessageId { get; set; }

        // Receipt of the message, used for confirming the message delivery.
        public string MessageReceipt { get; set; }
    }
}


using System;
using System.Collections.Generic;
using System.Text;

namespace MyCloudProject.Common
{
    public interface IExperimentResult
    {
        string ExperimentId { get; set; }
        public string InputFileUrl { get; set; }

        DateTime? StartTimeUtc { get; set; }

        DateTime? EndTimeUtc { get; set; }

        public TimeSpan Duration { get; set; }

        public long DurationSec { get; set; }

        public double Accuracy { get; set; }

        public string[] OutputFiles { get; set; }

    }

}

using System;
using System.Collections.Generic;
using Claudable.ViewModels;
using Newtonsoft.Json;

namespace Claudable.Models
{
    public class SerializableProjectData
    {
        [JsonProperty("projectAssociation")]
        public ProjectAssociation ProjectAssociation { get; set; }

        [JsonProperty("artifacts")]
        public List<ArtifactViewModel> Artifacts { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTime LastUpdated { get; set; }

        public SerializableProjectData()
        {
            Artifacts = new List<ArtifactViewModel>();
            LastUpdated = DateTime.Now;
        }
    }
}
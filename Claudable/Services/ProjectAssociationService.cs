using Claudable.Models;
using Claudable.ViewModels;
using Newtonsoft.Json;
using System.IO;

namespace Claudable.Services
{
    public class ProjectAssociationService
    {
        private readonly string _filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Claudable", "ProjectData.json");
        private Dictionary<string, SerializableProjectData> _projectData;

        public ProjectAssociationService()
        {
            _projectData = LoadProjectData();
        }

        private Dictionary<string, SerializableProjectData> LoadProjectData()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<Dictionary<string, SerializableProjectData>>(json) ?? new Dictionary<string, SerializableProjectData>();
            }
            return new Dictionary<string, SerializableProjectData>();
        }

        private void SaveProjectData()
        {
            string json = JsonConvert.SerializeObject(_projectData, Formatting.Indented);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
            File.WriteAllText(_filePath, json);
        }

        public SerializableProjectData GetProjectDataByUrl(string projectUrl)
        {
            return _projectData.TryGetValue(projectUrl, out var data) ? data : null;
        }

        public void AddOrUpdateProjectData(string projectUrl, string localFolderPath, List<ArtifactViewModel> artifacts)
        {
            if (_projectData.TryGetValue(projectUrl, out var existingData))
            {
                existingData.ProjectAssociation.LocalFolderPath = localFolderPath;
                existingData.Artifacts = artifacts;
                existingData.LastUpdated = DateTime.Now;
            }
            else
            {
                _projectData[projectUrl] = new SerializableProjectData
                {
                    ProjectAssociation = new ProjectAssociation
                    {
                        ProjectUrl = projectUrl,
                        LocalFolderPath = localFolderPath
                    },
                    Artifacts = artifacts
                };
            }
            SaveProjectData();
        }

        public void UpdateArtifacts(string projectUrl, List<ArtifactViewModel> artifacts)
        {
            if (_projectData.TryGetValue(projectUrl, out var existingData))
            {
                existingData.Artifacts = artifacts;
                existingData.LastUpdated = DateTime.Now;
                SaveProjectData();
            }
        }

        public List<string> GetAllProjectUrls()
        {
            return _projectData.Keys.ToList();
        }
    }
}
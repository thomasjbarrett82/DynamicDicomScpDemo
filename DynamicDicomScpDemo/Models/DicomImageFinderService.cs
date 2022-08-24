using FellowOakDicom;

namespace DynamicDicomScpDemo.Models {
    // can have multiple finder services for different sources: SQL database, NoSQL database, etc.
    public interface IDicomImageFinderService {
        Task<List<string>> FindPatientFiles(string patientId);
        Task<List<string>> FindStudyFiles(string studyUID);
        Task<List<string>> FindSeriesFiles(string seriesUID);
        Task<List<string>> FindImageFile(string sopInstanceUid);

        bool IsValidFinder(string finder);
    }

    // slow finder that searches through every file
    // real-world implementation would search a SQL or NoSQL database
    // real-world implementation would also allow filtering on other tag parameters, like modality or date
    public class DicomImageFinderService : IDicomImageFinderService {
        private const string RootFolder = @"C:\DICOM\Source";

        public async Task<List<string>> FindPatientFiles(string patientId) {
            return await FindFiles(DicomTag.PatientID, patientId);
        }

        public async Task<List<string>> FindStudyFiles(string studyUID) {
            return await FindFiles(DicomTag.StudyInstanceUID, studyUID);
        }

        public async Task<List<string>> FindSeriesFiles(string seriesUID) {
            return await FindFiles(DicomTag.SeriesInstanceUID, seriesUID);
        }

        public async Task<List<string>> FindImageFile(string sopInstanceUid) {
            return await FindFiles(DicomTag.SOPInstanceUID, sopInstanceUid);
        }

        public bool IsValidFinder(string finder) {
            return nameof(DicomImageFinderService) == finder;
        }

        #region PrivateMethods 

        private static IEnumerable<string> GetAllFiles() {
            return Directory.GetFiles(RootFolder, "*", SearchOption.AllDirectories);
        }

        private static async Task<List<string>> FindFiles(DicomTag tag, string uid) {
            var allFiles = Directory.GetFiles(RootFolder, "*.dcm", SearchOption.AllDirectories);
            var results = new List<string>();
            foreach (var file in allFiles) {
                var dicomFile = await DicomFile.OpenAsync(file);
                if (dicomFile == null) {
                    continue;
                }
                if (dicomFile.Dataset.TryGetString(tag, out string value) && value == uid) {
                    results.Add(file);
                }
            }
            return results;
        }

        #endregion PrivateMethods
    }
}

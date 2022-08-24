using DynamicDicomScpDemo.Models;
using FellowOakDicom;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using System.Text;

namespace DynamicDicomScpDemo.ServiceClassProviders {
    public abstract class BaseFinderSCP : BaseSCP {
        protected FinderServiceFactory FinderFactory;
        protected IDicomImageFinderService FinderService;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected BaseFinderSCP(INetworkStream stream, Encoding fallbackEncoding, ILogger logger, DicomServiceDependencies dependencies) : base(stream, fallbackEncoding, logger, dependencies) {
            // TODO use DI instead of new() 
            // TODO how to access container from here?
            FinderFactory = new FinderServiceFactory(new List<IDicomImageFinderService> {
                new DicomImageFinderService()
            });
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        #region ProtectedMethods

        protected async Task<IEnumerable<string>> GetMatchingFiles(DicomPriorityRequest request, string level) {
            var matchingFiles = Enumerable.Empty<string>();

            // real world implementation would also include filters for other UID parameters sent, like series + study + modality, etc.
            switch (level) {
                case Constants.Patient:
                    matchingFiles = await FinderService.FindPatientFiles(request.Dataset.GetString(DicomTag.PatientID));
                    break;

                case Constants.Study:
                    matchingFiles = await FinderService.FindStudyFiles(request.Dataset.GetString(DicomTag.StudyInstanceUID));
                    break;

                case Constants.Series:
                    matchingFiles = await FinderService.FindSeriesFiles(request.Dataset.GetString(DicomTag.SeriesInstanceUID));
                    break;

                case Constants.Image:
                    matchingFiles = await FinderService.FindImageFile(request.Dataset.GetString(DicomTag.SOPInstanceUID));
                    break;
            }

            return matchingFiles;
        }

        #endregion ProtectedMethods
    }
}

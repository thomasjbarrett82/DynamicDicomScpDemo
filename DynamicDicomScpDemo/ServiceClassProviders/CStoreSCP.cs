using DynamicDicomScpDemo.Models;
using FellowOakDicom;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using System.Text;
using System.Text.Json;

namespace DynamicDicomScpDemo.ServiceClassProviders {
    public class CStoreSCP : BaseSCP, IDicomServiceProvider, IDicomCStoreProvider {
        private readonly string _storagePath;
        private readonly string _mailboxPath;
        private readonly List<string> _cstoreDicomObjects = new();

        public CStoreSCP(INetworkStream stream, Encoding fallbackEncoding, ILogger logger, DicomServiceDependencies dependencies) : base(stream, fallbackEncoding, logger, dependencies) {
            Port = stream.LocalPort;

            var scpConfig = SCPConfigHelper.SCPConfigFile().Result;
            var config = scpConfig.CStoreConfigurations.FirstOrDefault(c => c.Port == stream.LocalPort);
            if (config == null) {
                throw new InvalidOperationException($"No SCP config for {stream.LocalPort}");
            }

            AE = config.AETitle;
            AllowedAETitles = config.AllowedAETitles;
            _storagePath = config.StoragePath;
            _mailboxPath = config.MailboxPath;
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association) {
            if (association == null) {
                throw new ArgumentNullException(nameof(association));
            }

            Console.WriteLine($"Received {association.CalledAE} CStore association request from {association.CallingAE}");
            if (!CalledAETitleIsValid(association.CalledAE)) {
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }
            if (AllowedAETitles != null && !CallingAETitleIsValid(association.CallingAE)) {
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CallingAENotRecognized);
            }

            CallingAE = association.CallingAE;

            foreach (var pc in association.PresentationContexts) {
                if (pc.AbstractSyntax == DicomUID.Verification) {
                    pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
                }
                else if (pc.AbstractSyntax.StorageCategory != DicomStorageCategory.None) {
                    pc.AcceptTransferSyntaxes(_acceptedImageTransferSyntaxes);
                }
            }

            Console.WriteLine($"Accepted {association.CalledAE} CStore association request from {association.CallingAE}");

            return SendAssociationAcceptAsync(association);
        }

        public Task<DicomCStoreResponse> OnCStoreRequestAsync(DicomCStoreRequest request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var dataset = request.Dataset;
            if (dataset == null) {
                throw new NullReferenceException(nameof(dataset));
            }

            // extract values from dataset
            var patientId = dataset.GetString(DicomTag.PatientID);
            var objectUid = dataset.GetString(DicomTag.SOPInstanceUID);
            var studyUid = dataset.GetString(DicomTag.StudyInstanceUID);
            var seriesUid = dataset.GetString(DicomTag.SeriesInstanceUID);
            var modality = dataset.GetString(DicomTag.Modality);

            var isCt = modality.Equals(Constants.CT, StringComparison.CurrentCultureIgnoreCase);

            // build file path
            string filename = BuildDicomFilePath(patientId, studyUid, seriesUid, objectUid, isCt);

            // CT files don't change, but otherwise overwrite existing files
            if (!File.Exists(filename) || !isCt) {
                request.File.Save(filename);
            }

            _cstoreDicomObjects.Add(filename);

            Console.WriteLine($"Stored file {filename}.");

            return Task.FromResult(new DicomCStoreResponse(request, DicomStatus.Success));
        }

        public Task OnCStoreRequestExceptionAsync(string tempFileName, Exception e) {
            /* let library handle exceptions */
            return Task.CompletedTask;
        }

        public override Task OnReceiveAssociationReleaseRequestAsync() {
            Task.Run(() => ProcessCStoreRequest(_cstoreDicomObjects));
            return SendAssociationReleaseResponseAsync();
        }

        #region PrivateMethods

        private string BuildDicomFilePath(string patientId, string studyUid, string seriesUid, string objectUid, bool isCt) {
            var filePath = Path.Combine(_storagePath, (string.IsNullOrWhiteSpace(patientId)) ? studyUid : patientId);
            if (isCt) {
                if (!string.IsNullOrWhiteSpace(seriesUid)) {
                    filePath = Path.Combine(filePath, seriesUid);
                }
            }
            if (!Directory.Exists(filePath)) {
                Directory.CreateDirectory(filePath);
            }
            filePath = Path.Combine(filePath, $"{objectUid}.dcm");
            return filePath;
        }

        private void ProcessCStoreRequest(List<string> cstoreDicomObjects) {
            if (cstoreDicomObjects == null) {
                throw new ArgumentNullException(nameof(cstoreDicomObjects));
            }
            if (cstoreDicomObjects.Count == 0) {
                return;
            }
            if (!Directory.Exists(_mailboxPath)) {
                Directory.CreateDirectory(_mailboxPath);
            }

            // write DICOM objects to JSON
            var dicomObject = new DicomObject {
                CalledAETitle = AE,
                Port = Port,
                CallingAETitle = CallingAE,
                Items = cstoreDicomObjects
            };
            var dicomJson = JsonSerializer.Serialize(dicomObject);
            var filename = $"{_mailboxPath}{Guid.NewGuid()}.json";
            File.WriteAllText(filename, dicomJson);

            // mark JSON file as readonly to indicate ready for processing
            var attributes = File.GetAttributes(filename);
            attributes |= FileAttributes.ReadOnly;
            File.SetAttributes(filename, attributes);

            Console.WriteLine($"Stored mail {filename}.");
        }

        #endregion PrivateMethods
    }
}

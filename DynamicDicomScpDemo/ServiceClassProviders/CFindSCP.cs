using FellowOakDicom;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using System.Text;

namespace DynamicDicomScpDemo.ServiceClassProviders {
    public class CFindSCP : BaseFinderSCP, IDicomServiceProvider, IDicomCFindProvider {
        public CFindSCP(INetworkStream stream, Encoding fallbackEncoding, ILogger logger, DicomServiceDependencies dependencies) : base(stream, fallbackEncoding, logger, dependencies) {
            var scpConfig = SCPConfigHelper.SCPConfigFile().Result;
            var config = scpConfig.CFindConfigurations.FirstOrDefault(c => c.Port == stream.LocalPort);
            if (config == null) {
                throw new InvalidOperationException($"No SCP config for {stream.LocalPort}");
            }

            AE = config.AETitle;
            Port = stream.LocalPort;
            AllowedAETitles = config.AllowedAETitles;

            FinderService = FinderFactory.Create(config.FinderService);
        }

        public async IAsyncEnumerable<DicomCFindResponse> OnCFindRequestAsync(DicomCFindRequest request) {
            if(request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            var matchingFiles = await GetMatchingFiles(request, request.Level.ToString());

            // now read the required dicomtags from the matching files and return as results
            foreach (var matchingFile in matchingFiles) {
                var dicomFile = DicomFile.Open(matchingFile);
                var result = new DicomDataset();
                foreach (var requestedTag in request.Dataset) {
                    if (dicomFile.Dataset.Contains(requestedTag.Tag)) {
                        dicomFile.Dataset.CopyTo(result, requestedTag.Tag);
                    }
                    else {
                        result.Add(requestedTag);
                    }
                }
                yield return new DicomCFindResponse(request, DicomStatus.Pending) { Dataset = result };
            }

            yield return new DicomCFindResponse(request, DicomStatus.Success);
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association) {
            if (association == null) {
                throw new ArgumentNullException(nameof(association));
            }
            CallingAE = association.CallingAE;
            Console.WriteLine($"Received association request from AE: {CallingAE} with IP: {association.RemoteHost} ");

            if (!CalledAETitleIsValid(association.CalledAE)) {
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }

            foreach (var pc in association.PresentationContexts) {
                if (pc.AbstractSyntax == DicomUID.Verification
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelFind
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelFind) {
                    pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
                }
                else {
                    Console.WriteLine($"Requested abstract syntax {pc.AbstractSyntax} from {CallingAE} not supported");
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            Console.WriteLine($"Accepted {association.CalledAE} C-Find association request from {CallingAE}");
            return SendAssociationAcceptAsync(association);
        }

        #region PrivateMethods

        #endregion PrivateMethods
    }
}

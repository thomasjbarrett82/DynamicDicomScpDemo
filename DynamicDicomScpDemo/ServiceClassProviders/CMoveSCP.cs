using FellowOakDicom;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using FellowOakDicom.Network.Client;
using System.Text;

namespace DynamicDicomScpDemo.ServiceClassProviders {
    public class CMoveSCP : BaseFinderSCP, IDicomServiceProvider, IDicomCMoveProvider {
        private readonly string _destinationAETitle;
        private readonly string _destinationHost;
        private readonly int _destinationPort;
        private readonly bool _destinationUseTls;

        public CMoveSCP(INetworkStream stream, Encoding fallbackEncoding, ILogger logger, DicomServiceDependencies dependencies) : base(stream, fallbackEncoding, logger, dependencies) {
            var scpConfig = SCPConfigHelper.SCPConfigFile().Result;
            var config = scpConfig.CMoveConfigurations.FirstOrDefault(c => c.Port == stream.LocalPort);
            if (config == null) {
                throw new InvalidOperationException($"No SCP config for {stream.LocalPort}");
            }

            AE = config.AETitle;
            Port = stream.LocalPort;
            AllowedAETitles = config.AllowedAETitles;
            
            _destinationAETitle = config.DestinationAETitle;
            _destinationHost = config.DestinationHost;
            _destinationPort = config.DestinationPort;
            _destinationUseTls = config.DestinationUseTls;

            FinderService = FinderFactory.Create(config.FinderService);
        }

        // this would be great to do in background/Hangfire job, but DICOM standard means that the move is part of the association
        public async IAsyncEnumerable<DicomCMoveResponse> OnCMoveRequestAsync(DicomCMoveRequest request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }

            // the c-move request contains the DestinationAE. the data of this AE should be configured somewhere.
            if (request.DestinationAE != _destinationAETitle) {
                yield return new DicomCMoveResponse(request, DicomStatus.QueryRetrieveMoveDestinationUnknown);
                yield return new DicomCMoveResponse(request, DicomStatus.ProcessingFailure);
                yield break;
            }

            var matchingFiles = await GetMatchingFiles(request, request.Level.ToString());

            var client = DicomClientFactory.Create(_destinationHost, _destinationPort, _destinationUseTls, AE, _destinationAETitle);
            client.NegotiateAsyncOps();
            int storeTotal = matchingFiles.Count();
            int storeDone = 0; // this variable stores the number of instances that have already been sent
            int storeFailure = 0; // this variable stores the number of failures returned in an OnResponseReceived
            foreach (string file in matchingFiles) {
                var storeRequest = new DicomCStoreRequest(file);
                storeRequest.OnResponseReceived += (req, resp) => {
                    if (resp.Status == DicomStatus.Success) {
                        storeDone++;
                    }
                    else {
                        storeFailure++;
                    }
                };
                client.AddRequestAsync(storeRequest).Wait();
            }

            var sendTask = client.SendAsync();

            while (!sendTask.IsCompleted) {
                // while the send-task is runnin we inform the QR SCU every 2 seconds about the status and how many instances are remaining to send. 
                yield return new DicomCMoveResponse(request, DicomStatus.Pending) { Remaining = storeTotal - storeDone - storeFailure, Completed = storeDone };
                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            yield return new DicomCMoveResponse(request, DicomStatus.Success);
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association) {
            if (association == null) {
                throw new ArgumentNullException(nameof(association));
            }
            CallingAE = association.CallingAE;
            Console.WriteLine($"Received {association.CalledAE} C-Move association request from AE: {CallingAE} with IP: {association.RemoteHost} "); ;
            if (!CalledAETitleIsValid(association.CalledAE)) {
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CalledAENotRecognized);
            }
            if (AllowedAETitles != null && !CallingAETitleIsValid(CallingAE)) {
                return SendAssociationRejectAsync(DicomRejectResult.Permanent, DicomRejectSource.ServiceUser, DicomRejectReason.CallingAENotRecognized);
            }

            CallingAE = association.CallingAE;

            foreach (var pc in association.PresentationContexts) {
                if (pc.AbstractSyntax == DicomUID.Verification
                    || pc.AbstractSyntax == DicomUID.PatientRootQueryRetrieveInformationModelMove
                    || pc.AbstractSyntax == DicomUID.StudyRootQueryRetrieveInformationModelMove) {
                    pc.AcceptTransferSyntaxes(_acceptedTransferSyntaxes);
                }
                else {
                    Console.WriteLine($"Requested abstract syntax {pc.AbstractSyntax} from {CallingAE} not supported");
                    pc.SetResult(DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
                }
            }

            Console.WriteLine($"Accepted {association.CalledAE} C-Move association request from {CallingAE}");
            return SendAssociationAcceptAsync(association);
        }

        #region PrivateMethods

        #endregion PrivateMethods
    }
}

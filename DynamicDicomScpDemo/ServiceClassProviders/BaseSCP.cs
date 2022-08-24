using FellowOakDicom;
using FellowOakDicom.Log;
using FellowOakDicom.Network;
using System.Text;

namespace DynamicDicomScpDemo.ServiceClassProviders {
    public abstract class BaseSCP : DicomService, IDicomCEchoProvider {
        protected static readonly DicomTransferSyntax[] _acceptedTransferSyntaxes = new DicomTransferSyntax[] {
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian
        };

        protected static readonly DicomTransferSyntax[] _acceptedImageTransferSyntaxes = new DicomTransferSyntax[] {
               // Lossless
               DicomTransferSyntax.JPEGLSLossless,
               DicomTransferSyntax.JPEG2000Lossless,
               DicomTransferSyntax.JPEGProcess14SV1,
               DicomTransferSyntax.JPEGProcess14,
               DicomTransferSyntax.RLELossless,
               // Lossy
               DicomTransferSyntax.JPEGLSNearLossless,
               DicomTransferSyntax.JPEG2000Lossy,
               DicomTransferSyntax.JPEGProcess1,
               DicomTransferSyntax.JPEGProcess2_4,
               // Uncompressed
               DicomTransferSyntax.ExplicitVRLittleEndian,
               DicomTransferSyntax.ExplicitVRBigEndian,
               DicomTransferSyntax.ImplicitVRLittleEndian
        };

        protected string AE;
        protected int Port;
        protected string[]? AllowedAETitles;
        protected string CallingAE = "";

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        protected BaseSCP(INetworkStream stream, Encoding fallbackEncoding, ILogger logger, DicomServiceDependencies dependencies) : base(stream, fallbackEncoding, logger, dependencies) { }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public virtual Task<DicomCEchoResponse> OnCEchoRequestAsync(DicomCEchoRequest request) {
            return Task.FromResult(new DicomCEchoResponse(request, DicomStatus.Success));
        }

        public virtual void OnConnectionClosed(Exception exception) {
            /* nothing to do here */
        }

        public virtual void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason) {
            /* nothing to do here */
        }

        public virtual Task OnReceiveAssociationReleaseRequestAsync() {
            return SendAssociationReleaseResponseAsync();
        }

        protected bool CalledAETitleIsValid(string calledAE) {
            if (string.IsNullOrWhiteSpace(calledAE)) {
                return false;
            }
            return calledAE.Equals(AE, StringComparison.CurrentCultureIgnoreCase);
        }

        protected bool CallingAETitleIsValid(string callingAE) {
            if (string.IsNullOrWhiteSpace(callingAE) || AllowedAETitles == null) {
                return false;
            }
            return AllowedAETitles.Contains(callingAE, StringComparer.CurrentCultureIgnoreCase);
        }
    }
}

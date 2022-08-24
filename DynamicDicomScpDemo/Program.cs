using DynamicDicomScpDemo.Models;
using DynamicDicomScpDemo.ServiceClassProviders;
using FellowOakDicom.Network;
using SimpleInjector;
using System.Reflection;

namespace DynamicDicomScpDemo {
    static class Program {
        public static readonly Container DIContainer;

        private static readonly List<IDicomServer> _cstoreServers = new();
        private static readonly List<IDicomServer> _cmoveServers = new();
        private static readonly List<IDicomServer> _cfindServers = new();

        static Program() {
            DIContainer = new Container(); // TODO how to access container down inside SCP creation?
            DIContainer.Collection.Register<IDicomImageFinderService>(new Assembly[] {
                Assembly.GetExecutingAssembly()
            });
            DIContainer.Verify();
        }

        static void Main() {
            var scpConfig = SCPConfigHelper.SCPConfigFile().Result;
            foreach (var config in scpConfig.CStoreConfigurations) {
                var server = DicomServerFactory.Create<CStoreSCP>(config.Port);
                _cstoreServers.Add(server);
                Console.WriteLine($"Started: {config.AETitle}:{config.Port}");
            }

            foreach (var config in scpConfig.CMoveConfigurations) {
                var server = DicomServerFactory.Create<CMoveSCP>(config.Port);
                _cmoveServers.Add(server);
                Console.WriteLine($"Started: {config.AETitle}:{config.Port}");
            }

            foreach (var config in scpConfig.CFindConfigurations) {
                var server = DicomServerFactory.Create<CFindSCP>(config.Port);
                _cfindServers.Add(server);
                Console.WriteLine($"Started: {config.AETitle}:{config.Port}");
            }

            Console.WriteLine("Press <return> to end...");
            Console.ReadLine();
        }
    }
}


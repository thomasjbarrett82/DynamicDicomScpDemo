using System.Collections.Generic;

namespace DynamicDicomScpDemo.Models {
    public class SCPConfiguration {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public List<CStoreConfiguration> CStoreConfigurations { get; set; }
        public List<CMoveConfiguration> CMoveConfigurations { get; set; }
        public List<CFindConfiguration> CFindConfigurations { get; set; }
    }
}

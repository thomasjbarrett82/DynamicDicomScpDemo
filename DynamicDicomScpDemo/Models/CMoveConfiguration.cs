namespace DynamicDicomScpDemo.Models {
    public class CMoveConfiguration : BaseModuleConfiguration {
        public string DestinationAETitle { get; set; }
        public string DestinationHost { get; set; }
        public int DestinationPort { get; set; }
        public bool DestinationUseTls { get; set; }
        public string FinderService { get; set; }
    }
}

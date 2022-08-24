namespace DynamicDicomScpDemo.Models {
    public class DicomObject {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string CalledAETitle { get; set; }
        public int Port { get; set; }
        public string CallingAETitle { get; set; }

        public List<string> Items { get; set; }
    }
}

namespace DynamicDicomScpDemo.Models {
    public abstract class BaseModuleParameter {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string AETitle { get; set; }
        public int Port { get; set; }
        public string[]? AllowedAETitles { get; set; }
    }
}

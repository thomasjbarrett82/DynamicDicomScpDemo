namespace DynamicDicomScpDemo.Models {
    public abstract class BaseModuleConfiguration {
        public virtual string AETitle { get; set; }
        public virtual int Port { get; set; }
        public virtual string[]? AllowedAETitles { get; set; }
    }
}

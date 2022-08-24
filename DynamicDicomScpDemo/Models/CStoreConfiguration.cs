namespace DynamicDicomScpDemo.Models
{
    public class CStoreConfiguration : BaseModuleConfiguration {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string StoragePath { get; set; }
        public string MailboxPath { get; set; }
    }
}

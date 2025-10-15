using COMWizard.Common.Messaging.Enums;

namespace COMWizard.Common.Messaging
{
  public class StartExtractorRequestMessage : MessageBase
  {
    public ExtractorType ExtractorType { get; set; }
    public string PipeName { get; set; }
  }
}
using COMWizard.Common.Messaging.Enums;

namespace COMWizard.Common.Messaging
{
  public class StartRegistrarRequestMessage : MessageBase
  {
    public override MessageType Type => MessageType.StartRegistrarRequest;
    public ExtractorType ExtractorType { get; set; }
    public string PipeName { get; set; }
  }
}
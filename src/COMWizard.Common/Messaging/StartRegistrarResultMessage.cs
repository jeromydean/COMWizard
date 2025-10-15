using COMWizard.Common.Messaging.Enums;

namespace COMWizard.Common.Messaging
{
  public class StartRegistrarResultMessage : MessageBase
  {
    public override MessageType Type => MessageType.StartRegistrarResult;
    public int? PID { get; set; }
  }
}
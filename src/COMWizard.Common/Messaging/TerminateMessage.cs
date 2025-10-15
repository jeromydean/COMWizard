using COMWizard.Common.Messaging.Enums;

namespace COMWizard.Common.Messaging
{
  public class TerminateMessage : MessageBase
  {
    public override MessageType Type => MessageType.Terminate;
  }
}
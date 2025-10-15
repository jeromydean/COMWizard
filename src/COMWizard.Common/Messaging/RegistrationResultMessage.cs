using COMWizard.Common.Messaging.Enums;

namespace COMWizard.Common.Messaging
{
  public class RegistrationResultMessage : MessageBase
  {
    public override MessageType Type => MessageType.RegistrationResult;
  }
}
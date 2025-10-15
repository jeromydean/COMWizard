using COMWizard.Common.Messaging.Enums;

namespace COMWizard.Common.Messaging
{
  public class RegistrationRequestMessage : MessageBase
  {
    public override MessageType Type => MessageType.RegistrationRequest;
    public string Path { get; set; }
    public string SHA256 { get; set; }
  }
}
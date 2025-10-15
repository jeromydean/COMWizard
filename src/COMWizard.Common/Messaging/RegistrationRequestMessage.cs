using COMWizard.Common.Messaging.Enums;
using COMWizard.Common.PortableExecutable;

namespace COMWizard.Common.Messaging
{
  public class RegistrationRequestMessage : MessageBase
  {
    public override MessageType Type => MessageType.RegistrationRequest;
    public PEMetadata FileInformation { get; set; }
    public string Path { get; set; }
    public string SHA256 { get; set; }
  }
}
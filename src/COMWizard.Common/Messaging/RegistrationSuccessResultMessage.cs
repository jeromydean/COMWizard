using COMWizard.Common.PortableExecutable;

namespace COMWizard.Common.Messaging
{
  public class RegistrationSuccessResultMessage : RegistrationResultMessage
  {
    public PEMetadata FileInformation { get; set; }
    public string OutputPath { get; set; }
    public string Name { get; set; }
  }
}

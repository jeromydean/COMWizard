namespace COMWizard.Common.Messaging
{
  public class RegistrationFailureResultMessage : RegistrationResultMessage
  {
    public string Name { get; set; }
    public string Path { get; set; }
    public string Exception { get; set; }
  }
}
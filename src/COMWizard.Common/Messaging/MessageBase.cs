using COMWizard.Common.Messaging.Enums;

namespace COMWizard.Common.Messaging
{
  public abstract class MessageBase
  {
    public MessageType Type { get; set; }
  }
}
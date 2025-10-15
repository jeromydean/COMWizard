using COMWizard.Common.Messaging.Enums;

namespace COMWizard.Common.Messaging
{
  public abstract class MessageBase
  {
    public abstract MessageType Type { get; }
  }
}
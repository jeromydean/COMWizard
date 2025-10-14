using System;
using System.Threading;

namespace COMWizard.Services
{
  internal interface IProgressDialogHandle : IDialogHandle
  {
    CancellationToken Token { get; }
    IProgress<double> Progress { get; }
    void UpdateMessage(string message);
  }
}
using System;
using System.Threading;

namespace COMWizard.Services
{
  internal interface IDialogHandle : IDisposable
  {
    void Close();
  }
}
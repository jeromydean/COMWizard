using System;
using System.Threading;
using Avalonia.Controls;
using COMWizard.ViewModels.Dialogs;

namespace COMWizard.Services
{
  internal class ProgressDialogHandle : IProgressDialogHandle
  {
    private bool _disposed = false;

    private readonly Window _progressDialog;
    private readonly ProgressDialogViewModel _progressDialogViewModel;

    public IProgress<double> Progress
    {
      get => _progressDialogViewModel.Progress;
    }

    public CancellationToken Token
    {
      get => _progressDialogViewModel.Token;
    }

    public ProgressDialogHandle(Window progressDialog,
      ProgressDialogViewModel progressDialogViewModel)
    {
      _progressDialog = progressDialog;
      _progressDialogViewModel = progressDialogViewModel;
    }

    public void Close()
    {
      _progressDialog?.Close();
      _progressDialogViewModel?.Dispose();
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!_disposed)
      {
        if (disposing)
        {
          if (_progressDialog != null)
          {
            _progressDialog.Close();
          }

          if (_progressDialogViewModel != null)
          {
            _progressDialogViewModel.Dispose();
          }
        }

        _disposed = true;
      }
    }

    public void UpdateMessage(string message)
    {
      _progressDialogViewModel.UpdateMessage(message);
    }
  }
}
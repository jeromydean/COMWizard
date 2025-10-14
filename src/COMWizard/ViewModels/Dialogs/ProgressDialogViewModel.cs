using System;
using System.Threading;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace COMWizard.ViewModels.Dialogs
{
  internal partial class ProgressDialogViewModel : ViewModelBase, IDisposable
  {
    private bool _disposed = false;

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly IProgress<double> _progress;

    public IProgress<double> Progress
    {
      get { return _progress; }
    }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _message;

    [ObservableProperty]
    private bool _isIndeterminate;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool _canCancel;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private double _progressMaximum;

    public CancellationToken Token
    {
      get { return _cancellationTokenSource.Token; }
    }

    //designer ctor
    public ProgressDialogViewModel()
    {
      Title = "title";
      Message = "message";
      _cancellationTokenSource = new CancellationTokenSource();
      _progress = new Progress<double>(ReportProgress);
    }

    public ProgressDialogViewModel(string title, string message, bool isIndeterminate, bool canCancel, double maximum)
    {
      Title = title;
      Message = message;
      IsIndeterminate = isIndeterminate;
      CanCancel = canCancel;
      ProgressMaximum = maximum;

      _cancellationTokenSource = new CancellationTokenSource();
      _progress = new Progress<double>(ReportProgress);
    }

    private bool CanCancelExecute()
    {
      return CanCancel;
    }

    [RelayCommand(CanExecute = nameof(CanCancelExecute))]
    private void Cancel()
    {
      if (CanCancel)
      {
        _cancellationTokenSource.Cancel();
      }
    }

    public void UpdateMessage(string message)
    {
      Message = message;
    }

    private void ReportProgress(double value)
    {
      Dispatcher.UIThread.Post(() =>
      {
        if (IsIndeterminate)
        {
          IsIndeterminate = false;
        }

        if (value < 0.0)
        {
          value = 0.0;
        }

        if (value > ProgressMaximum)
        {
          value = ProgressMaximum;
        }

        ProgressValue = value;
      });
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
          if (_cancellationTokenSource != null)
          {
            _cancellationTokenSource.Dispose();
          }
        }

        _disposed = true;
      }
    }
  }
}
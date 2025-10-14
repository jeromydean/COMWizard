using System.Linq;
using COMWizard.ViewModels.Dialogs;

namespace COMWizard.Services
{
  internal class DialogService : IDialogService
  {
    private readonly IApplicationService _applicationService;

    public DialogService(IApplicationService applicationService)
    {
      _applicationService = applicationService;
    }

    public IProgressDialogHandle ShowProgressDialogAsync(string title, string message, bool isIndeterminate, bool canCancel, double maximum = 100)
    {
      ProgressDialog progressDialog = new ProgressDialog();
      ProgressDialogViewModel progressDialogViewModel = new ProgressDialogViewModel(title, message, isIndeterminate, canCancel, maximum);
      progressDialog.DataContext = progressDialogViewModel;

      //use the currently active window as the parent or the main window if we can't locate an active one
      _ = progressDialog.ShowDialog((_applicationService.DesktopStyleApplicationLifetime.Windows.SingleOrDefault(w => w.IsActive) ?? _applicationService.MainWindow));

      ProgressDialogHandle handle = new ProgressDialogHandle(progressDialog, progressDialogViewModel);
      return handle;
    }
  }
}
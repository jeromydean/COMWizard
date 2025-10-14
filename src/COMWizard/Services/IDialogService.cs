namespace COMWizard.Services
{
  internal interface IDialogService
  {
    IProgressDialogHandle ShowProgressDialogAsync(string title, string message, bool isIndeterminate, bool canCancel, double maximum = 100.0);
  }
}
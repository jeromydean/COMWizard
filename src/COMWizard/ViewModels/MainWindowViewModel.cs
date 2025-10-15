using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using COMWizard.Common.Messaging;
using COMWizard.Engine;
using COMWizard.Services;

namespace COMWizard.ViewModels
{
  internal partial class MainWindowViewModel : ViewModelBase
  {
    private readonly IApplicationService _applicationService;
    private readonly IFilePickerService _filePickerService;
    private readonly IDialogService _dialogService;
    private readonly IRegistrationEngine _registrationEngine;

    public ICommand ExitCommand { get; private set; }
    public ICommand OpenFilesCommand { get; private set; }

    //designer ctor
    public MainWindowViewModel() { }

    public MainWindowViewModel(IApplicationService applicationService,
      IFilePickerService filePickerService,
      IDialogService dialogService,
      IRegistrationEngine registrationEngine)
    {
      _applicationService = applicationService;
      _filePickerService = filePickerService;
      _dialogService = dialogService;
      _registrationEngine = registrationEngine;

      ExitCommand = new RelayCommand(CloseApplication);
      OpenFilesCommand = new AsyncRelayCommand(OpenFiles);
    }

    private async Task OpenFiles()
    {
      IReadOnlyList<IStorageFile> files = await _filePickerService.OpenFilePickerAsync(new FilePickerOpenOptions
      {
        Title = "Select files for registration",
        AllowMultiple = true,
        FileTypeFilter = new[] {
            new FilePickerFileType("Executable and DLL Files")
            {
              Patterns = new[] { "*.exe", "*.dll" }
            }
          }
      });

      if (files.Any())
      {
        using (IProgressDialogHandle progressDialogHandle = _dialogService.ShowProgressDialogAsync("Registering files", "Registration.....", false, true, files.Count))
        {
          try
          {
            int registered = 0;
            await foreach (RegistrationResultMessage registrationResult in _registrationEngine.Register(files.Select(f => f.Path.LocalPath),
              progressDialogHandle.Token))
            {
              registered++;
              progressDialogHandle.Progress.Report(registered);
            }
          }
          finally
          {
            progressDialogHandle.Close();
          }
        }
      }
    }

    private void CloseApplication()
    {
      _applicationService.DesktopStyleApplicationLifetime?.Shutdown();
    }
  }
}

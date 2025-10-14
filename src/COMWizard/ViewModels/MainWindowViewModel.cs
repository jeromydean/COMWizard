using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using COMWizard.Services;

namespace COMWizard.ViewModels
{
  internal partial class MainWindowViewModel : ViewModelBase
  {
    private readonly IApplicationService _applicationService;

    public ICommand ExitCommand { get; private set; }

    public MainWindowViewModel(IApplicationService applicationService)
    {
      _applicationService = applicationService;

      ExitCommand = new RelayCommand(CloseApplication);
    }

    private void CloseApplication()
    {
      _applicationService.DesktopStyleApplicationLifetime?.Shutdown();
    }
  }
}

using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace COMWizard.Services
{
  internal class ApplicationService : IApplicationService
  {
    private readonly IApplicationLifetime _applicationLifetime;
    public Window? MainWindow => DesktopStyleApplicationLifetime?.MainWindow;
    public IClassicDesktopStyleApplicationLifetime? DesktopStyleApplicationLifetime => _applicationLifetime as IClassicDesktopStyleApplicationLifetime;
    public ApplicationService(IApplicationLifetime applicationLifetime)
    {
      _applicationLifetime = applicationLifetime;
    }
  }
}

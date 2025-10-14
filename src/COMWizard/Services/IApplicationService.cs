using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace COMWizard.Services
{
  internal interface IApplicationService
  {
    public Window? MainWindow { get; }
    IClassicDesktopStyleApplicationLifetime? DesktopStyleApplicationLifetime { get; }
  }
}
using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using COMWizard.Engine;
using COMWizard.Services;
using COMWizard.ViewModels;
using COMWizard.Views;
using Microsoft.Extensions.DependencyInjection;

namespace COMWizard
{
  public partial class App : Application
  {
    public override void Initialize()
    {
      AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
      if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
      {
        // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
        // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
        DisableAvaloniaDataAnnotationValidation();

        MainWindow mainWindow = new MainWindow();

        ServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<IStorageProvider>(mainWindow.StorageProvider);
        serviceCollection.AddSingleton<IApplicationService>((sp) =>
        {
          return new ApplicationService(ApplicationLifetime);
        });
        ConfigureServices(serviceCollection);
        IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        mainWindow.DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();
        desktop.MainWindow = mainWindow;
      }

      base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
      //Microsoft DI will auto choose the ctor with the most resolvable parameters so we don't need to specify not to use the designer ctor
      services.AddTransient<MainWindowViewModel>();

      services.AddSingleton<IFilePickerService, FilePickerService>();
      services.AddSingleton<IDialogService, DialogService>();
      services.AddSingleton<IRegistrationEngine, RegistrationEngine>();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
      // Get an array of plugins to remove
      var dataValidationPluginsToRemove =
          BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

      // remove each entry found
      foreach (var plugin in dataValidationPluginsToRemove)
      {
        BindingPlugins.DataValidators.Remove(plugin);
      }
    }
  }
}
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace COMWizard.Services
{
  internal class FilePickerService : IFilePickerService
  {
    private readonly IStorageProvider _storageProvider;

    public FilePickerService(IStorageProvider storageProvider)
    {
      _storageProvider = storageProvider;
    }

    public Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
    {
      return _storageProvider.OpenFilePickerAsync(options);
    }
  }
}

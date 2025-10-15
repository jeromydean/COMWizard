using CommunityToolkit.Mvvm.ComponentModel;

namespace COMWizard.ViewModels
{
  public partial class CheckedListBoxItem<T> : ObservableObject
  {
    private T _value;

    [ObservableProperty]
    private bool _isChecked;

    [ObservableProperty]
    private string _content;

    public T Value
    {
      get => _value;
      set => _value = value;
    }
  }
}
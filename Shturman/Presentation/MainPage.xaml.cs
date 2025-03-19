using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Input;

namespace Shturman.Presentation;

public sealed partial class MainPage : Page
{
    private const string DefaultText = "Я заблудился";
    private const string DisableText = "Отключить";
    private string _string;
    public string CurrentText
    {
        get => _string;
        set
        {
            _string = value;
            OnPropertyChanged();
        }
    }
    public MainPage()
    {
        this.InitializeComponent();
    }
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        if (MainModel.ActivatePath)
        {
            MainModel.ActivatePath = false;
            CurrentText = DefaultText;
            return;
        }
        MainModel.ActivatePath = true;
        CurrentText = DisableText;
    }
}

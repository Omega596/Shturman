using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Input;

namespace Shturman.Presentation;

public sealed partial class MainPage : Page
{
    public string CurrentText
    {
        get => MainModel.CurrentText;
        set
        {
            MainModel.CurrentText = value;
            OnPropertyChanged();
        }
    }
    public MainPage()
    {
        this.InitializeComponent();
    }
    private void Button_PointerPressed(object sender, PointerRoutedEventArgs e)

    {

        MainModel.Button = true;

    }

    private void Button_PointerExited(object sender, PointerRoutedEventArgs e)

    {

// In case the pointer exits while pressed, release

        MainModel.Button = false;

    }
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

using Windows.System;

using WinUIVLC.ViewModels;

namespace WinUIVLC.Views;

public sealed partial class NetworkPage : Page
{
    public NetworkViewModel ViewModel
    {
        get;
    }

    public NetworkPage()
    {
        ViewModel = App.GetService<NetworkViewModel>();
        InitializeComponent();
    }

    private void OnUrlKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel.OpenCommand.CanExecute(null))
        {
            ViewModel.OpenCommand.Execute(null);
        }
    }
}

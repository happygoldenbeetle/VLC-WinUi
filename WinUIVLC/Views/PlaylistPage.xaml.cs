using Microsoft.UI.Xaml.Controls;

using WinUIVLC.ViewModels;

namespace WinUIVLC.Views;

public sealed partial class PlaylistPage : Page
{
    public PlaylistViewModel ViewModel
    {
        get;
    }

    public PlaylistPage()
    {
        ViewModel = App.GetService<PlaylistViewModel>();
        InitializeComponent();
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        ViewModel.Play(e.ClickedItem as PlaylistItem);
    }
}

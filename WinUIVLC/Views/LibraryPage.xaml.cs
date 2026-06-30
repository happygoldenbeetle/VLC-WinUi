using Microsoft.UI.Xaml.Controls;

using WinUIVLC.ViewModels;

namespace WinUIVLC.Views;

public sealed partial class LibraryPage : Page
{
    public LibraryViewModel ViewModel
    {
        get;
    }

    public LibraryPage()
    {
        ViewModel = App.GetService<LibraryViewModel>();
        InitializeComponent();
    }

    private void OnItemClick(object sender, ItemClickEventArgs e)
    {
        ViewModel.Play(e.ClickedItem as LibraryItem);
    }
}

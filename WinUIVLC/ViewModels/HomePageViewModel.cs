using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using Windows.Storage;
using Windows.Storage.Pickers;

using WinUIVLC.Contracts.Services;

namespace WinUIVLC.ViewModels;

public partial class HomePageViewModel : ObservableRecipient
{
    private readonly INavigationService _navigationService;
    private readonly ILogger _log;

    public HomePageViewModel(INavigationService navigationService, ILogger log)
    {
        _navigationService = navigationService;
        _log = log;
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var picker = new FileOpenPicker
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.VideosLibrary,
        };
        foreach (var ext in VideoFileExtensions)
        {
            picker.FileTypeFilter.Add(ext);
        }

        // WinUI 3 pickers must be associated with the app window via its HWND.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file == null)
        {
            _log.Information("Open file dialog cancelled by user.");
            return;
        }

        _log.Information("User selected file '{0}'", file.Path);
        PlayFile(file);
    }

    /// <summary>
    /// Opens the first playable file from a set of dropped/shared storage items.
    /// </summary>
    public void OpenStorageItems(IReadOnlyList<IStorageItem> items)
    {
        if (items.OfType<IStorageFile>().FirstOrDefault() is { } file)
        {
            _log.Information("User dropped file '{0}'", file.Path);
            PlayFile(file);
        }
    }

    private void PlayFile(IStorageItem file)
    {
        // Hand the file to the player the same way file activation does.
        _navigationService.NavigateTo(typeof(VideoPlayerViewModel).FullName!, new List<IStorageItem> { file });
    }

    // libVLC plays far more than mp4/mkv; allow the common containers plus partial-download files.
    public static readonly string[] VideoFileExtensions =
    {
        ".mp4", ".mkv", ".avi", ".mov", ".webm", ".flv", ".ts", ".m4v", ".wmv",
        ".mpg", ".mpeg", ".m2ts", ".3gp", ".ogv",
        ".fdmdownload",  // Free Download Manager
        ".part",         // Firefox / generic
        ".crdownload",   // Chrome / Edge
        ".download",     // Safari / generic
    };
}

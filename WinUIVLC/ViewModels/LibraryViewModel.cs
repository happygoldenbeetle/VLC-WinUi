using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Media.Imaging;

using Serilog;

using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;

using WinUIVLC.Contracts.Services;
using WinUIVLC.Contracts.ViewModels;

namespace WinUIVLC.ViewModels;

public partial class LibraryViewModel : ObservableRecipient, INavigationAware
{
    private static readonly string[] VideoExtensions =
    {
        ".mp4", ".mkv", ".avi", ".mov", ".webm", ".flv", ".ts", ".m4v", ".wmv", ".mpg", ".mpeg", ".m2ts", ".3gp", ".ogv",
    };

    private readonly INavigationService _navigationService;
    private readonly ILogger _log;

    public ObservableCollection<LibraryItem> Items
    {
        get;
    } = new();

    public ObservableCollection<FolderEntry> Folders
    {
        get;
    } = new();

    [ObservableProperty]
    private bool _isLoading;

    // No folders added yet - prompt the user to pick one.
    [ObservableProperty]
    private bool _showFolderPrompt;

    // Folders exist but contain no playable videos.
    [ObservableProperty]
    private bool _showNoVideos;

    public LibraryViewModel(INavigationService navigationService, ILogger log)
    {
        _navigationService = navigationService;
        _log = log;
    }

    public async void OnNavigatedTo(object parameter)
    {
        LoadFolders();

        if (Folders.Count > 0 && Items.Count == 0)
        {
            await LoadAsync();
        }
    }

    public void OnNavigatedFrom()
    {
    }

    public void Play(LibraryItem? item)
    {
        if (item == null)
        {
            return;
        }

        _navigationService.NavigateTo(typeof(VideoPlayerViewModel).FullName!, new List<IStorageItem> { item.File });
    }

    public void RemoveFolder(FolderEntry? entry)
    {
        if (entry == null)
        {
            return;
        }

        if (StorageApplicationPermissions.FutureAccessList.ContainsItem(entry.Token))
        {
            StorageApplicationPermissions.FutureAccessList.Remove(entry.Token);
        }

        LoadFolders();
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task AddFolder()
    {
        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.VideosLibrary,
        };
        picker.FileTypeFilter.Add("*");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        if (folder == null)
        {
            return;
        }

        var fal = StorageApplicationPermissions.FutureAccessList;
        var alreadyAdded = fal.Entries.Any(e => string.Equals(e.Metadata, folder.Path, StringComparison.OrdinalIgnoreCase));
        if (!alreadyAdded)
        {
            fal.Add(folder, folder.Path);
            _log.Information("Added library folder '{0}'", folder.Path);
        }

        LoadFolders();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    private void LoadFolders()
    {
        Folders.Clear();
        foreach (var entry in StorageApplicationPermissions.FutureAccessList.Entries)
        {
            var path = entry.Metadata;
            Folders.Add(new FolderEntry
            {
                Token = entry.Token,
                Path = path,
                Name = System.IO.Path.GetFileName(path.TrimEnd('\\', '/')),
            });
        }

        ShowFolderPrompt = Folders.Count == 0;
        ShowNoVideos = false;
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        Items.Clear();

        try
        {
            var options = new QueryOptions(CommonFileQuery.OrderByName, VideoExtensions)
            {
                FolderDepth = FolderDepth.Deep,
            };

            foreach (var entry in Folders.ToList())
            {
                StorageFolder folder;
                try
                {
                    folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(entry.Token);
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Could not open library folder '{0}'", entry.Path);
                    continue;
                }

                var query = folder.CreateFileQueryWithOptions(options);
                var files = await query.GetFilesAsync();
                foreach (var file in files)
                {
                    var item = new LibraryItem { File = file, Name = file.Name };
                    Items.Add(item);
                    _ = LoadDetailsAsync(item);
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to load the library");
        }
        finally
        {
            ShowFolderPrompt = Folders.Count == 0;
            ShowNoVideos = Folders.Count > 0 && Items.Count == 0;
            IsLoading = false;
        }
    }

    private async Task LoadDetailsAsync(LibraryItem item)
    {
        try
        {
            using var thumbnail = await item.File.GetThumbnailAsync(ThumbnailMode.VideosView, 240);
            if (thumbnail != null && thumbnail.Type == ThumbnailType.Image)
            {
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(thumbnail);
                item.Thumbnail = bitmap;
            }

            var properties = await item.File.Properties.GetVideoPropertiesAsync();
            if (properties.Duration > TimeSpan.Zero)
            {
                var format = properties.Duration.TotalHours >= 1 ? @"h\:mm\:ss" : @"m\:ss";
                item.Duration = properties.Duration.ToString(format);
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to load thumbnail/duration for {0}", item.Name);
        }
    }
}

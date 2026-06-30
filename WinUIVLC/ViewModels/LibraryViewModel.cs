using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml.Media.Imaging;

using Serilog;

using Windows.Storage;
using Windows.Storage.FileProperties;
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

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    public LibraryViewModel(INavigationService navigationService, ILogger log)
    {
        _navigationService = navigationService;
        _log = log;
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (Items.Count == 0)
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

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task AddFolder()
    {
        try
        {
            var library = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Videos);
            var folder = await library.RequestAddFolderAsync();
            if (folder != null)
            {
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to add folder to the Videos library");
        }
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
            var query = KnownFolders.VideosLibrary.CreateFileQueryWithOptions(options);
            var files = await query.GetFilesAsync();

            foreach (var file in files)
            {
                var item = new LibraryItem { File = file, Name = file.Name };
                Items.Add(item);
                _ = LoadDetailsAsync(item);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to enumerate the Videos library");
        }
        finally
        {
            IsEmpty = Items.Count == 0;
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

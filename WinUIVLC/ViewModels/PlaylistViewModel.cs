using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using Windows.Storage;
using Windows.Storage.Pickers;

using WinUIVLC.Contracts.Services;
using WinUIVLC.Contracts.ViewModels;
using WinUIVLC.Helpers;

namespace WinUIVLC.ViewModels;

public partial class PlaylistViewModel : ObservableRecipient, INavigationAware
{
    private readonly IPlaylistService _playlistService;
    private readonly INavigationService _navigationService;
    private readonly ILogger _log;

    private List<PlaylistItem> _all = new();

    [ObservableProperty]
    private IReadOnlyList<PlaylistItem> _channels = new List<PlaylistItem>();

    [ObservableProperty]
    private string _m3uUrl = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _status = string.Empty;

    public PlaylistViewModel(IPlaylistService playlistService, INavigationService navigationService, ILogger log)
    {
        _playlistService = playlistService;
        _navigationService = navigationService;
        _log = log;
    }

    public void OnNavigatedTo(object parameter)
    {
        // Reflect whatever playlist is already loaded in the shared service.
        _all = _playlistService.Items.ToList();
        ApplyFilter();
    }

    public void OnNavigatedFrom()
    {
    }

    public void Play(PlaylistItem? item)
    {
        if (item == null)
        {
            return;
        }

        var index = _all.IndexOf(item);
        _playlistService.SetCurrentIndex(index >= 0 ? index : 0);
        _navigationService.NavigateTo(typeof(VideoPlayerViewModel).FullName!, item.Uri);
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadFromUrl()
    {
        var url = M3uUrl.Trim();
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        IsLoading = true;
        Status = "Loading playlist…";
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "VLC/3.0.18 LibVLC/3.0.18");
            var content = await http.GetStringAsync(url);
            ApplyPlaylistContent(content);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to load M3U from '{0}'", url);
            Status = "Could not load that playlist URL.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadFromFile()
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.Downloads,
        };
        picker.FileTypeFilter.Add(".m3u");
        picker.FileTypeFilter.Add(".m3u8");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file == null)
        {
            return;
        }

        IsLoading = true;
        Status = "Loading playlist…";
        try
        {
            var content = await FileIO.ReadTextAsync(file);
            ApplyPlaylistContent(content);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to load M3U file '{0}'", file.Path);
            Status = "Could not read that playlist file.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyPlaylistContent(string content)
    {
        if (content.TrimStart().StartsWith("<", StringComparison.Ordinal))
        {
            Status = "That URL returned a web page, not a playlist. Use a direct .m3u link (e.g. https://iptv-org.github.io/iptv/index.m3u).";
            return;
        }

        var channels = M3uParser.Parse(content);
        if (channels.Count == 0)
        {
            Status = "No channels found. Make sure the link points to an M3U playlist.";
            return;
        }

        SetChannels(channels);
    }

    private void SetChannels(List<PlaylistItem> channels)
    {
        _all = channels;
        _playlistService.SetItems(channels);
        SearchText = string.Empty;
        ApplyFilter();
        _log.Information("Loaded playlist with {0} channels", channels.Count);
    }

    private void ApplyFilter()
    {
        IEnumerable<PlaylistItem> source = _all;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            source = _all.Where(c =>
                c.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Group.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        Channels = source.ToList();
        Status = _all.Count == 0
            ? string.Empty
            : $"{Channels.Count} of {_all.Count} channels";
    }
}

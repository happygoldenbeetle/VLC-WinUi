using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using WinUIVLC.Contracts.Services;

namespace WinUIVLC.ViewModels;

public partial class NetworkViewModel : ObservableRecipient
{
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "http", "https", "rtsp", "rtmp", "rtmps", "mms", "mmsh", "udp", "rtp", "ftp", "ftps", "smb", "hls",
    };

    private readonly INavigationService _navigationService;
    private readonly ILogger _log;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public NetworkViewModel(INavigationService navigationService, ILogger log)
    {
        _navigationService = navigationService;
        _log = log;
    }

    partial void OnUrlChanged(string value)
    {
        // Only flag an error once the user has typed something that isn't a valid URL.
        HasError = !string.IsNullOrWhiteSpace(value) && !IsValidUrl(value);
    }

    private bool CanOpen() => IsValidUrl(Url);

    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        var url = Url.Trim();
        _log.Information("Opening network stream '{0}'", url);
        _navigationService.NavigateTo(typeof(VideoPlayerViewModel).FullName!, url);
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && AllowedSchemes.Contains(uri.Scheme);
    }
}

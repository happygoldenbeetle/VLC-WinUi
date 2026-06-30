using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Serilog;

using WinUIVLC.Contracts.Services;

namespace WinUIVLC.ViewModels;

public partial class NetworkViewModel : ObservableRecipient
{
    private readonly INavigationService _navigationService;
    private readonly ILogger _log;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCommand))]
    private string _url = string.Empty;

    public NetworkViewModel(INavigationService navigationService, ILogger log)
    {
        _navigationService = navigationService;
        _log = log;
    }

    private bool CanOpen() => !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanOpen))]
    private void Open()
    {
        var url = Url.Trim();
        _log.Information("Opening network stream '{0}'", url);
        _navigationService.NavigateTo(typeof(VideoPlayerViewModel).FullName!, url);
    }
}

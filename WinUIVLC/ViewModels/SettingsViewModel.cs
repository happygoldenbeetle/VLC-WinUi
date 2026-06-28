using System.Reflection;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.UI.Xaml;

using Windows.ApplicationModel;

using WinUIVLC.Contracts.Services;
using WinUIVLC.Helpers;

namespace WinUIVLC.ViewModels;

public partial class SettingsViewModel : ObservableRecipient
{
    private const string AnyLanguage = "Any";

    private readonly IThemeSelectorService _themeSelectorService;
    private readonly IPlayerSettingsService _playerSettings;

    [ObservableProperty]
    private ElementTheme _elementTheme;

    [ObservableProperty]
    private string _versionDescription;

    // Playback
    [ObservableProperty]
    private bool _resumePlayback;
    [ObservableProperty]
    private double _defaultVolume;
    [ObservableProperty]
    private double _defaultPlaybackSpeed;
    [ObservableProperty]
    private double _autoHideDelaySeconds;
    [ObservableProperty]
    private double _seekShortSeconds;
    [ObservableProperty]
    private double _seekNormalSeconds;
    [ObservableProperty]
    private double _seekLongSeconds;

    // Subtitles & audio
    [ObservableProperty]
    private bool _subtitlesEnabledByDefault;
    [ObservableProperty]
    private string _preferredSubtitleLanguage;
    [ObservableProperty]
    private double _subtitleFontScale;
    [ObservableProperty]
    private string _preferredAudioLanguage;

    public List<string> Languages
    {
        get;
    } = new()
    {
        AnyLanguage, "English", "Spanish", "French", "German", "Italian", "Portuguese",
        "Russian", "Japanese", "Korean", "Chinese", "Arabic", "Hindi", "Dutch", "Turkish",
    };

    public ICommand SwitchThemeCommand
    {
        get;
    }

    public SettingsViewModel(IThemeSelectorService themeSelectorService, IPlayerSettingsService playerSettings)
    {
        _themeSelectorService = themeSelectorService;
        _playerSettings = playerSettings;
        _elementTheme = _themeSelectorService.Theme;
        _versionDescription = GetVersionDescription();

        // Seed backing fields directly so the partial change handlers don't re-persist during construction.
        _resumePlayback = _playerSettings.ResumePlayback;
        _defaultVolume = _playerSettings.DefaultVolume;
        _defaultPlaybackSpeed = _playerSettings.DefaultPlaybackSpeed;
        _autoHideDelaySeconds = _playerSettings.AutoHideDelaySeconds;
        _seekShortSeconds = _playerSettings.SeekShortSeconds;
        _seekNormalSeconds = _playerSettings.SeekNormalSeconds;
        _seekLongSeconds = _playerSettings.SeekLongSeconds;
        _subtitlesEnabledByDefault = _playerSettings.SubtitlesEnabledByDefault;
        _preferredSubtitleLanguage = ToDisplayLanguage(_playerSettings.PreferredSubtitleLanguage);
        _subtitleFontScale = _playerSettings.SubtitleFontScale;
        _preferredAudioLanguage = ToDisplayLanguage(_playerSettings.PreferredAudioLanguage);

        SwitchThemeCommand = new RelayCommand<ElementTheme>(
            async (param) =>
            {
                if (ElementTheme != param)
                {
                    ElementTheme = param;
                    await _themeSelectorService.SetThemeAsync(param);
                }
            });
    }

    partial void OnResumePlaybackChanged(bool value) => _ = _playerSettings.SetResumePlaybackAsync(value);

    partial void OnDefaultVolumeChanged(double value) => _ = _playerSettings.SetDefaultVolumeAsync((int)value);

    partial void OnDefaultPlaybackSpeedChanged(double value) => _ = _playerSettings.SetDefaultPlaybackSpeedAsync(value);

    partial void OnAutoHideDelaySecondsChanged(double value) => _ = _playerSettings.SetAutoHideDelaySecondsAsync((int)value);

    partial void OnSeekShortSecondsChanged(double value) => _ = _playerSettings.SetSeekShortSecondsAsync((int)value);

    partial void OnSeekNormalSecondsChanged(double value) => _ = _playerSettings.SetSeekNormalSecondsAsync((int)value);

    partial void OnSeekLongSecondsChanged(double value) => _ = _playerSettings.SetSeekLongSecondsAsync((int)value);

    partial void OnSubtitlesEnabledByDefaultChanged(bool value) => _ = _playerSettings.SetSubtitlesEnabledByDefaultAsync(value);

    partial void OnPreferredSubtitleLanguageChanged(string value) => _ = _playerSettings.SetPreferredSubtitleLanguageAsync(FromDisplayLanguage(value));

    partial void OnSubtitleFontScaleChanged(double value) => _ = _playerSettings.SetSubtitleFontScaleAsync((int)value);

    partial void OnPreferredAudioLanguageChanged(string value) => _ = _playerSettings.SetPreferredAudioLanguageAsync(FromDisplayLanguage(value));

    private static string ToDisplayLanguage(string stored) => string.IsNullOrEmpty(stored) ? AnyLanguage : stored;

    private static string FromDisplayLanguage(string display) => display == AnyLanguage ? string.Empty : display;

    private static string GetVersionDescription()
    {
        Version version;

        if (RuntimeHelper.IsMSIX)
        {
            var packageVersion = Package.Current.Id.Version;

            version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
        }
        else
        {
            version = Assembly.GetExecutingAssembly().GetName().Version!;
        }

        return $"{"AppDisplayName".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}

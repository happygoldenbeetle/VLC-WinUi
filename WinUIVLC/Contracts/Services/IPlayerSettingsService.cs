namespace WinUIVLC.Contracts.Services;

/// <summary>
/// Persisted player preferences. Values are loaded into memory on <see cref="InitializeAsync"/>
/// so the player can read them synchronously when starting playback; setters persist asynchronously.
/// </summary>
public interface IPlayerSettingsService
{
    Task InitializeAsync();

    int DefaultVolume
    {
        get;
    }

    double DefaultPlaybackSpeed
    {
        get;
    }

    int SeekShortSeconds
    {
        get;
    }

    int SeekNormalSeconds
    {
        get;
    }

    int SeekLongSeconds
    {
        get;
    }

    bool ResumePlayback
    {
        get;
    }

    bool SubtitlesEnabledByDefault
    {
        get;
    }

    string PreferredSubtitleLanguage
    {
        get;
    }

    int SubtitleFontScale
    {
        get;
    }

    string PreferredAudioLanguage
    {
        get;
    }

    int AutoHideDelaySeconds
    {
        get;
    }

    Task SetDefaultVolumeAsync(int value);

    Task SetDefaultPlaybackSpeedAsync(double value);

    Task SetSeekShortSecondsAsync(int value);

    Task SetSeekNormalSecondsAsync(int value);

    Task SetSeekLongSecondsAsync(int value);

    Task SetResumePlaybackAsync(bool value);

    Task SetSubtitlesEnabledByDefaultAsync(bool value);

    Task SetPreferredSubtitleLanguageAsync(string value);

    Task SetSubtitleFontScaleAsync(int value);

    Task SetPreferredAudioLanguageAsync(string value);

    Task SetAutoHideDelaySecondsAsync(int value);

    /// <summary>Returns the saved resume position in milliseconds for a file, or 0 if none.</summary>
    long GetResumePosition(string filePath);

    Task SaveResumePositionAsync(string filePath, long positionMs);

    Task ClearResumePositionAsync(string filePath);
}

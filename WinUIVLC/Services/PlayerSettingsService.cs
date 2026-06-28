using WinUIVLC.Contracts.Services;

namespace WinUIVLC.Services;

public class PlayerSettingsService : IPlayerSettingsService
{
    private const string VolumeKey = "Player_DefaultVolume";
    private const string SpeedKey = "Player_DefaultPlaybackSpeed";
    private const string SeekShortKey = "Player_SeekShortSeconds";
    private const string SeekNormalKey = "Player_SeekNormalSeconds";
    private const string SeekLongKey = "Player_SeekLongSeconds";
    private const string ResumeKey = "Player_ResumePlayback";
    private const string SubtitlesDefaultKey = "Player_SubtitlesEnabledByDefault";
    private const string SubtitleLanguageKey = "Player_PreferredSubtitleLanguage";
    private const string SubtitleScaleKey = "Player_SubtitleFontScale";
    private const string AudioLanguageKey = "Player_PreferredAudioLanguage";
    private const string AutoHideKey = "Player_AutoHideDelaySeconds";
    private const string ResumePositionsKey = "Player_ResumePositions";

    private readonly ILocalSettingsService _localSettingsService;
    private Dictionary<string, long> _resumePositions = new();

    public PlayerSettingsService(ILocalSettingsService localSettingsService)
    {
        _localSettingsService = localSettingsService;
    }

    public int DefaultVolume
    {
        get; private set;
    } = 100;

    public double DefaultPlaybackSpeed
    {
        get; private set;
    } = 1.0;

    public int SeekShortSeconds
    {
        get; private set;
    } = 3;

    public int SeekNormalSeconds
    {
        get; private set;
    } = 10;

    public int SeekLongSeconds
    {
        get; private set;
    } = 60;

    public bool ResumePlayback
    {
        get; private set;
    } = true;

    public bool SubtitlesEnabledByDefault
    {
        get; private set;
    }

    public string PreferredSubtitleLanguage
    {
        get; private set;
    } = string.Empty;

    public int SubtitleFontScale
    {
        get; private set;
    } = 100;

    public string PreferredAudioLanguage
    {
        get; private set;
    } = string.Empty;

    public int AutoHideDelaySeconds
    {
        get; private set;
    } = 3;

    public async Task InitializeAsync()
    {
        DefaultVolume = await _localSettingsService.ReadSettingAsync<int?>(VolumeKey) ?? DefaultVolume;
        DefaultPlaybackSpeed = await _localSettingsService.ReadSettingAsync<double?>(SpeedKey) ?? DefaultPlaybackSpeed;
        SeekShortSeconds = await _localSettingsService.ReadSettingAsync<int?>(SeekShortKey) ?? SeekShortSeconds;
        SeekNormalSeconds = await _localSettingsService.ReadSettingAsync<int?>(SeekNormalKey) ?? SeekNormalSeconds;
        SeekLongSeconds = await _localSettingsService.ReadSettingAsync<int?>(SeekLongKey) ?? SeekLongSeconds;
        ResumePlayback = await _localSettingsService.ReadSettingAsync<bool?>(ResumeKey) ?? ResumePlayback;
        SubtitlesEnabledByDefault = await _localSettingsService.ReadSettingAsync<bool?>(SubtitlesDefaultKey) ?? SubtitlesEnabledByDefault;
        PreferredSubtitleLanguage = await _localSettingsService.ReadSettingAsync<string>(SubtitleLanguageKey) ?? PreferredSubtitleLanguage;
        SubtitleFontScale = await _localSettingsService.ReadSettingAsync<int?>(SubtitleScaleKey) ?? SubtitleFontScale;
        PreferredAudioLanguage = await _localSettingsService.ReadSettingAsync<string>(AudioLanguageKey) ?? PreferredAudioLanguage;
        AutoHideDelaySeconds = await _localSettingsService.ReadSettingAsync<int?>(AutoHideKey) ?? AutoHideDelaySeconds;
        _resumePositions = await _localSettingsService.ReadSettingAsync<Dictionary<string, long>>(ResumePositionsKey) ?? new();
    }

    public async Task SetDefaultVolumeAsync(int value)
    {
        DefaultVolume = value;
        await _localSettingsService.SaveSettingAsync(VolumeKey, value);
    }

    public async Task SetDefaultPlaybackSpeedAsync(double value)
    {
        DefaultPlaybackSpeed = value;
        await _localSettingsService.SaveSettingAsync(SpeedKey, value);
    }

    public async Task SetSeekShortSecondsAsync(int value)
    {
        SeekShortSeconds = value;
        await _localSettingsService.SaveSettingAsync(SeekShortKey, value);
    }

    public async Task SetSeekNormalSecondsAsync(int value)
    {
        SeekNormalSeconds = value;
        await _localSettingsService.SaveSettingAsync(SeekNormalKey, value);
    }

    public async Task SetSeekLongSecondsAsync(int value)
    {
        SeekLongSeconds = value;
        await _localSettingsService.SaveSettingAsync(SeekLongKey, value);
    }

    public async Task SetResumePlaybackAsync(bool value)
    {
        ResumePlayback = value;
        await _localSettingsService.SaveSettingAsync(ResumeKey, value);
    }

    public async Task SetSubtitlesEnabledByDefaultAsync(bool value)
    {
        SubtitlesEnabledByDefault = value;
        await _localSettingsService.SaveSettingAsync(SubtitlesDefaultKey, value);
    }

    public async Task SetPreferredSubtitleLanguageAsync(string value)
    {
        PreferredSubtitleLanguage = value;
        await _localSettingsService.SaveSettingAsync(SubtitleLanguageKey, value);
    }

    public async Task SetSubtitleFontScaleAsync(int value)
    {
        SubtitleFontScale = value;
        await _localSettingsService.SaveSettingAsync(SubtitleScaleKey, value);
    }

    public async Task SetPreferredAudioLanguageAsync(string value)
    {
        PreferredAudioLanguage = value;
        await _localSettingsService.SaveSettingAsync(AudioLanguageKey, value);
    }

    public async Task SetAutoHideDelaySecondsAsync(int value)
    {
        AutoHideDelaySeconds = value;
        await _localSettingsService.SaveSettingAsync(AutoHideKey, value);
    }

    public long GetResumePosition(string filePath)
    {
        return _resumePositions.TryGetValue(filePath, out var position) ? position : 0;
    }

    public async Task SaveResumePositionAsync(string filePath, long positionMs)
    {
        _resumePositions[filePath] = positionMs;
        await _localSettingsService.SaveSettingAsync(ResumePositionsKey, _resumePositions);
    }

    public async Task ClearResumePositionAsync(string filePath)
    {
        if (_resumePositions.Remove(filePath))
        {
            await _localSettingsService.SaveSettingAsync(ResumePositionsKey, _resumePositions);
        }
    }
}

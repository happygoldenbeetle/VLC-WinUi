namespace WinUIVLC.ViewModels.Wrappers;

/// <summary>
/// Per-playback preferences applied when a media player wrapper is created.
/// </summary>
public class PlayerStartupOptions
{
    public int Volume
    {
        get; init;
    } = 100;

    public double Speed
    {
        get; init;
    } = 1.0;

    public int SeekShortMs
    {
        get; init;
    } = 3000;

    public int SeekNormalMs
    {
        get; init;
    } = 10000;

    public int SeekLongMs
    {
        get; init;
    } = 60000;

    public bool EnableSubtitles
    {
        get; init;
    }

    public string PreferredSubtitleLanguage
    {
        get; init;
    } = string.Empty;

    public string PreferredAudioLanguage
    {
        get; init;
    } = string.Empty;
}

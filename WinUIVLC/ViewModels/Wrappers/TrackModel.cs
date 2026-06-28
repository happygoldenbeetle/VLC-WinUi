namespace WinUIVLC.ViewModels.Wrappers;

/// <summary>
/// A selectable audio or subtitle track exposed by the media player.
/// Id maps to the libVLC track id (-1 means "disabled" for subtitles).
/// </summary>
public class TrackModel
{
    public int Id
    {
        get; init;
    }

    public string Name
    {
        get; init;
    } = string.Empty;
}

namespace WinUIVLC.ViewModels;

/// <summary>
/// One entry in a playlist (an IPTV channel or a media item): a title and the URL/path to play.
/// </summary>
public class PlaylistItem
{
    public string Title
    {
        get; init;
    } = string.Empty;

    public string Uri
    {
        get; init;
    } = string.Empty;

    public string Group
    {
        get; init;
    } = string.Empty;
}

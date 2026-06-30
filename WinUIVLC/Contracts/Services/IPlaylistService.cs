using WinUIVLC.ViewModels;

namespace WinUIVLC.Contracts.Services;

/// <summary>
/// Holds the currently loaded playlist (e.g. IPTV channels) and the playback position within it,
/// so the player can move Next/Previous and pages can share the same list.
/// </summary>
public interface IPlaylistService
{
    IReadOnlyList<PlaylistItem> Items
    {
        get;
    }

    int CurrentIndex
    {
        get;
    }

    PlaylistItem? Current
    {
        get;
    }

    bool HasItems
    {
        get;
    }

    bool HasNext
    {
        get;
    }

    bool HasPrevious
    {
        get;
    }

    event EventHandler? Changed;

    void SetItems(IReadOnlyList<PlaylistItem> items);

    void SetCurrentIndex(int index);

    PlaylistItem? MoveNext();

    PlaylistItem? MovePrevious();

    void Clear();
}

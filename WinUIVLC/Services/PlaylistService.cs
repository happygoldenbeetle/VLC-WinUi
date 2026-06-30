using WinUIVLC.Contracts.Services;
using WinUIVLC.ViewModels;

namespace WinUIVLC.Services;

public class PlaylistService : IPlaylistService
{
    private readonly List<PlaylistItem> _items = new();

    public IReadOnlyList<PlaylistItem> Items => _items;

    public int CurrentIndex
    {
        get; private set;
    } = -1;

    public PlaylistItem? Current => CurrentIndex >= 0 && CurrentIndex < _items.Count ? _items[CurrentIndex] : null;

    public bool HasItems => _items.Count > 0;

    public bool HasNext => CurrentIndex >= 0 && CurrentIndex < _items.Count - 1;

    public bool HasPrevious => CurrentIndex > 0;

    public event EventHandler? Changed;

    public void SetItems(IReadOnlyList<PlaylistItem> items)
    {
        _items.Clear();
        _items.AddRange(items);
        CurrentIndex = _items.Count > 0 ? 0 : -1;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetCurrentIndex(int index)
    {
        if (index >= 0 && index < _items.Count)
        {
            CurrentIndex = index;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public PlaylistItem? MoveNext()
    {
        if (!HasNext)
        {
            return null;
        }

        CurrentIndex++;
        Changed?.Invoke(this, EventArgs.Empty);
        return Current;
    }

    public PlaylistItem? MovePrevious()
    {
        if (!HasPrevious)
        {
            return null;
        }

        CurrentIndex--;
        Changed?.Invoke(this, EventArgs.Empty);
        return Current;
    }

    public void Clear()
    {
        _items.Clear();
        CurrentIndex = -1;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

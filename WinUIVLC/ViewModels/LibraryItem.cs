using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.UI.Xaml.Media;

using Windows.Storage;

namespace WinUIVLC.ViewModels;

/// <summary>
/// A single video file shown in the library, with a lazily-loaded thumbnail and duration.
/// </summary>
public partial class LibraryItem : ObservableObject
{
    public StorageFile File
    {
        get; init;
    } = null!;

    public string Name
    {
        get; init;
    } = string.Empty;

    // Path of the allow-listed folder this video came from (used for filtering).
    public string FolderPath
    {
        get; init;
    } = string.Empty;

    [ObservableProperty]
    private ImageSource? _thumbnail;

    [ObservableProperty]
    private string _duration = string.Empty;
}

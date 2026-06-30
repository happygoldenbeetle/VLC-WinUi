using CommunityToolkit.Mvvm.ComponentModel;

namespace WinUIVLC.ViewModels;

/// <summary>
/// A folder the user has added to the library allow-list (backed by the FutureAccessList).
/// <see cref="IsSelected"/> controls whether its videos are shown in the grid.
/// </summary>
public partial class FolderEntry : ObservableObject
{
    public string Token
    {
        get; init;
    } = string.Empty;

    public string Path
    {
        get; init;
    } = string.Empty;

    public string Name
    {
        get; init;
    } = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}

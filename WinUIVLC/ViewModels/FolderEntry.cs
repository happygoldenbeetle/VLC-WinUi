namespace WinUIVLC.ViewModels;

/// <summary>
/// A folder the user has added to the library allow-list (backed by the FutureAccessList).
/// </summary>
public class FolderEntry
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
}

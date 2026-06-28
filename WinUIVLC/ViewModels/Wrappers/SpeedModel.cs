namespace WinUIVLC.ViewModels.Wrappers;

/// <summary>
/// A selectable playback-speed preset. Value is the libVLC rate multiplier.
/// </summary>
public class SpeedModel
{
    public double Value
    {
        get; init;
    }

    public string Label
    {
        get; init;
    } = string.Empty;
}

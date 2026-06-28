using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibVLCSharp.Platforms.Windows;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Serilog;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinUIVLC.Contracts.Services;
using WinUIVLC.Contracts.ViewModels;
using WinUIVLC.Models.Enums;
using WinUIVLC.ViewModels.Wrappers;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace WinUIVLC.ViewModels;

public partial class VideoPlayerViewModel : ObservableRecipient, INavigationAware
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly INavigationService _navigationService;
    private readonly IWindowPresenterService _windowPresenterService;
    private readonly IPlayerSettingsService _playerSettings;
    private readonly ILogger _log;

    private LibVLC libVLC;
    private MediaPlayer mediaPlayer;
    private string filePath = "Empty";
    private ObservableMediaPlayerWrapper mediaPlayerWrapper;
    private Visibility controlsVisibility;
    private bool isPointerOverControls;

    private readonly DispatcherTimer controlsHideTimer = new()
    {
        Interval = TimeSpan.FromSeconds(3),
    };

    public VideoPlayerViewModel(INavigationService navigationService, IWindowPresenterService windowPresenterService, IPlayerSettingsService playerSettings, ILogger log)
    {
        _navigationService = navigationService;
        _windowPresenterService = windowPresenterService;
        _playerSettings = playerSettings;
        _log = log;

        _windowPresenterService.WindowPresenterChanged += OnWindowPresenterChanged;
        controlsHideTimer.Tick += Timer_Tick;
        controlsHideTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, _playerSettings.AutoHideDelaySeconds));

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
    }

    ~VideoPlayerViewModel()
    {
        Dispose();
    }

    private LibVLC LibVLC
    {
        get => libVLC;
        set => libVLC = value;
    }

    public MediaPlayer Player
    {
        get => mediaPlayer;
        set => SetProperty(ref mediaPlayer, value);
    }

    public string FilePath
    {
        get => filePath;
        set => SetProperty(ref filePath, value);
    }

    public ObservableMediaPlayerWrapper MediaPlayerWrapper
    {
        get => mediaPlayerWrapper;
        set => SetProperty(ref mediaPlayerWrapper, value);
    }

    public bool IsNotFullScreen => !_windowPresenterService.IsFullScreen;

    public Visibility ControlsVisibility
    {
        get => controlsVisibility;
        set => SetProperty(ref controlsVisibility, value);
    }

    // Always span both rows so the control bar overlays the video; hiding it then never reflows the picture.
    public int RowSpan => 2;

    public bool LoadPlayer => FilePath != "Empty";

    //[RelayCommand]
    //private void VideoViewKeyDown(KeyRoutedEventArgs args)
    //{
    //    //switch (args.Key)
    //    //{
    //    //    case VirtualKey.Space:
    //    //        PlayPause();
    //    //        break;
    //    //    case VirtualKey.Escape:
    //    //        FullScreen();
    //    //        break;
    //    //    case VirtualKey.Up:
    //    //        VolumeUp();
    //    //        break;
    //    //    case VirtualKey.Down:
    //    //        VolumeDown();
    //    //        break;
    //    //    //case VirtualKey.Left:
    //    //    //    Rewind(new KeyboardAcceleratorInvokedEventArgs());
    //    //    //    break;
    //    //    //case VirtualKey.Right:
    //    //    //    FastForward(new KeyboardAcceleratorInvokedEventArgs());
    //    //    //    break;
    //    //    case VirtualKey.M:
    //    //        Mute();
    //    //        break;
    //    //    case VirtualKey.S:
    //    //        Stop();
    //    //        break;
    //    //    case VirtualKey.F:
    //    //        //FullScreen();
    //    //        break;
    //    //    case VirtualKey.P:
    //    //        PlayPause();
    //    //        break;
    //    //}
    //}

    [RelayCommand]
    private void Initialized(InitializedEventArgs eventArgs)
    {
        if (FilePath == "Empty")
        {
            _log.Information("Skipping LibVLC initialization, because no media file specified.");
            return;
        }

        _log.Information("Initializing LibVLC");

        // Subtitle font scale must be a libVLC creation option (no runtime API in this version).
        var libVlcOptions = eventArgs.SwapChainOptions.ToList();
        if (_playerSettings.SubtitleFontScale != 100)
        {
            libVlcOptions.Add($"--sub-text-scale={_playerSettings.SubtitleFontScale}");
        }

        LibVLC = new LibVLC(true, libVlcOptions.ToArray());
        Player = new MediaPlayer(LibVLC);

        var media = new Media(LibVLC, new Uri(FilePath));

        // Resume from the saved position if enabled and the offset is meaningful.
        if (_playerSettings.ResumePlayback)
        {
            var resumeMs = _playerSettings.GetResumePosition(FilePath);
            if (resumeMs > 5000)
            {
                media.AddOption($":start-time={resumeMs / 1000.0}");
                _log.Information("Resuming '{0}' at {1} ms", FilePath, resumeMs);
            }
        }

        Player.Play(media);
        _log.Information("Starting playback of '{0}'", FilePath);

        Player.EndReached += OnEndReached;

        var startupOptions = new PlayerStartupOptions
        {
            Volume = _playerSettings.DefaultVolume,
            Speed = _playerSettings.DefaultPlaybackSpeed,
            SeekShortMs = _playerSettings.SeekShortSeconds * 1000,
            SeekNormalMs = _playerSettings.SeekNormalSeconds * 1000,
            SeekLongMs = _playerSettings.SeekLongSeconds * 1000,
            EnableSubtitles = _playerSettings.SubtitlesEnabledByDefault,
            PreferredSubtitleLanguage = _playerSettings.PreferredSubtitleLanguage,
            PreferredAudioLanguage = _playerSettings.PreferredAudioLanguage,
        };

        MediaPlayerWrapper = new ObservableMediaPlayerWrapper(Player, _dispatcherQueue, startupOptions);
        RestartHideTimer();
    }

    private void OnEndReached(object? sender, EventArgs e)
    {
        // Playback finished - drop the saved resume point so it restarts from the beginning next time.
        _ = _playerSettings.ClearResumePositionAsync(FilePath);
    }

    private void SaveResumeState()
    {
        var player = Player;
        if (player == null || FilePath == "Empty")
        {
            return;
        }

        var time = player.Time;
        var length = player.Length;

        // Remember the position only when we are past the intro and not effectively at the end.
        if (time > 5000 && (length <= 0 || time < length - 5000))
        {
            _ = _playerSettings.SaveResumePositionAsync(FilePath, time);
        }
        else
        {
            _ = _playerSettings.ClearResumePositionAsync(FilePath);
        }

        // Persist the last volume so the next file opens at the same level.
        _ = _playerSettings.SetDefaultVolumeAsync(player.Volume);
    }

    [RelayCommand]
    private void PointerMoved(PointerRoutedEventArgs? args)
    {
        // Any movement over the video reveals the controls and resets the idle countdown.
        ShowControls();
        RestartHideTimer();
    }

    [RelayCommand]
    private void ControlsPointerEntered()
    {
        // While the pointer is on the control bar, keep it shown and pause the countdown.
        isPointerOverControls = true;
        controlsHideTimer.Stop();
        ShowControls();
    }

    [RelayCommand]
    private void ControlsPointerExited()
    {
        isPointerOverControls = false;
        RestartHideTimer();
    }

    private void RestartHideTimer()
    {
        controlsHideTimer.Stop();
        if (!isPointerOverControls)
        {
            controlsHideTimer.Start();
        }
    }

    private void Timer_Tick(object? sender, object e)
    {
        controlsHideTimer.Stop();

        // Keep the controls up while the pointer is on them or while playback is paused.
        if (!isPointerOverControls && (MediaPlayerWrapper?.IsPlaying ?? false))
        {
            HideControls();
        }
    }

    private void OnWindowPresenterChanged(object? sender, EventArgs e)
    {
        if (sender is not IWindowPresenterService windowPresenter)
        {
            return;
        }

        // Reveal the controls when toggling fullscreen, then resume the idle countdown.
        ShowControls();
        RestartHideTimer();

        OnPropertyChanged(nameof(IsNotFullScreen));
        OnPropertyChanged(nameof(ControlsVisibility));
        OnPropertyChanged(nameof(RowSpan));
    }

    private void ShowControls()
    {
        if (ControlsVisibility == Visibility.Visible)
        {
            return;
        }

        ControlsVisibility = Visibility.Visible;
        _log.Information("Showing controls");
    }

    private void HideControls()
    {
        if (ControlsVisibility == Visibility.Collapsed)
        {
            return;
        }

        ControlsVisibility = Visibility.Collapsed;
        _log.Information("Hiding controls");
    }

    [RelayCommand]
    private void PlayPause()
    {
        MediaPlayerWrapper?.PlayPause();
    }

    [RelayCommand]
    private void Stop()
    {
        MediaPlayerWrapper?.Stop();
    }

    [RelayCommand]
    private void Mute()
    {
        MediaPlayerWrapper?.Mute();
    }

    [RelayCommand]
    private async Task AddSubtitleFile()
    {
        if (MediaPlayerWrapper == null)
        {
            return;
        }

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.VideosLibrary,
        };
        foreach (var ext in new[] { ".srt", ".ass", ".ssa", ".sub", ".vtt", ".idx" })
        {
            picker.FileTypeFilter.Add(ext);
        }

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file == null)
        {
            return;
        }

        MediaPlayerWrapper.AddSubtitle(new Uri(file.Path).AbsoluteUri);
    }

    [RelayCommand]
    private void FullScreen()
    {
        _windowPresenterService.ToggleFullScreen();
    }

    [RelayCommand]
    private void ExitFullScreen()
    {
        // Escape only leaves fullscreen; it must never enter it.
        if (_windowPresenterService.IsFullScreen)
        {
            _windowPresenterService.ToggleFullScreen();
        }
    }

    [RelayCommand]
    private void VolumeDown()
    {
        MediaPlayerWrapper?.VolumeDown();
    }

    [RelayCommand]
    private void VolumeUp()
    {
        MediaPlayerWrapper?.VolumeUp();
    }

    [RelayCommand]
    private void ScrollChanged(PointerRoutedEventArgs args)
    {
        var delta = args.GetCurrentPoint(null).Properties.MouseWheelDelta;
        if (delta > 0)
        {
            MediaPlayerWrapper?.VolumeUp();
        }
        else
        {
            MediaPlayerWrapper?.VolumeDown();
        }
        args.Handled = true;
    }

    [RelayCommand]
    private void FastForward(object args)
    {
        if (args is KeyboardAcceleratorInvokedEventArgs keyboardAcceleratorInvokedEventArgs)
        {
            var modifier = keyboardAcceleratorInvokedEventArgs.KeyboardAccelerator.Modifiers;
            switch (modifier)
            {
                case VirtualKeyModifiers.None:
                case VirtualKeyModifiers.Menu://10s
                    MediaPlayerWrapper?.FastForward(RewindMode.Normal);
                    break;
                case VirtualKeyModifiers.Control://60s
                    MediaPlayerWrapper?.FastForward(RewindMode.Long);
                    break;
                case VirtualKeyModifiers.Shift://3s
                    MediaPlayerWrapper?.FastForward(RewindMode.Short);
                    break;
            }
            keyboardAcceleratorInvokedEventArgs.Handled = true;
        }
        else
        {
            MediaPlayerWrapper?.FastForward(RewindMode.Normal);
        }
    }

    [RelayCommand]
    private void Rewind(object args)
    {
        if (args is KeyboardAcceleratorInvokedEventArgs keyboardAcceleratorInvokedEventArgs)
        {
            var modifier = keyboardAcceleratorInvokedEventArgs.KeyboardAccelerator.Modifiers;
            switch (modifier)
            {
                case VirtualKeyModifiers.None:
                case VirtualKeyModifiers.Menu://10s
                    MediaPlayerWrapper?.Rewind(RewindMode.Normal);
                    break;
                case VirtualKeyModifiers.Control://60s
                    MediaPlayerWrapper?.Rewind(RewindMode.Long);
                    break;
                case VirtualKeyModifiers.Shift://3s
                    MediaPlayerWrapper?.Rewind(RewindMode.Short);
                    break;
            }
            keyboardAcceleratorInvokedEventArgs.Handled = true;
        }
        else
        {
            MediaPlayerWrapper?.Rewind(RewindMode.Normal);
        }
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is IReadOnlyList<IStorageItem> fileList)
        {
            var filePath = fileList.First().Path;
            FilePath = filePath;
        }
    }

    /// <summary>
    /// Opens a dropped file by re-navigating the player to it.
    /// </summary>
    public void OpenStorageItems(IReadOnlyList<IStorageItem> items)
    {
        if (items.OfType<IStorageFile>().FirstOrDefault() is { } file)
        {
            _log.Information("Player received dropped file '{0}'", file.Path);
            _navigationService.NavigateTo(typeof(VideoPlayerViewModel).FullName!, new List<IStorageItem> { file });
        }
    }

    public void OnNavigatedFrom()
    {
        // Remember where we left off (and the volume) before the player goes away.
        SaveResumeState();
    }

    public void Dispose()
    {
        var mediaPlayer = Player;
        if (mediaPlayer != null)
        {
            mediaPlayer.EndReached -= OnEndReached;
        }

        Player = null;
        mediaPlayer?.Dispose();
        LibVLC?.Dispose();
        LibVLC = null;
    }
}

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

    public VideoPlayerViewModel(INavigationService navigationService, IWindowPresenterService windowPresenterService, ILogger log)
    {
        _navigationService = navigationService;
        _windowPresenterService = windowPresenterService;
        _log = log;

        _windowPresenterService.WindowPresenterChanged += OnWindowPresenterChanged;
        controlsHideTimer.Tick += Timer_Tick;

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

        LibVLC = new LibVLC(true, eventArgs.SwapChainOptions);
        Player = new MediaPlayer(LibVLC);

        var media = new Media(LibVLC, new Uri(FilePath));
        Player.Play(media);
        _log.Information("Starting playback of '{0}'", FilePath);

        MediaPlayerWrapper = new ObservableMediaPlayerWrapper(Player, _dispatcherQueue);
        RestartHideTimer();
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
        //Player.Playing -= Player_Playing;
        //Player.TimeChanged -= Player_TimeChanged;
        //Player.Media.DurationChanged -= Media_DurationChanged;
        //Player.MediaChanged -= Player_MediaChanged;
        //Player.Paused -= Player_Paused;
        //Player.Stopped -= Player_Stopped;
        //Player.VolumeChanged -= Player_VolumeChanged;
    }

    public void Dispose()
    {
        var mediaPlayer = Player;
        Player = null;
        mediaPlayer?.Dispose();
        LibVLC?.Dispose();
        LibVLC = null;
    }
}

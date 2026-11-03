using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CountDownTimer;

/// <summary>
/// Borderless, transparent, always-on-top window that shows the remaining time.
/// It starts in the upper right corner of the screen. When only a few seconds
/// are left (see settings.json) the digits start flashing, and when the time is
/// up the "finished" text (default "TimeOut!") flashes until closed or restarted.
///
/// Controls:
///   drag with left mouse  - move the timer
///   mouse wheel           - make the digits bigger / smaller
///   Space                 - pause / resume
///   R                     - restart with the same duration
///   N                     - set a new duration
///   Esc                   - close
///   right-click           - menu with the same actions
/// </summary>
public partial class TimerWindow : Window
{
    // How often the display is refreshed while counting down.
    private static readonly TimeSpan DisplayRefreshInterval = TimeSpan.FromMilliseconds(100);

    // How fast the digits blink (time between on and off).
    private static readonly TimeSpan FlashInterval = TimeSpan.FromMilliseconds(500);

    // Gap between the timer and the screen edges when it starts in the corner.
    private const double ScreenEdgeMargin = 16;

    // Limits for resizing the digits with the mouse wheel.
    private const double MinimumFontSize = 16;
    private const double MaximumFontSize = 400;
    private const double FontSizeStep = 6;

    private static readonly Brush NormalDigitBrush = Brushes.White;
    private static readonly Brush FlashingDigitBrush = Brushes.Red;

    // Values from settings.json.
    private readonly TimerSettings settings;

    // Ticks while counting down and updates the digits.
    private readonly DispatcherTimer countdownTicker;

    // Toggles the digits on/off while flashing.
    private readonly DispatcherTimer flashTicker;

    // Measures time that has passed since the countdown was (re)started or resumed.
    // A Stopwatch is used instead of counting ticks so the timer never drifts.
    private readonly Stopwatch elapsedSinceResume = new();

    // The full length the user asked for (used by "Restart").
    private TimeSpan totalDuration;

    // Time that was left when the countdown was last started or resumed.
    private TimeSpan remainingAtLastResume;

    // Screen position of the window's right edge. The text changes width
    // (for example "00:05" -> "TimeOut!"), so the window grows to the left
    // and stays lined up with the right side of the screen.
    private double rightEdgePosition;

    private bool isPaused;
    private bool isFinished;

    public TimerWindow(TimeSpan duration, TimerSettings timerSettings)
    {
        InitializeComponent();
        settings = timerSettings;

        PlaceInUpperRightCorner();

        countdownTicker = new DispatcherTimer(DispatcherPriority.Render) { Interval = DisplayRefreshInterval };
        countdownTicker.Tick += OnCountdownTick;

        flashTicker = new DispatcherTimer { Interval = FlashInterval };
        flashTicker.Tick += OnFlashTick;

        StartCountdown(duration);
    }

    /// <summary>Time still left on the countdown right now.</summary>
    private TimeSpan RemainingTime => remainingAtLastResume - elapsedSinceResume.Elapsed;

    // ----- Position -----

    /// <summary>
    /// Puts the timer in the upper right corner of the main screen's work area
    /// (the screen minus the taskbar). The final Left is set in OnSizeChanged,
    /// once the window knows how wide it is.
    /// </summary>
    private void PlaceInUpperRightCorner()
    {
        Rect workArea = SystemParameters.WorkArea;
        rightEdgePosition = workArea.Right - ScreenEdgeMargin;
        Top = workArea.Top + ScreenEdgeMargin;
        Left = rightEdgePosition; // temporary, corrected as soon as the width is known
    }

    /// <summary>Keeps the right edge in place when the text gets wider or narrower.</summary>
    private void OnSizeChanged(object sender, SizeChangedEventArgs sizeArguments)
    {
        if (sizeArguments.WidthChanged)
        {
            Left = rightEdgePosition - ActualWidth;
        }
    }

    // ----- Countdown -----

    /// <summary>Starts (or restarts) the countdown from <paramref name="duration"/>.</summary>
    private void StartCountdown(TimeSpan duration)
    {
        totalDuration = duration;
        remainingAtLastResume = duration;
        isPaused = false;
        isFinished = false;
        PauseMenuItem.Header = "_Pause";

        elapsedSinceResume.Restart();
        countdownTicker.Start();
        UpdateDisplay();
    }

    /// <summary>Pauses a running countdown, or resumes a paused one.</summary>
    private void TogglePause()
    {
        // Nothing to pause once the time is up.
        if (isFinished)
        {
            return;
        }

        if (isPaused)
        {
            // Resume: continue from where we paused.
            isPaused = false;
            elapsedSinceResume.Restart();
            countdownTicker.Start();
            PauseMenuItem.Header = "_Pause";
        }
        else
        {
            // Pause: remember how much was left and stop the clock.
            remainingAtLastResume = RemainingTime;
            elapsedSinceResume.Reset();
            countdownTicker.Stop();
            isPaused = true;
            PauseMenuItem.Header = "_Resume";
        }
        UpdateDisplay();
    }

    /// <summary>Lets the user type a new duration and starts counting from it.</summary>
    private void AskForNewDuration()
    {
        TimeSpan? newDuration = DurationDialog.AskForDuration(totalDuration, ownerWindow: this);
        if (newDuration is not null)
        {
            StartCountdown(newDuration.Value);
        }
    }

    private void OnCountdownTick(object? sender, EventArgs tickArguments)
    {
        if (RemainingTime <= TimeSpan.Zero)
        {
            FinishCountdown();
        }
        else
        {
            UpdateDisplay();
        }
    }

    /// <summary>Called when the time is up: stop counting and show the "finished" text.</summary>
    private void FinishCountdown()
    {
        countdownTicker.Stop();
        elapsedSinceResume.Reset();
        remainingAtLastResume = TimeSpan.Zero;
        isFinished = true;
        UpdateDisplay();
    }

    // ----- Display and flashing -----

    /// <summary>Updates the text and decides whether it should be flashing.</summary>
    private void UpdateDisplay()
    {
        TimeTextBlock.Text = isFinished
            ? settings.FinishedText
            : DurationParser.FormatForDisplay(RemainingTime);

        UpdateFlashing();
    }

    /// <summary>
    /// Flashing starts when the remaining time reaches the configured number of
    /// seconds (default 10) and keeps going after the time is up.
    /// A paused timer doesn't flash; its digits are dimmed instead.
    /// </summary>
    private void UpdateFlashing()
    {
        TimeSpan flashThreshold = TimeSpan.FromSeconds(settings.FlashWhenSecondsLeft);
        bool isInFlashingPhase = isFinished || RemainingTime <= flashThreshold;
        bool shouldFlash = isInFlashingPhase && !isPaused;

        TimeTextBlock.Foreground = isInFlashingPhase ? FlashingDigitBrush : NormalDigitBrush;

        if (shouldFlash)
        {
            // Start blinking only once; restarting the ticker every update would reset the rhythm.
            if (!flashTicker.IsEnabled)
            {
                TimeTextBlock.Opacity = 1.0;
                flashTicker.Start();
            }
        }
        else
        {
            flashTicker.Stop();
            TimeTextBlock.Opacity = isPaused ? 0.5 : 1.0; // dimmed digits show that the timer is paused
        }
    }

    /// <summary>Blinks the digits by switching them between visible and invisible.</summary>
    private void OnFlashTick(object? sender, EventArgs tickArguments)
    {
        TimeTextBlock.Opacity = TimeTextBlock.Opacity > 0.5 ? 0.0 : 1.0;
    }

    // ----- Mouse and keyboard -----

    /// <summary>Lets the user drag the timer anywhere on screen.</summary>
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseArguments)
    {
        if (mouseArguments.ButtonState == MouseButtonState.Pressed)
        {
            DragMove(); // returns when the mouse button is released
            rightEdgePosition = Left + ActualWidth; // remember the new spot
        }
    }

    /// <summary>Mouse wheel makes the digits bigger (up) or smaller (down).</summary>
    private void OnMouseWheel(object sender, MouseWheelEventArgs wheelArguments)
    {
        double sizeChange = wheelArguments.Delta > 0 ? FontSizeStep : -FontSizeStep;
        TimeTextBlock.FontSize = Math.Clamp(TimeTextBlock.FontSize + sizeChange, MinimumFontSize, MaximumFontSize);
    }

    private void OnKeyDown(object sender, KeyEventArgs keyArguments)
    {
        switch (keyArguments.Key)
        {
            case Key.Space:
                TogglePause();
                break;
            case Key.R:
                StartCountdown(totalDuration);
                break;
            case Key.N:
                AskForNewDuration();
                break;
            case Key.Escape:
                Close();
                break;
        }
    }

    // ----- Right-click menu -----

    private void OnPauseClicked(object sender, RoutedEventArgs clickArguments) => TogglePause();

    private void OnRestartClicked(object sender, RoutedEventArgs clickArguments) => StartCountdown(totalDuration);

    private void OnNewTimeClicked(object sender, RoutedEventArgs clickArguments) => AskForNewDuration();

    private void OnCloseClicked(object sender, RoutedEventArgs clickArguments) => Close();

    /// <summary>Stops both timers when the window closes.</summary>
    protected override void OnClosed(EventArgs closedArguments)
    {
        countdownTicker.Stop();
        flashTicker.Stop();
        base.OnClosed(closedArguments);
    }
}

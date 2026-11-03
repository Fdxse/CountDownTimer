using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CountDownTimer;

/// <summary>
/// Borderless, transparent, always-on-top window that shows the remaining time
/// and flashes "00:00" once the countdown has finished.
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

    // How fast "00:00" blinks when the time is up (time between on and off).
    private static readonly TimeSpan FlashInterval = TimeSpan.FromMilliseconds(500);

    // Limits for resizing the digits with the mouse wheel.
    private const double MinimumFontSize = 16;
    private const double MaximumFontSize = 400;
    private const double FontSizeStep = 6;

    private static readonly Brush NormalDigitBrush = Brushes.White;
    private static readonly Brush FinishedDigitBrush = Brushes.Red;

    // Ticks while counting down and updates the digits.
    private readonly DispatcherTimer countdownTicker;

    // Ticks after the countdown has finished and toggles the digits on/off.
    private readonly DispatcherTimer flashTicker;

    // Measures time that has passed since the countdown was (re)started or resumed.
    // A Stopwatch is used instead of counting ticks so the timer never drifts.
    private readonly Stopwatch elapsedSinceResume = new();

    // The full length the user asked for (used by "Restart").
    private TimeSpan totalDuration;

    // Time that was left when the countdown was last started or resumed.
    private TimeSpan remainingAtLastResume;

    private bool isPaused;
    private bool isFinished;

    public TimerWindow(TimeSpan duration)
    {
        InitializeComponent();

        countdownTicker = new DispatcherTimer(DispatcherPriority.Render) { Interval = DisplayRefreshInterval };
        countdownTicker.Tick += OnCountdownTick;

        flashTicker = new DispatcherTimer { Interval = FlashInterval };
        flashTicker.Tick += OnFlashTick;

        StartCountdown(duration);
    }

    /// <summary>Time still left on the countdown right now.</summary>
    private TimeSpan RemainingTime => remainingAtLastResume - elapsedSinceResume.Elapsed;

    /// <summary>Starts (or restarts) the countdown from <paramref name="duration"/>.</summary>
    private void StartCountdown(TimeSpan duration)
    {
        StopFlashing();

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
            TimeTextBlock.Opacity = 1.0;
            PauseMenuItem.Header = "_Pause";
        }
        else
        {
            // Pause: remember how much was left and stop the clock.
            remainingAtLastResume = RemainingTime;
            elapsedSinceResume.Reset();
            countdownTicker.Stop();
            isPaused = true;
            TimeTextBlock.Opacity = 0.5; // dimmed digits show that the timer is paused
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

    /// <summary>Called when the time is up: stop counting and start flashing zero.</summary>
    private void FinishCountdown()
    {
        countdownTicker.Stop();
        elapsedSinceResume.Reset();
        remainingAtLastResume = TimeSpan.Zero;
        isFinished = true;

        UpdateDisplay(); // shows "00:00"
        TimeTextBlock.Foreground = FinishedDigitBrush;
        flashTicker.Start();
    }

    /// <summary>Blinks the digits by switching them between visible and invisible.</summary>
    private void OnFlashTick(object? sender, EventArgs tickArguments)
    {
        TimeTextBlock.Opacity = TimeTextBlock.Opacity > 0.5 ? 0.0 : 1.0;
    }

    private void StopFlashing()
    {
        flashTicker.Stop();
        TimeTextBlock.Opacity = 1.0;
        TimeTextBlock.Foreground = NormalDigitBrush;
    }

    private void UpdateDisplay()
    {
        TimeTextBlock.Text = DurationParser.FormatForDisplay(RemainingTime);
    }

    // ----- Mouse and keyboard -----

    /// <summary>Lets the user drag the timer anywhere on screen.</summary>
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs mouseArguments)
    {
        if (mouseArguments.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
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

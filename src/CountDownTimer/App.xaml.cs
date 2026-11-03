using System.Windows;

namespace CountDownTimer;

/// <summary>
/// Starts the app. If a duration is passed on the command line (for example
/// "CountDownTimer.exe 5:00") the timer starts straight away; otherwise the
/// user is asked for a duration first.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs startupArguments)
    {
        base.OnStartup(startupArguments);

        // Read settings.json (end text, when to start flashing) before anything is shown.
        TimerSettings timerSettings = TimerSettings.Load();

        // Try to use a duration given on the command line.
        TimeSpan? durationFromCommandLine = null;
        if (startupArguments.Args.Length > 0
            && DurationParser.TryParse(string.Join(" ", startupArguments.Args), out TimeSpan parsedDuration))
        {
            durationFromCommandLine = parsedDuration;
        }

        // Fall back to asking the user.
        TimeSpan? chosenDuration = durationFromCommandLine ?? DurationDialog.AskForDuration(previousDuration: null);
        if (chosenDuration is null)
        {
            // User cancelled the dialog: nothing to show, so quit.
            Shutdown();
            return;
        }

        var timerWindow = new TimerWindow(chosenDuration.Value, timerSettings);
        timerWindow.Closed += (_, _) => Shutdown();
        timerWindow.Show();
    }
}

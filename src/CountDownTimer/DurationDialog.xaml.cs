using System.Windows;

namespace CountDownTimer;

/// <summary>
/// Dialog that asks the user for the countdown length.
/// </summary>
public partial class DurationDialog : Window
{
    /// <summary>The duration the user entered, set when they press Start.</summary>
    public TimeSpan? ChosenDuration { get; private set; }

    public DurationDialog(TimeSpan? previousDuration)
    {
        InitializeComponent();

        // Pre-fill with the last used duration so "same again" is one click.
        if (previousDuration is not null)
        {
            DurationTextBox.Text = DurationParser.FormatForDisplay(previousDuration.Value);
        }
        DurationTextBox.SelectAll();
    }

    /// <summary>
    /// Shows the dialog and returns the chosen duration, or null if the user cancelled.
    /// </summary>
    public static TimeSpan? AskForDuration(TimeSpan? previousDuration, Window? ownerWindow = null)
    {
        var dialog = new DurationDialog(previousDuration);
        if (ownerWindow is not null)
        {
            dialog.Owner = ownerWindow;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        return dialog.ShowDialog() == true ? dialog.ChosenDuration : null;
    }

    private void OnStartClicked(object sender, RoutedEventArgs clickArguments)
    {
        if (DurationParser.TryParse(DurationTextBox.Text, out TimeSpan parsedDuration))
        {
            ChosenDuration = parsedDuration;
            DialogResult = true; // closes the dialog
        }
        else
        {
            // Keep the dialog open and tell the user what went wrong.
            ErrorTextBlock.Visibility = Visibility.Visible;
            DurationTextBox.Focus();
            DurationTextBox.SelectAll();
        }
    }
}

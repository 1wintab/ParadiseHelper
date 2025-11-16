using System;
using System.Linq;
using System.Windows.Forms;
using ParadiseHelper;

/// <summary>
/// A custom Windows Form class that implements special closing logic to manage owned child forms
/// and ensures a smooth, non-flickering fade-in effect upon loading.
/// </summary>
public class SmartForm : Form
{
    // Flag to prevent recursive calls during the forced closing sequence.
    private bool _isForceClosing = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartForm"/> class.
    /// </summary>
    public SmartForm()
    {
        // 1. Force the form to start completely transparent.
        this.Opacity = 0;

        // Enable double buffering for the form itself.
        // This forces GDI+ to draw all content into an off-screen buffer first,
        // and then display the finished frame. This is the MAIN FIX for "flickering" or visual artifacts.
        this.SetStyle(ControlStyles.UserPaint |
                      ControlStyles.AllPaintingInWmPaint |
                      ControlStyles.OptimizedDoubleBuffer, true);
    }

    /// <summary>
    /// This method is called AFTER the child form's constructor (e.g., AddAccountForm)
    /// and all initial component setup (like InitializeComponent) have completed their work.
    /// </summary>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    protected override void OnLoad(EventArgs e)
    {
        // 1. FORCE the Opacity to 0 here.
        // This guarantees the form is transparent, even if InitializeComponent()
        // in a derived class set Opacity = 1.
        if (this.Opacity != 0)
        {
            this.Opacity = 0;
        }

        base.OnLoad(e);

        // 2. Only now, when all UI creation work is complete and buffered,
        // do we start the smooth 'FadeIn' animation.
        UIEffects.ApplyFadeIn(this);
    }

    /// <summary>
    /// Overrides the standard event fired when the form is about to close.
    /// Implements logic to ensure all owned child forms are closed first.
    /// </summary>
    /// <param name="e">A <see cref="FormClosingEventArgs"/> that contains the event data.</param>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Check 1: Is this a normal close attempt (not the forced re-call)
        // AND are there still un-disposed "owned" child forms attached?
        if (!_isForceClosing && this.OwnedForms.Any(f => !f.IsDisposed))
        {
            // Cancel the initial close event for the main form.
            e.Cancel = true;

            // 1. Manually close all owned child forms first.
            foreach (var form in this.OwnedForms)
            {
                if (!form.IsDisposed)
                    form.Close(); // This triggers the OnFormClosing event on the child form.
            }

            // 2. Set the flag to indicate the closing sequence is now active,
            // preventing the infinite loop on the re-call.
            _isForceClosing = true;

            // 3. Re-initiate closing the main form asynchronously on the UI thread.
            // This ensures the main form closes after all owned forms have finished processing
            // their own close events (including any pending operations they might have).
            this.BeginInvoke(new Action(() => this.Close()));

            return;
        }

        // If the flag is set (it's the second, forced attempt) or no child forms exist,
        // proceed with standard closing.
        base.OnFormClosing(e);
    }
}
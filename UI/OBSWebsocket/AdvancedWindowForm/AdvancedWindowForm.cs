using System.Windows.Forms;
using ParadiseHelper;
using OBSWebsocketDotNet.Types;
using ParadiseHelper.OBS;

namespace UI.OBSWebsocket
{
    // NOTE: This class is split into multiple partial files for better code organization.
    // See AdvancedWindowForm.Events.cs, AdvancedWindowForm.CoreLogic.cs, etc. for full implementation.

    public partial class AdvancedWindowForm : SmartForm
    {
        // --- Constructor & Form Lifecycle ---

        /// <summary>
        /// Initializes a new instance of the <see cref="AdvancedWindowForm"/> class.
        /// Sets up components, event handlers, and initial UI state.
        /// </summary>
        public AdvancedWindowForm()
        {
            InitializeComponent();

            SetupEventHandlers();       // In AdvancedWindowForm.Events.cs
            InitObsUiManagement();      // In AdvancedWindowForm.Events.cs

            // Initialization for UI elements that should move to User Controls later
            InitTransformSourceSettings(); // Should move to TransformSettingsControl

            ApplyFont();                // In AdvancedWindowForm.UI.cs
            ApplyVisualStyle();         // In AdvancedWindowForm.UI.cs

            SetupFormBehavior();
            SetupInputFields();         // In AdvancedWindowForm.UI.cs
        }

        /// <summary>
        /// Handles cleanup tasks when the form is closed.
        /// Unsubscribes from all active OBS events to prevent memory leaks.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.FormClosedEventArgs"/> that contains the event data.</param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Unsubscribe from core OBS Controller events
            ObsController.Instance.ConnectionStatusChanged -= OBSConnectionStatusChanged;
            ObsController.Instance.VirtualCamStatusChanged -= OnVirtualCamStateChanged;

            // Unsubscribe from OBS websocket client events
            var obsClient = ObsController.Instance.Client;
            if (obsClient != null)
            {
                obsClient.SceneListChanged -= OnSceneStructureChanged;
                obsClient.SceneItemCreated -= OnSceneStructureChanged;
                obsClient.SceneItemRemoved -= OnSceneStructureChanged;
                obsClient.SceneNameChanged -= OnSceneStructureChanged;
                obsClient.InputNameChanged -= OnSceneStructureChanged;
                obsClient.CurrentProgramSceneChanged -= OnCurrentProgramSceneChanged;
            }

            base.OnFormClosed(e);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event to draw a custom border.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw a custom border for the form using the UI helper
            UIHelper.DrawFormBorder(e, this);
        }

        /// <summary>
        /// Initializes the transform source settings text fields with default values.
        /// </summary>
        private void InitTransformSourceSettings()
        {
            tbSourceWidth.Text = $"{ObsConstants.DefaultFullWindowTransform.BoundsWidth}";
            tbSourceHeight.Text = $"{ObsConstants.DefaultFullWindowTransform.BoundsHeight}";

            if (ObsConstants.DefaultFullWindowTransform.BoundsType == SceneItemBoundsType.OBS_BOUNDS_STRETCH)
            {
                tbSourceTransform.Text = "STRETCH";
            }
            else
            {
                tbSourceTransform.Text = "Unknown";
            }
        }

        /// <summary>
        /// Configures form-level properties, like visibility in the taskbar.
        /// </summary>
        private void SetupFormBehavior()
        {
            this.ShowInTaskbar = false;
        }
    }
}
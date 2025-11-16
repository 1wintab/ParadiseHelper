using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using SteamLogAccounts.SteamAuth;
using ParadiseHelper.Core;
using ParadiseHelper.Tools;
using ParadiseHelper.SteamAuth;
using ParadiseHelper.Managers.Steam;
using ParadiseHelper.Managers.MaFiles;
using ParadiseHelper.SteamLogAccounts.SteamEnv;
using ParadiseHelper.Data.Settings.LaunchParameters;
using Core;
using Data.Settings.LaunchParameters;
using WindowsInput.Native;
using WindowsInput;
using ParadiseHelper.Tools.WinAPI;
using ParadiseHelper.OBS;

namespace ParadiseHelper.SteamLogAccounts.SteamAuth
{
    /// <summary>
    /// Main class responsible for launching the Steam client, automating the login process using image recognition,
    /// and setting up the Counter-Strike 2 (CS2) process for further management.
    /// </summary>
    public class AuthSteam
    {
        /// <summary>
        /// Executes the full Steam login and CS2 launch sequence for a single account.
        /// </summary>
        /// <param name="login">The Steam account login name.</param>
        /// <param name="password">The Steam account password.</param>
        /// <param name="secretMap">A dictionary containing the account's login to its 2FA shared secret.</param>
        /// <param name="token">A <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
        /// <param name="steamParams">Additional command-line parameters for the Steam client.</param>
        /// <param name="cs2Params">Additional command-line parameters for CS2.</param>
        /// <param name="launchMode">The launch configuration mode for CS2 (e.g., Low, Medium, High).</param>
        /// <param name="isSpecialMode">A flag indicating if the account is running in a special/primary mode (for window title tracking).</param>
        /// <returns><see langword="true"/> if the authentication and setup sequence completed successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if initial account validation or setup fails (e.g., missing SteamID64, settings application error).</exception>
        public static bool Run(
            string login,
            string password,
            Dictionary<string, string> secretMap,
            CancellationToken token,
            string steamParams,
            string cs2Params,
            LaunchMode launchMode,
            bool isSpecialMode)
        {
            // 1. Initial Validation and Setup (Get SteamID64, apply game presets, retrieve 2FA secret).
            if (!ValidateAccountAndApplyPresets(
                login,
                out uint accountId,
                out string sharedSecret,
                out string error,
                secretMap,
                launchMode))
            {
                // Throw an exception with the error message.
                // This exception will be caught in the 'catch' block of the RunAccountsAsync method.
                throw new InvalidOperationException(error);
            }

            token.ThrowIfCancellationRequested();

            // 2. Launch Steam and register its PID.
            LaunchSteamAndRegisterProcess(login, token, steamParams, cs2Params);

            // Wait for the Steam sign-in window to appear.
            WindowController.WaitForWindowByClassName("SDL_app");
            token.ThrowIfCancellationRequested();

            // 3. Perform login with image recognition and a retry mechanism.
            if (!PerformLoginLoop(login, password, sharedSecret, token))
            {
                // If login fails, clean up the process registry and return false.
                int pid;
                
                if (SteamProcessRegistry.TryGetProcessId(login, out pid))
                {
                    // Attempt to unregister the PID if it was found.
                    SteamProcessRegistry.Unregister(login);
                }

                return false;
            }

            token.ThrowIfCancellationRequested();

            // 4. Wait for CS2 to launch and set the window title for identification.
            WaitForCs2AndSetup(login, isSpecialMode);

            return true;
        }

        /// <summary>
        /// Helper: Validates account prerequisites (SteamID64, 2FA secret) and applies game settings.
        /// </summary>
        private static bool ValidateAccountAndApplyPresets(
            string login,
            out uint accountId,
            out string sharedSecret,
            out string errorMessage,
            Dictionary<string, string> secretMap,
            LaunchMode launchMode)
        {
            var steamId64 = MaFilesManager.GetSteamID64ByLogin(login);
            accountId = 0;
            sharedSecret = null;
            errorMessage = null;

            if (steamId64 is null)
            {
                // Use a specific, unique prefix/tag to signal a MaFile error.
                // The calling method (RunAccountsAsync) will check for this exact prefix.
                errorMessage = $"CRITICAL_MAFILE_MISSING: {login}";

                return false;
            }

            accountId = SteamIdHelper.ToAccountId(steamId64.Value);
            try
            {
                // Apply CS2 video settings based on the configured launch mode.
                VideoSettingsCS2.ApplySettings(accountId, launchMode);
            }
            catch (Exception ex)
            {
                // If applying settings fails, consider it a validation error
                errorMessage = $"Failed to apply video settings for account ID {accountId}: {ex.Message}";

                return false;
            }

            // Attempt to retrieve the 2FA shared secret for the login.
            secretMap.TryGetValue(login, out sharedSecret);

            return true;
        }

        /// <summary>
        /// Launches the Steam process with specified parameters and registers the new process ID in the registry.
        /// </summary>
        private static void LaunchSteamAndRegisterProcess(string login, CancellationToken token, string steamParams, string cs2Params)
        {
            // Get PIDs of currently running Steam processes before launch.
            var before = Process.GetProcessesByName("steam").Select(p => p.Id).ToList();

            // Construct the final launch parameters string.
            string finalLaunchParams = $"{steamParams} -applaunch 730 {cs2Params}";

            // Launch the Steam process using the configured launcher path.
            ProcessManager.LaunchFromJson(
                AuthConstants.LauncherConfigJsonPath,
                "Steam",
                finalLaunchParams
            );

            token.ThrowIfCancellationRequested();

            // Identify the newly launched Steam process.
            var newProc = Process.GetProcessesByName("steam")
                .FirstOrDefault(proc => !before.Contains(proc.Id));

            if (newProc != null)
            {
                // Register the new process ID for the current login.
                SteamProcessRegistry.Register(login, newProc.Id);
            }
        }

        /// <summary>
        /// Executes the login sequence repeatedly until successful or the maximum number of retries is reached.
        /// </summary>
        private static bool PerformLoginLoop(string login, string password, string sharedSecret, CancellationToken token)
        {
            bool retry = true;
            int retries = 0;

            while (retry && retries < AuthConstants.MaxRetries)
            {
                try
                {
                    retry = false;
                    retries++;

                    // Wait for the login form fields to be visible/clickable.
                    if (!WaitForLoginFormFields(token))
                    {
                        return false;
                    }

                    // Attempt to input credentials and 2FA code.
                    if (!AttemptLogin(login, password, sharedSecret, token))
                    {
                        // Handle potential errors that require a retry (e.g., 'too many retries' message).
                        retry = HandleLoginRetryError(token);
                        if (!retry)
                        {
                            return false;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Handle expected cancellation request. Exit the login loop gracefully.
                    Console.WriteLine($"Login process for '{login}' was cancelled by request.");
                    return false;
                }    
            }

            return retries <= AuthConstants.MaxRetries;
        }

        /// <summary>
        /// Waits for the Steam login form fields to appear, handling the case where 'Add Another Account' is visible.
        /// </summary>
        private static bool WaitForLoginFormFields(CancellationToken token)
        {
            DateTime startTime = DateTime.Now;

            string windowTitle = "Sign in to Steam";

            while (true)
            {
                token.ThrowIfCancellationRequested();

                if (!WindowController.IsSteamActive())
                {
                    WindowController.EnsureWindowFocusByTitle(windowTitle);
                }

                bool loginVisible = TemplateMatcher.IsTemplateVisible(
                    AuthConstants.LoginTemplatePath,
                    AuthConstants.Threshold
                );

                bool passwordVisible = TemplateMatcher.IsTemplateVisible(
                    AuthConstants.PasswordTemplatePath,
                    AuthConstants.Threshold
                );

                bool addAccountVisible = TemplateMatcher.IsTemplateVisible(
                    AuthConstants.AddAnotherAccountTemplatePath,
                    AuthConstants.Threshold
                );

                // Success condition: Both fields are visible.
                if (loginVisible && passwordVisible) return true;

                // Alternate condition: 'Add Another Account' is visible, meaning a previous account is signed in.
                if (!loginVisible && !passwordVisible && addAccountVisible)
                {
                    token.ThrowIfCancellationRequested();

                    // Click 'Add Another Account' to reveal the login form.
                    TemplateHelper.ClickTemplateCenter(
                        windowTitle: windowTitle,
                        templatePath: AuthConstants.AddAnotherAccountTemplatePath,
                        threshold: AuthConstants.ThresholdForPlus,
                        position: TemplateHelper.TemplateClickPosition.Center
                    );

                    // Wait briefly for the login fields to become visible after the click.
                    TemplateHelper.WaitForTemplateVisible(
                        templatePath: AuthConstants.LoginTemplatePath,
                        threshold: AuthConstants.Threshold,
                        timeoutMs: 3000
                        );
                }
                else
                {
                    Thread.Sleep(100);
                }

                // Timeout check.
                if (DateTime.Now - startTime > AuthConstants.LoginTimeout)
                {
                    MessageBox.Show(
                        "⏳ Failed to find login/password fields within 50 seconds",
                        "Steam AutoLogin",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                        );

                    return false;
                }
            }
        }

        /// <summary>
        /// Attempts to input the login, password, and 2FA code (if available) into the Steam sign-in window.
        /// </summary>
        private static bool AttemptLogin(string login, string password, string sharedSecret, CancellationToken token)
        {
            string windowTitle = "Sign in to Steam";
            token.ThrowIfCancellationRequested();

            // Type Login
            TemplateHelper.TypeIntoTemplate(
                windowTitle,
                AuthConstants.LoginTemplatePath,
                AuthConstants.Threshold,
                login,
                TemplateHelper.TemplateClickPosition.BottomCenter
            );
            token.ThrowIfCancellationRequested();

            // Type Password
            TemplateHelper.TypeIntoTemplate(
                windowTitle,
                AuthConstants.PasswordTemplatePath,
                AuthConstants.Threshold,
                password,
                TemplateHelper.TemplateClickPosition.BottomCenter
            );
            token.ThrowIfCancellationRequested();

            // Click Sign In button
            TemplateHelper.ClickTemplateCenter(
                windowTitle,
                AuthConstants.SignInButtonTemplatePath,
                AuthConstants.Threshold,
                TemplateHelper.TemplateClickPosition.Center
            );

            // Handle 2FA if a shared secret is provided.
            if (!string.IsNullOrEmpty(sharedSecret))
            {
                // Wait for the Steam Guard Code input field to appear after clicking Sign In.
                if (!TemplateHelper.WaitForTemplateVisible(
                    AuthConstants.SteamGuardCodeTemplatePath,
                    AuthConstants.Threshold,
                    5000))
                {
                    // If the code field doesn't appear, the login might have succeeded without needing 2FA 
                    // (e.g., if the user ticked 'Remember me') or failed instantly. Assuming success if no error is visible yet.
                    return true;
                }

                token.ThrowIfCancellationRequested();
                // Generate the time-based code.
                string code2FA = SteamGuardCodeGenerator.GenerateCode(sharedSecret);

                // Type the 2FA code.
                TemplateHelper.TypeIntoTemplate(
                    windowTitle,
                    AuthConstants.SteamGuardCodeTemplatePath,
                    AuthConstants.Threshold,
                    code2FA,
                    TemplateHelper.TemplateClickPosition.TopCenter
                );
                token.ThrowIfCancellationRequested();
            }
            return true;
        }

        /// <summary>
        /// Checks for common login error messages (e.g., rate-limiting) and attempts to dismiss them for a retry.
        /// </summary>
        /// <returns><see langword="true"/> if a retryable error was found and handled; otherwise, <see langword="false"/>.</returns>
        private static bool HandleLoginRetryError(CancellationToken token)
        {
            // Check if the generic 'Retries Error' template is visible.
            bool errorAppeared = TemplateHelper.WaitForTemplateVisible(
                AuthConstants.RetriesErrorTemplatePath,
                AuthConstants.Threshold,
                3000
            );
            token.ThrowIfCancellationRequested();

            if (errorAppeared)
            {
                // Click the error message's center to dismiss the Steam dialog.
                TemplateHelper.ClickTemplateCenter(
                    "Sign in to Steam",
                    AuthConstants.RetriesErrorTemplatePath,
                    AuthConstants.Threshold,
                    TemplateHelper.TemplateClickPosition.BottomCenter
                );

                return true; // Indicate that a retry should be performed.
            }

            return false; // Indicate no retryable error was found.
        }

        /// <summary>
        /// Waits for the CS2 process to launch and fully initialize, handles common Steam prompts (Cloud Conflict, Play Anyway),
        /// hides the Steam client, and sets the CS2 window title for identification.
        /// </summary>
        private static void WaitForCs2AndSetup(string login, bool isSpecialMode)
        {
            nint cs2Handle = nint.Zero;
            bool steamHidden = false;
            bool playAnywayClicked = false;
            bool cloudConflictClicked = false;

            // The loop waits for CS2 process to appear or prompts to be handled.
            for (int i = 0; i < AuthConstants.Cs2WaitIterations; i++)
            {
                // --- 1. Cloud Conflict Check ---
                if (!cloudConflictClicked && !steamHidden
                && TemplateMatcher.IsTemplateVisible(AuthConstants.CloudConflictTemplatePath, AuthConstants.ThresholdForPlus))
                {
                    // Select the cloud save option (assuming the preferred option is the first one, or click near a template that represents it).
                    TemplateHelper.ClickTemplateCenter(
                        windowTitle: "Steam",
                        templatePath: AuthConstants.SelectClouldSaveTemplatePath, // Template near the cloud save selection box
                        threshold: AuthConstants.ThresholdForPlus,
                        position: TemplateHelper.TemplateClickPosition.Center
                    );

                    Thread.Sleep(300);

                    // Click the 'Continue Play' button.
                    TemplateHelper.ClickTemplateCenter(
                        windowTitle: "Steam",
                        templatePath: AuthConstants.ContinuePlayTemplatePath,
                        threshold: AuthConstants.ThresholdForPlus,
                        position: TemplateHelper.TemplateClickPosition.Center
                    );

                    Thread.Sleep(300);

                    // Repeat click for reliability.
                    TemplateHelper.ClickTemplateCenter(
                        windowTitle: "Steam",
                        templatePath: AuthConstants.ContinuePlayTemplatePath,
                        threshold: AuthConstants.ThresholdForPlus,
                        position: TemplateHelper.TemplateClickPosition.Center
                    );

                    Thread.Sleep(300);

                    cloudConflictClicked = true;
                    
                    continue;
                }

                // --- 2. 'Play Anyway' Check (if Steam is visible) ---
                if (!playAnywayClicked && !steamHidden
                    && TemplateMatcher.IsTemplateVisible(AuthConstants.PlayAnywayTemplatePath, AuthConstants.ThresholdForPlus))
                {
                    // Click the 'Play Anyway' button center.
                    TemplateHelper.ClickTemplateCenter(
                        windowTitle: "Steam",
                        templatePath: AuthConstants.PlayAnywayTemplatePath,
                        threshold: AuthConstants.ThresholdForPlus,
                        position: TemplateHelper.TemplateClickPosition.Center
                    );

                    Thread.Sleep(250);   
                    
                    playAnywayClicked = true;  
                    
                    continue;
                }

                // --- 3. 'Continue Play' Check (only after 'Play Anyway' was potentially clicked) ---
                if (playAnywayClicked && !steamHidden &&
                    TemplateMatcher.IsTemplateVisible(
                        AuthConstants.ContinuePlayTemplatePath,
                        AuthConstants.ThresholdForPlus))
                {
                    // Click the 'Continue Play' button
                    TemplateHelper.ClickTemplateCenter(
                        windowTitle: "Steam",
                        templatePath: AuthConstants.ContinuePlayTemplatePath,
                        threshold: AuthConstants.ThresholdForPlus,
                        position: TemplateHelper.TemplateClickPosition.Center
                    );

                    Thread.Sleep(250);

                    // Find and hide Steam window after the click to ensure CS2 gets focus.
                    IntPtr steamHwnd = WindowController.FindWindowDirect("SDL_app", "Steam");
                    if (steamHwnd != IntPtr.Zero)
                    {
                        // Hide window using Alt+F4.
                        WindowController.HideWindowWithAltF4(steamHwnd);
                        steamHidden = true;

                        // Wait for the window to disappear before CS2 takes focus.
                        Thread.Sleep(1000);
                    }

                    continue;
                }

                // --- 4. CHECK FOR CS2 PROCESS ---
                var cs2 = Process.GetProcessesByName("cs2").FirstOrDefault(p => p.MainWindowHandle != nint.Zero);
                if (cs2 == null)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // Check for valid window dimensions to ensure CS2 is fully initialized and not just a placeholder window.
                if (!NativeMethods.GetWindowRect(cs2.MainWindowHandle, out NativeMethods.RECT rect))
                {
                    Thread.Sleep(100);
                    continue;
                }

                int widthNow = rect.Right - rect.Left;
                int heightNow = rect.Bottom - rect.Top;

                if (widthNow > 100 && heightNow > 100)
                {
                    cs2Handle = cs2.MainWindowHandle;

                    // If CS2 is ready, but Steam wasn't hidden by the 'Continue Play' check (case where no prompts appeared),
                    // hide it now.
                    if (!steamHidden)
                    {
                        IntPtr steamHwnd = WindowController.FindWindowDirect("SDL_app", "Steam");
                        if (steamHwnd != IntPtr.Zero)
                        {
                            WindowController.HideWindowWithAltF4(steamHwnd);
                            steamHidden = true;
                            Thread.Sleep(1000);
                        }
                    }

                    // CS2 is ready, exit the loop.
                    break;
                }
                Thread.Sleep(100);
            }

            // 5. FINAL SETUP: Set the CS2 window title.
            if (cs2Handle != nint.Zero)
            {
                // Set the CS2 window title to the account's login for identification and OBS tracking.
                string baseTitle = $"{login} [Counter-Strike 2]";

                string newTitle = isSpecialMode
                    ? $"{login} {ObsConstants.WindowCaptureSources.CS2.SpecialWindowTitleMarker} [Counter-Strike 2]"
                    : baseTitle;

                NativeMethods.SetWindowText(cs2Handle, newTitle);
            }
        }
    }
}
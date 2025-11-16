using System;
using System.Management;
using System.Windows.Forms;
using Microsoft.ML.OnnxRuntime;

namespace ParadiseHelper.AI.Video.GameCapture
{
    /// <summary>
    /// Provides utility methods for creating an <see cref="InferenceSession"/> for an ONNX model,
    /// prioritizing GPU acceleration via DirectML.
    /// </summary>
    public static class GpuProvider
    {
        /// <summary>
        /// Creates and configures an <see cref="InferenceSession"/> for the specified ONNX model path.
        /// It attempts to enable GPU acceleration using DirectML based on detected vendor.
        /// </summary>
        /// <param name="modelPath">The file path to the ONNX model.</param>
        /// <returns>A configured <see cref="InferenceSession"/> instance.</returns>
        public static InferenceSession CreateSession(string modelPath)
        {
            // SessionOptions holds configuration settings for the inference session.
            var options = new SessionOptions();

            // Attempt to identify the GPU vendor to conditionally enable DirectML.
            string gpuVendor = DetectGpuVendor();

            // Set execution mode for potentially better FPS stability.
            options.ExecutionMode = ExecutionMode.ORT_SEQUENTIAL;

            // Enable all available graph optimizations for performance.
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;

            try
            {
                // Check if a supported vendor (NVIDIA, AMD, or Intel) was detected.
                if (gpuVendor == "nvidia" || gpuVendor == "amd" || gpuVendor == "intel")
                {
                    // Attempt to add the DirectML execution provider for GPU offloading.
                    options.AppendExecutionProvider_DML();
                }
                else
                {
                    // Inform the user that the model will fall back to CPU execution.
                    MessageBox.Show(
                        "No supported GPU was detected. The model will run on CPU.",
                        "GPU Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions if GPU initialization (DML) fails and fall back to CPU.
                MessageBox.Show(
                    $"Failed to initialize GPU acceleration. The model will run on CPU.\n\nDetails:\n{ex.Message}",
                    "GPU Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            // Create and return the inference session, using the configured options (with or without DML).
            return new InferenceSession(modelPath, options);
        }

        // Attempts to detect the primary GPU vendor using Windows Management Instrumentation (WMI).
        private static string DetectGpuVendor()
        {
            try
            {
                // Query Windows for video controller information.
                using var searcher = new ManagementObjectSearcher("select * from Win32_VideoController");

                // Iterate through the detected video controller objects.
                foreach (var obj in searcher.Get())
                {
                    // Get the name and convert it to lower case for case-insensitive matching.
                    string name = obj["Name"]?.ToString()?.ToLower() ?? "";

                    // Check for common vendor keywords.
                    if (name.Contains("nvidia")) return "nvidia";
                    if (name.Contains("amd") || name.Contains("radeon")) return "amd";
                    if (name.Contains("intel")) return "intel";
                }
            }
            catch
            {
                // Ignore WMI access errors and fall through to return 'unknown'.
            }

            // Default return if no known vendor is found or an exception occurs.
            return "unknown";
        }
    }
}
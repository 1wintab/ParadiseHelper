using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using ParadiseHelper.AI.Core.Settings;
using Core;
using ParadiseHelper.AI.Video.GameCapture;
using ParadiseHelper.AI.Video.GameVideoSource;

namespace ParadiseHelper.AI.Video.Detect
{
    /// <summary>
    /// Represents a single detection result, containing the object's label,
    /// the confidence score, and the bounding box coordinates.
    /// </summary>
    public class YoloResult
    {
        /// <summary>
        /// Gets or sets the class label (e.g., "ct_head").
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets or sets the confidence score of the detection.
        /// </summary>
        public float Confidence { get; set; }

        /// <summary>
        /// Gets or sets the bounding box coordinates (x, y, w, h) in normalized (0-1) format.
        /// </summary>
        public RectangleF Box { get; set; }
    }

    /// <summary>
    /// Implements an object detection mechanism using a YOLO model run via the ONNX Runtime.
    /// </summary>
    public class YoloDetector : IDisposable
    {
        // The file name for the trained YOLO model.
        private const string YOLO_MODEL_FILE_NAME = "best.onnx";

        // Model's expected square input size (e.g., 512x512).
        private readonly int _inputSize = ModelSettings.Width;

        // The ONNX runtime session for executing the model.
        private readonly InferenceSession _session;

        // Name of the model's primary input tensor.
        private readonly string _inputName;

        // List of possible class labels the model is trained to detect.
        private readonly List<string> _labels = new()
        {
            "t_body",
            "t_head",
            "ct_body",
            "ct_head"
        };

        // Dependency for frame preprocessing, typically handles resize and color conversion on GPU.
        private readonly GpuPreprocessor _gpuPreprocessor;

        /// <summary>
        /// Gets a value indicating whether the detector is initialized and ready for inference.
        /// </summary>
        public bool IsReady => _session != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="YoloDetector"/> class.
        /// </summary>
        /// <param name="gpuPreprocessor">The preprocessor dependency used for frame preparation.</param>
        public YoloDetector(GpuPreprocessor gpuPreprocessor)
        {
            _gpuPreprocessor = gpuPreprocessor ?? throw new ArgumentNullException(nameof(gpuPreprocessor));

            // Construct the full model path using FilePaths.ModelsDirectory.
            string modelPath = Path.Combine(FilePaths.AI.ModelsDirectory, YOLO_MODEL_FILE_NAME);

            // Creates the inference session, typically using GPU acceleration via GpuProvider.
            _session = GpuProvider.CreateSession(modelPath);

            // Gets the name of the model's first input layer.
            _inputName = _session.InputMetadata.Keys.First();
        }

        /// <summary>
        /// Performs object detection on the input frame and returns a list of detected objects.
        /// </summary>
        /// <param name="inputFrameMat">The raw input frame (OpenCvSharp.Mat BGR).</param>
        /// <param name="originalWidth">The width of the original frame.</param>
        /// <param name="originalHeight">The height of the original frame.</param>
        /// <returns>A list of <see cref="YoloResult"/> objects.</returns>
        public List<YoloResult> Detect(Mat inputFrameMat, int originalWidth, int originalHeight)
        {
            if (!IsReady || inputFrameMat == null || inputFrameMat.Empty())
            {
                return new List<YoloResult>();
            }

            // Preprocess the frame (resize, normalize) using the GPU preprocessor, returning an OpenCvSharp.Mat.
            using var preprocessedMat = _gpuPreprocessor.ProcessFrame(inputFrameMat);
            if (preprocessedMat == null)
            {
                return new List<YoloResult>();
            }

            // Rent an array from the pool to hold the float tensor data (optimized memory usage).
            var floatData = ArrayPool<float>.Shared.Rent(preprocessedMat.Width * preprocessedMat.Height * 3);
            try
            {
                // Copy raw byte data from the Mat to a managed byte array.
                byte[] matData = new byte[preprocessedMat.Width * preprocessedMat.Height * 3];
                Marshal.Copy(preprocessedMat.Data, matData, 0, matData.Length);

                int totalPixels = preprocessedMat.Width * preprocessedMat.Height;
                for (int i = 0; i < totalPixels; i++)
                {
                    // Convert BGR byte data to normalized RGB floats (0-1.0) for the model, channel-first format (C, H, W).
                    floatData[0 * totalPixels + i] = matData[i * 3 + 2] / 255.0f; // R
                    floatData[1 * totalPixels + i] = matData[i * 3 + 1] / 255.0f; // G
                    floatData[2 * totalPixels + i] = matData[i * 3 + 0] / 255.0f; // B
                }

                // Create the input tensor in the required 1x3xHxW format.
                var inputTensor = new DenseTensor<float>(
                    floatData.AsMemory(0, totalPixels * 3),
                    new[] { 1, 3, _inputSize, _inputSize });
                var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor(_inputName, inputTensor) };

                // Run the inference session.
                using var results = _session.Run(inputs);
                var output = results.First().AsTensor<float>();

                // Process the raw model output into human-readable detection results.
                return Postprocess(output, originalWidth, originalHeight);
            }
            finally
            {
                // Return the rented array to the pool to prevent memory pressure.
                ArrayPool<float>.Shared.Return(floatData);
            }
        }

        /// <summary>
        /// Converts the raw model output tensor into a list of <see cref="YoloResult"/> objects,
        /// applying confidence filtering and coordinate conversion.
        /// </summary>
        /// <param name="output">The raw tensor output from the ONNX model.</param>
        /// <param name="originalWidth">The width of the original frame for scaling.</param>
        /// <param name="originalHeight">The height of the original frame for scaling.</param>
        /// <returns>A filtered list of detection results.</returns>
        private List<YoloResult> Postprocess(Tensor<float> output, int originalWidth, int originalHeight)
        {
            var finalResults = new List<YoloResult>();
            int numClasses = _labels.Count;

            // Total properties per detection: 4 (box coordinates: x, y, w, h) + numClasses (confidence scores).
            int propertiesPerBox = numClasses + 4;

            // Validate the output tensor dimensions.
            if (output.Dimensions.Length < 3 || output.Dimensions[1] != propertiesPerBox)
            {
                return finalResults;
            }

            int numBoxes = output.Dimensions[2];
            
            // Get a Span for efficient, contiguous memory access.
            var span = output.ToDenseTensor().Buffer.Span;

            for (int i = 0; i < numBoxes; i++)
            {
                float maxConfidence = 0;
                int bestClassIndex = -1;
                // Iterate through class scores to find the highest confidence.
                for (int j = 0; j < numClasses; j++)
                {
                    float currentConfidence = span[(4 + j) * numBoxes + i];
                    if (currentConfidence > maxConfidence)
                    {
                        maxConfidence = currentConfidence;
                        bestClassIndex = j;
                    }
                }

                // Filter by the minimum confidence threshold defined in ModelSettings.
                if (maxConfidence < ModelSettings.ConfidenceThreshold) continue;

                // Extract normalized box coordinates (center_x, center_y, width, height).
                float x = span[0 * numBoxes + i];
                float y = span[1 * numBoxes + i];
                float w = span[2 * numBoxes + i];
                float h = span[3 * numBoxes + i];

                finalResults.Add(new YoloResult
                {
                    Label = _labels[bestClassIndex],
                    Confidence = maxConfidence,

                    // Convert YOLO center-width-height format to the required top-left-width-height format.
                    Box = new RectangleF(x - w / 2, y - h / 2, w, h)
                });
            }

            // Apply Non-Maximum Suppression (NMS) to eliminate redundant, overlapping boxes.
            return Nms(finalResults, ModelSettings.IouThreshold);
        }

        /// <summary>
        /// Implements Non-Maximum Suppression (NMS) to filter redundant bounding boxes based on IoU overlap.
        /// </summary>
        /// <param name="boxes">The list of candidate detection results.</param>
        /// <param name="iouThreshold">The maximum acceptable IoU overlap before a box is suppressed.</param>
        /// <returns>A final, filtered list of detection results.</returns>
        private List<YoloResult> Nms(List<YoloResult> boxes, float iouThreshold)
        {
            var final = new List<YoloResult>();

            // Sort boxes by confidence score in descending order.
            var sorted = boxes.OrderByDescending(r => r.Confidence).ToList();

            while (sorted.Count > 0)
            {
                var current = sorted[0];

                final.Add(current);
                
                // Take the highest confidence box and remove it from the consideration list.
                sorted.RemoveAt(0);

                // Compare the current box against all remaining boxes.
                for (int i = sorted.Count - 1; i >= 0; i--)
                {
                    // If the overlap (IoU) is too high, remove the weaker box.
                    if (IoU(current.Box, sorted[i].Box) > iouThreshold)
                        sorted.RemoveAt(i);
                }
            }
            return final;
        }

        /// <summary>
        /// Calculates the Intersection over Union (IoU) ratio between two bounding boxes.
        /// </summary>
        /// <param name="a">The first bounding box.</param>
        /// <param name="b">The second bounding box.</param>
        /// <returns>The IoU value (0 to 1).</returns>
        private float IoU(RectangleF a, RectangleF b)
        {
            // Calculate coordinates of the intersection rectangle.
            float x1 = Math.Max(a.Left, b.Left);
            float y1 = Math.Max(a.Top, b.Top);
            float x2 = Math.Min(a.Right, b.Right);
            float y2 = Math.Min(a.Bottom, b.Bottom);

            // Calculate intersection width and height (clamped at 0).
            float interW = Math.Max(0, x2 - x1);
            float interH = Math.Max(0, y2 - y1);
            float interArea = interW * interH;

            // Calculate union area.
            float areaA = a.Width * a.Height;
            float areaB = b.Width * b.Height;

            // IoU = Intersection / Union. Avoid division by zero.
            float unionArea = areaA + areaB - interArea;

            return unionArea > 0 ? interArea / unionArea : 0;
        }

        /// <summary>
        /// Disposes of the inference session to free unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _session?.Dispose();
            // The GpuPreprocessor dependency is not disposed here,
            // as it's typically managed by a dependency injection container or calling class.
        }
    }
}
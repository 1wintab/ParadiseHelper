namespace ParadiseHelper.AI.Core.Settings
{
    /// <summary>
    /// Static class containing core configuration settings for the AI detection model (e.g., YOLO).
    /// These parameters define the expected input size and the required confidence thresholds.
    /// </summary>
    public static class ModelSettings
    {
        /// <summary>
        /// Gets or sets the expected width of the input image for the AI detection model (in pixels).
        /// This must match the training resolution of the model.
        /// </summary>
        public static int Width { get; set; } = 512;

        /// <summary>
        /// Gets or sets the expected height of the input image for the AI detection model (in pixels).
        /// This must match the training resolution of the model.
        /// </summary>
        public static int Height { get; set; } = 512;

        /// <summary>
        /// The minimum confidence score threshold. Any detection bounding box with a score 
        /// below this value will be immediately discarded as unreliable.
        /// </summary>
        public const float ConfidenceThreshold = 0.45f;

        /// <summary>
        /// The Intersection over Union (IoU) threshold used for Non-Maximum Suppression (NMS).
        /// This value helps filter out redundant or overlapping bounding boxes for the same object.
        /// </summary>
        public const float IouThreshold = 0.6f;
    }
}
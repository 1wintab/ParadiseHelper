namespace Data.Settings.LaunchParameters
{
    /// <summary>
    /// Defines the different operational modes for the application's launch parameters.
    /// </summary>
    public enum LaunchMode
    {
        /// <summary>
        /// Normal startup mode without special configurations.
        /// </summary>
        Default = 0, 
        
        /// <summary>
        /// Launch mode configured specifically for running the AI bot (e.g., in CS2).
        /// </summary>
        AICore = 1   
    }
}
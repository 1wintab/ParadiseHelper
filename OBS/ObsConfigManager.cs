using System;
using System.IO;
using System.Text.Json;
using ParadiseHelper.OBS;
using Core;

/// <summary>
/// Provides static methods for loading and saving the configuration parameters 
/// required to connect to OBS Studio (Open Broadcaster Software).
/// </summary>
public static class OBSConfigManager
{
    // The base directory path where the OBS configuration file is stored.
    private static readonly string OBS_CONFIG_DIR = FilePaths.Standard.OBS.OBSConfigDirectory;
    
    // The full path to the OBS connection configuration JSON file.
    private static readonly string CONFIG_FILE_PATH = Path.Combine(OBS_CONFIG_DIR, "obs_config.json");

    /// <summary>
    /// Loads the OBS connection parameters from the configuration file.
    /// </summary>
    /// <returns>
    /// The deserialized <see cref="OBSConnectionParams"/> object if the file exists and is valid; 
    /// otherwise, returns a new default <see cref="OBSConnectionParams"/> object.
    /// </returns>
    public static OBSConnectionParams Load()
    {
        if (!File.Exists(CONFIG_FILE_PATH))
        {
            return new OBSConnectionParams();
        }

        try
        {
            string jsonString = File.ReadAllText(CONFIG_FILE_PATH);

            // Deserialize the JSON string. If deserialization returns null, return a new default object.
            return JsonSerializer.Deserialize<OBSConnectionParams>(jsonString)
                ?? new OBSConnectionParams();
        }
        catch (Exception)
        {
            // In case of any file reading or JSON parsing error, return a default configuration
            // to prevent application crash.
            return new OBSConnectionParams();
        }
    }

    /// <summary>
    /// Saves the provided OBS connection parameters to the configuration file.
    /// The file is saved with indentation for readability.
    /// </summary>
    /// <param name="parameters">The <see cref="OBSConnectionParams"/> object containing the connection details to be saved.</param>
    public static void Save(OBSConnectionParams parameters)
    {
        try
        {
            // Ensure the configuration directory exists before attempting to write the file.
            Directory.CreateDirectory(OBS_CONFIG_DIR);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string jsonString = JsonSerializer.Serialize(parameters, options);

            File.WriteAllText(CONFIG_FILE_PATH, jsonString);
        }
        catch (Exception ex)
        {
            // Output an error message if saving the configuration fails.
            Console.WriteLine($"Error saving OBS configuration: {ex.Message}");
        }
    }
}
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using Core;

namespace ParadiseHelper.AI.Orientation.Map.Navmesh
{
    /// <summary>
    /// Static class for managing and analyzing navigation mesh data across different maps.
    /// Handles loading, storage, and utility operations related to navmeshes.
    /// </summary>
    public static class NavmeshCore
    {
        // Internal list to store all loaded Navmesh objects.
        private static List<Navmesh> _navmeshes = new List<Navmesh>();
        
        // List of clean map names corresponding to the loaded navmeshes.
        private static List<string> _navmeshNames = new List<string>();

        /// <summary>
        /// Static constructor. Automatically loads all navmesh files from the designated directory
        /// when the class is first accessed.
        /// </summary>
        static NavmeshCore()
        {
            // Check if the navmesh directory exists. If not, create it and exit.
            if (!Directory.Exists(FilePaths.AI.NavmeshDirectory))
            {
                Directory.CreateDirectory(FilePaths.AI.NavmeshDirectory);
                return;
            }

            // Find all JSON files matching the navmesh naming pattern.
            var navmeshFiles = Directory.GetFiles(FilePaths.AI.NavmeshDirectory, "*_map_navmesh.json");

            foreach (var navmeshFile in navmeshFiles)
            {
                // Attempt to load and deserialize the navmesh data.
                var navmesh = LoadNavmeshData(navmeshFile);

                // Validate the loaded navmesh before storing.
                if (navmesh != null && !navmesh.Empty())
                {
                    _navmeshes.Add(navmesh);
                    // Remove the standard file suffix to get the clean map name.
                    _navmeshNames.Add(Path.GetFileNameWithoutExtension(navmeshFile).Replace("_map_navmesh", ""));
                }
            }
        }

        /// <summary>
        /// Loads and deserializes a Navmesh object from a specified JSON file path.
        /// </summary>
        /// <param name="filePath">The full path to the navmesh JSON file.</param>
        /// <returns>The loaded <see cref="Navmesh"/> object, or null if loading fails.</returns>
        public static Navmesh LoadNavmeshData(string filePath)
        {
            try
            {
                // Read the entire JSON file content.
                string json = File.ReadAllText(filePath);

                // Deserialize the JSON string into a Navmesh object.
                // Assuming Newtonsoft.Json is available in the Core namespace or is imported.
                Navmesh navmeshData = Newtonsoft.Json.JsonConvert.DeserializeObject<Navmesh>(json);

                return navmeshData;
            }
            catch (Exception ex)
            {
                // Log the error during file reading or deserialization.
                Console.WriteLine($"Error reading file {filePath}: {ex.Message}");
                
                return null;
            }
        }

        /// <summary>
        /// Identifies and returns the IDs of all dead-end nodes (nodes with only one connection) 
        /// within the given navigation mesh.
        /// </summary>
        /// <param name="navmesh">The navmesh to analyze.</param>
        /// <returns>A <see cref="HashSet{T}"/> of node IDs that are dead ends.</returns>
        public static HashSet<int> FindDeadEndNodeIds(Navmesh navmesh)
        {
            // Check for invalid or empty navmesh input.
            if (navmesh == null || navmesh.Empty())
            {
                return new HashSet<int>();
            }

            // Dictionary to count the number of connections for each node ID.
            var nodeConnectionCounts = new Dictionary<int, int>();

            // Initialize connection counters for all nodes in the mesh to zero.
            // This ensures all nodes are present in the dictionary.
            foreach (var node in navmesh.Nodes)
            {
                // Assuming the Node class has an 'ID' property.
                nodeConnectionCounts[node.ID] = 0;
            }

            // Iterate through all edges and increment the connection count for the linked nodes.
            foreach (var edge in navmesh.Edges)
            {
                if (nodeConnectionCounts.ContainsKey(edge.Node1ID))
                {
                    nodeConnectionCounts[edge.Node1ID]++;
                }
                if (nodeConnectionCounts.ContainsKey(edge.Node2ID))
                {
                    nodeConnectionCounts[edge.Node2ID]++;
                }
            }

            // Use LINQ to find node IDs where the connection count is exactly 1 (a dead end).
            var deadEndNodeIds = nodeConnectionCounts
                .Where(pair => pair.Value == 1)
                .Select(pair => pair.Key)
                .ToHashSet(); // Convert the result to a HashSet for efficient lookups.

            return deadEndNodeIds;
        }

        /// <summary>
        /// Finds the nearest navigational node to a given map coordinate.
        /// Performs a validation check against the map's bounding box dimensions.
        /// </summary>
        /// <param name="navmeshCoordinates">The map coordinates (X, Y) to check against the navmesh nodes.</param>
        /// <param name="currentNavmesh">The active navmesh.</param>
        /// <param name="currentMap">The current map detection result (used for dimension validation).</param>
        /// <returns>The nearest <see cref="Node"/> object, or null if no nodes are found or dimensions mismatch.</returns>
        public static Node FindNearestNode(PointF navmeshCoordinates, Navmesh currentNavmesh, MapDetectionResult currentMap)
        {
            // Basic validation for existence of navmesh and nodes.
            if (currentNavmesh == null || currentNavmesh.Empty() || currentNavmesh.Nodes.Count == 0) return null;

            // Validate that the loaded navmesh dimensions match the detected map's bounding box dimensions.
            if (currentNavmesh.MapWidth != currentMap.BoundingBox.Width || currentNavmesh.MapHeight != currentMap.BoundingBox.Height) { return null; }

            float minDistance = float.MaxValue;
            Node nearestNode = null;

            // Simple linear search through all nodes to find the closest one.
            foreach (var node in currentNavmesh.Nodes)
            {
                // Calculate the squared difference in X and Y coordinates.
                double diffX_Squared = (node.X - navmeshCoordinates.X) * (node.X - navmeshCoordinates.X);
                double diffY_Squared = (node.Y - navmeshCoordinates.Y) * (node.Y - navmeshCoordinates.Y);

                // Calculate the Euclidean distance (the square root of the sum of squared differences).
                float distance = (float)Math.Sqrt(diffX_Squared + diffY_Squared);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestNode = node;
                }
            }

            return nearestNode;
        }

        /// <summary>
        /// Retrieves a loaded Navmesh object by its map name.
        /// </summary>
        /// <param name="mapName">The clean name of the map (e.g., "Dust2").</param>
        /// <returns>The matching <see cref="Navmesh"/> object, or null if not found.</returns>
        public static Navmesh FindNavmesh(string mapName)
        {
            // Simple LINQ query to find the first navmesh matching the given map name.
            return _navmeshes.FirstOrDefault(n => n.MapName == mapName);
        }
    }
}
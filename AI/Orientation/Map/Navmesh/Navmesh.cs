using System.Collections.Generic;

namespace ParadiseHelper.AI.Orientation.Map.Navmesh
{
    /// <summary>
    /// Represents a complete navigation mesh for a single map, including nodes (locations) and edges (connections).
    /// </summary>
    public class Navmesh
    {
        /// <summary>
        /// Gets or sets the list of all navigational nodes (key points/locations) in the mesh.
        /// </summary>
        public List<Node> Nodes { get; set; }

        /// <summary>
        /// Gets or sets the list of all edges (connections) that link the nodes in the mesh.
        /// </summary>
        public List<Edge> Edges { get; set; }

        /// <summary>
        /// Gets or sets the expected width of the map image (in pixels) this navmesh corresponds to.
        /// Uses a nullable type to indicate if the dimension is not yet set.
        /// </summary>
        public int? MapWidth { get; set; }

        /// <summary>
        /// Gets or sets the expected height of the map image (in pixels) this navmesh corresponds to.
        /// Uses a nullable type to indicate if the dimension is not yet set.
        /// </summary>
        public int? MapHeight { get; set; }

        /// <summary>
        /// Gets or sets the name of the map this navigation mesh belongs to (e.g., "Dust2").
        /// </summary>
        public string MapName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Navmesh"/> class,
        /// ensuring the collections are initialized and dimensions are unset.
        /// </summary>
        public Navmesh()
        {
            Nodes = new List<Node>();
            Edges = new List<Edge>();
            MapWidth = null;
            MapHeight = null;
            MapName = null;
        }

        /// <summary>
        /// Checks if the navmesh is considered empty or invalid (missing core components or metadata).
        /// </summary>
        /// <returns>True if the mesh has no nodes/edges or is missing map dimensions/name; otherwise, false.</returns>
        public bool Empty()
        {
            return Nodes.Count == 0
                || Edges.Count == 0
                || MapWidth is null
                || MapHeight is null
                || string.IsNullOrEmpty(MapName);
        }
    }
}
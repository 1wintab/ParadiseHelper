namespace ParadiseHelper.AI.Orientation.Map.Navmesh
{
    /// <summary>
    /// Represents a connection (edge) between two navigational nodes in the navmesh.
    /// This is used to define pathways for movement between map locations.
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// Gets or sets the unique ID of the first node connected by this edge.
        /// </summary>
        public int Node1ID { get; set; }

        /// <summary>
        /// Gets or sets the unique ID of the second node connected by this edge.
        /// </summary>
        public int Node2ID { get; set; }
    }
}
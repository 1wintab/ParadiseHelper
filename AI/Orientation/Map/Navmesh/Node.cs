using System.Drawing;

namespace ParadiseHelper.AI.Orientation.Map.Navmesh
{
    /// <summary>
    /// Represents a single navigation point (node) in a navigation mesh.
    /// Each node defines a traversable location on the map.
    /// </summary>
    public class Node
    {
        // Static counter for automatically assigning unique IDs to new nodes if created in code.
        private static int nextId = 0;

        /// <summary>
        /// Gets or sets the X-coordinate of the node's position (typically map coordinates).
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Gets or sets the Y-coordinate of the node's position (typically map coordinates).
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Gets or sets the unique integer identifier for the node.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the coordinates using the <see cref="PointF"/> structure.
        /// This is a convenience wrapper for setting/getting the <see cref="X"/> and <see cref="Y"/> properties simultaneously.
        /// </summary>
        public PointF Position
        {
            get { return new PointF(X, Y); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class and automatically assigns a new unique ID.
        /// </summary>
        public Node()
        {
            ID = nextId++;
        }
    }
}
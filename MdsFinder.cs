using System.Collections.Generic;
using System.Linq;

namespace MinimumDominatingSet
{
    /// <summary>
    /// Represents a node in the graph.
    /// </summary>
    public class GraphNode
    {
        /// <summary>
        /// Index to the ParentList array
        /// </summary>
        public int VertexIndex { get; }
        /// <summary>
        /// Reference to the parent of this node (if any). Null if a root.
        /// </summary>
        public GraphNode Parent { get; set; }
        /// <summary>
        /// List of all of the children of this particular node in the graph.
        /// </summary>
        public ICollection<GraphNode> Children { get; set; }
        /// <summary>
        /// Whether this node is considered a "leaf" node (it has no children).
        /// </summary>
        public bool IsLeaf => !Children.Any();
        /// <summary>
        /// Whether this node is considered a young parent or not (all of its children are leaves).
        /// </summary>
        public bool IsYoungParent => !IsLeaf && Children.All(c => c.IsLeaf);
        /// <summary>
        /// Whether this node is considered a stranger (a root with no parents).
        /// </summary>
        public bool IsStranger => Parent == null && IsLeaf;

        /// <summary>
        /// Whether this node has now been covered, e.g. as a result of this
        /// node being the parent of a young parent that was added to the
        /// dominating set.
        /// </summary>
        public bool IsCovered { get; set; }

        /// <summary>
        /// Constructor, instantiates a new node instance.
        /// </summary>
        /// <param name="vertexIndex">The zero-indexed number assigned to the node.</param>
        public GraphNode(int vertexIndex)
        {
            VertexIndex = vertexIndex;
            Children = new List<GraphNode>();
        }

        /// <summary>
        /// Get a string representation of this node, its properties and its children.
        /// </summary>
        /// <returns>String representation of the object.</returns>
        public override string ToString()
        {
            var childrenIds = "{" + string.Join(", ", Children.Select(c => c.VertexIndex + 1)) + "}";
            return string.Format("Zero-based index: {0}, IsLeaf: {1}, IsYoungParent: {2}, IsStranger: {3}, Children: {4}",
            VertexIndex, IsLeaf, IsYoungParent, IsStranger, childrenIds);
        }
    }

    /// <summary>
    /// Class that implements an algorithm for finding the mimimum cardinality
    /// dominaing set for a given *tree* (arbitrary graphs are not supported).
    /// </summary>
    public class MdsFinder
    {
        private int?[] ParentList { get; }
        private Dictionary<int, GraphNode> NodeMap { get; }

        /// <summary>
        /// Instantiates the MdsFinder class which takes an array
        /// of parent node references (as defined in the exercise sheet)
        /// and returns an array of the vertices that belong to the found
        /// minimum dominating set via the ComputeSet method.
        /// </summary>
        /// <param name="parentList">
        /// Array of vertex number (starting from zero) to
        /// parent vertex index (or null if a tree root).
        /// </param>
        public MdsFinder(int?[] parentList)
        {
            ParentList = parentList;
            NodeMap = new Dictionary<int, GraphNode>();
            PerformPreprocessing();
        }

        /// <summary>
        /// Gets a minimum-size dominating set. Should execute in linear time in number of edges
        /// (upper bound n^2 where n = number of nodes).
        /// </summary>
        /// <returns>A (hopefully) minimum-size dominating set.</returns>
        public int[] ComputeSet()
        {
            // Variable to hold the current minimum dominating set
            var workingDominatingSet = new List<int>();

            // All nodes currently in the graph:
            var nodesInGraph = NodeMap.Values;

            // First things first, add all of the strangers to the dominating set and then
            // delete them from the graph.
            var strangers = nodesInGraph.Where(s => s.IsStranger).ToList();
            workingDominatingSet.AddRange(strangers.Select(s => s.VertexIndex));
            DeleteRangeFromGraph(strangers);

            // Now, whilst the graph has > 1 node, retrieve a young parent and add it to the set.
            // Delete the young parent and its children, and repeat. Includes extra handling
            // for the parents of young parents (see inline explanation).
            while (nodesInGraph.Count > 1)
            {
                var nextYp = nodesInGraph.FirstOrDefault(n => n.IsYoungParent); // should never be null
                workingDominatingSet.Add(nextYp.VertexIndex);

                // Now, we'll mark the parent (if there is one) of the YP that we just removed as covered
                if (nextYp.Parent != null)
                {
                    nextYp.Parent.IsCovered = true;
                }

                // Remove the young parent and its children from the graph
                DeleteFromGraph(nextYp);

                // ---------- // // ---------- // // ---------- // // ---------- // // ---------- // // ---------- //

                // Find all covered nodes which are also leaves and remove them. Many thanks to John Haslegrave
                // and David Purser for identifying and explaining this issue with the original algorithm.

                // This handles the edge case where we have a graph like this:
                /*
                    [1]--[2]--[3]--[4]--[5]--[6]
                */
                // By inspection, an MDS is {2,5} which is of size two. Let's try and apply the algorithm
                // *without* the marking system:
                /*
                    [1]--[2]--[3]--[4]--{5}--[6]        Identifies {5} as YP, adds to set and deletes
                    [1]--[2]--{3}--[4]                  Identifies {3} as YP, adds to set and deletes
                    {1}--[2]                            Identifies {1} as YP, adds to set and deletes

                    Final "minimum" dominating set given is {1,3,5} which clearly isn't right!
                */

                // Let's try it again with the IsCovered marker system:

                /*
                    [1]--[2]--[3]--[4]--{5}--[6]        Identifies {5} as YP, adds to set and deletes
                    [1]--[2]--[3]--<4>                  {4} is marked as covered and removed as it's a leaf
                    [1]--{2}--[3]                       Identifies {2} as YP, adds to set and deletes
                    <1>                                 {1} is marked as covered and removed as it's a leaf

                    Final "minimum" dominating set given is {2,5}. Much better!
                */

                DeleteRangeFromGraph(nodesInGraph.Where(n => n.IsLeaf && n.IsCovered).ToList());
            }

            // Termination: Every tree with 2 or more vertices includes a young parent,
            // so the algorithm should always get down to one node.

            // Finally, we turn the (variable size) list into a fixed array to return...
            return workingDominatingSet.ToArray();
        }

        /// <summary>
        /// Preprocesses the array described in the exercise sheet
        /// to get an dictionary, OOP representation of the tree nodes.
        /// </summary>
        private void PerformPreprocessing()
        {
            // Temporary dictionary to store parentId => List of children
            var tempParentToChildListMap = new Dictionary<int, ICollection<GraphNode>>();
            for (var i = 0; i < ParentList.Length; i++)
            {
                // Create a new object instance
                var obj = new GraphNode(i);

                if (ParentList[i] != null) // ensure the node does have a parent
                {
                    // Then add *this* node to *the parent's* list of children
                    InitializeOrAddToMap(tempParentToChildListMap, (int) ParentList[i], obj);
                }
                NodeMap[i] = obj;
            }

            // Loop over all nodes which have children and set-up the Children list.
            // Then, loop over the children and set the Parent reference appropriately.
            foreach (var node in NodeMap.Values.Where(n => tempParentToChildListMap.ContainsKey(n.VertexIndex)))
            {
                node.Children = tempParentToChildListMap[node.VertexIndex];
                foreach (var child in node.Children)
                {
                    child.Parent = node;
                }
            }
        }

        /// <summary>
        /// Takes a dictionary, key and node. If the key exists, add the node to the end.
        /// Otherwise, initialize a new list with the node in and store it at the key.
        /// </summary>
        /// <param name="tempParentMap">The dictionary.</param>
        /// <param name="parent">The key for the dictionary.</param>
        /// <param name="graphNode">The node to append.</param>
        private void InitializeOrAddToMap(IDictionary<int, ICollection<GraphNode>> tempParentMap, int parent, GraphNode graphNode)
        {
            // If there's already a List instance, great! Let's add to it
            if (tempParentMap.ContainsKey(parent))
            {
                tempParentMap[parent].Add(graphNode);
                return;
            }
            // If not, we need to instantiate a new one with our node in.
            tempParentMap[parent] = new List<GraphNode> {graphNode};
        }

        /// <summary>
        /// Removes a node and all of its parents
        /// </summary>
        /// <param name="node">The node to remove.</param>
        private void DeleteFromGraph(GraphNode node)
        {
            node.Parent?.Children.Remove(node);
            NodeMap.Remove(node.VertexIndex);
            DeleteRangeFromGraph(new List<GraphNode>(node.Children)); // recursive call
        }

        /// <summary>
        /// Wraps DeleteFromGraph to delete multiple nodes.
        /// </summary>
        /// <param name="range">The nodes to remove.</param>
        private void DeleteRangeFromGraph(IEnumerable<GraphNode> range)
        {
            foreach (var node in range)
            {
                DeleteFromGraph(node);
            }
        }
    }
}
using Day02_AStar.Grid;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine;

namespace Day02_AStar.Pathfinding
{
    public class Pathfinder : MonoBehaviour
    {
        public GridManager gridManager;

        [Header("Start & Goal")]
        public Transform startMarker;
        public Transform goalMarker;

        [Header("Materials")]
        public Material pathMaterial;
        public Material openMaterial;
        public Material closedMaterial;

        private List<Node> lastPath; // Store the last computed path

        private InputAction pathfindAction;

        private void RunPathfinding()
        {
            if (gridManager == null || startMarker == null || goalMarker == null)
            {
                Debug.LogWarning("Pathfindinder: missing references.");
                return;
            }

            // Get nodes for start and goal
            Node startNode = gridManager.GetNodeFromWorldPosition(startMarker.position);
            Node goalNode = gridManager.GetNodeFromWorldPosition(goalMarker.position);

            if (startNode == null || goalNode == null)
            {
                Debug.LogWarning("Invalid start or goal node.");
                return;
            }

            // Reset color colours to walkable / wall first
            ResetGridVisuals();

            // Run A*
            // open and closed sets for visualization
            // open represents nodes to be evaluated for path
            // closed represents nodes already evaluated
            HashSet<Node> openSetVisual = new HashSet<Node>(); // 
            HashSet<Node> closedSetVisual = new HashSet<Node>();

            lastPath = FindPath(startNode, goalNode, openSetVisual, closedSetVisual);

            // Colour open closed and closed sets
            foreach (var node in openSetVisual)
            {
                if (node.Walkable)
                {
                    SetTileMaterialSafe(node, openMaterial);
                }
            }

            foreach (var node in closedSetVisual)
            {
                if (node.Walkable)
                {
                    SetTileMaterialSafe(node, closedMaterial);
                }
            }

            // Color the final path
            if (lastPath != null)
            {
                foreach (var node in lastPath)
                {
                    SetTileMaterialSafe(node, pathMaterial);
                }
            }
            else
            {
                Debug.Log("No path found.");
            }

            // Color start and goal
            SetTileMaterialSafe(startNode, pathMaterial);
            SetTileMaterialSafe(goalNode, pathMaterial);

        }

        private void ResetGridVisuals()
        {
            for (int x = 0; x < gridManager.width; x++)
            {
                for (int y = 0; y < gridManager.height; y++)
                {
                    Node node = gridManager.GetNode(x, y);
                    if (node.Walkable)
                    {
                        SetTileMaterialSafe(node, gridManager.walkableMaterial);
                    }
                    else
                    {
                        SetTileMaterialSafe(node, gridManager.wallMaterial);
                    }
                }
            }
        }

        private void SetTileMaterialSafe(Node node, Material mat)
        {
            var renderer = node.Tile.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null)
            {
                renderer.material = mat;
            }
        }

        // A* core implementation
        // Future optimization: could include using a priority queue for the open set
        public List<Node> FindPath(Node startNode, Node goalNode, HashSet<Node> openVisual = null, HashSet<Node> closedVisual = null)
        {
            // Reset Node costs
            // We do this since we may run multiple pathfinding operations
            gridManager.ResetAllNodes();

            List<Node> openSet = new List<Node>(); // Nodes to be evaluated
            HashSet<Node> closedSet = new HashSet<Node>(); // Nodes already evaluated

            startNode.GCost = 0f; // Cost so far to reach start node is zero initially
            startNode.HCost = HeuristicCost(startNode, goalNode); // First guess, from start to goal
            openSet.Add(startNode); // Add start node to open set, we will evaluate it first
            openVisual?.Add(startNode);

            while (openSet.Count > 0)
            {
                // Get node in open set with lowest F cost
                Node current = GetLowestFCostNode(openSet);

                if (current == goalNode)
                {
                    // Found our goal node
                    // When we reach here, we can reconstruct the path
                    // by following parent nodes from goal to start
                    return ReconstructPath(startNode, goalNode);
                }

                openSet.Remove(current);
                closedSet.Add(current); // Mark current node as evaluated
                closedVisual?.Add(current);

                // Explore neighbours to see if we can find a better path
                foreach (Node neighbour in gridManager.GetNeighbours(current))
                {
                    if (neighbour == null || !neighbour.Walkable)
                        // Skip non-walkable or null neighbours
                        continue;
                    if (closedSet.Contains(neighbour))
                        // Already evaluated
                        continue;

                    // cost(current, neighbour) = 1. Unweighted grid.
                    // Tentative = cost so far to reach neighbour
                    float tentativeG = current.GCost + 1f;

                    if (tentativeG < neighbour.GCost)
                    {
                        // Found a better path to neighbour
                        neighbour.Parent = current;
                        neighbour.GCost = tentativeG; // Update cost to reach neighbour
                        neighbour.HCost = HeuristicCost(neighbour, goalNode);

                        if (!openSet.Contains(neighbour))
                        {
                            openSet.Add(neighbour);
                            openVisual?.Add(neighbour);
                        }
                    }
                }
            }

            // No path found
            return null;
        }

        private Node GetLowestFCostNode(List<Node> openSet)
        {
            Node best = openSet[0]; // Assume first node is best initially
            for (int i = 1; i < openSet.Count; i++)
            {
                // For each node, check if its F cost is lower than the best found so far
                Node candidate = openSet[i];
                if (candidate.FCost < best.FCost ||
                    Mathf.Approximately(candidate.FCost, best.FCost) && candidate.HCost < best.HCost)
                {
                    best = candidate;
                }
            }
            return best;
        }

        private float HeuristicCost(Node a, Node b)
        {
            // Using Manhattan distance as heuristic for grid-based pathfinding
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            return dx + dy;
        }

        // Used to reconstruct the path from start to goal by following parent nodes
        private List<Node> ReconstructPath(Node startNode, Node goalNode)
        {
            List<Node> path = new List<Node>();
            Node current = goalNode;

            while (current != null)
            {
                path.Add(current);
                if (current == startNode)
                    break;
                current = current.Parent;
            }

            path.Reverse(); // Reverse to get path from start to goal
            return path;
        }

        private void OnEnable()
        {
            pathfindAction = new InputAction(
                name: "Pathfind",
                type: InputActionType.Button,
                binding: "<Keyboard>/space"
            );

            pathfindAction.performed += OnPathfindPerformed;
            pathfindAction.Enable();
        }

        private void OnDisable()
        {
            if (pathfindAction != null)
            {
                pathfindAction.performed -= OnPathfindPerformed;
                pathfindAction.Disable();
            }
        }

        private void OnPathfindPerformed(InputAction.CallbackContext ctx)
        {
            RunPathfinding();
        }
    }
}

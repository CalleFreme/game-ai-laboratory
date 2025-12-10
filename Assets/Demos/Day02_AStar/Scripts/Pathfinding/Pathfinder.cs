using Day02_AStar.Grid;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private List<Node> lastPath;

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
        HashSet<Node> openSetVisual = new HashSet<Node>();
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
        return;
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
    public List<Node> FindPath(Node startNode, Node goalNode, HashSet<Node> openVisual = null, HashSet<Node> closedVisual = null)
    {
        // Reset Node costs
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Node n = gridManager.GetNode(x, y);
                n.GCost = float.PositiveInfinity;
                n.HCost = 0f;
                n.Parent = null;
            }
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.GCost = 0f;
        startNode.HCost = HeuristicCost(startNode, goalNode);
        openSet.Add(startNode);
        openVisual?.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = GetLowestFCostNode(openSet);

            if (current == goalNode)
            {
                // Found our goal node
                return ReconstructPath(startNode, goalNode);
            }

            openSet.Remove(current);
            closedSet.Add(current);
            closedVisual?.Add(current);

            foreach (Node neighbour in gridManager.GetNeighbours(current))
            {
                if (neighbour == null || !neighbour.Walkable)
                    continue;
                if (closedSet.Contains(neighbour))
                    continue;

                // cost(current, neighbour) = 1
                float tentativeG = current.GCost + 1f;

                if (tentativeG < neighbour.GCost)
                {
                    neighbour.Parent = current;
                    neighbour.GCost = tentativeG; // 
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
/*    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RunPathfinding();
        }
    }*/

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
        if ( pathfindAction != null )
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

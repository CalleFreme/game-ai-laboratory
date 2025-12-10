using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class Node
{
    public int X { get; }
    public int Y { get; }

    public bool Walkable { get; private set; }
    public GameObject Tile { get; }

    public float GCost { get; set; }
    public float HCost { get; set; }
    public Node Parent { get; set; }

    public float FCost => GCost + HCost;

    public Node(int x, int y, bool walkable, GameObject tile)
    {
        X = x;
        Y = y;
        Walkable = walkable;
        Tile = tile;
        ResetCosts();
    }

    public void SetWalkable(bool walkable)
    {
        Walkable = walkable;
    }

    public void ResetCosts()
    {
        GCost = float.PositiveInfinity; // Infinitely high cost initially
        HCost = 0f; // No heuristic cost initially
        Parent = null;
    }
}

namespace Day02_AStar.Grid
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int width = 10;
        public int height = 10;
        public float cellSize = 1f;

        [Header("Prefabs & Materials")]
        public GameObject tilePrefab;
        public Material walkableMaterial;
        public Material wallMaterial;

        [Header("Input")]
        [SerializeField]
        private Camera inputCamera;
        [SerializeField]
        private Transform tilesRoot; //

        private Node[,] nodes;
        private Dictionary<GameObject, Node> tileToNode = new Dictionary<GameObject, Node>();
        private InputAction clickAction;

        private void Awake()
        {
            if (tilesRoot == null)
            {
                // This creates an empty GameObject to hold the tiles as children
                // Helps keep the hierarchy clean
                tilesRoot = new GameObject("TilesRoot").transform;
                tilesRoot.parent = this.transform;
            }

            if (inputCamera == null)
            {
                // The camera from which we will cast rays on mouse clicks
                inputCamera = Camera.main;
            }

            GenerateGrid();
        }

        private void GenerateGrid()
        {
            nodes = new Node[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // World position for this tile
                    Vector3 worldPos = new Vector3(x * cellSize, 0f, y * cellSize);

                    GameObject tileGO = Instantiate(tilePrefab, worldPos, Quaternion.identity, this.transform);
                    tileGO.name = $"Tile_{x}_{y}";

                    // Create node
                    Node node = new Node(x, y, true, tileGO);
                    nodes[x, y] = node;
                    tileToNode[tileGO] = node;

                    // Set initial colour
                    SetTileMaterial(node, walkableMaterial);
                }
            }
        }

        public bool IsWithinBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public void ResetAllNodes()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    nodes[x, y].ResetCosts();
                }
            }
        }

        public bool TryGetNode(int x, int y, out Node node)
        {
            if (IsWithinBounds(x, y))
            {
                node = nodes[x, y];
                return true;
            }

            node = null;
            return false;
        }

        public Node GetNode(int x, int y)
        {
            return TryGetNode(x, y, out Node node) ? node : null;
        }

        public void SetWalkable(Node node, bool walkable)
        {
            if (node == null)
            {
                return;
            }

            node.SetWalkable(walkable);
            SetTileMaterial(node, walkable ? walkableMaterial : wallMaterial);
        }

        private void SetTileMaterial(Node node, Material mat)
        {
            if (node?.Tile == null)
            {
                return;
            }

            if (mat == null)
            {
                Debug.LogWarning("Missing material reference for tile update.", this);
                return;
            }

            // Try to get MeshRenderer and set material
            if (node.Tile.TryGetComponent(out MeshRenderer renderer))
            {
                renderer.material = mat;
            }
        }

        public IEnumerable<Node> GetNeighbours(Node node, bool allowDiagonals = false)
        {
            // We return an enumerable of nodes, so we can use yield return
            // This makes it easy to iterate over the neighbours without creating a temporary list
            if (node == null)
            {
                yield break; // yield break exits the iterator
            }
            int x = node.X;
            int y = node.Y;

            // 4-neighbour
            Node right = GetNode(x + 1, y);
            Node left = GetNode(x - 1, y);
            Node up = GetNode(x, y + 1);
            Node down = GetNode(x, y - 1);

            if (right != null) yield return right;
            if (left != null) yield return left;
            if (up != null) yield return up;
            if (down != null) yield return down;

            if (allowDiagonals)
            {
                Node diagRightUp = GetNode(x + 1, y + 1);
                Node diagLeftUp = GetNode(x - 1, y + 1);
                Node diagRightDown = GetNode(x + 1, y - 1);
                Node diagLeftDown = GetNode(x - 1, y - 1);

                // Diagonals should have cost 1.4f (approx sqrt(2)) in pathfinding calculations
                if (diagRightUp != null) yield return diagRightUp;
                if (diagLeftUp != null) yield return diagLeftUp;
                if (diagRightDown != null) yield return diagRightDown;
                if (diagLeftDown != null) yield return diagLeftDown;
            }
        }

        // Helper to get node from world position (for selecting start/goal)
        public Node GetNodeFromWorldPosition(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int y = Mathf.RoundToInt(worldPos.z / cellSize); // Should be z, because y is up axis in Unity 3D
            return GetNode(x, y);
        }

        // For clicking tiles: get node from tile GO
        public Node GetNodeFromTile(GameObject tileGO)
        {
            if (tileGO == null)
            {
                return null;
            }

            // Try to get node from dictionary
            return tileToNode.TryGetValue(tileGO, out var node) ? node : null;
        }

        private void OnEnable()
        {
            // Create an input action that fires when left mouse buttons is pressed
            clickAction = new InputAction(
                name: "Click",
                type: InputActionType.Button,
                binding: "<Mouse>/leftButton"
            );

            clickAction.performed -= OnClickPerformed; // Unsubscribe first to avoid multiple subscriptions
            clickAction.performed += OnClickPerformed; // Subscribe to event
            clickAction.Enable();
        }

        private void OnDisable()
        {
            // Called when the object is disabled, i.e. removed from the scene or deactivated
            if (clickAction != null)
            {
                clickAction.performed -= OnClickPerformed; // Unsubscribe from event
                clickAction.Disable();
            }
        }

        private void OnDestroy()
        {
            if (clickAction == null)
            {
                return;
            }

            clickAction.Dispose(); // This frees up resources used by the action
            clickAction = null;
        }

        private void OnClickPerformed(InputAction.CallbackContext ctx)
        {
            HandleMouseClick();
        }

        private void HandleMouseClick()
        {
            if (Mouse.current == null)
            {
                return;
            }
            
            Camera cameraToUse = inputCamera != null ? inputCamera : Camera.main;
            if (cameraToUse == null)
            {
                Debug.LogWarning("No camera available for grid clicks.", this);
                return;
            }

            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Ray ray = cameraToUse.ScreenPointToRay(new Vector3(mousePosition.x, mousePosition.y, 0f));
            if (!Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                return; // No hit
            }

            Node node = GetNodeFromTile(hitInfo.collider.gameObject);
            if (node == null)
            {
                return;
            }

            SetWalkable(node, !node.Walkable);
        }
    }

}

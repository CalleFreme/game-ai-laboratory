using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem; // NEW

public class Node
{
    public int x;
    public int y;

    public bool walkable;
    public GameObject tile;

    public float gCost;
    public float hCost;
    public Node parent;

    public float fCost => gCost + hCost;

    public Node(int x, int y, bool walkable, GameObject tile)
    {
        this.x = x;
        this.y = y;
        this.walkable = walkable;
        this.tile = tile;
        gCost = float.PositiveInfinity;
        hCost = 0f;
        parent = null;
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

        private Node[,] nodes;

        private Dictionary<GameObject, Node> tileToNode = new Dictionary<GameObject, Node>();

        private InputAction clickAction;

        private void Awake()
        {
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

        public Node GetNode(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return null;
            return nodes[x, y];
        }

        public void SetWalkable(Node node, bool walkable)
        {
            node.walkable = walkable;
            SetTileMaterial(node, walkable ? walkableMaterial : wallMaterial);
        }

        private void SetTileMaterial(Node node, Material mat)
        {
            var renderer = node.tile.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material = mat;
            }
        }

        public IEnumerable<Node> GetNeighbours(Node node, bool allowDiagonals = false)
        {
            int x = node.x;
            int y = node.y;

            // 4-neighbour
            yield return GetNode(x + 1, y);
            yield return GetNode(x - 1, y);
            yield return GetNode(x, y + 1);
            yield return GetNode(x, y - 1);

            if (allowDiagonals)
            {
                yield return GetNode(x + 1, y + 1);
                yield return GetNode(x - 1, y + 1);
                yield return GetNode(x + 1, y - 1);
                yield return GetNode(x - 1, y - 1);
            }
        }

        // Helper to get node from world position (for selecting start/goal)
        public Node GetNodeFromWorldPosition(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / cellSize);
            int y = Mathf.RoundToInt(worldPos.y / cellSize);
            return GetNode(x, y);
        }

        // For clicking tiles: get node from tile GO
        public Node GetNodeFromTile(GameObject tileGO)
        {
            if (tileToNode.TryGetValue(tileGO, out var node))
            {
                return node;
            }
            return null;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
        }

        private void OnEnable()
        {
            // Create an input action that fires when left mouse buttons is pressed
            clickAction = new InputAction(
                name: "Click",
                type: InputActionType.Button,
                binding: "<Mouse>/leftButton"
            );

            clickAction.performed += OnClickPerformed;
            clickAction.Enable();
        }

        private void OnDisable()
        {
            if (clickAction != null)
            {
                clickAction.performed -= OnClickPerformed;
                clickAction.Disable();
            }
        }

        private void OnClickPerformed(InputAction.CallbackContext ctx)
        {
            HandleMouseClick();
        }

        private void HandleMouseClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                GameObject clicked = hitInfo.collider.gameObject;
                Node node = GetNodeFromTile(clicked);
                if (node != null)
                {
                    bool newWalkable = !node.walkable;
                    SetWalkable(node, newWalkable);
                }
            }
        }
    }

}

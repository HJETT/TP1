using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GenerateExterior(10, 10);
        GenerateFloor(10, 10);
        GenerateCeiling(10, 10);

        Generate(10, 10);
        GenerateDoors(10, 10);
        GenerateLevers(10, 10);
    }

    #region Walls

    [Header("Walls")]
    public WeightedItem[] wallVariants;
    public float wallWidth;
    public Transform wallParent;
    public GameObject exitDoorPrefab;

    private void GenerateExterior(int width, int height)
    {
        // South
        for (int i = 0; i < width; i++)
        {
            var wall = Instantiate(GetRandomExterior(), wallParent);
            wall.transform.localPosition = new Vector3(0, 0, i + 1) * wallWidth;
            wall.transform.localRotation = Quaternion.Euler(0, 180, 0);
            wall.name += " (South)";
        }

        // North
        for (int i = 0; i < width; i++)
        {
            var wall = Instantiate(GetRandomExterior(), wallParent);
            wall.transform.localPosition = new Vector3(height, 0, i) * wallWidth;
            wall.name += " (North)";
        }

        // East
        for (int i = 0; i < width; i++)
        {
            var wall = Instantiate(GetRandomExterior(), wallParent);
            wall.transform.localPosition = new Vector3(i, 0, 0) * wallWidth;
            wall.transform.localRotation = Quaternion.Euler(0, 90, 0);
            wall.name += " (East)";
        }

        int rndIndex = Random.Range(0, width);
        // West
        for (int i = 0; i < width; i++)
        {
            GameObject prefab = i == rndIndex ? exitDoorPrefab : GetRandomExterior();
            var wall = Instantiate(prefab, wallParent);
            wall.transform.localPosition = new Vector3(i + 1, 0, width) * wallWidth;
            wall.transform.localRotation = Quaternion.Euler(0, -90, 0);

            if (i == rndIndex)
            {
                GameManager.Instance.exitDoorScript = wall.GetComponent<ExitDoorScript>();
                wall.name += " (Exit)";
            }
            else
                wall.name += " (West)";
        }
    }
    
    private GameObject GetRandomExterior() => GetRandom(wallVariants);

    #endregion

    #region Floor

    [Header("Floor")]
    public WeightedItem[] floorVariants;
    public float floorSize;
    public Transform floorParent;

    private void GenerateFloor(int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var floor = Instantiate(GetRandomFloor(), floorParent);
                floor.transform.localPosition = new Vector3(x, 0, y) * floorSize;
                floor.name += $" ({x};{y})";
            }
        }
    }

    private GameObject GetRandomFloor() => GetRandom(floorVariants);

    #endregion

    #region Ceiling

    [Header("Ceiling")]
    public WeightedItem[] ceilingVariants;
    public float ceilingSize;
    public float yOffset;
    public Transform ceilingParent;

    private void GenerateCeiling(int width, int height)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var ceiling = Instantiate(GetRandomCeiling(), ceilingParent);
                ceiling.transform.localPosition = new Vector3(x, 0, y) * floorSize + new Vector3(0, yOffset, 0);
                ceiling.name += $" ({x};{y})";
            }
        }
    }

    private GameObject GetRandomCeiling() => GetRandom(ceilingVariants);

    #endregion

    #region Random

    [System.Serializable]
    public class WeightedItem
    {
        public GameObject prefab;
        public float weight;
    }

    private GameObject GetRandom(WeightedItem[] items)
    {
        var total = items.Sum(i => i.weight);
        var rdm = Random.Range(0, total);

        foreach (var item in items)
        {
            if (item.weight >= rdm)
                return item.prefab;

            rdm -= item.weight;
        }

        return items[^1].prefab;
    }

    #endregion

    #region Door

    [Header("Doors")]
    public WeightedItem[] openedDoorsPrefab;
    public WeightedItem[] closedDoorsPrefab;
    public Transform doorParent;

    private List<GameObject> doors = new();

    private void GenerateDoors(int width, int height)
    {
        // Destroy all previous doors
        foreach (var item in doors)
            Destroy(item);

        // Set maze
        foreach (var cell in _cells)
        {
            // Vertical doors
            if (cell.X < width - 1)
            {
                GameObject prefab = cell.State.HasFlag(CellState.Right)
                    ? GetClosedDoor()
                    : GetOpenedDoor();

                GameObject door = null;

                if (prefab != null)
                {
                    door = Instantiate(prefab, doorParent);
                    door.transform.localPosition = new Vector3(cell.X + 1, 0, cell.Y) * wallWidth;
                    door.name += $" (Vertical)({cell.X};{cell.Y})";
                }

                doors.Add(door);
            }

            // Horizontal doors
            if (cell.Y > 0)
            {
                GameObject prefab = cell.State.HasFlag(CellState.Bottom)
                    ? GetClosedDoor()
                    : GetOpenedDoor();

                GameObject door = null;

                if (prefab != null)
                {
                    door = Instantiate(prefab, doorParent);
                    door.transform.localPosition = new Vector3(cell.X, 0, cell.Y) * wallWidth;
                    door.transform.localRotation = Quaternion.Euler(0, 90, 0);
                    door.name += $" (Horizontal)({cell.X};{cell.Y})";
                }

                doors.Add(door);
            }

        }
    }

    private GameObject GetClosedDoor() => GetRandom(closedDoorsPrefab);
    private GameObject GetOpenedDoor() => GetRandom(openedDoorsPrefab);


    #endregion

    #region Lever

    [Header("Levers")]
    [SerializeField] GameObject lever;
    [SerializeField] Transform leverParent;
    private int maxCountLevers = 3;

    private void GenerateLevers(int width, int height)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        List<Vector2Int> leversPos = new List<Vector2Int>();
        for(int y = 0; y < height; y++) 
        {
            for(int x = 0; x < width; x++)
            {
                positions.Add(new Vector2Int(x, y));
            }
        }
        
        while (leversPos.Count < maxCountLevers && positions.Count > 0)
        {
            int rndIndex = Random.Range(0, positions.Count);
            Vector2Int pos = positions[rndIndex];
            positions.RemoveAt(rndIndex);
            if(!IsLeverValid(pos, leversPos))
                continue;

            leversPos.Add(pos);
        }

        foreach (var pos in leversPos)
        {
            GameObject newLever = Instantiate(lever, leverParent);
            newLever.transform.localPosition = new Vector3(pos.x, 0, pos.y) * floorSize;
        }
    }
    private bool IsLeverValid(Vector2Int position, List<Vector2Int> leverPositions)
    {
        if (leverPositions.Count == 0) return true;

        List<Vector2Int> neighborPos = new List<Vector2Int>()
        {
            new(position.x - 1, position.y - 1),
            new(position.x - 1, position.y),
            new(position.x - 1, position.y + 1),
            new(position.x, position.y - 1),
            new(position.x, position.y + 1),
            new(position.x + 1, position.y - 1),
            new(position.x + 1, position.y),
            new(position.x + 1, position.y + 1),
        };

        foreach (var pos in leverPositions)
        {
            if (neighborPos.Contains(pos))
                return false;
        }

        return true;
    }
    #endregion

    #region Generation

    private Cell[] _cells;
    private int _width;
    private int _height;

    public void Generate(int width, int height)
    {
        // Generate the grid
        _width = width;
        _height = height;
        _cells = new Cell[width * height];

        for (var i = 0; i < _cells.Length; i++)
        {
            _cells[i] = new Cell
            {
                X = i % width,
                Y = i / width
            };
        }

        // Generate the maze
        var cellsToVisit = new Stack<Cell>();
        cellsToVisit.Push(_cells[0]);

        // While the stack is not empty
        while (cellsToVisit.Count > 0)
        {
            // Pop a cell from the stack and make it a current cell
            var currentCell = cellsToVisit.Pop();

            var neighbors = GetNeighbours(currentCell).Where(c => !c.WasVisited).ToArray();

            // If the current cell has any neighbours which have not been visited
            if (neighbors.Length == 0)
                continue;

            // Push the current cell to the stack
            cellsToVisit.Push(currentCell);

            // Choose one of the unvisited neighbours
            var rdmNeighbor = neighbors[Random.Range(0, neighbors.Length)];

            // Remove the wall between the current cell and the chosen cell
            RemoveWall(currentCell, rdmNeighbor);

            // Mark the chosen cell as visited and push it to the stack
            rdmNeighbor.WasVisited = true;
            cellsToVisit.Push(rdmNeighbor);
        }
    }

    private List<Cell> GetNeighbours(Cell cell)
    {
        var neighbours = new List<Cell>();

        if (cell.X > 0)
            neighbours.Add(GetCell(cell.X - 1, cell.Y));

        if (cell.X < _width - 1)
            neighbours.Add(GetCell(cell.X + 1, cell.Y));

        if (cell.Y > 0)
            neighbours.Add(GetCell(cell.X, cell.Y - 1));

        if (cell.Y < _height - 1)
            neighbours.Add(GetCell(cell.X, cell.Y + 1));

        return neighbours;
    }
    private Cell GetCell(int x, int y) => _cells[x + y * _width];
    private void RemoveWall(Cell cell1, Cell cell2)
    {
        if (cell1.X > cell2.X)
        {
            cell1.State &= ~CellState.Left;
            cell2.State &= ~CellState.Right;
        }

        if (cell1.X < cell2.X)
        {
            cell1.State &= ~CellState.Right;
            cell2.State &= ~CellState.Left;
        }

        if (cell1.Y > cell2.Y)
        {
            cell1.State &= ~CellState.Bottom;
            cell2.State &= ~CellState.Top;
           
        }

        if (cell1.Y < cell2.Y)
        {
            cell1.State &= ~CellState.Top;
            cell2.State &= ~CellState.Bottom;
        }
    }

    class Cell
    {
        public bool WasVisited;
        public CellState State = CellState.All;

        public int X;
        public int Y;
    }

    [System.Flags]
    enum CellState
    {
        None = 0,
        Top = 1 << 0,
        Right = 1 << 1,
        Bottom = 1 << 2,
        Left = 1 << 3,
        All = 0b1111
    }

    #endregion
}

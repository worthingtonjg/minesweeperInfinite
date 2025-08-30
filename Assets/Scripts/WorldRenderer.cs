using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Draws the infinite Minesweeper world by instantiating simple cell prefabs.
/// </summary>
public class WorldRenderer : MonoBehaviour
{
    [Header("Numbers")]
    public TMP_FontAsset numberFont;
    public Color numberColor = Color.black;
    public float numberSize = 0.6f; // relative to cellSize

    [Header("Prefabs")]
    public GameObject hiddenPrefab;
    public GameObject revealedPrefab;
    public GameObject minePrefab;
    public GameObject flagPrefab;

    [Header("World Settings")]
    public int seed = 12345;
    [Range(0f, 1f)] public double mineDensity = 0.18;
    public int chunkSize = Chunk.Size;
    public float cellSize = 1f;
    public World World => world;

    [Header("First-click Safety")]
    [Tooltip("0 = only the clicked cell, 1 = 3x3 around it")]
    public int firstClickSafeRadius = 1;

    [Header("Streaming")]
    public Camera targetCamera;       
    [Tooltip("Extra grid cells to draw beyond the viewport on all sides.")]
    public int viewPaddingCells = 2;
    [Tooltip("Cap how many new cells we instantiate per frame.")]
    public int maxAddsPerFrame = 1500;
    [Tooltip("Cap how many offscreen cells we destroy per frame.")]
    public int maxRemovalsPerFrame = 4000;

    private World world;
    private readonly Dictionary<(int gx, int gy), GameObject> cellObjects = new();
    private readonly Dictionary<(int gx, int gy), TextMeshPro> numberLabels = new();
    private readonly HashSet<(int gx, int gy)> drawnSet = new();

    void Start()
    {
        GameState.Instance.ResetRun();

        world = new World(seed, mineDensity, chunkSize);
        if (targetCamera == null) targetCamera = Camera.main;

        // Draw whatever is visible to start
        if (targetCamera != null) SyncVisibleToCamera();
    }

    void LateUpdate()
    {
        if (targetCamera == null) return;
        SyncVisibleToCamera();
    }

    private void GetVisibleGridBounds(out int gxMin, out int gxMax, out int gyMin, out int gyMax)
    {
        // Visible rect in world units
        float halfH = targetCamera.orthographicSize;
        float halfW = halfH * targetCamera.aspect;
        Vector3 cpos = targetCamera.transform.position;

        float xMin = cpos.x - halfW;
        float xMax = cpos.x + halfW;
        float yMin = cpos.y - halfH;
        float yMax = cpos.y + halfH;

        // Convert to grid indices and pad
        gxMin = Mathf.FloorToInt(xMin / cellSize) - viewPaddingCells;
        gxMax = Mathf.FloorToInt(xMax / cellSize) + viewPaddingCells;
        gyMin = Mathf.FloorToInt(yMin / cellSize) - viewPaddingCells;
        gyMax = Mathf.FloorToInt(yMax / cellSize) + viewPaddingCells;
    }

    private void SyncVisibleToCamera()
    {
        GetVisibleGridBounds(out int gxMin, out int gxMax, out int gyMin, out int gyMax);

        var desired = new HashSet<(int, int)>();
        int adds = 0;
        bool addBudgetExceeded = false;

        for (int gy = gyMin; gy <= gyMax && !addBudgetExceeded; gy++)
        {
            for (int gx = gxMin; gx <= gxMax; gx++)
            {
                desired.Add((gx, gy));
                if (!drawnSet.Contains((gx, gy)))
                {
                    DrawCell(gx, gy); // your existing DrawCell
                    drawnSet.Add((gx, gy));

                    if (++adds >= maxAddsPerFrame)
                    {
                        addBudgetExceeded = true;
                        break;
                    }
                }
            }
        }

        // Collect cells to remove
        var toRemove = new List<(int, int)>();
        foreach (var key in drawnSet)
        {
            if (!desired.Contains(key))
                toRemove.Add(key);
        }

        int removes = 0;
        foreach (var key in toRemove)
        {
            if (cellObjects.TryGetValue(key, out var go))
            {
                Destroy(go);
                cellObjects.Remove(key);
            }
            drawnSet.Remove(key);

            if (++removes >= maxRemovalsPerFrame)
                break;
        }
    }

    /// <summary>
    /// Draw a single global cell.
    /// </summary>
    private void DrawCell(int gx, int gy)
    {
        if (cellObjects.ContainsKey((gx, gy))) return;

        var state = world.GetCellState(gx, gy);
        bool mine = world.IsMine(gx, gy);

        GameObject prefab = state switch
        {
            CellState.Hidden => hiddenPrefab,
            CellState.Flagged => flagPrefab,
            CellState.Revealed when mine => minePrefab,
            CellState.Revealed => revealedPrefab,
            _ => hiddenPrefab
        };

        var go = Instantiate(prefab, transform);
        go.transform.position = new Vector3(
            (gx + 0.5f) * cellSize,
            (gy + 0.5f) * cellSize,
            0f
        );
        go.transform.localScale = Vector3.one * cellSize;

        // If revealed & not a mine, show neighbor count if > 0
        if (world.GetCellState(gx, gy) == CellState.Revealed && !world.IsMine(gx, gy))
        {
            int n = world.NeighborCountGlobal(gx, gy);
            if (n > 0)
            {
                var goNum = new GameObject($"num_{gx}_{gy}");
                goNum.transform.SetParent(transform, worldPositionStays: true);
                goNum.transform.position = new Vector3(
                    (gx + 0.5f) * cellSize,
                    (gy + 0.5f) * cellSize,
                    -0.1f // render over the tile
                );

                var tmp = goNum.AddComponent<TextMeshPro>();
                tmp.font = numberFont;
                tmp.text = n.ToString();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = numberColor;
                tmp.fontSize = cellSize * (numberSize * 10f); // tweak as needed

                numberLabels[(gx, gy)] = tmp;
            }
        }

        cellObjects[(gx, gy)] = go;
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        // With cells centered at (gx+0.5, gy+0.5), the square spans [gx, gx+1) in world/grid space.
        int gx = Mathf.FloorToInt(worldPos.x / cellSize);
        int gy = Mathf.FloorToInt(worldPos.y / cellSize);
        return new Vector2Int(gx, gy);
    }

    public void RefreshCell(int gx, int gy)
    {
        if (!cellObjects.TryGetValue((gx, gy), out var go)) return;

        // Destroy old object
        Destroy(go);
        cellObjects.Remove((gx, gy));

        // Destroy old number label   
        if (numberLabels.TryGetValue((gx, gy), out var oldLabel))
        {
            if (oldLabel) Destroy(oldLabel.gameObject);
            numberLabels.Remove((gx, gy));
        }

        // Redraw with updated state
        DrawCell(gx, gy);
    }

    public void RevealCell(int gx, int gy)
    {
        world.RevealCell(gx, gy);
        RefreshCell(gx, gy);
    }

    public void ToggleFlag(int gx, int gy)
    {
        world.ToggleFlag(gx, gy);
        RefreshCell(gx, gy);
    }

    /// <summary>
    /// Run flood reveal from (gx,gy) and refresh all changed cells.
    /// </summary>
    public void RevealFlood(int gx, int gy)
    {
        // Ensure first-click safety (does nothing after the first time)
        world.EnsureFirstClickSafety(gx, gy, firstClickSafeRadius);

        // If the clicked cell is a mine, reveal it and kill the player immediately.
        if (world.IsMine(gx, gy))
        {
            world.RevealCell(gx, gy); // ensure the bomb shows visually
            RefreshCell(gx, gy);
            GameState.Instance?.KillPlayer();
            return; // do not proceed into flood/neighbor logic
        }

        var changed = world.FloodReveal(gx, gy);
        if (changed == null || changed.Count == 0)
        {
            // Still refresh the clicked cell in case it was a mine reveal or already revealed
            RefreshCell(gx, gy);
            return;
        }

        // Refresh all newly revealed cells
        for (int i = 0; i < changed.Count; i++)
        {
            var c = changed[i];
            RefreshCell(c.x, c.y);
        }
    }

    public void ChordReveal(int gx, int gy)
    {
        // only if center is revealed and > 0 neighbors
        if (world.GetCellState(gx, gy) != CellState.Revealed) return;

        int n = world.NeighborCountGlobal(gx, gy);
        if (n <= 0) return;

        int flags = world.CountFlagsAround(gx, gy);
        if (flags != n) return;

        var changed = new List<Vector2Int>();
        world.RevealNeighborsSafely(gx, gy, changed);

        // Refresh everything that changed
        if (changed != null && changed.Count > 0)
        {
            for (int i = 0; i < changed.Count; i++)
            {
                var c = changed[i];
                RefreshCell(c.x, c.y);
            }
        }

        // Also refresh the center cell (keeps number label consistent)
        RefreshCell(gx, gy);
    }

}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all chunks in the infinite Minesweeper world.
/// Loads chunks on demand and gives global-cell access to mines & states.
/// </summary>
public class World
{
    public readonly int seed;
    public readonly double mineDensity;
    public readonly int chunkSize;

    private readonly Dictionary<Vector2Int, Chunk> chunks = new();
    private bool _firstClickApplied = false;
    private readonly HashSet<Vector2Int> _protected = new(); 

    public World(int worldSeed, double density, int chunkSize = Chunk.Size)
    {
        seed = worldSeed;
        mineDensity = density;
        this.chunkSize = chunkSize;
    }

    /// <summary>
    /// Get or create the chunk at (cx,cy).
    /// </summary>
    public Chunk GetChunk(Vector2Int c)
    {
        if (!chunks.TryGetValue(c, out var ch))
        {
            ch = new Chunk(c, seed, mineDensity);
            chunks[c] = ch;
        }
        return ch;
    }

    /// <summary>
    /// Access chunk at global cell (gx,gy).
    /// </summary>
    private Chunk GetChunkAtGlobal(int gx, int gy, out int lx, out int ly)
    {
        int cx = Coords.GlobalToChunk(gx, chunkSize);
        int cy = Coords.GlobalToChunk(gy, chunkSize);
        lx = Coords.GlobalToLocal(gx, chunkSize);
        ly = Coords.GlobalToLocal(gy, chunkSize);
        return GetChunk(new Vector2Int(cx, cy));
    }

    // Returns true if (gx,gy) is a mine, respecting first-click safety overrides.
    private bool IsMineGlobal(int gx, int gy)
    {
        if (_protected.Contains(new Vector2Int(gx, gy))) return false; // protected cells are never mines
        return MineMap.IsMine(seed, gx, gy, mineDensity);
    }

    /// <summary>
    /// Is the global cell a mine?
    /// </summary>
    public bool IsMine(int gx, int gy)
    {
        return IsMineGlobal(gx, gy);
    }

    /// <summary>
    /// Ensure the first click is safe. Call this BEFORE revealing the first cell.
    /// radius = 0 protects only (gx,gy); radius = 1 protects the 3x3 around it.
    /// Subsequent calls do nothing.
    /// </summary>
    public void EnsureFirstClickSafety(int gx, int gy, int radius)
    {
        if (_firstClickApplied) return;
        _firstClickApplied = true;

        // Protect a square around the first click
        for (int dy = -radius; dy <= radius; dy++)
        for (int dx = -radius; dx <= radius; dx++)
        {
            _protected.Add(new Vector2Int(gx + dx, gy + dy));
        }
    }

    /// <summary>
    /// How many neighbor mines does this global cell have?
    /// </summary>
    public int NeighborCount(int gx, int gy)
    {
        var ch = GetChunkAtGlobal(gx, gy, out int lx, out int ly);

        // Right now, this only returns cached count from within the same chunk.
        // Later we’ll extend to check adjacent chunks for edge cells.
        return ch.neighborCount[lx, ly];
    }

    /// <summary>
    /// Reveal a global cell. Returns true if it was safe.
    /// </summary>
    public bool RevealCell(int gx, int gy)
    {
        var ch = GetChunkAtGlobal(gx, gy, out int lx, out int ly);
        return ch.RevealCell(lx, ly);
    }

    /// <summary>
    /// Toggle flag on a global cell.
    /// </summary>
    public void ToggleFlag(int gx, int gy)
    {
        var ch = GetChunkAtGlobal(gx, gy, out int lx, out int ly);
        ch.ToggleFlag(lx, ly);
    }

    /// <summary>
    /// Get cell state (hidden, revealed, flagged).
    /// </summary>
    public CellState GetCellState(int gx, int gy)
    {
        var ch = GetChunkAtGlobal(gx, gy, out int lx, out int ly);
        return ch.state[lx, ly];
    }

    /// <summary>
    /// Enumerate all active chunks (for rendering).
    /// </summary>
    public IEnumerable<KeyValuePair<Vector2Int, Chunk>> GetLoadedChunks()
    {
        return chunks;
    }

    /// <summary>
    /// Count mines around a global cell by checking all 8 neighbors across chunk boundaries.
    /// </summary>
    public int NeighborCountGlobal(int gx, int gy)
    {
        int count = 0;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            if (IsMine(gx + dx, gy + dy)) count++;
        }
        return count;
    }

    /// <summary>
    /// Flood-reveal starting at (gx,gy). Reveals the clicked cell.
    /// If its neighbor count is 0, expands to neighbors (BFS).
    /// Skips flagged cells. Returns all global cells whose state changed to Revealed.
    /// </summary>
    public List<Vector2Int> FloodReveal(int startGx, int startGy)
    {
        var revealed = new List<Vector2Int>();

        // If start is a mine, just reveal that one and stop.
        if (IsMine(startGx, startGy))
        {
            GameState.Instance?.KillPlayer();
            var chMine = GetChunkAtGlobal(startGx, startGy, out int lxM, out int lyM);
            if (chMine.state[lxM, lyM] == CellState.Hidden)
            {
                chMine.state[lxM, lyM] = CellState.Revealed;
                revealed.Add(new Vector2Int(startGx, startGy));
            }
            return revealed;
        }

        // BFS over safe cells
        var q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(startGx, startGy));

        while (q.Count > 0)
        {
            var g = q.Dequeue();
            var ch = GetChunkAtGlobal(g.x, g.y, out int lx, out int ly);

            // Skip non-hidden and flagged
            var st = ch.state[lx, ly];
            if (st != CellState.Hidden) continue;
            if (st == CellState.Flagged) continue; // (redundant due to previous check, here for clarity)

            // Safe reveal
            ch.state[lx, ly] = CellState.Revealed;
            revealed.Add(g);

            // If neighbors > 0, stop here (don’t enqueue neighbors)
            int n = NeighborCountGlobal(g.x, g.y);
            if (n > 0) continue;

            // Expand to 8 neighbors
            for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                int ngx = g.x + dx;
                int ngy = g.y + dy;

                // Only enqueue if not mine and not already revealed/flagged
                if (IsMine(ngx, ngy)) continue;

                var nch = GetChunkAtGlobal(ngx, ngy, out int nlx, out int nly);
                var nst = nch.state[nlx, nly];
                if (nst == CellState.Hidden)
                    q.Enqueue(new Vector2Int(ngx, ngy));
            }
        }

        return revealed;
    }

    public int CountFlagsAround(int gx, int gy)
    {
        int flags = 0;
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            var st = GetCellState(gx + dx, gy + dy);
            if (st == CellState.Flagged) flags++;
        }
        return flags;
    }

    public void RevealNeighborsSafely(int gx, int gy, List<Vector2Int> outChanged)
    {
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            if (dx == 0 && dy == 0) continue;
            int ngx = gx + dx, ngy = gy + dy;
            // Skip flagged
            if (GetCellState(ngx, ngy) == CellState.Flagged) continue;

            // Use flood so zeros expand naturally
            var more = FloodReveal(ngx, ngy);
            if (more != null && more.Count > 0)
            {
                outChanged.AddRange(more);
            }
        }
    }
}

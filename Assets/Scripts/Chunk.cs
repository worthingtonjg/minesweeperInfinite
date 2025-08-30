using UnityEngine;

/// <summary>
/// A single chunk of the infinite Minesweeper world.
/// Holds per-cell state (hidden, revealed, flagged) and cached mine info.
/// </summary>
public class Chunk
{
    public const int Size = 16;   // chunk dimensions (16×16 for example)

    public readonly Vector2Int coord;   // chunk coordinate (cx, cy)
    public readonly CellState[,] state; // player state for each cell
    public readonly bool[,] isMine;     // cached mine layout
    public readonly byte[,] neighborCount; // cached neighbor counts

    public bool finished; // true when all non-mine cells are revealed

    private readonly int worldSeed;
    private readonly double mineDensity;

    public Chunk(Vector2Int chunkCoord, int seed, double density)
    {
        coord = chunkCoord;
        worldSeed = seed;
        mineDensity = density;

        state = new CellState[Size, Size];
        isMine = new bool[Size, Size];
        neighborCount = new byte[Size, Size];

        Generate();
    }

    /// <summary>
    /// Fill this chunk’s mine layout and neighbor counts deterministically.
    /// </summary>
    private void Generate()
    {
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                int gx = Coords.ChunkLocalToGlobal(coord.x, x, Size);
                int gy = Coords.ChunkLocalToGlobal(coord.y, y, Size);

                // stable mine layout
                isMine[x, y] = MineMap.IsMine(worldSeed, gx, gy, mineDensity);

                // start all cells hidden
                state[x, y] = CellState.Hidden;
            }
        }

        // compute neighbor counts
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (isMine[x, y])
                {
                    neighborCount[x, y] = 0;
                }
                else
                {
                    neighborCount[x, y] = CountNeighbors(x, y);
                }
            }
        }
    }

    private byte CountNeighbors(int lx, int ly)
    {
        byte count = 0;
        for (int oy = -1; oy <= 1; oy++)
        {
            for (int ox = -1; ox <= 1; ox++)
            {
                if (ox == 0 && oy == 0) continue;

                int nx = lx + ox;
                int ny = ly + oy;
                if (nx < 0 || ny < 0 || nx >= Size || ny >= Size)
                    continue; // neighbor might be in another chunk, ignore for now

                if (isMine[nx, ny]) count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Mark a cell as revealed. Returns true if it was safe (not a mine).
    /// </summary>
    public bool RevealCell(int lx, int ly)
    {
        if (state[lx, ly] != CellState.Hidden)
            return true; // already revealed or flagged, ignore

        state[lx, ly] = CellState.Revealed;
        if (!isMine[lx, ly]) CheckFinished();
        return !isMine[lx, ly];
    }

    /// <summary>
    /// Toggle a flag on this cell.
    /// </summary>
    public void ToggleFlag(int lx, int ly)
    {
        if (state[lx, ly] == CellState.Hidden)
            state[lx, ly] = CellState.Flagged;
        else if (state[lx, ly] == CellState.Flagged)
            state[lx, ly] = CellState.Hidden;
    }

    private void CheckFinished()
    {
        for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
                if (!isMine[x, y] && state[x, y] != CellState.Revealed)
                    return;

        finished = true;
    }
}

/// <summary>
/// Possible states of a cell in play.
/// </summary>
public enum CellState : byte
{
    Hidden,
    Revealed,
    Flagged
}

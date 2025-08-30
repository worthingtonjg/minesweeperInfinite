using UnityEngine;

/// <summary>
/// Helper functions for converting between:
/// - Global cell coords (gx, gy) → the absolute position in the infinite grid
/// - Chunk coords (cx, cy) → which chunk contains that cell
/// - Local coords (lx, ly) → index inside the chunk [0..ChunkSize-1]
/// </summary>
public static class Coords
{
    /// <summary>
    /// Global cell coordinate → chunk coordinate.
    /// Handles negatives correctly.
    /// </summary>
    public static int GlobalToChunk(int g, int chunkSize)
    {
        // floor division so that negative cells map correctly
        return Mathf.FloorToInt((float)g / chunkSize);
    }

    /// <summary>
    /// Global cell coordinate → local coordinate inside the chunk.
    /// Always returns a value in [0, chunkSize-1].
    /// </summary>
    public static int GlobalToLocal(int g, int chunkSize)
    {
        int m = g % chunkSize;
        if (m < 0) m += chunkSize; // fix negative modulo
        return m;
    }

    /// <summary>
    /// Chunk + local coords → global coordinate.
    /// </summary>
    public static int ChunkLocalToGlobal(int c, int l, int chunkSize)
    {
        return c * chunkSize + l;
    }
}

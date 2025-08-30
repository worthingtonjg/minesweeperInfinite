using System;

/// <summary>
/// MineMap = deterministic map logic for an infinite Minesweeper world.
/// Given a world seed and global cell coordinates, it will always return
/// the same results (mine placement, random noise values, etc.).
/// </summary>
public static class MineMap
{
    // SplitMix64 mixer: gives us stable, high-quality pseudo-random bits.
    private static ulong Mix64(ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        x ^= (x >> 31);
        return x;
    }

    /// <summary>
    /// Unique, stable hash for a specific cell (seed + global x,y).
    /// </summary>
    public static ulong CellHash(int worldSeed, int gx, int gy)
    {
        unchecked
        {
            ulong s = (ulong)(uint)worldSeed;
            ulong x = (ulong)(uint)gx;
            ulong y = (ulong)(uint)gy;

            ulong h = s * 0x9E3779B185EBCA87UL;
            h ^= x + 0x632BE59BD9B4E019UL;
            h = Mix64(h);
            h ^= y + 0xC6BC279692B5C323UL;
            h = Mix64(h);
            return h;
        }
    }

    /// <summary>
    /// Turn a hash into a double in [0,1).
    /// </summary>
    private static double ToUnit01(ulong h)
    {
        const double inv = 1.0 / (1UL << 53);
        return (double)(h >> 11) * inv;
    }

    /// <summary>
    /// Get a "random-looking" but stable value for this cell in [0,1).
    /// </summary>
    public static double CellNoise(int worldSeed, int gx, int gy)
        => ToUnit01(CellHash(worldSeed, gx, gy));

    /// <summary>
    /// Get a second (or third, etc.) independent noise channel for the same cell.
    /// Useful for decorations, difficulty modifiers, etc.
    /// </summary>
    public static double CellNoiseWithSalt(int worldSeed, int gx, int gy, ulong salt)
    {
        ulong h = CellHash(worldSeed, gx, gy) ^ Mix64(salt);
        return ToUnit01(Mix64(h));
    }

    /// <summary>
    /// Decide if this cell contains a mine based on a global density value.
    /// </summary>
    public static bool IsMine(int worldSeed, int gx, int gy, double mineDensity)
        => CellNoise(worldSeed, gx, gy) < mineDensity;

    /// <summary>
    /// Get a stable random value in [min,max) for this cell.
    /// </summary>
    public static double CellValueInRange(int worldSeed, int gx, int gy, double min, double max)
        => min + CellNoise(worldSeed, gx, gy) * (max - min);
}

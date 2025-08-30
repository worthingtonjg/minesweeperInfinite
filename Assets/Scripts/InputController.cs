using UnityEngine;

/// <summary>
/// Handles player input (mouse/touch) and applies it to the Minesweeper world.
/// </summary>
public class InputController : MonoBehaviour
{
    public Camera cam;                  // assign Main Camera in inspector
    public WorldRenderer worldRenderer; // assign your WorldRenderer in inspector

    public bool flagOnlyMode = false;  // when true: left-click on hidden = flag
    public KeyCode toggleFlagOnlyKey = KeyCode.F;

    public float leftClickDragThreshold = 6f;   // pixels

    bool lmbDown = false;
    bool leftDragging = false;
    bool startedOnRevealed = false;

    Vector2 lmbDownScreenPos;
    Vector3 dragAnchorWorld;   


    void Update()
    {
        // Toggle Flag-Only Mode (keep if you had this)
        if (Input.GetKeyDown(toggleFlagOnlyKey))
        {
            flagOnlyMode = !flagOnlyMode;
            Debug.Log("Flag-Only Mode: " + (flagOnlyMode ? "ON" : "OFF"));
        }

        // --- LEFT mouse down: record start; allow drag only if starting on a revealed cell
        if (Input.GetMouseButtonDown(0))
        {
            lmbDown = true;
            leftDragging = false;
            lmbDownScreenPos = (Vector2)Input.mousePosition;

            var worldPos = MouseWorldAtZ0();
            var g = worldRenderer.WorldToGrid(worldPos);
            startedOnRevealed = (worldRenderer.World.GetCellState(g.x, g.y) == CellState.Revealed);

            // World-space anchor under the cursor at drag start
            dragAnchorWorld = worldPos;
        }

        // --- LEFT mouse held: if started on revealed, convert to drag after threshold
        if (lmbDown && Input.GetMouseButton(0) && startedOnRevealed)
        {
            float dist = Vector2.Distance(lmbDownScreenPos, (Vector2)Input.mousePosition);
            if (!leftDragging && dist > leftClickDragThreshold)
            {
                leftDragging = true;
            }

            if (leftDragging)
            {
                // Move camera so the world point under the cursor stays at the original anchor
                Vector3 nowWorld = MouseWorldAtZ0();
                Vector3 delta = dragAnchorWorld - nowWorld;
                cam.transform.position += delta;
                // NOTE: keep dragAnchorWorld constant; do NOT update it each frame.
            }
        }

        // --- LEFT mouse up: click if not dragging; otherwise end drag
        if (lmbDown && Input.GetMouseButtonUp(0))
        {
            lmbDown = false;

            if (!leftDragging)
            {
                // Normal left-click (reveal / chord / flag-only)
                HandleClick(reveal: true);
            }

            leftDragging = false;
            startedOnRevealed = false;
        }

        // --- RIGHT click still toggles flag (no pan on RMB)
        if (Input.GetMouseButtonDown(1))
        {
            HandleClick(reveal: false);
        }
    }


    private Vector3 MouseWorldAtZ0()
    {
        // Distance from camera to the Z=0 plane
        float dz = -cam.transform.position.z;
        var mp = Input.mousePosition;
        mp.z = dz;
        return cam.ScreenToWorldPoint(mp);
    }


    private void HandleClick(bool reveal)
    {
        if (GameState.Instance != null && GameState.Instance.IsDead) return;

        // ray from camera into world
        Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
        var g = worldRenderer.WorldToGrid(worldPos);

        // Decide what to do based on mode and current cell state
        var state = worldRenderer.World.GetCellState(g.x, g.y);

        if (reveal)
        {
            if (flagOnlyMode && state == CellState.Hidden || state == CellState.Flagged)
            {
                // In Flag-Only Mode, left-click on HIDDEN -> place/remove flag
                worldRenderer.ToggleFlag(g.x, g.y);
            }
            else
            {
                // Normal behavior:
                // If it's a revealed number, try chord; otherwise do flood reveal
                if (state == CellState.Revealed && worldRenderer.World.NeighborCountGlobal(g.x, g.y) > 0)
                {
                    worldRenderer.ChordReveal(g.x, g.y);
                }
                else
                {
                    worldRenderer.RevealFlood(g.x, g.y);
                }

                // Your explicit redraw preference
                worldRenderer.RefreshCell(g.x, g.y);
            }
        }
        else
        {
            // Right-click (or reveal==false) still toggles flag
            worldRenderer.ToggleFlag(g.x, g.y);
        }
    }

}

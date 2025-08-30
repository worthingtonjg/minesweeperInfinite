using UnityEngine;
using UnityEngine.Rendering.Universal;


[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    public int basePPU = 32;   // Match your sprite import PPU
    public int minStep = 1;    // Minimum zoom step (1 = base scale)
    public int maxStep = 8;    // Maximum zoom step
    public int startStep = 2;  // Starting zoom step

    private Camera cam;
    private PixelPerfectCamera ppc;
    private int zoomStep;

    void Awake()
    {
        cam = GetComponent<Camera>();
        ppc = GetComponent<PixelPerfectCamera>();
        zoomStep = Mathf.Clamp(startStep, minStep, maxStep);
        ApplyZoom();
    }

    void Update()
    {
        // Wheel zoom
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            Vector3 mouseWorldBefore = cam.ScreenToWorldPoint(Input.mousePosition);

            // Change zoom step instead of orthographic size
            int stepChange = scroll > 0 ? 1 : -1;
            zoomStep = Mathf.Clamp(zoomStep + stepChange, minStep, maxStep);
            ApplyZoom();

            Vector3 mouseWorldAfter = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position += mouseWorldBefore - mouseWorldAfter;
        }
    }

    void ApplyZoom()
    {
        // Adjust Pixel Perfect Camera’s Assets PPU
        ppc.assetsPPU = basePPU * zoomStep;
    }
}

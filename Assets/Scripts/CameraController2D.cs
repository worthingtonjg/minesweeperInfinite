using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController2D : MonoBehaviour
{
    public float keyPanSpeed = 12f;
    public float zoomSpeed = 5f;
    public float minSize = 2f;
    public float maxSize = 40f;

    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void Update()
    {
        // Keyboard pan
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h != 0f || v != 0f)
        {
            Vector3 delta = new Vector3(h, v, 0f).normalized * keyPanSpeed * Time.deltaTime;
            transform.position += delta;
        }

        // Wheel zoom
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.0001f)
        {
            Vector3 mouseWorldBefore = cam.ScreenToWorldPoint(Input.mousePosition);

            cam.orthographicSize = Mathf.Clamp(
                cam.orthographicSize * Mathf.Pow(1f - 0.1f * zoomSpeed, scroll),
                minSize, maxSize
            );

            Vector3 mouseWorldAfter = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position += mouseWorldBefore - mouseWorldAfter;
        }
    }
}

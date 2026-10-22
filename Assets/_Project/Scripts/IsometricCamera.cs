using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Angled ARPG camera that follows a target, with mouse-wheel / trackpad zoom.
/// Attach to the Main Camera.
/// </summary>
public class IsometricCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [Tooltip("Aim slightly above the target's feet.")]
    [SerializeField] private float targetHeightOffset = 1f;

    [Header("Angle")]
    [Tooltip("How steeply the camera looks down. Higher = more top-down.")]
    [SerializeField, Range(20f, 85f)] private float pitch = 50f;
    [Tooltip("Rotation around the vertical axis. 45 gives the classic diagonal view.")]
    [SerializeField] private float yaw = 45f;

    [Header("Zoom")]
    [SerializeField] private float distance = 18f;
    [SerializeField] private float minDistance = 10f;
    [SerializeField] private float maxDistance = 28f;
    [Tooltip("Scroll sensitivity. Mouse wheels and trackpads report very different values, so tune this.")]
    [SerializeField] private float zoomSensitivity = 0.01f;
    [SerializeField] private float zoomSmoothing = 12f;

    [Header("Follow")]
    [Tooltip("Lower = tighter follow. 0 = locked to the target.")]
    [SerializeField] private float followSmoothTime = 0.08f;

    private float targetDistance;
    private Vector3 smoothedFocus;
    private Vector3 followVelocity;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        SnapToTarget();
    }

    private void Start()
    {
        targetDistance = distance;
        SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleZoom();
        distance = Mathf.Lerp(distance, targetDistance, 1f - Mathf.Exp(-zoomSmoothing * Time.deltaTime));

        Vector3 focus = target.position + Vector3.up * targetHeightOffset;
        smoothedFocus = followSmoothTime > 0f
            ? Vector3.SmoothDamp(smoothedFocus, focus, ref followVelocity, followSmoothTime)
            : focus;

        ApplyTransform(smoothedFocus);
    }

    private void HandleZoom()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f)) return;

        targetDistance = Mathf.Clamp(targetDistance - scroll * zoomSensitivity, minDistance, maxDistance);
    }

    private void SnapToTarget()
    {
        if (target == null) return;
        smoothedFocus = target.position + Vector3.up * targetHeightOffset;
        followVelocity = Vector3.zero;
        ApplyTransform(smoothedFocus);
    }

    private void ApplyTransform(Vector3 focus)
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
        transform.position = focus - rotation * Vector3.forward * distance;
    }
}

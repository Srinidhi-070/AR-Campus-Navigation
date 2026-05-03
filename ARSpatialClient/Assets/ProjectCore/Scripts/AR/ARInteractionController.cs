using System;
using UnityEngine;
using UnityEngine.InputSystem;

#if !UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif

/// <summary>
/// Handles input for navigation.
/// Editor  : left mouse click on ground plane.
/// Android : AR touch raycast on detected plane.
/// </summary>
public class ARInteractionController : MonoBehaviour
{
    [Header("Editor Test")]
    [SerializeField] private LayerMask m_GroundLayer = ~0;

    public event Action<Vector3> OnWorldTapped;

#if !UNITY_EDITOR
    private ARRaycastManager m_RaycastManager;
    private readonly List<ARRaycastHit> m_Hits = new List<ARRaycastHit>();

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
        if (m_RaycastManager == null)
            Debug.LogError("[ARInteractionController] ARRaycastManager missing.");
    }
#endif

    void Update()
    {
#if UNITY_EDITOR
        HandleEditorInput();
#else
        HandleTouchInput();
#endif
    }

    private void HandleEditorInput()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, m_GroundLayer))
            Dispatch(hit.point);
    }

#if !UNITY_EDITOR
    private void HandleTouchInput()
    {
        if (Touchscreen.current == null) return;
        var touch = Touchscreen.current.primaryTouch;
        if (!touch.press.wasPressedThisFrame) return;

        Vector2 pos = touch.position.ReadValue();
        if (m_RaycastManager == null) return;
        if (!m_RaycastManager.Raycast(pos, m_Hits, TrackableType.PlaneWithinPolygon)) return;

        Dispatch(m_Hits[0].pose.position);
    }
#endif

    private void Dispatch(Vector3 worldPos)
    {
        OnWorldTapped?.Invoke(worldPos);
        Debug.Log($"[ARInteractionController] Tapped at {worldPos}");
    }
}

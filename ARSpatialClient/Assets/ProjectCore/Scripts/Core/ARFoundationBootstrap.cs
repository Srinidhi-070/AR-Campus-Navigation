using UnityEngine;
using UnityEngine.XR.ARFoundation;

#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;
#endif

/// <summary>
/// Adds AR Foundation components at runtime for device builds.
/// This ensures AR camera and session work on Android without scene setup.
/// </summary>
public class ARFoundationBootstrap : MonoBehaviour
{
    void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS
        SetupXROrigin();
        SetupARSession();
#else
        Debug.Log("[ARFoundationBootstrap] Skipping AR setup in Editor");
#endif
    }

    private void SetupXROrigin()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Check if XR Origin already exists
        XROrigin existingOrigin = FindObjectOfType<XROrigin>();
        if (existingOrigin != null)
        {
            Debug.Log("[ARFoundationBootstrap] XR Origin already exists");
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("[ARFoundationBootstrap] No main camera found!");
            return;
        }

        // Create XR Origin
        GameObject xrOriginGO = new GameObject("XR Origin");
        xrOriginGO.transform.SetParent(transform, false);
        xrOriginGO.transform.localPosition = Vector3.zero;
        xrOriginGO.transform.localRotation = Quaternion.identity;
        
        XROrigin xrOrigin = xrOriginGO.AddComponent<XROrigin>();
        
        // Create Camera Offset
        GameObject cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOriginGO.transform, false);
        cameraOffset.transform.localPosition = Vector3.zero;
        cameraOffset.transform.localRotation = Quaternion.identity;
        
        // Move main camera under Camera Offset
        mainCamera.transform.SetParent(cameraOffset.transform, false);
        mainCamera.transform.localPosition = Vector3.zero;
        mainCamera.transform.localRotation = Quaternion.identity;
        
        // Add AR components to camera
        if (mainCamera.GetComponent<ARCameraManager>() == null)
        {
            mainCamera.gameObject.AddComponent<ARCameraManager>();
            mainCamera.gameObject.AddComponent<ARCameraBackground>();
        }
        
        // Set camera to clear solid color for AR
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.black;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 20f;
        
        // Configure XR Origin
        xrOrigin.Camera = mainCamera;
        xrOrigin.CameraFloorOffsetObject = cameraOffset;
        
        Debug.Log("[ARFoundationBootstrap] Created XR Origin with camera");
#endif
    }

    private void SetupARSession()
    {
#if UNITY_ANDROID || UNITY_IOS
        ARSession existingSession = FindObjectOfType<ARSession>();
        if (existingSession != null)
        {
            Debug.Log("[ARFoundationBootstrap] ARSession already exists");
            return;
        }

        GameObject sessionGO = new GameObject("AR Session");
        sessionGO.transform.SetParent(transform, false);
        ARSession session = sessionGO.AddComponent<ARSession>();
        
        Debug.Log("[ARFoundationBootstrap] Created ARSession");
#endif
    }
}

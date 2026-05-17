using UnityEngine;
using UnityEngine.XR.ARFoundation;

#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
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
        EnsureXRLoaderRunning();
        SetupXROrigin();
        SetupARSession();
#else
        Debug.Log("[ARFoundationBootstrap] Skipping AR setup in Editor");
#endif
    }

    private void EnsureXRLoaderRunning()
    {
#if UNITY_ANDROID || UNITY_IOS
        XRGeneralSettings settings = XRGeneralSettings.Instance;
        if (settings == null || settings.Manager == null)
        {
            Debug.LogError("[ARFoundationBootstrap] XRGeneralSettings is missing. Check XR Plug-in Management preloaded assets.");
            return;
        }

        XRManagerSettings manager = settings.Manager;
        if (manager.activeLoader == null)
        {
            Debug.Log("[ARFoundationBootstrap] Initializing XR loader");
            manager.InitializeLoaderSync();
        }

        if (manager.activeLoader == null)
        {
            Debug.LogError("[ARFoundationBootstrap] XR loader did not initialize. AR camera background and tracking will not run.");
            return;
        }

        manager.StartSubsystems();
        Debug.Log($"[ARFoundationBootstrap] XR loader active: {manager.activeLoader.name}");
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

        EnsureTrackedPoseDriver(mainCamera);

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
        
        if (mainCamera.GetComponent<ARCameraManager>() == null)
            mainCamera.gameObject.AddComponent<ARCameraManager>();
        if (mainCamera.GetComponent<ARCameraBackground>() == null)
            mainCamera.gameObject.AddComponent<ARCameraBackground>();
        
        // Set camera to clear solid color for AR
        mainCamera.orthographic = false;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = Color.black;
        mainCamera.fieldOfView = 60f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 20f;
        
        // Configure XR Origin
        xrOrigin.Camera = mainCamera;
        xrOrigin.CameraFloorOffsetObject = cameraOffset;
        
        // Add ARPlaneManager for surface detection (horizontal + vertical)
        ARPlaneManager planeManager = xrOriginGO.AddComponent<ARPlaneManager>();
        planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal
                                            | UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Vertical;
        
        // Try to load plane prefab from Resources
        GameObject planePrefab = Resources.Load<GameObject>("Prefabs/ARFeatheredPlane");
        if (planePrefab == null)
        {
            // Create a simple plane prefab at runtime
            planePrefab = CreateSimplePlanePrefab();
            Debug.Log("[ARFoundationBootstrap] Created runtime plane prefab");
        }
        else
        {
            Debug.Log("[ARFoundationBootstrap] Loaded plane prefab from Resources");
        }
        
        planeManager.planePrefab = planePrefab;
        
        // Point Cloud visualization is disabled to prevent ARCore feature overload 
        // (resolves "Too many image measurements" and RANSAC errors in logcat)
        
        // Add ARRaycastManager for placing objects
        xrOriginGO.AddComponent<ARRaycastManager>();
        
        Debug.Log("[ARFoundationBootstrap] Created XR Origin with camera, plane detection, point cloud, and raycast");
#endif
    }

    private void EnsureTrackedPoseDriver(Camera mainCamera)
    {
#if UNITY_ANDROID || UNITY_IOS
        TrackedPoseDriver poseDriver = mainCamera.GetComponent<TrackedPoseDriver>();
        if (poseDriver == null)
            poseDriver = mainCamera.gameObject.AddComponent<TrackedPoseDriver>();

        var trackingStateAction = new InputAction(
            "HMD Tracking State",
            InputActionType.PassThrough,
            "<XRHMD>/trackingState",
            expectedControlType: "Integer");
        trackingStateAction.Enable();
        poseDriver.trackingStateInput = new InputActionProperty(trackingStateAction);

        var positionAction = new InputAction(
            "HMD Position",
            InputActionType.PassThrough,
            "<XRHMD>/centerEyePosition",
            expectedControlType: "Vector3");
        positionAction.Enable();
        poseDriver.positionInput = new InputActionProperty(positionAction);

        var rotationAction = new InputAction(
            "HMD Rotation",
            InputActionType.PassThrough,
            "<XRHMD>/centerEyeRotation",
            expectedControlType: "Quaternion");
        rotationAction.Enable();
        poseDriver.rotationInput = new InputActionProperty(rotationAction);
        poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
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
        sessionGO.AddComponent<ARSession>();
        sessionGO.AddComponent<ARInputManager>();
        
        Debug.Log("[ARFoundationBootstrap] Created ARSession");
#endif
    }
    
    private GameObject CreateSimplePlanePrefab()
    {
#if UNITY_ANDROID || UNITY_IOS
        // Create a simple plane visualization
        GameObject planePrefab = new GameObject("SimplePlane");
        
        // Add ARPlane component (required)
        planePrefab.AddComponent<ARPlane>();
        
        // Add ARPlaneMeshVisualizer (CRITICAL: updates the mesh to match the physical plane)
        planePrefab.AddComponent<ARPlaneMeshVisualizer>();
        
        // Add MeshFilter and MeshRenderer for visualization
        MeshFilter meshFilter = planePrefab.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = planePrefab.AddComponent<MeshRenderer>();
        
        // Create a simple quad mesh (ARPlaneMeshVisualizer will replace this)
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0, -0.5f),
            new Vector3(0.5f, 0, -0.5f),
            new Vector3(-0.5f, 0, 0.5f),
            new Vector3(0.5f, 0, 0.5f)
        };
        mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
        mesh.normals = new Vector3[]
        {
            Vector3.up, Vector3.up, Vector3.up, Vector3.up
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        meshFilter.mesh = mesh;
        
        // Use Sprites/Default shader — it reliably supports transparency on ALL platforms.
        // Runtime-created URP/Lit transparent materials are fragile and often fail.
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Transparent");
            Debug.LogWarning("[ARFoundationBootstrap] Sprites/Default not found, trying Unlit/Transparent");
        }
        if (shader == null)
        {
            shader = Shader.Find("Standard");
            Debug.LogWarning("[ARFoundationBootstrap] Fallback to Standard shader");
        }
        
        Material planeMaterial = new Material(shader);
        planeMaterial.color = new Color(0f, 0.83f, 0.88f, 0.35f); // Semi-transparent cyan
        planeMaterial.renderQueue = 3000; // Transparent queue
        meshRenderer.material = planeMaterial;
        
        Debug.Log($"[ARFoundationBootstrap] Created plane prefab with shader: {shader?.name ?? "NULL"}");
        
        // Add LineRenderer for plane boundary
        LineRenderer lineRenderer = planePrefab.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        
        Material lineMaterial = new Material(shader);
        lineMaterial.color = new Color(0f, 0.95f, 1f, 0.9f); // Bright cyan border
        lineRenderer.material = lineMaterial;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = false;
        
        // Make it inactive (ARPlaneManager will activate instances)
        planePrefab.SetActive(false);
        
        return planePrefab;
#else
        return null;
#endif
    }
    
    private GameObject CreatePointCloudPrefab()
    {
#if UNITY_ANDROID || UNITY_IOS
        GameObject prefab = new GameObject("ARPointCloud");
        
        // ARPointCloud component is REQUIRED — it feeds feature point data to the visualizer
        prefab.AddComponent<ARPointCloud>();
        
        // ARPointCloudParticleVisualizer renders each feature point as a particle
        prefab.AddComponent<ARPointCloudParticleVisualizer>();
        
        // ParticleSystem — required by ARPointCloudParticleVisualizer
        ParticleSystem ps = prefab.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = prefab.AddComponent<ParticleSystem>();
        
        // Configure particle system for small white dots
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startSize = 0.02f;
        main.startColor = new Color(1f, 1f, 1f, 0.85f); // White with slight transparency
        main.startLifetime = float.MaxValue;
        main.maxParticles = 5000;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Disable emission (ARPointCloudParticleVisualizer sets particles manually)
        var emission = ps.emission;
        emission.enabled = false;
        
        // Disable shape
        var shape = ps.shape;
        shape.enabled = false;
        
        // Configure renderer for small dots
        ParticleSystemRenderer psRenderer = prefab.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            // Use the default particle material
            Shader particleShader = Shader.Find("Sprites/Default");
            if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader != null)
            {
                Material particleMat = new Material(particleShader);
                particleMat.color = new Color(1f, 1f, 1f, 0.85f);
                psRenderer.material = particleMat;
            }
        }
        
        prefab.SetActive(false);
        
        Debug.Log("[ARFoundationBootstrap] Created point cloud prefab with ParticleSystem");
        return prefab;
#else
        return null;
#endif
    }
}

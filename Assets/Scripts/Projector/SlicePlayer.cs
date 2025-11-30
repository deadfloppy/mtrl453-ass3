using UnityEngine;

public class SlicePlayer : MonoBehaviour
{
    [Header("Slice Images")]
    public Texture2D[] sliceTextures;
    
    [Header("Helicoid Settings")]
    public int flutes = 4;
    public float rpm = 24f;
    
    [Header("Cameras")]
    public Camera mainSimulationCamera;  // Shows the 3D helicoid spinning
    public Camera projectorCamera;       // Shows only the flat slices
    
    [Header("Helicoid Model (Optional)")]
    public GameObject helicoidModel;     // Your 3D helicoid mesh
    
    [Header("Display Settings")]
    public Color backgroundColor = Color.black;
    
    private GameObject displayQuad;
    private Material quadMaterial;
    
    private float currentAngle = 0f;
    private float degreesPerSecond;
    private int currentSliceIndex = 0;
    
    void Start()
    {
        if (sliceTextures == null || sliceTextures.Length == 0)
        {
            Debug.LogError("No slice textures assigned!");
            return;
        }
        
        degreesPerSecond = (rpm / 60f) * 360f;
        
        // Setup cameras
        SetupCameras();
        
        // Create the quad that displays slices
        CreateDisplayQuad();
        
        // Setup helicoid model if assigned
        if (helicoidModel != null)
        {
            SetupHelicoidModel();
        }
        
        Debug.Log($"Loaded {sliceTextures.Length} slices at {rpm} RPM ({degreesPerSecond}°/sec)");
    }
    
    void SetupCameras()
    {
        // Main Camera - Display 0 (your laptop screen)
        // Shows the 3D simulation of the helicoid
        if (mainSimulationCamera != null)
        {
            mainSimulationCamera.targetDisplay = 0;
            mainSimulationCamera.cullingMask = LayerMask.GetMask("Default", "Simulation");
            // Keep your existing sky/lighting setup
        }
        else
        {
            Debug.LogWarning("Main simulation camera not assigned!");
        }
        
        // Projector Camera - Display 1 (HDMI projector)
        // Shows ONLY the flat slice images
        if (projectorCamera != null)
        {
            projectorCamera.targetDisplay = 1;
            projectorCamera.cullingMask = LayerMask.GetMask("ProjectorSlices"); // ONLY sees the quad
            projectorCamera.clearFlags = CameraClearFlags.SolidColor;
            projectorCamera.backgroundColor = backgroundColor;
            projectorCamera.orthographic = true;
            projectorCamera.orthographicSize = 0.329f;
            
            // Position to look at the quad (stationary, doesn't move)
            projectorCamera.transform.position = new Vector3(0, 0, -2);
            projectorCamera.transform.rotation = Quaternion.identity;
        }
        else
        {
            Debug.LogError("Projector camera not assigned!");
        }
    }
    
    void CreateDisplayQuad()
    {
        // Create a quad to display the slice textures
        displayQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        displayQuad.name = "SliceDisplayQuad";
        displayQuad.layer = LayerMask.NameToLayer("ProjectorSlices");
        
        // Position it at origin, facing the projector camera
        displayQuad.transform.position = Vector3.zero;
        displayQuad.transform.rotation = Quaternion.identity;
        displayQuad.transform.localScale = new Vector3(1.6f, 0.9f, 1f);
        
        // Create transparent material
        quadMaterial = new Material(Shader.Find("Unlit/Transparent"));
        quadMaterial.mainTexture = sliceTextures[0];
        
        displayQuad.GetComponent<Renderer>().material = quadMaterial;
    }
    
    void SetupHelicoidModel()
    {
        // Put the helicoid on the Simulation layer
        // so only the main camera sees it, not the projector
        SetLayerRecursively(helicoidModel, LayerMask.NameToLayer("Simulation"));
    }
    
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
    
    void Update()
    {
        // Update rotation angle
        currentAngle += degreesPerSecond * Time.deltaTime;
        currentAngle %= 360f;
        
        // Rotate the helicoid model if assigned (for simulation view)
        if (helicoidModel != null)
        {
            helicoidModel.transform.rotation = Quaternion.Euler(0, currentAngle, 0);
        }
        
        // Calculate which slice to display
        int newSliceIndex = Mathf.FloorToInt((currentAngle / 360f) * sliceTextures.Length) % sliceTextures.Length;
        
        if (newSliceIndex != currentSliceIndex)
        {
            currentSliceIndex = newSliceIndex;
            UpdateDisplayedSlice();
        }
    }
    
    void UpdateDisplayedSlice()
    {
        if (quadMaterial != null && currentSliceIndex < sliceTextures.Length)
        {
            quadMaterial.mainTexture = sliceTextures[currentSliceIndex];
        }
    }
    
    void OnDestroy()
    {
        if (displayQuad != null)
        {
            Destroy(displayQuad);
        }
        if (quadMaterial != null)
        {
            Destroy(quadMaterial);
        }
    }
}
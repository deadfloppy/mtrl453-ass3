using UnityEngine;

public class HelicoidVolumetricDisplay : MonoBehaviour
{
    [Header("Helicoid Properties")]
    public float helixPitch = 2.0f;
    public float rotationSpeed = 3600.0f; // Very fast - 10 rotations/sec for POV effect
    public int radialSegments = 64;
    public int heightSegments = 128;
    
    [Header("Projection Settings")]
    public ComputeShader projectionCompute;
    public Texture2D[] sliceTextures;
    public float projectionAngle = 0f; // Fixed projection angle (typically 0)
    public float projectionWidth = 0.3f; // Width of projection beam in radians
    public int bufferResolution = 512;
    
    [Header("Persistence Settings")]
    public float decayRate = 0.1f;  // Very slow - sphere stays visible
    public float emissionStrength = 15.0f;  // Very bright for bloom
    
    [Header("Visualization Mode")]
    public bool enhancedVisualizationMode = false; // Toggle for extreme blur/accumulation
    [Range(0.01f, 5.0f)]
    public float enhancedDecayRate = 0.02f; // Even slower decay in visualization mode
    [Range(1.0f, 50.0f)]
    public float enhancedEmissionStrength = 30.0f; // Super bright in visualization mode
    
    [Header("Display Settings")]
    public Material displayMaterial;
    public bool enableGlow = true;
    
    [Header("Debug Visualization")]
    public bool showPersistenceBuffer = false; // Show raw buffer contents
    public bool showTimeBuffer = false; // Show when pixels were last hit
    
    private RenderTexture persistenceBuffer;
    private RenderTexture timeBuffer;
    private float currentRotation = 0f;
    private int kernelHandle;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    void Start()
    {
        GenerateHelicoid();
        InitializeBuffers();
        SetupMaterial();
        
        if (projectionCompute != null)
        {
            kernelHandle = projectionCompute.FindKernel("CSMain");
        }
    }
    
    void GenerateHelicoid()
    {
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        
        Mesh mesh = new Mesh();
        mesh.name = "Helicoid";
        
        int vertexCount = (radialSegments + 1) * (heightSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        
        // Generate vertices for a TRUE HELICOID
        // Parametric equations: x = u * cos(v), y = u * sin(v), z = c * v
        // where u = radius (0 to 1), v = angle (0 to 2π per turn)
        
        for (int h = 0; h <= heightSegments; h++)
        {
            for (int r = 0; r <= radialSegments; r++)
            {
                // v parameter: angle that increases with height
                // This creates the twist
                float heightParam = (float)h / heightSegments;
                float v = heightParam * Mathf.PI * 2f; // One full rotation over the height
                
                // u parameter: radius from center to edge
                float u = (float)r / radialSegments; // 0 to 1
                
                // z is simply the height
                float z = heightParam * helixPitch;
                
                // Helicoid surface equations
                float x = u * Mathf.Cos(v);
                float y = u * Mathf.Sin(v);
                
                int index = h * (radialSegments + 1) + r;
                vertices[index] = new Vector3(x, y, z);
                
                // UV MAPPING - CRITICAL:
                // U = radius (r / radialSegments) - 0 at center, 1 at edge
                // V = height - 0 at bottom, 1 at top
                // This maps the buffer as (radius, height)
                uvs[index] = new Vector2((float)r / radialSegments, heightParam);
            }
        }
        
        // Generate triangles
        int[] triangles = new int[radialSegments * heightSegments * 6];
        int triIndex = 0;
        
        for (int h = 0; h < heightSegments; h++)
        {
            for (int r = 0; r < radialSegments; r++)
            {
                int current = h * (radialSegments + 1) + r;
                int next = current + radialSegments + 1;
                
                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = current + 1;
                
                triangles[triIndex++] = current + 1;
                triangles[triIndex++] = next;
                triangles[triIndex++] = next + 1;
            }
        }
        
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        
        // Debug: Check UV range
        float minU = 1f, maxU = 0f, minV = 1f, maxV = 0f;
        foreach (var uv in uvs)
        {
            minU = Mathf.Min(minU, uv.x);
            maxU = Mathf.Max(maxU, uv.x);
            minV = Mathf.Min(minV, uv.y);
            maxV = Mathf.Max(maxV, uv.y);
        }
        Debug.Log($"Helicoid UV range: U=[{minU:F2}, {maxU:F2}], V=[{minV:F2}, {maxV:F2}]");
    }
    
    void InitializeBuffers()
    {
        persistenceBuffer = new RenderTexture(bufferResolution, bufferResolution, 0, RenderTextureFormat.ARGBFloat);
        persistenceBuffer.enableRandomWrite = true;
        persistenceBuffer.Create();
        
        timeBuffer = new RenderTexture(bufferResolution, bufferResolution, 0, RenderTextureFormat.RFloat);
        timeBuffer.enableRandomWrite = true;
        timeBuffer.Create();
        
        // Initialize time buffer to -100 (very old)
        RenderTexture.active = timeBuffer;
        GL.Clear(true, true, new Color(-100, -100, -100, -100));
        RenderTexture.active = null;
        
        Debug.Log($"Initialized buffers: {bufferResolution}x{bufferResolution}, Helicoid mesh: {radialSegments} radial x {heightSegments} height segments");
    }
    
    void SetupMaterial()
    {
        if (displayMaterial == null)
        {
            // Auto-create material if not assigned
            Shader shader = Shader.Find("Custom/HelicoidDisplay");
            if (shader == null)
            {
                Debug.LogError("HelicoidDisplay shader not found! Make sure HelicoidDisplay.shader is in your project.");
                return;
            }
            
            displayMaterial = new Material(shader);
            displayMaterial.name = "HelicoidDisplayMaterial (Auto-created)";
            Debug.Log("Auto-created display material using HelicoidDisplay shader");
        }
        
        meshRenderer.material = displayMaterial;
        displayMaterial.SetTexture("_PersistenceTex", persistenceBuffer);
        displayMaterial.SetTexture("_TimeTex", timeBuffer);
        displayMaterial.SetFloat("_DecayRate", decayRate);
        displayMaterial.SetFloat("_EmissionStrength", emissionStrength);
    }
    
    void Update()
    {
        // Rotate the helicoid
        currentRotation += rotationSpeed * Time.deltaTime * Mathf.Deg2Rad;
        currentRotation = currentRotation % (Mathf.PI * 2f);
        
        // Project ALL slices based on their height and current rotation
        ProjectAllSlices();
        
        // Apply visualization mode settings
        float activeDecayRate = enhancedVisualizationMode ? enhancedDecayRate : decayRate;
        float activeEmissionStrength = enhancedVisualizationMode ? enhancedEmissionStrength : emissionStrength;
        
        // Update material properties
        displayMaterial.SetFloat("_CurrentTime", Time.time);
        displayMaterial.SetFloat("_DecayRate", activeDecayRate);
        displayMaterial.SetFloat("_EmissionStrength", activeEmissionStrength);
        
        // Rotate the actual GameObject for visual effect
        transform.rotation = Quaternion.Euler(0, 0, currentRotation * Mathf.Rad2Deg);
    }
    
    void ProjectAllSlices()
    {
        if (projectionCompute == null)
        {
            Debug.LogError("Projection compute shader is not assigned!");
            return;
        }
        
        if (sliceTextures == null || sliceTextures.Length == 0)
        {
            Debug.LogError("No slice textures assigned!");
            return;
        }
        
        // Debug log once at start
        if (Time.frameCount == 10)
        {
            Debug.Log($"Projecting {sliceTextures.Length} slices, buffer resolution: {bufferResolution}x{bufferResolution}");
        }
        
        // Set common compute shader parameters
        projectionCompute.SetTexture(kernelHandle, "_PersistenceBuffer", persistenceBuffer);
        projectionCompute.SetTexture(kernelHandle, "_TimeBuffer", timeBuffer);
        projectionCompute.SetFloat("_CurrentRotation", currentRotation);
        projectionCompute.SetFloat("_ProjectionAngle", projectionAngle);
        projectionCompute.SetFloat("_ProjectionWidth", projectionWidth);
        projectionCompute.SetFloat("_CurrentTime", Time.time);
        projectionCompute.SetFloat("_HelixPitch", helixPitch);
        projectionCompute.SetInt("_BufferWidth", bufferResolution);
        projectionCompute.SetInt("_BufferHeight", bufferResolution);
        
        int slicesProjected = 0;
        
        // TEMPORARY DEBUG: Project ALL slices every frame to test
        for (int i = 0; i < sliceTextures.Length; i++)
        {
            if (sliceTextures[i] == null) continue;
            
            // Calculate the height for this slice
            float sliceHeight = ((float)i / sliceTextures.Length) * helixPitch;
            
            projectionCompute.SetTexture(kernelHandle, "_CurrentSlice", sliceTextures[i]);
            projectionCompute.SetFloat("_SliceHeight", sliceHeight);
            
            projectionCompute.Dispatch(kernelHandle, bufferResolution / 8, bufferResolution / 8, 1);
            slicesProjected++;
        }
        
        // Debug output every 60 frames
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"Frame {Time.frameCount}: Dispatched {slicesProjected}/{sliceTextures.Length} slices, rotation {currentRotation * Mathf.Rad2Deg:F1}°, projection angle {projectionAngle * Mathf.Rad2Deg:F1}°");
        }
    }
    
    // Additional debug to show slice distribution
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || sliceTextures == null) return;
        
        // Draw a line showing the current projection angle
        Vector3 projectionDirection = new Vector3(
            Mathf.Cos(projectionAngle), 
            Mathf.Sin(projectionAngle), 
            0
        );
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + projectionDirection * 2f);
        
        // Draw the current rotation
        Vector3 rotationDirection = new Vector3(
            Mathf.Cos(currentRotation), 
            Mathf.Sin(currentRotation), 
            0
        );
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + rotationDirection * 1.5f);
    }
    
    void OnDestroy()
    {
        if (persistenceBuffer != null) persistenceBuffer.Release();
        if (timeBuffer != null) timeBuffer.Release();
    }
    
    // Debug visualization
    void OnGUI()
    {
        if (!enableGlow) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.Label($"Rotation: {(currentRotation * Mathf.Rad2Deg) % 360f:F1}°");
        GUILayout.Label($"Active Slices: {sliceTextures.Length}");
        GUILayout.Label($"FPS: {1f / Time.deltaTime:F0}");
        GUILayout.Label($"Decay Rate: {decayRate:F2}");
        GUILayout.Label($"Emission: {emissionStrength:F1}");
        GUILayout.EndArea();
        
        // Show buffer previews if enabled
        if (showPersistenceBuffer && persistenceBuffer != null)
        {
            GUI.DrawTexture(new Rect(Screen.width - 260, 10, 250, 250), persistenceBuffer);
            GUI.Label(new Rect(Screen.width - 260, 265, 250, 20), "Persistence Buffer");
        }
        
        if (showTimeBuffer && timeBuffer != null)
        {
            GUI.DrawTexture(new Rect(Screen.width - 260, 290, 250, 250), timeBuffer);
            GUI.Label(new Rect(Screen.width - 260, 545, 250, 20), "Time Buffer");
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectionUIController : MonoBehaviour
{
    [Header("UI Refs")]
    public Button browseButton;
    public TMP_InputField pathField;
    public Slider rpmSlider;
    public TMP_Text rpmValueText;
    public Slider helicoidSlider;
    public TMP_Text helicoidValueText;
    public Button applyButton;
    public TMP_Text statusText;

    [Header("External")]
    [SerializeField] private OBJImporter objImporter;              // <- drag in Inspector
    [SerializeField] private MonoBehaviour receiverBehaviour;      // <- HelicoidProjectionReceiver

    IProjectionInputReceiver receiver; 

    public event Action<Mesh, float, float> OnInputApplied;

    Mesh loadedMesh;
    string loadedPath;

    void Awake()
    {
        receiver = receiverBehaviour as IProjectionInputReceiver;
        if (receiver == null && receiverBehaviour != null)
            Debug.LogError("receiverBehaviour does not implement IProjectionInputReceiver");

        browseButton.onClick.AddListener(Browse);
        rpmSlider.onValueChanged.AddListener(v => rpmValueText.text = $"{v:0} rpm");
        helicoidSlider.onValueChanged.AddListener(v => helicoidValueText.text = $"{v:0.00} scale");

        applyButton.onClick.AddListener(Apply);

        rpmValueText.text = $"{rpmSlider.value:0} rpm";
        helicoidValueText.text = $"{helicoidSlider.value:0.00}";
        statusText.text = "No mesh loaded.";
    }

    void Browse()
    {
    #if UNITY_EDITOR
        string p = UnityEditor.EditorUtility.OpenFilePanel(
            "Select mesh (OBJ/STL)", "", "obj,stl");
        if (!string.IsNullOrEmpty(p))
        {
            pathField.text = p;
            TryLoad(p);
        }
    #else
        statusText.text = "Builds: paste a full file path then press Apply.";
    #endif
    }

    void TryLoad(string path)
    {
        try
        {
            string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            loadedPath = path;

            if (ext == ".obj")
            {
                loadedMesh = MeshLoader.LoadObj(path);
            }
            else if (ext == ".stl")
            {
                loadedMesh = MeshLoader.LoadStl(path);
            }

            else
            {
                loadedMesh = null;
                statusText.text = $"Unsupported file: {ext}. Use OBJ or STL.";
                return;
            }

            statusText.text = $"Loaded: {System.IO.Path.GetFileName(path)}";
        }
        catch (Exception e)
        {
            loadedMesh = null;
            statusText.text = $"Load failed: {e.Message}";
            Debug.LogException(e);
        }
    }


    void Apply()
    {
        if (loadedMesh == null && !string.IsNullOrWhiteSpace(pathField.text))
            TryLoad(pathField.text);

        if (loadedMesh == null)
        {
            statusText.text = "No mesh loaded.";
            return;
        }

        float rpm = rpmSlider.value;
        float helicoidSize = helicoidSlider.value;

        statusText.text = $"Applied: {loadedMesh.name}, {rpm:0} rpm, size {helicoidSize:0.00}";
        OnInputApplied?.Invoke(loadedMesh, rpm, helicoidSize);

        if (receiver != null)
        {
            receiver.ApplyProjectionInput(loadedMesh, rpm, helicoidSize);
        }
        else
        {
            Debug.LogWarning("ProjectionUIController: no IProjectionInputReceiver assigned.");
        }
    }

}

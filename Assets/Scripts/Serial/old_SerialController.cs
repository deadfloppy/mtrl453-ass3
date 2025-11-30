using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO.Ports;
using System.Threading;

public class SerialController : MonoBehaviour
{
    // Configuration - set these before Start() if needed, or use defaults
    private string readPortName = "/dev/cu.usbmodem156549603";    // Port for reading telemetry
    private string writePortName = "/dev/cu.usbmodem156549601";   // Port for writing commands
    private int baudRate = 115200;
    private float commandSendRate = 1f; // How often to send speed command (Hz)

    private SerialPort readPort;
    private SerialPort writePort;
    private Thread readThread;
    private bool running = false;

    private HelicoidModelController helicoidController;
    private float lastSendTime;

    // Buffer for accumulating serial data
    private System.Text.StringBuilder serialBuffer = new System.Text.StringBuilder();

    // UI components - created at runtime
    private Canvas canvas;
    private Text speedDisplayText;
    private Text degreesDisplayText;

    // Telemetry data (thread-safe)
    private float currentSpeed;
    private float currentDegrees;
    private readonly object dataLock = new object();

    public float CurrentSpeed
    {
        get { lock (dataLock) { return currentSpeed; } }
        private set { lock (dataLock) { currentSpeed = value; } }
    }

    public float CurrentDegrees
    {
        get { lock (dataLock) { return currentDegrees; } }
        private set { lock (dataLock) { currentDegrees = value; } }
    }

    // Alias for backwards compatibility
    public float CurrentAngle => CurrentDegrees;

    // Public methods to configure ports before Start() is called
    public void ConfigurePorts(string readPort, string writePort, int baud = 115200)
    {
        this.readPortName = readPort;
        this.writePortName = writePort;
        this.baudRate = baud;
    }

    public void SetCommandRate(float hz)
    {
        this.commandSendRate = hz;
    }

    // Public method to manually send speed command
    public void SendSpeed(float speed)
    {
        if (writePort == null || !writePort.IsOpen)
        {
            Debug.LogError("SerialController: Write port is not open!");
            return;
        }

        try
        {
            string message = $"$speed({speed})\n";
            writePort.Write(message);
            Debug.Log($"SerialController: Manually sent {message.Trim()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SerialController: Failed to send speed command: {e.Message}");
        }
    }

    void Start()
    {
        // Get the HelicoidModelController component from the same GameObject
        helicoidController = GetComponent<HelicoidModelController>();
        if (helicoidController == null)
        {
            Debug.LogError("SerialController: HelicoidModelController not found on the same GameObject!");
            enabled = false;
            return;
        }

        // Create UI
        CreateUI();

        // Open read port
        try
        {
            readPort = new SerialPort(readPortName, baudRate);
            readPort.ReadTimeout = 50;
            readPort.Open();
            Debug.Log($"SerialController: Opened read port {readPortName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SerialController: Failed to open read port {readPortName}: {e.Message}");
            enabled = false;
            return;
        }

        // Open write port
        try
        {
            writePort = new SerialPort(writePortName, baudRate);
            writePort.Open();
            Debug.Log($"SerialController: Opened write port {writePortName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SerialController: Failed to open write port {writePortName}: {e.Message}");
            if (readPort != null && readPort.IsOpen)
                readPort.Close();
            enabled = false;
            return;
        }

        // Start read thread
        running = true;
        readThread = new Thread(ReadSerialLoop);
        readThread.Start();

        Debug.Log("SerialController: Started successfully");
    }

    void Update()
    {
        // Send speed command at specified rate
        if (Time.time - lastSendTime >= (1f / commandSendRate))
        {
            lastSendTime = Time.time;
            SendSpeedCommand();
        }

        // Update UI with current telemetry every frame
        UpdateUI();
    }

    void OnDestroy()
    {
        // Stop the read thread
        running = false;
        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(1000); // Wait up to 1 second
        }

        // Close ports
        if (readPort != null && readPort.IsOpen)
        {
            readPort.Close();
            Debug.Log("SerialController: Read port closed");
        }

        if (writePort != null && writePort.IsOpen)
        {
            writePort.Close();
            Debug.Log("SerialController: Write port closed");
        }

        // Destroy UI
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }

    // Create UI overlay at runtime
    private void CreateUI()
    {
        // Check if there's already a Canvas in the scene
        canvas = FindObjectOfType<Canvas>();

        // If no canvas exists, create one
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("SerialController_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create panel for telemetry display
        GameObject panel = new GameObject("TelemetryPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(10, -10);
        panelRect.sizeDelta = new Vector2(300, 100);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);

        // Create speed text
        GameObject speedTextObj = new GameObject("SpeedText");
        speedTextObj.transform.SetParent(panel.transform, false);

        RectTransform speedRect = speedTextObj.AddComponent<RectTransform>();
        speedRect.anchorMin = new Vector2(0, 0.5f);
        speedRect.anchorMax = new Vector2(1, 1);
        speedRect.pivot = new Vector2(0.5f, 0.5f);
        speedRect.anchoredPosition = Vector2.zero;
        speedRect.sizeDelta = new Vector2(-20, -10);

        speedDisplayText = speedTextObj.AddComponent<Text>();
        speedDisplayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        speedDisplayText.fontSize = 18;
        speedDisplayText.color = Color.white;
        speedDisplayText.alignment = TextAnchor.MiddleLeft;
        speedDisplayText.text = "Speed: 0.00 RPM";

        // Create degrees text
        GameObject degreesTextObj = new GameObject("DegreesText");
        degreesTextObj.transform.SetParent(panel.transform, false);

        RectTransform degreesRect = degreesTextObj.AddComponent<RectTransform>();
        degreesRect.anchorMin = new Vector2(0, 0);
        degreesRect.anchorMax = new Vector2(1, 0.5f);
        degreesRect.pivot = new Vector2(0.5f, 0.5f);
        degreesRect.anchoredPosition = Vector2.zero;
        degreesRect.sizeDelta = new Vector2(-20, -10);

        degreesDisplayText = degreesTextObj.AddComponent<Text>();
        degreesDisplayText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        degreesDisplayText.fontSize = 18;
        degreesDisplayText.color = Color.white;
        degreesDisplayText.alignment = TextAnchor.MiddleLeft;
        degreesDisplayText.text = "Degrees: 0.00°";

        Debug.Log("SerialController: UI created");
    }

    // Send $speed(rotationSpeed) command to write port
    private void SendSpeedCommand()
    {
        if (writePort == null || !writePort.IsOpen || helicoidController == null)
            return;

        try
        {
            float speed = helicoidController.rotationSpeed;
            string message = $"$speed({speed})\n";
            writePort.Write(message);
            Debug.Log($"SerialController: Sent {message.Trim()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SerialController: Failed to send speed command: {e.Message}");
        }
    }

    // Read thread - continuously reads from read port
    private void ReadSerialLoop()
    {
        Debug.Log("SerialController: Read thread started");
        int bytesReadCount = 0;

        while (running)
        {
            try
            {
                if (readPort != null && readPort.IsOpen)
                {
                    string line = readPort.ReadLine().Trim();
                    bytesReadCount++;

                    Debug.Log($"SerialController: [{bytesReadCount}] Raw: '{line}'");

                    // Remove "serial2: " prefix if present
                    if (line.StartsWith("serial2: "))
                    {
                        line = line.Substring(9); // Remove "serial2: "
                    }

                    // Accumulate characters in buffer
                    if (line.Length > 0)
                    {
                        serialBuffer.Append(line);
                        string bufferedData = serialBuffer.ToString();
                        Debug.Log($"SerialController: Buffer now: '{bufferedData}'");

                        // Check if we have a complete message
                        if (bufferedData.Contains("$current(") && bufferedData.Contains(")"))
                        {
                            Debug.Log($"SerialController: Complete message received: '{bufferedData}'");

                            // Find the start and end of the message
                            int startIdx = bufferedData.IndexOf("$current(");
                            int endIdx = bufferedData.IndexOf(")", startIdx);

                            if (startIdx >= 0 && endIdx > startIdx)
                            {
                                // Extract the complete message
                                string message = bufferedData.Substring(startIdx, endIdx - startIdx + 1);

                                // Clear the buffer up to and including this message
                                serialBuffer.Remove(0, endIdx + 1);

                                // Parse message: $current(rotationSpeed, degrees)
                                string content = message.Substring(9, message.Length - 10); // Remove "$current(" and ")"
                                string[] parts = content.Split(',');

                                if (parts.Length == 2)
                                {
                                    if (float.TryParse(parts[0].Trim(), out float speed) &&
                                        float.TryParse(parts[1].Trim(), out float degrees))
                                    {
                                        CurrentSpeed = speed;
                                        CurrentDegrees = degrees;

                                        Debug.Log($"SerialController: *** UPDATED VALUES *** speed={speed}, degrees={degrees}");
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"SerialController: Failed to parse values: '{parts[0].Trim()}', '{parts[1].Trim()}'");
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning($"SerialController: Expected 2 parts, got {parts.Length}");
                                }
                            }
                        }

                        // Prevent buffer from growing too large
                        if (serialBuffer.Length > 200)
                        {
                            Debug.LogWarning("SerialController: Buffer overflow, clearing old data");
                            serialBuffer.Clear();
                        }
                    }
                }
            }
            catch (TimeoutException)
            {
                // Normal timeout, continue (this happens frequently)
            }
            catch (Exception e)
            {
                if (running) // Only log if we're still supposed to be running
                {
                    Debug.LogError($"SerialController: Read error: {e.Message}");
                }
            }
        }

        Debug.Log("SerialController: Read thread stopped");
    }

    // Update UI components with current telemetry
    private void UpdateUI()
    {
        if (speedDisplayText != null)
        {
            speedDisplayText.text = $"Speed: {CurrentSpeed:F2} RPM";
        }
        else
        {
            Debug.LogError("SerialController: speedDisplayText is NULL in UpdateUI!");
        }

        if (degreesDisplayText != null)
        {
            degreesDisplayText.text = $"Degrees: {CurrentDegrees:F2}°";
        }
        else
        {
            Debug.LogError("SerialController: degreesDisplayText is NULL in UpdateUI!");
        }
    }
}

using UnityEngine;
using System.IO.Ports;
using System.Threading;
using System.Text;

public class SpeedSerialReader : MonoBehaviour
{
    private SerialPort readSerialPort;
    private SerialPort writeSerialPort;
    private Thread readThread;
    private Thread writeThread;
    private bool isReading = false;
    private bool isWriting = false;

    private string readPortName = "/dev/cu.usbmodem156549603";
    private string writePortName = "/dev/cu.usbmodem156549601"; // Change to your write port
    private int baudRate = 9600;
    private float writeInterval = 1.0f; // 1 Hz = 1 second interval

    private StringBuilder readBuffer = new StringBuilder();

    void Start()
    {
        // Open read port
        try
        {
            readSerialPort = new SerialPort(readPortName, baudRate);
            readSerialPort.ReadTimeout = 100;
            readSerialPort.NewLine = "\n"; // Set newline character
            readSerialPort.DtrEnable = true;
            readSerialPort.RtsEnable = true;
            readSerialPort.Open();

            isReading = true;
            readThread = new Thread(ReadSerialData);
            readThread.Start();

            Debug.Log($"Read serial port {readPortName} opened successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open read serial port: {e.Message}");
        }

        // Open write port
        try
        {
            writeSerialPort = new SerialPort(writePortName, baudRate);
            writeSerialPort.Open();

            isWriting = true;
            writeThread = new Thread(WriteSerialData);
            writeThread.Start();

            Debug.Log($"Write serial port {writePortName} opened successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to open write serial port: {e.Message}");
        }
    }

    void ReadSerialData()
    {
        Debug.Log("Read thread started");
        while (isReading && readSerialPort != null && readSerialPort.IsOpen)
        {
            try
            {
                // Check if there's data available
                int bytesAvailable = readSerialPort.BytesToRead;
                if (bytesAvailable > 0)
                {
                    //Debug.Log($"Bytes available: {bytesAvailable}");

                    // Read one character at a time
                    char c = (char)readSerialPort.ReadChar();
                    //Debug.Log($"Read char: '{c}' (ASCII: {(int)c})");

                    // Check for newline or carriage return
                    if (c == '\n' || c == '\r')
                    {
                        // Process the complete message if buffer has content
                        if (readBuffer.Length > 0)
                        {
                            string message = readBuffer.ToString();
                            //Debug.Log($"Complete message assembled: {message}");
                            ProcessMessage(message);
                            readBuffer.Clear();
                        }
                    }
                    else
                    {
                        // Add character to buffer
                        readBuffer.Append(c);
                    }
                }
                else
                {
                    Thread.Sleep(10); // Small delay when no data available
                }
            }
            catch (System.TimeoutException)
            {
                // Timeout is expected when no data
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error reading serial data: {e.Message}");
                Thread.Sleep(100);
            }
        }
        Debug.Log("Read thread stopped");
    }

    void ProcessMessage(string message)
    {
        // Always print what we received
        Debug.Log($"Received: {message}");

        // Check if message matches the pattern $speed(value)
        if (message.StartsWith("$speed(") && message.EndsWith(")"))
        {
            // Extract the value between parentheses
            int startIndex = message.IndexOf('(') + 1;
            int endIndex = message.IndexOf(')');
            string valueStr = message.Substring(startIndex, endIndex - startIndex);

            Debug.Log($"Parsed speed value: {valueStr}");
        }
    }

    void WriteSerialData()
    {
        int messageCounter = 0;
        while (isWriting && writeSerialPort != null && writeSerialPort.IsOpen)
        {
            try
            {
                string message = $"$setspeed({messageCounter})\n";
                writeSerialPort.Write(message);
                Debug.Log($"Sent: {message.Trim()}");
                messageCounter++;

                // Sleep for 1 second (1 Hz)
                Thread.Sleep((int)(writeInterval * 1000));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error writing serial data: {e.Message}");
                Thread.Sleep(1000); // Wait before retrying
            }
        }
    }

    void OnApplicationQuit()
    {
        CloseSerialPort();
    }

    void OnDestroy()
    {
        CloseSerialPort();
    }

    void CloseSerialPort()
    {
        isReading = false;
        isWriting = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(1000);
        }

        if (writeThread != null && writeThread.IsAlive)
        {
            writeThread.Join(1000);
        }

        if (readSerialPort != null && readSerialPort.IsOpen)
        {
            readSerialPort.Close();
            Debug.Log("Read serial port closed");
        }

        if (writeSerialPort != null && writeSerialPort.IsOpen)
        {
            writeSerialPort.Close();
            Debug.Log("Write serial port closed");
        }
    }
}

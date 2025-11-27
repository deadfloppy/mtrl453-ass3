using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using UnityEngine;

public class SerialPortController : MonoBehaviour
{
    // Teensy Vendor ID and Product ID
    private const string TEENSY_VID = "16C0";
    private const string TEENSY_PID = "0478";  // Default Serial/MTP. Change if using HID/MIDI/etc.

    private SerialPort serialPort;
    private HelicoidModelController helicoidController;
    private float lastSendTime;
    private const float SEND_INTERVAL = 1f; // 1 Hz

    void Start()
    {
        // Get the HelicoidModelController component from the same GameObject
        helicoidController = GetComponent<HelicoidModelController>();
        if (helicoidController == null)
        {
            Debug.LogError("SerialPortController: HelicoidModelController not found on the same GameObject!");
            enabled = false;
            return;
        }

        // Find and open the Teensy port
        string portName = FindTeensyPort();
        if (string.IsNullOrEmpty(portName))
        {
            Debug.LogError("SerialPortController: Teensy port not found!");
            enabled = false;
            return;
        }

        try
        {
            serialPort = new SerialPort(portName, 115200); // Common baud rate for Teensy
            serialPort.Open();
            Debug.Log($"SerialPortController: Successfully opened port {portName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SerialPortController: Failed to open port {portName}: {e.Message}");
            enabled = false;
        }
    }

    void Update()
    {
        if (serialPort == null || !serialPort.IsOpen)
            return;

        // Send message at 1 Hz
        if (Time.time - lastSendTime >= SEND_INTERVAL)
        {
            lastSendTime = Time.time;

            float speed = helicoidController.rotationSpeed;
            string message = $"$speed({speed})\n";

            try
            {
                serialPort.Write(message);
                Debug.Log($"SerialPortController: Sent {message.Trim()}");
            }
            catch (Exception e)
            {
                Debug.LogError($"SerialPortController: Failed to send message: {e.Message}");
            }
        }
    }

    void OnDestroy()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("SerialPortController: Serial port closed");
        }
    }

    private static string FindTeensyPort()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return FindTeensyWindows();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        return FindTeensyMacOS();
#else
        Debug.LogError("TeensyPortFinder: Unsupported platform");
        return null;
#endif
    }

    // ===========================
    // Windows Registry Scan
    // ===========================
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static string FindTeensyWindows()
    {
        try
        {
            var baseKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Enum\USB");

            if (baseKey == null)
                return null;

            foreach (var dev in baseKey.GetSubKeyNames())
            {
                if (!dev.Contains("VID_" + TEENSY_VID, StringComparison.OrdinalIgnoreCase) ||
                    !dev.Contains("PID_" + TEENSY_PID, StringComparison.OrdinalIgnoreCase))
                    continue;

                var vidpid = baseKey.OpenSubKey(dev);
                foreach (var instance in vidpid.GetSubKeyNames())
                {
                    var instKey = vidpid.OpenSubKey(instance);
                    var deviceParams = instKey.OpenSubKey("Device Parameters");

                    string portName = deviceParams?.GetValue("PortName") as string;

                    if (!string.IsNullOrEmpty(portName))
                        return portName;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("TeensyPortFinder Windows error: " + e);
        }

        return null;
    }
#endif

    // ===========================
    // macOS IORegistry Scan
    // ===========================
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
    private static string FindTeensyMacOS()
    {
        try
        {
            // List all USB modem ports (Teensy uses these)
            string[] ports = Directory.GetFiles("/dev", "tty.usbmodem*");

            foreach (string port in ports)
            {
                if (IsTeensyMacOS(port))
                    return port;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("TeensyPortFinder macOS error: " + e);
        }

        return null;
    }

    private static bool IsTeensyMacOS(string port)
    {
        try
        {
            // Query IORegistry for VID/PID of this device
            string cmd = $"ioreg -p IOUSB -l | grep -A5 '{Path.GetFileName(port)}'";

            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{cmd}\"";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Contains("idVendor = " + Convert.ToInt32(TEENSY_VID, 16)) &&
                   output.Contains("idProduct = " + Convert.ToInt32(TEENSY_PID, 16));
        }
        catch
        {
            return false;
        }
    }
#endif
}

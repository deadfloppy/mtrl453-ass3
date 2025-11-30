using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;

/// <summary>
/// Real serial implementation (Windows / Windows Editor only)
/// </summary>
public class SerialPortController : MonoBehaviour
{
    [Header("Serial connection")]
    [Tooltip("If false, no serial port will be opened or used.")]
    public bool serialEnabled = false;

    // Teensy Vendor ID and Product ID
    private const string TEENSY_VID = "16C0";
    private const string TEENSY_PID = "0478";  // Default Serial/MTP. Change if using HID/MIDI/etc.

    private SerialPort serialPort;
    private HelicoidModelController helicoidController;
    private float lastSendTime;
    private const float SEND_INTERVAL = 1f; // 1 Hz

    void Start()
    {
        if (!serialEnabled)
        {
            Debug.Log("[SerialPortController] Serial disabled, not opening port.");
            return;
        }

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
        if (!serialEnabled)
            return;

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
        // On Windows we only use the Windows implementation;
        // if someone later wants macOS support with System.IO.Ports
        // they can extend this method and the compile guards.
        return FindTeensyWindows();
    }

    // ===========================
    // Windows Registry Scan
    // ===========================
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
}

#else
/// <summary>
/// Dummy serial controller for platforms where System.IO.Ports is unavailable (e.g. macOS Unity).
/// Keeps the same public API so scenes don't break, but does nothing.
/// </summary>
public class SerialPortController : MonoBehaviour
{
    [Header("Serial connection")]
    public bool serialEnabled = false;

    void Start()
    {
        if (serialEnabled)
        {
            Debug.LogWarning("[SerialPortController] Serial is enabled, but this platform does not support System.IO.Ports in Unity.");
        }
    }

    void Update() { }
    void OnDestroy() { }
}
#endif

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// ============================================================
// 필수 설정!
// Edit > Project Settings > Player > Other Settings
// Api Compatibility Level → .NET Framework
// ============================================================

public class SerialController : MonoBehaviour
{
    public static SerialController Instance { get; private set; }

    [Header("Serial Settings")]
    public string portName = "COM3";
    public int baudRate = 9600;
    public bool autoConnect = true;

    [Header("Status")]
    public bool isConnected = false;
    public string lastReceivedData = "";

    // Events
    public event Action<float, float> OnSensorDataReceived;
    public event Action<int, int, int> OnLEDStateReceived;
    public event Action<int, string> OnServoAngleReceived;

    private Thread readThread;
    private volatile bool keepReading = false;
    private Queue<string> dataQueue = new Queue<string>();
    private readonly object queueLock = new object();

    // SerialPort는 reflection으로 처리
    private object serialPort;
    private System.Type serialPortType;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (autoConnect)
        {
            Connect();
        }
    }

    void Update()
    {
        ProcessQueue();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    public void Connect()
    {
        if (isConnected) return;

        try
        {
            // Reflection으로 SerialPort 접근
            serialPortType = System.Type.GetType("System.IO.Ports.SerialPort, System");
            
            if (serialPortType == null)
            {
                Debug.LogError("[Serial] SerialPort not available. Check API Compatibility Level → .NET Framework");
                return;
            }

            // Get available ports
            var getPortsMethod = serialPortType.GetMethod("GetPortNames");
            string[] ports = (string[])getPortsMethod.Invoke(null, null);

            if (ports.Length == 0)
            {
                Debug.LogWarning("[Serial] No COM ports found!");
                return;
            }

            if (string.IsNullOrEmpty(portName) || !Array.Exists(ports, p => p == portName))
            {
                portName = ports[0];
                Debug.Log($"[Serial] Auto-selected port: {portName}");
            }

            // Create SerialPort instance
            serialPort = Activator.CreateInstance(serialPortType, portName, baudRate);
            
            // Set properties
            serialPortType.GetProperty("ReadTimeout").SetValue(serialPort, 100);
            serialPortType.GetProperty("WriteTimeout").SetValue(serialPort, 100);
            serialPortType.GetProperty("DtrEnable").SetValue(serialPort, true);
            serialPortType.GetProperty("RtsEnable").SetValue(serialPort, true);

            // Open
            serialPortType.GetMethod("Open").Invoke(serialPort, null);
            isConnected = true;

            keepReading = true;
            readThread = new Thread(ReadSerialThread);
            readThread.IsBackground = true;
            readThread.Start();

            Debug.Log($"[Serial] Connected to {portName} at {baudRate} baud");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Serial] Connection failed: {e.Message}");
            isConnected = false;
        }
    }

    public void Disconnect()
    {
        keepReading = false;

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join(500);
        }

        if (serialPort != null && serialPortType != null)
        {
            try
            {
                bool isOpen = (bool)serialPortType.GetProperty("IsOpen").GetValue(serialPort);
                if (isOpen)
                {
                    serialPortType.GetMethod("Close").Invoke(serialPort, null);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Serial] Error closing port: {e.Message}");
            }
        }

        isConnected = false;
        Debug.Log("[Serial] Disconnected");
    }

    private void ReadSerialThread()
    {
        var readLineMethod = serialPortType?.GetMethod("ReadLine");
        var isOpenProp = serialPortType?.GetProperty("IsOpen");

        while (keepReading)
        {
            try
            {
                if (serialPort != null && (bool)isOpenProp.GetValue(serialPort))
                {
                    string line = (string)readLineMethod.Invoke(serialPort, null);
                    if (!string.IsNullOrEmpty(line))
                    {
                        lock (queueLock)
                        {
                            dataQueue.Enqueue(line.Trim());
                        }
                    }
                }
            }
            catch (System.Reflection.TargetInvocationException tie)
            {
                if (!(tie.InnerException is TimeoutException))
                {
                    if (keepReading)
                        Debug.LogWarning($"[Serial] Read error: {tie.InnerException?.Message}");
                }
            }
            catch (Exception e)
            {
                if (keepReading)
                    Debug.LogWarning($"[Serial] Read error: {e.Message}");
            }

            Thread.Sleep(10);
        }
    }

    private void ProcessQueue()
    {
        lock (queueLock)
        {
            while (dataQueue.Count > 0)
            {
                string data = dataQueue.Dequeue();
                lastReceivedData = data;
                ParseData(data);
            }
        }
    }

    private void ParseData(string data)
    {
        try
        {
            Dictionary<string, string> values = new Dictionary<string, string>();
            string[] parts = data.Split(',');

            foreach (string part in parts)
            {
                string[] kv = part.Split(':');
                if (kv.Length == 2)
                {
                    values[kv[0].Trim()] = kv[1].Trim();
                }
            }

            if (values.ContainsKey("T") && values.ContainsKey("H"))
            {
                float temp = float.Parse(values["T"]);
                float humidity = float.Parse(values["H"]);
                OnSensorDataReceived?.Invoke(temp, humidity);
            }

            if (values.ContainsKey("R") && values.ContainsKey("G") && values.ContainsKey("B"))
            {
                int r = int.Parse(values["R"]);
                int g = int.Parse(values["G"]);
                int b = int.Parse(values["B"]);
                OnLEDStateReceived?.Invoke(r, g, b);
            }

            if (values.ContainsKey("ROT"))
            {
                int angle = int.Parse(values["ROT"]);
                string source = values.ContainsKey("SRC") ? values["SRC"] : "UNKNOWN";
                OnServoAngleReceived?.Invoke(angle, source);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Serial] Parse error: {e.Message} | Data: {data}");
        }
    }

    public void SendCommand(string command)
    {
        // VirtualArduino가 활성화되어 있으면 거기로 전송
        if (VirtualArduino.Instance != null && VirtualArduino.Instance.enableVirtualArduino)
        {
            VirtualArduino.Instance.ReceiveCommand(command);
            return;
        }
        
        if (!isConnected || serialPort == null)
        {
            Debug.LogWarning("[Serial] Cannot send - not connected");
            return;
        }

        try
        {
            var writeLineMethod = serialPortType.GetMethod("WriteLine", new[] { typeof(string) });
            writeLineMethod.Invoke(serialPort, new object[] { command });
            Debug.Log($"[Serial] Sent: {command}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Serial] Send error: {e.Message}");
        }
    }

    public void SendLEDCommand(int r, int g, int b, int brightness = 255, char mode = 'M')
    {
        string cmd = $"LED:{r},{g},{b},{brightness},{mode}";
        SendCommand(cmd);
    }

    public void SendServoAngle(int angle)
    {
        angle = Mathf.Clamp(angle, 0, 180);
        string cmd = $"ROT:{angle}";
        SendCommand(cmd);
    }

    // VirtualArduino에서 이벤트를 트리거하기 위한 public 메서드들
    public void TriggerSensorData(float temp, float humidity)
    {
        OnSensorDataReceived?.Invoke(temp, humidity);
    }
    
    public void TriggerLEDState(int r, int g, int b)
    {
        OnLEDStateReceived?.Invoke(r, g, b);
    }
    
    public void TriggerServoAngle(int angle, string source)
    {
        OnServoAngleReceived?.Invoke(angle, source);
    }

    public string[] GetAvailablePorts()
    {
        try
        {
            if (serialPortType == null)
            {
                serialPortType = System.Type.GetType("System.IO.Ports.SerialPort, System");
            }
            if (serialPortType != null)
            {
                var getPortsMethod = serialPortType.GetMethod("GetPortNames");
                return (string[])getPortsMethod.Invoke(null, null);
            }
        }
        catch { }
        return new string[0];
    }
}

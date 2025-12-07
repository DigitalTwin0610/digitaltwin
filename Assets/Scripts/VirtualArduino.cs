using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 가상 Arduino 시뮬레이터
/// 실제 Arduino 없이 Serial 양방향 통신을 완전히 테스트합니다.
/// 
/// 이 스크립트가 "가상 Arduino"로 동작합니다:
/// 1. Unity → Arduino: 명령 수신 로그 (LED, 서보)
/// 2. Arduino → Unity: 센서 데이터 송신 시뮬레이션
/// 3. 물리 다이얼 시뮬레이션: 슬라이더로 다이얼 입력 가장
/// </summary>
public class VirtualArduino : MonoBehaviour
{
    public static VirtualArduino Instance { get; private set; }
    
    [Header("가상 Arduino 활성화")]
    public bool enableVirtualArduino = true;
    
    [Header("=== 가상 센서 (DHT11) ===")]
    [Range(10, 35)] public float sensorTemperature = 22.5f;
    [Range(20, 80)] public float sensorHumidity = 45f;
    public bool autoFluctuation = true;  // 자동으로 값 변동
    
    [Header("=== 가상 다이얼 (가변저항) ===")]
    [Range(0, 180)] public int dialAngle = 90;
    public bool dialChanged = false;  // Inspector에서 체크하면 다이얼 입력 발생
    
    [Header("=== 가상 서보모터 상태 ===")]
    [SerializeField] private int currentServoAngle = 90;
    [SerializeField] private string lastServoSource = "NONE";
    
    [Header("=== 가상 LED 상태 ===")]
    [SerializeField] private int ledR = 0;
    [SerializeField] private int ledG = 255;
    [SerializeField] private int ledB = 0;
    [SerializeField] private int ledBrightness = 255;
    [SerializeField] private char ledMode = 'A';
    
    [Header("=== 통신 로그 ===")]
    [SerializeField] private string lastReceivedCommand = "";
    [SerializeField] private string lastSentData = "";
    
    [Header("=== 데이터 전송 주기 ===")]
    public float sensorUpdateInterval = 1.0f;  // 1초마다 센서 데이터 전송
    
    // 내부 상태
    private float lastSensorUpdate = 0f;
    private bool isConnected = false;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
    }
    
    void Start()
    {
        if (enableVirtualArduino)
        {
            StartCoroutine(InitializeVirtualArduino());
        }
    }
    
    IEnumerator InitializeVirtualArduino()
    {
        // SerialController가 초기화될 때까지 대기
        yield return new WaitForSeconds(0.5f);
        
        if (SerialController.Instance != null)
        {
            // SerialController의 연결 상태를 가상으로 설정
            Debug.Log("[VirtualArduino] 가상 Arduino 시작됨");
            Debug.Log("[VirtualArduino] === 양방향 통신 테스트 모드 ===");
            isConnected = true;
            
            // 초기 데이터 전송
            SendSensorData();
        }
        else
        {
            Debug.LogError("[VirtualArduino] SerialController.Instance가 없습니다!");
        }
    }
    
    void Update()
    {
        if (!enableVirtualArduino || !isConnected) return;
        
        // 센서 값 자동 변동
        if (autoFluctuation)
        {
            sensorTemperature += UnityEngine.Random.Range(-0.1f, 0.1f);
            sensorTemperature = Mathf.Clamp(sensorTemperature, 10f, 35f);
            
            sensorHumidity += UnityEngine.Random.Range(-0.5f, 0.5f);
            sensorHumidity = Mathf.Clamp(sensorHumidity, 20f, 80f);
        }
        
        // 주기적으로 센서 데이터 전송
        if (Time.time - lastSensorUpdate > sensorUpdateInterval)
        {
            lastSensorUpdate = Time.time;
            SendSensorData();
        }
        
        // 다이얼 변경 감지 (Inspector에서 체크)
        if (dialChanged)
        {
            dialChanged = false;
            SimulateDialInput();
        }
    }
    
    /// <summary>
    /// 가상 센서 데이터를 Unity로 전송 (Arduino → Unity)
    /// </summary>
    void SendSensorData()
    {
        // 실제 Arduino가 보내는 형식과 동일
        string data = $"T:{sensorTemperature:F1},H:{sensorHumidity:F0},R:{ledR},G:{ledG},B:{ledB},ROT:{currentServoAngle},SRC:{lastServoSource},S:OK";
        lastSentData = data;
        
        Debug.Log($"[VirtualArduino → Unity] {data}");
        
        // SerialController의 이벤트 트리거
        if (SerialController.Instance != null)
        {
            SerialController.Instance.TriggerSensorData(sensorTemperature, sensorHumidity);
            SerialController.Instance.TriggerLEDState(ledR, ledG, ledB);
        }
    }
    
    /// <summary>
    /// 가상 다이얼 입력 시뮬레이션 (물리 다이얼 → Unity)
    /// </summary>
    public void SimulateDialInput()
    {
        currentServoAngle = dialAngle;
        lastServoSource = "DIAL";
        
        string data = $"T:{sensorTemperature:F1},H:{sensorHumidity:F0},R:{ledR},G:{ledG},B:{ledB},ROT:{dialAngle},SRC:DIAL,S:OK";
        lastSentData = data;
        
        Debug.Log($"[VirtualArduino → Unity] 다이얼 입력: {dialAngle}° (물리 입력 시뮬레이션)");
        
        if (SerialController.Instance != null)
        {
            // 다이얼 입력이므로 DIAL 소스로 이벤트 발생
            SerialController.Instance.TriggerServoAngle(dialAngle, "DIAL");
        }
    }
    
    /// <summary>
    /// Unity에서 보낸 명령 수신 처리 (Unity → Arduino)
    /// 실제 SerialController.SendCommand()를 가로채서 처리
    /// </summary>
    public void ReceiveCommand(string command)
    {
        lastReceivedCommand = command;
        Debug.Log($"[Unity → VirtualArduino] 수신: {command}");
        
        // LED 명령 파싱: LED:R,G,B,Brightness,Mode
        if (command.StartsWith("LED:"))
        {
            string[] parts = command.Substring(4).Split(',');
            if (parts.Length >= 5)
            {
                ledR = int.Parse(parts[0]);
                ledG = int.Parse(parts[1]);
                ledB = int.Parse(parts[2]);
                ledBrightness = int.Parse(parts[3]);
                ledMode = parts[4][0];
                
                Debug.Log($"[VirtualArduino] LED 설정: R={ledR}, G={ledG}, B={ledB}, Mode={ledMode}");
            }
        }
        // 서보 명령 파싱: ROT:angle
        else if (command.StartsWith("ROT:"))
        {
            int angle = int.Parse(command.Substring(4));
            currentServoAngle = angle;
            lastServoSource = "UNITY";
            
            Debug.Log($"[VirtualArduino] 서보모터 회전: {angle}° (Unity에서 제어)");
            
            // 실제 Arduino는 이 명령을 받고 서보를 움직임
            // 그리고 상태를 다시 Unity로 보냄
            StartCoroutine(SendServoResponse(angle));
        }
    }
    
    /// <summary>
    /// 서보 명령에 대한 응답 (실제 Arduino 동작 시뮬레이션)
    /// </summary>
    IEnumerator SendServoResponse(int angle)
    {
        // 실제 서보가 움직이는 시간 시뮬레이션
        yield return new WaitForSeconds(0.05f);
        
        // Unity에서 온 명령이므로 소스는 UNITY
        if (SerialController.Instance != null)
        {
            SerialController.Instance.TriggerServoAngle(angle, "UNITY");
        }
    }
    
    /// <summary>
    /// GUI로 테스트 패널 표시
    /// </summary>
    void OnGUI()
    {
        if (!enableVirtualArduino) return;
        
        // 우측 상단에 가상 Arduino 상태 표시
        GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 310, 500));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== 가상 Arduino (회로 시뮬레이터) ===");
        GUILayout.Space(5);
        
        // 연결 상태
        GUI.color = isConnected ? Color.green : Color.red;
        GUILayout.Label(isConnected ? "● 연결됨" : "○ 연결 안됨");
        GUI.color = Color.white;
        
        GUILayout.Space(10);
        GUILayout.Label("--- 가상 DHT11 센서 ---");
        GUILayout.Label($"온도: {sensorTemperature:F1}°C");
        GUILayout.Label($"습도: {sensorHumidity:F0}%");
        
        GUILayout.Space(10);
        GUILayout.Label("--- 가상 서보모터 ---");
        GUILayout.Label($"현재 각도: {currentServoAngle}°");
        GUILayout.Label($"제어 소스: {lastServoSource}");
        
        GUILayout.Space(10);
        GUILayout.Label("--- 가상 LED ---");
        
        // LED 색상 미리보기
        GUI.color = new Color(ledR / 255f, ledG / 255f, ledB / 255f);
        GUILayout.Box("", GUILayout.Width(50), GUILayout.Height(30));
        GUI.color = Color.white;
        GUILayout.Label($"R:{ledR} G:{ledG} B:{ledB}");
        GUILayout.Label($"Mode: {(ledMode == 'A' ? "AUTO" : "MANUAL")}");
        
        GUILayout.Space(10);
        GUILayout.Label("--- 양방향 통신 테스트 ---");
        
        // 다이얼 입력 시뮬레이션
        GUILayout.Label($"다이얼 각도: {dialAngle}°");
        dialAngle = (int)GUILayout.HorizontalSlider(dialAngle, 0, 180);
        
        if (GUILayout.Button("다이얼 돌리기 (물리→가상)"))
        {
            SimulateDialInput();
        }
        
        GUILayout.Space(10);
        GUILayout.Label("--- 통신 로그 ---");
        GUILayout.Label($"마지막 수신: {TruncateString(lastReceivedCommand, 35)}");
        GUILayout.Label($"마지막 송신: {TruncateString(lastSentData, 35)}");
        
        GUILayout.Space(10);
        GUILayout.Label("--- 테스트 시나리오 ---");
        
        if (GUILayout.Button("① 물리 다이얼 0° → Unity"))
        {
            dialAngle = 0;
            SimulateDialInput();
        }
        
        if (GUILayout.Button("② 물리 다이얼 180° → Unity"))
        {
            dialAngle = 180;
            SimulateDialInput();
        }
        
        if (GUILayout.Button("③ 온도차 크게 (외출 위험)"))
        {
            sensorTemperature = 25f;
            // 외부 온도는 WeatherAPI에서 오므로 여기서는 실내만
            SendSensorData();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    string TruncateString(string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str)) return "(없음)";
        if (str.Length <= maxLength) return str;
        return str.Substring(0, maxLength) + "...";
    }
}

/// <summary>
/// SerialController 확장 - 가상 Arduino 연동
/// </summary>
public static class SerialControllerExtension
{
    /// <summary>
    /// SendCommand를 가로채서 VirtualArduino로 전달
    /// </summary>
    public static void SendToVirtualArduino(this SerialController controller, string command)
    {
        if (VirtualArduino.Instance != null && VirtualArduino.Instance.enableVirtualArduino)
        {
            VirtualArduino.Instance.ReceiveCommand(command);
        }
    }
}

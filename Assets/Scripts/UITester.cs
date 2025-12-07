using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 회로 연결 없이 UI 동작을 테스트하는 스크립트
/// 빈 GameObject에 추가하고 Play 모드에서 테스트하세요
/// </summary>
public class UITester : MonoBehaviour
{
    [Header("테스트 모드")]
    public bool enableTesting = true;
    
    [Header("시뮬레이션 데이터")]
    [Range(-10, 40)] public float indoorTemp = 22f;
    [Range(-10, 40)] public float outdoorTemp = 5f;
    [Range(0, 100)] public float indoorHumidity = 45f;
    [Range(0, 100)] public float outdoorHumidity = 60f;
    
    [Header("테스트 버튼 (Inspector에서 클릭)")]
    public bool testSensorData = false;
    public bool testWeatherData = false;
    public bool testServoAngle = false;
    public bool testLEDColor = false;
    
    [Header("서보 테스트")]
    [Range(0, 180)] public int testAngle = 90;
    
    [Header("LED 테스트")]
    [Range(0, 255)] public int testR = 255;
    [Range(0, 255)] public int testG = 128;
    [Range(0, 255)] public int testB = 64;

    void Update()
    {
        if (!enableTesting) return;
        
        // Inspector에서 체크박스 클릭하면 테스트 실행
        if (testSensorData)
        {
            testSensorData = false;
            SimulateSensorData();
        }
        
        if (testWeatherData)
        {
            testWeatherData = false;
            SimulateWeatherData();
        }
        
        if (testServoAngle)
        {
            testServoAngle = false;
            SimulateServoAngle();
        }
        
        if (testLEDColor)
        {
            testLEDColor = false;
            SimulateLEDColor();
        }
    }
    
    /// <summary>
    /// DHT11 센서 데이터 시뮬레이션 (실내 온습도)
    /// </summary>
    void SimulateSensorData()
    {
        Debug.Log($"[UITester] 센서 데이터 시뮬레이션: 온도={indoorTemp}°C, 습도={indoorHumidity}%");
        
        if (SerialController.Instance != null)
        {
            // SerialController의 이벤트 직접 호출 (리플렉션 사용)
            var eventField = typeof(SerialController).GetField("OnSensorDataReceived", 
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            
            if (eventField != null)
            {
                var eventDelegate = eventField.GetValue(SerialController.Instance) as System.Action<float, float>;
                eventDelegate?.Invoke(indoorTemp, indoorHumidity);
            }
        }
        
        // DataVisualizer 직접 호출
        if (DataVisualizer.Instance != null)
        {
            DataVisualizer.Instance.SetIndoorData(indoorTemp, indoorHumidity);
            Debug.Log("[UITester] DataVisualizer.SetIndoorData 호출됨");
        }
        else
        {
            Debug.LogWarning("[UITester] DataVisualizer.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 날씨 API 데이터 시뮬레이션 (외부 온습도)
    /// </summary>
    void SimulateWeatherData()
    {
        Debug.Log($"[UITester] 날씨 데이터 시뮬레이션: 온도={outdoorTemp}°C, 습도={outdoorHumidity}%");
        
        // DataVisualizer 직접 호출
        if (DataVisualizer.Instance != null)
        {
            DataVisualizer.Instance.SetOutdoorData(outdoorTemp, outdoorHumidity);
            Debug.Log("[UITester] DataVisualizer.SetOutdoorData 호출됨");
        }
        else
        {
            Debug.LogWarning("[UITester] DataVisualizer.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 서보 모터 각도 시뮬레이션 (다이얼 입력)
    /// </summary>
    void SimulateServoAngle()
    {
        Debug.Log($"[UITester] 서보 각도 시뮬레이션: {testAngle}° (DIAL)");
        
        if (SerialController.Instance != null)
        {
            var eventField = typeof(SerialController).GetField("OnServoAngleReceived",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            
            if (eventField != null)
            {
                var eventDelegate = eventField.GetValue(SerialController.Instance) as System.Action<int, string>;
                eventDelegate?.Invoke(testAngle, "DIAL");
            }
        }
        
        // LampTwinSync가 있으면 직접 호출도 시도
        if (LampTwinSync.Instance != null)
        {
            // SetAngle은 UNITY 소스로 Arduino에 전송하므로,
            // 여기서는 시뮬레이션용으로 슬라이더만 움직임
            Debug.Log("[UITester] LampTwinSync.Instance 발견됨");
        }
    }
    
    /// <summary>
    /// LED 색상 시뮬레이션
    /// </summary>
    void SimulateLEDColor()
    {
        Debug.Log($"[UITester] LED 색상 시뮬레이션: R={testR}, G={testG}, B={testB}");
        
        if (LEDController.Instance != null)
        {
            LEDController.Instance.SetLEDColor(testR, testG, testB);
            Debug.Log("[UITester] LEDController.SetLEDColor 호출됨");
        }
        else
        {
            Debug.LogWarning("[UITester] LEDController.Instance가 null입니다!");
        }
    }
    
    /// <summary>
    /// 키보드 단축키 테스트
    /// </summary>
    void OnGUI()
    {
        if (!enableTesting) return;
        
        // 화면 좌상단에 테스트 UI 표시
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("=== UI Tester (회로 없이 테스트) ===");
        GUILayout.Space(10);
        
        // 상태 표시
        GUILayout.Label($"SerialController: {(SerialController.Instance != null ? "OK" : "NULL")}");
        GUILayout.Label($"DataVisualizer: {(DataVisualizer.Instance != null ? "OK" : "NULL")}");
        GUILayout.Label($"LampTwinSync: {(LampTwinSync.Instance != null ? "OK" : "NULL")}");
        GUILayout.Label($"LEDController: {(LEDController.Instance != null ? "OK" : "NULL")}");
        GUILayout.Label($"WeatherAPI: {(WeatherAPIManager.Instance != null ? "OK" : "NULL")}");
        
        GUILayout.Space(10);
        GUILayout.Label("--- 키보드 단축키 ---");
        GUILayout.Label("1: 센서 데이터 테스트");
        GUILayout.Label("2: 날씨 데이터 테스트");
        GUILayout.Label("3: 서보 각도 테스트");
        GUILayout.Label("4: LED 색상 테스트");
        GUILayout.Label("R: 랜덤 데이터 생성");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("센서 데이터 시뮬레이션"))
        {
            SimulateSensorData();
        }
        
        if (GUILayout.Button("날씨 데이터 시뮬레이션"))
        {
            SimulateWeatherData();
        }
        
        if (GUILayout.Button("서보 각도 시뮬레이션"))
        {
            SimulateServoAngle();
        }
        
        if (GUILayout.Button("LED 색상 시뮬레이션"))
        {
            SimulateLEDColor();
        }
        
        if (GUILayout.Button("랜덤 데이터 생성"))
        {
            RandomizeData();
        }
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    void LateUpdate()
    {
        if (!enableTesting) return;
        
        // 키보드 단축키
        if (Input.GetKeyDown(KeyCode.Alpha1)) SimulateSensorData();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SimulateWeatherData();
        if (Input.GetKeyDown(KeyCode.Alpha3)) SimulateServoAngle();
        if (Input.GetKeyDown(KeyCode.Alpha4)) SimulateLEDColor();
        if (Input.GetKeyDown(KeyCode.R)) RandomizeData();
    }
    
    void RandomizeData()
    {
        indoorTemp = Random.Range(15f, 30f);
        outdoorTemp = Random.Range(-5f, 35f);
        indoorHumidity = Random.Range(30f, 70f);
        outdoorHumidity = Random.Range(20f, 90f);
        testAngle = Random.Range(0, 180);
        testR = Random.Range(0, 255);
        testG = Random.Range(0, 255);
        testB = Random.Range(0, 255);
        
        Debug.Log("[UITester] 랜덤 데이터 생성됨. Inspector에서 확인하세요.");
        
        // 자동으로 시뮬레이션 실행
        SimulateSensorData();
        SimulateWeatherData();
    }
}

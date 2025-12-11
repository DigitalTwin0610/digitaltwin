using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

/// <summary>
/// Cloudtype 서버로 통계 로그를 전송하는 매니저
/// 감정 분석, 날씨 정보, 상태 변경 로그를 수집하여 대시보드에 표시
/// 
/// 역할: 로그 수집 전용 (상태 동기화는 FirebaseManager 담당)
/// </summary>
public class MQTTManager : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://your-app.cloudtype.app";
    [SerializeField] private bool autoConnect = true;

    [Header("Auto Log Settings")]
    [SerializeField] private bool autoLogEmotion = true;
    [SerializeField] private bool autoLogWeather = true;
    [SerializeField] private bool autoLogState = true;

    [Header("Components")]
    [SerializeField] private ClaudeManager claudeManager;
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private HSVController hsvController;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // 이벤트
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnError;

    public bool IsConnected { get; private set; } = false;

    #region Unity Lifecycle

    private void Start()
    {
        // 컴포넌트 자동 찾기
        if (claudeManager == null)
            claudeManager = FindObjectOfType<ClaudeManager>();
        if (weatherManager == null)
            weatherManager = FindObjectOfType<WeatherManager>();
        if (hsvController == null)
            hsvController = FindObjectOfType<HSVController>();

        // 이벤트 구독
        SubscribeToEvents();

        if (autoConnect)
        {
            Connect();
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Event Subscription

    private void SubscribeToEvents()
    {
        // 감정 분석 완료 시
        if (claudeManager != null && autoLogEmotion)
        {
            claudeManager.OnEmotionAnalyzed += OnEmotionAnalyzed;
        }

        // 날씨 업데이트 시
        if (weatherManager != null && autoLogWeather)
        {
            weatherManager.OnWeatherUpdated += OnWeatherUpdated;
        }

        // HSV 변경 시 (옵션)
        if (hsvController != null && autoLogState)
        {
            hsvController.OnHSVChanged += OnHSVChanged;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (claudeManager != null)
        {
            claudeManager.OnEmotionAnalyzed -= OnEmotionAnalyzed;
        }

        if (weatherManager != null)
        {
            weatherManager.OnWeatherUpdated -= OnWeatherUpdated;
        }

        if (hsvController != null)
        {
            hsvController.OnHSVChanged -= OnHSVChanged;
        }
    }

    #endregion

    #region Event Handlers

    private void OnEmotionAnalyzed(EmotionResult result)
    {
        LogEmotion(result);
    }

    private void OnWeatherUpdated(WeatherData data)
    {
        LogWeather(data);
    }

    private void OnHSVChanged(float h, float s, float v)
    {
        // HSV 변경은 너무 자주 발생할 수 있으므로
        // 필요시 디바운싱 적용 가능
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 서버 URL 설정
    /// </summary>
    public void SetServerUrl(string url)
    {
        serverUrl = url.TrimEnd('/');
    }

    /// <summary>
    /// 서버 연결 확인
    /// </summary>
    public void Connect()
    {
        if (IsConnected)
        {
            Log("Already connected");
            return;
        }

        if (string.IsNullOrEmpty(serverUrl) || serverUrl.Contains("your-app"))
        {
            LogError("Invalid server URL. Please set the Cloudtype server URL.");
            return;
        }

        StartCoroutine(ConnectCoroutine());
    }

    /// <summary>
    /// 연결 해제
    /// </summary>
    public void Disconnect()
    {
        IsConnected = false;
        OnDisconnected?.Invoke();
        Log("Disconnected from server");
    }

    /// <summary>
    /// 감정 분석 결과 로그 전송
    /// </summary>
    public void LogEmotion(EmotionResult result)
    {
        if (!IsConnected) return;

        var state = hsvController?.GetLampState();
        
        string payload = $@"{{
            ""emotion"": ""{result.emotion.ToString().ToLower()}"",
            ""hue"": {result.hue},
            ""saturation"": {state?.saturation ?? 70},
            ""brightness"": {state?.brightness ?? 70},
            ""summary"": ""{EscapeJson(result.summary)}"",
            ""colorHex"": ""{state?.colorHex ?? "#50C878"}""
        }}";

        StartCoroutine(PostLogCoroutine("/api/log/emotion", payload));
    }

    /// <summary>
    /// 감정 직접 로그 (EmotionResult 없이)
    /// </summary>
    public void LogEmotionDirect(string emotion, int hue, int saturation, int brightness, string colorHex, string summary = "")
    {
        if (!IsConnected) return;

        string payload = $@"{{
            ""emotion"": ""{emotion}"",
            ""hue"": {hue},
            ""saturation"": {saturation},
            ""brightness"": {brightness},
            ""summary"": ""{EscapeJson(summary)}"",
            ""colorHex"": ""{colorHex}""
        }}";

        StartCoroutine(PostLogCoroutine("/api/log/emotion", payload));
    }

    /// <summary>
    /// 날씨 정보 로그 전송
    /// </summary>
    public void LogWeather(WeatherData data)
    {
        if (!IsConnected) return;

        string payload = $@"{{
            ""temperature"": {data.temperature},
            ""humidity"": {data.humidity},
            ""condition"": ""{data.conditionText}"",
            ""description"": ""{EscapeJson(data.description)}"",
            ""cityName"": ""{data.cityName}""
        }}";

        StartCoroutine(PostLogCoroutine("/api/log/weather", payload));
    }

    /// <summary>
    /// 상태 변경 로그 전송
    /// </summary>
    public void LogStateChange(string action, string mode, string details = "")
    {
        if (!IsConnected) return;

        string payload = $@"{{
            ""mode"": ""{mode}"",
            ""action"": ""{action}"",
            ""details"": {{ ""info"": ""{EscapeJson(details)}"" }}
        }}";

        StartCoroutine(PostLogCoroutine("/api/log/state", payload));
    }

    /// <summary>
    /// 모드 변경 시 호출
    /// </summary>
    public void LogModeChange(string newMode)
    {
        LogStateChange("mode_change", newMode, $"Mode changed to {newMode}");
    }

    /// <summary>
    /// 수동 색상 변경 시 호출
    /// </summary>
    public void LogManualColorChange(string colorHex)
    {
        var state = hsvController?.GetLampState();
        LogStateChange("manual_color", state?.mode ?? "MANUAL", $"Color set to {colorHex}");
    }

    #endregion

    #region Coroutines

    private IEnumerator ConnectCoroutine()
    {
        Log($"Connecting to: {serverUrl}");

        string url = $"{serverUrl}/api/status";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                IsConnected = true;
                Log($"★ Connected! Response: {request.downloadHandler.text}");
                OnConnected?.Invoke();
            }
            else
            {
                LogError($"★ Connection FAILED: {request.error}");
                LogError($"Response Code: {request.responseCode}");
                OnError?.Invoke(request.error);
                IsConnected = false;
            }
        }
    }

    private IEnumerator PostLogCoroutine(string endpoint, string payload)
    {
        string url = $"{serverUrl}{endpoint}";

        Log($"Sending log to {endpoint}: {payload.Substring(0, Mathf.Min(100, payload.Length))}...");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Log($"Log sent successfully: {endpoint}");
            }
            else
            {
                LogError($"Log failed: {request.error}");
                OnError?.Invoke(request.error);
                
                // 연결 끊김 감지
                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    IsConnected = false;
                    OnDisconnected?.Invoke();
                }
            }
        }
    }

    #endregion

    #region Utility

    private string EscapeJson(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    #endregion

    #region Debug

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[MQTTManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MQTTManager] {message}");
    }

    #endregion
}
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;

/// <summary>
/// Cloudtype 서버를 통한 MQTT 스타일 실시간 통신 매니저
/// WebSocket 대신 HTTP Long Polling 방식으로 구현 (Unity 호환성)
/// </summary>
public class MQTTManager : MonoBehaviour
{
    [Header("Server Settings")]
    [SerializeField] private string serverUrl = "https://your-app.cloudtype.app";
    [SerializeField] private float pollInterval = 2f;
    [SerializeField] private bool autoConnect = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // 이벤트
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string, string> OnMessageReceived;  // (topic, payload)
    public event Action<string> OnError;

    public bool IsConnected { get; private set; } = false;

    private Coroutine _pollCoroutine;
    private string _clientId;
    private long _lastMessageTime = 0;

    #region Unity Lifecycle

    private void Start()
    {
        _clientId = $"unity_{SystemInfo.deviceUniqueIdentifier.Substring(0, 8)}";

        if (autoConnect)
        {
            Connect();
        }
    }

    private void OnDestroy()
    {
        Disconnect();
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
    /// 서버 연결
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
            LogError("Invalid server URL");
            return;
        }

        StartCoroutine(ConnectCoroutine());
    }

    /// <summary>
    /// 서버 연결 해제
    /// </summary>
    public void Disconnect()
    {
        if (_pollCoroutine != null)
        {
            StopCoroutine(_pollCoroutine);
            _pollCoroutine = null;
        }

        IsConnected = false;
        OnDisconnected?.Invoke();
        Log("Disconnected from server");
    }

    /// <summary>
    /// 메시지 발행
    /// </summary>
    public void Publish(string topic, string payload)
    {
        if (!IsConnected)
        {
            LogError("Not connected to server");
            return;
        }

        StartCoroutine(PublishCoroutine(topic, payload));
    }

    /// <summary>
    /// 램프 상태 발행
    /// </summary>
    public void PublishLampState(LampState state)
    {
        string payload = $@"{{
            ""mode"": ""{state.mode}"",
            ""emotion"": ""{state.emotion}"",
            ""hue"": {state.hue},
            ""saturation"": {state.saturation},
            ""brightness"": {state.brightness},
            ""colorHex"": ""{state.colorHex}""
        }}";

        Publish("emolamp/state", payload);
    }

    /// <summary>
    /// LED 색상 발행
    /// </summary>
    public void PublishLedColor(int r, int g, int b)
    {
        string payload = $@"{{""r"":{r},""g"":{g},""b"":{b}}}";
        Publish("emolamp/led", payload);
    }

    #endregion

    #region Connection & Polling

    private IEnumerator ConnectCoroutine()
    {
        Log($"Trying to connect: {serverUrl}");

        // 서버 상태 확인
        string url = $"{serverUrl}/api/status";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                IsConnected = true;
                Log("Success to Connect");
                OnConnected?.Invoke();

                // 폴링 시작
                _pollCoroutine = StartCoroutine(PollCoroutine());
            }
            else
            {
                LogError($"Failed Connected: {request.error}");
                OnError?.Invoke(request.error);
                IsConnected = false;
            }
        }
    }

    private IEnumerator PollCoroutine()
    {
        while (IsConnected)
        {
            yield return new WaitForSeconds(pollInterval);

            string url = $"{serverUrl}/api/poll?clientId={_clientId}&since={_lastMessageTime}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = 30;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    
                    if (!string.IsNullOrEmpty(response) && response != "null" && response != "[]")
                    {
                        ProcessPollResponse(response);
                    }
                }
                else if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    LogError("Connection lost");
                    IsConnected = false;
                    OnDisconnected?.Invoke();
                    break;
                }
            }
        }
    }

    private void ProcessPollResponse(string json)
    {
        try
        {
            Log($"Pulling Response: {json}");

            // 간단한 메시지 파싱
            // 형식: {"topic":"emolamp/state","payload":"{...}","timestamp":123456}
            
            if (json.Contains("\"topic\""))
            {
                string topic = ExtractStringValue(json, "topic");
                string payload = ExtractStringValue(json, "payload");
                string timestampStr = ExtractNumberValue(json, "timestamp");

                if (!string.IsNullOrEmpty(topic))
                {
                    // 이스케이프 문자 복원
                    if (!string.IsNullOrEmpty(payload))
                    {
                        payload = payload.Replace("\\\"", "\"").Replace("\\\\", "\\");
                    }

                    Log($"Recieve Massage: [{topic}] {payload}");
                    OnMessageReceived?.Invoke(topic, payload);

                    if (long.TryParse(timestampStr, out long timestamp))
                    {
                        _lastMessageTime = timestamp;
                    }
                }
            }
        }
        catch (Exception e)
        {
            LogError($"Polling Response Parsing Error: {e.Message}");
        }
    }

    #endregion

    #region Publish

    private IEnumerator PublishCoroutine(string topic, string payload)
    {
        string url = $"{serverUrl}/api/publish";

        string json = $@"{{
            ""topic"": ""{topic}"",
            ""payload"": {payload},
            ""clientId"": ""{_clientId}""
        }}";

        Log($"발행: [{topic}] {payload}");

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Log("Publish Success");
            }
            else
            {
                LogError($"Publish Failed: {request.error}");
                OnError?.Invoke(request.error);
            }
        }
    }

    #endregion

    #region Utility

    private string ExtractStringValue(string json, string key)
    {
        string pattern = $"\"{key}\":\"";
        int index = json.IndexOf(pattern);
        if (index < 0) return null;

        int startIndex = index + pattern.Length;
        int endIndex = FindStringEnd(json, startIndex);
        if (endIndex < 0) return null;

        return json.Substring(startIndex, endIndex - startIndex);
    }

    private int FindStringEnd(string json, int startIndex)
    {
        bool escaped = false;
        for (int i = startIndex; i < json.Length; i++)
        {
            if (escaped)
            {
                escaped = false;
                continue;
            }
            if (json[i] == '\\')
            {
                escaped = true;
                continue;
            }
            if (json[i] == '"')
            {
                return i;
            }
        }
        return -1;
    }

    private string ExtractNumberValue(string json, string key)
    {
        string pattern = $"\"{key}\":";
        int index = json.IndexOf(pattern);
        if (index < 0) return null;

        int startIndex = index + pattern.Length;
        string result = "";

        for (int i = startIndex; i < json.Length; i++)
        {
            char c = json[i];
            if (char.IsDigit(c) || c == '-' || c == '.')
            {
                result += c;
            }
            else if (result.Length > 0)
            {
                break;
            }
        }

        return result;
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

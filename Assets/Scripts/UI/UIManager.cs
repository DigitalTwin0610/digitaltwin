using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 전체 UI 및 시스템 통합 관리 매니저
/// 모든 컴포넌트를 연결하고 데이터 흐름 제어
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("=== Managers ===")]
    [SerializeField] private WeatherManager weatherManager;
    [SerializeField] private ClaudeManager claudeManager;
    [SerializeField] private PapagoManager papagoManager;
    [SerializeField] private FirebaseManager firebaseManager;
    [SerializeField] private MQTTManager mqttManager;
    [SerializeField] private HSVController hsvController;
    [SerializeField] private LampController lampController;
    [SerializeField] private BackgroundController backgroundController;
    [SerializeField] private SerialController serialController;

    [Header("=== Status Bar UI ===")]
    [SerializeField] private TMP_Text weatherText;       // 🌤️ 맑음 8°C
    [SerializeField] private TMP_Text emotionText;       // 😊 기쁨
    [SerializeField] private TMP_Text summaryText;       // AI 요약 메시지

    [Header("=== Input UI ===")]
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button analyzeButton;
    [SerializeField] private TMP_Text analyzeButtonText;

    [Header("=== Mode UI ===")]
    [SerializeField] private Toggle autoModeToggle;
    [SerializeField] private Toggle manualModeToggle;
    [SerializeField] private GameObject colorPickerPanel;
    [SerializeField] private Image colorPreview;
    [SerializeField] private Slider hueSlider;
    [SerializeField] private Slider saturationSlider;
    [SerializeField] private Slider brightnessSlider;

    [Header("=== Connection Status ===")]
    [SerializeField] private Image serialStatusIcon;
    [SerializeField] private Image firebaseStatusIcon;
    [SerializeField] private TMP_Text connectionText;

    [Header("=== Colors ===")]
    [SerializeField] private Color connectedColor = Color.green;
    [SerializeField] private Color disconnectedColor = Color.red;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // 상태
    private bool _isAnalyzing = false;
    private bool _isManualMode = false;

    #region Unity Lifecycle

    private void Start()
    {
        InitializeComponents();
        SetupEventListeners();
        SetupUIListeners();

        // 초기 상태
        SetAutoMode();
        UpdateConnectionStatus();

        // 초기 날씨 로드 후 분석
        StartCoroutine(InitialLoadCoroutine());
    }

    private void OnDestroy()
    {
        RemoveEventListeners();
    }

    #endregion

    #region Initialization

    private void InitializeComponents()
    {
        // 컴포넌트 자동 찾기
        if (weatherManager == null) weatherManager = FindObjectOfType<WeatherManager>();
        if (claudeManager == null) claudeManager = FindObjectOfType<ClaudeManager>();
        if (papagoManager == null) papagoManager = FindObjectOfType<PapagoManager>();
        if (firebaseManager == null) firebaseManager = FindObjectOfType<FirebaseManager>();
        if (mqttManager == null) mqttManager = FindObjectOfType<MQTTManager>();
        if (hsvController == null) hsvController = FindObjectOfType<HSVController>();
        if (lampController == null) lampController = FindObjectOfType<LampController>();
        if (backgroundController == null) backgroundController = FindObjectOfType<BackgroundController>();
        if (serialController == null) serialController = FindObjectOfType<SerialController>();

        Log("컴포넌트 초기화 완료");
    }

    private void SetupEventListeners()
    {
        // Weather
        if (weatherManager != null)
        {
            weatherManager.OnWeatherUpdated += OnWeatherUpdated;
        }

        // Claude
        if (claudeManager != null)
        {
            claudeManager.OnEmotionAnalyzed += OnEmotionAnalyzed;
            claudeManager.OnAnalysisError += OnAnalysisError;
        }

        // Firebase
        if (firebaseManager != null)
        {
            firebaseManager.OnRemoteStateChanged += OnRemoteStateChanged;
        }

        // Serial
        if (serialController != null)
        {
            serialController.OnConnected += OnSerialConnected;
            serialController.OnDisconnected += OnSerialDisconnected;
            serialController.OnBrightnessReceived += OnBrightnessReceived;
        }

        // HSV
        if (hsvController != null)
        {
            hsvController.OnColorChanged += OnHSVColorChanged;
        }
    }

    private void SetupUIListeners()
    {
        // 분석 버튼
        if (analyzeButton != null)
        {
            analyzeButton.onClick.AddListener(OnAnalyzeButtonClicked);
        }

        // 입력 필드 엔터키
        if (inputField != null)
        {
            inputField.onSubmit.AddListener((_) => OnAnalyzeButtonClicked());
        }

        // 모드 토글
        if (autoModeToggle != null)
        {
            autoModeToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetAutoMode(); });
        }
        if (manualModeToggle != null)
        {
            manualModeToggle.onValueChanged.AddListener((isOn) => { if (isOn) SetManualMode(); });
        }

        // 컬러 슬라이더
        if (hueSlider != null)
        {
            hueSlider.onValueChanged.AddListener(OnManualColorChanged);
        }
        if (saturationSlider != null)
        {
            saturationSlider.onValueChanged.AddListener(OnManualColorChanged);
        }
        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.AddListener(OnManualColorChanged);
        }
    }

    private void RemoveEventListeners()
    {
        if (weatherManager != null) weatherManager.OnWeatherUpdated -= OnWeatherUpdated;
        if (claudeManager != null)
        {
            claudeManager.OnEmotionAnalyzed -= OnEmotionAnalyzed;
            claudeManager.OnAnalysisError -= OnAnalysisError;
        }
        if (firebaseManager != null) firebaseManager.OnRemoteStateChanged -= OnRemoteStateChanged;
        if (serialController != null)
        {
            serialController.OnConnected -= OnSerialConnected;
            serialController.OnDisconnected -= OnSerialDisconnected;
            serialController.OnBrightnessReceived -= OnBrightnessReceived;
        }
        if (hsvController != null) hsvController.OnColorChanged -= OnHSVColorChanged;
    }

    private IEnumerator InitialLoadCoroutine()
    {
        // 날씨 로드 대기
        yield return new WaitForSeconds(2f);

        // 기본 메시지로 분석
        if (!_isAnalyzing && weatherManager != null && weatherManager.CurrentWeather != null)
        {
            AnalyzeWithText("오늘 하루 시작");
        }
    }

    #endregion

    #region Mode Management

    private void SetAutoMode()
    {
        _isManualMode = false;
        
        if (hsvController != null)
        {
            hsvController.SetManualMode(false);
        }

        if (colorPickerPanel != null)
        {
            colorPickerPanel.SetActive(false);
        }

        if (autoModeToggle != null) autoModeToggle.isOn = true;
        if (manualModeToggle != null) manualModeToggle.isOn = false;

        Log("AUTO 모드 활성화");
        SaveCurrentState();
    }

    private void SetManualMode()
    {
        _isManualMode = true;

        if (hsvController != null)
        {
            hsvController.SetManualMode(true);
        }

        if (colorPickerPanel != null)
        {
            colorPickerPanel.SetActive(true);
        }

        if (autoModeToggle != null) autoModeToggle.isOn = false;
        if (manualModeToggle != null) manualModeToggle.isOn = true;

        // 현재 색상으로 슬라이더 초기화
        if (hsvController != null)
        {
            if (hueSlider != null) hueSlider.value = hsvController.Hue;
            if (saturationSlider != null) saturationSlider.value = hsvController.Saturation;
            if (brightnessSlider != null) brightnessSlider.value = hsvController.Brightness;
        }

        Log("MANUAL 모드 활성화");
        SaveCurrentState();
    }

    private void OnManualColorChanged(float _)
    {
        if (!_isManualMode) return;

        float h = hueSlider != null ? hueSlider.value : 120;
        float s = saturationSlider != null ? saturationSlider.value : 70;
        float v = brightnessSlider != null ? brightnessSlider.value : 70;

        Color color = Color.HSVToRGB(h / 360f, s / 100f, v / 100f);

        if (hsvController != null)
        {
            hsvController.SetManualColor(color);
        }

        if (colorPreview != null)
        {
            colorPreview.color = color;
        }

        SaveCurrentState();
    }

    #endregion

    #region Analysis

    private void OnAnalyzeButtonClicked()
    {
        if (_isAnalyzing) return;

        string text = inputField != null ? inputField.text.Trim() : "";
        
        if (string.IsNullOrEmpty(text))
        {
            text = "현재 기분";  // 기본값
        }

        AnalyzeWithText(text);
    }

    private void AnalyzeWithText(string text)
    {
        if (_isAnalyzing) return;

        _isAnalyzing = true;
        UpdateAnalyzeButton(true);

        Log($"분석 시작: {text}");

        // 한국어가 아니면 번역
        if (papagoManager != null && !PapagoManager.IsKorean(text))
        {
            papagoManager.Translate(text, (translated) =>
            {
                PerformAnalysis(translated);
            });
        }
        else
        {
            PerformAnalysis(text);
        }
    }

    private void PerformAnalysis(string text)
    {
        if (claudeManager == null)
        {
            LogError("ClaudeManager가 없습니다.");
            _isAnalyzing = false;
            UpdateAnalyzeButton(false);
            return;
        }

        // 날씨 정보 포함
        string weatherInfo = "";
        if (weatherManager != null && weatherManager.CurrentWeather != null)
        {
            var w = weatherManager.CurrentWeather;
            weatherInfo = $"{w.description}, {w.temperature}°C";
        }

        claudeManager.AnalyzeEmotion(text, weatherInfo);
    }

    #endregion

    #region Event Handlers

    private void OnWeatherUpdated(WeatherData data)
    {
        Log($"날씨 업데이트: {data.GetIcon()} {data.description} {data.temperature}°C");

        // UI 업데이트
        if (weatherText != null)
        {
            weatherText.text = $"{data.GetIcon()} {data.description} {data.temperature}°C";
        }

        // HSV 채도 업데이트 (AUTO 모드일 때만)
        if (!_isManualMode && hsvController != null)
        {
            hsvController.SetSaturationFromWeather(data.condition);
        }

        SaveCurrentState();
    }

    private void OnEmotionAnalyzed(EmotionResult result)
    {
        Log($"감정 분석 완료: {result.GetEmoji()} {result.GetEmotionKorean()} (H:{result.hue})");

        _isAnalyzing = false;
        UpdateAnalyzeButton(false);

        // UI 업데이트
        if (emotionText != null)
        {
            emotionText.text = $"{result.GetEmoji()} {result.GetEmotionKorean()}";
        }

        if (summaryText != null)
        {
            summaryText.text = $"💬 \"{result.summary}\"";
        }

        // HSV 색감 업데이트 (AUTO 모드일 때만)
        if (!_isManualMode && hsvController != null)
        {
            hsvController.SetHue(result.hue);
        }

        // 입력창 초기화
        if (inputField != null)
        {
            inputField.text = "";
        }

        SaveCurrentState();
    }

    private void OnAnalysisError(string error)
    {
        LogError($"분석 오류: {error}");

        _isAnalyzing = false;
        UpdateAnalyzeButton(false);

        if (summaryText != null)
        {
            summaryText.text = "💬 분석 중 오류가 발생했습니다.";
        }
    }

    private void OnBrightnessReceived(int brightness)
    {
        Log($"조도 수신: {brightness}%");

        // HSV 명도 업데이트
        if (hsvController != null)
        {
            hsvController.SetBrightnessFromLight(brightness);
        }

        SaveCurrentState();
    }

    private void OnRemoteStateChanged(LampState state)
    {
        Log($"원격 상태 변경: mode={state.mode}");

        // 모드 동기화
        if (state.mode == "MANUAL" && !_isManualMode)
        {
            SetManualMode();
            
            // 원격 색상 적용
            if (hsvController != null)
            {
                hsvController.SetManualColorHex(state.manualColorHex);
            }
        }
        else if (state.mode == "AUTO" && _isManualMode)
        {
            SetAutoMode();
        }
    }

    private void OnHSVColorChanged(Color color)
    {
        if (colorPreview != null)
        {
            colorPreview.color = color;
        }
    }

    private void OnSerialConnected()
    {
        Log("Serial 연결됨");
        UpdateConnectionStatus();
    }

    private void OnSerialDisconnected()
    {
        Log("Serial 연결 해제됨");
        UpdateConnectionStatus();
    }

    #endregion

    #region UI Updates

    private void UpdateAnalyzeButton(bool analyzing)
    {
        if (analyzeButton != null)
        {
            analyzeButton.interactable = !analyzing;
        }

        if (analyzeButtonText != null)
        {
            analyzeButtonText.text = analyzing ? "분석 중..." : "분석 ▶";
        }
    }

    private void UpdateConnectionStatus()
    {
        bool serialConnected = serialController != null && serialController.IsConnected;
        bool firebaseConnected = firebaseManager != null && firebaseManager.IsConnected;

        if (serialStatusIcon != null)
        {
            serialStatusIcon.color = serialConnected ? connectedColor : disconnectedColor;
        }

        if (firebaseStatusIcon != null)
        {
            firebaseStatusIcon.color = firebaseConnected ? connectedColor : disconnectedColor;
        }

        if (connectionText != null)
        {
            string status = "";
            status += serialConnected ? "● Serial " : "○ Serial ";
            status += firebaseConnected ? "● Firebase" : "○ Firebase";
            connectionText.text = status;
        }
    }

    #endregion

    #region State Management

    private void SaveCurrentState()
    {
        if (firebaseManager == null || hsvController == null) return;

        LampState state = hsvController.GetLampState();
        
        if (weatherManager != null && weatherManager.CurrentWeather != null)
        {
            state.weather = weatherManager.CurrentWeather.description;
        }

        if (claudeManager != null && claudeManager.LastResult != null)
        {
            state.emotion = claudeManager.LastResult.emotion.ToString().ToLower();
            state.summary = claudeManager.LastResult.summary;
        }

        firebaseManager.SaveState(state);

        // MQTT도 발행
        if (mqttManager != null && mqttManager.IsConnected)
        {
            mqttManager.PublishLampState(state);
        }
    }

    #endregion

    #region Debug

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[UIManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[UIManager] {message}");
    }

    #endregion
}

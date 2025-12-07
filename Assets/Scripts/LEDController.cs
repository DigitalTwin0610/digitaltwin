using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LEDController : MonoBehaviour
{
    public static LEDController Instance { get; private set; }

    [Header("Mode")]
    public bool autoMode = true;

    [Header("UI - Mode Toggle")]
    public Button modeToggleButton;
    public TMP_Text modeButtonText;
    public Image modeIndicator;

    [Header("UI - RGB Sliders")]
    public Slider redSlider;
    public Slider greenSlider;
    public Slider blueSlider;
    public Slider brightnessSlider;

    [Header("UI - Display")]
    public TMP_Text rgbValueText;
    public Image ledPreview;
    public TMP_Text autoReasonText;

    [Header("UI - Preset Buttons")]
    public Button presetWarm;
    public Button presetCool;
    public Button presetAlert;
    public Button presetOff;

    [Header("Settings")]
    public float sendInterval = 0.1f;

    private int currentR = 255;
    private int currentG = 255;
    private int currentB = 255;
    private int currentBrightness = 255;

    private Color autoSafeColor = new Color(0.2f, 1f, 0.4f);
    private Color autoCautionColor = new Color(1f, 0.9f, 0.2f);
    private Color autoWarningColor = new Color(1f, 0.5f, 0.1f);
    private Color autoDangerColor = new Color(1f, 0.1f, 0.1f);
    private Color autoRainColor = new Color(0.2f, 0.4f, 1f);
    private Color autoSnowColor = new Color(0.9f, 0.95f, 1f);

    private float lastSendTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeUI()
    {
        if (redSlider != null)
        {
            redSlider.minValue = 0;
            redSlider.maxValue = 255;
            redSlider.value = 255;
            redSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        }

        if (greenSlider != null)
        {
            greenSlider.minValue = 0;
            greenSlider.maxValue = 255;
            greenSlider.value = 255;
            greenSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        }

        if (blueSlider != null)
        {
            blueSlider.minValue = 0;
            blueSlider.maxValue = 255;
            blueSlider.value = 255;
            blueSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0;
            brightnessSlider.maxValue = 255;
            brightnessSlider.value = 255;
            brightnessSlider.onValueChanged.AddListener(_ => OnSliderChanged());
        }

        if (modeToggleButton != null)
        {
            modeToggleButton.onClick.AddListener(ToggleMode);
        }

        if (presetWarm != null)
            presetWarm.onClick.AddListener(() => SetPreset(255, 180, 100, "Warm"));

        if (presetCool != null)
            presetCool.onClick.AddListener(() => SetPreset(100, 180, 255, "Cool"));

        if (presetAlert != null)
            presetAlert.onClick.AddListener(() => SetPreset(255, 50, 0, "Alert"));

        if (presetOff != null)
            presetOff.onClick.AddListener(() => SetPreset(0, 0, 0, "Off"));

        UpdateModeDisplay();
        UpdateSliderInteractivity();
    }

    private void SubscribeToEvents()
    {
        if (WeatherAPIManager.Instance != null)
        {
            WeatherAPIManager.Instance.OnWeatherDataReceived += OnWeatherDataReceived;
        }

        if (SerialController.Instance != null)
        {
            SerialController.Instance.OnLEDStateReceived += OnLEDStateReceived;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (WeatherAPIManager.Instance != null)
        {
            WeatherAPIManager.Instance.OnWeatherDataReceived -= OnWeatherDataReceived;
        }

        if (SerialController.Instance != null)
        {
            SerialController.Instance.OnLEDStateReceived -= OnLEDStateReceived;
        }
    }

    public void ToggleMode()
    {
        autoMode = !autoMode;
        UpdateModeDisplay();
        UpdateSliderInteractivity();

        if (autoMode)
        {
            UpdateAutoColor();
        }
    }

    private void UpdateModeDisplay()
    {
        if (modeButtonText != null)
        {
            modeButtonText.text = autoMode ? "AUTO" : "MANUAL";
        }

        if (modeIndicator != null)
        {
            modeIndicator.color = autoMode ?
                new Color(0.2f, 0.8f, 0.4f) :
                new Color(0.2f, 0.6f, 1f);
        }
    }

    private void UpdateSliderInteractivity()
    {
        bool interactive = !autoMode;

        if (redSlider != null) redSlider.interactable = interactive;
        if (greenSlider != null) greenSlider.interactable = interactive;
        if (blueSlider != null) blueSlider.interactable = interactive;
        if (brightnessSlider != null) brightnessSlider.interactable = interactive;
    }

    private void OnSliderChanged()
    {
        if (autoMode) return;

        currentR = (int)(redSlider?.value ?? 255);
        currentG = (int)(greenSlider?.value ?? 255);
        currentB = (int)(blueSlider?.value ?? 255);
        currentBrightness = (int)(brightnessSlider?.value ?? 255);

        UpdatePreview();
        SendLEDCommand();
    }

    private void SetPreset(int r, int g, int b, string name)
    {
        if (autoMode)
        {
            autoMode = false;
            UpdateModeDisplay();
            UpdateSliderInteractivity();
        }

        currentR = r;
        currentG = g;
        currentB = b;

        if (redSlider != null) redSlider.SetValueWithoutNotify(r);
        if (greenSlider != null) greenSlider.SetValueWithoutNotify(g);
        if (blueSlider != null) blueSlider.SetValueWithoutNotify(b);

        UpdatePreview();
        SendLEDCommand();

        Debug.Log($"[LED] Preset: {name}");
    }

    private void OnWeatherDataReceived(float temp, float humidity, int sky, int pty)
    {
        if (!autoMode) return;
        UpdateAutoColor();
    }

    private void UpdateAutoColor()
    {
        if (!autoMode) return;

        Color targetColor;
        string reason;

        if (WeatherAPIManager.Instance != null)
        {
            int pty = WeatherAPIManager.Instance.PrecipitationType;
            if (pty == 1 || pty == 5 || pty == 6)
            {
                targetColor = autoRainColor;
                reason = "Rain";
                ApplyAutoColor(targetColor, reason);
                return;
            }
            else if (pty == 3 || pty == 7)
            {
                targetColor = autoSnowColor;
                reason = "Snow";
                ApplyAutoColor(targetColor, reason);
                return;
            }
        }

        float tempDiff = GetTemperatureDifference();

        if (tempDiff < 5f)
        {
            targetColor = autoSafeColor;
            reason = "Comfortable";
        }
        else if (tempDiff < 10f)
        {
            targetColor = autoCautionColor;
            reason = "Caution";
        }
        else if (tempDiff < 15f)
        {
            targetColor = autoWarningColor;
            reason = "Warning";
        }
        else
        {
            targetColor = autoDangerColor;
            reason = "Danger";
        }

        ApplyAutoColor(targetColor, reason);
    }

    private void ApplyAutoColor(Color color, string reason)
    {
        currentR = (int)(color.r * 255);
        currentG = (int)(color.g * 255);
        currentB = (int)(color.b * 255);

        if (redSlider != null) redSlider.SetValueWithoutNotify(currentR);
        if (greenSlider != null) greenSlider.SetValueWithoutNotify(currentG);
        if (blueSlider != null) blueSlider.SetValueWithoutNotify(currentB);

        if (autoReasonText != null)
        {
            autoReasonText.text = reason;
        }

        UpdatePreview();
        SendLEDCommand();
    }

    private float GetTemperatureDifference()
    {
        float indoor = 22f;
        float outdoor = 20f;

        if (WeatherAPIManager.Instance != null)
        {
            outdoor = WeatherAPIManager.Instance.OutdoorTemperature;
        }

        if (DataVisualizer.Instance != null)
        {
            return DataVisualizer.Instance.GetTemperatureDifference();
        }

        return Mathf.Abs(indoor - outdoor);
    }

    private void OnLEDStateReceived(int r, int g, int b)
    {
        currentR = r;
        currentG = g;
        currentB = b;

        if (!autoMode)
        {
            if (redSlider != null) redSlider.SetValueWithoutNotify(r);
            if (greenSlider != null) greenSlider.SetValueWithoutNotify(g);
            if (blueSlider != null) blueSlider.SetValueWithoutNotify(b);
        }

        UpdatePreview();
    }

    private void SendLEDCommand()
    {
        if (Time.time - lastSendTime < sendInterval) return;
        lastSendTime = Time.time;

        if (SerialController.Instance != null)
        {
            char mode = autoMode ? 'A' : 'M';
            SerialController.Instance.SendLEDCommand(currentR, currentG, currentB, currentBrightness, mode);
        }
    }

    private void UpdatePreview()
    {
        if (ledPreview != null)
        {
            ledPreview.color = new Color(
                currentR / 255f,
                currentG / 255f,
                currentB / 255f
            );
        }

        if (rgbValueText != null)
        {
            rgbValueText.text = $"R:{currentR} G:{currentG} B:{currentB}";
        }

        if (LampTwinSync.Instance != null)
        {
            Color ledColor = new Color(currentR / 255f, currentG / 255f, currentB / 255f);
            LampTwinSync.Instance.SetLEDColor(ledColor);
        }
    }

    public void SetLEDColor(int r, int g, int b)
    {
        if (autoMode)
        {
            autoMode = false;
            UpdateModeDisplay();
            UpdateSliderInteractivity();
        }

        currentR = r;
        currentG = g;
        currentB = b;

        if (redSlider != null) redSlider.SetValueWithoutNotify(r);
        if (greenSlider != null) greenSlider.SetValueWithoutNotify(g);
        if (blueSlider != null) blueSlider.SetValueWithoutNotify(b);

        UpdatePreview();
        SendLEDCommand();
    }

    public Color GetCurrentColor()
    {
        return new Color(currentR / 255f, currentG / 255f, currentB / 255f);
    }
}

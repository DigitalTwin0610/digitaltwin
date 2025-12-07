using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DataVisualizer : MonoBehaviour
{
    public static DataVisualizer Instance { get; private set; }

    [Header("Indoor Sensor Display")]
    public Image indoorTempBar;
    public Image indoorHumidBar;
    public TMP_Text indoorTempText;
    public TMP_Text indoorHumidText;

    [Header("Outdoor Weather Display")]
    public Image outdoorTempBar;
    public Image outdoorHumidBar;
    public TMP_Text outdoorTempText;
    public TMP_Text outdoorHumidText;

    [Header("Temperature Difference Gauge")]
    public RectTransform gaugeNeedle;
    public Image gaugeFill;
    public TMP_Text tempDiffText;
    public TMP_Text riskLevelText;

    [Header("Settings")]
    public float minTemp = -10f;
    public float maxTemp = 40f;
    public float minHumid = 0f;
    public float maxHumid = 100f;
    public float maxTempDiff = 20f;
    public float animationSpeed = 3f;

    [Header("Colors")]
    public Color safeColor = new Color(0.2f, 0.8f, 0.4f);
    public Color cautionColor = new Color(1f, 0.8f, 0.2f);
    public Color warningColor = new Color(1f, 0.5f, 0.2f);
    public Color dangerColor = new Color(0.9f, 0.2f, 0.2f);

    private float indoorTemp = 22f;
    private float indoorHumidity = 50f;
    private float outdoorTemp = 20f;
    private float outdoorHumidity = 60f;

    private float targetIndoorTemp;
    private float targetIndoorHumidity;
    private float targetOutdoorTemp;
    private float targetOutdoorHumidity;

    public event Action<float> OnTemperatureDifferenceChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        InitializeTargets();
        SubscribeToEvents();
    }

    void Update()
    {
        AnimateValues();
        UpdateDisplay();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeTargets()
    {
        targetIndoorTemp = indoorTemp;
        targetIndoorHumidity = indoorHumidity;
        targetOutdoorTemp = outdoorTemp;
        targetOutdoorHumidity = outdoorHumidity;
    }

    private void SubscribeToEvents()
    {
        if (SerialController.Instance != null)
        {
            SerialController.Instance.OnSensorDataReceived += OnSensorDataReceived;
        }

        if (WeatherAPIManager.Instance != null)
        {
            WeatherAPIManager.Instance.OnWeatherDataReceived += OnWeatherDataReceived;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (SerialController.Instance != null)
        {
            SerialController.Instance.OnSensorDataReceived -= OnSensorDataReceived;
        }

        if (WeatherAPIManager.Instance != null)
        {
            WeatherAPIManager.Instance.OnWeatherDataReceived -= OnWeatherDataReceived;
        }
    }

    private void OnSensorDataReceived(float temp, float humidity)
    {
        targetIndoorTemp = temp;
        targetIndoorHumidity = humidity;
    }

    private void OnWeatherDataReceived(float temp, float humidity, int sky, int pty)
    {
        targetOutdoorTemp = temp;
        targetOutdoorHumidity = humidity;
    }

    private void AnimateValues()
    {
        indoorTemp = Mathf.Lerp(indoorTemp, targetIndoorTemp, Time.deltaTime * animationSpeed);
        indoorHumidity = Mathf.Lerp(indoorHumidity, targetIndoorHumidity, Time.deltaTime * animationSpeed);
        outdoorTemp = Mathf.Lerp(outdoorTemp, targetOutdoorTemp, Time.deltaTime * animationSpeed);
        outdoorHumidity = Mathf.Lerp(outdoorHumidity, targetOutdoorHumidity, Time.deltaTime * animationSpeed);
    }

    private void UpdateDisplay()
    {
        UpdateTemperatureBars();
        UpdateHumidityBars();
        UpdateDifferenceGauge();
    }

    private void UpdateTemperatureBars()
    {
        if (indoorTempBar != null)
        {
            float fillAmount = Mathf.InverseLerp(minTemp, maxTemp, indoorTemp);
            indoorTempBar.fillAmount = fillAmount;
            indoorTempBar.color = GetTemperatureColor(indoorTemp);
        }

        if (indoorTempText != null)
        {
            indoorTempText.text = $"{indoorTemp:F1}°C";
        }

        if (outdoorTempBar != null)
        {
            float fillAmount = Mathf.InverseLerp(minTemp, maxTemp, outdoorTemp);
            outdoorTempBar.fillAmount = fillAmount;
            outdoorTempBar.color = GetTemperatureColor(outdoorTemp);
        }

        if (outdoorTempText != null)
        {
            outdoorTempText.text = $"{outdoorTemp:F1}°C";
        }
    }

    private void UpdateHumidityBars()
    {
        if (indoorHumidBar != null)
        {
            float fillAmount = Mathf.InverseLerp(minHumid, maxHumid, indoorHumidity);
            indoorHumidBar.fillAmount = fillAmount;
        }

        if (indoorHumidText != null)
        {
            indoorHumidText.text = $"{indoorHumidity:F0}%";
        }

        if (outdoorHumidBar != null)
        {
            float fillAmount = Mathf.InverseLerp(minHumid, maxHumid, outdoorHumidity);
            outdoorHumidBar.fillAmount = fillAmount;
        }

        if (outdoorHumidText != null)
        {
            outdoorHumidText.text = $"{outdoorHumidity:F0}%";
        }
    }

    private void UpdateDifferenceGauge()
    {
        float tempDiff = Mathf.Abs(indoorTemp - outdoorTemp);

        if (gaugeNeedle != null)
        {
            float normalizedDiff = Mathf.Clamp01(tempDiff / maxTempDiff);
            float angle = Mathf.Lerp(0f, 180f, normalizedDiff);
            gaugeNeedle.localRotation = Quaternion.Euler(0, 0, -angle + 90f);
        }

        if (gaugeFill != null)
        {
            gaugeFill.color = GetRiskColor(tempDiff);
        }

        if (tempDiffText != null)
        {
            string sign = indoorTemp > outdoorTemp ? "+" : "-";
            tempDiffText.text = $"{sign}{tempDiff:F1}°C";
        }

        if (riskLevelText != null)
        {
            riskLevelText.text = GetRiskLevelText(tempDiff);
            riskLevelText.color = GetRiskColor(tempDiff);
        }

        OnTemperatureDifferenceChanged?.Invoke(tempDiff);
    }

    private Color GetTemperatureColor(float temp)
    {
        if (temp < 10f) return new Color(0.2f, 0.6f, 1f);
        if (temp < 20f) return new Color(0.2f, 0.8f, 0.6f);
        if (temp < 26f) return new Color(0.2f, 0.8f, 0.4f);
        if (temp < 32f) return new Color(1f, 0.7f, 0.2f);
        return new Color(0.9f, 0.3f, 0.2f);
    }

    private Color GetRiskColor(float tempDiff)
    {
        if (tempDiff < 5f) return safeColor;
        if (tempDiff < 10f) return cautionColor;
        if (tempDiff < 15f) return warningColor;
        return dangerColor;
    }

    private string GetRiskLevelText(float tempDiff)
    {
        if (tempDiff < 5f) return "Good";
        if (tempDiff < 10f) return "Caution";
        if (tempDiff < 15f) return "Warning";
        return "Danger";
    }

    public float GetTemperatureDifference()
    {
        return Mathf.Abs(indoorTemp - outdoorTemp);
    }

    public void SetIndoorData(float temp, float humidity)
    {
        targetIndoorTemp = temp;
        targetIndoorHumidity = humidity;
    }

    public void SetOutdoorData(float temp, float humidity)
    {
        targetOutdoorTemp = temp;
        targetOutdoorHumidity = humidity;
    }
}

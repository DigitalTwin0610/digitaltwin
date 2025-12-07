using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class WeatherAPIManager : MonoBehaviour
{
    public static WeatherAPIManager Instance { get; private set; }

    [Header("API Settings")]
    [TextArea(2, 4)]
    public string serviceKey = "YOUR_DECODED_API_KEY_HERE";
    public int nx = 60;
    public int ny = 127;

    [Header("Update Settings")]
    public float updateInterval = 600f;
    public bool autoUpdate = true;

    [Header("UI Display")]
    public TMP_Text temperatureText;
    public TMP_Text humidityText;
    public TMP_Text skyConditionText;
    public TMP_Text lastUpdateText;

    [Header("Status")]
    public bool isLoading = false;
    public string lastError = "";

    public float OutdoorTemperature { get; private set; }
    public float OutdoorHumidity { get; private set; }
    public int SkyCondition { get; private set; }
    public int PrecipitationType { get; private set; }

    public event Action<float, float, int, int> OnWeatherDataReceived;

    private string baseUrl = "http://apis.data.go.kr/1360000/VilageFcstInfoService_2.0/getUltraSrtNcst";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (autoUpdate)
        {
            StartCoroutine(AutoUpdateRoutine());
        }
    }

    private IEnumerator AutoUpdateRoutine()
    {
        while (true)
        {
            yield return StartCoroutine(FetchWeatherData());
            yield return new WaitForSeconds(updateInterval);
        }
    }

    public void RefreshWeather()
    {
        if (!isLoading)
        {
            StartCoroutine(FetchWeatherData());
        }
    }

    private IEnumerator FetchWeatherData()
    {
        isLoading = true;
        lastError = "";

        string url = BuildRequestUrl();
        Debug.Log($"[Weather] Requesting: {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ParseWeatherResponse(request.downloadHandler.text);
            }
            else
            {
                lastError = request.error;
                Debug.LogError($"[Weather] Request failed: {request.error}");
            }
        }

        isLoading = false;
    }

    private string BuildRequestUrl()
    {
        DateTime now = DateTime.Now;

        int minute = now.Minute < 30 ? 0 : 30;
        if (now.Minute < 10)
        {
            now = now.AddHours(-1);
            minute = 30;
        }

        string baseDate = now.ToString("yyyyMMdd");
        string baseTime = now.ToString("HH") + minute.ToString("00");

        string url = $"{baseUrl}?" +
                     $"serviceKey={serviceKey}" +
                     $"&pageNo=1" +
                     $"&numOfRows=10" +
                     $"&dataType=JSON" +
                     $"&base_date={baseDate}" +
                     $"&base_time={baseTime}" +
                     $"&nx={nx}" +
                     $"&ny={ny}";

        return url;
    }

    private void ParseWeatherResponse(string jsonResponse)
    {
        try
        {
            if (jsonResponse.Contains("SERVICE_KEY_IS_NOT_REGISTERED"))
            {
                lastError = "API key not registered";
                Debug.LogError($"[Weather] {lastError}");
                return;
            }

            if (jsonResponse.Contains("NO_DATA"))
            {
                lastError = "No data available";
                Debug.LogWarning($"[Weather] {lastError}");
                return;
            }

            Dictionary<string, float> weatherValues = new Dictionary<string, float>();

            string[] categories = { "T1H", "REH", "SKY", "PTY" };
            foreach (string cat in categories)
            {
                float value = ExtractValue(jsonResponse, cat);
                if (value != float.MinValue)
                {
                    weatherValues[cat] = value;
                }
            }

            if (weatherValues.ContainsKey("T1H"))
                OutdoorTemperature = weatherValues["T1H"];

            if (weatherValues.ContainsKey("REH"))
                OutdoorHumidity = weatherValues["REH"];

            if (weatherValues.ContainsKey("SKY"))
                SkyCondition = (int)weatherValues["SKY"];

            if (weatherValues.ContainsKey("PTY"))
                PrecipitationType = (int)weatherValues["PTY"];

            Debug.Log($"[Weather] Temp: {OutdoorTemperature}C, Humidity: {OutdoorHumidity}%, Sky: {SkyCondition}");

            UpdateUI();
            OnWeatherDataReceived?.Invoke(OutdoorTemperature, OutdoorHumidity, SkyCondition, PrecipitationType);
        }
        catch (Exception e)
        {
            lastError = $"Parse error: {e.Message}";
            Debug.LogError($"[Weather] {lastError}");
        }
    }

    private float ExtractValue(string json, string category)
    {
        string searchPattern = $"\"category\":\"{category}\"";
        int catIndex = json.IndexOf(searchPattern);

        if (catIndex < 0) return float.MinValue;

        int valueStart = json.IndexOf("\"obsrValue\"", catIndex);
        if (valueStart < 0 || valueStart > catIndex + 200) return float.MinValue;

        int colonIndex = json.IndexOf(":", valueStart);
        int quoteStart = json.IndexOf("\"", colonIndex);
        int quoteEnd = json.IndexOf("\"", quoteStart + 1);

        if (quoteStart < 0 || quoteEnd < 0) return float.MinValue;

        string valueStr = json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);

        if (float.TryParse(valueStr, out float result))
        {
            return result;
        }

        return float.MinValue;
    }

    private void UpdateUI()
    {
        if (temperatureText != null)
            temperatureText.text = $"{OutdoorTemperature:F1}C";

        if (humidityText != null)
            humidityText.text = $"{OutdoorHumidity:F0}%";

        if (skyConditionText != null)
            skyConditionText.text = GetSkyConditionText();

        if (lastUpdateText != null)
            lastUpdateText.text = $"Updated: {DateTime.Now:HH:mm}";
    }

    private string GetSkyConditionText()
    {
        switch (PrecipitationType)
        {
            case 1: return "Rain";
            case 2: return "Rain/Snow";
            case 3: return "Snow";
        }

        switch (SkyCondition)
        {
            case 1: return "Clear";
            case 3: return "Cloudy";
            case 4: return "Overcast";
            default: return "-";
        }
    }

    public string GetWeatherSummary()
    {
        return $"Outdoor: {OutdoorTemperature:F1}C / {OutdoorHumidity:F0}% / {GetSkyConditionText()}";
    }
}

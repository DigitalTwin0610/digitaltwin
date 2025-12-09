using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Papago API를 통해 다국어 텍스트를 한국어로 번역하는 매니저
/// 한국어가 아닌 경우에만 번역 API 호출
/// </summary>
public class PapagoManager : MonoBehaviour
{
    [Header("API Settings")]
    [SerializeField] private string clientId = "YOUR_CLIENT_ID";
    [SerializeField] private string clientSecret = "tNQbPahKMcoWhM2Dp4BQryPR3oWlEjVNJoCwa7Sh";

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // 이벤트
    public event Action<string, string> OnTranslationComplete; // (원문, 번역문)
    public event Action<string> OnTranslationError;

    public bool IsTranslating { get; private set; } = false;

    private const string API_URL = "https://openapi.naver.com/v1/papago/n2mt";

    #region Public Methods

    /// <summary>
    /// 텍스트 번역 (자동 언어 감지)
    /// </summary>
    public void Translate(string text, Action<string> callback)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            callback?.Invoke(text);
            return;
        }

        // 한국어인지 확인
        if (IsKorean(text))
        {
            Log("한국어 텍스트 - 번역 불필요");
            callback?.Invoke(text);
            return;
        }

        // 언어 감지 후 번역
        string sourceLang = DetectLanguage(text);
        Log($"언어 감지: {sourceLang}");

        StartCoroutine(TranslateCoroutine(text, sourceLang, "ko", callback));
    }

    /// <summary>
    /// API 키 설정
    /// </summary>
    public void SetCredentials(string id, string secret)
    {
        clientId = id;
        clientSecret = secret;
    }

    /// <summary>
    /// 텍스트가 한국어인지 확인
    /// </summary>
    public static bool IsKorean(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;

        // 한글 유니코드 범위: AC00-D7AF (완성형), 1100-11FF (자모)
        int koreanCount = 0;
        int totalCount = 0;

        foreach (char c in text)
        {
            if (char.IsLetter(c))
            {
                totalCount++;
                if ((c >= 0xAC00 && c <= 0xD7AF) || (c >= 0x1100 && c <= 0x11FF))
                {
                    koreanCount++;
                }
            }
        }

        // 50% 이상이 한글이면 한국어로 판단
        return totalCount > 0 && (float)koreanCount / totalCount > 0.5f;
    }

    #endregion

    #region Language Detection

    private string DetectLanguage(string text)
    {
        // 일본어 (히라가나, 가타카나)
        if (Regex.IsMatch(text, @"[\u3040-\u309F\u30A0-\u30FF]"))
        {
            return "ja";
        }

        // 중국어 (한자만 있고 히라가나/가타카나 없음)
        if (Regex.IsMatch(text, @"[\u4E00-\u9FFF]") && 
            !Regex.IsMatch(text, @"[\u3040-\u309F\u30A0-\u30FF]"))
        {
            return "zh-CN";
        }

        // 기본: 영어
        return "en";
    }

    #endregion

    #region API Call

    private IEnumerator TranslateCoroutine(string text, string sourceLang, string targetLang, Action<string> callback)
    {
        if (string.IsNullOrEmpty(clientId) || clientId == "YOUR_CLIENT_ID")
        {
            LogError("Client ID가 설정되지 않았습니다.");
            callback?.Invoke(text);
            yield break;
        }

        IsTranslating = true;
        Log($"번역 요청: [{sourceLang}→{targetLang}] {text}");

        // POST 데이터 생성
        string postData = $"source={sourceLang}&target={targetLang}&text={UnityWebRequest.EscapeURL(text)}";
        byte[] bodyRaw = Encoding.UTF8.GetBytes(postData);

        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            request.SetRequestHeader("X-Naver-Client-Id", clientId);
            request.SetRequestHeader("X-Naver-Client-Secret", clientSecret);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string result = ParseTranslationResponse(request.downloadHandler.text);
                Log($"번역 완료: {result}");
                
                OnTranslationComplete?.Invoke(text, result);
                callback?.Invoke(result);
            }
            else
            {
                LogError($"번역 API 오류: {request.error}");
                LogError($"응답: {request.downloadHandler.text}");
                
                OnTranslationError?.Invoke(request.error);
                callback?.Invoke(text); // 원문 반환
            }
        }

        IsTranslating = false;
    }

    private string ParseTranslationResponse(string json)
    {
        try
        {
            Log($"응답 JSON: {json}");

            // "translatedText":"번역된 텍스트" 패턴 찾기
            int index = json.IndexOf("\"translatedText\":\"");
            if (index >= 0)
            {
                int startIndex = index + 18;
                int endIndex = json.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    string translated = json.Substring(startIndex, endIndex - startIndex);
                    // 유니코드 이스케이프 처리
                    translated = DecodeUnicodeEscape(translated);
                    return translated;
                }
            }

            LogError("번역 결과를 찾을 수 없음");
            return "";
        }
        catch (Exception e)
        {
            LogError($"번역 파싱 오류: {e.Message}");
            return "";
        }
    }

    private string DecodeUnicodeEscape(string text)
    {
        return Regex.Replace(text, @"\\u([0-9a-fA-F]{4})", match =>
        {
            return ((char)int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber)).ToString();
        });
    }

    #endregion

    #region Debug

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[PapagoManager] {message}");
        }
    }

    private void LogError(string message)
    {
        Debug.LogError($"[PapagoManager] {message}");
    }

    #endregion
}

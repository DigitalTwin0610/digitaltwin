using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class LampTwinSync : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public static LampTwinSync Instance { get; private set; }

    [Header("3D Model")]
    public Transform lampShadeTransform;
    public Transform ledTransform;
    public Light ledLight;
    public Material ledMaterial;

    [Header("UI - Rotation Control")]
    public Slider angleSlider;
    public Button btn0Degree;
    public Button btn90Degree;
    public Button btn180Degree;

    [Header("UI - Display")]
    public TMP_Text angleValueText;
    public TMP_Text sourceText;
    public Image syncIndicator;

    [Header("Settings")]
    public float dragSensitivity = 0.5f;
    public float smoothSpeed = 5f;

    private float currentAngle = 90f;
    private float targetAngle = 90f;
    private bool isUpdatingFromSerial = false;
    private bool isDragging = false;
    private Vector2 dragStartPosition;
    private float dragStartAngle;

    private Color dialColor = new Color(0.2f, 0.8f, 0.4f);
    private Color unityColor = new Color(0.2f, 0.6f, 1f);

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        InitializeUI();
        SubscribeToEvents();
    }

    void Update()
    {
        SmoothRotation();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void InitializeUI()
    {
        if (angleSlider != null)
        {
            angleSlider.minValue = 0;
            angleSlider.maxValue = 180;
            angleSlider.value = 90;
            angleSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (btn0Degree != null)
            btn0Degree.onClick.AddListener(() => SetAngle(0, "UNITY"));

        if (btn90Degree != null)
            btn90Degree.onClick.AddListener(() => SetAngle(90, "UNITY"));

        if (btn180Degree != null)
            btn180Degree.onClick.AddListener(() => SetAngle(180, "UNITY"));

        UpdateUI();
    }

    private void SubscribeToEvents()
    {
        if (SerialController.Instance != null)
        {
            SerialController.Instance.OnServoAngleReceived += OnServoAngleReceived;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (SerialController.Instance != null)
        {
            SerialController.Instance.OnServoAngleReceived -= OnServoAngleReceived;
        }
    }

    private void OnServoAngleReceived(int angle, string source)
    {
        if (source == "DIAL")
        {
            isUpdatingFromSerial = true;
            targetAngle = angle;

            if (angleSlider != null)
            {
                angleSlider.SetValueWithoutNotify(angle);
            }

            UpdateSourceDisplay(source);
            isUpdatingFromSerial = false;
        }
    }

    private void OnSliderValueChanged(float value)
    {
        if (isUpdatingFromSerial) return;
        if (isDragging) return;

        SetAngle((int)value, "UNITY");
    }

    public void SetAngle(int angle, string source)
    {
        angle = Mathf.Clamp(angle, 0, 180);
        targetAngle = angle;

        if (SerialController.Instance != null)
        {
            SerialController.Instance.SendServoAngle(angle);
        }

        if (angleSlider != null && !isUpdatingFromSerial)
        {
            angleSlider.SetValueWithoutNotify(angle);
        }

        UpdateSourceDisplay(source);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        dragStartPosition = eventData.position;
        dragStartAngle = currentAngle;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        float deltaX = (eventData.position.x - dragStartPosition.x) * dragSensitivity;
        float newAngle = Mathf.Clamp(dragStartAngle + deltaX, 0f, 180f);

        targetAngle = newAngle;

        if (angleSlider != null)
        {
            angleSlider.SetValueWithoutNotify(newAngle);
        }

        UpdateSourceDisplay("UNITY");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        if (SerialController.Instance != null)
        {
            SerialController.Instance.SendServoAngle((int)targetAngle);
        }
    }

    private void SmoothRotation()
    {
        if (Mathf.Abs(currentAngle - targetAngle) > 0.1f)
        {
            currentAngle = Mathf.Lerp(currentAngle, targetAngle, Time.deltaTime * smoothSpeed);
            ApplyRotation(currentAngle);
            UpdateUI();
        }
    }

    private void ApplyRotation(float angle)
    {
        if (lampShadeTransform != null)
        {
            lampShadeTransform.localRotation = Quaternion.Euler(0, angle, 0);
        }
    }

    private void UpdateUI()
    {
        if (angleValueText != null)
        {
            angleValueText.text = $"{Mathf.RoundToInt(currentAngle)}°";
        }
    }

    private void UpdateSourceDisplay(string source)
    {
        if (sourceText != null)
        {
            sourceText.text = source == "DIAL" ? "Physical Dial" : "Unity Control";
            sourceText.color = source == "DIAL" ? dialColor : unityColor;
        }

        if (syncIndicator != null)
        {
            syncIndicator.color = source == "DIAL" ? dialColor : unityColor;
        }
    }

    public void SetLEDColor(Color color)
    {
        if (ledMaterial != null)
        {
            ledMaterial.color = color;
            ledMaterial.SetColor("_EmissionColor", color * 2f);
        }

        if (ledLight != null)
        {
            ledLight.color = color;
        }
    }

    public void SetLEDIntensity(float intensity)
    {
        if (ledLight != null)
        {
            ledLight.intensity = intensity;
        }
    }
}

# Smart Weather Lamp - Unity UI 완전 상세 설정 가이드

## 📋 목차
1. [사전 준비](#1-사전-준비)
2. [Canvas 기본 설정](#2-canvas-기본-설정)
3. [TempPanel - 온도 비교](#3-temppanel---온도-비교)
4. [HumidPanel - 습도 비교](#4-humidpanel---습도-비교)
5. [GaugePanel - 온도차 게이지](#5-gaugepanel---온도차-게이지)
6. [WeatherPanel - 날씨 상태](#6-weatherpanel---날씨-상태)
7. [TwinPanel - 3D 뷰](#7-twinpanel---3d-뷰)
8. [3D 램프 모델 생성](#8-3d-램프-모델-생성)
9. [ControlPanel - 방향 컨트롤](#9-controlpanel---방향-컨트롤)
10. [LEDControlPanel - LED 컨트롤](#10-ledcontrolpanel---led-컨트롤)
11. [스크립트 연결](#11-스크립트-연결)

---

## 1. 사전 준비

### 1.1 TextMeshPro 설치
```
Window → Package Manager → Unity Registry → TextMeshPro → Install
```
설치 후 팝업이 뜨면 **Import TMP Essentials** 클릭

### 1.2 API Compatibility Level 설정
```
Edit → Project Settings → Player → Other Settings
Api Compatibility Level → .NET Framework
```

---

## 2. Canvas 기본 설정

### 2.1 Canvas 생성
```
Hierarchy 우클릭 → UI → Canvas
```

### 2.2 Canvas 컴포넌트 설정
Inspector에서:
| 속성 | 값 |
|------|-----|
| Render Mode | Screen Space - Overlay |
| UI Scale Mode | Scale With Screen Size |
| Reference Resolution | X: 1920, Y: 1080 |
| Screen Match Mode | Match Width Or Height |
| Match | 0.5 |

### 2.3 배경 Panel 생성
```
Canvas 우클릭 → UI → Panel
이름: Background
```

**RectTransform 설정:**
| 속성 | 값 |
|------|-----|
| Anchor | Stretch - Stretch (사각형 아이콘 우하단) |
| Left, Top, Right, Bottom | 모두 0 |

**Image 컴포넌트:**
| 속성 | 값 |
|------|-----|
| Color | #1A1A2E (R:26, G:26, B:46, A:255) |

---

## 3. TempPanel - 온도 비교

### 3.1 TempPanel 생성
```
Background 우클릭 → UI → Panel
이름: TempPanel
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Top-Left |
| Pivot | (0, 1) |
| Pos X | 30 |
| Pos Y | -30 |
| Width | 350 |
| Height | 250 |

**Image 컴포넌트:**
| 속성 | 값 |
|------|-----|
| Color | #16213E (R:22, G:33, B:62, A:230) |

### 3.2 TitleText 생성
```
TempPanel 우클릭 → UI → Text - TextMeshPro
이름: TitleText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Top-Stretch (상단 가로 늘림) |
| Pivot | (0.5, 1) |
| Pos Y | -10 |
| Height | 40 |
| Left, Right | 10 |

**TextMeshPro 컴포넌트:**
| 속성 | 값 |
|------|-----|
| Text | 🌡️ 온도 비교 |
| Font Size | 24 |
| Alignment | Center, Middle |
| Color | White |
| Font Style | Bold |

### 3.3 IndoorBar 생성

#### 3.3.1 IndoorBar 컨테이너
```
TempPanel 우클릭 → Create Empty
이름: IndoorBar
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Top-Left |
| Pivot | (0, 1) |
| Pos X | 40 |
| Pos Y | -70 |
| Width | 60 |
| Height | 140 |

#### 3.3.2 BarBackground 생성
```
IndoorBar 우클릭 → UI → Image
이름: BarBackground
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Stretch - Stretch |
| Left, Top, Right, Bottom | 모두 0 |

**Image 컴포넌트:**
| 속성 | 값 |
|------|-----|
| Color | #333333 (어두운 회색) |

#### 3.3.3 BarFill 생성 ⭐ 중요
```
IndoorBar 우클릭 → UI → Image
이름: BarFill
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Stretch - Stretch |
| Left | 4 |
| Top | 4 |
| Right | 4 |
| Bottom | 4 |

**Image 컴포넌트:** ⭐ 핵심 설정
| 속성 | 값 |
|------|-----|
| Color | #FF6B35 (주황색) |
| Image Type | Filled |
| Fill Method | Vertical |
| Fill Origin | Bottom |
| Fill Amount | 0.5 (테스트용) |

> 💡 **Fill Amount**가 0~1 사이 값으로 바의 높이를 조절합니다.
> 스크립트에서 `image.fillAmount = 0.7f;` 형태로 제어합니다.

#### 3.3.4 ValueText 생성
```
IndoorBar 우클릭 → UI → Text - TextMeshPro
이름: ValueText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Bottom-Center |
| Pivot | (0.5, 1) |
| Pos X | 0 |
| Pos Y | -10 |
| Width | 80 |
| Height | 30 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | 22.5°C |
| Font Size | 18 |
| Alignment | Center |
| Color | #FF6B35 |

### 3.4 OutdoorBar 생성
IndoorBar와 동일한 구조로 생성

**차이점:**
| 항목 | IndoorBar | OutdoorBar |
|------|-----------|------------|
| Pos X | 40 | 140 |
| BarFill Color | #FF6B35 (주황) | #4A90D9 (파랑) |
| ValueText Color | #FF6B35 | #4A90D9 |

### 3.5 Labels 생성

#### 3.5.1 Labels 컨테이너
```
TempPanel 우클릭 → Create Empty
이름: Labels
```

#### 3.5.2 IndoorLabel
```
Labels 우클릭 → UI → Text - TextMeshPro
이름: IndoorLabel
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | 70 |
| Pos Y | -220 |
| Width | 60 |
| Height | 25 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | 실내 |
| Font Size | 16 |
| Alignment | Center |
| Color | #AAAAAA |

#### 3.5.3 OutdoorLabel
```
IndoorLabel 복제 (Ctrl+D)
이름: OutdoorLabel
```

| 속성 | 값 |
|------|-----|
| Pos X | 170 |
| Text | 외부 |

### 3.6 TempPanel 최종 구조
```
TempPanel/
├── TitleText (TMP)
├── IndoorBar/
│   ├── BarBackground (Image)
│   ├── BarFill (Image, Filled) ⭐
│   └── ValueText (TMP)
├── OutdoorBar/
│   ├── BarBackground (Image)
│   ├── BarFill (Image, Filled) ⭐
│   └── ValueText (TMP)
└── Labels/
    ├── IndoorLabel (TMP)
    └── OutdoorLabel (TMP)
```

---

## 4. HumidPanel - 습도 비교

### 4.1 TempPanel 복제
```
TempPanel 선택 → Ctrl+D
이름: HumidPanel
```

### 4.2 위치 변경
**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos Y | -300 |

### 4.3 내용 변경
| 자식 요소 | 변경 내용 |
|----------|----------|
| TitleText | 💧 습도 비교 |
| IndoorBar/BarFill Color | #4ECDC4 (청록색) |
| IndoorBar/ValueText | 45% |
| OutdoorBar/BarFill Color | #45B7D1 (하늘색) |
| OutdoorBar/ValueText | 60% |

---

## 5. GaugePanel - 온도차 게이지

### 5.1 GaugePanel 생성
```
Background 우클릭 → UI → Panel
이름: GaugePanel
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Bottom-Left |
| Pivot | (0, 0) |
| Pos X | 30 |
| Pos Y | 30 |
| Width | 350 |
| Height | 200 |

**Image:**
| 속성 | 값 |
|------|-----|
| Color | #16213E (A:230) |

### 5.2 TitleText
```
GaugePanel 우클릭 → UI → Text - TextMeshPro
이름: TitleText
```

| 속성 | 값 |
|------|-----|
| Text | ⚠️ 온도차 위험도 |
| Pos Y | -10 |
| Font Size | 20 |
| Alignment | Center |

### 5.3 GaugeBackground (반원 배경)
```
GaugePanel 우클릭 → UI → Image
이름: GaugeBackground
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Middle-Center |
| Pos X | 0 |
| Pos Y | -20 |
| Width | 200 |
| Height | 100 |

**Image:**
| 속성 | 값 |
|------|-----|
| Color | #333333 |
| Image Type | Filled |
| Fill Method | Radial 180 |
| Fill Origin | Bottom |
| Fill Amount | 1 |

> 💡 반원 이미지가 없다면 일반 사각형으로 대체해도 됩니다.

### 5.4 GaugeFill (색상 변하는 부분)
```
GaugePanel 우클릭 → UI → Image
이름: GaugeFill
```

**RectTransform:** (GaugeBackground와 동일)
| 속성 | 값 |
|------|-----|
| Pos X | 0 |
| Pos Y | -20 |
| Width | 190 |
| Height | 95 |

**Image:**
| 속성 | 값 |
|------|-----|
| Color | #4ADE80 (초록, 안전) |
| Image Type | Filled |
| Fill Method | Radial 180 |
| Fill Origin | Bottom |

### 5.5 GaugeNeedle (바늘) ⭐ 중요
```
GaugePanel 우클릭 → UI → Image
이름: GaugeNeedle
```

**RectTransform:** ⭐ Pivot 설정이 핵심!
| 속성 | 값 |
|------|-----|
| Anchor | Middle-Center |
| **Pivot** | **(0.5, 0)** ← 바늘 회전 중심 |
| Pos X | 0 |
| Pos Y | -70 |
| Width | 6 |
| Height | 80 |
| Rotation Z | 90 (초기값, 왼쪽 끝) |

**Image:**
| 속성 | 값 |
|------|-----|
| Color | #FF0000 (빨강) |

> 💡 **Pivot (0.5, 0)**: 바늘의 아래쪽 중앙을 회전 중심으로 설정
> 스크립트에서 `needle.localRotation = Quaternion.Euler(0, 0, -angle + 90);`

### 5.6 DiffText (온도차 표시)
```
GaugePanel 우클릭 → UI → Text - TextMeshPro
이름: DiffText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | 0 |
| Pos Y | -130 |
| Width | 150 |
| Height | 40 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | +15.0°C |
| Font Size | 28 |
| Font Style | Bold |
| Alignment | Center |
| Color | #FFFFFF |

### 5.7 WarningText
```
GaugePanel 우클릭 → UI → Text - TextMeshPro
이름: WarningText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | 0 |
| Pos Y | -165 |
| Width | 200 |
| Height | 30 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | 🚨 외출 주의! |
| Font Size | 18 |
| Alignment | Center |
| Color | #FF6B6B |

### 5.8 GaugePanel 최종 구조
```
GaugePanel/
├── TitleText (TMP)
├── GaugeBackground (Image, Filled)
├── GaugeFill (Image, Filled) ⭐
├── GaugeNeedle (Image, Pivot: 0.5, 0) ⭐
├── DiffText (TMP) ⭐
└── WarningText (TMP)
```

---

## 6. WeatherPanel - 날씨 상태

### 6.1 WeatherPanel 생성
```
Background 우클릭 → UI → Panel
이름: WeatherPanel
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Middle-Left |
| Pivot | (0, 0.5) |
| Pos X | 30 |
| Pos Y | 0 |
| Width | 350 |
| Height | 150 |

**Image:**
| 속성 | 값 |
|------|-----|
| Color | #16213E (A:230) |

### 6.2 EmojiText (날씨 아이콘)
```
WeatherPanel 우클릭 → UI → Text - TextMeshPro
이름: EmojiText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Middle-Left |
| Pos X | 30 |
| Pos Y | 10 |
| Width | 80 |
| Height | 80 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | ☀️ |
| Font Size | 60 |
| Alignment | Center, Middle |

### 6.3 StatusText
```
WeatherPanel 우클릭 → UI → Text - TextMeshPro
이름: StatusText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | 130 |
| Pos Y | 20 |
| Width | 200 |
| Height | 50 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | 맑음 |
| Font Size | 32 |
| Font Style | Bold |
| Alignment | Left, Middle |
| Color | #FFFFFF |

### 6.4 UpdateText
```
WeatherPanel 우클릭 → UI → Text - TextMeshPro
이름: UpdateText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | 130 |
| Pos Y | -30 |
| Width | 200 |
| Height | 25 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | 마지막 갱신: 14:30 |
| Font Size | 14 |
| Color | #888888 |

### 6.5 WeatherPanel 최종 구조
```
WeatherPanel/
├── EmojiText (TMP) - 큰 이모지 ⭐
├── StatusText (TMP) ⭐
└── UpdateText (TMP) ⭐
```

---

## 7. TwinPanel - 3D 뷰

### 7.1 TwinPanel 생성
```
Background 우클릭 → UI → Panel
이름: TwinPanel
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Top-Right |
| Pivot | (1, 1) |
| Pos X | -30 |
| Pos Y | -30 |
| Width | 450 |
| Height | 350 |

**Image:**
| 속성 | 값 |
|------|-----|
| Color | #0F3460 |

### 7.2 TitleText
```
TwinPanel 우클릭 → UI → Text - TextMeshPro
이름: TitleText
```

| 속성 | 값 |
|------|-----|
| Text | 🏠 Digital Twin |
| Font Size | 20 |
| Pos Y | -10 |

### 7.3 RenderView (3D 모델 표시용)
```
TwinPanel 우클릭 → UI → Raw Image
이름: RenderView
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Stretch-Stretch |
| Left | 10 |
| Top | 45 |
| Right | 10 |
| Bottom | 10 |

> 💡 이 RawImage에 Render Texture를 연결하면 3D 모델이 UI에 표시됩니다.

---

## 8. 3D 램프 모델 생성

### 8.1 SmartLamp 부모 오브젝트
```
Hierarchy 우클릭 → Create Empty
이름: SmartLamp
Position: (0, 0, 0)
```

### 8.2 Base (받침대)
```
SmartLamp 우클릭 → 3D Object → Cylinder
이름: Base
```

| 속성 | 값 |
|------|-----|
| Position | (0, 0.05, 0) |
| Scale | (1, 0.1, 1) |

**Material:**
- Create → Material → 이름: `Base_Mat`
- Albedo Color: #444444 (회색)

### 8.3 Pole (지지대)
```
SmartLamp 우클릭 → 3D Object → Cylinder
이름: Pole
```

| 속성 | 값 |
|------|-----|
| Position | (0, 0.35, 0) |
| Scale | (0.1, 0.3, 0.1) |

**Material:** Base_Mat 재사용

### 8.4 ServoMount (서보 마운트)
```
SmartLamp 우클릭 → 3D Object → Cube
이름: ServoMount
```

| 속성 | 값 |
|------|-----|
| Position | (0, 0.65, 0) |
| Scale | (0.3, 0.15, 0.2) |

**Material:**
- 새 Material → 이름: `Servo_Mat`
- Albedo: #2196F3 (파란색)

### 8.5 LampShade (램프 갓) ⭐ 회전 대상
```
SmartLamp 우클릭 → 3D Object → Capsule
이름: LampShade
```

| 속성 | 값 |
|------|-----|
| Position | (0, 0.95, 0) |
| Rotation | (180, 0, 0) |
| Scale | (0.8, 0.4, 0.8) |

**Material:**
- 새 Material → 이름: `Shade_Mat`
- Albedo: #FFF8E7 (밝은 크림색)
- Smoothness: 0.3

> ⭐ **이 오브젝트가 스크립트에서 회전됩니다!**
> `LampTwinSync.cs`의 `lampShadeTransform`에 연결

### 8.6 LED (발광체) ⭐ 색상 변경 대상
```
SmartLamp 우클릭 → 3D Object → Sphere
이름: LED
```

| 속성 | 값 |
|------|-----|
| Position | (0, 0.8, 0) |
| Scale | (0.25, 0.25, 0.25) |

**LED Material 생성:** ⭐ Emission 설정
```
Assets 우클릭 → Create → Material
이름: LED_Mat
```

| 속성 | 값 |
|------|-----|
| Shader | Standard |
| Albedo | #00FF00 (초록) |
| **Emission** | **✓ 체크** |
| Emission Color | #00FF00 (HDR 강도 2) |

### 8.7 Point Light 추가
```
LED 우클릭 → Light → Point Light
이름: LEDLight
```

| 속성 | 값 |
|------|-----|
| Position | (0, 0, 0) (LED 기준 로컬) |
| Color | #00FF00 |
| Intensity | 2 |
| Range | 3 |

### 8.8 SmartLamp 최종 구조
```
SmartLamp/                    [LampTwinSync.cs 연결]
├── Base (Cylinder)
├── Pole (Cylinder)
├── ServoMount (Cube)
├── LampShade (Capsule)       ⭐ lampShadeTransform
└── LED (Sphere)              ⭐ ledTransform
    └── LEDLight (Point Light) ⭐ ledLight
```

### 8.9 Render Texture 설정 (3D → UI 표시)

#### 8.9.1 Render Texture 생성
```
Assets 우클릭 → Create → Render Texture
이름: LampRenderTexture
```

| 속성 | 값 |
|------|-----|
| Size | 512 x 512 |
| Color Format | ARGB32 |
| Depth Buffer | 24 bit |

#### 8.9.2 전용 카메라 생성
```
Hierarchy 우클릭 → Camera
이름: LampCamera
```

| 속성 | 값 |
|------|-----|
| Position | (0, 1, -2) |
| Rotation | (15, 0, 0) |
| Clear Flags | Solid Color |
| Background | #0F3460 |
| **Target Texture** | **LampRenderTexture** |
| Culling Mask | Everything (또는 Lamp 레이어만) |

#### 8.9.3 RawImage에 연결
```
TwinPanel/RenderView 선택
Inspector → Raw Image → Texture → LampRenderTexture
```

---

## 9. ControlPanel - 방향 컨트롤

### 9.1 ControlPanel 생성
```
Background 우클릭 → UI → Panel
이름: ControlPanel
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Middle-Right |
| Pivot | (1, 0.5) |
| Pos X | -30 |
| Pos Y | 50 |
| Width | 450 |
| Height | 180 |

**Image:**
| 속성 | 값 |
|------|-----|
| Color | #16213E (A:230) |

### 9.2 TitleText
```
ControlPanel 우클릭 → UI → Text - TextMeshPro
이름: TitleText
```

| 속성 | 값 |
|------|-----|
| Text | 🎚️ 램프 방향 제어 |
| Font Size | 18 |
| Pos Y | -10 |

### 9.3 AngleSlider ⭐
```
ControlPanel 우클릭 → UI → Slider
이름: AngleSlider
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | 0 |
| Pos Y | -50 |
| Width | 350 |
| Height | 30 |

**Slider 컴포넌트:** ⭐
| 속성 | 값 |
|------|-----|
| Min Value | 0 |
| Max Value | 180 |
| Value | 90 |
| Whole Numbers | ✓ |

**슬라이더 색상 변경:**
```
AngleSlider/Fill Area/Fill → Image Color: #E94560
AngleSlider/Handle Slide Area/Handle → Image Color: #FFFFFF
```

### 9.4 AngleText
```
ControlPanel 우클릭 → UI → Text - TextMeshPro
이름: AngleText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | 200 |
| Pos Y | -50 |
| Width | 80 |
| Height | 30 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | 90° |
| Font Size | 24 |
| Font Style | Bold |
| Alignment | Center |

### 9.5 PresetButtons 컨테이너
```
ControlPanel 우클릭 → Create Empty
이름: PresetButtons
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos Y | -100 |
| Width | 350 |
| Height | 40 |

### 9.6 프리셋 버튼들

#### Btn0
```
PresetButtons 우클릭 → UI → Button - TextMeshPro
이름: Btn0
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | -120 |
| Width | 80 |
| Height | 35 |

**Button → Image:**
| 속성 | 값 |
|------|-----|
| Color | #333333 |

**자식 Text:**
| 속성 | 값 |
|------|-----|
| Text | 0° |
| Font Size | 16 |

#### Btn90
Btn0 복제 후:
| 속성 | 값 |
|------|-----|
| Pos X | 0 |
| Text | 90° |

#### Btn180
Btn0 복제 후:
| 속성 | 값 |
|------|-----|
| Pos X | 120 |
| Text | 180° |

### 9.7 SourceText
```
ControlPanel 우클릭 → UI → Text - TextMeshPro
이름: SourceText
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos Y | -145 |
| Width | 300 |
| Height | 25 |

**TextMeshPro:**
| 속성 | 값 |
|------|-----|
| Text | ⏸️ 대기 중 |
| Font Size | 14 |
| Color | #888888 |
| Alignment | Center |

### 9.8 ControlPanel 최종 구조
```
ControlPanel/
├── TitleText (TMP)
├── AngleSlider (Slider) ⭐
├── AngleText (TMP) ⭐
├── PresetButtons/
│   ├── Btn0 (Button) ⭐
│   ├── Btn90 (Button) ⭐
│   └── Btn180 (Button) ⭐
└── SourceText (TMP) ⭐
```

---

## 10. LEDControlPanel - LED 컨트롤

### 10.1 LEDControlPanel 생성
```
Background 우클릭 → UI → Panel
이름: LEDControlPanel
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Anchor | Bottom-Right |
| Pivot | (1, 0) |
| Pos X | -30 |
| Pos Y | 30 |
| Width | 450 |
| Height | 280 |

**Image:**
| 속성 | 값 |
|------|-----|
| Color | #16213E (A:230) |

### 10.2 TitleText
| 속성 | 값 |
|------|-----|
| Text | 💡 LED 컨트롤 |
| Font Size | 18 |

### 10.3 ModeToggleBtn ⭐
```
LEDControlPanel 우클릭 → UI → Button - TextMeshPro
이름: ModeToggleBtn
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | -150 |
| Pos Y | -50 |
| Width | 100 |
| Height | 40 |

**Button → Image:**
| 속성 | 값 |
|------|-----|
| Color | #4ADE80 (초록) |

**자식 Text:**
| 속성 | 값 |
|------|-----|
| Text | AUTO |
| Font Size | 18 |
| Font Style | Bold |

### 10.4 ModeIndicator
```
LEDControlPanel 우클릭 → UI → Image
이름: ModeIndicator
```

| 속성 | 값 |
|------|-----|
| Pos X | -80 |
| Pos Y | -50 |
| Width | 20 |
| Height | 20 |
| Color | #4ADE80 |

### 10.5 RGBSliders 컨테이너
```
LEDControlPanel 우클릭 → Create Empty
이름: RGBSliders
Pos Y: -100
```

#### RedSlider
```
RGBSliders 우클릭 → UI → Slider
이름: RedSlider
```

**RectTransform:**
| 속성 | 값 |
|------|-----|
| Pos X | 0 |
| Pos Y | 0 |
| Width | 250 |
| Height | 25 |

**Slider:**
| 속성 | 값 |
|------|-----|
| Min Value | 0 |
| Max Value | 255 |
| Value | 255 |
| Whole Numbers | ✓ |

**Fill Color:** #FF0000

#### RedLabel
```
RGBSliders 우클릭 → UI → Text - TextMeshPro
이름: RedLabel
```

| 속성 | 값 |
|------|-----|
| Pos X | -160 |
| Pos Y | 0 |
| Text | R |
| Font Size | 18 |
| Color | #FF0000 |

#### RedValue
```
RGBSliders 우클릭 → UI → Text - TextMeshPro
이름: RedValue
```

| 속성 | 값 |
|------|-----|
| Pos X | 160 |
| Pos Y | 0 |
| Text | 255 |
| Font Size | 14 |

#### GreenSlider, BlueSlider
위와 동일한 구조, Y 위치만 -35, -70으로 변경
색상: Green(#00FF00), Blue(#0000FF)

### 10.6 BrightnessSlider
```
LEDControlPanel 우클릭 → UI → Slider
이름: BrightnessSlider
```

| 속성 | 값 |
|------|-----|
| Pos Y | -180 |
| Width | 300 |
| Min/Max | 0 ~ 255 |

### 10.7 LEDPreview ⭐
```
LEDControlPanel 우클릭 → UI → Image
이름: LEDPreview
```

| 속성 | 값 |
|------|-----|
| Pos X | 150 |
| Pos Y | -100 |
| Width | 80 |
| Height | 80 |
| Color | #FFFFFF (시작 색) |

### 10.8 RGBValueText
```
LEDControlPanel 우클릭 → UI → Text - TextMeshPro
이름: RGBValueText
```

| 속성 | 값 |
|------|-----|
| Pos Y | -230 |
| Text | R:255 G:255 B:255 |
| Font Size | 14 |

### 10.9 AutoReasonText
```
LEDControlPanel 우클릭 → UI → Text - TextMeshPro
이름: AutoReasonText
```

| 속성 | 값 |
|------|-----|
| Pos Y | -255 |
| Text | ✓ 쾌적 구간 |
| Font Size | 16 |
| Color | #4ADE80 |

### 10.10 LEDControlPanel 최종 구조
```
LEDControlPanel/
├── TitleText (TMP)
├── ModeToggleBtn (Button) ⭐
├── ModeIndicator (Image) ⭐
├── RGBSliders/
│   ├── RedSlider + RedLabel + RedValue ⭐
│   ├── GreenSlider + GreenLabel + GreenValue ⭐
│   └── BlueSlider + BlueLabel + BlueValue ⭐
├── BrightnessSlider ⭐
├── LEDPreview (Image) ⭐
├── RGBValueText (TMP) ⭐
└── AutoReasonText (TMP) ⭐
```

---

## 11. 스크립트 연결

### 11.1 GameObjects 생성
```
Hierarchy 우클릭 → Create Empty
```

| 이름 | 연결할 스크립트 |
|------|----------------|
| GameManager | SerialController.cs |
| WeatherManager | WeatherAPIManager.cs |
| Visualizer | DataVisualizer.cs |
| LEDManager | LEDController.cs |

### 11.2 SerialController 연결
1. `GameManager` 선택
2. Add Component → SerialController
3. Inspector 설정:

| 속성 | 값 |
|------|-----|
| Port Name | COM3 (Arduino 포트) |
| Baud Rate | 9600 |
| Auto Connect | ✓ |

### 11.3 WeatherAPIManager 연결
1. `WeatherManager` 선택
2. Add Component → WeatherAPIManager
3. Inspector 설정:

| 속성 | 연결 대상 |
|------|----------|
| Service Key | (API 키 입력) |
| Nx | 60 |
| Ny | 127 |
| Temperature Text | WeatherPanel/StatusText |
| Last Update Text | WeatherPanel/UpdateText |

### 11.4 DataVisualizer 연결
1. `Visualizer` 선택
2. Add Component → DataVisualizer
3. Inspector 설정:

| 속성 | 연결 대상 |
|------|----------|
| Indoor Temp Bar | TempPanel/IndoorBar/BarFill |
| Indoor Temp Text | TempPanel/IndoorBar/ValueText |
| Outdoor Temp Bar | TempPanel/OutdoorBar/BarFill |
| Outdoor Temp Text | TempPanel/OutdoorBar/ValueText |
| Indoor Humid Bar | HumidPanel/IndoorBar/BarFill |
| Indoor Humid Text | HumidPanel/IndoorBar/ValueText |
| Outdoor Humid Bar | HumidPanel/OutdoorBar/BarFill |
| Outdoor Humid Text | HumidPanel/OutdoorBar/ValueText |
| Gauge Needle | GaugePanel/GaugeNeedle |
| Gauge Fill | GaugePanel/GaugeFill |
| Temp Diff Text | GaugePanel/DiffText |
| Risk Level Text | GaugePanel/WarningText |

### 11.5 LampTwinSync 연결
1. `SmartLamp` 오브젝트 선택
2. Add Component → LampTwinSync
3. Inspector 설정:

| 속성 | 연결 대상 |
|------|----------|
| Lamp Shade Transform | SmartLamp/LampShade |
| Led Transform | SmartLamp/LED |
| Led Light | SmartLamp/LED/LEDLight |
| Led Material | LED_Mat |
| Angle Slider | ControlPanel/AngleSlider |
| Btn 0 Degree | ControlPanel/PresetButtons/Btn0 |
| Btn 90 Degree | ControlPanel/PresetButtons/Btn90 |
| Btn 180 Degree | ControlPanel/PresetButtons/Btn180 |
| Angle Value Text | ControlPanel/AngleText |
| Source Text | ControlPanel/SourceText |

### 11.6 LEDController 연결
1. `LEDManager` 선택
2. Add Component → LEDController
3. Inspector 설정:

| 속성 | 연결 대상 |
|------|----------|
| Mode Toggle Button | LEDControlPanel/ModeToggleBtn |
| Mode Button Text | ModeToggleBtn의 자식 Text |
| Mode Indicator | LEDControlPanel/ModeIndicator |
| Red Slider | LEDControlPanel/RGBSliders/RedSlider |
| Green Slider | LEDControlPanel/RGBSliders/GreenSlider |
| Blue Slider | LEDControlPanel/RGBSliders/BlueSlider |
| Brightness Slider | LEDControlPanel/BrightnessSlider |
| RGB Value Text | LEDControlPanel/RGBValueText |
| LED Preview | LEDControlPanel/LEDPreview |
| Auto Reason Text | LEDControlPanel/AutoReasonText |

---

## 12. 최종 Hierarchy 구조

```
Scene
├── Main Camera
├── Directional Light
├── EventSystem (자동 생성됨)
│
├── GameManager          [SerialController.cs]
├── WeatherManager       [WeatherAPIManager.cs]
├── Visualizer           [DataVisualizer.cs]
├── LEDManager           [LEDController.cs]
│
├── SmartLamp            [LampTwinSync.cs]
│   ├── Base
│   ├── Pole
│   ├── ServoMount
│   ├── LampShade        ← 회전 대상
│   └── LED
│       └── LEDLight
│
├── LampCamera           (Render Texture 용)
│
└── Canvas
    └── Background
        ├── TempPanel
        │   ├── TitleText
        │   ├── IndoorBar
        │   │   ├── BarBackground
        │   │   ├── BarFill      ← fillAmount
        │   │   └── ValueText
        │   ├── OutdoorBar
        │   │   └── (동일)
        │   └── Labels
        │
        ├── HumidPanel (TempPanel과 동일 구조)
        │
        ├── GaugePanel
        │   ├── GaugeBackground
        │   ├── GaugeFill
        │   ├── GaugeNeedle     ← rotation
        │   ├── DiffText
        │   └── WarningText
        │
        ├── WeatherPanel
        │   ├── EmojiText
        │   ├── StatusText
        │   └── UpdateText
        │
        ├── TwinPanel
        │   ├── TitleText
        │   └── RenderView      ← RawImage + RenderTexture
        │
        ├── ControlPanel
        │   ├── AngleSlider
        │   ├── AngleText
        │   ├── PresetButtons
        │   └── SourceText
        │
        └── LEDControlPanel
            ├── ModeToggleBtn
            ├── RGBSliders
            ├── BrightnessSlider
            ├── LEDPreview
            └── AutoReasonText
```

---

## 13. 테스트 체크리스트

- [ ] Play 모드에서 Console 오류 없음
- [ ] 슬라이더 조작 시 AngleText 값 변경됨
- [ ] 프리셋 버튼 클릭 시 슬라이더 이동
- [ ] 3D 램프 모델이 TwinPanel에 표시됨
- [ ] LED 색상 슬라이더 조작 시 LEDPreview 색상 변경
- [ ] AUTO/MANUAL 모드 토글 작동
- [ ] Serial 연결 시 Console에 "Connected" 메시지

---

끝! 이 가이드대로 따라하면 완전한 UI가 구성됩니다. 🎉
/*
 * EmoLamp - Arduino 코드
 * 
 * 기능:
 * 1. RGB LED 색상 제어 (Unity에서 명령 수신)
 * 2. 조도 센서 값 읽기 (Unity로 전송)
 * 
 * 회로 연결:
 * - RGB LED: D9(R), D10(G), D11(B), GND
 * - 조도센서(TEMT6000): A0(OUT), 5V(VCC), GND
 * 
 * 통신 프로토콜:
 * - 수신: "RGB:255,200,100" → RGB LED 색상 설정
 * - 송신: "LIGHT:72" → 조도 센서값 (0-100%)
 */

// ==================== 핀 설정 ====================
const int PIN_LED_R = 9;      // RGB LED Red (PWM)
const int PIN_LED_G = 10;     // RGB LED Green (PWM)
const int PIN_LED_B = 11;     // RGB LED Blue (PWM)
const int PIN_LIGHT = A0;     // 조도 센서 (Analog)

// ==================== 설정값 ====================
const int BAUD_RATE = 9600;
const int LIGHT_SEND_INTERVAL = 500;  // 조도 전송 주기 (ms)
const int LIGHT_THRESHOLD = 3;         // 변화 감지 임계값 (%)

// ==================== 변수 ====================
int currentR = 0;
int currentG = 255;
int currentB = 0;

int lastLightPercent = -1;
unsigned long lastLightSendTime = 0;

String inputBuffer = "";

// ==================== 초기화 ====================
void setup() {
    // Serial 초기화
    Serial.begin(BAUD_RATE);
    
    // LED 핀 설정
    pinMode(PIN_LED_R, OUTPUT);
    pinMode(PIN_LED_G, OUTPUT);
    pinMode(PIN_LED_B, OUTPUT);
    
    // 조도 센서 핀 설정
    pinMode(PIN_LIGHT, INPUT);
    
    // 초기 LED 색상 (초록)
    setLED(0, 255, 0);
    
    // 시작 메시지
    Serial.println("STATUS:OK");
    Serial.println("EmoLamp Arduino Ready");
    
    delay(100);
}

// ==================== 메인 루프 ====================
void loop() {
    // 1. Serial 명령 처리
    processSerial();
    
    // 2. 조도 센서 읽기 및 전송
    processLightSensor();
}

// ==================== Serial 처리 ====================
void processSerial() {
    while (Serial.available() > 0) {
        char c = Serial.read();
        
        if (c == '\n' || c == '\r') {
            if (inputBuffer.length() > 0) {
                parseCommand(inputBuffer);
                inputBuffer = "";
            }
        } else {
            inputBuffer += c;
            
            // 버퍼 오버플로우 방지
            if (inputBuffer.length() > 50) {
                inputBuffer = "";
            }
        }
    }
}

void parseCommand(String cmd) {
    cmd.trim();
    
    // 디버그 출력
    Serial.print("DEBUG:Received=");
    Serial.println(cmd);
    
    // RGB 명령: "RGB:255,200,100"
    if (cmd.startsWith("RGB:")) {
        String values = cmd.substring(4);
        
        int comma1 = values.indexOf(',');
        int comma2 = values.indexOf(',', comma1 + 1);
        
        if (comma1 > 0 && comma2 > comma1) {
            int r = values.substring(0, comma1).toInt();
            int g = values.substring(comma1 + 1, comma2).toInt();
            int b = values.substring(comma2 + 1).toInt();
            
            r = constrain(r, 0, 255);
            g = constrain(g, 0, 255);
            b = constrain(b, 0, 255);
            
            setLED(r, g, b);
            
            Serial.print("DEBUG:LED_SET=");
            Serial.print(r);
            Serial.print(",");
            Serial.print(g);
            Serial.print(",");
            Serial.println(b);
        }
    }
    // HSV 명령: "HSV:120,100,80"
    else if (cmd.startsWith("HSV:")) {
        String values = cmd.substring(4);
        
        int comma1 = values.indexOf(',');
        int comma2 = values.indexOf(',', comma1 + 1);
        
        if (comma1 > 0 && comma2 > comma1) {
            float h = values.substring(0, comma1).toFloat();
            float s = values.substring(comma1 + 1, comma2).toFloat();
            float v = values.substring(comma2 + 1).toFloat();
            
            setLEDFromHSV(h, s, v);
        }
    }
    // 상태 요청
    else if (cmd == "STATUS") {
        Serial.println("STATUS:OK");
    }
    // 조도 요청
    else if (cmd == "LIGHT") {
        sendLightValue(true);
    }
}

// ==================== LED 제어 ====================
void setLED(int r, int g, int b) {
    currentR = r;
    currentG = g;
    currentB = b;
    
    // PWM 출력 (Common Cathode RGB LED)
    analogWrite(PIN_LED_R, r);
    analogWrite(PIN_LED_G, g);
    analogWrite(PIN_LED_B, b);
}

void setLEDFromHSV(float h, float s, float v) {
    // HSV to RGB 변환
    // h: 0-360, s: 0-100, v: 0-100
    
    h = fmod(h, 360.0f);
    if (h < 0) h += 360.0f;
    s = constrain(s, 0.0f, 100.0f) / 100.0f;
    v = constrain(v, 0.0f, 100.0f) / 100.0f;
    
    float c = v * s;
    float x = c * (1 - abs(fmod(h / 60.0f, 2) - 1));
    float m = v - c;
    
    float r1, g1, b1;
    
    if (h < 60) {
        r1 = c; g1 = x; b1 = 0;
    } else if (h < 120) {
        r1 = x; g1 = c; b1 = 0;
    } else if (h < 180) {
        r1 = 0; g1 = c; b1 = x;
    } else if (h < 240) {
        r1 = 0; g1 = x; b1 = c;
    } else if (h < 300) {
        r1 = x; g1 = 0; b1 = c;
    } else {
        r1 = c; g1 = 0; b1 = x;
    }
    
    int r = (int)((r1 + m) * 255);
    int g = (int)((g1 + m) * 255);
    int b = (int)((b1 + m) * 255);
    
    setLED(r, g, b);
}

// ==================== 조도 센서 ====================
void processLightSensor() {
    unsigned long now = millis();
    
    // 전송 주기 확인
    if (now - lastLightSendTime < LIGHT_SEND_INTERVAL) {
        return;
    }
    lastLightSendTime = now;
    
    sendLightValue(false);
}

void sendLightValue(bool force) {
    // 조도 센서 읽기 (0-1023)
    int rawValue = analogRead(PIN_LIGHT);
    
    // 퍼센트로 변환 (0-100)
    int lightPercent = map(rawValue, 0, 1023, 0, 100);
    lightPercent = constrain(lightPercent, 0, 100);
    
    // 변화가 있거나 강제 전송일 때만 전송
    if (force || abs(lightPercent - lastLightPercent) >= LIGHT_THRESHOLD) {
        lastLightPercent = lightPercent;
        
        Serial.print("LIGHT:");
        Serial.println(lightPercent);
    }
}

// ==================== 유틸리티 ====================
// 부동소수점 abs (Arduino 기본 abs는 int용)
float absf(float x) {
    return x < 0 ? -x : x;
}

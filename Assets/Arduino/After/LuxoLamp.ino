/*
 * Luxo Jr. 4관절 램프 - Arduino R4 WiFi
 * Unity Digital Twin 양방향 통신
 * 
 * 서보모터 4개 + 가변저항 4개 제어
 * 
 * 연결:
 * - DHT11: D2
 * - RGB LED: D9(R), D10(G), D11(B) + 220Ω 저항
 * - Servo 1 (베이스): D3
 * - Servo 2 (하부암): D5
 * - Servo 3 (엘보우): D6
 * - Servo 4 (헤드): D7
 * - Pot 1: A0
 * - Pot 2: A1
 * - Pot 3: A2
 * - Pot 4: A3
 * 
 * ⚠️ 서보 3개만 있다면: Servo 1 주석 처리하고 베이스 고정
 */

#include <Servo.h>
#include <DHT.h>

// ===== 핀 설정 =====
#define DHT_PIN 2
#define DHT_TYPE DHT11

#define LED_R 9
#define LED_G 10
#define LED_B 11

// 서보 핀 (4개)
#define SERVO1_PIN 3   // 베이스 회전
#define SERVO2_PIN 5   // 하부 암
#define SERVO3_PIN 6   // 엘보우
#define SERVO4_PIN 7   // 헤드

// 가변저항 핀 (4개)
#define POT1_PIN A0
#define POT2_PIN A1
#define POT3_PIN A2
#define POT4_PIN A3

// ===== 객체 생성 =====
DHT dht(DHT_PIN, DHT_TYPE);
Servo servo1, servo2, servo3, servo4;

// ===== 관절 각도 범위 (Unity와 동일하게) =====
// 서보는 0-180도만 가능하므로 오프셋 적용
struct JointConfig {
  float minAngle;   // 논리적 최소 (-180 등)
  float maxAngle;   // 논리적 최대 (180 등)
  int servoMin;     // 서보 최소 (보통 0)
  int servoMax;     // 서보 최대 (보통 180)
};

JointConfig joints[4] = {
  { -180, 180, 0, 180 },  // J1: 베이스 (-180~180 → 0~180)
  { -90, 90, 0, 180 },    // J2: 하부암 (-90~90 → 0~180)
  { -120, 120, 0, 180 },  // J3: 엘보우 (-120~120 → 0~180)
  { -90, 90, 0, 180 }     // J4: 헤드 (-90~90 → 0~180)
};

// ===== 상태 변수 =====
float currentAngles[4] = { 0, 50, -60, 30 };  // 현재 각도 (논리값)
float targetAngles[4] = { 0, 50, -60, 30 };   // 목표 각도
String lastSource = "NONE";

int currentR = 0, currentG = 255, currentB = 0;
int currentBrightness = 255;
char currentMode = 'A';

float temperature = 0;
float humidity = 0;

// ===== 타이밍 =====
unsigned long lastSensorRead = 0;
unsigned long lastSerialSend = 0;
unsigned long lastServoMove = 0;

const int SENSOR_INTERVAL = 2000;
const int SERIAL_INTERVAL = 100;
const int SERVO_MOVE_DELAY = 20;

// ===== 다이얼 노이즈 필터 =====
int lastPotValues[4] = { 0, 0, 0, 0 };
const int POT_THRESHOLD = 15;

void setup() {
  Serial.begin(9600);
  
  dht.begin();
  
  pinMode(LED_R, OUTPUT);
  pinMode(LED_G, OUTPUT);
  pinMode(LED_B, OUTPUT);
  
  // 서보 연결
  servo1.attach(SERVO1_PIN);
  servo2.attach(SERVO2_PIN);
  servo3.attach(SERVO3_PIN);
  servo4.attach(SERVO4_PIN);
  
  // 초기 위치
  moveAllServos();
  setLED(0, 255, 0);
  
  delay(1000);
  Serial.println("S:LUXO_READY");
}

void loop() {
  // 1. 센서 읽기
  if (millis() - lastSensorRead > SENSOR_INTERVAL) {
    lastSensorRead = millis();
    readSensors();
  }
  
  // 2. 다이얼 읽기
  readPotentiometers();
  
  // 3. 서보 부드럽게 이동
  if (millis() - lastServoMove > SERVO_MOVE_DELAY) {
    lastServoMove = millis();
    moveServosSmooth();
  }
  
  // 4. Unity 명령 수신
  if (Serial.available()) {
    String cmd = Serial.readStringUntil('\n');
    cmd.trim();
    parseCommand(cmd);
  }
  
  // 5. 상태 전송
  if (millis() - lastSerialSend > SERIAL_INTERVAL) {
    lastSerialSend = millis();
    sendStatus();
  }
}

// ===== 센서 읽기 =====
void readSensors() {
  float h = dht.readHumidity();
  float t = dht.readTemperature();
  
  if (!isnan(h) && !isnan(t)) {
    humidity = h;
    temperature = t;
  }
}

// ===== 4개 가변저항 읽기 =====
void readPotentiometers() {
  int potPins[4] = { POT1_PIN, POT2_PIN, POT3_PIN, POT4_PIN };
  bool anyChanged = false;
  
  for (int i = 0; i < 4; i++) {
    int potValue = analogRead(potPins[i]);
    
    // 노이즈 필터
    if (abs(potValue - lastPotValues[i]) > POT_THRESHOLD) {
      lastPotValues[i] = potValue;
      
      // 0-1023 → 논리적 각도 범위로 매핑
      float angle = map(potValue, 0, 1023, 
                        joints[i].minAngle * 10, joints[i].maxAngle * 10) / 10.0;
      
      targetAngles[i] = angle;
      anyChanged = true;
    }
  }
  
  if (anyChanged) {
    lastSource = "DIAL";
  }
}

// ===== 논리 각도 → 서보 각도 변환 =====
int logicalToServo(int jointIndex, float logicalAngle) {
  JointConfig& j = joints[jointIndex];
  
  // 논리 범위 → 서보 범위로 매핑
  float ratio = (logicalAngle - j.minAngle) / (j.maxAngle - j.minAngle);
  int servoAngle = j.servoMin + ratio * (j.servoMax - j.servoMin);
  
  return constrain(servoAngle, j.servoMin, j.servoMax);
}

// ===== 서보 부드럽게 이동 =====
void moveServosSmooth() {
  bool anyMoved = false;
  
  for (int i = 0; i < 4; i++) {
    if (abs(currentAngles[i] - targetAngles[i]) > 0.5) {
      // 부드러운 이동
      if (currentAngles[i] < targetAngles[i]) {
        currentAngles[i] += 1.0;
      } else {
        currentAngles[i] -= 1.0;
      }
      anyMoved = true;
    }
  }
  
  if (anyMoved) {
    moveAllServos();
  }
}

// ===== 모든 서보 위치 적용 =====
void moveAllServos() {
  servo1.write(logicalToServo(0, currentAngles[0]));
  servo2.write(logicalToServo(1, currentAngles[1]));
  servo3.write(logicalToServo(2, currentAngles[2]));
  servo4.write(logicalToServo(3, currentAngles[3]));
}

// ===== Unity 명령 파싱 =====
void parseCommand(String cmd) {
  // LED 명령: LED:R,G,B,Brightness,Mode
  if (cmd.startsWith("LED:")) {
    String params = cmd.substring(4);
    int values[5];
    int idx = 0;
    int lastComma = -1;
    
    for (int i = 0; i <= params.length() && idx < 5; i++) {
      if (i == params.length() || params[i] == ',') {
        String val = params.substring(lastComma + 1, i);
        if (idx < 4) {
          values[idx] = val.toInt();
        } else {
          currentMode = val[0];
        }
        idx++;
        lastComma = i;
      }
    }
    
    currentR = constrain(values[0], 0, 255);
    currentG = constrain(values[1], 0, 255);
    currentB = constrain(values[2], 0, 255);
    currentBrightness = constrain(values[3], 0, 255);
    
    setLED(currentR, currentG, currentB);
  }
  // 4관절 명령: JOINTS:J1,J2,J3,J4
  else if (cmd.startsWith("JOINTS:")) {
    String params = cmd.substring(7);
    int idx = 0;
    int lastComma = -1;
    
    for (int i = 0; i <= params.length() && idx < 4; i++) {
      if (i == params.length() || params[i] == ',') {
        String val = params.substring(lastComma + 1, i);
        targetAngles[idx] = val.toFloat();
        idx++;
        lastComma = i;
      }
    }
    
    lastSource = "UNITY";
  }
  // 단일 서보 (기존 호환): ROT:angle
  else if (cmd.startsWith("ROT:")) {
    int angle = cmd.substring(4).toInt();
    targetAngles[3] = map(angle, 0, 180, joints[3].minAngle, joints[3].maxAngle);
    lastSource = "UNITY";
  }
}

// ===== LED 설정 =====
void setLED(int r, int g, int b) {
  r = map(r, 0, 255, 0, currentBrightness);
  g = map(g, 0, 255, 0, currentBrightness);
  b = map(b, 0, 255, 0, currentBrightness);
  
  analogWrite(LED_R, r);
  analogWrite(LED_G, g);
  analogWrite(LED_B, b);
}

// ===== 상태 전송 =====
void sendStatus() {
  String data = "T:" + String(temperature, 1) +
                ",H:" + String(humidity, 0) +
                ",R:" + String(currentR) +
                ",G:" + String(currentG) +
                ",B:" + String(currentB) +
                ",J1:" + String(currentAngles[0], 0) +
                ",J2:" + String(currentAngles[1], 0) +
                ",J3:" + String(currentAngles[2], 0) +
                ",J4:" + String(currentAngles[3], 0) +
                ",SRC:" + lastSource +
                ",S:OK";
  
  Serial.println(data);
}

/*
 * Smart Weather Lamp - Arduino R4 WiFi
 * Unity Digital Twin 양방향 통신
 * 
 * 연결:
 * - DHT11: D2
 * - RGB LED: D9(R), D10(G), D11(B) + 220Ω 저항
 * - Servo: D6
 * - Potentiometer: A0
 */

#include <Servo.h>
#include <DHT.h>

// ===== 핀 설정 =====
#define DHT_PIN 2
#define DHT_TYPE DHT11

#define LED_R 9
#define LED_G 10
#define LED_B 11

#define SERVO_PIN 6
#define POT_PIN A0

// ===== 객체 생성 =====
DHT dht(DHT_PIN, DHT_TYPE);
Servo servo;

// ===== 상태 변수 =====
int currentR = 0, currentG = 255, currentB = 0;
int currentBrightness = 255;
char currentMode = 'A';  // A: Auto, M: Manual

int currentServoAngle = 90;
int targetServoAngle = 90;
String lastSource = "NONE";

float temperature = 0;
float humidity = 0;

// ===== 타이밍 =====
unsigned long lastSensorRead = 0;
unsigned long lastSerialSend = 0;
unsigned long lastServoMove = 0;

const int SENSOR_INTERVAL = 2000;    // 2초마다 센서 읽기
const int SERIAL_INTERVAL = 100;     // 100ms마다 상태 전송
const int SERVO_MOVE_DELAY = 15;     // 서보 부드러운 이동

// ===== 다이얼 관련 =====
int lastPotValue = 0;
const int POT_THRESHOLD = 10;  // 노이즈 필터

void setup() {
  Serial.begin(9600);
  
  // DHT 센서
  dht.begin();
  
  // RGB LED
  pinMode(LED_R, OUTPUT);
  pinMode(LED_G, OUTPUT);
  pinMode(LED_B, OUTPUT);
  
  // 서보
  servo.attach(SERVO_PIN);
  servo.write(90);
  
  // 초기 LED (초록)
  setLED(0, 255, 0);
  
  delay(1000);
  Serial.println("S:READY");
}

void loop() {
  // 1. 센서 읽기
  if (millis() - lastSensorRead > SENSOR_INTERVAL) {
    lastSensorRead = millis();
    readSensors();
  }
  
  // 2. 다이얼(가변저항) 읽기
  readPotentiometer();
  
  // 3. 서보 부드럽게 이동
  if (millis() - lastServoMove > SERVO_MOVE_DELAY) {
    lastServoMove = millis();
    moveServoSmooth();
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

// ===== 가변저항(다이얼) 읽기 =====
void readPotentiometer() {
  int potValue = analogRead(POT_PIN);
  int potAngle = map(potValue, 0, 1023, 0, 180);
  
  // 노이즈 필터: 일정 이상 변화해야 반응
  if (abs(potAngle - lastPotValue) > POT_THRESHOLD) {
    lastPotValue = potAngle;
    targetServoAngle = potAngle;
    lastSource = "DIAL";  // 물리 다이얼에서 입력됨
  }
}

// ===== 서보 부드럽게 이동 =====
void moveServoSmooth() {
  if (currentServoAngle != targetServoAngle) {
    if (currentServoAngle < targetServoAngle) {
      currentServoAngle++;
    } else {
      currentServoAngle--;
    }
    servo.write(currentServoAngle);
  }
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
  // 서보 명령: ROT:angle
  else if (cmd.startsWith("ROT:")) {
    int angle = cmd.substring(4).toInt();
    angle = constrain(angle, 0, 180);
    targetServoAngle = angle;
    lastSource = "UNITY";  // Unity에서 입력됨
  }
}

// ===== LED 설정 =====
void setLED(int r, int g, int b) {
  // 밝기 적용
  r = map(r, 0, 255, 0, currentBrightness);
  g = map(g, 0, 255, 0, currentBrightness);
  b = map(b, 0, 255, 0, currentBrightness);
  
  analogWrite(LED_R, r);
  analogWrite(LED_G, g);
  analogWrite(LED_B, b);
}

// ===== 상태 전송 (Arduino → Unity) =====
void sendStatus() {
  String data = "T:" + String(temperature, 1) +
                ",H:" + String(humidity, 0) +
                ",R:" + String(currentR) +
                ",G:" + String(currentG) +
                ",B:" + String(currentB) +
                ",ROT:" + String(currentServoAngle) +
                ",SRC:" + lastSource +
                ",S:OK";
  
  Serial.println(data);
}

#include <Servo.h>
#include <math.h>
#include <DHTesp.h>
#include <protocol.h>
#include <ESP8266WiFi.h>
#include <VentController.h>

#define LED_PIN 12         // D6
#define RELAY_1_PIN 5      // D1
#define RELAY_2_PIN 4      // D2
#define DHT_PIN 14         // D5
#define UP_MID 0           // D3           // blue       // power brown
#define UPPER_TANK_FLOAT 2 // D4           // orange     // power brown
#define UP_BOT 13          // D7           // green      // power brown
#define LOW_TOP 10         // D8 / SD3 10
#define LOW_MID 3          // RX
#define LOW_BOT 1          // TX

bool ledState = false;
bool autoConnect = true;
bool autoReconnect = true;
bool isCalibrated = false;
bool setVent = false;
bool isRunning = false;
bool setupComplete = false;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

int minSpeed = 1060;
int maxSpeed = 1260;
int stopESC = 500;
int _ventSpeed = 0;

DHTesp dht;

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 129);
IPAddress repeater_ip(192, 168, 1, 254);
IPAddress subnet_mask(255, 255, 255, 0);
IPAddress RemoteIp(192, 168, 1, 18);

unsigned int localUdpPort = 4210;

enum Device
{
  VENT = 0,
  TANK = 1
};

int switchOnStr(const char *input)
{
  // Serial.printf("\nSwitching: %s\n", input);
  if (strcmp(input, "vt\0") == 0)
  {
    // Serial.println("Returning Vent");
    return 0;
  }
  else if (strcmp(input, "tk\0") == 0)
  {
    // Serial.println("Returning Tank");
    return 1;
  }
  else if (strcmp(input, "lv\0") == 0)
  {
    // Serial.println("Returning Levels");
    return 2;
  }

  // Serial.println("Returning Error");
  return -1;
}

void probeLevels()
{
  if (setupComplete)
  {
    float upper = 0.0f;
    char pData[6];
    int i = digitalRead(UPPER_TANK_FLOAT);
    upper = (i) ? 0.0f : 1.0f;
    sprintf(pData, "lv:%.2f", upper);
    Serial.printf("Depth: %0.2f\n", upper);
    sendMessage("tk", "01", "*", 1, pData);
  }
}

bool tankOn(int id, bool state)
{
  // Serial.printf("\nTurning tank %d %s\n", id, (state) ? "on" : "off");

  switch (id)
  {
  case 1:
    // Relay pin 1
    digitalWrite(RELAY_1_PIN, state);
    break;
  case 2:
    // Relay pin 2
    digitalWrite(RELAY_2_PIN, state);
    break;
  default:
    break;
  };

  return true;
}

bool ventOn(bool state, int speed = 0)
{
#ifdef VENT_ENABLED
  if (speed == 0)
    Serial.printf("\nTurning vent %s\n", (state) ? "on" : "off");
  else
    Serial.printf("\nTurning vent %s to: %d\n", (state) ? "on" : "off", speed);

  if (state)
  {
    bool sendCalState = false;
    if (!isCalibrated)
    {
      sendCalState = true;
      while (!calibrate(LED_PIN, RELAY_2_PIN, D3, minSpeed, maxSpeed, stopESC))
        ;
      isCalibrated = true;
      isRunning = false;
      Serial.println("Calibrated");
    }

    // Arduino function to map speed from 0-100 to minimum and maximum speed of the esc
    _ventSpeed = map(speed, 0, 100, minSpeed, maxSpeed);
    Serial.println(_ventSpeed);
    if (!isRunning)
    {
      Serial.println("Isnt Running");
      arm(LED_PIN, RELAY_2_PIN, D3, minSpeed, maxSpeed, stopESC);
      throttleUP(_ventSpeed, minSpeed);
      isRunning = true;
    }
    // else
    // {
    Serial.println("Is Running");
    throttle(_ventSpeed);
    // myESC.writeMicroseconds(_ventSpeed);
    // }

    if (sendCalState)
    {
      char data[15];
      sprintf(data, "%d-true", speed);
      sendMessage("vt", toStr(0), "*", 1, data); // use in to hexstring
      sendCalState = false;
    }
    return true;
  }
  else
  {
    throttleDOWN(minSpeed, _ventSpeed, RELAY_2_PIN, stopESC);
    isRunning = false;
    return true;
  }
  return false;
#else
  return true;
#endif
}

char strPack[5][256];
char incomingPacket[256];

void processMessage()
{
  char dType[3];
  memcpy(dType, &strPack[1][0], 2);
  dType[2] = '\0';
  char sTid[3];
  memcpy(sTid, &strPack[1][2], 2);
  sTid[2] = '\0';

  int targetId = toInt(sTid);

  int iState;
  bool state = (bool)atoi(strPack[2]);
  bool status = false;
  int spd = 0;
  // targetId =
  // 0 -> Vent

  // Using switch case for situations where more than 1 device is present.
  switch (switchOnStr(dType))
  {
  case Device::VENT:
    // Vent
    // Serial.printf("\nTurning Vent %d to %s\n", targetId, (state) ? "on" : "off");

    spd = atoi(strPack[3]);
    if (spd > 100 && !state)
      spd = 0;
    else if (spd > 100)
      spd = 70; // For safety

    if (!state)
      status = ventOn(state);
    else
      status = ventOn(state, spd);

    if (!status)
    {
      // Serial.println("Sending Error in Vent");
      iState = (int)!state;
      sendMessage("vt", toStr(targetId), "*", iState, "ERR");
    }
    break;
  case Device::TANK:
    // Tank
    // Serial.printf("\nTurning Tank %d to %s\n", targetId, (state) ? "on" : "off");
    iState = atoi(strPack[2]);
    state = (bool)iState;

    status = tankOn(targetId, state);

    if (!status)
    {
      // Serial.println("Sending Error in Tank");
      iState = (int)!state;
      sendMessage("tk", toStr(targetId), "*", iState, "ERR");
    }
    break;
  default:
    break;
  };
}

float prevTemp = 0.0f;
float prevHum = 0.0f;

void getDHTSample(int yieldTime) // yieldTime in milliseconds
{
  if (millis() % yieldTime < 5)
  {
    delay(dht.getMinimumSamplingPeriod());
    float humidity = dht.getHumidity();
    float temperature = dht.getTemperature();
    char tdata[3];
    sprintf(tdata, "%.2f", temperature);
    char hdata[3];
    sprintf(hdata, "%.2f", humidity);
    // delay(2000);
    if (strcmp(tdata, "nan") == 0 || strcmp(hdata, "nan") == 0)
    {
      temperature = prevTemp;
      humidity = prevHum;
    }
    else
    {
      // prevTemp = temperature;
      // prevHum = humidity;
      char data[11];
      sprintf(data, "H:%.2f;T:%.2f", humidity, temperature);
      sendMessage("dh", "00", "*", 1, data);
      // Serial.printf("*^dh00^1^%s\n", data);
    }
  }
}

void setup()
{
  Serial.begin(115200);
  delay(3000);
  // Set D5 as output to send messages to relay
  pinMode(RELAY_1_PIN, OUTPUT);
  pinMode(RELAY_2_PIN, OUTPUT);
  pinMode(LED_PIN, OUTPUT); // Initialize the LED_PIN pin as an output

  digitalWrite(RELAY_1_PIN, LOW);
  digitalWrite(RELAY_2_PIN, LOW);
  digitalWrite(LED_PIN, ledState); // Turn the LED on (Note that LOW is the voltage level
  // delay(1000);

  wifiSetup(static_ip, gateway, subnet_mask, WIFI_AP_STA, network_name, passkey, true, true, true, true, repeater_ip);
  setUdpServer(localUdpPort, RemoteIp);
  setupOTA("ktmcu00", 8266);
  setupComplete = true;
  pinMode(UPPER_TANK_FLOAT, INPUT_PULLUP);
  dht.setup(DHT_PIN, DHTesp::DHT11);
  getDHTSample(2000);
}

int cnt = 0;
int del = 1000;
int probDelay = 20;
int numValToSend = 3;
bool sendValues = true;

void loop()
{
  ArduinoOTA.handle();
  getDHTSample(2000);

  onReceive(incomingPacket);
  for (int i = 0; i < 5; i++)
    memcpy(strPack[i], splitString(i), strlen(splitString(i)));
  processMessage();

  if (((millis() % del) < 5))
  {
    sendValues = true;
  }

  if (sendValues)
  {
    if (((millis() % probDelay) < 5) && cnt < numValToSend)
    {
      probeLevels();
      cnt++;
    }
    else if (cnt == numValToSend)
    {
      cnt = 0;
      sendValues = false;
    }
  }
}

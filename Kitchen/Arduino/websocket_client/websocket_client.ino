#include <DeviceMessage.h>
#include <sync_time.h>
#include </home/kingmoney/.arduino15/packages/esp8266/hardware/esp8266/2.5.2/libraries/ESP8266WiFi/src/ESP8266WiFi.h>
#include <Arduino.h>
#include <TimeLib.h>
#include <WebSockets.h>
#include <WebSocketsClient.h>
#include <Hash.h>
#include <ArduinoJson.h>

using namespace Devices;

bool isTimeSet = false;

char *LivingRoom = "01";
char *KitchenRoom = "02";
Room kitchenRoom(KitchenRoom, LivingRoom);
Device lowerTankLevel("", "string", 1, "Water Level");
Device upperTankLevel("", "string", 2, "Water Level");
Device Booster("", "string", 1, "Booster");
Device Motor("", "string", 1, "Motor");
Device Vent("", "string", 1, "Vent");
Device devices[5] = {lowerTankLevel, upperTankLevel, Booster, Motor, Vent};

bool autoConnect = true;
bool autoReconnect = true;

bool ledState = false;

enum MessageType
{
  TEXT = 1,
  BINARY = 2
};

unsigned long messageTimestamp = 0;
unsigned long currentTime = 0;
unsigned long lastTime = currentTime;
unsigned long interval = 3000;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

const char *piServer = "192.168.1.13"; // Port 5000
WebSocketsClient webSocket;

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 25);
IPAddress subnet_mask(255, 255, 255, 0);

char *getDateTime()
{
  time_t t = now();
  sprintf(strGlob, "%s-%s-%s:%s-%s-%s-000",
          toCharArray(year()), toCharArray(month()), toCharArray(day()), toCharArray(hour()), toCharArray(minute()), toCharArray(second()));

  return strGlob;
}

uint8_t *toByteArray(char *str)
{
  String msg = String(str);
  uint8_t byteArray[msg.length()];
  msg.getBytes(byteArray, msg.length());

  return byteArray;
}

DeviceMessage fromJsonString(char *json)
{
  DeviceMessage receivedMessage;
  Room room;
  Device device;
  StaticJsonDocument<800> jsonDoc;

  DeserializationError err = deserializeJson(jsonDoc, json);
  if (err)
  {
    Serial.print(F("deserializeJson() failed with code "));
    Serial.println(err.c_str());
  }
  JsonObject object = jsonDoc.as<JsonObject>();
  room.setSource(strdup(jsonDoc["room"]["source"]));
  room.setDestination(strdup(jsonDoc["room"]["destination"]));
  if (!jsonDoc["device"].isNull())
  {
    device.setData(strdup(jsonDoc["device"]["data"]));
    device.setDType(strdup(jsonDoc["device"]["dtype"]));
    device.setName(strdup(jsonDoc["device"]["name"]));
    device.setId(jsonDoc["device"]["id"]);
  }
  receivedMessage.setTime(strdup(jsonDoc["time"]));
  receivedMessage.setStatus(strdup(jsonDoc["status"]));
  receivedMessage.setDirection(strdup(jsonDoc["direction"]));
  receivedMessage.setRoom(room);
  receivedMessage.setDevice(device);

  return receivedMessage;
}

// void send(DeviceMessage deviceMessage)
// {

//   webSocket.sendBIN(toByteArray(msg), msg.length());
// }

void setup()
{
  Serial.begin(115200);
  Serial.setDebugOutput(true);
  Serial.println();

  pinMode(LED_BUILTIN, OUTPUT);        // Initialize the LED_BUILTIN pin as an output
  digitalWrite(LED_BUILTIN, ledState); // Turn the LED on (Note that LOW is the voltage level
  delay(1000);

  Serial.println("Configuring connection parameters");

  WiFi.config(static_ip, gateway, subnet_mask);
  WiFi.setAutoConnect(autoConnect);
  WiFi.setAutoReconnect(autoReconnect);
  WiFi.begin(network_name, passkey);

  digitalWrite(LED_BUILTIN, !ledState); // Turn the LED off by making the voltage HIGH
  delay(1000);

  Serial.print("Connecting");
  while (WiFi.status() != WL_CONNECTED)
  {
    delay(500);
    Serial.print(".");
  }
  Serial.println();

  digitalWrite(LED_BUILTIN, !ledState); // Turn the LED on (Note that LOW is the voltage level
  Serial.print("Connected, IP address: ");
  Serial.println(WiFi.localIP());

  // server address, port and URL
  webSocket.begin(piServer, 5000, "/ws");

  // event handler
  webSocket.onEvent(webSocketEvent);

  webSocket.setReconnectInterval(5000);

  webSocket.enableHeartbeat(15000, 3000, 2);

  currentTime = millis();
  lastTime = currentTime;
}
uint64_t sendTime;

void loop()
{
  currentTime = millis();
  webSocket.loop();

  // if (isTimeSet)
  //   Serial.println(String(now()));

  // setNTPTime();

  // if (currentTime == (lastTime + interval))
  // {
  //   messageTimestamp = currentTime;

  //   String msg = "Hello from Arduino : " + String(messageTimestamp);
  //   webSocket.sendTXT(msg);

  //   Serial.println("Sending...");
  //   Serial.println(msg);

  //   lastTime = currentTime;
  // }
}

void onDisconnected()
{
  Serial.printf("[WSc] Disconnected!");
  digitalWrite(LED_BUILTIN, !ledState);
  delay(100);
}

void onConnected(uint8_t *payload)
{
  Serial.printf("[WSc] Connected to url: %u\n", payload);

  // char *json;
  // char *data;
  // DeviceMessage kitchenMessage(
  //     kitchenRoom,
  //     devices[0],
  //     "S2M",
  //     "Connected");
  // kitchenMessage.setTime(getDateTime());
  // sprintf(json, kitchenMessage.toJsonString());

  // webSocket.sendTXT(json);
  // Serial.printf("Sent:\n%s\n", kitchenMessage.toPrettyJsonString());
  digitalWrite(LED_BUILTIN, LOW);
  ledState = false;
}

char *toUTF8(uint8_t *payload)
{
  return (char *)payload;
}

void onMessage(int messageType, uint8_t *payload, size_t length)
{
  char msgData[500];
  char msgJson[500];
  char *json;
  char *data;
  Device *device = devices;
  data = msgData;
  json = msgJson;

  if (messageType == TEXT)
  {
    // Serial.printf("[WSc] get text: %s", payload);
    // sprintf(data, "Message received: %u", payload);
    // kitchenMessage.getDevice().setData(data);
    // sprintf(json, kitchenMessage.toJsonString());
    // webSocket.sendTXT(json);
    // Serial.printf("Sent:\n%s\n", kitchenMessage.toPrettyJsonString());
  }
  else if (messageType == BINARY)
  {
    Serial.printf("[WSc] get bin: %u\n", payload);
    strcpy(msgData, toUTF8(payload));
    Serial.printf("%s\n", data);
    DeviceMessage kitchenMessage = fromJsonString(data);

    if (!isTimeSet)
    {
      processStringTime(kitchenMessage.getTime());
      isTimeSet = true;
    }

    // payload = toByteArray((char *)payload);
    // sprintf(data, "Message received: %u", payload);

    device[0].setData(data);
    DeviceMessage newKitchenMessage(
        kitchenRoom,
        device[0],
        "S2M",
        "Test Message from Arduino");
    newKitchenMessage.setTime(getDateTime());
    newKitchenMessage.setStatus(data);
    Serial.printf("Sending:\n%s\n", kitchenMessage.toPrettyJsonString());
    char* jsonString = new char[512];
    strcpy(jsonString, newKitchenMessage.toJsonString());

    memcpy(json, jsonString, sizeof(jsonString));
    uint8_t msg[500];
    uint8_t *jsonMsg = msg;
    memcpy(jsonMsg, (uint8_t *)json, sizeof((uint8_t *)json));
    
    webSocket.sendBIN(jsonMsg, sizeof(jsonMsg));
  }

  for (uint8_t i = 0; i < length; i++)
  {
    digitalWrite(LED_BUILTIN, !ledState);
    delay(500);
  }
  digitalWrite(LED_BUILTIN, LOW);
  ledState = false;
}

void onError()
{
  Serial.println("Error");

  for (uint8_t i = 0; i < 100; i++)
  {
    digitalWrite(LED_BUILTIN, !ledState);
    delay(1000);
  }

  digitalWrite(LED_BUILTIN, HIGH);
}

void webSocketEvent(WStype_t type, uint8_t *payload, size_t length)
{
  MessageType Text = TEXT;
  MessageType Bin = BINARY;
  switch (type)
  {
  case WStype_DISCONNECTED:
    onDisconnected();
    break;
  case WStype_CONNECTED:
    onConnected(payload);
    break;
  case WStype_TEXT:
    onMessage((int)Text, payload, length);
    break;
  case WStype_BIN:
    onMessage((int)Bin, payload, length);
    break;
    // case WStype_PING:
    //   Serial.printf("[WSc] get ping\n");
    //   for (int i = 0; i < 5; i++)
    //   {
    //     digitalWrite(LED_BUILTIN, !ledState);
    //     delay(500);
    //   }
    //   digitalWrite(LED_BUILTIN, LOW);
    //   ledState = false;
    //   break;
    // case WStype_PONG:
    //   Serial.printf("[WSc] get pong\n");
    //   for (int i = 0; i < 5; i++)
    //   {
    //     digitalWrite(LED_BUILTIN, !ledState);
    //     delay(500);
    //   }
    //   digitalWrite(LED_BUILTIN, LOW);
    //   ledState = false;
    //   break;
  case WStype_ERROR:
    onError();
    break;
  }
}
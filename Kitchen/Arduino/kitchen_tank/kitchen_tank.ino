#include <protocol.h>
#include <ESP8266WiFi.h>

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 130);
IPAddress subnet_mask(255, 255, 255, 0);
IPAddress RemoteIp(192, 168, 1, 18);

char incomingPacket[256] = "";
char replyPacket[256] = "";

const float MAX_HEIGHT = 120.0f;

unsigned int localUdpPort = 4211;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

void setup()
{
    Serial.begin(115200);
    delay(3000);
    pinMode(D3, INPUT_PULLUP);
    // wifiSetup(static_ip, gateway, subnet_mask, WIFI_AP_STA, network_name, passkey, true, true, true);
    // setUdpServer(localUdpPort);
    // setupOTA("ktmcu01", 8266);
}

void probeLevels()
{
    float upper = 0.0f;
    char pData[6];
    int i = digitalRead(D3);
    upper = (i) ? 0.0f : 1.0f;
    sprintf(pData, "lv:%.2f", upper);
    Serial.printf("Depth: %0.2f\n", upper);
    // sendMessage("tk", "01", "*", 1, pData);
}

int cnt = 0;
int del = 10000;
int probDelay = 20;
bool sendValues = true;

void loop()
{
    ArduinoOTA.handle();

    if (((millis() % del) < 5))
    {
        sendValues = true;
    }

    if (sendValues)
    {
        if (((millis() % probDelay) < 5) && cnt < 10)
        {
            probeLevels();
            cnt++;
        }
        else if (cnt == 10)
        {
            cnt = 0;
            sendValues = false;
        }
    }

    onReceive(incomingPacket);
}

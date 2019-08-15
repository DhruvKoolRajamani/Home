#include <ESP8266WiFi.h>
#include <protocol.h>

#define TX 1
#define RX 3
#define GPIO2 2

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 140);
IPAddress subnet_mask(255, 255, 255, 0);
IPAddress RemoteIp(192, 168, 1, 18);

unsigned long lastDebounceTimeRx = 0; // the last time the output pin was toggled
unsigned long lastDebounceTimeTx = 0; // the last time the output pin was toggled
unsigned long debounceDelay = 50;     // the debounce time; increase if the output flickers

unsigned int localUdpPort = 6881;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

char strPack[5][256];
char incomingPacket[256];

int buttonState;

bool ledState = true;

bool rxState = false;
bool txState = false;

bool prevRxState = false;
bool prevTxState = false;

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

    if (strcmp(dType, "rl\0") == 0)
    {
        // Serial.println("Returning Tank");
        iState = atoi(strPack[2]);
        state = (bool)iState;

        if (targetId == 0)
        {
            digitalWrite(GPIO2, !state);
            rxState = state;
        }
        else
        {
            digitalWrite(GPIO2, !state);
            txState = state;
        }
    }
}

void setup()
{
    pinMode(RX, FUNCTION_3);
    pinMode(TX, FUNCTION_3);

    pinMode(RX, INPUT);
    pinMode(TX, INPUT);
    pinMode(GPIO2, OUTPUT);
    digitalWrite(GPIO2, HIGH);

    wifiSetup(static_ip, gateway, subnet_mask, WIFI_AP_STA, network_name, passkey, true, true);
    digitalWrite(GPIO2, HIGH);
    delay(1000);

    setUdpServer(localUdpPort, RemoteIp);
    setupOTA("lrsw00", 8266);
}

void loop()
{
    ArduinoOTA.handle();

    onReceive(incomingPacket);
    for (int i = 0; i < 5; i++)
        memcpy(strPack[i], splitString(i), strlen(splitString(i)));
    processMessage();

    int rxRead = digitalRead(RX);
    int txRead = digitalRead(TX);

    if (rxRead != prevRxState)
    {
        sendMessage("sw", "00", "*", rxRead, "st:toggle");
        prevRxState = rxRead;
    }

    if (txRead != prevTxState)
    {
        sendMessage("sw", "01", "*", txRead, "st:toggle");
        prevTxState = txRead;
    }
}

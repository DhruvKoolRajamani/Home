#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <DHTesp.h>
#include <protocol.h>

// NodeMCU Dev Board
// IP: 192.168.1.26
// Port: 4211

#define LED_PIN D6
#define RELAY_1_PIN D1
#define RELAY_2_PIN D2
#define DHT_PIN 14

#define DHT_ENABLE

#ifdef DHT_ENABLE
DHTesp dht;
#endif

bool ledState = false;
bool autoConnect = true;
bool autoReconnect = true;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 26);
IPAddress subnet_mask(255, 255, 255, 0);

unsigned int localUdpPort = 4211;

const char *piServer = "192.168.1.13"; // Port 5000

const char *sVt = "vt";
const char *sTk = "tk";
enum Device
{
    VENT = 0,
    TANK = 1
};

int switchOnStr(const char *input)
{
    Serial.printf("\nSwitching: %s\n", input);
    if (strcmp(input, "vt\0") == 0)
    {
        Serial.println("Returning Vent");
        return 0;
    }
    else if (strcmp(input, "tk\0") == 0)
    {
        Serial.println("Returning Tank");
        return 1;
    }

    Serial.println("Returning Error");
    return -1;
}

void wifiSetup()
{
    WiFi.config(static_ip, gateway, subnet_mask);
    WiFi.setAutoConnect(autoConnect);
    WiFi.setAutoReconnect(autoReconnect);
    WiFi.begin(network_name, passkey);

    digitalWrite(LED_PIN, !ledState); // Turn the LED off by making the voltage HIGH
    delay(1000);

    // while (WiFi.status() != WL_CONNECTED);

    digitalWrite(LED_PIN, !ledState); // Turn the LED on (Note that LOW is the voltage level
    Serial.print("Connected, IP address: ");
    Serial.println(WiFi.localIP());
}

char strPack[5][256];
char incomingPacket[256];
char replyPacket[256];

bool tankOn(int id, bool state)
{
    Serial.printf("\nTurning tank %d %s\n", id, (state) ? "on" : "off");

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
}

void processMessage()
{
    // for (int i = 0; i < 5; i++)
    //     Serial.println(strPack[i]);
    char dType[3];
    memcpy(dType, &strPack[1][0], 2);
    dType[2] = '\0';
    char sTid[3];
    memcpy(sTid, &strPack[1][2], 2);
    sTid[2] = '\0';
    
    int targetId = toInt(sTid);

    int iState;
    bool state;
    bool status = false;

    // targetId =
    // 0 -> Vent

    // Using switch case for situations where more than 1 device is present.
    switch (switchOnStr(dType))
    {
    case Device::TANK:
        // Vent
        Serial.printf("\n%s\n", (state) ? "on" : "off");
        iState = atoi(strPack[2]);
        state = (bool)iState;

        status = tankOn(targetId, state);

        if (!status)
        {
            Serial.println("Sending Error in Tank");
            iState = (int)!state;
            sendMessage("tk", sTid, "*", iState, "ERR");
        }
        break;
    default:
        Serial.printf("\ntype: %s with id: %d is %s\n", dType, targetId, (state) ? "on" : "off");
        break;
    };
}

void setup()
{
    Serial.begin(115200);
    delay(3000);

    // Set D5 as output to send messages to relay
    pinMode(RELAY_1_PIN, OUTPUT);
    pinMode(RELAY_2_PIN, OUTPUT);
    pinMode(LED_PIN, OUTPUT); // Initialize the LED_PIN pin as an output
    // Turn relay off
    digitalWrite(RELAY_1_PIN, LOW);
    digitalWrite(RELAY_2_PIN, LOW);
    digitalWrite(LED_PIN, ledState); // Turn the LED on (Note that LOW is the voltage level
    delay(1000);

    wifiSetup();

    setUdpServer(localUdpPort);
#ifdef DHT_ENABLE
    Serial.println();
    Serial.println("Status\tHumidity (%)\tTemperature (C)");
    dht.setup(DHT_PIN, DHTesp::DHT11);
#endif
}

#ifdef DHT_ENABLE
void getDHTSample()
{
    delay(dht.getMinimumSamplingPeriod());
    float humidity = dht.getHumidity();
    float temperature = dht.getTemperature();
    delay(2000);
    char data[20];
    sprintf(data, "H:%.2f;T:%.2f\0", humidity, temperature);
    sendMessage("dh", "00", "*", 1, data);
    Serial.printf("*^dh00^1^%s\n", data);
}
#endif

void loop()
{
    char sRec[256];
    int packetSize = parsePacket();

    #ifdef DHT_ENABLE
    getDHTSample();
    #endif

    if (packetSize > 0)
    {
        onReceive(packetSize, incomingPacket);
        Serial.println(incomingPacket);
        for (int i = 0; i < 5; i++)
            memcpy(strPack[i], splitString(i), strlen(splitString(i)));
        processMessage();
    }
}

#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <Servo.h>
#include <math.h>
#include <DHTesp.h>
#include <protocol.h>
#include <VentController.h>

#define DHT_PIN 14

static const uint8_t LED_PIN = D6;
static const uint8_t RELAY_1_PIN = D1;
static const uint8_t RELAY_2_PIN = D2;

bool ledState = false;
bool autoConnect = true;
bool autoReconnect = true;
bool isCalibrated = false;
bool setVent = false;
bool isRunning = false;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

int minSpeed = 1060;
int maxSpeed = 1260;
int stopESC = 500;
int _ventSpeed = 0;

DHTesp dht;

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 25);
IPAddress subnet_mask(255, 255, 255, 0);

unsigned int localUdpPort = 4210;

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
        // digitalWrite(RELAY_2_PIN, state);
        break;
    default:
        break;
    };

    return true;
}

bool ventOn(bool state, int speed = 0)
{
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
            isCalibrated = calibrate(LED_PIN, RELAY_2_PIN, D3, minSpeed, maxSpeed, stopESC);
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
        Serial.printf("\nTurning Vent %d to %s\n", targetId, (state) ? "on" : "off");

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
            Serial.println("Sending Error in Vent");
            iState = (int)!state;
            sendMessage("vt", sTid, "*", iState, "ERR");
        }
        break;
    case Device::TANK:
        // Tank
        Serial.printf("\nTurning Tank %d to %s\n", targetId, (state) ? "on" : "off");
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
        break;
    };
}

float prevTemp = 0.0f;
float prevHum = 0.0f;

void getDHTSample()
{
    delay(dht.getMinimumSamplingPeriod());
    float humidity = dht.getHumidity();
    float temperature = dht.getTemperature();
    char tdata[10];
    sprintf(tdata, "%.2f\0", temperature);
    char hdata[10];
    sprintf(hdata, "%.2f\0", humidity);
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
        char data[30];
        sprintf(data, "H:%.2f;T:%.2f\0", humidity, temperature);
        sendMessage("dh", "00", "*", 1, data);
        // Serial.printf("*^dh00^1^%s\n", data);
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

    wifiSetup();

    setUdpServer(localUdpPort);
    dht.setup(DHT_PIN, DHTesp::DHT11);
    getDHTSample();
}

void loop()
{
    char sRec[256];
    int packetSize = parsePacket();
    getDHTSample();

    if (packetSize > 0)
    {
        onReceive(packetSize, incomingPacket);
        for (int i = 0; i < 5; i++)
            memcpy(strPack[i], splitString(i), strlen(splitString(i)));
        processMessage();
    }

    // If statement to check if vent has already throttled up, no need to throttle up/down once fan starts
    // if (isRunning)
    // {
    //     myESC.writeMicroseconds(_ventSpeed);
    // }
}

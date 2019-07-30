#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <Servo.h>
#include <protocol.h>

#define LED_PIN LED_BUILTIN

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
Servo myESC;

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
    if (strcmp(input, sVt))
    {
        return 0;
    }
    else if (strcmp(input, sTk))
    {
        return 1;
    }
    return -1;
}

// Function to calibrate ESC to set the min and max speed of the ESC
// Doesn't need to be performed everytime the ESC is controlled.
void calibrate()
{
    int mil = 0;
    int sec = 0;
    ledState = HIGH;
    digitalWrite(LED_PIN, ledState);
    Serial.println("\nCONNECT POWER NOW");
    // ESC Connected to pin D3 of the Wemos
    myESC.attach(D3, minSpeed, maxSpeed);
    // Stop the ESC first by sending a 500 ms
    myESC.writeMicroseconds(stopESC);
    // NO Relay connected to pin D5 of the Wemos for the ESC Power supply
    // You will hear a 1-2-3 beep from the ESC
    delay(1000);
    digitalWrite(D5, LOW);
    // Stop the ESC first by sending a 500 ms
    myESC.writeMicroseconds(stopESC);
    // Write the required maximum speed first and wait for 2 seconds.
    // You will hear a low beep to validate the setting.
    myESC.writeMicroseconds(maxSpeed);
    delay(2000);
    // Write the required minimum speed first and wait for 2 seconds.
    myESC.writeMicroseconds(minSpeed);
    delay(2000);
    // Stop the ESC first by sending a 500 ms
    myESC.writeMicroseconds(stopESC);
    delay(5000);
    digitalWrite(LED_PIN, !ledState);
    isCalibrated = true;
    isRunning = false;
}

// Function to arm the ESC with the required values.
void arm(int val)
{
    pinMode(LED_PIN, OUTPUT);
    digitalWrite(LED_PIN, HIGH);
    // NO Relay connected to pin D5 of the Wemos for the ESC Power supply
    // You will hear a 1-2-3 beep from the ESC
    myESC.attach(D3, minSpeed, maxSpeed);
    // Ideally send 500ms pwm to stop esc after arming.
    myESC.writeMicroseconds(val);
    delay(1000);
    digitalWrite(D5, LOW);
    delay(5000);
}

// Function to slowly increment the speed of the ESC
void throttleUP(int speed)
{
    for (int i = minSpeed; i <= speed; i += 10)
    {
        myESC.writeMicroseconds(i);
        Serial.println(i);
        delay(100);
    }
}

// Function to slowly decrement the speed of the ESC
void throttleDOWN()
{
    setVent = false;
    isRunning = false;
    for (int i = _ventSpeed; i >= minSpeed - 10; i -= 10)
    {
        myESC.writeMicroseconds(i);
        Serial.println(i);
        delay(100);
    }

    delay(2000);
    // Stopping the ESC by sending a 500ms signal
    myESC.writeMicroseconds(stopESC);
    // Turning off the Relay to stop supply to the fan
    digitalWrite(D5, HIGH);
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
            calibrate();
        }

        // Arduino function to map speed from 0-100 to minimum and maximum speed of the esc
        _ventSpeed = map(speed, 0, 100, minSpeed, maxSpeed);
        Serial.println(_ventSpeed);
        if (!isRunning)
        {
            arm(stopESC);
            throttleUP(_ventSpeed);
            setVent = true;
        }
        else
        {
            isRunning = true;
            myESC.writeMicroseconds(_ventSpeed);
            setVent = true;
        }

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
        throttleDOWN();
        return true;
    }

    return false;
}

void processMessage()
{
    char dType[2];
    memcpy(dType, &strPack[1][0], 2);
    char sTid[2];
    memcpy(sTid, &strPack[1][1], 2);
    int targetId = toInt(sTid);
    char *pReply = replyPacket;

    int iState;
    bool state;
    bool status = false;

    // targetId =
    // 0 -> Vent

    // Using switch case for situations where more than 1 device is present.
    switch (switchOnStr(dType))
    {
    case Device::VENT:
        // Vent
        iState = atoi(strPack[2]);
        state = (bool)iState;

        if (!state)
            status = ventOn(state);
        else
            status = ventOn(state, atoi(strPack[3]));

        if (!status)
        {
            Serial.println("Sending Error in Vent");
            iState = (int)!state;
            sendMessage("vt", sTid, "*", iState, "ERR");
        }
        break;
    default:
        break;
    };
}

void setup()
{
    Serial.begin(115200);

    // Set D5 as output to send messages to relay
    pinMode(D5, OUTPUT);
    pinMode(LED_PIN, OUTPUT); // Initialize the LED_PIN pin as an output
    // Turn relay off
    digitalWrite(D5, HIGH);
    digitalWrite(LED_PIN, ledState); // Turn the LED on (Note that LOW is the voltage level
    // delay(1000);

    wifiSetup();

    setUdpServer(localUdpPort);
}

void loop()
{
    char sRec[256];
    int packetSize = parsePacket();

    if (packetSize > 0)
    {
        onReceive(packetSize, incomingPacket);
        for (int i = 0; i < 5; i++)
            memcpy(strPack[i], splitString(i), strlen(splitString(i)));
        processMessage();
    }

    // If statement to check if vent has already throttled up, no need to throttle up/down once fan starts
    if (setVent)
    {
        isRunning = true;
        myESC.writeMicroseconds(_ventSpeed);
    }
}

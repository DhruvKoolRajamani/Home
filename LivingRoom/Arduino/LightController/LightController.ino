#include <protocol.h>
#include <ESP8266WiFi.h>

#define TX 1
#define RX 3
#define GPIO2 2

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 101);
IPAddress subnet_mask(255, 255, 255, 0);

unsigned int localUdpPort = 6881;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

unsigned long lastDebounceTime = 0; // the last time the output pin was toggled
unsigned long debounceDelay = 50;   // the debounce time; increase if the output flickers
int lastButtonState = LOW;
int buttonState;

bool ledState = true;

void setup()
{
    // pinMode(TX, FUNCTION_3);
    pinMode(RX, FUNCTION_3);
    pinMode(RX, INPUT_PULLUP);
    wifiSetup(static_ip, gateway, subnet_mask, WIFI_AP_STA, network_name, passkey, true, true);
    delay(1000);
    pinMode(GPIO2, OUTPUT);
    digitalWrite(GPIO2, LOW);
    setUdpServer(localUdpPort);
    setupOTA("lvsw00", 8266);
}

char incomingPacket[256];

void loop()
{
    ArduinoOTA.handle();
    onReceive(incomingPacket);
    int i = digitalRead(RX);
    if (i == HIGH)
    {
        sendMessage("sw", "00", "*", 1, "on");
        digitalWrite(GPIO2, HIGH);
    }
    else
    {
        sendMessage("sw", "00", "*", 0, "on");
        digitalWrite(GPIO2, LOW);
    }
}

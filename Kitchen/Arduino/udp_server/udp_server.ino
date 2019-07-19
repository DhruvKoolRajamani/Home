#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include <sync_time.h>

bool isTimeSet = false;
bool ledState = false;
bool autoConnect = true;
bool autoReconnect = true;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 25);
IPAddress subnet_mask(255, 255, 255, 0);

WiFiUDP udpServer;
unsigned int localUdpPort = 4210;
char incomingPacket[256];
char replyPacket[] = "Hi there! Got the message :-)";

const char *piServer = "192.168.1.13"; // Port 5000

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

    udpServer.begin(localUdpPort);
    Serial.printf("Now listening at IP %s, UDP port %d\n", WiFi.localIP().toString().c_str(), localUdpPort);
}

void loop()
{
    int packetSize = udpServer.parsePacket();
    if (packetSize)
    {
        // receive incoming UDP packets
        Serial.printf("Received %d bytes from %s, port %d\n", packetSize, udpServer.remoteIP().toString().c_str(), udpServer.remotePort());
        int len = udpServer.read(incomingPacket, 255);
        if (len > 0)
        {
            incomingPacket[len] = 0;
        }
        Serial.printf("UDP packet contents: %s\n", incomingPacket);

        // send back a reply, to the IP address and port we got the packet from
        udpServer.beginPacket(udpServer.remoteIP(), udpServer.remotePort());
        udpServer.write(replyPacket);
        udpServer.endPacket();
    }
}
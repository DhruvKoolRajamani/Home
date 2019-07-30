#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
// #include <sync_time.h>
#include <DeviceMessage.h>

using namespace Devices;

bool ledState = false;
bool autoConnect = true;
bool autoReconnect = true;

bool firstMessage = true;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 25);
IPAddress subnet_mask(255, 255, 255, 0);

// typedef void (*UdpEvent)(WStype_t type, uint8_t * payload, size_t length);

WiFiUDP udpServer;
unsigned int localUdpPort = 4210;

const char *piServer = "192.168.1.13"; // Port 5000

// void setDateTime(DeviceMessage msg)
// {
//     if (firstMessage)
//     {
//         char _tm[255];
//         char *tm = _tm;
//         tm = msg.getTime();
//         processStringTime(tm);
//         firstMessage = false;
//     }

//     char _yr[5], _mth[5], _d[5], _h[5], _m[5], _s[5], _ms[5];

//     // Serial.printf("\nTime is: %d-%d-%dT%d-%d-%d\n", year(), month(), day(), hour(), minute(), second());
//     sprintf(_yr, "%d", year());
//     sprintf(_mth, "%d", month());
//     sprintf(_d, "%d", day());
//     sprintf(_h, "%d", hour());
//     sprintf(_m, "%d", minute());
//     sprintf(_s, "%d", second() + 1);
//     sprintf(_ms, "%s", "000");

//     msg.setDTime(_yr, _mth, _d, _h, _m, _s, _ms);
// }

void wifiSetup()
{
    WiFi.config(static_ip, gateway, subnet_mask);
    WiFi.setAutoConnect(autoConnect);
    WiFi.setAutoReconnect(autoReconnect);
    WiFi.begin(network_name, passkey);

    digitalWrite(LED_BUILTIN, !ledState); // Turn the LED off by making the voltage HIGH
    delay(1000);

    // while (WiFi.status() != WL_CONNECTED);

    digitalWrite(LED_BUILTIN, !ledState); // Turn the LED on (Note that LOW is the voltage level
    Serial.print("Connected, IP address: ");
    Serial.println(WiFi.localIP());
}

void setup()
{
    Serial.begin(115200);
    // Serial.setDebugOutput(true);
    Serial.println();

    pinMode(LED_BUILTIN, OUTPUT);        // Initialize the LED_BUILTIN pin as an output
    digitalWrite(LED_BUILTIN, ledState); // Turn the LED on (Note that LOW is the voltage level
    delay(1000);

    wifiSetup();

    udpServer.begin(localUdpPort);
}

void loop()
{
    int packetSize = udpServer.parsePacket();
    if (packetSize > 0)
        while(onReceive(packetSize) != 0) ;
}

void sendMessage(char *replyPacket)
{
    // send back a reply, to the IP address and port we got the packet from
    udpServer.beginPacket(udpServer.remoteIP(), udpServer.remotePort());
    udpServer.write(replyPacket);
    udpServer.endPacket();
}

int onReceive(int size)
{
    char incomingPacket[512];
    char replyPacket[512];
    int len = udpServer.read(incomingPacket, size);
    
    if (len == 0)
    {
        incomingPacket[len] = 0;
        return 0;
    }

    snprintf(incomingPacket, len+2, "%s\0", incomingPacket);
    // Serial.printf("\nReceived %d bytes\n", len);
    // Serial.printf("Message is: \n%s\n", incomingPacket);

    DeviceMessage masterMessage;

    int status = 0;
    status = masterMessage.fromXmlMessage(incomingPacket, len);
    // setDateTime(masterMessage);

    DeviceMessage replyMessage = masterMessage;
    Device *_dev = replyMessage.getDevice();

    _dev->setSData("On");

    Serial.printf("Message is: \n%s\n", incomingPacket);

    // char t[255];
    // sprintf(t, "%s", masterMessage.ToString());
    // Serial.printf("%s", t);

    // sprintf(replyPacket, );

    // sendMessage(replyPacket);

    // return 0;
    return 0;
}
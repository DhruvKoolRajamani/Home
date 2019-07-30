#include <TimeLib.h>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
// #include <Arduino.h>

IPAddress timeServer(139, 59, 50, 38);
const float timeZone = 5.5;

WiFiUDP Udp;
unsigned int localPort = 8888;

char strGlob[250];

/*-------- NTP code ----------*/

const int NTP_PACKET_SIZE = 48;     // NTP time is in the first 48 bytes of message
byte packetBuffer[NTP_PACKET_SIZE]; //buffer to hold incoming & outgoing packets

// send an NTP request to the time server at the given address
void sendNTPpacket(IPAddress &address)
{
    // set all bytes in the buffer to 0
    memset(packetBuffer, 0, NTP_PACKET_SIZE);
    // Initialize values needed to form NTP request
    // (see URL above for details on the packets)
    packetBuffer[0] = 0b11100011; // LI, Version, Mode
    packetBuffer[1] = 0;          // Stratum, or type of clock
    packetBuffer[2] = 6;          // Polling Interval
    packetBuffer[3] = 0xEC;       // Peer Clock Precision
    // 8 bytes of zero for Root Delay & Root Dispersion
    packetBuffer[12] = 49;
    packetBuffer[13] = 0x4E;
    packetBuffer[14] = 49;
    packetBuffer[15] = 52;
    // all NTP fields have been given values, now
    // you can send a packet requesting a timestamp:
    Udp.beginPacket(address, 123); //NTP requests are to port 123
    Udp.write(packetBuffer, NTP_PACKET_SIZE);
    Udp.endPacket();
}

time_t getNtpTime()
{
    while (Udp.parsePacket() > 0)
        ; // discard any previously received packets
    Serial.println("Transmit NTP Request");
    sendNTPpacket(timeServer);
    uint32_t beginWait = millis();
    while (millis() - beginWait < 1500)
    {
        int size = Udp.parsePacket();
        if (size >= NTP_PACKET_SIZE)
        {
            Serial.println("Receive NTP Response");
            Udp.read(packetBuffer, NTP_PACKET_SIZE); // read packet into the buffer
            unsigned long secsSince1900;
            // convert four bytes starting at location 40 to a long integer
            secsSince1900 = (unsigned long)packetBuffer[40] << 24;
            secsSince1900 |= (unsigned long)packetBuffer[41] << 16;
            secsSince1900 |= (unsigned long)packetBuffer[42] << 8;
            secsSince1900 |= (unsigned long)packetBuffer[43];
            return secsSince1900 - 2208988800UL + timeZone * SECS_PER_HOUR;
        }
    }
    Serial.println("No NTP Response :-(");
    return 0; // return 0 if unable to get the time
}

void processStringTime(char *strTime)
{
    char strStatic1[250];
    char strStatic2[250];
    int tm;
    int parsedTime[7];
    strcpy(strStatic1, strTime);
    strcpy(strStatic2, strTime);
    String sTemp1 = strStatic1;
    String sTemp2 = strStatic2;

    sTemp1 = sTemp1.substring(0, sTemp1.indexOf(":"));
    sTemp2 = sTemp2.substring(sTemp2.indexOf(":") + 1, sTemp2.length());

    for (int i = 0; i < 2; i++)
    {
        parsedTime[i] = sTemp1.substring(0, sTemp1.indexOf("-")).toInt();
        sTemp1 = sTemp1.substring((sTemp1.indexOf("-") + 1), sTemp1.indexOf(":"));
        // Serial.printf("%s:\t%d\n", sTemp, parsedTime[i]);
    }
    parsedTime[2] = sTemp1.substring(0, sTemp1.indexOf(":")).toInt();
    sTemp1 = sTemp1.substring((sTemp1.indexOf(":") + 1), sTemp1.length());
    // Serial.printf("%s:\t%d\n", sTemp, parsedTime[2]);

    for (int i = 3; i < 7; i++)
    {
        parsedTime[i] = sTemp2.substring(0, sTemp2.indexOf("-")).toInt();
        sTemp2 = sTemp2.substring((sTemp2.indexOf("-") + 1), sTemp2.length());
        // Serial.printf("%s:\t%d\n", sTemp, parsedTime[i]);
    }

    setTime(parsedTime[3], parsedTime[4], parsedTime[5], parsedTime[2], parsedTime[1], parsedTime[0]);
}

void setNTPTime()
{
    Serial.println("Starting UDP");
    Udp.begin(localPort);
    Serial.print("Local port: ");
    Serial.println(localPort);
    Serial.println("waiting for sync");
    setSyncProvider(getNtpTime);
}
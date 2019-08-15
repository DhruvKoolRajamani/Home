#ifndef PROTOCOL_H
#define PROTOCOL_H

#include <math.h>
#include <iomanip>
#include <string.h>
#include <ESP8266WiFi.h>
#include <ArduinoOTA.h>
#include <WiFiUdp.h>

#ifdef ENABLE_SOFTAP
#include <lwip/lwip_napt.h>
#include <lwip/app/dhcpserver.h>
#endif

WiFiUDP udpServer;
bool enableSerialOutput = false;

/**
 * Output WiFi setup
 * @param hostName
 * @param port
 */
void setupOTA(const char *hostName, int port = 8266)
{
    // Port defaults to 8266
    ArduinoOTA.setPort(port);

    // Hostname defaults to esp8266-[ChipID]
    ArduinoOTA.setHostname(hostName);

    ArduinoOTA.onStart([]() {
        String type;
        if (ArduinoOTA.getCommand() == U_FLASH)
            type = "sketch";
        else // U_SPIFFS
            type = "filesystem";

        // NOTE: if updating SPIFFS this would be the place to unmount SPIFFS using SPIFFS.end()
        if (enableSerialOutput)
            Serial.println("Start updating " + type);
    });
    ArduinoOTA.onEnd([]() {
        if (enableSerialOutput)
            Serial.println("\nEnd");
    });
    ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
        if (enableSerialOutput)
            Serial.printf("Progress: %u%%\r", (progress / (total / 100)));
    });
    ArduinoOTA.onError([](ota_error_t error) {
        if (enableSerialOutput)
            Serial.printf("Error[%u]: ", error);
        if (error == OTA_AUTH_ERROR)
        {
            if (enableSerialOutput)
                Serial.println("Auth Failed");
        }
        else if (error == OTA_BEGIN_ERROR)
        {
            if (enableSerialOutput)
                Serial.println("Begin Failed");
        }
        else if (error == OTA_CONNECT_ERROR)
        {
            if (enableSerialOutput)
                Serial.println("Connect Failed");
        }
        else if (error == OTA_RECEIVE_ERROR)
        {
            if (enableSerialOutput)
                Serial.println("Receive Failed");
        }
        else if (error == OTA_END_ERROR)
        {
            if (enableSerialOutput)
                Serial.println("End Failed");
        }
    });
    ArduinoOTA.begin();
}

/**
 * Output WiFi setup
 * @param staticIp
 * @param gateway
 * @param subnetMask
 * @param WiFiMode
 * @param networkName
 * @param password
 * @param autoConnect
 * @param autoReconnect
 * @param serialOutput
 * @param softApState
 * @param repeaterIp
 */
void wifiSetup(IPAddress staticIp, IPAddress gw, IPAddress sm, WiFiMode wifiMode, const char *networkName, const char *pw, bool aConnect = true, bool aReconnect = true, bool serialOutput = false, bool softApState = false, IPAddress repeaterIp = IPAddress(0, 0, 0, 0))
{
    if (serialOutput)
        enableSerialOutput = serialOutput;

    int ptCnt = 0;

    WiFi.mode(wifiMode);
    WiFi.config(staticIp, gw, sm);
    WiFi.setAutoConnect(aConnect);
    WiFi.setAutoReconnect(aReconnect);

    WiFi.begin(networkName, pw);

    if (enableSerialOutput)
    {
        Serial.print("Connecting...");
    }

    while (WiFi.status() != WL_CONNECTED)
    {
        if (enableSerialOutput)
        {
            if (ptCnt < 20)
            {
                Serial.print('.');
                ptCnt++;
            }
            else
            {
                Serial.println();
                ptCnt = 0;
            }
        }
        delay(10);
    }

    if (enableSerialOutput)
    {
        Serial.println();
        Serial.print("Connected, IP address: ");
        Serial.println(WiFi.localIP());
    }

#ifdef ENABLE_SOFTAP
    if (softApState)
    {
        if (enableSerialOutput)
        {
            Serial.print("Setting soft-AP configuration ... ");
            Serial.println(WiFi.softAPConfig(repeaterIp, gw, sm) ? "Ready" : "Failed!");

            Serial.print("Setting soft-AP ... ");
            Serial.println(WiFi.softAP(networkName, pw) ? "Ready" : "Failed!");

            Serial.print("Soft-AP IP address = ");
            Serial.println(WiFi.softAPIP());
        }
        else
        {
            WiFi.softAPConfig(repeaterIp, gw, sm);
            WiFi.softAP(networkName, pw);
        }

        ip_napt_init(IP_NAPT_MAX, IP_PORTMAP_MAX);
        ip_napt_enable_no(1, 1);
    }
#endif
}

bool delay_nb(int ms)
{
    if ((millis() % ms) < 5)
        return true;
    else
        return false;
}

struct ServerAddresses
{
    struct Addresses
    {
        IPAddress ServerIp;
        int Port;
    } addresses[10];
    int Count;
};

ServerAddresses servers;

bool checkServerIpList(IPAddress ip, int port)
{
    for (int i = 0; i < servers.Count; i++)
    {
        if (servers.addresses[i].ServerIp == ip && servers.addresses[i].Port == port)
            return true;
    }

    return false;
}

void addServerIp(IPAddress ip, int port)
{
    servers.addresses[servers.Count].ServerIp = ip;
    servers.addresses[servers.Count].Port = port;
    servers.Count++;
}

void setUdpServer(int port, IPAddress remote = IPAddress(0, 0, 0, 0))
{
    udpServer.begin(port);
    servers.Count = 0;
    if (remote != IPAddress(0, 0, 0, 0))
    {
        servers.addresses[servers.Count].ServerIp = remote;
        servers.addresses[servers.Count].Port = port;
        servers.Count++;
    }
}

int toInt(const char *input)
{
    return (int)strtol(input, NULL, 16);
}

char *toStr(int input)
{
    char output[3] = "00";
    sprintf(output, "%X", input);
    if ((int)strtol(output, NULL, 16) < 10)
        sprintf(output, "0%s", output);
    return output;
}

char msgPack[5][256] = {""};
// Since protocol has only 4 '.' separators, splitting the string can be hardcoded
bool searchForLength(int len, char *msg)
{
    char delim[] = "^";
    int i = 0;

    char *pMsg = msg;
    char *pMsgPack;

    // Split string based on '^' serparators
    pMsg = strtok(msg, delim);
    pMsgPack = msgPack[i];
    i++;
    while (pMsg != NULL)
    {
        if (i > 4)
            break;
        pMsg = strtok(NULL, delim);
        pMsgPack = msgPack[i];
        strcpy(pMsgPack, pMsg);
        i++;
    }

    // Check if the last character is end of message else throw exception and send NACK to daemon
    char endChar = msgPack[4][strlen(msgPack[4]) - 1];
    if (endChar != '|')
        return false;

    pMsg = strtok(msgPack[4], "|");
    // Compare message length with length received in message
    int recLength = atoi(pMsg);

    if (recLength == len)
    {
        pMsgPack = msgPack[4];
        strcpy(pMsgPack, pMsg);
        return true;
    }

    return false;
}

void udpSend(const char *sendMsg)
{
    for (int i = 0; i < servers.Count; i++)
    {
        udpServer.beginPacket(servers.addresses[i].ServerIp, servers.addresses[i].Port);
        udpServer.write(sendMsg);
        udpServer.endPacket();
    }
}

// Protocol format: <A/N/*>^<targetId>^<state>^<command>^<msg len>|
//      <A/N/*>     A-> Ack; N -> Nack; * -> Indicates message from mcu to udp Daemon,
//                  can be used to indicate error while changing motor speed/etc.
//      <state>     Indicates the desired state
//      <command>   Can be either a speed command, or a calibration command
//                  Use '-' to separate data commands. eg. *.*.*.50-calibrated.*
//      <len>       Length of message in bytes to provide idea about message loss
//          |       Indicates end of message.
void sendMessage(const char *dType = "*", const char *targetId = "*", const char *ack = "*", int state = -1, const char *data = "*")
{
    // send back a reply, to the IP address and port we got the packet from
    char replyPacket[255] = "";
    char *pReply = replyPacket;
    char st[1];
    if (state == -1)
    {
        sprintf(st, "*");
    }
    sprintf(pReply, "%s^%s%s^%d^%s^000|\0", ack, dType, targetId, state, data);
    int len = strlen(pReply);
    sprintf(pReply, "%s^%s%s^%d^%s^%d|\0", ack, dType, targetId, state, data, len);

    int count = 0;
    for (int i = 0; pReply[i] != '\0'; i++)
    {
        count++;
    }
    snprintf(pReply, count + 1, pReply);
    if (enableSerialOutput)
        Serial.println(pReply);
    udpSend(pReply);
}

void sendMessage(char *msg, char ack = '*')
{
    // send back a reply, to the IP address and port we got the packet from
    msg[0] = ack;

    udpSend(msg);
}

int onReceive(char *incomingPacket)
{
    int size = udpServer.parsePacket();
    if (size == 0)
    {
        incomingPacket[size] = 0;
        return -1;
    }

    if (!checkServerIpList(udpServer.remoteIP(), udpServer.remotePort()))
        addServerIp(udpServer.remoteIP(), udpServer.remotePort());

    int len = udpServer.read(incomingPacket, size);

    snprintf(incomingPacket, len + 1, "%s", incomingPacket);

    if (enableSerialOutput)
        Serial.printf("\n%s\n", incomingPacket);

    if (searchForLength(len, incomingPacket))
    {
        sendMessage(incomingPacket, 'A');
        return 0;
    }
    else
    {
        sendMessage(incomingPacket, 'N');
        return 1;
    }

    return 0;
}

char *splitString(int i)
{
    if (i >= 5)
        return nullptr;

    char *tmp = msgPack[i];
    return tmp;
}

void setMsgPackNull(const char* inputStr = "")
{
    for (int i = 0; i < 5; i++)
    {
        char* tmp = msgPack[i];
        sprintf(tmp, inputStr);
    }
}

#endif // PROTOCOL_H
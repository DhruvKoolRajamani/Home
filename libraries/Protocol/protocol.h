#ifndef PROTOCOL_H
#define PROTOCOL_H

#include <math.h>
#include <iomanip>
#include <string.h>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>

WiFiUDP udpServer;

void setUdpServer(int port)
{
    udpServer.begin(port);
}

int toInt(const char* input)
{
    return (int)strtol(input, NULL, 16);
}

char* toStr(int input)
{
    char output[3] = "00";
    sprintf(output, "%X", input);
    if ((int)strtol(output, NULL, 16) < 10)
        sprintf(output, "0%s", output);
    return output;
}

char msgPack[5][256];
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

// Protocol format: <A/N/*>^<targetId>^<state>^<command>^<msg len>|
//      <A/N/*>     A-> Ack; N -> Nack; * -> Indicates message from mcu to udp Daemon,
//                  can be used to indicate error while changing motor speed/etc.
//      <state>     Indicates the desired state
//      <command>   Can be either a speed command, or a calibration command
//                  Use '-' to separate data commands. eg. *.*.*.50-calibrated.*
//      <len>       Length of message in bytes to provide idea about message loss
//          |       Indicates end of message.
void sendMessage(const char* dType="*", const char* targetId="*", const char *ack="*", int state = -1, const char *data="*")
{
    // send back a reply, to the IP address and port we got the packet from
    char replyPacket[255];
    char *pReply = replyPacket;
    char st[1];
    if (state != -1)
    {
        sprintf(st, "%d", state);
    }
    else
    {
        sprintf(st, "*");
    }
    sprintf(pReply, "%s^%s%s^%s^%s^000|\0", ack, dType, targetId, st, data);
    int len = strlen(pReply);
    sprintf(pReply, "%s^%s%s^%s^%s^%d|\0", ack, dType, targetId, st, data, len);

    int count = 0;
    for (int i = 0; pReply[i] != '\0'; i++)
    {
        count++;
    }
    snprintf(pReply, count + 1, pReply);

    udpServer.beginPacket(udpServer.remoteIP(), udpServer.remotePort());
    udpServer.write(pReply);
    udpServer.endPacket();
}

int parsePacket()
{
    return udpServer.parsePacket();
}

void sendMessage(char *msg, char ack = '*')
{
    // send back a reply, to the IP address and port we got the packet from
    msg[0] = ack;

    udpServer.beginPacket(udpServer.remoteIP(), udpServer.remotePort());
    udpServer.write(msg);
    udpServer.endPacket();
}

int onReceive(int size, char* incomingPacket)
{
    int len = udpServer.read(incomingPacket, size);

    if (len == 0)
    {
        incomingPacket[len] = 0;
        return -1;
    }

    snprintf(incomingPacket, len + 1, "%s", incomingPacket);

    // Serial.printf("\n%s\n", incomingPacket);

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

char* splitString(int i)
{
    if (i >= 5)
        return nullptr;
    
    char* tmp = msgPack[i];
    return tmp;
}

#endif // PROTOCOL_H
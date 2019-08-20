#ifndef PROTOCOL_H
#define PROTOCOL_H

#include <math.h>
#include <iomanip>
#include <string.h>
#include <ESP8266WiFi.h>
#include <ArduinoOTA.h>
#include <WiFiUdp.h>
#include <RemoteDebug.h>
#include <RemoteDebugCfg.h>
#include <RemoteDebugWS.h>
#include <telnet.h>
#ifdef ENABLE_SOFTAP
#include <lwip/lwip_napt.h>
#include <lwip/app/dhcpserver.h>
#endif

WiFiUDP udpServer;
extern RemoteDebug Debug;
bool enableSerialOutput = true;

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
		Serial.println(networkName);
        Debug.println(networkName);

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

char *toStr( int input)
{
    char output[3] = "";
     sprintf(output, "%d", input);
    return output;
}

char msgPack[6][128] = {""};



//Message format : <Orig>^<TargetType>^<ID>^<Command>^<Data>^<Totalbytes not including the totalbytes>
//      <Orig>      dev / srv msg originator.
//      <TargetType>target type -> dev / srv
//      <ID>        Specfic ID of a target
//      <Command>   Action that needs to be performed could be "switch" / "info" / "query" /"register" etc
//      <Data>      Any data associated with the command : "switch" could have "0" / "1" etc.
//                  Use '-' to separate data commands. eg. *.*.*.50-calibrated.*
//      <len>       Length of message in bytes to provide idea about message loss - This might change to a single byte 
//                  since we want to contain the message size to 256 bytes

bool ParseMsg(int len, char *msg)
{
    
    Serial.println(msg);
    Debug.println(msg);
    //TODO: havent implement message size check
    bool bPass = false ;
    int i = 0;
    char * pMsg = strtok(msg, "^");

    strcpy( msgPack[i], pMsg);
		i++;
    Serial.println(msgPack[i]);
    while (pMsg != NULL)
    {
        pMsg = strtok(NULL, "^");
        if(pMsg == NULL)
            break;
	    strcpy(msgPack[i], pMsg);

        i++;
    }
    bPass = true;
    return bPass;
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

//Message format : <Orig>^<TargetType>^<ID>^<Command>^<Data>^<Totalbytes not including the totalbytes>
//      <Orig>      dev / srv msg originator.
//      <TargetType>target type -> dev / srv
//      <ID>        Specfic ID of a target
//      <Command>   Action that needs to be performed could be "switch" / "info" / "query" /"register" etc
//      <Data>      Any data associated with the command : "switch" could have "0" / "1" etc.
//                  Use '-' to separate data commands. eg. *.*.*.50-calibrated.*
//      <len>       Length of message in bytes to provide idea about message loss - This might change to a single byte 
//                  since we want to contain the message size to 256 bytes
void sendMessage(const char *Orig = "*",  const char *TargetType = "*" , const char *targetId = "*", const char *cmd = "status"  ,  const char *data = "*")
{
    

    char pReply[255] = "";

    sprintf(pReply, "%s^%s^%s^%s^%s^000|\0", Orig, TargetType, targetId, cmd, data);
    sprintf(pReply,"%s%d",pReply , strlen(pReply));
    Serial.println("sendMessage");
    Serial.println(pReply);
    udpSend(pReply);
}

void AckMessage(char *msg,bool bACk)
{
    // send back a reply, to the IP address and port we got the packet from
    if(bACk)
    {
        /*
        msgPack[0] = 
        msg[0] = ack;
        */
    }
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

    //Add the sender to our list - Although we should just send to one server 
    // we could look at changing the home server with a broadcast command
    if (!checkServerIpList(udpServer.remoteIP(), udpServer.remotePort()))
        addServerIp(udpServer.remoteIP(), udpServer.remotePort());

    int len = udpServer.read(incomingPacket, size);
    
    Debug.println("Incoming Pkt");
    Debug.println(incomingPacket);
    return size;
}

char *splitString(int i)
{
    if (i > 5)
        return nullptr;

    char *tmp = msgPack[i];
    return tmp;
}

void setMsgPackNull(const char* inputStr = "")
{
    for (int i = 0; i < 6; i++)
        memset( msgPack[i],0,sizeof(msgPack[i]));
}

#endif // PROTOCOL_H
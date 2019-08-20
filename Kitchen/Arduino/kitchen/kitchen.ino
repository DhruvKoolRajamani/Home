#include <RemoteDebug.h>
#include <RemoteDebugCfg.h>
#include <RemoteDebugWS.h>
#include <telnet.h>

#include "D:/Git/Home Automation/libraries/Protocol/protocol.h"
#include <string.h>
#include <math.h>


#define LED_PIN 12         // D6
#define RELAY_1_PIN 5      // D1
#define RELAY_2_PIN 4      // D2
#define DHT_PIN 14         // D5
#define UP_MID 0           // D3           // blue       // power brown
#define UPPER_TANK_FLOAT 2 // D4           // orange     // power brown
#define UP_BOT 13          // D7           // green      // power brown
#define LOW_TOP 10         // D8 / SD3 10
#define LOW_MID 3          // RX
#define LOW_BOT 1          // TX

// <Orig>^<TargetType>^<ID>^<Command>^<Data>^<Totalbytes not including the totalbytes>
#define MSG_Orig 0 
#define MSG_Tgt_Type 1 
#define MSG_ID 2 
#define MSG_Command 3 
#define MSG_Data 4 

bool ledState = true;
bool autoConnect = true;
bool autoReconnect = true;
bool isCalibrated = false;

bool isRunning = false;
bool bFloatSwitch = true;

const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

//limit processing once a command is received
int iDontProcess = 0;

//DHTesp dht;

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 129);
IPAddress repeater_ip(192, 168, 1, 254);
IPAddress subnet_mask(255, 255, 255, 0);
IPAddress RemoteIp(192, 168, 1, 18);

#define HOST_NAME "192.168.1.129"
unsigned int localUdpPort = 11466; // @ her majesty's command
// tank float switch
int cnt = 0;
int del = 30000;
int probDelay = 20;
int numValToSend = 3;
bool sendValues = true;


enum Device
{
  TANK = 1
};
RemoteDebug Debug;
int switchOnStr(const char *input)
{
  // Serial.println("\nSwitching: %s\n", input);
  if (strcmp(input, "vt") == 0)
  {
    // Serial.println("Returning Vent");
    return 0;
  }
  else if (strcmp(input, "tk") == 0)
  {
    // Serial.println("Returning Tank");
    return 1;
  }
  else if (strcmp(input, "lv") == 0)
  {
    // Serial.println("Returning Levels");
    return 2;
  }

  // Serial.println("Returning Error");
  return -1;
}

// Probelevels is supposed to determin water level in the tank
// Currently limited to switch off tank when the floatlevel switch turns on.
void probeLevels()
{
  int iFloatSwitch = digitalRead(UPPER_TANK_FLOAT);
 if ((millis() % del) == 0 )
 {
       Debug.printf("Float switch : " );
       Debug.println(iFloatSwitch);
 }
  else
    return;
    
    bool status  = true ; 
    if(iFloatSwitch == 0)
    {
      status  = tankOn(1 , false); //TODO:hardcoded to upper level tank
      //  <Orig>^<TargetType>^<ID>^<Command>^<Data>^<Totalbytes not including the totalbytes>
      sendMessage("tk", "srv","1","status","0");
    }
    

}

// Tank GPIO driver
bool tankOn(int id, bool state)
{
  int iState = abs((int)state);
  Serial.printf("Tank %d State %d \r\n",id,iState);
  switch (id)
  {
  case 1:
    // Relay pin 1
    digitalWrite(LED_PIN,iState);
    digitalWrite(RELAY_1_PIN, iState);
    return true;
  case 2:
    // Relay pin 2
    digitalWrite(RELAY_2_PIN, state);
    return true;
  default:
    return false;
  };  
}

extern char msgPack[6][128] ;
char incomingPacket[256] = "";


// tank should automatically turn off if probelevels works...
void processMessage()
{
  //srv^tk^01^switch^0^18
  Debug.println("processMessage");
  Serial.println("processMessage");
  int targetId = atoi(msgPack[MSG_ID]);
  bool state = (bool)atoi(msgPack[MSG_Data]);
  
  // Tank
  if ( strcmp(msgPack[MSG_Tgt_Type] ,"tk" ) == 0 )
      {
        if(strcmp( msgPack[MSG_Command],"switch" ) == 0)
        {
          if (!tankOn(targetId, state))
              sendMessage("tk",msgPack[MSG_Orig],msgPack[MSG_ID], "status", msgPack[MSG_Data]);
          else
             sendMessage("tk",msgPack[MSG_Orig],msgPack[MSG_ID], "status", "Error");
        }
      }
}

float prevTemp = 0.0f;
float prevHum = 0.0f;

void getDHTSample(int yieldTime) // yieldTime in milliseconds
{
/*  if (millis() % yieldTime < 5)
  {
    delay(dht.getMinimumSamplingPeriod());
    float humidity = dht.getHumidity();
    float temperature = dht.getTemperature();
    char tdata[3];
    sprintf(tdata, "%.2f", temperature);
    char hdata[3];
    sprintf(hdata, "%.2f", humidity);
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
      char data[11];
      sprintf(data, "H:%.2f;T:%.2f", humidity, temperature);
      sendMessage("dh", "00", "*", 1, data);
      // Serial.printf("*^dh00^1^%s\n", data);
    }
  }
  */
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
  digitalWrite(LED_PIN, LOW); // Turn the LED on (Note that LOW is the voltage level
  // delay(1000);

  wifiSetup(static_ip, gateway, subnet_mask, WIFI_AP_STA, network_name, passkey, true, true, true, true, repeater_ip);
  setUdpServer(localUdpPort, RemoteIp);
  setupOTA("ktmcu00", 8266);
 // setupComplete = true;
  pinMode(UPPER_TANK_FLOAT, INPUT_PULLUP);
  //dht.setup(DHT_PIN, DHTesp::DHT11);
  //getDHTSample(2000);

  Debug.begin(HOST_NAME); // Initialize the WiFi server
  Debug.setResetCmdEnabled(true); // Enable the reset command
  //Debug.showProfiler(true); // Profiler (Good to measure times, to optimize codes)
  
}



void nullAllStrings()
{
  
  strcpy(incomingPacket,"" );
  setMsgPackNull();
  
}

void loop()
{
  ArduinoOTA.handle();
 // getDHTSample(2000);
//check the tank float switch every 30 secs
  probeLevels();
 iDontProcess = onReceive(incomingPacket);
 
//no messages might as well return
 if(iDontProcess <= 0)
    return;
  else
    {
      // break the packet and fill the string arrays
      ParseMsg(iDontProcess , incomingPacket);
      Serial.printf("Data in %d \r\n", iDontProcess);
    }

  processMessage();
  debugHandle();
  return;

  
}

#include <RemoteDebug.h>
#include <RemoteDebugCfg.h>
#include <RemoteDebugWS.h>
#include <telnet.h>


#include "D:/Git/Home Automation/libraries/Protocol/protocol.h"

#define TX 1
#define RX 3
#define GPIO2 2

//limit processing once a command is received
int iDontProcess = 0;

IPAddress gateway(192, 168, 1, 1);
IPAddress static_ip(192, 168, 1, 131);
IPAddress subnet_mask(255, 255, 255, 0);
IPAddress RemoteIp(192, 168, 1, 18);

unsigned long lastDebounceTimeRx = 0; // the last time the output pin was toggled
unsigned long lastDebounceTimeTx = 0; // the last time the output pin was toggled
unsigned long debounceDelay = 50;     // the debounce time; increase if the output flickers

//Remote debugging
#define HOST_NAME "192.168.1.131"
RemoteDebug Debug;

unsigned int localUdpPort = 11466; // @ her majesty's command
const char *network_name = "Gunny"; // Remove before making repo public
const char *passkey = "kippukool";  // Remove before making repo public

int buttonState;
bool ledState = true;
int del = 2000;
bool bTXRead = false;
long lTXTimer = 0;
long lRXTimer = 0;

int iPrevTX_01 = 1;
int iPrevRX_02 = 1;
bool prevTxState = false;
bool bRead = false;
char pReply[255] ="";
void processMessage()
{
    // Keep reading state
    // if there is a transition then send a message
    // dont change the state within 2 sec in case we put a toggle switch
  
  int i =  digitalRead(TX);
  if (iPrevTX_01 != i)
  {
      //srv^tk^01^switch^0^18
    iPrevTX_01 = i;
    sprintf(pReply,"%d",i );
    Debug.printf(pReply);
    //sendMessage(const char *Orig = "*",  const char *TargetType = "*" , const char *targetId = "*", const char *cmd = "status"  ,  const char *data = "*")
    sendMessage("btn","tk","01","switch",pReply);
    Debug.printf("\n\r ");
    
  }
   i =  digitalRead(RX);
     if (iPrevRX_02 != i)
  {
    iPrevRX_02 = i;
    sprintf(pReply,"btn^tk^02^switch^%d",i );
    Debug.printf(pReply);
    sendMessage("btn","tk","02","switch",pReply);
    Debug.printf("\n\r ");
  }

  return ;
  /*
    if (iPrevTX_01 != digitalRead(TX))
        {
            if(!bRead)
            {
                lTXTimer =  millis() ;
                bTXRead = true;
            }
            else
            {
                bTXRead = false;
                //Send a message if the toggle happens within 2 sec
                if((millis() - lTXTimer) < 2000)
                     sendMessage("*", "01",bTXread "*", rxRead, "st:toggle");        
                 
            }
            
        }
    */           

}

void setup()
{
    Debug.begin(HOST_NAME);
    Debug.setSerialEnabled(true);
    pinMode(RX, FUNCTION_3);
    pinMode(TX, FUNCTION_3);

    pinMode(RX, INPUT);
    pinMode(TX, INPUT);
    pinMode(GPIO2, OUTPUT);
    digitalWrite(GPIO2, HIGH);

    wifiSetup(static_ip, gateway, subnet_mask, WIFI_AP_STA, network_name, passkey, true, true);
    digitalWrite(GPIO2, LOW);
    delay(1000);

    setUdpServer(localUdpPort, RemoteIp);
    setupOTA("tksw00", 8266);
    lTXTimer = millis(); // init the Timer
    Debug.setResetCmdEnabled(true); 
    
}

void loop()
{
    
    ArduinoOTA.handle();

    processMessage();

    Debug.handle();
}

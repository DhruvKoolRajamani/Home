#ifndef VENTCONTROLLER_H
#define VENTCONTROLLER_H

#include <Servo.h>
#include <ESP8266WiFi.h>
#include <protocol.h>
#include <delayfuncs.h>

// static const uint8_t D0   = 16;
// static const uint8_t D1   = 5;
// static const uint8_t D2   = 4;
// static const uint8_t D3   = 0;
// static const uint8_t D4   = 2;
// static const uint8_t D5   = 14;
// static const uint8_t D6   = 12;
// static const uint8_t D7   = 13;
// static const uint8_t D8   = 15;
// static const uint8_t D9   = 3;
// static const uint8_t D10  = 1;

Servo esc;

bool LED_STATE = false;

// Function to calibrate ESC to set the min and max speed of the ESC
// Doesn't need to be performed everytime the ESC is controlled.
bool calibrate(uint8_t ledPin, uint8_t relayPin, uint8_t pin, int min, int max, int stpESC)
{
    int mil = 0;
    int sec = 0;
    LED_STATE = true;
    digitalWrite(ledPin, LED_STATE);
    Serial.println("\nCONNECT POWER NOW");
    // ESC Connected to pin D3 of the Wemos
    esc.attach(pin, min, max);
    // Stop the ESC first by sending a 500 ms
    esc.writeMicroseconds(stpESC);
    // NO Relay connected to pin D5 of the Wemos for the ESC Power supply
    // You will hear a 1-2-3 beep from the ESC
    if (delay_ms(1000, 0) == 1)
    {
        digitalWrite(relayPin, HIGH);
        // Stop the ESC first by sending a 500 ms
        esc.writeMicroseconds(stpESC);
        // Write the required maximum speed first and wait for 2 seconds.
        // You will hear a low beep to validate the setting.
        esc.writeMicroseconds(max);
        delay_ms(2000, 0);
        // Write the required minimum speed first and wait for 2 seconds.
        esc.writeMicroseconds(min);
        if (delay_ms(2000, 0) == 1)
        {
            // Stop the ESC first by sending a 500 ms
            esc.writeMicroseconds(stpESC);
            if (delay_ms(5000, 0) == 1)
            {
                digitalWrite(ledPin, !LED_STATE);
                return true;
            }
            return false;
        }
        return false;
    }
    return false;
}

// Function to arm the ESC with the required values.
void arm(uint8_t ledPin, uint8_t relayPin, uint8_t pin, int min, int max, int stpESC)
{
    digitalWrite(ledPin, HIGH);
    // NO Relay connected to pin D5 of the Wemos for the ESC Power supply
    // You will hear a 1-2-3 beep from the ESC
    esc.attach(pin, min, max);
    // Ideally send 500ms pwm to stop esc after arming.
    esc.writeMicroseconds(stpESC);
    if (delay_ms(1000, 0) == 1)
    {
        digitalWrite(relayPin, HIGH);
        if (delay_ms(5000, 0) == 1)
            digitalWrite(ledPin, LOW);
    }
}

// Function to slowly increment the speed of the ESC
void throttleUP(int speed, int min)
{
    for (int i = min; i <= speed; i += 10)
    {
        esc.writeMicroseconds(i);
        Serial.println(i);
        while (delay_ms(100, 1) != 1)
            ;
    }
    esc.writeMicroseconds(speed);
}

void throttle(int speed)
{
    esc.writeMicroseconds(speed);
}

// Function to slowly decrement the speed of the ESC
void throttleDOWN(int min, int speed, uint8_t relayPin, int stpESC)
{
    for (int i = speed; i >= min - 10; i -= 10)
    {
        esc.writeMicroseconds(i);
        Serial.println(i);
        while (delay_ms(100, 1) != 1)
            ;
    }

    if (delay_ms(2000, 1) == 1)
    {
        // Stopping the ESC by sending a 500ms signal
        esc.writeMicroseconds(stpESC);
        // Turning off the Relay to stop supply to the fan
        digitalWrite(relayPin, LOW);
    }
}

#endif // VENTCONTROLLER_H
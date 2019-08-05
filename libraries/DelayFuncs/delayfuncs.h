#ifndef DELAYFUNCS_H
#define DELAYFUNCS_H
#include "Arduino.h"

unsigned long globDelay = 0;
uint8_t globPriority = 255;
bool isGlobDelayUsed = false;
unsigned long prevMicros = 0;
unsigned long loopTime = 0;

uint8_t delay_us(int us, uint8_t priority = 255)
{
    if (priority < globPriority)
    {
        globPriority = priority;
        isGlobDelayUsed = false;
    }

    if (!isGlobDelayUsed)
    {
        if (globDelay == 0)
        {
            globDelay = prevMicros + us;
            isGlobDelayUsed = true;
            return 0;
        }

        if (globDelay - micros() <= 100)
        {
            globDelay = 0;
            return 1;
        }
    }
    else
    {
        return -1;
    }
}

uint8_t delay_ms(int ms, uint8_t priority = 255)
{
    return delay_us(ms * 1000, priority);
}

#endif // DELAYFUNCS_H
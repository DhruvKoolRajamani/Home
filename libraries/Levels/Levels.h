#ifndef LEVELS_H
#define LEVELS_H

#include <Arduino.h>

class Levels
{
    char Name[11];
    int _max = 0;
    uint8_t levelPins[3];

    Levels(const char *name, uint8_t tankLow, uint8_t tankMid, uint8_t tankHigh)
    {
        strcpy(Name, name);

        levelPins[0] = tankLow;
        levelPins[1] = tankMid;
        levelPins[2] = tankHigh;

        pinMode(levelPins[0], INPUT);
        pinMode(levelPins[1], INPUT);
        pinMode(levelPins[2], INPUT);
    }

    uint8_t *returnLevelPins()
    {
        return levelPins;
    }

    void lowLevelCheckCallback()
    {
        Serial.println("Received a change");
        _max = 1;
    }

    void midLevelCheckCallback()
    {
        Serial.println("Received a change");
        _max = 2;
    }

    void highLevelCheckCallback()
    {
        Serial.println("Received a change");
        _max = 3;
    }
};

#endif // LEVELS_H
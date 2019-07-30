#ifndef DEVICEMESSAGE_H
#define DEVICEMESSAGE_H

// #include <Arduino.h>
// #include <TimeLib.h>
#include <stdio.h>
#include <string.h>
#include <tinyxml2.h>
#include <algorithm>

using namespace tinyxml2;

namespace Devices
{
class Device
{
public:
    Device(char *name, int id, bool state, char *data = nullptr)
    {
        if (data != nullptr)
        {
            _sdata = _sdt;
            strcpy(_sdata, data);
        }
        _state = state;
        _id = id;
        strcpy(_name, name);
        dtype = 0;
    }

    Device(const char *name, int id, bool state, char *data = nullptr)
    {
        if (data != nullptr)
        {
            _sdata = _sdt;
            strcpy(_sdata, data);
        }
        _state = state;
        _id = id;
        sprintf(_name, "%s", name);
        dtype = 0;
    }

    Device(char *name, int id, bool state, int *data = nullptr)
    {
        if (data != nullptr)
        {
            _idata = &_idt;
            _idata = data;
        }
        _state = state;
        _id = id;
        strcpy(_name, name);
        dtype = 1;
    }

    Device(char *name, int id, bool state, float *data = nullptr)
    {
        if (data != nullptr)
        {
            _fdata = &_fdt;
            _fdata = data;
        }
        _state = state;
        _id = id;
        strcpy(_name, name);
        dtype = 2;
    }

    Device() {}

private:
    char _sdt[255];
    int _idt;
    float _fdt;
    char *_sdata;
    int *_idata;
    float *_fdata;
    bool _state;
    char _nm[255];
    char *_name = _nm;
    int _id;
    int dtype;

public:
    char *getSData()
    {
        // if (_sdata != nullptr)
        return _sdata;
        // return "NULL";
    }
    void setSData(char *data)
    {
        dtype = 0;
        _sdata = _sdt;
        _sdata = data;
    }
    int *getIData()
    {
        // if (_idata != nullptr)
        return _idata;
        // *_idata = -1;
        // return _idata;
    }
    void setIData(int *data)
    {
        dtype = 1;
        _idata = &_idt;
        *_idata = *data;
    }
    float *getFData()
    {
        // if (_fdata != nullptr)
        return _fdata;
        // *_fdata = -1.0;
        // return _fdata;
    }
    void setFData(float *data)
    {
        dtype = 2;
        _fdata = &_fdt;
        *_fdata = *data;
    }
    char *getData()
    {
        if (dtype == 0)
            return _sdata;

        if (dtype == 1)
        {
            char *s;
            sprintf(s, "%d", *_idata);
            return s;
        }

        if (dtype == 2)
        {
            char *s;
            sprintf(s, "%f", *_fdata);
            return s;
        }

        return "NULL";
    }

    bool getState() { return _state; }
    void setState(bool state) { _state = state; }

    char *getName() { return _name; }
    void setName(char *name) { strcpy(_name, name); }

    int getId() { return _id; }
    void setId(int id) { _id = id; }

    int getDType() { return dtype; }
};

class DeviceMessage : public Device
{
public:
    DeviceMessage(char *room, Device device) : Device(device)
    {
        _room = room;
        _device = device;
        // _time = setTime();
    }

    DeviceMessage(const char *room, Device device) : Device(device)
    {
        sprintf(_room, "%s", room);
        _device = device;
        // _time = setTime();
    }

    DeviceMessage() {}

private:
    char _rm[255];
    char *_room = _rm;
    Device _device;
    // Device *pDevice;
    char _yr[5], _mth[5], _d[5], _h[5], _m[5], _s[5], _ms[5];
    char *_year = _yr;
    char *_month = _mth;
    char *_day = _d;
    char *_hour = _h;
    char *_minute = _m;
    char *_second = _s;
    char *_mSecond = _ms;

public:
    char *getRoom() { return _room; }
    void setRoom(char *room) { _room = room; }

    Device *getDevice() { return &_device; }
    void setDevice(Device device) { _device = device; }
    void setDevice(char *data, bool state, char *name, int id)
    {
        _device.setSData(data);
        _device.setState(state);
        _device.setName(name);
        _device.setId(id);
    }
    void setDevice(int *data, bool state, char *name, int id)
    {
        _device.setIData(data);
        _device.setState(state);
        _device.setName(name);
        _device.setId(id);
    }
    void setDevice(float *data, bool state, char *name, int id)
    {
        _device.setFData(data);
        _device.setState(state);
        _device.setName(name);
        _device.setId(id);
    }

    char *getTime()
    {
        char strTime[255];
        sprintf(strTime, "%s-%s-%s:%s-%s-%s-000", _year, _month, _day, _hour, _minute, _second);
        return strTime;
    }
    void setDTime(char *year, char *month, char *day, char *hour, char *minute, char *second, char *msecond)
    {
        strcpy(_year, year);
        strcpy(_month, month);
        strcpy(_day, day);
        strcpy(_hour, hour);
        strcpy(_minute, minute);
        strcpy(_second, second);
        strcpy(_mSecond, msecond);
    }

    char *ToString()
    {
        char s[255];
        sprintf(s, "room : %s\n"
                   "device :{\n"
                   "\tdata : %s,\n"
                   "\tstate : %s,\n"
                   "\tname : %s,\n"
                   "\tid : %d\n"
                   "},\n"
                   "stamp :{\n"
                   "\tyear : %s,\n"
                   "\tmonth : %s,\n"
                   "\tday : %s,\n"
                   "\thour : %s,\n"
                   "\tminute : %s,\n"
                   "\tsecond : %s,\n"
                   "\tmsecond : %s\n"
                   "}",
                _room, _device.getData(), _device.getState() ? "true" : "false", _device.getName(), _device.getId(), _year, _month, _day, _hour, _minute, _second, _mSecond);

        return s;
    }

    char *toXmlMessage()
    {
        char s[255];
        char *pXml = s;
        sprintf(pXml, "<%s>"
                     "<Device>"
                     "<State>%s</State>"
                     "<Name>%s</Name>"
                     "<Id>%d</Id>"
                     "<Data>%s</Data>"
                     "</Device>"
                     "<Stamp Year=\"%s\" Month=\"%s\" Day=\"%s\" Hour=\"%s\" Minute=\"%s\" Second=\"%s\" Millisecond=\"%s\"/>"
                     "</%s>",
                _room, _device.getState() ? "true" : "false", _device.getName(), _device.getId(), _device.getData(),
                _year, _month, _day, _hour, _minute, _second, _mSecond, _room);

        return pXml;
    }

    bool isFloat(float flt)
    {
        if (abs(flt - int(flt)) > 0)
            return true;
        return false;
    }

    int fromXmlMessage(char *strMsg, size_t size)
    {
        XMLDocument xmlDoc;
        XMLError eResult = xmlDoc.Parse(strMsg, size);

        if (eResult != 0)
            return XML_ERROR_FILE_READ_ERROR;

        XMLNode *pRoot = xmlDoc.FirstChildElement();
        if (pRoot == nullptr)
            return XML_ERROR_FILE_READ_ERROR;
        sprintf(_room, "%s", pRoot->Value());

        const char *year, *month, *day, *hour, *minute, *second, *msecond;
        XMLElement *pElement = pRoot->FirstChildElement("Stamp");
        if (pElement == nullptr)
            return XML_ERROR_PARSING_ELEMENT;

        year = pElement->Attribute("Year");
        if (year == nullptr)
            return XML_ERROR_PARSING_ATTRIBUTE;
        sprintf(_year, "%s", year);

        month = pElement->Attribute("Month");
        if (month == nullptr)
            return XML_ERROR_PARSING_ATTRIBUTE;
        sprintf(_month, "%s", month);

        day = pElement->Attribute("Day");
        if (day == nullptr)
            return XML_ERROR_PARSING_ATTRIBUTE;
        sprintf(_day, "%s", day);

        hour = pElement->Attribute("Hour");
        if (hour == nullptr)
            return XML_ERROR_PARSING_ATTRIBUTE;
        sprintf(_hour, "%s", hour);

        minute = pElement->Attribute("Minute");
        if (minute == nullptr)
            return XML_ERROR_PARSING_ATTRIBUTE;
        sprintf(_minute, "%s", minute);

        second = pElement->Attribute("Second");
        if (second == nullptr)
            return XML_ERROR_PARSING_ATTRIBUTE;
        sprintf(_second, "%s", second);

        msecond = pElement->Attribute("Millisecond");
        if (msecond == nullptr)
            return XML_ERROR_PARSING_ATTRIBUTE;
        sprintf(_mSecond, "%s", msecond);

        pElement = pRoot->FirstChildElement("Device");
        if (pElement == nullptr)
            return XML_ERROR_PARSING_ELEMENT;

        XMLElement *pSubElement = pElement->FirstChildElement("Name");
        if (pSubElement->GetText() == nullptr)
            return XML_ERROR_PARSING_ATTRIBUTE;
        char tmp[255];
        char *pTmp = tmp;
        sprintf(pTmp, "%s", pSubElement->GetText());
        _device.setName(pTmp);

        int id;
        bool state;
        float val;
        pSubElement = pElement->FirstChildElement("Id");
        if (eResult == 0)
        {
            eResult = pSubElement->QueryIntText(&id);
            _device.setId(id);
        }

        pSubElement = pElement->FirstChildElement("State");
        if (eResult == 0)
        {
            eResult = pSubElement->QueryBoolText(&state);
            _device.setState(state);
        }

        pSubElement = pElement->FirstChildElement("Data");
        if (eResult == 0)
            if (isdigit(pSubElement->GetText()[0]))
            {
                eResult = pSubElement->QueryFloatText(&val);
                if (isFloat(val))
                    _device.setFData(&val);
                else
                {
                    int ival = val;
                    _device.setIData(&ival);
                }
            }
            else
            {
                char stmp[255];
                char *pstmp = stmp;
                sprintf(pstmp, "%s", pSubElement->GetText());
                _device.setSData(pstmp);
            }

        return eResult;
    }
};
} // namespace Devices

#endif
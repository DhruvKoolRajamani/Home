#ifndef DEVICEMESSAGE_H
#define DEVICEMESSAGE_H

// #include <Arduino.h>
// #include <TimeLib.h>
#include <stdio.h>
#include <string.h>

namespace Devices
{
char str[250];

char *toCharArray(int num)
{
    if (num < 10)
        sprintf(str, "%02d", num);
    else
        sprintf(str, "%d", num);

    return str;
}

char *toCharArray(int num, bool ms)
{
    if (num < 10)
        sprintf(str, "%00d", num);
    else if (num < 100)
        sprintf(str, "%0d", num);
    else
        sprintf(str, "%d", num);

    return str;
}

class Room
{
public:
    Room(char *source, char *destination)
    {
        _source = source;
        _destination = destination;
    }

    Room(Room *room)
    {
        _source = room->getSource();
        _destination = room->getDestination();
    }

    Room() {}

private:
    char *_source;
    char *_destination;

public:
    char *getSource() { return _source; }
    void setSource(char *source) { _source = source; }

    char *getDestination() { return _destination; }
    void setDestination(char *destination) { _destination = destination; }
};

class Device
{
public:
    Device(char *data, char *dType, int id, char *name)
    {
        _data = data;
        _dType = dType;
        _id = id;
        _name = name;
    }

    Device(Device *device)
    {
        _data = device->getData();
        _dType = device->getDType();
        _name = device->getName();
        _id = device->getId();
    }

    Device() {}

private:
    char *_data;
    char *_dType;
    char *_name;
    int _id;

public:
    char *getData() { return _data; }
    void setData(char *data) { _data = data; }

    char *getDType() { return _dType; }
    void setDType(char *dType) { _dType = dType; }

    char *getName() { return _name; }
    void setName(char *name) { _name = name; }

    int getId() { return _id; }
    void setId(int id) { _id = id; }
};

class DeviceMessage : public Room, public Device
{
public:
    DeviceMessage(Room room, Device device, char *direction, char *status) : Room(room), Device(device)
    {
        _room = room;
        _device = device;
        _direction = direction;
        _status = status;
        // _time = setTime();
    }

    DeviceMessage() {}

private:
    Room _room;
    Device _device;
    char *_direction;
    char *_status;
    char *_time = "2019-07-13:11-01-55-130";

public:
    Room getRoom() { return _room; }
    void setRoom(Room room) { _room = room; }
    void setRoom(char *source, char *destination)
    {
        _room.setSource(source);
        _room.setDestination(destination);
    }

    Device getDevice() { return _device; }
    void setDevice(Device device) { _device = device; }
    void setDevice(char *data, char *dType, char *name, int id)
    {
        _device.setData(data);
        _device.setDType(dType);
        _device.setName(name);
        _device.setId(id);
    }

    char *getDirection() { return _direction; }
    void setDirection(char *direction) { _direction = direction; }

    char *getStatus() { return _status; }
    void setStatus(char *status) { _status = status; }

    char *getTime() { return _time; }
    void setTime(char *time) { _time = time; }

    char *toJsonString()
    {
        sprintf(str,
                "{"
                "\"room\":{"
                "\"source\":\"%s\","
                "\"destination\":\"%s\""
                "},"
                "\"device\":{"
                "\"data\":\"%s\","
                "\"dtype\":\"%s\""
                "\"name\":\"%s\""
                "\"id\":%d"
                "},"
                "\"time\":\"%s\","
                "\"direction\":\"%s\","
                "\"status\":\"%s\""
                "}",
                _room.getSource(), _room.getDestination(), _device.getData(), _device.getDType(), _device.getName(), _device.getId(), _time, _direction, _status);

        return str;
    }

    char *toPrettyJsonString()
    {
        sprintf(str,
                "{\n"
                "\t\"room\":{\n"
                "\t\t\"source\":\"%s\",\n"
                "\t\t\"destination\":\"%s\"\n"
                "\t},\n"
                "\t\"device\":{\n"
                "\t\t\"data\":\"%s\",\n"
                "\t\t\"dtype\":\"%s\"\n"
                "\t\t\"name\":\"%s\"\n"
                "\t\t\"id\":%d\n"
                "\t},\n"
                "\t\"time\":\"%s\",\n"
                "\t\"direction\":\"%s\",\n"
                "\t\"status\":\"%s\"\n"
                "}",
                _room.getSource(), _room.getDestination(), _device.getData(), _device.getDType(), _device.getName(), _device.getId(), _time, _direction, _status);

        return str;
    }
};
} // namespace Devices

#endif
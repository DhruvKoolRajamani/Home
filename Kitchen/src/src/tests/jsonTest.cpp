#include <iostream>
#include <DeviceMessage.h>

using namespace std;
using namespace Devices;

int main(int argc, char **argv)
{
    // int x;
    
    char* LivingRoom = "01";
    char* KitchenRoom = "02";
    Room kitchenRoom(KitchenRoom, LivingRoom);
    Device lowerTankLevel("", "string", 1, "Water Level");
    Device upperTankLevel("", "string", 2, "Water Level");
    Device Booster("", "string", 1, "Booster");
    Device Motor("", "string", 1, "Motor");
    Device Vent("", "string", 1, "Vent");
    Device devices[5] = {lowerTankLevel, upperTankLevel, Booster, Motor, Vent};

    DeviceMessage kitchenMessage(
        kitchenRoom,
        devices[0],
        "S2M",
        "Test Message from Arduino");

    cout << kitchenMessage.toPrettyJsonString() << "\n";
    // cin >> x;

    return 0;
}
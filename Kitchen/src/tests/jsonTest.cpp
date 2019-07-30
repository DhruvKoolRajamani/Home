#include <iostream>
#include <math.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
// #include <DeviceMessage.h>

using namespace std;
// using namespace Devices;

int toInt(const char* sinput)
{
    // static const char* const lut = "ABCDEF";
    // size_t len = strlen(sinput);
    // cout << std::endl;
    // char input[len];
    // size_t j = 0;
    // for (size_t i = len, j=0; i = 0; --i, ++j)
    // {
    //     input[j] = sinput[i];
    //     cout << sinput[i] << std::endl;
    //     cout << input[j];
    // }
    // int result = 0;

    // for (size_t i = 0; i < len; ++i)
    // {
    //     char cc[] = "0";
    //     cout << input[i] << '\n';
    //     sprintf(cc, "%s", input[i]);
    //     const unsigned char c = cc[0];
    //     if (isdigit(c))
    //     {
    //         result += atoi(cc)*pow(16, i);
    //         printf("\nDigit %lu in %s is %d\n", i, cc, result);
    //     }
    //     else
    //     {
    //         int k = 10;
    //         for (int j = 0; j < 6; ++j, ++k)
    //         {
    //             const unsigned char t = lut[j];
    //             if (toupper(c) == t)
    //                 break;
    //         }
    //         result += k*pow(16, i);
    //         printf("\nDigit %lu in %s is %d\n", i, cc, result);
    //     }
    // }

    // cout << sinput << std::endl;

    return (int)strtol(sinput, NULL, 16);
}

int main(int argc, char **argv)
{
    // int x;
    
    // char* LivingRoom = "01";
    // char* KitchenRoom = "02";
    // Room kitchenRoom(KitchenRoom, LivingRoom);
    // Device lowerTankLevel("", "string", 1, "Water Level");
    // Device upperTankLevel("", "string", 2, "Water Level");
    // Device Booster("", "string", 1, "Booster");
    // Device Motor("", "string", 1, "Motor");
    // Device Vent("", "string", 1, "Vent");
    // Device devices[5] = {lowerTankLevel, upperTankLevel, Booster, Motor, Vent};

    // DeviceMessage kitchenMessage(
    //     kitchenRoom,
    //     devices[0],
    //     "S2M",
    //     "Test Message from Arduino");

    // cout << kitchenMessage.toPrettyJsonString() << "\n";
    // cin >> x;

    printf("1: ");
    printf("%d\n", toInt("01"));
    printf("86: ");
    printf("%d\n", toInt("56"));
    printf("245: ");
    printf("%d\n", toInt("F5"));

    return 0;
}
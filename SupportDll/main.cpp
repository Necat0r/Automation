#include "Bluetooth.h"

#include <Windows.h>
#include <stdio.h>

void main(int argc, const char** argv)
{
	// Bluetooth stuff
#if 0
	if (bt_init())
	{
		const char* deviceName = "";
		if (bt_getDeviceCount() > 1)
			deviceName = bt_getDeviceName(1);

		if (deviceName)
		{
			printf("Using device %s", deviceName);

			for (;;)
			{
				bt_updateDevice(deviceName);

				if (bt_inRange(deviceName))
				{
					printf("In RANGE!\n");
				}
				else
				{
					printf("Out of range\n");
				}

				Sleep(1000);
			}
		}

		bt_shutdown();
	}
#endif

	return;
}
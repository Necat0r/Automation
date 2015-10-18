#include "Module.h"

extern "C" {
AUTOMATION_API bool bt_init();
AUTOMATION_API void bt_refreshDevices();
AUTOMATION_API unsigned int bt_getDeviceCount();
AUTOMATION_API const char* bt_getDeviceName(unsigned int index);
AUTOMATION_API void bt_updateDevice(const char* device);
AUTOMATION_API void bt_shutdown();
AUTOMATION_API bool bt_inRange(const char* deviceName);
AUTOMATION_API int bt_getLastError(const char* deviceName);
}
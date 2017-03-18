#include "Bluetooth.h"

#include <stdio.h>
#include <assert.h>

#define _WINSOCK_DEPRECATED_NO_WARNINGS
#include <winsock2.h>
#include <ws2bth.h>

#pragma comment(lib, "ws2_32.lib")

class Device
{
public:
	Device()
	: m_isInRange(false)
	{}

	Device(const char* name, const BTH_ADDR& address)
	: m_isInRange(false)
	, m_address(address)
	, m_lastError(ERROR_SUCCESS)
	{
		strcpy_s(m_name, name);
	}

	const char* getName() const { return m_name; }
	bool isInRange() const { return m_isInRange; }
	int getLastError() const { return m_lastError; }

	bool refreshData();
private:
	enum {
		MaxName = 32,
	};

	bool m_isInRange;
	char m_name[MaxName];
	BTH_ADDR m_address;
	int m_lastError;
};

bool Device::refreshData()
{
	m_isInRange = false;
	m_lastError = ERROR_SUCCESS;

	SOCKET socketPtr = socket(AF_BTH, SOCK_STREAM, BTHPROTO_RFCOMM);
	if (socketPtr == INVALID_SOCKET)
	{
		m_lastError = WSAGetLastError();
		printf("socket failed %d\n", m_lastError);
		return false;
	}

	SOCKADDR_BTH socketAddress;
	socketAddress.addressFamily = AF_BTH;
	socketAddress.btAddr = m_address;
	socketAddress.serviceClassId = OBEX_PROTOCOL_UUID;
	socketAddress.port = 0;

	int connectResult = connect(socketPtr, (SOCKADDR*)&socketAddress, sizeof(socketAddress));
	if (connectResult == SOCKET_ERROR)
	{
		m_lastError = WSAGetLastError();
		if (m_lastError != WSAETIMEDOUT)
			printf("connect failed %d\n", m_lastError);

		closesocket(socketPtr);
		return false;
	}

	closesocket(socketPtr);
	m_isInRange = true;

	return true;
}

#define MAX_DEVICES 8
Device g_devices[MAX_DEVICES];
unsigned int g_numDevices = 0;
bool g_started = false;

bool bt_init()
{
	WSADATA wsaData;
	DWORD result = WSAStartup(MAKEWORD(2,2), &wsaData);
	if (result != 0)
	{
		printf("Cannot startup Winsock, error code %d\n", result);
		return false;
	}

	bt_refreshDevices();

	g_started = true;
	return true;
}

void bt_shutdown()
{
	assert(g_started && "Trying to shut down BT even though it haven't been successfully started");
	WSACleanup();
}

void bt_refreshDevices()
{
	g_numDevices = 0;

	HANDLE lookupHandle;

	WSAQUERYSET querySet;
	ZeroMemory(&querySet, sizeof(querySet));
	querySet.dwSize = sizeof(querySet);
	querySet.dwNameSpace = NS_BTH;

	if (ERROR_SUCCESS != WSALookupServiceBegin (&querySet, LUP_CONTAINERS, &lookupHandle))
	{
		printf("WSALookupServiceBegin failed %d\n", WSAGetLastError());
		return;
	}

	char data[5000] = {0};
	WSAQUERYSET& queryData = *(WSAQUERYSET*)data;
	DWORD dwSize = sizeof(data);

	int result = 0;
	while ((result = WSALookupServiceNext(lookupHandle, LUP_RETURN_NAME | LUP_RETURN_ADDR, &dwSize, &queryData)) == ERROR_SUCCESS)
	{
		if (queryData.dwNumberOfCsAddrs != 1)
		{
			printf("NumberOfCsAddrs != 1. Value: %d\n", WSAGetLastError());
			WSALookupServiceEnd(lookupHandle);
			return;
		}

		SOCKADDR_BTH& socketAddress = *(SOCKADDR_BTH *)queryData.lpcsaBuffer->RemoteAddr.lpSockaddr;

		if (g_numDevices < MAX_DEVICES)
		{
			g_devices[g_numDevices++] = Device(queryData.lpszServiceInstanceName, socketAddress.btAddr);
		}
	}

	int error = WSAGetLastError();
	if (error != WSA_E_NO_MORE && error != WSAENOMORE)
	{
		printf("WSALookupServiceNext failed %d\n", error);
		WSALookupServiceEnd(lookupHandle);
		return;
	}

	WSALookupServiceEnd(lookupHandle);
}

unsigned int bt_getDeviceCount()
{
	return g_numDevices;
}

const char* bt_getDeviceName(unsigned int index)
{
	if (index < g_numDevices)
	{
		return g_devices[index].getName();
	}
	return "";
}

void bt_updateDevice(const char* deviceName)
{
	for (unsigned int i = 0; i < g_numDevices; ++i)
	{
		Device& device = g_devices[i];
		if (strcmp(deviceName, device.getName()) == 0)
			device.refreshData();
	}
}

bool bt_inRange(const char* deviceName)
{
	for (unsigned int i = 0; i < g_numDevices; ++i)
	{
		const Device& device = g_devices[i];
		if (strcmp(deviceName, device.getName()) == 0 && device.isInRange())
			return true;
	}

	return false;
}

int bt_getLastError(const char* deviceName)
{
	for (unsigned int i = 0; i < g_numDevices; ++i)
	{
		const Device& device = g_devices[i];
		if (strcmp(deviceName, device.getName()) == 0)
		{
			return device.getLastError();
		}
	}

	return -1;
}
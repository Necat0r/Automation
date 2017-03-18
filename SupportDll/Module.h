#pragma once

#if defined(SUPPORTDLL_EXPORTS)
#define AUTOMATION_API __declspec(dllexport)
#else
#define AUTOMATION_API __declspec(dllimport)
#endif
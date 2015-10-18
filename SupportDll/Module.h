

#if defined(AUTOMATION_BUILD)
#define AUTOMATION_API __declspec(dllexport)
#else
#define AUTOMATION_API __declspec(dllimport)
#endif
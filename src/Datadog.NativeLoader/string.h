#ifndef STRING_H_
#define STRING_H_

#include <corhlpr.h>
#include <sstream>
#include <string>

#ifdef _WIN32
#define WStr(value) L##value
#define WStrLen(value) (size_t) wcslen(value)
#else
#define WStr(value) u##value
#define WStrLen(value) (size_t) std::char_traits<char16_t>::length(value)
#endif

typedef std::basic_string<WCHAR> WSTRING;

#ifndef MACOS
typedef std::basic_stringstream<WCHAR> WSTRINGSTREAM;
#endif

std::string ToString(const std::string& str);
std::string ToString(const char* str);
std::string ToString(const uint64_t i);
std::string ToString(const WSTRING& wstr);

WSTRING ToWSTRING(const std::string& str);
WSTRING ToWSTRING(const uint64_t i);

WSTRING HexStr(const void* data, int len);

#endif // STRING_H_

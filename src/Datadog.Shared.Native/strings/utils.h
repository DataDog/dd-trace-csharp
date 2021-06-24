#ifndef DD_SHARED_STRING_H_
#define DD_SHARED_STRING_H_

#include <corhlpr.h>
#include <sstream>
#include <string>
#include <vector>

#ifdef _WIN32
#define WStr(value) L##value
#define WStrLen(value) (size_t) wcslen(value)
#else
#define WStr(value) u##value
#define WStrLen(value) (size_t) std::char_traits<char16_t>::length(value)
#endif

namespace trace
{

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

template <typename Out>
void Split(const WSTRING& s, wchar_t delim, Out result);

// Split splits a string by the given delimiter.
std::vector<WSTRING> Split(const WSTRING& s, wchar_t delim);

// Trim removes space from the beginning and end of a string.
WSTRING Trim(const WSTRING& str);

// Convert Hex to string
WSTRING HexStr(const void* data, int len);

// Convert Token to string
WSTRING TokenStr(const mdToken* token);

} // namespace trace

#endif // DD_SHARED_STRING_H_

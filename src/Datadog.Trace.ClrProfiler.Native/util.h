#ifndef DD_CLR_PROFILER_UTIL_H_
#define DD_CLR_PROFILER_UTIL_H_

#include <sstream>
#include <string>
#include <vector>

namespace trace {

std::string toString(const std::string &str);
std::string toString(const std::wstring &wstr);
std::string toString(int x);

template <typename Arg>
inline std::string ToString(Arg const &arg) {
  return toString(arg);
}

template <typename... Args>
inline std::string ToString(Args const &... args) {
  std::ostringstream oss;
  int a[] = {0, ((void)(oss << ToString(args) << " "), 0)...};
  return oss.str();
}

std::wstring toWString(const std::string &str);
std::wstring toWString(const std::wstring &wstr);
std::wstring toWString(int x);

template <typename Arg>
inline std::wstring ToWString(Arg const &arg) {
  return toWString(arg);
}

template <typename... Args>
inline std::wstring ToWString(Args const &... args) {
  std::wstringstream s;
  int a[] = {0, ((void)(s << ToWString(args) << " "), 0)...};
  return s.str();
}

template <typename Out>
void Split(const std::wstring &s, wchar_t delim, Out result);

// Split splits a string by the given delimiter.
std::vector<std::wstring> Split(const std::wstring &s, wchar_t delim);

// Trim removes space from the beginning and end of a string.
std::wstring Trim(const std::wstring &str);

// GetEnvironmentValue returns the environment variable value for the given
// name. Space is trimmed.
std::wstring GetEnvironmentValue(const std::wstring &name);

// GetEnvironmentValues returns environment variable values for the given name
// split by the delimiter. Space is trimmed and empty values are ignored.
std::vector<std::wstring> GetEnvironmentValues(const std::wstring &name,
                                               const wchar_t delim);

// GetEnvironmentValues calls GetEnvironmentValues with a semicolon delimiter.
std::vector<std::wstring> GetEnvironmentValues(const std::wstring &name);

// GetCurrentProcessName gets the current process file name.
std::wstring GetCurrentProcessName();

}  // namespace trace

#endif  // DD_CLR_PROFILER_UTIL_H_

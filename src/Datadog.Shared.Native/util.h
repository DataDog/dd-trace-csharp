#ifndef DD_SHARED_UTIL_H_
#define DD_SHARED_UTIL_H_

#include <condition_variable>
#include <mutex>
#include <queue>
#include <vector>

#include "strings/utils.h"

namespace trace
{

// GetEnvironmentValue returns the environment variable value for the given
// name. Space is trimmed.
WSTRING GetEnvironmentValue(const WSTRING& name);

// GetEnvironmentValues returns environment variable values for the given name
// split by the delimiter. Space is trimmed and empty values are ignored.
std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name, const wchar_t delim);

// GetEnvironmentValues calls GetEnvironmentValues with a semicolon delimiter.
std::vector<WSTRING> GetEnvironmentValues(const WSTRING& name);

template <class Container>
bool Contains(const Container& items, const typename Container::value_type& value)
{
    return std::find(items.begin(), items.end(), value) != items.end();
}

// Singleton definition
class UnCopyable
{
protected:
    UnCopyable(){};
    ~UnCopyable(){};

private:
    UnCopyable(const UnCopyable&) = delete;
    UnCopyable(const UnCopyable&&) = delete;
    UnCopyable& operator=(const UnCopyable&) = delete;
    UnCopyable& operator=(const UnCopyable&&) = delete;
};

template <typename T>
class Singleton : public UnCopyable
{
public:
    static T* Instance()
    {
        static T instance_obj;
        return &instance_obj;
    }
};

template <typename T>
class BlockingQueue : public UnCopyable
{
private:
    std::queue<T> queue_;
    mutable std::mutex mutex_;
    std::condition_variable condition_;

public:
    T pop()
    {
        std::unique_lock<std::mutex> mlock(mutex_);
        while (queue_.empty())
        {
            condition_.wait(mlock);
        }
        T value = queue_.front();
        queue_.pop();
        return value;
    }
    void push(const T& item)
    {
        {
            std::lock_guard<std::mutex> guard(mutex_);
            queue_.push(item);
        }
        condition_.notify_one();
    }
};

} // namespace trace

#endif // DD_SHARED_UTIL_H_

using System;

namespace Datadog.Trace.ClrProfiler.Integrations
{
    internal static class Interception
    {
        internal static readonly Type[] EmptyTypes = Type.EmptyTypes;

        internal static Type[] TypeArray(params object[] objectsToCheck)
        {
            var types = new Type[objectsToCheck.Length];

            for (var i = 0; i < objectsToCheck.Length; i++)
            {
                types[i] = objectsToCheck[i].GetType();
            }

            return types;
        }

        internal static string MethodKey(Type[] genericTypes, Type[] parameterTypes)
        {
            var key = "m";

            for (int i = 0; i < genericTypes.Length; i++)
            {
                key = string.Concat(key, $"_g{genericTypes[i].FullName}");
            }

            for (int i = 0; i < parameterTypes.Length; i++)
            {
                key = string.Concat(key, $"_p{parameterTypes[i].FullName}");
            }

            return key;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Datadog.Trace.Configuration
{
    internal static class IntegrationRegistry
    {
        internal static readonly string[] Names;

        internal static readonly IReadOnlyDictionary<string, int> Ids;

        static IntegrationRegistry()
        {
            var values = Enum.GetValues(typeof(IntegrationIds));
            var ids = new Dictionary<string, int>(values.Length);

            Names = new string[values.Cast<int>().Max() + 1];

            foreach (IntegrationIds value in values)
            {
                var name = value.ToString();

                Names[(int)value] = name;
                ids.Add(name, (int)value);
            }

            Ids = ids;
        }
    }
}

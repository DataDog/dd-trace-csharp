using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Datadog.Trace.ClrProfiler.Managed.Tests
{
    public class TypeNameTests
    {
        public static IEnumerable<object[]> GetConstTypeAssociations()
        {
            yield return new object[] { ClrNames.Void, typeof(void) };
            yield return new object[] { ClrNames.Object, typeof(object) };
            yield return new object[] { ClrNames.Bool, typeof(bool) };
            yield return new object[] { ClrNames.String, typeof(string) };
            yield return new object[] { ClrNames.Byte, "System.UInt8" };
            yield return new object[] { ClrNames.Int8, "System.Int8" };
            yield return new object[] { ClrNames.Int16, typeof(short) };
            yield return new object[] { ClrNames.Int32, typeof(int) };
            yield return new object[] { ClrNames.Int64, typeof(long) };
            yield return new object[] { ClrNames.UInt8, "System.UInt8" };
            yield return new object[] { ClrNames.UInt16, typeof(ushort) };
            yield return new object[] { ClrNames.UInt32, typeof(uint) };
            yield return new object[] { ClrNames.UInt64, typeof(ulong) };
        }

        [Fact]
        public void EveryMemberOfTypeNamesIsRepresented()
        {
            var associations = GetConstTypeAssociations().Select(i => i[0]).ToList();
            var expectedItems =
                typeof(ClrNames)
                   .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                   .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                   .Where(fi => fi.Name != nameof(ClrNames.Ignore))
                   .ToList();

            Assert.Equal(actual: associations.Count, expected: expectedItems.Count);

            var missing = new List<string>();
            foreach (var expectedItem in expectedItems)
            {
                var value = (string)expectedItem.GetRawConstantValue();
                if (associations.Contains(value))
                {
                    continue;
                }

                missing.Add(value);
            }

            Assert.Empty(missing);
        }

        [Theory]
        [MemberData(nameof(GetConstTypeAssociations))]
        public void StringMatches(string constant, object type)
        {
            if (type is Type)
            {
                Assert.Equal(constant, ((Type)type).FullName);
            }

            if (type is string)
            {
                Assert.Equal(constant, (string)type);
            }
        }
    }
}

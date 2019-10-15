using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Samples.SqlServer
{
    public static class Extensions
    {
        public static IEnumerable<IDataRecord> AsDataRecords(this DbDataReader reader)
        {
            while (reader.Read())
            {
                yield return reader;
            }
        }
    }
}

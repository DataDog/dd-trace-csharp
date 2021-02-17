//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the GeneratePackageVersions tool. To safely
//     modify this file, edit PackageVersionsGeneratorDefinitions.json and
//     re-run the GeneratePackageVersions project in Visual Studio. See the
//     launchSettings.json for the project if you would like to run the tool
//     with the correct arguments outside of Visual Studio.

//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated. 
// </auto-generated>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    [SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1516:Elements must be separated by blank line", Justification = "This is an auto-generated file.")]
    public class PackageVersions
    {
#if COMPREHENSIVE_TESTS
        public static readonly bool IsComprehensive = true;
#else
        public static readonly bool IsComprehensive = false;
#endif

        public static IEnumerable<object[]> MongoDB => IsComprehensive ? PackageVersionsComprehensive.MongoDB : PackageVersionsLatestMinors.MongoDB;

        public static IEnumerable<object[]> ElasticSearch6 => IsComprehensive ? PackageVersionsComprehensive.ElasticSearch6 : PackageVersionsLatestMinors.ElasticSearch6;

        public static IEnumerable<object[]> ElasticSearch5 => IsComprehensive ? PackageVersionsComprehensive.ElasticSearch5 : PackageVersionsLatestMinors.ElasticSearch5;

        public static IEnumerable<object[]> Npgsql => IsComprehensive ? PackageVersionsComprehensive.Npgsql : PackageVersionsLatestMinors.Npgsql;

        public static IEnumerable<object[]> RabbitMQ => IsComprehensive ? PackageVersionsComprehensive.RabbitMQ : PackageVersionsLatestMinors.RabbitMQ;

        public static IEnumerable<object[]> SystemDataSqlClient => IsComprehensive ? PackageVersionsComprehensive.SystemDataSqlClient : PackageVersionsLatestMinors.SystemDataSqlClient;

        public static IEnumerable<object[]> MicrosoftDataSqlClient => IsComprehensive ? PackageVersionsComprehensive.MicrosoftDataSqlClient : PackageVersionsLatestMinors.MicrosoftDataSqlClient;

        public static IEnumerable<object[]> StackExchangeRedis => IsComprehensive ? PackageVersionsComprehensive.StackExchangeRedis : PackageVersionsLatestMinors.StackExchangeRedis;

        public static IEnumerable<object[]> ServiceStackRedis => IsComprehensive ? PackageVersionsComprehensive.ServiceStackRedis : PackageVersionsLatestMinors.ServiceStackRedis;

        public static IEnumerable<object[]> MySqlData => IsComprehensive ? PackageVersionsComprehensive.MySqlData : PackageVersionsLatestMinors.MySqlData;

        public static IEnumerable<object[]> MicrosoftDataSqlite => IsComprehensive ? PackageVersionsComprehensive.MicrosoftDataSqlite : PackageVersionsLatestMinors.MicrosoftDataSqlite;

        public static IEnumerable<object[]> OracleMDA => IsComprehensive ? PackageVersionsComprehensive.OracleMDA : PackageVersionsLatestMinors.OracleMDA;

        public static IEnumerable<object[]> OracleMDACore => IsComprehensive ? PackageVersionsComprehensive.OracleMDACore : PackageVersionsLatestMinors.OracleMDACore;
    }
}

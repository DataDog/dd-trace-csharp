using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace GeneratePackageVersions
{
    public class XunitStrategyFileGenerator : FileGenerator
    {
        private const string HeaderConst =
@"//------------------------------------------------------------------------------
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
    [SuppressMessage(""StyleCop.CSharp.LayoutRules"", ""SA1516:Elements must be separated by blank line"", Justification = ""This is an auto-generated file."")]
    public class PackageVersions
    {
#if COMPREHENSIVE_TESTS
        public static readonly bool IsComprehensive = true;
#else
        public static readonly bool IsComprehensive = false;
#endif";

        private const string FooterConst =
@"    }
}";

        private const string BodyFormat =
@"{1}        public static IEnumerable<object[]> {0} => IsComprehensive ? PackageVersionsComprehensive.{0} : PackageVersionsLatestMinors.{0};{2}";

        private const string EndIfDirectiveConst =
            @"
#endif";

        public XunitStrategyFileGenerator(string filename)
            : base(filename)
        {
        }

        protected override string Header
        {
            get
            {
                return HeaderConst;
            }
        }

        protected override string Footer
        {
            get
            {
                return FooterConst;
            }
        }

        public override void Write(PackageVersionEntry packageVersionEntry, IEnumerable<string> packageVersions)
        {
            Debug.Assert(Started, "Cannot call Write() before calling Start()");
            Debug.Assert(!Finished, "Cannot call Write() after calling Finish()");

            FileStringBuilder.AppendLine();
            string ifDirective = string.IsNullOrEmpty(packageVersionEntry.SampleTargetFramework) ? string.Empty : $"#if {packageVersionEntry.SampleTargetFramework.ToUpper().Replace('.', '_')}{Environment.NewLine}";
            string endifDirective = string.IsNullOrEmpty(packageVersionEntry.SampleTargetFramework) ? string.Empty : EndIfDirectiveConst;
            FileStringBuilder.AppendLine(string.Format(BodyFormat, packageVersionEntry.IntegrationName, ifDirective, endifDirective));
        }
    }
}

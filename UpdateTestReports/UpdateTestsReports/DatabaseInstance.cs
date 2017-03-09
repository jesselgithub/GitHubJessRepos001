using System.Collections.Generic;
using DbRef;

namespace UpTestDater
{
    public static class DatabaseInstance
    {
        public static List<Assembly> Assemblies { get; set; }
        public static List<Fixture> Fixtures { get; set; }
        public static List<Testcase> Testcases { get; set; }
        public static List<Testcase> NewTestcases { get; set; }
        public static List<TestDate> TestDates { get; set; }
        public static List<TiaVersion> TiaVersions { get; set; }
        public static List<BuildDefinition> BuildDefinitions { get; set; }
        public static List<BuildInstance> BuildInstances { get; set; }
        public static List<TestReport> TestReports { get; set; }

    }
}
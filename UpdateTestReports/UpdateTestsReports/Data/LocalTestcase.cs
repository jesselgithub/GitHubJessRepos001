using System.Collections.Generic;

namespace UpTestDater.Data
{
    public class LocalTestcase
    {
        public LocalTestcase(LocalFixture localFixture, string name, string fullname)
        {
            TestcaseName = name;
            TestcaseFullName = fullname;
            ParentFixture = localFixture;
        }
        public LocalReport CurrentLocalReport { get; set; }
        public int TestcaseId { get; set; }
        public int FixtureId { get { return ParentFixture.FixtureId; } }
        public string TestcaseName { get; set; }
        public string TestcaseFullName { get; set; }
        public LocalFixture ParentFixture { get; set; }
        public List<LocalReport> LocalReports { get; set; } = new List<LocalReport>();
        public override bool Equals(object obj)
        {
            LocalTestcase ltc = obj as LocalTestcase;
            return (ltc?.ParentFixture != null) && ltc.ParentFixture.Equals(ParentFixture) && (ltc.TestcaseFullName == TestcaseFullName) && (ltc.TestcaseName == TestcaseName);
        }
    }
}
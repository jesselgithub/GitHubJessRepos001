using System.Collections.Generic;

namespace UpTestDater.Data
{
    public class LocalFixture
    {
        public LocalTestcase CurrentLocalTestcase { get; set; }
        public LocalFixture(LocalAssembly assembly, string name, string fullname)
        {
            ParentAssembly = assembly;
            FixtureName = name;
            FixtureFullName = fullname;
        }
        public int FixtureId { get; set; }
        public int AssemblyId { get { return ParentAssembly.AssemblyId; }  }
        public string FixtureName { get; set; }
        public string FixtureFullName { get; set; }
        public List<LocalTestcase> LocalTestcases { get; set; } = new List<LocalTestcase>();
        public LocalAssembly ParentAssembly { get; private set; }
        //public void AddTestcase(Loca)
        public override bool Equals(object obj)
        {
            LocalFixture lf = obj as LocalFixture;
            return (ParentAssembly == lf?.ParentAssembly) && (FixtureName == lf?.FixtureName) && (FixtureFullName == lf?.FixtureFullName);
        }
    }
}
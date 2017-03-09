using System.Collections.Generic;
using System.Linq;

namespace UpTestDater.Data
{
    public class LocalAssembly
    {
        public LocalFixture CurrentLocalFixture { get; set; }

        public LocalAssembly(string name, string relativepath)
        {
            AssemblyName = name;
            AssemblyRelativePath = relativepath;
        }
        public int AssemblyId { get; set; }
        public string AssemblyName { get; set; }
        public string AssemblyRelativePath { get; set; }
        public List<LocalFixture> LocalFixtures { get; set; } = new List<LocalFixture>();

        public override bool Equals(object obj)
        {
            LocalAssembly ls = obj as LocalAssembly;
            return (ls?.AssemblyRelativePath == AssemblyRelativePath) && (AssemblyName == ls?.AssemblyName);
        }
    }
}
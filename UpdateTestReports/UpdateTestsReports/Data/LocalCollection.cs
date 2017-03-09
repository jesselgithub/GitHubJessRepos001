using System.Collections.Generic;
using System.Linq;

namespace UpTestDater.Data
{
    public static class LocalCollection
    {
        public static List<LocalAssembly> LocalAssemblies = new List<LocalAssembly>();
        //private static List<LocalFixture> m_LocalFixtures;
        public static List<LocalFixture> AllLocalFixtures
        {
            get
            {
                //if (m_LocalFixtures == null)
                //{
                var m_LocalFixtures = new List<LocalFixture>();
                LocalAssemblies.ForEach(x => { m_LocalFixtures.AddRange(x.LocalFixtures); });
                //}
                return m_LocalFixtures;
            }
        }

        public static List<LocalTestcase> AllLocalTestcases
        {
            get
            {
                var allTcs = new List<LocalTestcase>();
                AllLocalFixtures.ForEach(x => { allTcs.AddRange(x.LocalTestcases); });
                return allTcs;
            }
        }
        public static List<LocalReport> AllLocalReports
        {
            get
            {
                var allRprts = new List<LocalReport>();
                AllLocalTestcases.ForEach(x => { allRprts.AddRange(x.LocalReports); });
                return allRprts;
            }
        }
        public static LocalAssembly CurrentLocalAssembly { get; set; }

        public static int CountOfFixtures
        {
            get { return LocalAssemblies.Sum(x => x.LocalFixtures.Count); }
        }
        public static int CountOfTestCases
        {
            get { return LocalAssemblies.Sum(x => x.LocalFixtures.Sum(y => y.LocalTestcases.Count)); }
        }
    }
}
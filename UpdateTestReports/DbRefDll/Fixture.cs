//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DbRef
{
    using System;
    using System.Collections.Generic;
    
    public partial class Fixture
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Fixture()
        {
            this.Testcases = new HashSet<Testcase>();
            this.ReportFixtures = new HashSet<ReportFixture>();
        }
    
        public int FixtureId { get; set; }
        public int AssemblyId { get; set; }
        public string FixtureName { get; set; }
        public string FixtureFullName { get; set; }
    
        public virtual Assembly Assembly { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Testcase> Testcases { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ReportFixture> ReportFixtures { get; set; }
    }
}

﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class PEUTEST002Entities : DbContext
    {
        public PEUTEST002Entities()
            : base("name=PEUTEST002Entities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Assembly> Assemblies { get; set; }
        public virtual DbSet<BuildDefinition> BuildDefinitions { get; set; }
        public virtual DbSet<BuildInstance> BuildInstances { get; set; }
        public virtual DbSet<Fixture> Fixtures { get; set; }
        public virtual DbSet<Testcase> Testcases { get; set; }
        public virtual DbSet<TestDate> TestDates { get; set; }
        public virtual DbSet<TestReport> TestReports { get; set; }
        public virtual DbSet<TiaVersion> TiaVersions { get; set; }
        public virtual DbSet<ReportAssembly> ReportAssemblies { get; set; }
        public virtual DbSet<ReportBuildInstance> ReportBuildInstances { get; set; }
        public virtual DbSet<ReportFixture> ReportFixtures { get; set; }
    }
}
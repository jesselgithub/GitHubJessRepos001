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
    
    public partial class ReportFixture
    {
        public Nullable<int> PassCount { get; set; }
        public Nullable<int> PassPercent { get; set; }
        public Nullable<int> FailCount { get; set; }
        public Nullable<int> FailPercent { get; set; }
        public Nullable<int> IgnoreCount { get; set; }
        public Nullable<int> IgnorePercent { get; set; }
        public int BuildInstanceId { get; set; }
        public int FixtureId { get; set; }
        public Nullable<int> TotalCount { get; set; }
    
        public virtual BuildInstance BuildInstance { get; set; }
        public virtual Fixture Fixture { get; set; }
    }
}

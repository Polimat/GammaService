//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GammaService
{
    using System;
    using System.Collections.Generic;
    
    public partial class Templates
    {
        public System.Guid TemplateID { get; set; }
        public Nullable<System.Guid> ReportID { get; set; }
        public string Name { get; set; }
        public byte[] Template { get; set; }
        public string Version { get; set; }
    
        public virtual Reports Reports { get; set; }
    }
}

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
    
    public partial class C1CPropertyValues
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public C1CPropertyValues()
        {
            this.C1CCharacteristicProperties = new HashSet<C1CCharacteristicProperties>();
        }
    
        public System.Guid C1CPropertyValueID { get; set; }
        public string C1CCode { get; set; }
        public Nullable<System.Guid> C1CPropertyID { get; set; }
        public Nullable<byte> ValueType { get; set; }
        public Nullable<bool> Marked { get; set; }
        public Nullable<bool> Folder { get; set; }
        public Nullable<System.Guid> ParentID { get; set; }
        public string Description { get; set; }
        public string PrintDescription { get; set; }
        public Nullable<decimal> ValueNumeric { get; set; }
        public string SortValue { get; set; }
        public Nullable<bool> NotForName { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<C1CCharacteristicProperties> C1CCharacteristicProperties { get; set; }
    }
}

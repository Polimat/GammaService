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
    
    public partial class Products
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Products()
        {
            this.DocProductionProducts = new HashSet<DocProductionProducts>();
            this.DocWithdrawalProducts = new HashSet<DocWithdrawalProducts>();
            this.ProductItems = new HashSet<ProductItems>();
        }
    
        public System.Guid ProductID { get; set; }
        public string Number { get; set; }
        public string BarCode { get; set; }
        public byte ProductKindID { get; set; }
        public Nullable<byte> StateID { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DocProductionProducts> DocProductionProducts { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<DocWithdrawalProducts> DocWithdrawalProducts { get; set; }
        public virtual ProductPallets ProductPallets { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<ProductItems> ProductItems { get; set; }
    }
}

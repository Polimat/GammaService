//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace GammaService
{
    using System;
    using System.Collections.Generic;
    
    public partial class ModbusDevices
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public ModbusDevices()
        {
            this.PlaceRemotePrintingSettings = new HashSet<PlaceRemotePrintingSettings>();
        }
    
        public int ModbusDeviceID { get; set; }
        public Nullable<int> ModbusDeviceTypeID { get; set; }
        public string IPAddress { get; set; }
        public string Name { get; set; }
        public int TimerTick { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<PlaceRemotePrintingSettings> PlaceRemotePrintingSettings { get; set; }
    }
}

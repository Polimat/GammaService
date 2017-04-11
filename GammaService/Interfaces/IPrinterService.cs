using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace GammaService.Interfaces
{
    [ServiceContract]
    interface IPrinterService
    {
        [OperationContract]
        bool PrintPallet(Guid productId);
    }
}

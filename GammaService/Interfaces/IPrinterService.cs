using System;
using System.ServiceModel;

namespace GammaService.Interfaces
{
    [ServiceContract]
    interface IPrinterService
    {
        [OperationContract]
        bool PrintPallet(Guid productId);

		[OperationContract]
	    bool ActivateProductionTask(Guid productionTaskId);
    }
}

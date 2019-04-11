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
	    bool? ActivateProductionTask(Guid productionTaskId, int placeId, int remotePrinterLabelId);

        [OperationContract]
        bool? ChangePrinterStatus(int placeId, int remotePrinterLabelId);

        [OperationContract]
        bool? GetPrinterStatus(int placeId, int remotePrinterLabelId);

        [OperationContract]
        bool PrintLabel(int placeId, int remotePrinterLabelId, Guid? productId);

        [OperationContract]
        bool? ChangePrintPortStatus(int placeId, int remotePrinterLabelId);

        [OperationContract]
        bool UpdateGroupPackageLabelInProductionTask(Guid productionTaskId);

        [OperationContract]
        Tuple<bool,string> UpdateGroupPackLabelInProductionTask(Guid productionTaskId);

        [OperationContract]
        bool SendMessageNewEvent(Guid eventID);

    }
}

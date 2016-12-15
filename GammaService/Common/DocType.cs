using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GammaService.Common
{
    public enum DocType
    {
        DocProduction,
        DocWithdrawal,
        DocMovement,
        DocCloseShift,
        DocChangeState,
        DocShipment,
        DocUnpack,
        DocBroke,
        DocMovementOrder
    }
}

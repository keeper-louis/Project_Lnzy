using Kingdee.BOS.WebApi.ServicesStub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.ServiceFacade.KDServiceFx;

namespace Keeper_Louis.K3.MRP.Interface.PlugIn.Service
{
    public class MBillSyncService : AbstractWebApiBusinessService
    {
        public MBillSyncService(KDServiceContext context) : base(context) { }

        public string SyncMBill()
        {
            return null;
        }
    }
}

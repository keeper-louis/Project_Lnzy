using Kingdee.BOS.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using System.ComponentModel;

namespace Keeper_Louis.K3.MRP.Interface.PlugIn.Action
{
    [Description("抽取造易中间库数据同步业务对象")]
    public class SyncAction : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            //1、同步物料
            SyncMaterial(ctx);
            //2、同步BOM清单
            SyncBomBill(ctx);
            //3、同步销售订单
            SyncSalBill(ctx);
            //throw new NotImplementedException();
        }
        /// <summary>
        /// 同步销售订单
        /// 判断造易订单删除标识，将订单、bom、物料一并删除
        /// 判断造易新增订单，同步订单
        /// </summary>
        /// <param name="ctx"></param>
        private void SyncSalBill(Context ctx)
        {
            //throw new NotImplementedException();
        }
        /// <summary>
        /// 同步物料清单
        /// 造易已传递的bom清单，若果做修改，会生成新的编码重新传递，所以只判断新增物料清单即可
        /// 保存-提交-审核
        /// </summary>
        /// <param name="ctx"></param>
        private void SyncBomBill(Context ctx)
        {
            //throw new NotImplementedException();
        }
        /// <summary>
        /// 同步物料
        /// 造易已传递的物料，若果做修改，会生成新的编码重新传递，所以只判断新增物料即可
        /// 保存-提交-审核
        /// </summary>
        /// <param name="ctx"></param>
        private void SyncMaterial(Context ctx)
        {
            //throw new NotImplementedException();
        }
    }
}

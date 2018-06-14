using Kingdee.BOS.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS;
using Kingdee.BOS.Core;
using System.ComponentModel;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Orm.DataEntity;
using System.Data;
using Newtonsoft.Json.Linq;
using Keeper_Louis.K3.MRP.Interface.PlugIn.BaseModel;
using Newtonsoft.Json;
using Kingdee.BOS.WebApi.Client;

namespace Keeper_Louis.K3.MRP.Interface.PlugIn.Action
{
    [Description("抽取造易中间库数据同步业务对象")]
    public class SyncAction : IScheduleService
    {
        public void Run(Context ctx, Schedule schedule)
        {
            //0、获取本次同步订单数据
            DynamicObjectCollection salNoCol = getSalNo(ctx);
            if (salNoCol != null && salNoCol.Count() > 0)
            {
                //1、同步物料
                List<string> salNosList_1 = SyncMaterial(ctx, salNoCol);
                if (salNosList_1 != null)
                {
                    //2、同步BOM清单
                    List<string> salNosList_2 = SyncBomBill(ctx, salNosList_1);
                    if (salNosList_2.Count() > 0 && salNosList_2 != null)
                    {
                        //3、同步销售订单
                        SyncSalBill(ctx, salNosList_2);
                    }
                }
            }
            else
            {
                //删除订单数据
            }

            //throw new NotImplementedException();
        }

        //获取本次处理订单数据集合
        private DynamicObjectCollection getSalNo(Context ctx)
        {
            string strSql = string.Format(@"/*dialect*/SELECT FSALBILLNO FROM SALE_MAIN WHERE FFLAG = 0 AND FDELFLAG = 0");
            DynamicObjectCollection salNoCol = DBUtils.ExecuteDynamicObject(ctx, strSql);
            return salNoCol;
        }

        /// <summary>
        /// 同步销售订单
        /// 判断造易订单删除标识，将订单、bom、物料一并删除
        /// 判断造易新增订单，同步订单
        /// </summary>
        /// <param name="ctx"></param>
        private void SyncSalBill(Context ctx, List<string> salNoCol)
        {
            //throw new NotImplementedException();
        }
        /// <summary>
        /// 同步物料清单
        /// 造易已传递的bom清单，若果做修改，会生成新的编码重新传递，所以只判断新增物料清单即可
        /// 保存-提交-审核
        /// </summary>
        /// <param name="ctx"></param>
        private List<string> SyncBomBill(Context ctx, List<string> salNoCol)
        {
            long fid = 0;//单据序号
            string fsalbillno = "";//销售单号
            string forgno = "";//组织编号
            string ffathermatno = "";//父物料编号
            string ffatherunit = "";//父物料单位
            string fchildmaterialno = "";//子物料编码
            string fchildunit = "";//子物料单位
            double fnumerator = 0;//用量分子
            double fdenominator = 0;//用量分母
            string feffectdate = "";//生效日期
            string fexpiredate = "";//失效日期
            
            #region 弃用
            //0 本次处理销售订单号
            //string salNos = "";
            //foreach (string salNo in salNoCol)
            //{
            //    salNos += ",'" + salNo + "'";
            //}
            //salNos = salNos.Substring(1);

            //0 获取BOM_MAIN表信息(where FSALBILLNO = ? and FDELFLAG = 0 AND FFLAG = 0)
            //            string bomMainSql = string.Format(@"/dialect*/SELECT BOMMAIN.FID,
            //        BOMMAIN.FSALBILLNO,
            //        BOMMAIN.FORGNO,
            //        BOMMAIN.FFATHERMATNO,
            //        BOMMAIN.FFATHERUNIT
            //FROM BOM_MAIN BOMMAIN
            //WHERE BOMMAIN.FSALBILLNO IN(@ids) AND BOMMAIN.FFLAG = 0 AND BOMMAIN.FDELFLAG = 0");
            //            SqlParam sp = new SqlParam("@ids", KDDbType.String, salNos);

            //1.1 更新子表状态(需要判断主表FFLAG = 0,FDELFLAG = 0)
            //StringBuilder updateListFlagSql = new StringBuilder();
            //updateListFlagSql.AppendFormat(@"/*dialect*/UPDATE BOM_LIST SET FFLAG = 1 WHERE EXISTS(");
            //updateListFlagSql.AppendFormat(@"SELECT /*+cardinality(d " + salNoCol.Distinct().Count().ToString() + ") */ 1 FROM BOM_MAIN d WHERE D.FID = FID AND D.FFLAG = 0 AMD D.FDELFLAG = 0 AND EXISTS (");
            //updateListFlagSql.AppendFormat(@"SELECT /*+cardinality(b " + salNoCol.Distinct().Count().ToString() + ") */ FID FROM TABLE(fn_StrSplit(@ids,',',2)) b WHERE b.FID = d.FSALBILLNO))");
            //DBUtils.Execute(ctx, updateListFlagSql.ToString(), new SqlParam("@ids", KDDbType.String, salNos.Distinct().ToArray()));
            //1.2 更新主表(需要判断FFLAG=0,FDELFLAG=0)，子表状态
            //StringBuilder updateMainFlagSql = new StringBuilder();
            //updateMainFlagSql.AppendFormat(@"/*dialect*/UPDATE BOM_MAIN SET FFLAG = 1 WHERE EXISTS(SELECT /*+cardinality(c " + salNoCol.Distinct().Count().ToString() + ") */ FID FROM TABLE(fn_StrSplit(@ids,',',2)) c WHERE c.FID = BOM_MAIN.FSALBILLNO) AND BOM_MAIN.FFLAG = 0 AND BOM_MAIN.FDELFLAG = 0");
            //DBUtils.Execute(ctx, updateMainFlagSql.ToString(), new SqlParam("@ids", KDDbType.String, salNos.Distinct().ToArray()));

            //using (IDataReader dr = DBUtils.ExecuteReader(ctx, bomMainSql, sp))
            //{
            //    while (dr.Read())
            //    {

            //    }
            //}
            #endregion
            List<string> salNosList = new List<string>();//返回允许处理订单集合
            foreach (string item in salNoCol)
            {
                //0 获取BOM_MAIN表信息(where FSALBILLNO = ? and FDELFLAG = 0 AND FFLAG = 0)
                string bomMainSql = string.Format(@"/*dialect*/SELECT BOMMAIN.FID,
        BOMMAIN.FSALBILLNO,
        BOMMAIN.FORGNO,
        BOMMAIN.FFATHERMATNO,
        BOMMAIN.FFATHERUNIT
FROM BOM_MAIN BOMMAIN
WHERE BOMMAIN.FSALBILLNO = '{0}' AND BOMMAIN.FFLAG = 0 AND BOMMAIN.FDELFLAG = 0", item);
                DynamicObjectCollection bomMainCol = DBUtils.ExecuteDynamicObject(ctx, bomMainSql);
                //1 获取BOM_LIST表信息（where FSALBILLNO IN (?) AND BOM_MAIN.FID = BOM_LIST.FID AND FDELFLAG = 0 AND FFLAG = 0）
                string bomListSql = string.Format(@"/*dialect*/SELECT L.*
FROM BOM_MAIN M
INNER JOIN BOM_LIST L
    ON M.FID = L.FID
WHERE M.FSALBILLNO = '{0}'
        AND M.FFLAG = 0
        AND M.FDELFLAG = 0", item);
                DynamicObjectCollection bomListCol = DBUtils.ExecuteDynamicObject(ctx, bomListSql);
                //2 更新子表数据状态，更新主表数据状态
                string updateBomListSql = string.Format(@"/*dialect*/UPDATE BOM_LIST SET FFLAG = 1 WHERE FID IN (SELECT FID FROM BOM_MAIN WHERE FSALBILLNO = '{0}' AND FFLAG = 0 AND FDELFLAG = 0)",item);
                string updateBomMainSql = string.Format(@"/*dialect*/UPDATE BOM_MAIN SET FFLAG = 1 WHERE FSALBILLNO = '{0}' AND FFLAG = 0 AND FDELFLAG = 0",item);
                DBUtils.Execute(ctx,updateBomListSql);
                DBUtils.Execute(ctx,updateBomMainSql);
                //3 拼接参数json
                if (bomMainCol != null && bomMainCol.Count() > 0 && bomListCol != null && bomListCol.Count() > 0)
                {
                    for (int i = 0; i < bomMainCol.Count(); i++)
                    {
                        fid = Convert.ToInt64(bomMainCol[i]["FID"]);
                        fsalbillno = Convert.ToString(bomMainCol[i]["FSALBILLNO"]);
                        forgno = Convert.ToString(bomMainCol[i]["FORGNO"]);
                        ffathermatno = Convert.ToString(bomMainCol[i]["FFATHERMATNO"]);
                        ffatherunit = Convert.ToString(bomMainCol[i]["FFATHERUNIT"]);
                        JObject mBHeader = PJFJson(fid, fsalbillno, forgno, ffathermatno, ffatherunit);//拼接HeaderJson
                        JArray entrys = new JArray();//单个model中存储多行分录体集合，存储mBentry
                        for (int j = 0; j < bomListCol.Count(); j++)
                        {
                            if (fid==Convert.ToInt64(bomListCol[j]["FID"]))
                            {
                                fchildmaterialno = Convert.ToString(bomListCol[j]["FCHILDMATERIALNO"]);
                                fchildunit = Convert.ToString(bomListCol[j]["FCHILDUNIT"]);
                                fnumerator = Convert.ToDouble(bomListCol[j]["FNUMERATOR"]);
                                fdenominator = Convert.ToDouble(bomListCol[j]["FDENOMINATOR"]);
                                feffectdate = Convert.ToString(bomListCol[j]["FEFFECTDATE"]);
                                fexpiredate = Convert.ToString(bomListCol[j]["FEXPIREDATE"]);
                                //拼接单据体json
                                JObject mBEntry = PJJson(fchildmaterialno, fchildunit, fnumerator, fdenominator, feffectdate, fexpiredate);
                                entrys.Add(mBEntry);
                            }
                        }
                        mBHeader.Add("FTreeEntity",entrys);
                        string sContent = JsonConvert.SerializeObject(mBHeader);
                        ApiClient client = new ApiClient(DBHelper.ServerUrl);
                        string dbId = DBHelper.DBID; //AotuTest117
                        bool bLogin = client.Login(dbId, DBHelper.UserName, DBHelper.PassWord, Convert.ToInt32(DBHelper.ICID));
                        if (bLogin)
                        {
                            var ret = client.Execute<string>("Keeper_Louis.K3.MRP.Interface.PlugIn.Service.MBillSyncService.SyncMBill,Keeper_Louis.K3.MRP.Interface.PlugIn", new object[] { sContent });
                        }
                    }
                }
            }


            //4 拼接json参数
            //5 调用接口，返回结果日志

            return salNosList;
            //throw new NotImplementedException();
        }
        /// <summary>
        /// 拼接HeaderJson
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="fsalbillno"></param>
        /// <param name="forgno"></param>
        /// <param name="ffathermatno"></param>
        /// <param name="ffatherunit"></param>
        private JObject PJFJson(long fid, string fsalbillno, string forgno, string ffathermatno, string ffatherunit)
        {
            JObject mBHeader = new JObject();//model中单据头,存储普通变量、baseData、entrys
            JObject baseData = new JObject();//model中基础资料
            mBHeader.Add("ServerUrl", DBHelper.ServerUrl);//ServerUrl
            mBHeader.Add("DBID", DBHelper.DBID);//DBID
            mBHeader.Add("UserName", DBHelper.UserName);//USERNAME
            mBHeader.Add("PassWord", DBHelper.PassWord);//PASSWORD;
            mBHeader.Add("ICID", DBHelper.ICID);//IC
            mBHeader.Add("FSALBILLNO", fsalbillno);//销售单号
            baseData = new JObject();
            baseData.Add("FNumber", forgno);
            mBHeader.Add("FCreateOrgId", baseData);//创建组织//组织编号
            baseData = new JObject();
            baseData.Add("FNumber", ffathermatno);
            mBHeader.Add("FMATERIALID", baseData);//父物料编号
            baseData = new JObject();
            baseData.Add("FNumber", ffatherunit);
            mBHeader.Add("FUNITID", baseData);//父物料单位
            return mBHeader;

        }

        /// <summary>
        /// 拼接物料清单Json
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="fsalbillno"></param>
        /// <param name="forgno"></param>
        /// <param name="ffathermatno"></param>
        /// <param name="ffatherunit"></param>
        /// <param name="fchildmaterialno"></param>
        /// <param name="fchildunit"></param>
        /// <param name="fnumerator"></param>
        /// <param name="fdenominator"></param>
        /// <param name="feffectdate"></param>
        /// <param name="fexpiredate"></param>
        private JObject PJJson(string fchildmaterialno, string fchildunit, double fnumerator, double fdenominator, string feffectdate, string fexpiredate)
        {
            JObject mBEntry = new JObject();//model中单据体，存储普通变量，baseData
            JObject baseData = new JObject();//model中基础资料
            baseData = new JObject();
            baseData.Add("FNumber", fchildmaterialno);
            mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
            baseData = new JObject();
            baseData.Add("FNumber", fchildunit);
            mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
            mBEntry.Add("FNUMERATOR", fnumerator);//用量：分子
            mBEntry.Add("FDENOMINATOR", fdenominator);//用量：分母
            mBEntry.Add("FEFFECTDATE", feffectdate);//生效日期
            mBEntry.Add("FEXPIREDATE", fexpiredate);//失效日期
            return mBEntry;
        }

        /// <summary>
        /// 同步物料
        /// 造易已传递的物料，若果做修改，会生成新的编码重新传递，所以只判断新增物料即可
        /// 保存-提交-审核
        /// </summary>
        /// <param name="ctx"></param>
        private List<string> SyncMaterial(Context ctx, DynamicObjectCollection salNoCol)
        {
            List<string> arrayList = new List<string>();
            arrayList.Add("XXDD001");
            return arrayList;
        }
    }
}

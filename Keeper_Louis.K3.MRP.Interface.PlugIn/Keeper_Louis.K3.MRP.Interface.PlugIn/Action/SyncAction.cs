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
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.Validation;

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
                string saleBIllNos = "";
                IsDelBill(ctx, saleBIllNos);
            }
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
            //解析需要同步的销售订单单号
            string saleBIllNos = "'" + string.Join("','", salNoCol.ToArray()) + "'";
            IsDelBill(ctx, saleBIllNos);
        }

        /// <summary>
        /// 删除需要删除的销售订单
        /// </summary>
        /// <param name="fid"></param>
        /// <param name="p"></param>
        /// <param name="ctx"></param>
        public void IsDelBill(Context ctx, string saleBIllNos)
        {
            //获取中间表需要删除的物料数据、bom数据、销售订单数据
            string strSql = string.Format(@"/*dialect*/SELECT FSALBILLNO FROM  SALE_MAIN  WHERE (FFLAG = 1 OR FFLAG =2 ) AND FDELFLAG = 1");
            DynamicObjectCollection queryResult = DBUtils.ExecuteDynamicObject(ctx, strSql) as DynamicObjectCollection;
            if (queryResult != null && queryResult.Count() > 0)//存在需要删除的数据
            {
                string saleBillNo = "";
                foreach (DynamicObject item in queryResult)
                {
                    saleBillNo += ",'" + item["FSALBILLNO"].ToString() + "'";
                }
                saleBillNo = saleBillNo.Substring(1);
                //删除销售订单
                string delSql = string.Format(@"/*dialect*/SELECT FID FROM T_SAL_ORDER WHERE FBILLNO IN ({0}) ", saleBillNo);
                DynamicObjectCollection result = DBUtils.ExecuteDynamicObject(ctx, delSql) as DynamicObjectCollection;
                string[] pkids = new string[] { };
                if (result != null && result.Count() > 0)
                {
                    string UnAuditids = "";
                    foreach (DynamicObject item in result)
                    {
                        UnAuditids += item["FID"].ToString() + ",";
                    }
                    UnAuditids = UnAuditids.Substring(0, UnAuditids.LastIndexOf(','));
                    pkids = UnAuditids.Split(',');
                    UnAuditBill(ctx, "SAL_SaleOrder", pkids);
                }
                //删除物料清单(BOM)
                string delSql1 = string.Format(@"/*dialect*/SELECT FID FROM T_ENG_BOM WHERE FNUMBER IN ({0}) ", saleBillNo);
                DynamicObjectCollection bomResult = DBUtils.ExecuteDynamicObject(ctx, delSql1) as DynamicObjectCollection;
                if (bomResult != null && bomResult.Count() > 0)
                {
                    string UnAuditids = "";
                    foreach (DynamicObject item in bomResult)
                    {
                        UnAuditids += item["FID"].ToString() + ",";
                    }
                    UnAuditids = UnAuditids.Substring(0, UnAuditids.LastIndexOf(','));
                    pkids = UnAuditids.Split(',');
                    UnAuditBill(ctx, "ENG_BOM", pkids);
                }
                //删除物料
                string delSql2 = string.Format(@"/*dialect*/SELECT FMATERIALID FROM T_BD_MATERIAL WHERE FNUMBER IN ({0}) ", saleBillNo);
                DynamicObjectCollection prdResult = DBUtils.ExecuteDynamicObject(ctx, delSql2) as DynamicObjectCollection;
                if (prdResult != null && prdResult.Count() > 0)
                {
                    string UnAuditids = "";
                    foreach (DynamicObject item in prdResult)
                    {
                        UnAuditids += item["FMATERIALID"].ToString() + ",";
                    }
                    UnAuditids = UnAuditids.Substring(0, UnAuditids.LastIndexOf(','));
                    pkids = UnAuditids.Split(',');
                    UnAuditBill(ctx, "BD_MATERIAL", pkids);
                }
            }
            //判断是否存在同步销售订单单号
            if (saleBIllNos != null && !"".Equals(saleBIllNos))
            {
                SyncSaleBill(ctx, saleBIllNos); //同步销售订单
            }
        }

        /// <summary>
        /// 反审核并删除需要删除的销售订单
        /// </summary>
        /// <param name="pkids"></param>
        /// <param name="p"></param>
        /// <param name="ctx"></param>
        private void UnAuditBill(Context ctx, string p, string[] pkids)
        {
            //反审核服务
            FormMetadata meta = MetaDataServiceHelper.Load(ctx, p, true) as FormMetadata;
            OperateOption UnAuditOption = OperateOption.Create();
            var UnAuditResult = BusinessDataServiceHelper.UnAudit(ctx, meta.BusinessInfo, pkids, UnAuditOption);
            if (UnAuditResult.IsSuccess)
            {
                //删除服务
                OperateOption deleteOption = OperateOption.Create();
                var delectResult = BusinessDataServiceHelper.Delete(ctx, meta.BusinessInfo, pkids, deleteOption);
                //删除成功将成功信息及状态返回写中间表
                if (delectResult.IsSuccess)
                {
                    OperateResultCollection successResult = delectResult.OperateResult;
                    List<string> succSql = new List<string>();
                    foreach (OperateResult item in successResult)
                    {
                        if ("SAL_SaleOrder".Equals(p))
                        {
                            string strSql = string.Format(@"/*dialect*/UPDATE SALE_MAIN@ZyK3Link
                                                                              SET FFLAG = '3', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                              WHERE FSALBILLNO = '{2}'
                                                                             ", System.DateTime.Now.ToString(), item.Number.ToString() + "销售订单删除成功",
                                                                              item.Number);
                            succSql.Add(strSql);
                        }
                        if ("ENG_BOM".Equals(p))
                        {
                            string strSql = string.Format(@"/*dialect*/UPDATE BOM_MAIN@ZyK3Link
                                                                             SET FFLAG = '3', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                             WHERE FSALBILLNO = '{2}'
                                                                           ", System.DateTime.Now.ToString(), item.Number.ToString() + "物料清单删除成功",
                                                                              item.Number);
                            succSql.Add(strSql);
                        }
                        if ("BD_MATERIAL".Equals(p))
                        {
                            string strSql = string.Format(@"/*dialect*/UPDATE PRD_MAIN@ZyK3Link
                                                                              SET FFLAG = '3', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                              WHERE FSALBILLNO = '{2}'
                                                                            ", System.DateTime.Now.ToString(), item.Number.ToString() + "物料删除成功",
                                                                              item.Number);
                            succSql.Add(strSql);
                        }
                    }
                    Kingdee.BOS.Log.Logger.Info(DateTime.Now.ToString() + "deleteSucc", succSql.ToString(), true);
                    DBUtils.ExecuteBatch(ctx, succSql, 100);
                }
                else
                {
                    //多条记录被删除，一部分删除成功，一部分删除失败，将成功和失败的信息及状态都返写回中间表
                    List<ValidationErrorInfo> errorDeleteList = new List<ValidationErrorInfo>();
                    errorDeleteList = delectResult.ValidationErrors;//删除失败记录返写中间表
                    if (errorDeleteList != null && errorDeleteList.Count() > 0)
                    {
                        List<string> errorSql = new List<string>();
                        for (int k = 0; k < errorDeleteList.Count(); k++)
                        {
                            if ("SAL_SaleOrder".Equals(p))
                            {
                                string strSql = string.Format(@"/*dialect*/UPDATE SALE_MAIN@ZyK3Link
                                                                              SET FFLAG = '2', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                              WHERE FSALBILLNO = (SELECT FBILLNO FROM T_SAL_ORDER WHERE FID = '{2}')
                                                                             ", System.DateTime.Now.ToString(), errorDeleteList[k].Message, errorDeleteList[k].BillPKID);
                                errorSql.Add(strSql);
                            }
                            if ("ENG_BOM".Equals(p))
                            {
                                string strSql = string.Format(@"/*dialect*/UPDATE BOM_MAIN@ZyK3Link
                                                                             SET FFLAG = '2', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                             WHERE FSALBILLNO = (SELECT FNUMBER FROM T_ENG_BOM WHERE FID = '{2}')
                                                                           ", System.DateTime.Now.ToString(), errorDeleteList[k].Message, errorDeleteList[k].BillPKID);
                                errorSql.Add(strSql);
                            }
                            if ("BD_MATERIAL".Equals(p))
                            {
                                string strSql = string.Format(@"/*dialect*/UPDATE PRD_MAIN@ZyK3Link
                                                                              SET FFLAG = '2', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                              WHERE FSALBILLNO = (SELECT FNUMBER FROM T_BD_MATERIAL WHERE FID = '{2}')
                                                                            ", System.DateTime.Now.ToString(), errorDeleteList[k].Message, errorDeleteList[k].BillPKID);
                                errorSql.Add(strSql);
                            }
                        }
                        Kingdee.BOS.Log.Logger.Error(DateTime.Now.ToString() + "deleteError", errorSql.ToString(), new Exception());
                        DBUtils.ExecuteBatch(ctx, errorSql, 100);
                    }
                    //删除成功记录返回写中间表
                    OperateResultCollection successResult = delectResult.OperateResult;
                    if (successResult != null && successResult.Count() > 0)
                    {
                        List<string> succSql = new List<string>();
                        foreach (OperateResult item in successResult)
                        {
                            if ("SAL_SaleOrder".Equals(p))
                            {
                                string strSql = string.Format(@"/*dialect*/UPDATE SALE_MAIN@ZyK3Link
                                                                              SET FFLAG = '3', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                              WHERE FSALBILLNO = '{2}'
                                                                             ", System.DateTime.Now.ToString(), item.Number.ToString() + "销售订单删除成功",
                                                                              item.Number);
                                succSql.Add(strSql);
                            }
                            if ("ENG_BOM".Equals(p))
                            {
                                string strSql = string.Format(@"/*dialect*/UPDATE BOM_MAIN@ZyK3Link
                                                                             SET FFLAG = '3', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                             WHERE FSALBILLNO = '{2}'
                                                                           ", System.DateTime.Now.ToString(), item.Number.ToString() + "物料清单删除成功",
                                                                            item.Number);
                                succSql.Add(strSql);
                            }
                            if ("BD_MATERIAL".Equals(p))
                            {
                                string strSql = string.Format(@"/*dialect*/UPDATE PRD_MAIN@ZyK3Link
                                                                              SET FFLAG = '3', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                              WHERE FSALBILLNO = '{2}'
                                                                            ", System.DateTime.Now.ToString(), item.Number.ToString() + "物料删除成功",
                                                                             item.Number);
                                succSql.Add(strSql);
                            }
                            Kingdee.BOS.Log.Logger.Info(DateTime.Now.ToString() + "deleteSucc", succSql.ToString(), true);
                            DBUtils.ExecuteBatch(ctx, succSql, 100);
                        }
                    }
                }
            }
            else
            {
                //审核失败记录的信息及状态反写回中间表
                List<ValidationErrorInfo> errorUnAuditList = new List<ValidationErrorInfo>();
                errorUnAuditList = UnAuditResult.ValidationErrors;
                List<string> errorSql = new List<string>();
                for (int k = 0; k < errorUnAuditList.Count(); k++)
                {
                    if ("SAL_SaleOrder".Equals(p))
                    {
                        string strSql = string.Format(@"/*dialect*/UPDATE SALE_MAIN@ZyK3Link
                                                                          SET FFLAG = '2', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                          WHERE FSALBILLNO = (SELECT FBILLNO FROM T_SAL_ORDER WHERE FID = '{2}')
                                                                        ", System.DateTime.Now.ToString(), errorUnAuditList[k].Message, errorUnAuditList[k].BillPKID);
                        errorSql.Add(strSql);
                    }
                    if ("ENG_BOM".Equals(p))
                    {
                        string strSql = string.Format(@"/*dialect*/UPDATE BOM_MAIN@ZyK3Link
                                                                          SET FFLAG = '2', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                          WHERE FSALBILLNO = (SELECT FNUMBER FROM T_ENG_BOM WHERE FID = '{2}')
                                                                        ", System.DateTime.Now.ToString(), errorUnAuditList[k].Message, errorUnAuditList[k].BillPKID);
                        errorSql.Add(strSql);
                    }
                    if ("BD_MATERIAL".Equals(p))
                    {
                        string strSql = string.Format(@"/*dialect*/UPDATE PRD_MAIN@ZyK3Link
                                                                          SET FFLAG = '2', FUPDATEDATE = '{0}', FERRORMESSAGE = '{1}'
                                                                          WHERE FSALBILLNO = (SELECT FNUMBER FROM T_BD_MATERIAL WHERE FID = '{2}')
                                                                          ", System.DateTime.Now.ToString(), errorUnAuditList[k].Message, errorUnAuditList[k].BillPKID);
                        errorSql.Add(strSql);
                    }
                }
                Kingdee.BOS.Log.Logger.Error(DateTime.Now.ToString() + "UnAuditError", errorSql.ToString(), new Exception());
                DBUtils.ExecuteBatch(ctx, errorSql, 100);
            }
        }

        /// <summary>
        /// 同步销售订单
        /// </summary>
        /// <param name="saleBIllNos"></param>
        /// <param name="ctx"></param>
        public string SyncSaleBill(Context ctx, string saleBIllNos)
        {
            //获取同步销售订单的数据
            string strSql = string.Format(@"/*dialect*/SELECT FID, FSALBILLNO, FDATE, FORGNO, FCUSTNO, FPROJECTNO, FREMARK
                                                         FROM SALE_MAIN
                                                         WHERE FSALBILLNO in ({0})", saleBIllNos);
            DynamicObjectCollection queryResult = DBUtils.ExecuteDynamicObject(ctx, strSql) as DynamicObjectCollection;
            if (queryResult != null && queryResult.Count() > 0)
            {
                foreach (DynamicObject qResult in queryResult)
                {
                    saveSaleBill(ctx, qResult);
                }
            }
            return null;
        }

        public string saveSaleBill(Context ctx, DynamicObject data)
        {
            /*
               1、拼接json对象
               2、执行标准销售订单保存
          */
            JObject jsonRoot = new JObject();//存储models
            JArray models = new JArray();//多model批量保存时使用，存储mBHeader
            JObject mBHeader = new JObject();//model中单据头,存储普通变量、baseData、entrys
            JObject mBEntry = new JObject();//model中单据体，存储普通变量，baseData
            JObject sBEntry = new JObject();//model中财务子单据体，存储普通变量，baseData
            JObject baseData = new JObject();//model中基础资料
            JArray entrys = new JArray();//单个model中存储多行分录体集合，存储mBentry

            mBHeader.Add("FID", 0);//FID
            mBHeader.Add("FBillNo", data["FSALBILLNO"].ToString());//单据编号
            baseData = new JObject();
            baseData.Add("FNumber", "XSDD01_SYS");
            mBHeader.Add("FBillTypeID", baseData);//单据类型
            mBHeader.Add("FBusinessType", "NORMAL");//业务类型
            mBHeader.Add("FDate", data["FDATE"].ToString());//业务日期
            baseData = new JObject();
            baseData.Add("FNumber", data["FORGNO"].ToString());
            mBHeader.Add("FSaleOrgId", baseData);//销售组织
            baseData = new JObject();
            baseData.Add("FNumber", data["FCUSTNO"].ToString());
            mBHeader.Add("FCustId", baseData);//客户编码

            sBEntry = new JObject();
            baseData = new JObject();
            baseData.Add("FNumber", "HLTX01_SYS");
            sBEntry.Add("FExchangeTypeId", baseData);//汇率类型
            sBEntry.Add("FExchangeRate", "1.0");//汇率
            baseData = new JObject();
            baseData.Add("FNumber", "PRE001");
            sBEntry.Add("FLocalCurrId", baseData);//本位币
            mBHeader.Add("FSaleOrderFinance", sBEntry);//财务信息
            baseData = new JObject();
            baseData.Add("FNumber", "HLTX01_SYS");
            mBHeader.Add("FExchangeTypeId", baseData);//汇率类型
            //baseData = new JObject();
            //baseData.Add("FNumber", "BM000002");
            //mBHeader.Add("FSaleDeptId", baseData);//销售部门
            baseData = new JObject();
            baseData.Add("FNumber", "PRE001");
            mBHeader.Add("FSettleCurrId", baseData);//结算币别
            baseData = new JObject();
            baseData.Add("FNumber", data["FPROJECTNO"].ToString());
            mBHeader.Add("FPROJECTNO", baseData);//工程名称  
            mBHeader.Add("FNote", data["FREMARK"].ToString());//备注（单据编号+工程名称）  

            string saleFid = data["FID"].ToString();
            if (!"".Equals(saleFid) && saleFid != null)
            {
                string querySql = string.Format(@"/*dialect*/SELECT  FMATERIALNO,FQTY ,FUNIT ,FDELIVERYDATE FROM SALE_LIST WHERE FID = '{0}'", saleFid);
                DynamicObjectCollection queryResult = DBUtils.ExecuteDynamicObject(ctx, querySql) as DynamicObjectCollection;
                if (queryResult != null && queryResult.Count() > 0)
                {
                    foreach (DynamicObject qResult in queryResult)
                    {
                        mBEntry = new JObject();
                        baseData = new JObject();
                        baseData.Add("FNumber", qResult["FMATERIALNO"].ToString());
                        mBEntry.Add("FMaterialId", baseData);//物料编码
                        mBEntry.Add("FQty", Convert.ToInt32(qResult["FQTY"]));//销售数量
                        baseData = new JObject();
                        baseData.Add("FNumber", qResult["FUNIT"].ToString());
                        mBEntry.Add("FUnitID", baseData);//销售单位
                        mBEntry.Add("FOUTLMTUNIT", "SAL");//超发控制单位
                        mBEntry.Add("FReserveType", "1");//预留类型
                        mBEntry.Add("FDeliveryDate", data["FDATE"].ToString());//要货日期
                        baseData = new JObject();
                        baseData.Add("FNumber", data["FORGNO"].ToString());
                        mBEntry.Add("FSettleOrgIds", baseData);//结算组织
                        entrys.Add(mBEntry);
                    }
                    mBHeader.Add("FSaleOrderEntry", entrys);
                    models.Add(mBHeader);
                }
            }

            jsonRoot.Add("Creator", "");
            jsonRoot.Add("IsDeleteEntry", "True");
            jsonRoot.Add("SubSystemId", "");
            jsonRoot.Add("IsVerifyBaseDataField", "false");
            jsonRoot.Add("BatchCount", "1");
            jsonRoot.Add("Model", models);

            string sFormId = "SAL_SaleOrder";
            string sContent = JsonConvert.SerializeObject(jsonRoot);
            object[] saveInfo = new object[] { sFormId, sContent };

            ApiClient client = new ApiClient(DBHelper.ServerUrl);
            string dbId = DBHelper.DBID;
            bool bLogin = client.Login(dbId, DBHelper.UserName, DBHelper.PassWord, Convert.ToInt32(DBHelper.ICID));
            string result = "";
            if (bLogin)
            {
                result = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.BatchSave", saveInfo);
                JObject jObject = (JObject)JsonConvert.DeserializeObject(result);
                string flag = jObject["Result"]["ResponseStatus"]["IsSuccess"].ToString();
                if ("True".Equals(flag))//销售订单同步成功状态及更新时间更新到中间表
                {
                    string updateSql = string.Format(@"/*dialect*/UPDATE SALE_MAIN@ZyK3Link 
                                                                         SET FFLAG = '1' , FUPDATEDATE  =  '{0}' 
                                                                         WHERE  FID = '{1}'", System.DateTime.Now.ToString(), saleFid);
                    DBUtils.Execute(ctx, updateSql);
                    string updateSql2 = string.Format(@"/*dialect*/UPDATE SALE_MAIN@ZyK3Link
                                                                          SET FFLAG = '1' , FUPDATEDATE  =  '{0}' 
                                                                          WHERE  FID = '{1}'", System.DateTime.Now.ToString(), saleFid);

                    Kingdee.BOS.Log.Logger.Info(DateTime.Now.ToString() + "succWebApi", saleFid.ToString(), true);
                    DBUtils.Execute(ctx, updateSql2);
                }
                else //销售订单同步失败将状态机失败原因更新到中间表
                {
                    string essorMes = jObject["Result"]["ResponseStatus"]["Errors"][0].ToString();
                    string updateSql = string.Format(@"/*dialect*/UPDATE SALE_MAIN@ZyK3Link 
                                                                          SET FFLAG = '2' , FUPDATEDATE  =  '{0}', FERRORMESSAGE  = '{1}' 
                                                                          WHERE  FID = '{2}'", System.DateTime.Now.ToString(), essorMes, saleFid);

                    Kingdee.BOS.Log.Logger.Error(DateTime.Now.ToString() + "errorWebApi", saleFid.ToString() + "同步失败原因：" + essorMes, new Exception());
                    DBUtils.Execute(ctx, updateSql);
                }
            }
            else
            {
                return ResponseResult.Faild("登陆失败！").ToString();
            }
            return result;
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
            foreach (string list in salNoCol)
            {
                salNosList.Add(list);
            }
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
                string updateBomListSql = string.Format(@"/*dialect*/UPDATE BOM_LIST@ZyK3Link SET FFLAG = 1 WHERE FID IN (SELECT FID FROM BOM_MAIN WHERE FSALBILLNO = '{0}' AND FFLAG = 0 AND FDELFLAG = 0)", item);
                string updateBomMainSql = string.Format(@"/*dialect*/UPDATE BOM_MAIN@ZyK3Link SET FFLAG = 1 WHERE FSALBILLNO = '{0}' AND FFLAG = 0 AND FDELFLAG = 0", item);
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
                            string ret = client.Execute<string>("Keeper_Louis.K3.MRP.Interface.PlugIn.Service.MBillSyncService.SyncMBill,Keeper_Louis.K3.MRP.Interface.PlugIn", new object[] { sContent });
                            if (ret.Equals("syncSuccess"))
                            {
                                //改bom同步成功，无需更改,
                            }
                            else
                            {
                                string faildListSql = string.Format(@"/*dialect*/UPDATE BOM_LIST@ZyK3Link SET FFLAG = 2 WHERE FID = {0}", fid);
                                string faildMainSql = string.Format(@"/*dialect*/UPDATE BOM_MAIN@ZyK3Link SET FFLAG = 2,FERRORMESSAGE = '{0}' WHERE FID = {1}", ret,fid);
                                DBUtils.Execute(ctx,faildListSql);
                                DBUtils.Execute(ctx, faildMainSql);
                                salNosList.Remove(item);
                                //for (int k = salNosList.Count - 1; k >= 0; k--)
                                //{
                                //    if (salNosList[k] == item)
                                //        salNosList.Remove(salNosList[i]);
                                //}
                                break;
                                //该bom同步失败，更改主表，子表FFLAG = 2,并更新主表错误信息，根据主表FID进行更新
                            }
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
            mBHeader.Add("FCreateOrgId", baseData);//创建组织
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
            List<string> salNoColOut = new List<string>();
            for (int k=0;k< salNoCol.Count();k++)
            {
                salNoColOut.Add(salNoCol[k]["FSALBILLNO"].ToString());
            }
            for (int i=0;i< salNoCol.Count();i++)
            {
                string FSALBILLNO = salNoCol[i]["FSALBILLNO"].ToString();
                //1.获取需要同步销售订单的数据
                string strSql = string.Format(@"/*dialect*/ select pm.fid,pm.FSALBILLNO,pl.FORGNO,pl.FMATERIALLNO,pl.FMATERIALNAME,pl.FUNIT,pl.FDESCRIPTION,pl.FSTDLTIME 
from PRD_MAIN pm,PRD_LIST pl where pm.FID=pl.fid and pm.FFLAG=0 and pl.FFLAG=0 and pm.FDELFLAG=0 and pm.FSALBILLNO = '{0}' ", FSALBILLNO);
                DynamicObjectCollection queryResult = DBUtils.ExecuteDynamicObject(ctx, strSql) as DynamicObjectCollection;
                if (queryResult != null && queryResult.Count() > 0)
                {
                    //2 更新子表数据状态，更新主表数据状态
                    string updateBomListSql = string.Format(@"/*dialect*/UPDATE PRD_LIST@ZyK3Link SET FFLAG = 1 WHERE FID = (SELECT FID FROM PRD_MAIN@ZyK3Link WHERE FSALBILLNO = '{0}' AND FFLAG = 0 AND FDELFLAG = 0)", FSALBILLNO);
                    string updateBomMainSql = string.Format(@"/*dialect*/UPDATE PRD_MAIN@ZyK3Link SET FFLAG = 1 WHERE FSALBILLNO = '{0}' AND FFLAG = 0 AND FDELFLAG = 0", FSALBILLNO);
                    DBUtils.Execute(ctx, updateBomListSql);
                    DBUtils.Execute(ctx, updateBomMainSql);

                    foreach (DynamicObject data in queryResult)
                    {
                        //3 拼接json对象
                        JArray models = new JArray();//多model批量保存时使用，存储mBHeader
                        JObject mBHeader = new JObject();//model中单据头,存储普通变量、baseData、entrys
                        JObject SubHeadEntity = new JObject();//model中SubHeadEntity
                        JObject SubHeadEntity5 = new JObject();//model中SubHeadEntity5
                        JObject baseData = new JObject();//model中基础资料

                        mBHeader.Add("ServerUrl", DBHelper.ServerUrl);//ServerUrl
                        mBHeader.Add("DBID", DBHelper.DBID);//DBID
                        mBHeader.Add("UserName", DBHelper.UserName);//USERNAME
                        mBHeader.Add("PassWord", DBHelper.PassWord);//PASSWORD;
                        mBHeader.Add("ICID", DBHelper.ICID);//IC
                        mBHeader.Add("FSALBILLNO", data["FSALBILLNO"].ToString());//销售订单号
                        mBHeader.Add("FCreateOrgId", data["FORGNO"].ToString());//申请组织
                        mBHeader.Add("FNumber", data["FMATERIALLNO"].ToString());//物料编码
                        mBHeader.Add("FName", data["FMATERIALNAME"].ToString());//物料名称
                        mBHeader.Add("FDESCRIPTION", data["FDESCRIPTION"].ToString());//规格型号
                        mBHeader.Add("FBaseUnitId", data["FUNIT"].ToString());//基本单位
                        mBHeader.Add("FSTDLTIME", data["FSTDLTIME"].ToString());//标准工时

                        string sContent = JsonConvert.SerializeObject(mBHeader);
                        ApiClient client = new ApiClient(DBHelper.ServerUrl);
                        string dbId = DBHelper.DBID; //AotuTest117
                        bool bLogin = client.Login(dbId, DBHelper.UserName, DBHelper.PassWord, Convert.ToInt32(DBHelper.ICID));

                        if (bLogin)
                        {
                            string ret = client.Execute<string>("Keeper_Louis.K3.MRP.Interface.PlugIn.Service.SyncMaterialBill.SyncMBill,Keeper_Louis.K3.MRP.Interface.PlugIn", new object[] { sContent });
                            if (ret.Equals("syncSuccess"))
                            {
                                //该物料同步成功，无需更改,
                            }
                            else
                            {
                                string faildListSql = string.Format(@"/*dialect*/UPDATE PRD_LIST@ZyK3Link SET FFLAG = 2,FERRORMESSAGE = '{0}' WHERE fid in (select fid from PRD_MAIN@ZyK3Link where  FSALBILLNO = {1})", ret, FSALBILLNO);
                                string faildMainSql = string.Format(@"/*dialect*/UPDATE PRD_MAIN@ZyK3Link SET FFLAG = 2 WHERE FSALBILLNO = {0}",  FSALBILLNO);
                                DBUtils.Execute(ctx, faildListSql);
                                DBUtils.Execute(ctx, faildMainSql);

                                salNoColOut.Remove(FSALBILLNO);
                                break;
                                //物料同步失败，更改主表，子表FFLAG = 2,并更新主表错误信息，根据主表FID进行更新
                            }
                        }
                    }
     
                }
            }
            return salNoColOut;
        }

        
    }
}

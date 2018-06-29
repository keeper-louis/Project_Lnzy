using Kingdee.BOS.WebApi.ServicesStub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Kingdee.BOS.JSON;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Kingdee.BOS.WebApi.Client;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using Kingdee.BOS.Log;

namespace Keeper_Louis.K3.MRP.Interface.PlugIn.Service
{
    [Description("同步物料清单服务")]
    public class MBillSyncService : AbstractWebApiBusinessService
    {

        public MBillSyncService(KDServiceContext context) : base(context) { }

        public string SyncMBill(string parameter)
        {
            JObject jsonRoot = new JObject();//存储models
            //JArray models = new JArray();//多model批量保存时使用，存储mBHeader
            JObject mBHeader = new JObject();//model中单据头,存储普通变量、baseData、entrys
            JObject mBEntry = new JObject();//model中单据体，存储普通变量，baseData
            JObject baseData = new JObject();//model中基础资料
            JArray entrys = new JArray();//单个model中存储多行分录体集合，存储mBentry

            //0 解析parameter Json
            try
            {
                JObject Jo = (JObject)JsonConvert.DeserializeObject(parameter);
                string ServerUrl = Jo["ServerUrl"].ToString();
                string DBID = Jo["DBID"].ToString();
                string UserName = Jo["UserName"].ToString();
                string PassWord = Jo["PassWord"].ToString();
                int ICID = Convert.ToInt32(Jo["ICID"].ToString());
                string FSALBILLNO = Jo["FSALBILLNO"].ToString();//销售单号
                ((JObject)JsonConvert.DeserializeObject(Jo["FCreateOrgId"].ToString()))["FNumber"].ToString();//创建组织
                ((JObject)JsonConvert.DeserializeObject(Jo["FMATERIALID"].ToString()))["FNumber"].ToString();//父物料编号
                ((JObject)JsonConvert.DeserializeObject(Jo["FUNITID"].ToString()))["FNumber"].ToString();//父物料单位
                                                                                                         //单据头
                mBHeader.Add("FID", 0);//FID
                //baseData = new JObject();
                //baseData.Add("FNumber", "102");
                mBHeader.Add("FCreateOrgId", (JObject)JsonConvert.DeserializeObject(Jo["FCreateOrgId"].ToString()));//创建组织
                //baseData = new JObject();
                //baseData.Add("FNumber", "102");
                mBHeader.Add("FUseOrgId", (JObject)JsonConvert.DeserializeObject(Jo["FCreateOrgId"].ToString()));//使用组织
                baseData = new JObject();
                baseData.Add("FNumber", "WLQD01_SYS");
                mBHeader.Add("FBILLTYPE", baseData);//单据类型
                mBHeader.Add("FBOMCATEGORY", "1");//BOM分类
                mBHeader.Add("FBOMUSE", "99");//BOM用途
                //baseData = new JObject();
                //baseData.Add("FNumber", "A01-001");
                mBHeader.Add("FMATERIALID", (JObject)JsonConvert.DeserializeObject(Jo["FMATERIALID"].ToString()));//父物料
                //baseData = new JObject();
                //baseData.Add("FNumber", "Pcs");
                mBHeader.Add("FUNITID", (JObject)JsonConvert.DeserializeObject(Jo["FUNITID"].ToString()));//单位

                JArray Ja = (JArray)JsonConvert.DeserializeObject(Jo["FTreeEntity"].ToString());
                foreach (JObject item in Ja)
                {
                    mBEntry = new JObject();
                    //baseData = new JObject();
                    //baseData.Add("FNumber", "M01-001");
                    mBEntry.Add("FMATERIALIDCHILD", (JObject)JsonConvert.DeserializeObject(item["FMATERIALIDCHILD"].ToString()));//子物料
                    mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
                    //baseData = new JObject();
                    //baseData.Add("FNumber", "kg");
                    mBEntry.Add("FCHILDUNITID", (JObject)JsonConvert.DeserializeObject(item["FCHILDUNITID"].ToString()));//子物料单位
                    mBEntry.Add("FDOSAGETYPE", "2");//用量类型
                    mBEntry.Add("FNUMERATOR", Convert.ToDouble(item["FNUMERATOR"].ToString()));//用量：分子
                    mBEntry.Add("FDENOMINATOR", Convert.ToDouble(item["FDENOMINATOR"].ToString()));//用量：分母
                    mBEntry.Add("FOverControlMode", "2");//超发控制方式
                    mBEntry.Add("FEntrySource", "1");//子项来源
                    mBEntry.Add("FEFFECTDATE", Convert.ToString(item["FEFFECTDATE"].ToString()));//生效日期
                    mBEntry.Add("FEXPIREDATE", Convert.ToString(item["FEXPIREDATE"].ToString()));//失效日期
                    mBEntry.Add("FISSUETYPE", "1");//发料方式
                    mBEntry.Add("FTIMEUNIT", "1");//时间单位
                    mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型111
                    entrys.Add(mBEntry);

                }
                mBHeader.Add("FTreeEntity", entrys);
                #region 弃用
                /*
                     1、拼接json对象
                     2、执行标准bom清单保存
                */
                //单据体
                //mBEntry = new JObject();
                //baseData = new JObject();
                //baseData.Add("FNumber", "M01-001");
                //mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
                //mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
                //baseData = new JObject();
                //baseData.Add("FNumber", "kg");
                //mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
                //mBEntry.Add("FDOSAGETYPE", "2");//用量类型
                //mBEntry.Add("FNUMERATOR", 12);//用量：分子
                //mBEntry.Add("FDENOMINATOR", 1);//用量：分母
                //mBEntry.Add("FOverControlMode", "2");//超发控制方式
                //mBEntry.Add("FEntrySource", "1");//子项来源
                //mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
                //mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
                //mBEntry.Add("FISSUETYPE", "1");//发料方式
                //mBEntry.Add("FTIMEUNIT", "1");//时间单位
                //mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
                //entrys.Add(mBEntry);

                //mBEntry = new JObject();
                //baseData = new JObject();
                //baseData.Add("FNumber", "M02-001");
                //mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
                //mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
                //baseData = new JObject();
                //baseData.Add("FNumber", "Pcs");
                //mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
                //mBEntry.Add("FDOSAGETYPE", "2");//用量类型
                //mBEntry.Add("FNUMERATOR", 2);//用量：分子
                //mBEntry.Add("FDENOMINATOR", 1);//用量：分母
                //mBEntry.Add("FOverControlMode", "2");//超发控制方式
                //mBEntry.Add("FEntrySource", "1");//子项来源
                //mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
                //mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
                //mBEntry.Add("FISSUETYPE", "1");//发料方式
                //mBEntry.Add("FTIMEUNIT", "1");//时间单位
                //mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
                //entrys.Add(mBEntry);

                //mBEntry = new JObject();
                //baseData = new JObject();
                //baseData.Add("FNumber", "M03-001");
                //mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
                //mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
                //baseData = new JObject();
                //baseData.Add("FNumber", "m");
                //mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
                //mBEntry.Add("FDOSAGETYPE", "2");//用量类型
                //mBEntry.Add("FNUMERATOR", 860);//用量：分子
                //mBEntry.Add("FDENOMINATOR", 1);//用量：分母
                //mBEntry.Add("FOverControlMode", "2");//超发控制方式
                //mBEntry.Add("FEntrySource", "1");//子项来源
                //mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
                //mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
                //mBEntry.Add("FISSUETYPE", "1");//发料方式
                //mBEntry.Add("FTIMEUNIT", "1");//时间单位
                //mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
                //entrys.Add(mBEntry);

                //mBEntry = new JObject();
                //baseData = new JObject();
                //baseData.Add("FNumber", "M04-001");
                //mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
                //mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
                //baseData = new JObject();
                //baseData.Add("FNumber", "Pcs");
                //mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
                //mBEntry.Add("FDOSAGETYPE", "2");//用量类型
                //mBEntry.Add("FNUMERATOR", 4);//用量：分子
                //mBEntry.Add("FDENOMINATOR", 1);//用量：分母
                //mBEntry.Add("FOverControlMode", "2");//超发控制方式
                //mBEntry.Add("FEntrySource", "1");//子项来源
                //mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
                //mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
                //mBEntry.Add("FISSUETYPE", "1");//发料方式
                //mBEntry.Add("FTIMEUNIT", "1");//时间单位
                //mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
                //entrys.Add(mBEntry);

                //mBEntry = new JObject();
                //baseData = new JObject();
                //baseData.Add("FNumber", "M04-002");
                //mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
                //mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
                //baseData = new JObject();
                //baseData.Add("FNumber", "Pcs");
                //mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
                //mBEntry.Add("FDOSAGETYPE", "2");//用量类型
                //mBEntry.Add("FNUMERATOR", 16);//用量：分子
                //mBEntry.Add("FDENOMINATOR", 1);//用量：分母
                //mBEntry.Add("FOverControlMode", "2");//超发控制方式
                //mBEntry.Add("FEntrySource", "1");//子项来源
                //mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
                //mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
                //mBEntry.Add("FISSUETYPE", "1");//发料方式
                //mBEntry.Add("FTIMEUNIT", "1");//时间单位
                //mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
                //entrys.Add(mBEntry);

                //mBHeader.Add("FTreeEntity",entrys);
                //models.Add(mBHeader);
                #endregion


                jsonRoot.Add("Creator", "");
                jsonRoot.Add("IsDeleteEntry", "True");
                jsonRoot.Add("SubSystemId", "");
                jsonRoot.Add("IsVerifyBaseDataField", "true");
                jsonRoot.Add("Model", mBHeader);


                string sFormId = "ENG_BOM";
                string sContent = JsonConvert.SerializeObject(jsonRoot);
                object[] saveInfo = new object[] { sFormId, sContent };


                ApiClient client = new ApiClient(ServerUrl);
                bool bLogin = client.Login(DBID, UserName, PassWord, ICID);
                if (bLogin)
                {
                    var ret = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Save", saveInfo);
                    JObject sResult = JObject.Parse(ret);
                    JObject saveStatus = sResult["Result"]["ResponseStatus"] as JObject;
                    if (saveStatus["IsSuccess"].ToString().Equals("True"))
                    {
                        //将保存成功信息写入日志ret
                        Logger.Info("saveSuccess:", ret);

                        JArray successEntity = JArray.Parse(saveStatus["SuccessEntitys"].ToString());
                        JObject jo = new JObject();
                        jo.Add("Ids", successEntity[0]["Id"]);
                        //jo.Add("Numbers", successEntity[0]["Number"]);
                        string submitJson = JsonConvert.SerializeObject(jo);
                        var submitResult = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Submit",
                        new object[] { "ENG_BOM", submitJson });
                        JObject bResult = JObject.Parse(submitResult);
                        JObject submitStatus = bResult["Result"]["ResponseStatus"] as JObject;
                        if (submitStatus["IsSuccess"].ToString().Equals("True"))
                        {
                            //将提交成功信息写入日志submitResult
                            Logger.Info("submitSuccess:", submitResult);

                            JArray succEntity = JArray.Parse(saveStatus["SuccessEntitys"].ToString());
                            JObject joi = new JObject();
                            joi.Add("Ids", succEntity[0]["Id"]);
                            //jo.Add("Numbers", successEntity[0]["Number"]);
                            string auditJson = JsonConvert.SerializeObject(joi);
                            var auditResult = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Audit",
                            new object[] { "ENG_BOM", auditJson });
                            JObject aResult = JObject.Parse(auditResult);
                            JObject audittStatus = aResult["Result"]["ResponseStatus"] as JObject;
                            if (audittStatus["IsSuccess"].ToString().Equals("True"))
                            {
                                //将审核成功信息写入日志auditResult
                                Logger.Info("auditSuccess:", auditResult);
                                return "syncSuccess";
                            }
                            else
                            {
                                JArray audit_errors_Entity = JArray.Parse(audittStatus["Errors"].ToString());
                                //将审核失败信息写入日志auditResult
                                Logger.Error("auditFaild:", auditResult, null);
                                //返回审核失败信息
                                return audit_errors_Entity[0]["FieldName"].ToString() + audit_errors_Entity[0]["Message"].ToString();
                            }
                            
                        }
                        else
                        {
                            JArray submit_errors_Entity = JArray.Parse(submitStatus["Errors"].ToString());
                            //将错误信息写入日志submitResult
                            Logger.Error("submitFaild:", submitResult, null);
                            //返回错误信息
                            return submit_errors_Entity[0]["FieldName"].ToString() + submit_errors_Entity[0]["Message"].ToString();
                        }

                    }
                    else
                    {
                        JArray save_errors_Entity = JArray.Parse(saveStatus["Errors"].ToString());
                        //将错误信息写入日志ret
                        Logger.Error("saveFaild:", ret, null);
                        //返回错误信息
                        return save_errors_Entity[0]["FieldName"].ToString() + save_errors_Entity[0]["Message"].ToString();
                    }
                    
                }
                else
                {
                    //将错误信息写到日志
                    Logger.Error("Login:", "登录失败", null);
                    //返回错误信息
                    return returnJsonError("Login", "登录失败");
                }
            }
            catch (Exception ex)
            {
                //将异常写到日志
                Logger.Error("exception:", ex.Message, null);
                //返回错误信息
                return returnJsonError("捕获异常：", ex.Message);
            }

        }
        string returnJsonError(string fieldName, string message)
        {
            return "{\"Result\":{\"ResponseStatus\":{\"ErrorCode\":500,\"IsSuccess\":false,\"Errors\":[{\"FieldName\":\"" + fieldName + "\",\"Message\":\"" + message + "\"}]}}}";
        }

        string returnJsonSuccess()
        {
            return "{\"Result\":{\"ResponseStatus\":{\"ErrorCode\":0,\"IsSuccess\":true,\"Errors\":[]}}}";
        }
    }
}

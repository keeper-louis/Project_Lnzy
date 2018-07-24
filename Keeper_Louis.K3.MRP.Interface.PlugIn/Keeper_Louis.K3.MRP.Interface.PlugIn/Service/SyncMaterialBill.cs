using Kingdee.BOS;
using Kingdee.BOS.App.Data;
using Kingdee.BOS.Core.DynamicForm;
using Kingdee.BOS.Core.DynamicForm.PlugIn;
using Kingdee.BOS.Core.Metadata;
using Kingdee.BOS.Core.Validation;
using Kingdee.BOS.Log;
using Kingdee.BOS.Orm;
using Kingdee.BOS.Orm.DataEntity;
using Kingdee.BOS.ServiceHelper;
using Kingdee.BOS.WebApi.Client;
using Kingdee.BOS.WebApi.ServicesStub;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.ServiceFacade.KDServiceFx;

namespace Keeper_Louis.K3.MRP.Interface.PlugIn.Service
{
        public class SyncMaterialBill : AbstractWebApiBusinessService
    {
        public SyncMaterialBill(KDServiceContext context) : base(context)
        {
        }
        public string SyncMBill(string parameter)
        {
            JObject jsonRoot = new JObject();//存储models
            //JArray models = new JArray();//多model批量保存时使用，存储mBHeader
            JObject mBHeader = new JObject();//model中单据头,存储普通变量、baseData、entrys
            JObject SubHeadEntity = new JObject();//model中SubHeadEntity
            JObject SubHeadEntity5 = new JObject();//model中SubHeadEntity5
            JObject baseData = new JObject();//model中基础资料

            JObject Jo = (JObject)JsonConvert.DeserializeObject(parameter);
            string ServerUrl = Jo["ServerUrl"].ToString();
            string DBID = Jo["DBID"].ToString();
            string UserName = Jo["UserName"].ToString();
            string PassWord = Jo["PassWord"].ToString();
            int ICID = Convert.ToInt32(Jo["ICID"].ToString());
            baseData.Add("FNumber", Jo["FCreateOrgId"].ToString());
            mBHeader.Add("FCreateOrgId", baseData);//创建组织
            baseData = new JObject();
            baseData.Add("FNumber", Jo["FCreateOrgId"].ToString());
            mBHeader.Add("FUseOrgId", baseData);//使用组织
            mBHeader.Add("FNumber", Jo["FNumber"].ToString());//物料编码
            mBHeader.Add("FName", Jo["FName"].ToString());//物料名称
            mBHeader.Add("FSpecification", Jo["FDESCRIPTION"].ToString());//规格型号
            mBHeader.Add("FSALBILLNO", Jo["FSALBILLNO"].ToString());//销售订单号
            baseData = new JObject();
            baseData.Add("FNumber", Jo["FBaseUnitId"].ToString());
            //baseData.Add("FNumber","Pcs");
            SubHeadEntity.Add("FBaseUnitId", baseData);//基本单位
            string FCategoryID = string.Empty;
            string FErpClsID= string.Empty;
            if (Jo["FNumber"].ToString().Substring(0,2).Equals("01"))
            {
                FCategoryID = "01";
                FErpClsID = "9";
            }
            if (Jo["FNumber"].ToString().Substring(0, 2).Equals("02"))
            {
                FCategoryID = "02";
                FErpClsID = "2";
            }
            if (Jo["FNumber"].ToString().Substring(0, 2).Equals("03"))
            {
                FCategoryID = "03";
                FErpClsID = "1";
            }
            baseData = new JObject();
            baseData.Add("FNumber", FCategoryID);//存货类别
            SubHeadEntity.Add("FCategoryID", baseData);
            SubHeadEntity.Add("FErpClsID", FErpClsID);//物料属性
            SubHeadEntity.Add("FIsPurchase", "true");
            SubHeadEntity.Add("FIsInventory", "true");
            SubHeadEntity.Add("FIsSubContract", "true");
            SubHeadEntity.Add("FIsSale", "true");
            SubHeadEntity.Add("FIsProduce", "true");
            SubHeadEntity.Add("FIsAsset", "true");

            mBHeader.Add("SubHeadEntity", SubHeadEntity);
            SubHeadEntity5.Add("FIsMainPrd", "true");
            SubHeadEntity5.Add("FStdLaborPrePareTime",  Jo["FSTDLTIME"].ToString());//标准工时

            baseData = new JObject();
            baseData.Add("FNumber", Jo["FBaseUnitId"].ToString());
            //baseData.Add("FNumber", "Pcs");
            SubHeadEntity5.Add("FMinIssueUnitId", baseData);//最小发料批量单位
            

            mBHeader.Add("SubHeadEntity5", SubHeadEntity5);
            jsonRoot.Add("Model", mBHeader);

            string sFormId = "BD_MATERIAL";
            string sContent = JsonConvert.SerializeObject(jsonRoot);
            object[] saveInfo = new object[] { sFormId, sContent };

            ApiClient client = new ApiClient(ServerUrl);
            bool bLogin = client.Login(DBID, UserName, PassWord, ICID);
            if (bLogin)//登录成功
            {
                var ret = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Save", saveInfo);
                JObject sResult = JObject.Parse(ret);
                JObject saveStatus = sResult["Result"]["ResponseStatus"] as JObject;
                if (saveStatus["IsSuccess"].ToString().Equals("True"))//保存成功
                {
                    //将保存成功信息写入日志ret
                    Logger.Info("saveSuccess:", ret);

                    JArray successEntity = JArray.Parse(saveStatus["SuccessEntitys"].ToString());
                    JObject jo = new JObject();
                    jo.Add("Ids", successEntity[0]["Id"]);
                    //jo.Add("Numbers", successEntity[0]["Number"]);
                    string submitJson = JsonConvert.SerializeObject(jo);
                    var submitResult = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Submit",
                    new object[] { "BD_MATERIAL", submitJson });
                    JObject bResult = JObject.Parse(submitResult);
                    JObject submitStatus = bResult["Result"]["ResponseStatus"] as JObject;

                    if (submitStatus["IsSuccess"].ToString().Equals("True"))//提交成功
                    {
                        //将提交成功信息写入日志submitResult
                        Logger.Info("submitSuccess:", submitResult);

                        JArray succEntity = JArray.Parse(saveStatus["SuccessEntitys"].ToString());
                        JObject joi = new JObject();
                        joi.Add("Ids", succEntity[0]["Id"]);
                        //jo.Add("Numbers", successEntity[0]["Number"]);
                        string auditJson = JsonConvert.SerializeObject(joi);
                        var auditResult = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.Audit",
                        new object[] { "BD_MATERIAL", auditJson });
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
                    else//提交失败
                    {
                        JArray submit_errors_Entity = JArray.Parse(submitStatus["Errors"].ToString());
                        //将错误信息写入日志submitResult
                        Logger.Error("submitFaild:", submitResult, null);
                        //返回错误信息
                        return submit_errors_Entity[0]["FieldName"].ToString() + submit_errors_Entity[0]["Message"].ToString();
                    }
                }
                else//保存失败
                {
                    JArray save_errors_Entity = JArray.Parse(saveStatus["Errors"].ToString());
                    //将错误信息写入日志ret
                    Logger.Error("saveFaild:", ret, null);
                    //返回错误信息
                    return save_errors_Entity[0]["FieldName"].ToString() + save_errors_Entity[0]["Message"].ToString();
                }
            }
            else//登录失败
            {
                //将错误信息写到日志
                Logger.Error("Login:", "登录失败", null);
                //返回错误信息
                return returnJsonError("Login", "登录失败");
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

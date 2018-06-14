using Kingdee.BOS.WebApi.ServicesStub;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kingdee.BOS.ServiceFacade.KDServiceFx;
using System.ComponentModel;
using Kingdee.BOS.JSON;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Kingdee.BOS.WebApi.Client;

namespace Keeper_Louis.K3.MRP.Interface.PlugIn.Service
{
    [Description("同步物料清单服务")]
    public class MBillSyncService : AbstractWebApiBusinessService
    {

        public MBillSyncService(KDServiceContext context) : base(context) { }

        public string SyncMBill(string parameter)
        {
            /*
                 1、拼接json对象
                 2、执行标准bom清单保存
            */
            JObject jsonRoot = new JObject();//存储models
            JArray models = new JArray();//多model批量保存时使用，存储mBHeader
            JObject mBHeader = new JObject();//model中单据头,存储普通变量、baseData、entrys
            JObject mBEntry = new JObject();//model中单据体，存储普通变量，baseData
            JObject baseData = new JObject();//model中基础资料
            JArray entrys = new JArray();//单个model中存储多行分录体集合，存储mBentry

            mBHeader.Add("FID",0);//FID
            baseData = new JObject();
            baseData.Add("FNumber", "102");
            mBHeader.Add("FCreateOrgId", baseData);//创建组织
            baseData = new JObject();
            baseData.Add("FNumber","102");
            mBHeader.Add("FUseOrgId", baseData);//使用组织
            baseData = new JObject();
            baseData.Add("FNumber", "WLQD01_SYS");
            mBHeader.Add("FBILLTYPE",baseData);//单据类型
            mBHeader.Add("FBOMCATEGORY", "1");//BOM分类
            mBHeader.Add("FBOMUSE", "99");//BOM用途
            baseData = new JObject();
            baseData.Add("FNumber", "A01-001");
            mBHeader.Add("FMATERIALID", baseData);//父物料
            baseData = new JObject();
            baseData.Add("FNumber", "Pcs");
            mBHeader.Add("FUNITID", baseData);//单位


            //单据体
            mBEntry = new JObject();
            baseData = new JObject();
            baseData.Add("FNumber", "M01-001");
            mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
            mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
            baseData = new JObject();
            baseData.Add("FNumber", "kg");
            mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
            mBEntry.Add("FDOSAGETYPE", "2");//用量类型
            mBEntry.Add("FNUMERATOR", 12);//用量：分子
            mBEntry.Add("FDENOMINATOR", 1);//用量：分母
            mBEntry.Add("FOverControlMode", "2");//超发控制方式
            mBEntry.Add("FEntrySource", "1");//子项来源
            mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
            mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
            mBEntry.Add("FISSUETYPE", "1");//发料方式
            mBEntry.Add("FTIMEUNIT", "1");//时间单位
            mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
            entrys.Add(mBEntry);

            mBEntry = new JObject();
            baseData = new JObject();
            baseData.Add("FNumber", "M02-001");
            mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
            mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
            baseData = new JObject();
            baseData.Add("FNumber", "Pcs");
            mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
            mBEntry.Add("FDOSAGETYPE", "2");//用量类型
            mBEntry.Add("FNUMERATOR", 2);//用量：分子
            mBEntry.Add("FDENOMINATOR", 1);//用量：分母
            mBEntry.Add("FOverControlMode", "2");//超发控制方式
            mBEntry.Add("FEntrySource", "1");//子项来源
            mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
            mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
            mBEntry.Add("FISSUETYPE", "1");//发料方式
            mBEntry.Add("FTIMEUNIT", "1");//时间单位
            mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
            entrys.Add(mBEntry);

            mBEntry = new JObject();
            baseData = new JObject();
            baseData.Add("FNumber", "M03-001");
            mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
            mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
            baseData = new JObject();
            baseData.Add("FNumber", "m");
            mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
            mBEntry.Add("FDOSAGETYPE", "2");//用量类型
            mBEntry.Add("FNUMERATOR", 860);//用量：分子
            mBEntry.Add("FDENOMINATOR", 1);//用量：分母
            mBEntry.Add("FOverControlMode", "2");//超发控制方式
            mBEntry.Add("FEntrySource", "1");//子项来源
            mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
            mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
            mBEntry.Add("FISSUETYPE", "1");//发料方式
            mBEntry.Add("FTIMEUNIT", "1");//时间单位
            mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
            entrys.Add(mBEntry);

            mBEntry = new JObject();
            baseData = new JObject();
            baseData.Add("FNumber", "M04-001");
            mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
            mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
            baseData = new JObject();
            baseData.Add("FNumber", "Pcs");
            mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
            mBEntry.Add("FDOSAGETYPE", "2");//用量类型
            mBEntry.Add("FNUMERATOR", 4);//用量：分子
            mBEntry.Add("FDENOMINATOR", 1);//用量：分母
            mBEntry.Add("FOverControlMode", "2");//超发控制方式
            mBEntry.Add("FEntrySource", "1");//子项来源
            mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
            mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
            mBEntry.Add("FISSUETYPE", "1");//发料方式
            mBEntry.Add("FTIMEUNIT", "1");//时间单位
            mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
            entrys.Add(mBEntry);

            mBEntry = new JObject();
            baseData = new JObject();
            baseData.Add("FNumber", "M04-002");
            mBEntry.Add("FMATERIALIDCHILD", baseData);//子物料
            mBEntry.Add("FMATERIALTYPE", "1");//子物料类型
            baseData = new JObject();
            baseData.Add("FNumber", "Pcs");
            mBEntry.Add("FCHILDUNITID", baseData);//子物料单位
            mBEntry.Add("FDOSAGETYPE", "2");//用量类型
            mBEntry.Add("FNUMERATOR", 16);//用量：分子
            mBEntry.Add("FDENOMINATOR", 1);//用量：分母
            mBEntry.Add("FOverControlMode", "2");//超发控制方式
            mBEntry.Add("FEntrySource", "1");//子项来源
            mBEntry.Add("FEFFECTDATE", "2018-05-24 00:00:00");//生效日期
            mBEntry.Add("FEXPIREDATE", "9999-12-31 00:00:00");//失效日期
            mBEntry.Add("FISSUETYPE", "1");//发料方式
            mBEntry.Add("FTIMEUNIT", "1");//时间单位
            mBEntry.Add("FOWNERTYPEID", "BD_OwnerOrg");//货主类型
            entrys.Add(mBEntry);

            mBHeader.Add("FTreeEntity",entrys);
            models.Add(mBHeader);


            jsonRoot.Add("Creator","");
            jsonRoot.Add("IsDeleteEntry", "True");
            jsonRoot.Add("SubSystemId","");
            jsonRoot.Add("IsVerifyBaseDataField", "false");
            jsonRoot.Add("BatchCount","1");
            jsonRoot.Add("Model",models);


            string sFormId = "ENG_BOM";
            string sContent = JsonConvert.SerializeObject(jsonRoot);
            object[] saveInfo = new object[] { sFormId,sContent};


            ApiClient client = new ApiClient("http://127.0.0.1/k3cloud/");
            string dbId = "5acc6bdcca8cce"; //AotuTest117
            bool bLogin = client.Login(dbId, "Administrator", "888888", 2052);
            if (bLogin)
            {
                var ret = client.Execute<string>("Kingdee.BOS.WebApi.ServicesStub.DynamicFormService.BatchSave", saveInfo);
            }

            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keeper_Louis.K3.MRP.Interface.PlugIn.BaseModel
{
    public class ResponseResult
    {
        public ResponseResult()
        {
        }

        /// <summary>
        /// 状态
        /// </summary>
        public int RequestStatus
        {
            get;
            set;
        }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Msg
        {
            get;
            set;
        }

        public static ResponseResult Default()
        {
            var result = new ResponseResult();
            result.RequestStatus = (int)ResponseResultStatus.Default;
            result.Msg = "";
            return result;
        }

        public static ResponseResult Success(string message = null)
        {
            var result = new ResponseResult();
            result.RequestStatus = (int)ResponseResultStatus.Succeed;
            result.Msg = message;
            return result;
        }

        public static ResponseResult Exception(string message)
        {
            var result = new ResponseResult();
            result.RequestStatus = (int)ResponseResultStatus.Exception;
            result.Msg = message;
            return result;
        }

        public static ResponseResult Faild(string message)
        {
            var result = new ResponseResult();
            result.RequestStatus = (int)ResponseResultStatus.Faild;
            result.Msg = message;
            return result;
        }

        public static ResponseResult NotAuthorization(string message)
        {
            var result = new ResponseResult();
            result.RequestStatus = (int)ResponseResultStatus.NotAuthorization;
            result.Msg = message;
            return result;
        }

    }

    public enum ResponseResultStatus
    {
        Default = 0,
        Succeed = 100,
        Faild = 101,
        Exception = 102,
        NotAuthorization = 403
    }
}

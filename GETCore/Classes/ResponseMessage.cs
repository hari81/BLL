using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Classes
{
    public class GETResponseMessage
    {
        public ResponseTypes responseType { get; set; }
        public string message { get; set;}

        public GETResponseMessage(ResponseTypes responseType, string message)
        {
            this.responseType = responseType;
            this.message = message;
        }
    }

    public enum ResponseTypes
    {
        Success = 1,
        InvalidInputs = 2,
        Failed = 3
    }
}
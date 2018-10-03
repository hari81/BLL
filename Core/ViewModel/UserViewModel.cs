using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ViewModel
{
    public class UserViewModel
    {
        public int UserId { get; set; } = 0;
        public string AspNetUserId { get; set; } = "";
        public string Name { get; set; } = "Unknown";
    }
}
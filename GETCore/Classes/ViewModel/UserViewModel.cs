using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Classes.ViewModel
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public UserAccessTypes AccessLevel { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Classes
{
    public enum UserAccessTypes
    {
        GlobalAdministrator = 1,
        DealershipAdministrator = 2,
        DealershipUser = 3,
        CustomerAdministrator = 4,
        CustomerUser = 5,
        EquipmentUser = 6,
        Unknown = 7
    }
}
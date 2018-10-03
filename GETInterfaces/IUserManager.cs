using BLL.GETCore.Classes.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.GETInterfaces
{
    public interface IUserManager
    {
        Task<Tuple<bool, UserViewModel>> GetUser(int userId, System.Security.Principal.IPrincipal User);
    }
}
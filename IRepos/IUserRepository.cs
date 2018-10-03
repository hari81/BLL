using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Interfaces;
namespace BLL.Core.Repositories
{
    public interface IUserRepository:IRepository<IUser>
    {
        IUser GetUserById(int id);
    }

}

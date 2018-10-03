using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Interfaces
{
    public interface IUser
    {
        int Id { get; set; }
        string userStrId { get; set; }
        string userName { get; set; }
    }

    public interface IUserService
    {

    }
}

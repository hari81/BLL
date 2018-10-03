using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Core.Domain;

namespace BLL.Core.Repositories
{
    interface IUCSystemRepository : IRepository<UCSystem>
    {
        // By updating a system life all the connected components to this system will be updated.
        bool UpdateSystemLife(int id, int NewLife, int UserId, ActionType TypeOfAction, int ActionId, DateTime date);
        int GetSystemLife(int id);
    }
}

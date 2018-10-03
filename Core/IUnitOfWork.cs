using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Core.Repositories;

namespace BLL.Core
{
    interface IUnitOfWork : IDisposable
    {
        IComponentRepository Components { get; }
        IUserRepository Users { get; }
        IEquipmentRepository Equipments { get; }
        int Complete();
    }
}

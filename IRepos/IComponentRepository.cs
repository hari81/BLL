using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Core.Domain;
using BLL.Interfaces;
using DAL;
namespace BLL.Core.Repositories
{
    public interface IComponentRepository:IRepository<GeneralComponent>
    {
        GeneralComponent GetComponentWithId(int id);
    }
}

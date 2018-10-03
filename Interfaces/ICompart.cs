using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Core.Domain;
using DAL;
namespace BLL.Interfaces
{
    public interface ICompart
    {
        int Id { get; }
        LU_COMPART DALCompart { get; set; }
        LU_COMPART_TYPE DALType { get; set; }
        int DefaultBudgetLife { get; set; }
        List<LU_COMPART> getChildComparts();
    }
}

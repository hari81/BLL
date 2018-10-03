using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;
namespace BLL.Interfaces
{
    public interface IUCSystem
    {
        int Id { get; }
        EQUIPMENT DALEquipment { get; set; }
        LU_Module_Sub DALSystem { get; set; }
        int SystemLatestLife { get; }
        int GetSystemLife(DateTime date);
        IList<GENERAL_EQ_UNIT> Components { get; set; }
    }
}

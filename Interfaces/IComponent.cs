using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Interfaces;
using BLL.Core.Domain;
using BLL.Core.Repositories;
using DAL;

namespace BLL.Interfaces
{
    public interface IComponent
    {
        int Id { get;}
        string Message { get; set; }
        EQUIPMENT DALEquipment { get; set; }
        LU_Module_Sub DALSystem { get; set; }
        GENERAL_EQ_UNIT DALComponent { get; set; }
        ICompart Compart { get; set; }
        int ComponentLatestLife { get; }
        int GetComponentLife(DateTime date);
        decimal CalcWornPercentage(decimal reading, int toolId, InspectionImpact? impact);
        bool GetEvalCodeByWorn(decimal worn, out char result);
    }

}

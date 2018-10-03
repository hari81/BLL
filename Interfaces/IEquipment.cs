using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Core.Domain;
using BLL.Core.Repositories;
using DAL;
namespace BLL.Interfaces
{
    public interface IEquipment
    {
        int Id { get;}
        EQUIPMENT DALEquipment { get; set; }
        int LatestSerialMeterUnit { get;}
        int EquipmentLatestLife { get; }
        int GetEquipmentLife(DateTime date);
        int GetSerialMeterUnit(DateTime date);
        IList<LU_Module_Sub> DALSystems { get; set; }
        //IList<GENERAL_EQ_UNIT> DALComponents { get; set; }
    }
    public interface IEquipmentService
    {
    }
    public interface IEquipmentActionRecord
    {
        int Id { get; set; }//Coming from ACTION_TAKEN_HISTORY
        int EquipmentId { get; set; }
        int GETHistoryId { get; set; }
        int ReadSmuNumber { get; set; }
        int EquipmentActualLife { get; set; }
        ActionType TypeOfAction { get; set; }
        decimal Cost { get; set; }
        IUser ActionUser { get; set; }
        DateTime ActionDate { get; set; }
        string Comment { get; set; }
        decimal PartsCost { get; set; }
        decimal LabourCost { get; set; }
        decimal MiscCost { get; set; }
    }
}

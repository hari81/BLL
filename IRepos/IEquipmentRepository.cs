using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Core.Domain;
using BLL.Interfaces;
namespace BLL.Core.Repositories
{
    public interface IEquipmentRepository:IRepository<Equipment>
    {
        // By updating EquipmentLife all the systems and components on this equipment will be updated
        IEquipmentActionRecord UpdateEquipmentByAction(IEquipmentActionRecord actionRecord, ref string OperationResult);
        bool UpdateEquipmentLife(int id, int ReadSmuNumber, int UserId, int ActionId, DateTime ActionDate, ref string OperationResult);
        bool ResetMeterUnit(int Id, int ReadSmuNumber, int UserId, ActionType TypeOfAction, DateTime date);
        int GetEquipmentLife(int Id, DateTime date);
        int GetEquipmentSerialMeterUnit(int Id, DateTime date);
        int GetSystemLife(int Id, DateTime date);
        int GetComponentLife(int Id, DateTime date);
    }
}

using System;
using System.Linq;
using BLL.Interfaces;
using BLL.Core.Domain;
using System.Data.Entity;
using DAL;
using BLL.Extensions;

namespace BLL.Core.Repositories
{
    /// <summary>
    /// Installs a component on a system which is on an equipment!
    /// cannot install component if system is in inventory!
    /// </summary>
    public class InstallComponentOnSystemAction : Domain.Action, IAction
    {
        private IEquipmentActionRecord pvActionRecord;
        private ActionStatus pvActionStatus;
        private string pvActionLog;
        private string pvMessage;
        private int pvUniqueId;
        public string ActionLog
        {
            get { return pvActionLog; }
            private set { pvActionLog = value; }
        }

        public ActionStatus Status
        {
            get { return pvActionStatus; }
            private set { pvActionStatus = value; }
        }

        public int UniqueId
        {
            get { return pvUniqueId; }
            private set { pvUniqueId = value; }
        }

        public IEquipmentActionRecord _actionRecord
        {
            get { return pvActionRecord; }
            private set { pvActionRecord = value; }
        }

        public new string Message
        {
            get { return pvMessage; }
            private set { pvMessage = value; }
        }
        private InstallComponentOnSystemParams Params;
        private UCSystem _Logicalsystem;
        private Component _Logicalcomponent;
        private Equipment _Logicalequipment;
        public InstallComponentOnSystemAction(DbContext context, IEquipmentActionRecord actionRecord, InstallComponentOnSystemParams Parameters)
            : base(context)
        {
            Params = Parameters;
            _actionRecord = actionRecord;
            Status = ActionStatus.Close;
        }

        public ActionStatus Start()
        {
            if (Status == ActionStatus.Close)
            {
                string log = "";
                _actionRecord = UpdateEquipmentByAction(_actionRecord, ref log);
                ActionLog += log;
                Message = base.Message;
                if (_actionRecord != null && _actionRecord.Id != 0)
                {
                    _Logicalsystem = new UCSystem(_context, Params.SystemId);
                    _Logicalcomponent = new Component(_context, Params.Id);
                    Status = ActionStatus.Started;
                    Message = "Started successfully";
                }
                else
                {
                    Message = "Cannot Start the action!";
                    Status = ActionStatus.Close;
                    return Status;
                }
            }
            return Status;
        }

        public ActionStatus Validate()
        {
            ActionLog += "Starting validation ..." + Environment.NewLine;
            if (Status == ActionStatus.Started)
            {
                if (_Logicalcomponent == null || _Logicalcomponent.Id == 0 || _Logicalcomponent.DALComponent == null)
                {
                    ActionLog += "Component not found!";
                    Message = "Component not found! Installing component on the system failed";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                if (_Logicalsystem == null || _Logicalsystem.Id == 0 || _Logicalsystem.DALSystem == null)
                {
                    ActionLog += "System not found!";
                    Message = "System not found! Installing component on the system failed";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                if (_Logicalsystem.DALSystem.equipmentid_auto == null || _Logicalsystem.DALSystem.EQUIPMENT == null)
                {
                    ActionLog += "System is not installed on an equipment!";
                    Message = "Operation is not valid! System is not installed on an equipment!";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                _Logicalequipment = new Equipment(_context, longNullableToint(_Logicalsystem.DALSystem.equipmentid_auto));
                if (_Logicalequipment.Id == 0 || _Logicalequipment.DALEquipment == null)
                {
                    ActionLog += "Equipment not found!";
                    Message = "Equipment not found! Installing component on the system failed";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                ActionLog += "Validation completed." + Environment.NewLine;
                Message = "Operation is valid.";
                Status = ActionStatus.Valid;
            }
            return Status;
        }
        public ActionStatus Commit()
        {
            ActionLog += "Commiting the Operation ..." + Environment.NewLine;
            if (Status != ActionStatus.Valid)
            {
                ActionLog += "Operation Status is not valid" + Environment.NewLine;
                Status = ActionStatus.Failed;
                return Status;
            }
            _Logicalcomponent.DALComponent.date_installed = _actionRecord.ActionDate;
            _Logicalcomponent.DALComponent.smu_at_install = _actionRecord.ReadSmuNumber;
            _Logicalcomponent.DALComponent.eq_smu_at_install = _actionRecord.ReadSmuNumber;
            _Logicalcomponent.DALComponent.pos = Params.Position;
            _Logicalcomponent.DALComponent.side = (byte)Params.side;
            _Logicalcomponent.DALComponent.eq_ltd_at_install = _actionRecord.EquipmentActualLife;
            _Logicalcomponent.DALComponent.module_ucsub_auto = _Logicalsystem.Id;
            _Logicalcomponent.DALComponent.system_LTD_at_install = _Logicalsystem.GetSystemLife(_actionRecord.ActionDate);
            _Logicalcomponent.DALComponent.equipmentid_auto = _Logicalequipment.Id;
            _context.COMPONENT_LIFE.Add(new ComponentLife
            {
                ActionDate = _actionRecord.ActionDate,
                ActionId = _actionRecord.Id,
                ActualLife = (_Logicalcomponent.DALComponent.cmu ?? 0).LongNullableToInt(),//_Logicalcomponent.GetComponentLife(_actionRecord.ActionDate),
                ComponentId = _Logicalcomponent.Id,
                Title = "Installing component on the system!",
                UserId = _actionRecord.ActionUser.Id
            });
            _context.Entry(_Logicalcomponent.DALComponent).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                ActionLog += "Installing component completed!";
                Message = "Installing component succeeded";
                Status = ActionStatus.Succeed;
            }
            catch (Exception e)
            {
                ActionLog += "Installing component failed!" + e.Message;
                Message = "Failed to install component!";
                Status = ActionStatus.Failed;
            }
            if (Status == ActionStatus.Succeed)
            {
                UniqueId = _actionRecord.Id;
                updateActionRecord();
            }
            return Status;
        }
        public ActionStatus Cancel()
        {
            Status = ActionStatus.Cancelled;
            Message = "Operation cancelled";
            return Status;
        }

        public new void Dispose()
        {
            if (Status != ActionStatus.Succeed)
                rollBack();
            else
            {
                _gContext = new GETContext();
                UpdateGETByAction(_actionRecord, ref pvActionLog);
            }
            _context.Dispose();
        }

        private bool updateActionRecord()
        {
            //Step3 Update action record to have component fields
            ActionLog += "Updating Action History ..." + Environment.NewLine;
            var dalActionRecord = _context.ACTION_TAKEN_HISTORY.Find(_actionRecord.Id);
            //TRACK_ACTION_TYPE Table should be updated to show actions related to the new actions and previous ones were unusable 
            dalActionRecord.action_type_auto = (int)ActionType.InstallComponentOnSystemOnEquipment;
            dalActionRecord.system_auto_id = Params.SystemId;
            dalActionRecord.equnit_auto = Params.Id;
            dalActionRecord.recordStatus = (int)RecordStatus.Available;
            //Should be updated after commit 
            _context.Entry(dalActionRecord).State = System.Data.Entity.EntityState.Modified;
            try
            {
                _context.SaveChanges();
                ActionLog += "Action History updated successfully!" + Environment.NewLine;
                return true;
            }
            catch (Exception ex)
            {
                ActionLog += "Failed to update action history!" + ex.Message + Environment.NewLine;
                return false;
            }
        }
    }
}
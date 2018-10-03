using System;
using System.Linq;
using BLL.Interfaces;
using BLL.Core.Domain;
using System.Data.Entity;
using DAL;
namespace BLL.Core.Repositories
{
    /// <summary>
    /// Installs the system on the equipment!
    /// </summary>
    public class InstallSystemAction : Domain.Action, IAction
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
        private InstallSystemParams Params;
        private UCSystem _Logicalsystem;
        private Equipment _Logicalequipment;
        public InstallSystemAction(DbContext context, IEquipmentActionRecord actionRecord, InstallSystemParams Parameters)
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
                var DALSystemTobeInstalled = _context.LU_Module_Sub.Find(Params.Id);
                if (DALSystemTobeInstalled == null)
                {
                    Message = "System cannot be found!";
                    Status = ActionStatus.Close;
                    return Status;
                }
                if (DALSystemTobeInstalled.equipmentid_auto != null) {
                    Message = "System is already installed!";
                    Status = ActionStatus.Close;
                    return Status;
                }

                DALSystemTobeInstalled.equipmentid_auto = Params.EquipmentId;

                string log = "";
                _actionRecord = UpdateEquipmentByAction(_actionRecord, ref log);
                ActionLog += log;
                Message = base.Message;
                if (_actionRecord != null && _actionRecord.Id != 0)
                {
                    _Logicalsystem = new UCSystem(_context, Params.Id);
                    _Logicalequipment = new Equipment(_context, Params.EquipmentId);
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
                if (_Logicalsystem == null || _Logicalsystem.Id == 0 || _Logicalsystem.DALSystem == null)
                {
                    ActionLog += "System not found!";
                    Message = "System not found! Installing component on the system failed";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                
                if (_Logicalequipment.Id == 0 || _Logicalequipment.DALEquipment == null)
                {
                    ActionLog += "Equipment not found!";
                    Message = "Equipment not found! Installing component on the system failed";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                var systemType = _Logicalsystem.GetSystemType();
                if(systemType == UCSystemType.Unknown)
                {
                    ActionLog += "System Type is not defined!";
                    Message = "System Type is not defined! Link or Idler should be installed first";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                var keq = _Logicalequipment.getSystemsInstallationStatus();
                bool systemExist = false;
                if(Params.side == Side.Left)
                {
                    if ((systemType == UCSystemType.Frame && keq.LeftFrame) || (systemType == UCSystemType.Chain && keq.LeftChain))
                        systemExist = true;
                }else if(Params.side == Side.Right)
                {
                    if ((systemType == UCSystemType.Frame && keq.RightFrame) || (systemType == UCSystemType.Chain && keq.RightChain))
                        systemExist = true;
                }
                else
                {
                    ActionLog += "Side is not valid!";
                    Message = "Side is not valid!";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                if (systemExist)
                {
                    ActionLog += "A system with the same type is already installed on this equipment!";
                    Message = "Installation failed! system already exist.";
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
            _Logicalsystem.DALSystem.SMU_at_install = _actionRecord.ReadSmuNumber;
            _Logicalsystem.DALSystem.equipmentid_auto = Params.EquipmentId;
            _Logicalsystem.DALSystem.equipment_LTD_at_attachment = _actionRecord.EquipmentActualLife;

            foreach(var comp in _Logicalsystem.Components)
            {
                comp.equipmentid_auto = Params.EquipmentId;
                comp.date_installed = _actionRecord.ActionDate;
                comp.smu_at_install = _actionRecord.ReadSmuNumber;
                comp.eq_smu_at_install = _actionRecord.ReadSmuNumber;
                comp.side = (byte)Params.side;
                comp.eq_ltd_at_install = _actionRecord.EquipmentActualLife;
                comp.module_ucsub_auto = _Logicalsystem.Id;
                comp.system_LTD_at_install = _Logicalsystem.GetSystemLife(_actionRecord.ActionDate);
                _context.Entry(comp).State = EntityState.Modified;
            }
            _context.Entry(_Logicalsystem.DALSystem).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                ActionLog += "Setup system completed!";
                Message = "System setup succeeded";
                Status = ActionStatus.Succeed;
            }
            catch (Exception e)
            {
                ActionLog += "installing system failed!" + e.Message;
                Message = "Failed to install system!";
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
            if (Status != ActionStatus.Succeed) {
                _Logicalsystem.detachSystemNoAction();
                rollBack();
            }
                
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
            dalActionRecord.action_type_auto = (int)ActionType.InstallSystemOnEquipment;
            dalActionRecord.system_auto_id = Params.Id;
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
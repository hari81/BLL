using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Interfaces;
using DAL;
using System.Data.Entity;

namespace BLL.Core.Repositories
{
    public class ChangeMeterUnitAction : Domain.Action, IAction
    {
        private IEquipmentActionRecord pvActionRecord;
        private ActionStatus pvActionStatus;
        private string pvActionLog;
        private string pvMessage;
        private int pvInspectionId = 0;

        public int UniqueId
        {
            get { return pvInspectionId; }
            private set { pvInspectionId = value; }
        }
        public IEquipmentActionRecord _actionRecord
        {
            get { return pvActionRecord; }
            private set { pvActionRecord = value; }
        }
        public ActionStatus Status
        {
            get { return pvActionStatus; }
            private set { pvActionStatus = value; }
        }
        public string ActionLog
        {
            get { return pvActionLog; }
            private set { pvActionLog = value; }
        }
        public new string Message
        {
            get { return pvMessage; }
            private set { pvMessage = value; }
        }
        private ChangeMeterUnitParams Params;

        public ChangeMeterUnitAction(DbContext context, IEquipmentActionRecord actionRecord, ChangeMeterUnitParams Paramteres)
            : base(context)
        {
            Params = Paramteres;
            _actionRecord = actionRecord;
            Status = ActionStatus.Close;
        }

        public ActionStatus Start()
        {
            if (Status == ActionStatus.Close)
            {
                string log = "";
                //_actionRecord = UpdateEquipmentByAction(_actionRecord, ref log);
                var actionMonitor = UpdateEquipmentByAction(_actionRecord, ref log);
                _actionRecord = actionMonitor;

                ActionLog += log;
                Message = base.Message;
                if (_actionRecord != null && _actionRecord.Id != 0)
                {
                    Status = ActionStatus.Started;
                    Message = "Opened successfully";
                }
                else
                {
                    Status = ActionStatus.Close;
                    return Status;
                }
            }
            return Status;
        }

        public ActionStatus Validate()
        {
            ActionLog += "Validation started!" + Environment.NewLine;
            if (Status != ActionStatus.Started)
            {
                Message = "Action should start first!";
                ActionLog += Message + Environment.NewLine;
                Status = ActionStatus.Invalid;
                return Status;
            }



            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Params.Id && m.recordStatus == (int)RecordStatus.Available  ).OrderByDescending(m => m.event_date);
            if(actions.Count() > 0)
            {
                if(actions.First().event_date > _actionRecord.ActionDate)
                {
                    Message = "Operation not allowed! You cannot change meter unit while there is an action after "+ _actionRecord.ActionDate.ToString("dd MMM yyyy");
                    ActionLog += Message + Environment.NewLine;
                    Status = ActionStatus.Invalid;
                    return Status;
                }
            }

            ActionLog += "Validation completed!" + Environment.NewLine;
            Message = "Action validated successfully!";
            Status = ActionStatus.Valid;
            return Status;
        }
        public ActionStatus Cancel()
        {
            throw new NotImplementedException();
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
            //Just a new record will be added to the Equipment_Life Table
            _context.EQUIPMENT_LIVES.Add(new EQUIPMENT_LIFE
            {
                ActionDate = _actionRecord.ActionDate,
                ActionId = _actionRecord.Id,
                EquipmentId = _actionRecord.EquipmentId,
                SerialMeterReading = Params.SMUnew,
                UserId = _actionRecord.ActionUser.Id,
                Title = _actionRecord.Comment,
                ActualLife = _actionRecord.EquipmentActualLife
            });
            var equipment = _context.EQUIPMENTs.Find(_actionRecord.EquipmentId);
            if(equipment != null)
            {
                equipment.currentsmu = Params.SMUnew;
                _context.Entry(equipment).State = EntityState.Modified;
            }
            ActionLog += "Start adding new record in EQUIPMENT_LIFE";
            try
            {
                _context.SaveChanges();
                ActionLog += "Start adding new record in EQUIPMENT_LIFE" + Environment.NewLine;
                Message = "Action Recorded Successfully!";
                updateActionRecord();
                Status = ActionStatus.Succeed;
                return Status;
            }
            catch (Exception ex)
            {
                ActionLog += ex.Message + Environment.NewLine;
                ActionLog += ex.InnerException != null ? "Inner Exception: " + ex.InnerException.Message : "";
                Message = "Operation Failed!" + ex.Message;
                Status = ActionStatus.Failed;
                return Status;
            }
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
            dalActionRecord.action_type_auto = (int)ActionType.ChangeMeterUnit;
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
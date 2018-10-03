using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Interfaces;
using DAL;
using System.Data.Entity;

namespace BLL.Core.Actions
{
    public class SMUReadingAction : Domain.Action, IAction
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

        public SMUReadingAction(DbContext context, IEquipmentActionRecord actionRecord)
            : base(context)
        {
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
                ActionLog += "Nothing for commit in this action!" + Environment.NewLine;
                Message = "Action Recorded Successfully!";
                updateActionRecord();
                Status = ActionStatus.Succeed;
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
            dalActionRecord.action_type_auto = (int)ActionType.SMUReadingAction;
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
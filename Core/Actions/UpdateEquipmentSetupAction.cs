using BLL.Core.Domain;
using BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Actions
{
    public class UpdateEquipmentSetupAction : Domain.Action, Interfaces.IAction
    {
        private GETEquipmentSetupParams Params;
        private ActionStatus _status = ActionStatus.Close;
        private string _log = "";
        private string _message = "";
        private int _id;
        private DAL.ACTION_TAKEN_HISTORY _modifiedDALHistory;
        private RecordStatus _previousRecordState = RecordStatus.NoContent;
        private DAL.EQUIPMENT _equipment;
        private int OldSmu = 0;
        private int OldLtd = 0;

        public UpdateEquipmentSetupAction(System.Data.Entity.DbContext context, IEquipmentActionRecord actionRecord, GETEquipmentSetupParams Paramteres)
            : base(context)
        {
            Params = Paramteres;
            _status = ActionStatus.Close;
            _current = actionRecord;
        }
        public string ActionLog
        {
            get
            {
                return _log;
            }
        }

        public new string Message
        {
            get
            {
                return _message;
            }
        }

        public ActionStatus Status
        {
            get
            {
                return _status;
            }
        }

        public int UniqueId
        {
            get
            {
                return _id;
            }
        }

        IEquipmentActionRecord IAction._actionRecord
        {
            get
            {
                return _current;
            }
        }

        ActionStatus IAction.Start()
        {
            if (_status != ActionStatus.Close)
            {
                _message = "Action cannot be started becasue it's state indicates that it is not closed!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            if (!Params.IsUpdating)
            {
                _message = "Action cannot be started becasue it is not an update action!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            _equipment = _context.EQUIPMENTs.Find(Params.EquipmentId);
            if (_equipment == null)
            {
                _message = "Equipment cannot be found!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            
            _modifiedDALHistory = _context.ACTION_TAKEN_HISTORY.Find(_equipment.ActionTakenHistoryId);
            if (_modifiedDALHistory == null)
            {
                _modifiedDALHistory = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == _equipment.equipmentid_auto && m.recordStatus == (int)RecordStatus.Available && m.action_type_auto == (int)ActionType.EquipmentSetup).FirstOrDefault();
                if (_modifiedDALHistory == null)
                {
                    _message = "History of the equipment setup cannot be found!";
                    _log += _message + Environment.NewLine;
                    _id = -2;
                    return _status;
                }
            }
            if (_modifiedDALHistory.recordStatus != (int)RecordStatus.Available)
            {
                _message = "This equipment is not available to be updated!";
                _log += _message + "RecordStatus for this equipment must be 0 in TRACK_ACTION_HISTORY table" + Environment.NewLine;
                return _status;
            }
            _previousRecordState = (RecordStatus)_modifiedDALHistory.recordStatus;
            _modifiedDALHistory.recordStatus = (int)RecordStatus.Modified;
            OldSmu = (int)(_equipment.smu_at_start ?? 0);
            OldLtd = (int)(_equipment.LTD_at_start ?? 0);
            _equipment.smu_at_start = Params.MeterReading;
            _equipment.LTD_at_start = Params.EquipmentLTD;
            _context.Entry(_equipment).State = System.Data.Entity.EntityState.Modified;
            _context.Entry(_modifiedDALHistory).State = System.Data.Entity.EntityState.Modified;
            try
            {
                _log += "Start modifying ACTION_TAKEN_HISTORY RecordStatus to modified" + Environment.NewLine;
                _context.SaveChanges();
                _log += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception ex)
            {
                _log += "Failed to save!" + Environment.NewLine;
                _log += "Error Details: " + ex.Message + Environment.NewLine;
                _status = ActionStatus.Failed;
                _message = "Operation failed! Cannot update existing history record!";
                return _status;
            }
            _current = UpdateEquipmentByAction(_current, ref _log);
            _message = Message;
            if (_current == null || _current.Id == 0)
            {
                _message = Message;
                return _status;
            }
            var dalActionRecord = _context.ACTION_TAKEN_HISTORY.Find(_current.Id);
            dalActionRecord.action_type_auto = (int)ActionType.UpdateSetupEquipment;
            dalActionRecord.recordStatus = (int)RecordStatus.MiddleOfAction;
            _context.Entry(dalActionRecord).State = System.Data.Entity.EntityState.Modified;
            try
            {
                _log += "Saving Record Status... " + Environment.NewLine;
                _context.SaveChanges();
                _log += "Record status updated successfully";
            }
            catch (Exception Ex)
            {
                _log += "Saving record status failed!" + Ex.Message + "InnerException:" + Ex.InnerException != null ? Ex.InnerException.Message : "";
                _message = "Cannot Update equipment! Please check log!";
                return _status;
            }
            _status = ActionStatus.Started;
            _message = "Action Started Successfully!";
            _log += _message + Environment.NewLine;
            return _status;
        }

        ActionStatus IAction.Validate()
        {
            if (_status != ActionStatus.Started)
            {
                _message = "Validation Failed! Action needs to be started first!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            var _existingAction = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == _equipment.equipmentid_auto & m.recordStatus == (int)RecordStatus.Available && m.event_date < _current.ActionDate).OrderBy(m => m.event_date).FirstOrDefault();
            if (_existingAction != null)
            {
                _message = "Validation Failed! Equipment setup date must be before any other action. " + _existingAction.TRACK_ACTION_TYPE.action_description + " in " + _existingAction.event_date.ToString("dd-MMM-yyyy") + " is exist!";
                _log += _message + Environment.NewLine;
                _status = ActionStatus.Invalid;
                return _status;
            }
            var _smuChanged = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == _equipment.equipmentid_auto & m.recordStatus == (int)RecordStatus.Available && m.event_date >= _current.ActionDate && m.action_type_auto == (int)ActionType.ChangeMeterUnit).FirstOrDefault();
            if (_smuChanged != null)
            {
                _message = "Validation Failed! Serial Meter Unit has been replaced in" + _existingAction.event_date.ToString("dd-MMM-yyyy");
                _log += _message + Environment.NewLine;
                _status = ActionStatus.Invalid;
                return _status;
            }
            _status = ActionStatus.Valid;
            return _status;
        }

        ActionStatus IAction.Commit()
        {
            _log += "Commiting the Operation ..." + Environment.NewLine;
            if (_status != ActionStatus.Valid)
            {
                _log += "Operation Status is not valid" + Environment.NewLine;
                _message = "Operation is not valid";
                _status = ActionStatus.Failed;
                return _status;
            }
            if (_equipment == null)
            {
                _log += "Equipment cannot be found!" + Environment.NewLine;
                _message = "Equipment cannot be found!";
                _status = ActionStatus.Failed;
                return _status;
            }

            int SmuOffset = _current.ReadSmuNumber - _modifiedDALHistory.equipment_smu;
            int LTDOffset = Params.EquipmentLTD - _modifiedDALHistory.equipment_ltd;
            _log += "Calculated Smu offset: " + SmuOffset + "Calculated LTD offset: " + LTDOffset + Environment.NewLine;
            try
            {
                var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == _equipment.equipmentid_auto & m.recordStatus == (int)RecordStatus.Available && m.event_date >= _current.ActionDate).Select(m => m.history_id).ToList();
                var EqLives = _context.EQUIPMENT_LIVES.Where(m => actions.Any(k => k == m.ActionId)).OrderBy(m => m.ActionDate).ToList();

                foreach (var life in EqLives)
                {
                    life.ActualLife -= SmuOffset;
                    _context.Entry(life).State = System.Data.Entity.EntityState.Modified;
                }
                /*var SystemLivesG = _context.UCSYSTEM_LIFE.Where(m => actions.Any(k => k == m.ActionId)).GroupBy(m => m.SystemId).ToList();
                foreach (var systemLives in SystemLivesG)
                {
                    foreach (var life in systemLives)
                    {
                        life.ActualLife -= SmuOffset;
                        if (life.ActualLife < 0) life.ActualLife = 0;
                        _context.Entry(life).State = System.Data.Entity.EntityState.Modified;
                    }

                    var _system = _context.LU_Module_Sub.Find(systemLives.Key);
                    _system.equipment_LTD_at_attachment += SmuOffset;
                    if (_system.equipment_LTD_at_attachment < 0) _system.equipment_LTD_at_attachment = Params.EquipmentLTD;
                    _system.LTD -= SmuOffset;
                    if (_system.LTD < 0) _system.LTD = Params.EquipmentLTD;
                    _context.Entry(_system).State = System.Data.Entity.EntityState.Modified;
                }
                var ComponentLivesG = _context.COMPONENT_LIFE.Where(m => actions.Any(k => k == m.ActionId)).GroupBy(m => m.ComponentId).ToList();
                foreach (var ComponentLives in ComponentLivesG)
                {
                    
                    foreach (var life in ComponentLives)
                    {
                        life.ActualLife -= SmuOffset;
                        if (life.ActualLife < 0) life.ActualLife = 0;
                        _context.Entry(life).State = System.Data.Entity.EntityState.Modified;
                    }
                    
                    var _component = _context.GENERAL_EQ_UNIT.Find(ComponentLives.Key);
                    
                    _component.eq_ltd_at_install += SmuOffset;
                    if (_component.eq_ltd_at_install < 0) _component.eq_ltd_at_install = Params.EquipmentLTD;
                    _component.smu_at_install += SmuOffset;
                    if (_component.smu_at_install < 0) _component.smu_at_install = _current.ReadSmuNumber;
                    _component.eq_smu_at_install += SmuOffset;
                    if (_component.eq_smu_at_install < 0) _component.eq_smu_at_install = _current.ReadSmuNumber;
                    _context.Entry(_component).State = System.Data.Entity.EntityState.Modified;
                }
                var _inspectionDetails = _context.TRACK_INSPECTION_DETAIL.Where(m => actions.Any(k => k == m.TRACK_INSPECTION.ActionHistoryId));
                foreach (var detail in _inspectionDetails)
                {
                    detail.hours_on_surface -= SmuOffset;
                    if (detail.hours_on_surface < 0) detail.hours_on_surface = 0;
                    _context.Entry(detail).State = System.Data.Entity.EntityState.Modified;
                }*/
                _log += "Modifying Equipment to have new Action History Id" + Environment.NewLine;
                _equipment.ActionTakenHistoryId = _current.Id;
                _context.Entry(_equipment).State = System.Data.Entity.EntityState.Modified;

            }
            catch (Exception Ex)
            {
                _log += "Updating Action Lives Failed!" + Ex.Message + Ex.InnerException != null ? Ex.InnerException.Message : "" + Environment.NewLine;
                _message = "Operation Failed when updating Life Tables";
                _status = ActionStatus.Failed;
                return _status;
            }
            try
            {
                _log += "Start saving Equipment ..." + Environment.NewLine;
                _context.SaveChanges();
                _log += "Equipment saved successfully!" + Environment.NewLine;
            }
            catch (Exception Ex)
            {
                _log += "Updating Action History Id Failed!" + Ex.Message + Ex.InnerException != null ? Ex.InnerException.Message : "" + Environment.NewLine;
                _message = "Operation Failed when updating Equipment history";
                _status = ActionStatus.Failed;
                return _status;
            }
            Available();
            _log += "Action committed successfully!" + Environment.NewLine;
            _message += "Operation Completed Successfully!";
            _status = ActionStatus.Succeed;
            return _status;
        }
        ActionStatus IAction.Cancel()
        {
            _status = ActionStatus.Cancelled;
            _message = "Operation Cancelled!";
            return _status;
        }

        public new void Dispose()
        {
            if (_status != ActionStatus.Succeed)
            {
                rollBack();
                if (_previousRecordState != RecordStatus.NoContent)
                {
                    if(_equipment != null)
                    {
                        _equipment.smu_at_start = OldSmu;
                        _equipment.LTD_at_start = OldLtd;
                        _context.Entry(_equipment).State = System.Data.Entity.EntityState.Modified;
                    }
                    _modifiedDALHistory.recordStatus = (int)_previousRecordState;
                    _context.Entry(_modifiedDALHistory).State = System.Data.Entity.EntityState.Modified;
                    _context.SaveChanges();
                }
            }
            else
            {
                _gContext = new DAL.GETContext();
                UpdateGETByAction(_current, ref _log);
            }
            _context.Dispose();
        }
    }
}


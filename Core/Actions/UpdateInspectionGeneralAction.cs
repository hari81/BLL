using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Interfaces;

namespace BLL.Core.Actions
{
    /// <summary>
    /// This method is a simplified version of InsertInspectionAction 
    /// This method will not update components reading but will update the life of the components ans system
    /// 
    /// </summary>
    public class UpdateInspectionGeneralAction : Domain.Action, Interfaces.IAction
    {
        private IGeneralInspectionModel Params;
        private ActionStatus _status = ActionStatus.Close;
        private string _log = "";
        private string _message = "";
        private int _id;
        private DAL.ACTION_TAKEN_HISTORY _modifiedDALHistory;
        private RecordStatus _previousRecordState = RecordStatus.NoContent;
        private DAL.TRACK_INSPECTION _oldDALInspection;
        
        public UpdateInspectionGeneralAction(System.Data.Entity.DbContext context, IEquipmentActionRecord actionRecord, IGeneralInspectionModel Paramteres)
            : base(context)
        {
            Params = Paramteres;
            _status = ActionStatus.Close;
            _current = actionRecord;
        }
        string IAction.ActionLog
        {
            get
            {
                return _log;
            }
        }

        string IAction.Message
        {
            get
            {
                return _message;
            }
        }

        ActionStatus IAction.Status
        {
            get
            {
                return _status;
            }
        }

        int IAction.UniqueId
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
            if (Params.Id <= 0)
            {
                _message = "Action cannot be started becasue it is not an update action!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            _oldDALInspection = _context.TRACK_INSPECTION
                .Include("TRACK_ACTION")
                .Include("TRACK_INSPECTION_DETAIL")
                .Include("TRACK_INSPECTION_DETAIL.Images")
                .Include("TRACK_INSPECTION_DETAIL.MeaseurementPointRecors")
                .Include("TRACK_INSPECTION_DETAIL.MeaseurementPointRecors.Photos")
                .Include("CompartTypeAdditionals")
                .Include("CompartTypeAdditionals.RecordImages")
                .Include("CompartTypeAdditionalImages")
                .Include("CompartTypeAdditionalImages.HiddenInReports")
                .Include("InspectionMandatoryImages")
                .Include("InspectionMandatoryImages.HiddenInReports")
                .Include("InspectionCompartTypeImages")
                .Include("InspectionCompartTypeImages.HiddenInReports")
                .Include("Quotes")
                .Include("Quotes.Recommendations")
                .Where(m=> m.inspection_auto == Params.Id).FirstOrDefault();
            if (_oldDALInspection == null) {
                foreach (var _detail in _oldDALInspection.TRACK_INSPECTION_DETAIL)
                    _context.Entry(_detail).Reference(m => m.SIDE).Load();
                _message = "Inspection cannot be found!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            _modifiedDALHistory = _context.ACTION_TAKEN_HISTORY.Find(_oldDALInspection.ActionHistoryId);
            if (_modifiedDALHistory == null) {
                _message = "History of the inspection cannot be found!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            if (_modifiedDALHistory.recordStatus != (int)RecordStatus.Available)
            {
                _message = "This inspection is not available to be updated!";
                _log += _message + "RecordStatus for this inspection must be 0 in TRACK_ACTION_HISTORY table" +Environment.NewLine;
                return _status;
            }
            _previousRecordState = (RecordStatus)_modifiedDALHistory.recordStatus;
            _modifiedDALHistory.recordStatus = (int)RecordStatus.Modified;
            _context.Entry(_modifiedDALHistory).State = System.Data.Entity.EntityState.Modified;
            try
            {
                _log += "Start modifying ACTION_TAKEN_HISTORY RecordStatus to modified" + Environment.NewLine;
                _context.SaveChanges();
                _log += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception ex) {
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
            dalActionRecord.action_type_auto = (int)ActionType.UpdateInspectionGeneral;
            dalActionRecord.recordStatus = (int)RecordStatus.MiddleOfAction;
            _context.Entry(dalActionRecord).State = System.Data.Entity.EntityState.Modified;
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
            if (_current.ActionDate > DateTime.Now.ToLocalTime().AddDays(1))
            {
                _message = "Inspection date cannot be more than one day in the future!";
                _status = ActionStatus.Invalid;
                return _status;
            }
            var ti = _context.TRACK_INSPECTION.Find(Params.Id);
            if (ti == null)
            {
                _message = "Inspection cannot be found!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            var kAfter = _context.ACTION_TAKEN_HISTORY.Where(m => m.history_id != ti.ActionHistoryId && m.recordStatus == 0 && m.event_date > ti.inspection_date && m.equipmentid_auto == ti.equipmentid_auto).OrderBy(m => m.event_date).FirstOrDefault();
            if (kAfter != null && _current.ActionDate > kAfter.event_date) {
                _message = "Operation not allowed! Inspection date should be before " + kAfter.TRACK_ACTION_TYPE.action_description + " on " + kAfter.event_date.ToShortDateString();
                _status = ActionStatus.Invalid;
                return _status;
            }

            var kBefore = _context.ACTION_TAKEN_HISTORY.Where(m => m.history_id != ti.ActionHistoryId && m.recordStatus == 0 && m.event_date < ti.inspection_date && m.equipmentid_auto == ti.equipmentid_auto).OrderByDescending(m => m.event_date).FirstOrDefault();
            if (kBefore != null && _current.ActionDate < kBefore.event_date)
            {
                _message = "Operation not allowed! Inspection date should be after " + kBefore.TRACK_ACTION_TYPE.action_description + " on " + kBefore.event_date.ToShortDateString();
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
                _status = ActionStatus.Failed;
                return _status;
            }


            _oldDALInspection.ActionHistoryId = _current.Id;
            _oldDALInspection.created_date = DateTime.Now.ToLocalTime();
            _oldDALInspection.created_user = _current.ActionUser.userName;
            _oldDALInspection.inspection_date = _current.ActionDate.ToLocalTime().Date;
            _oldDALInspection.smu = _current.ReadSmuNumber;
            _oldDALInspection.examiner = _current.ActionUser.userName;
            _context.Entry(_oldDALInspection).State = System.Data.Entity.EntityState.Added;
            foreach(var _detail in _oldDALInspection.TRACK_INSPECTION_DETAIL)
            {
                if (_detail.SIDE != null)
                {
                    _detail.Side = _detail.SIDE.Side;
                    _context.Entry(_detail.SIDE).State = System.Data.Entity.EntityState.Added;
                }

                _context.Entry(_detail).State = System.Data.Entity.EntityState.Added;
                foreach (var _image in _detail.Images)
                    _context.Entry(_image).State = System.Data.Entity.EntityState.Added;
                foreach (var _record in _detail.MeaseurementPointRecors)
                {
                    _context.Entry(_record).State = System.Data.Entity.EntityState.Added;
                    foreach (var _photo in _record.Photos)
                        _context.Entry(_photo).State = System.Data.Entity.EntityState.Added;
                }
            }
            foreach(var _t_action in _oldDALInspection.TRACK_ACTION)
            {
                _context.Entry(_t_action).State = System.Data.Entity.EntityState.Added;
            }
            foreach(var _record in _oldDALInspection.CompartTypeAdditionals)
            {
                _context.Entry(_record).State = System.Data.Entity.EntityState.Added;
                foreach(var _rec in _record.RecordImages)
                    _context.Entry(_rec).State = System.Data.Entity.EntityState.Added;
            }
            foreach(var _record in _oldDALInspection.CompartTypeAdditionalImages)
            {
                _context.Entry(_record).State = System.Data.Entity.EntityState.Added;
                foreach (var _rec in _record.HiddenInReports)
                    _context.Entry(_rec).State = System.Data.Entity.EntityState.Added;
            }
            foreach (var _record in _oldDALInspection.InspectionMandatoryImages)
            {
                _context.Entry(_record).State = System.Data.Entity.EntityState.Added;
                foreach (var _rec in _record.HiddenInReports)
                    _context.Entry(_rec).State = System.Data.Entity.EntityState.Added;
            }
            foreach (var _record in _oldDALInspection.InspectionCompartTypeImages)
            {
                _context.Entry(_record).State = System.Data.Entity.EntityState.Added;
                foreach (var _rec in _record.HiddenInReports)
                    _context.Entry(_rec).State = System.Data.Entity.EntityState.Added;
            }
            foreach (var _record in _oldDALInspection.Quotes)
            {
                _context.Entry(_record).State = System.Data.Entity.EntityState.Added;
                foreach (var _rec in _record.Recommendations)
                    _context.Entry(_rec).State = System.Data.Entity.EntityState.Added;
            }
            try
            {
                _log += "Start saving New Inspection in TRACK_INSPECTION" + Environment.NewLine;
                _context.SaveChanges();
                _id = _oldDALInspection.inspection_auto;
                _log += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception e1)
            {
                _log += "Failed to save!" + Environment.NewLine;
                _log += "Error Details: " + e1.Message + Environment.NewLine;
                _status = ActionStatus.Failed;
                _message = "Inspection saving failed!";
                return _status;
            }
            /*
            var components = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == _current.EquipmentId);
            var _detailEntities = new List<DAL.TRACK_INSPECTION_DETAIL>();
            foreach (var comp in components)
            {
                var cmpntd = new Component(_context, longNullableToint(comp.equnit_auto));
                var _detailEntity = new DAL.TRACK_INSPECTION_DETAIL
                {
                    inspection_auto = _inspectionEntity.inspection_auto,
                    track_unit_auto = comp.equnit_auto,
                    tool_auto = -1, /*░░░░░░Needs to be updated later░░░░░░*-/
                    reading = 0, /*░░░░░░Needs to be updated later░░░░░░*-/
                    worn_percentage = 0, /*░░░░░░Needs to be updated later░░░░░░*-/
                    eval_code = "U",/*░░░░░░Needs to be updated later░░░░░░*-/
                    hours_on_surface = cmpntd.GetComponentLifeMiddleOfNewAction(_current.ActionDate),
                    projected_hours = 0,/*░░░░░░Needs to be updated later░░░░░░*-/
                    ext_projected_hours = 0, /*░░░░░░Needs to be updated later░░░░░░*-/
                    remaining_hours = 0, /*░░░░░░Needs to be updated later░░░░░░*-/
                    ext_remaining_hours = 0, /*░░░░░░Needs to be updated later░░░░░░*-/
                    worn_percentage_120 = 0, /*░░░░░░Needs to be updated later░░░░░░*-/
                    UCSystemId = comp.module_ucsub_auto,
                    Side = comp.side ?? 0
                };
                _detailEntities.Add(_detailEntity);
            }
            _context.TRACK_INSPECTION_DETAIL.AddRange(_detailEntities);
            try
            {
                _log += "Start saving range of inspection details to the TRACK_INSPECTION_DETAIL" + Environment.NewLine;
                _context.SaveChanges();
                _log += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception e2)
            {
                _log += "Failed to save!" + Environment.NewLine;
                _log += "Error Details: " + e2.Message + Environment.NewLine;
                _status = ActionStatus.Failed;
                _message = "Save details range failed";
                return _status;
            }

            foreach (var tid in _detailEntities)
                _context.InspectionDetails_Side.Add(new DAL.InspectionDetails_Side { InspectionDetailsId = tid.inspection_detail_auto, Side = tid.Side });
            try
            {
                _log += "Start saving range of inspection sides to the InspectionDetails_Side" + Environment.NewLine;
                _context.SaveChanges();
                _log += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception e2)
            {
                _log += "Failed to save!" + Environment.NewLine;
                _log += "Error Details: " + e2.Message + Environment.NewLine;
                _status = ActionStatus.Failed;
                _message = "Save side for the component failed";
                return _status;
            }*/
            Available();
            _status = ActionStatus.Succeed;
            _message = "All done successfully";
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
                if (_previousRecordState != RecordStatus.NoContent) {
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
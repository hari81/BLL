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
    public class InsertInspectionGeneralAction : Domain.Action, Interfaces.IAction
    {
        private IGeneralInspectionModel Params;
        private ActionStatus _status;
        private string _log;
        private string _message;
        private int _id;
        public InsertInspectionGeneralAction(System.Data.Entity.DbContext context, IEquipmentActionRecord actionRecord, IGeneralInspectionModel Paramteres)
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
            if (Params.Id > 0) {
                _message = "Action cannot be started becasue it is not a new inspection!";
                _log += _message + Environment.NewLine;
                return _status;
            }
            _log += "Starting Insert Inspection General Action" + Environment.NewLine;
            var _res = PreValidate(_current);
            if (!_res.IsValid)
            {
                _message = "Pre Validation Failed! " + "Smallest valid smu for date is: " + _res.SmallestValidSmuForProvidedDate + ". Earliest valid date for the provided smu is: " + _res.EarliestValidDateForProvidedSMU + ". Please justify your inputs.";
                _log += _message + Environment.NewLine;
                _status = ActionStatus.Close;
                return _status;
            }

            _current = UpdateEquipmentByAction(_current, ref _log);
            if (_current == null || _current.Id == 0)
            {
                _message = Message;
                return _status;
            }
            var dalActionRecord = _context.ACTION_TAKEN_HISTORY.Find(_current.Id);
            dalActionRecord.action_type_auto = (int)ActionType.InsertInspectionGeneral;
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
            if (_current.ActionDate > DateTime.Now.AddDays(1))
            {
                _status = ActionStatus.Invalid;
                _message = "Inspection date cannot be more than one day in the future!";
                return _status;
            }
            if (_context.TRACK_INSPECTION.Where(m => m.docket_no == Params.DocketNo && m.equipmentid_auto == Params.Id).Count() > 0)
            {
                _status = ActionStatus.Invalid;
                _message = "This Docket No has been used for this Equipment! Please change Docket No.";
                return _status;
            }
            var kAfter = _context.ACTION_TAKEN_HISTORY.Where(m => m.event_date > _current.ActionDate && m.recordStatus == 0 && m.equipmentid_auto == Params.Id && (m.action_type_auto == (int)ActionType.ReplaceComponentWithNew || m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory)).OrderByDescending(m => m.event_date);
            if (kAfter.Count() > 0)
            {
                _message = "Operation not allowed! Inspection date should be after " + kAfter.First().TRACK_ACTION_TYPE.action_description + " on " + kAfter.First().event_date.ToShortDateString();
                _status = ActionStatus.Invalid;
                return _status;
            }
            //Currently there is nothing to be validated after prevalidation
            _message = "Validation Passed!";
            _log += _message + Environment.NewLine;
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
            var impact = LowNormalHigh.Low;
            try { impact = (LowNormalHigh)Params.Impact; } catch (Exception ex) { string message = ex.Message; }
            var abrasive = LowNormalHigh.Low;
            try { abrasive = (LowNormalHigh)Params.Abrasive; } catch (Exception ex) { string message = ex.Message; }
            var moisture = LowNormalHigh.Low;
            try { moisture = (LowNormalHigh)Params.Moisture; } catch (Exception ex) { string message = ex.Message; }
            var packing = LowNormalHigh.Low;
            try { packing = (LowNormalHigh)Params.Packing; } catch (Exception ex) { string message = ex.Message; }

            var _inspectionEntity = new DAL.TRACK_INSPECTION();
            _inspectionEntity.equipmentid_auto = _current.EquipmentId;
            _inspectionEntity.inspection_date = _current.ActionDate.ToLocalTime().Date;
            _inspectionEntity.smu = _current.ReadSmuNumber;
            _inspectionEntity.examiner = _current.ActionUser.userName;
            _inspectionEntity.notes = Params.InspectionNotes;
            _inspectionEntity.Jobsite_Comms = Params.JobSiteNotes;
            _inspectionEntity.CustomerContact = Params.CustomerContact;
            _inspectionEntity.TrammingHours = Params.TrammingHours;
            _inspectionEntity.track_sag_left = Params.TrackSagLeft;
            _inspectionEntity.track_sag_right = Params.TrackSagRight;
            _inspectionEntity.dry_joints_left = Params.DryJointsLeft;
            _inspectionEntity.dry_joints_right = Params.DryJointsRight;
            _inspectionEntity.impact = (short)impact;
            _inspectionEntity.abrasive = (short)abrasive;
            _inspectionEntity.packing = (short)packing;
            _inspectionEntity.moisture = (short)moisture;
            _inspectionEntity.created_date = DateTime.Now.ToLocalTime();
            _inspectionEntity.created_user = _current.ActionUser.userName;
            _inspectionEntity.docket_no = Params.DocketNo;
            _inspectionEntity.ltd = _current.EquipmentActualLife;
            _inspectionEntity.evalcode = "U";//░░░░░░Needs to be updated later░░░░░░
            _inspectionEntity.ActionHistoryId = _current.Id;
            //░░░░░░There are some other fields that needs to be updated later░░░░░░

            _context.TRACK_INSPECTION.Add(_inspectionEntity);
            try
            {
                _log += "Start saving New Inspection in TRACK_INSPECTION" + Environment.NewLine;
                _context.SaveChanges();
                _id = _inspectionEntity.inspection_auto;
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

            //var components = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == _current.EquipmentId);
            //var _detailEntities = new List<DAL.TRACK_INSPECTION_DETAIL>();
            //foreach (var comp in components)
            //{
            //    var cmpntd = new Component(_context, longNullableToint(comp.equnit_auto));
            //    var _detailEntity = new DAL.TRACK_INSPECTION_DETAIL {
            //        inspection_auto = _inspectionEntity.inspection_auto,
            //        track_unit_auto = comp.equnit_auto,
            //        tool_auto = -1, /*░░░░░░Needs to be updated later░░░░░░*/
            //        reading = 0, /*░░░░░░Needs to be updated later░░░░░░*/
            //        worn_percentage = 0, /*░░░░░░Needs to be updated later░░░░░░*/
            //        eval_code = "U",/*░░░░░░Needs to be updated later░░░░░░*/
            //        hours_on_surface = cmpntd.GetComponentLifeMiddleOfNewAction(_current.ActionDate),
            //        projected_hours = 0,/*░░░░░░Needs to be updated later░░░░░░*/
            //        ext_projected_hours = 0, /*░░░░░░Needs to be updated later░░░░░░*/
            //        remaining_hours = 0, /*░░░░░░Needs to be updated later░░░░░░*/
            //        ext_remaining_hours = 0, /*░░░░░░Needs to be updated later░░░░░░*/
            //        worn_percentage_120 = 0, /*░░░░░░Needs to be updated later░░░░░░*/
            //        UCSystemId = comp.module_ucsub_auto,
            //        Side = comp.side ?? 0
            //    };
            //    _detailEntities.Add(_detailEntity);
            //}
            //_context.TRACK_INSPECTION_DETAIL.AddRange(_detailEntities);
            //try
            //{
            //    _log += "Start saving range of inspection details to the TRACK_INSPECTION_DETAIL" + Environment.NewLine;
            //    _context.SaveChanges();
            //    _log += "Saved successfully." + Environment.NewLine;
            //}
            //catch (Exception e2)
            //{
            //    _log += "Failed to save!" + Environment.NewLine;
            //    _log += "Error Details: " + e2.Message + Environment.NewLine;
            //    _status = ActionStatus.Failed;
            //    _message = "Save details range failed";
            //    return _status;
            //}

            //foreach (var tid in _detailEntities) 
            //    _context.InspectionDetails_Side.Add(new DAL.InspectionDetails_Side { InspectionDetailsId = tid.inspection_detail_auto, Side = tid.Side });
            //try
            //{
            //    _log += "Start saving range of inspection sides to the InspectionDetails_Side" + Environment.NewLine;
            //    _context.SaveChanges();
            //    _log += "Saved successfully." + Environment.NewLine;
            //}
            //catch (Exception e2)
            //{
            //    _log += "Failed to save!" + Environment.NewLine;
            //    _log += "Error Details: " + e2.Message + Environment.NewLine;
            //    _status = ActionStatus.Failed;
            //    _message = "Save side for the component failed";
            //    return _status;
            //}
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
                rollBack();
            else
            {
                _gContext = new DAL.GETContext();
                UpdateGETByAction(_current, ref _log);
            }
            _context.Dispose();
        }
    }
}
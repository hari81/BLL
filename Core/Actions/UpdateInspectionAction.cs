using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using BLL.Persistence.Repositories;
using BLL.Core.Domain;
using System.Data.Entity;
using DAL;
using BLL.Extensions;

namespace BLL.Core.Repositories
{
    public class UpdateInspectionAction : Domain.Action, IAction
    {
        private IEquipmentActionRecord pvActionRecord;
        private IEquipmentActionRecord removingActionRecord;
        private ACTION_TAKEN_HISTORY removingActionHistoryRecord;
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
        private UpdateInspectionParams Params;
        public UpdateInspectionAction(DbContext context, IEquipmentActionRecord actionRecord, UpdateInspectionParams Paramteres)
            : base(context)
        {
            Params = Paramteres;
            _actionRecord = actionRecord;
            Status = ActionStatus.Close;
        }

        public UndercarriageContext _Inspectioncontext
        {
            get { return _context as UndercarriageContext; }
        }

        public ActionStatus Start()
        {
            if (Status == ActionStatus.Close)
            {
                if (Params.EquipmentInspection != null)
                {
                    var ti = _context.TRACK_INSPECTION.Find(Params.EquipmentInspection.inspection_auto);
                    if (ti != null)
                    {
                        if (ti.ActionHistoryId != null && ti.ActionTakenHistory != null)
                        {
                            //We need to keep previous action record data and if this action is not valid bring that one back
                            removingActionHistoryRecord = ti.ActionTakenHistory;
                            removingActionRecord = new EquipmentActionRecord
                            {
                                Id = longNullableToint(removingActionHistoryRecord.history_id),
                                ActionDate = removingActionHistoryRecord.event_date,
                                ActionUser = new User { Id = longNullableToint(removingActionHistoryRecord.entry_user_auto) },
                                EquipmentId = longNullableToint(removingActionHistoryRecord.equipmentid_auto),
                                ReadSmuNumber = removingActionHistoryRecord.equipment_smu,
                                TypeOfAction = ActionType.InsertInspection,
                                Comment = removingActionHistoryRecord.comment,
                                Cost = removingActionHistoryRecord.cost
                            };
                            _context.ACTION_TAKEN_HISTORY.Remove(removingActionHistoryRecord);
                            try
                            {
                                _context.SaveChanges();
                            }
                            catch (Exception e)
                            {
                                Message = "Error on removing previous action history: " + e.Message;
                                return ActionStatus.Close;
                            }
                        }
                    }
                    else
                    {
                        Message = "Inspection not found to be updated!";
                        return ActionStatus.Close;
                    }
                    string log = "";
                    _actionRecord = UpdateEquipmentByAction(_actionRecord, ref log);
                    ActionLog += log;
                    Message = base.Message;
                    if (_actionRecord != null && _actionRecord.Id != 0)
                    {
                        //Step3 Update action record to have component fields
                        ActionLog += "Updating Action History ..." + Environment.NewLine;
                        var dalActionRecord = _context.ACTION_TAKEN_HISTORY.Find(_actionRecord.Id);
                        //TRACK_ACTION_TYPE Table should be updated to show actions related to the new actions and previous ones were unusable 
                        dalActionRecord.action_type_auto = (int)ActionType.UpdateInspection;
                        _context.Entry(dalActionRecord).State = System.Data.Entity.EntityState.Modified;
                        Status = ActionStatus.Started;
                        Message = "Opened successfully";
                    }
                    else
                    {
                        Status = ActionStatus.Close;
                        return Status;
                    }
                }
                else
                {
                    Status = ActionStatus.Close;
                    Message = "Parameters are not set correctly";
                }
            }
            return Status;
        }
        /// <summary>
        /// This method validates the action and if there is any error returns Invalid status
        /// Should be Implemented later
        /// </summary>
        /// <param name="ActionLog"></param>
        /// <returns></returns>
        public ActionStatus Validate()
        {
            if (Status != ActionStatus.Started)
                return Status;
            if (Params.EquipmentInspection == null)
            {
                Message = "Inspection not found!";
                Status = ActionStatus.Invalid;
                return Status;
            }
            var ti = _context.TRACK_INSPECTION.Find(Params.EquipmentInspection.inspection_auto);
            long? actionId = 0;
            if (ti != null)
            {
                actionId = ti.ActionHistoryId;
                var kAfter = _context.ACTION_TAKEN_HISTORY.Where(m => m.history_id != actionId && m.recordStatus == 0 && m.event_date > ti.inspection_date && m.equipmentid_auto == ti.equipmentid_auto).OrderBy(m => m.event_date);
                if(Params.EquipmentInspection.inspection_date > DateTime.Now.AddDays(1))
                {
                    Message = "Inspection date cannot be more than one day in the future!";
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                if (kAfter.Count() > 0 && Params.EquipmentInspection.inspection_date > kAfter.First().event_date)
                {
                    Message = "Operation not allowed! Inspection date should be before " + kAfter.First().TRACK_ACTION_TYPE.action_description + " on " + kAfter.First().event_date.ToShortDateString();
                    Status = ActionStatus.Invalid;
                    return Status;
                }
                var kBefore = _context.ACTION_TAKEN_HISTORY.Where(m => m.history_id != actionId && m.recordStatus == 0 && m.event_date < ti.inspection_date && m.equipmentid_auto == ti.equipmentid_auto).OrderByDescending(m => m.event_date);
                if (kBefore.Count() > 0 && Params.EquipmentInspection.inspection_date < kBefore.First().event_date)
                {
                    Message = "Operation not allowed! Inspection date should be after " + kBefore.First().TRACK_ACTION_TYPE.action_description + " on " + kBefore.First().event_date.ToShortDateString();
                    Status = ActionStatus.Invalid;
                    return Status;
                }
            }
            Status = ActionStatus.Valid;
            return Status;
        }

        public ActionStatus Commit()
        {
            MiddleOfAction();
            ActionLog += "Commiting the Operation ..." + Environment.NewLine;
            if (Status != ActionStatus.Valid)
            {
                ActionLog += "Operation Status is not valid" + Environment.NewLine;
                Status = ActionStatus.Failed;
                return Status;
            }

            //Including steps
            //1- Update inspection record in the database
            //2- Update records in Inspection Detail table for each component

            Params.EquipmentInspection.equipmentid_auto = _actionRecord.EquipmentId;
            Params.EquipmentInspection.inspection_date = _actionRecord.ActionDate;
            Params.EquipmentInspection.smu = _actionRecord.ReadSmuNumber;
            char evalOverAll = 'A';
            var impact = InspectionImpact.Low;
            try { impact = (InspectionImpact)Params.EquipmentInspection.impact; } catch (Exception ex) { string message = ex.Message; }
            foreach (var comp in Params.ComponentsInspection)
            {
                char eval;
                IComponent cmpn = new Component(_context, longNullableToint(comp.ComponentInspectionDetail.track_unit_auto));
                if (comp.ComponentInspectionDetail.tool_auto != null)
                    comp.ComponentInspectionDetail.worn_percentage = cmpn.CalcWornPercentage(comp.ComponentInspectionDetail.reading.ConvertFrom(MeasurementType.Milimeter), comp.ComponentInspectionDetail.tool_auto ?? 0, impact);
                cmpn.GetEvalCodeByWorn(comp.ComponentInspectionDetail.worn_percentage, out eval);
                comp.ComponentInspectionDetail.eval_code = eval.ToString();
                if (eval > evalOverAll)
                    evalOverAll = eval;
            }
            Params.EquipmentInspection.evalcode = evalOverAll.ToString();
            Params.EquipmentInspection.ActionHistoryId = actionLifeUpdate.ActionTakenHistory.history_id;
            Params.EquipmentInspection.ltd = _actionRecord.EquipmentActualLife;
            var currentTI = _context.TRACK_INSPECTION.Find(Params.EquipmentInspection.inspection_auto);
            if (currentTI == null)
            {
                Message = "Inspection not found to be updated!";
                return ActionStatus.Failed;
            }
            // TT-581 These feilds are updated directly in the page!! 
            // I added these lines to set the inspection paramter values to be as whatever is now in database
            Params.EquipmentInspection.LeftTrackSagImage = currentTI.LeftTrackSagImage;
            Params.EquipmentInspection.LeftTrackSagComment = currentTI.LeftTrackSagComment;
            Params.EquipmentInspection.RightTrackSagImage = currentTI.RightTrackSagImage;
            Params.EquipmentInspection.RightTrackSagComment = currentTI.RightTrackSagComment;
            Params.EquipmentInspection.LeftCannonExtensionImage = currentTI.LeftCannonExtensionImage;
            Params.EquipmentInspection.LeftCannonExtensionComment = currentTI.LeftCannonExtensionComment;
            Params.EquipmentInspection.RightCannonExtensionImage = currentTI.RightCannonExtensionImage;
            Params.EquipmentInspection.RightCannonExtensionComment = currentTI.RightCannonExtensionComment;
            // TT-581

            _context.Entry(currentTI).CurrentValues.SetValues(Params.EquipmentInspection);
            try
            {
                ActionLog += "Start Updating Inspection in TRACK_INSPECTION" + Environment.NewLine;
                _context.SaveChanges();
                UniqueId = currentTI.inspection_auto;
                ActionLog += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception e1)
            {
                ActionLog += "Failed to save!" + Environment.NewLine;
                ActionLog += "Error Details: " + e1.Message + Environment.NewLine;
                Status = ActionStatus.Failed;
                Message = "Inspection updating failed!";
                return Status;
            }

            foreach (var Comp in Params.ComponentsInspection)
            {
                Comp.ComponentInspectionDetail.inspection_auto = Params.EquipmentInspection.inspection_auto;
                var cmpntd = new Component(_context, longNullableToint(Comp.ComponentInspectionDetail.track_unit_auto));
                var kSys = cmpntd.DALSystem;
                //Comp.ComponentInspectionDetail.UCSystemId = kSys == null ? 0 : kSys.Module_sub_auto;
                Comp.ComponentInspectionDetail.hours_on_surface = cmpntd.GetComponentLifeMiddleOfNewAction(_actionRecord.ActionDate);
                var currentTID = _context.TRACK_INSPECTION_DETAIL.Find(Comp.ComponentInspectionDetail.inspection_detail_auto);
                if (currentTID == null)
                {
                    var currentTIDs = _context.TRACK_INSPECTION_DETAIL.Where(m => m.inspection_auto == Params.EquipmentInspection.inspection_auto && m.track_unit_auto == Comp.ComponentInspectionDetail.track_unit_auto);
                    if (currentTIDs.Count() == 1)
                        currentTID = currentTIDs.First();
                }
                if (currentTID != null)
                {
                    Comp.ComponentInspectionDetail.UCSystemId = currentTID.UCSystemId;
                    Comp.ComponentInspectionDetail.inspection_detail_auto = currentTID.inspection_detail_auto;
                    _context.Entry(currentTID).CurrentValues.SetValues(Comp.ComponentInspectionDetail);
                }
            }
            try
            {
                ActionLog += "Start updating range of inspection details to the TRACK_INSPECTION_DETAIL" + Environment.NewLine;
                _context.SaveChanges();
                ActionLog += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception e2)
            {
                ActionLog += "Failed to save!" + Environment.NewLine;
                ActionLog += "Error Details: " + e2.Message + Environment.NewLine;
                Status = ActionStatus.Failed;
                Message = "Save details range failed";
                return Status;
            }
            foreach (var Comp in Params.ComponentsInspection)
            {
                var si = _context.InspectionDetails_Side.Find(Comp.ComponentInspectionDetail.inspection_detail_auto);
                if (si != null)
                {
                    si.Side = Comp.side;
                    _context.Entry(si).State = EntityState.Modified;
                }
            }
            try
            {
                ActionLog += "Start updating range of inspection sides to the InspectionDetails_Side" + Environment.NewLine;
                _context.SaveChanges();
                ActionLog += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception e2)
            {
                ActionLog += "Failed to save!" + Environment.NewLine;
                ActionLog += "Error Details: " + e2.Message + Environment.NewLine;
                Status = ActionStatus.Failed;
                Message = "Save side for the component failed";
                return Status;
            }
            Available();
            Status = ActionStatus.Succeed;
            Message = "All done successfully";
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
            {
                rollBack();
                string str = "";
                if (removingActionRecord != null)
                {
                    UpdateEquipmentByAction(removingActionRecord, ref str);
                    var ti = _context.TRACK_INSPECTION.Find(Params.EquipmentInspection.inspection_auto);
                    if (ti != null && actionLifeUpdate != null && actionLifeUpdate.ActionTakenHistory != null)
                    {
                        ti.ActionHistoryId = actionLifeUpdate.ActionTakenHistory.history_id;
                        _context.Entry(ti).State = EntityState.Modified;
                        try
                        {
                            _context.SaveChanges();
                        }
                        catch (Exception e1)
                        {
                            string m = e1.Message;
                        }

                    }

                }
            }
            else
            {
                _gContext = new GETContext();
                rollBackGETAction(removingActionRecord, ref pvActionLog);
                UpdateGETByAction(_actionRecord, ref pvActionLog);
            }
            _context.Dispose();
        }
    }
}
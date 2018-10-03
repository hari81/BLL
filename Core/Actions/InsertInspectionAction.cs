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
    public class InsertInspectionAction : Domain.Action, IAction
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
        private InsertInspectionParams Params;
        public InsertInspectionAction(DbContext context, IEquipmentActionRecord actionRecord, InsertInspectionParams Paramteres)
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
                    dalActionRecord.action_type_auto = (int)ActionType.InsertInspection;
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
            if (Status == ActionStatus.Started)
            {
                if (_actionRecord.ActionDate > DateTime.Now.AddDays(1))
                {
                    Status = ActionStatus.Invalid;
                    Message = "Inspection date cannot be more than one day in the future!";
                    return Status;
                }
                if (_context.TRACK_INSPECTION.Where(m=>m.docket_no == Params.EquipmentInspection.docket_no && m.equipmentid_auto == Params.EquipmentInspection.equipmentid_auto).Count() > 0)
                {
                    Status = ActionStatus.Invalid;
                    Message = "This Docket No has been used for this Equipment! Please change Docket No.";
                    return Status;
                }
                var kAfter = _context.ACTION_TAKEN_HISTORY.Where(m => m.event_date > _actionRecord.ActionDate && m.recordStatus == 0 && m.equipmentid_auto == Params.EquipmentInspection.equipmentid_auto && (m.action_type_auto == (int)ActionType.ReplaceComponentWithNew || m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory)).OrderByDescending(m => m.event_date);
                if (kAfter.Count() > 0)
                {
                    Message = "Operation not allowed! Inspection date should be after " + kAfter.First().TRACK_ACTION_TYPE.action_description + " on " + kAfter.First().event_date.ToShortDateString();
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
            //1- Create a New inspection record in the database
            //2- Create a record in Inspection Detail table for each component

            var LogicalEquipment = new Equipment(_context, _actionRecord.EquipmentId);

            Params.EquipmentInspection.equipmentid_auto = _actionRecord.EquipmentId;
            Params.EquipmentInspection.inspection_date = _actionRecord.ActionDate;
            Params.EquipmentInspection.smu = _actionRecord.ReadSmuNumber;
            var impact = InspectionImpact.Low;
            try { impact = (InspectionImpact)Params.EquipmentInspection.impact; } catch(Exception ex) { string message = ex.Message; }
            char evalOverAll = 'A';
            foreach (var comp in Params.ComponentsInspection)
            {
                char eval;
                IComponent cmpn = new Component(_context, longNullableToint(comp.ComponentInspectionDetail.track_unit_auto));
                
                if(comp.ComponentInspectionDetail.tool_auto != null)
                    comp.ComponentInspectionDetail.worn_percentage = cmpn.CalcWornPercentage(comp.ComponentInspectionDetail.reading.ConvertFrom(MeasurementType.Milimeter), comp.ComponentInspectionDetail.tool_auto ?? 0, impact);
                //TT805    comp.ComponentInspectionDetail.worn_percentage = cmpn.CalcWornPercentage(comp.ComponentInspectionDetail.reading.ConvertFrom(MeasurementType.Milimeter), comp.ComponentInspectionDetail.tool_auto ?? 0, impact);
                
                cmpn.GetEvalCodeByWorn(comp.ComponentInspectionDetail.worn_percentage, out eval);
                comp.ComponentInspectionDetail.eval_code = eval.ToString();
                if (eval > evalOverAll)
                    evalOverAll = eval;
            }
            Params.EquipmentInspection.evalcode = evalOverAll.ToString();
            Params.EquipmentInspection.ActionHistoryId = actionLifeUpdate.ActionTakenHistory.history_id;
            Params.EquipmentInspection.ltd = _actionRecord.EquipmentActualLife;
            _context.TRACK_INSPECTION.Add(Params.EquipmentInspection);
            try
            {
                ActionLog += "Start saving New Inspection in TRACK_INSPECTION" + Environment.NewLine;
                _context.SaveChanges();
                UniqueId = Params.EquipmentInspection.inspection_auto;
                ActionLog += "Saved successfully." + Environment.NewLine;
            }
            catch (Exception e1)
            {
                ActionLog += "Failed to save!" + Environment.NewLine;
                ActionLog += "Error Details: " + e1.Message + Environment.NewLine;
                Status = ActionStatus.Failed;
                Message = "Inspection saving failed!";
                return Status;
            }
            
            foreach (var Comp in Params.ComponentsInspection)
            {
                Comp.ComponentInspectionDetail.inspection_auto = Params.EquipmentInspection.inspection_auto;
                var cmpntd = new Component(_context, longNullableToint(Comp.ComponentInspectionDetail.track_unit_auto));
                var kSys = cmpntd.DALSystem;
                Comp.ComponentInspectionDetail.UCSystemId = kSys == null ? 0 : kSys.Module_sub_auto;
                Comp.ComponentInspectionDetail.hours_on_surface = cmpntd.GetComponentLifeMiddleOfNewAction(_actionRecord.ActionDate);
                _context.TRACK_INSPECTION_DETAIL.Add(Comp.ComponentInspectionDetail);
                if(Comp.CompartAttachFileStreamImage != null)
                {
                    foreach (var streamImg in Comp.CompartAttachFileStreamImage)
                    {

                        streamImg.compartid_auto = cmpntd.DALComponent.compartid_auto;
                        streamImg.comparttype_auto = cmpntd.DALComponent.LU_COMPART.comparttype_auto;
                        streamImg.compart_attach_type_auto = cmpntd.DALComponent.side == 2 ? 4 : 3;
                        streamImg.entry_date = streamImg.entry_date;
                        streamImg.guid = streamImg.guid;
                        streamImg.inspection_auto = Params.EquipmentInspection.inspection_auto;
                        streamImg.position = cmpntd.DALComponent.pos;
                        streamImg.tool_auto = Comp.ComponentInspectionDetail.tool_auto;
                        streamImg.user_auto = _actionRecord.ActionUser.Id;
                        //"EquipmentSerial-CompartId-Side-Position",
                        streamImg.attachment_name = streamImg.attachment_name.Replace("EquipmentSerial", LogicalEquipment.DALEquipment.serialno).Replace("CompartId", cmpntd.DALComponent.LU_COMPART.compartid).Replace("Side", cmpntd.side.ToString()).Replace("Position", cmpntd.DALComponent.pos.ToString());
                        _context.COMPART_ATTACH_FILESTREAM.Add(streamImg);
                    }
                }
            }
            try
            {
                ActionLog += "Start saving range of inspection details to the TRACK_INSPECTION_DETAIL" + Environment.NewLine;
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
                _context.InspectionDetails_Side.Add(new InspectionDetails_Side { InspectionDetailsId = Comp.ComponentInspectionDetail.inspection_detail_auto, Side = Comp.side });
                if (Comp.ComponentInspectionDetail.Images != null)
                {
                    foreach (var img in Comp.ComponentInspectionDetail.Images)
                    {
                        img.inspection_detail_auto = Comp.ComponentInspectionDetail.inspection_detail_auto.ToString();
                        _context.Entry(img).State = EntityState.Modified;
                    }
                }
            }
            try
            {
                ActionLog += "Start saving range of inspection sides to the InspectionDetails_Side" + Environment.NewLine;
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
                rollBack();
            else
            {
                _gContext = new GETContext();
                UpdateGETByAction(_actionRecord, ref pvActionLog);
            }
            _context.Dispose();
        }
    }
}
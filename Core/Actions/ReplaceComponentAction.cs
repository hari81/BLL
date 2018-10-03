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
    public class ReplaceComponentAction : Domain.Action, IAction
    {
        private IEquipmentActionRecord pvActionRecord;
        private ActionStatus pvActionStatus;
        private string pvActionLog;
        private string pvMessage;
        private int pvUniqueId;
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
        private ReplaceComponentParams Params;
        public ReplaceComponentAction(DbContext context, IEquipmentActionRecord actionRecord, ReplaceComponentParams Parameters)
            : base(context)
        {
            Params = Parameters;
            _actionRecord = actionRecord;
            Status = ActionStatus.Close;
        }

        public UndercarriageContext _Replacecontext
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
                Params.oldgeuComponent = _context.GENERAL_EQ_UNIT.Find(Params.currentComponentId);
                if (_actionRecord != null && _actionRecord.Id != 0)
                {
                    Status = ActionStatus.Started;
                    Message = "Open successfully";
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
        /// </summary>
        /// <param name="OperationResult"></param>
        /// <returns></returns>
        public ActionStatus Validate()
        {
            ActionLog += "Starting validation ..." + Environment.NewLine;
            if (Status == ActionStatus.Started)
            {
                if (Params.oldgeuComponent == null)
                {
                    ActionLog += "Component not found!";
                    Status = ActionStatus.Invalid;
                    Message = "Component not found!";
                    return Status;
                }
                ActionLog += "Validate Component For Component Replacement started";
                //If there is any component replacement in the same type and position in the future, operation is not allowed
                if (!ValidateComponentForComponentReplacement())
                {
                    ActionLog += "Operation is not allowed! There is at least one component replacement with the same type and pos in the future!";
                    Status = ActionStatus.Invalid;
                    Message = "Operation is not allowed! There is at least one component replacement with the same type and pos in the future!";
                    return Status;
                }
                if (!ValidateForSystemReplacement())
                {
                    ActionLog += "Operation is not allowed! There is at least one system replacement with the same type and pos in the future!";
                    Status = ActionStatus.Invalid;
                    Message = "Operation is not allowed! There is at least one component replacement with the same type and pos in the future!";
                    return Status;
                }
                Status = ActionStatus.Valid;
                Message = "Validation succeed!";
                return Status;
            }
            return Status;
        }
        /// <summary>
        /// This method validates the component and if there is any component replacement with the same compart type and pos in the future 
        /// and if there is any replacement will return false which means operation is not allowed
        /// </summary>
        /// <returns>Validatation result as bool</returns>
        private bool ValidateComponentForComponentReplacement()
        {
            int typeId = Params.oldgeuComponent.LU_COMPART != null ? Params.oldgeuComponent.LU_COMPART.comparttype_auto : 0;
            int pos = Params.oldgeuComponent.pos != null ? (int)Params.oldgeuComponent.pos : 0;
            //If no valid information is available we should let them do it ans consider as valid
            if (pos == 0 || typeId == 0)
                return true;
            var componentReplaces = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == _actionRecord.EquipmentId && m.recordStatus == 0 && m.event_date > _actionRecord.ActionDate && m.action_type_auto == (int)ActionType.ReplaceComponentWithNew).ToList();
            bool isValid = true;
            foreach (var replace in componentReplaces)
            {
                if (replace.equnit_auto != 0)
                {
                    var g = _context.GENERAL_EQ_UNIT.Find(replace.equnit_auto);
                    if (g != null && g.LU_COMPART != null && g.LU_COMPART.comparttype_auto == typeId && g.pos == pos)
                        isValid = false;
                }
            }
            return isValid;
        }
        /// <summary>
        /// If there is any system replacement in the future on the same side with the same system type
        /// Opeation is not allowed
        /// </summary>
        /// <returns></returns>
        private bool ValidateForSystemReplacement()
        {
            var systemReplacements = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == _actionRecord.EquipmentId && m.recordStatus == 0 && m.event_date > _actionRecord.ActionDate && m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory).ToList();
            var logicalCurrentSystem = new UCSystem(_context, longNullableToint(Params.oldgeuComponent.module_ucsub_auto));
            //If we cannot find the current system we cannot validate the operation and operation should be continued
            if (logicalCurrentSystem.Id == 0)
                return true;
            bool isValid = true;
            foreach (var replace in systemReplacements)
            {
                if (replace.system_auto_id != null)
                {
                    var k = new UCSystem(_context, (int)replace.system_auto_id);
                    if (k.Id != 0)
                    {
                        if (logicalCurrentSystem.side == k.side && logicalCurrentSystem.SystemType == k.SystemType)
                            isValid = false;
                    }
                }
            }
            return isValid;
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

            //Including Steps:
            //Step1 Detach the old component
            //Step2 Create a new component attached to this Equipment and set system as well
            //Step3 Update ActionRecord

            //Step1
            ActionLog += "Adding New Component ..." + Environment.NewLine;
            GENERAL_EQ_UNIT geuNew = new GENERAL_EQ_UNIT
            {
                equipmentid_auto = _actionRecord.EquipmentId,
                module_ucsub_auto = Params.oldgeuComponent.module_ucsub_auto,
                date_installed = _actionRecord.ActionDate,
                created_user = _actionRecord.ActionUser.userStrId,
                compartid_auto = Params.newComponent.CompartId,
                created_date = _actionRecord.ActionDate,
                comp_status = (byte)Params.newComponent.ComponentStatus,
                pos = Params.oldgeuComponent.pos,
                side = Params.oldgeuComponent.side,
                eq_ltd_at_install = _actionRecord.EquipmentActualLife,
                track_0_worn = 0,
                track_100_worn = 0,
                track_120_worn = 0,
                track_budget_life = Params.newComponent.BudgetedLife,
                cmu = Params.newComponent.ComponentLifeAtInstall,
                cost = Params.newComponent.Cost,
                eq_smu_at_install = _actionRecord.ReadSmuNumber,
                smu_at_install = _actionRecord.ReadSmuNumber,
                system_LTD_at_install = GetSystemLife(longNullableToint(Params.oldgeuComponent.equnit_auto), _actionRecord.ActionDate),
                component_current_value = 0,
                variable_comp = false,
                insp_uom = 0,
                make_auto = Params.newComponent.brandId == 0 ? null : (int?)Params.newComponent.brandId
            };
            _context.GENERAL_EQ_UNIT.Add(geuNew);
            try
            {
                ActionLog += "Save new component to the database and get the new Id ..." + Environment.NewLine;
                _context.SaveChanges();
                ActionLog += "Save new component succeed" + Environment.NewLine;
            }
            catch (Exception e)
            {
                ActionLog += "Save new component FAILED : " + e.Message;
                Status = ActionStatus.Failed;
                Message = "Save new component FAILED";
                return Status;
            }

            //Step3 Update action record to have component fields
            ActionLog += "Updating Action History ..." + Environment.NewLine;
            var dalActionRecord = _context.ACTION_TAKEN_HISTORY.Find(_actionRecord.Id);
            //Now the Action Taken History Table should be updated to show actions details
            dalActionRecord.compartid_auto = Params.oldgeuComponent.compartid_auto;

            if (new Compart(_context).isAChild(Params.oldgeuComponent.compartid_auto)) //TT-610 -> if is a child then don't reference the old component because it would cause duplicate records in history page
            {
                dalActionRecord.equnit_auto = -(Params.oldgeuComponent.equnit_auto);
                dalActionRecord.equnit_auto_new = -geuNew.equnit_auto;
            }
            else
            {
                dalActionRecord.equnit_auto = Params.oldgeuComponent.equnit_auto;
                dalActionRecord.equnit_auto_new = geuNew.equnit_auto;
            }

            dalActionRecord.action_type_auto = (int)ActionType.ReplaceComponentWithNew;
            dalActionRecord.cmu = (int)Params.oldgeuComponent.cmu;
            dalActionRecord.cost = (long)_actionRecord.Cost;
            
            dalActionRecord.system_auto_id = Params.oldgeuComponent.module_ucsub_auto;
            dalActionRecord.recordStatus = (int)RecordStatus.Available;
            _context.Entry(dalActionRecord).State = System.Data.Entity.EntityState.Modified;
            //Update all life records of the old component to be for the new component
            var componentLifesAfterChangeOut = _context.COMPONENT_LIFE.Where(m => m.ComponentId == Params.oldgeuComponent.equnit_auto && m.ActionDate > _actionRecord.ActionDate).ToList();
            foreach (var life in componentLifesAfterChangeOut)
            {
                var athistoryFuture = _context.ACTION_TAKEN_HISTORY.Find(life.ActionId);
                int EqLifeInFutureAction = 0;
                int EqSMUInFutureAction = 0;
                if (athistoryFuture != null)
                {
                    if (athistoryFuture.equnit_auto == Params.oldgeuComponent.equnit_auto)
                    {
                        athistoryFuture.equnit_auto = geuNew.equnit_auto;
                        _context.Entry(athistoryFuture).State = EntityState.Modified;
                    }
                    EqLifeInFutureAction = athistoryFuture.equipment_ltd;
                    EqSMUInFutureAction = athistoryFuture.equipment_smu;
                }
                int newComponentActualLife = 0;
                if (EqLifeInFutureAction != 0 && EqLifeInFutureAction > _actionRecord.EquipmentActualLife)
                {
                    newComponentActualLife = Params.newComponent.ComponentLifeAtInstall + (EqLifeInFutureAction - _actionRecord.EquipmentActualLife);
                }
                else if (EqSMUInFutureAction != 0 && EqSMUInFutureAction > _actionRecord.ReadSmuNumber)
                {
                    newComponentActualLife = Params.newComponent.ComponentLifeAtInstall + (EqSMUInFutureAction - _actionRecord.ReadSmuNumber);
                }
                if (newComponentActualLife == 0)
                    newComponentActualLife = Params.newComponent.ComponentLifeAtInstall;

                life.ActualLife = newComponentActualLife;
                life.ComponentId = geuNew.equnit_auto;
                life.Title = "Updated because of component replacement in the past";
                _context.Entry(life).State = EntityState.Modified;
            }

            //Step4 Update all inspections after this change out 
            var inspections = _context.EQUIPMENTs.Find(_actionRecord.EquipmentId).TRACK_INSPECTION.Where(m => m.inspection_date > _actionRecord.ActionDate).ToList();
            var logicalEquipment = new Equipment(_context, _actionRecord.EquipmentId);
            foreach (var inspect in inspections)
            {
                var details = inspect.TRACK_INSPECTION_DETAIL.Where(m => m.track_unit_auto == Params.oldgeuComponent.equnit_auto).ToList();
                foreach (var d in details)
                {
                    var cmpnt = new Component(_context, longNullableToint(geuNew.equnit_auto));
                    InspectionImpact impact = d.TRACK_INSPECTION.impact == 2 ? InspectionImpact.High : InspectionImpact.Low;
                    int ToolId = d.tool_auto == null ? 0 : (int)d.tool_auto;
                    if (cmpnt.Id != 0)
                    {
                        d.worn_percentage = cmpnt.CalcWornPercentage(ConvertFrom(MeasurementType.Milimeter,d.reading), ToolId, impact);
                        char evl = '-';
                        if (cmpnt.GetEvalCodeByWorn(d.worn_percentage, out evl))
                            d.eval_code = evl.ToString();
                        d.hours_on_surface = cmpnt.GetComponentLife(inspect.inspection_date);
                    }
                    // Update images stored in the old table to reference the new compartid_auto if the component type has changed
                    // with the new replacement. Otherwise the image will not be viewable in the image popup. 
                    var images = _context.COMPART_ATTACH_FILESTREAM.Where(f => f.compartid_auto == Params.oldgeuComponent.compartid_auto)
                                                                  .Where(f => f.position == Params.oldgeuComponent.pos)
                                                                  .Where(f => f.inspection_auto == d.inspection_auto);
                    foreach (var i in images) {
                        i.compartid_auto = geuNew.compartid_auto;
                        _context.Entry(i).State = EntityState.Modified;
                    }

                    d.track_unit_auto = geuNew.equnit_auto;
                    _context.Entry(d).State = System.Data.Entity.EntityState.Modified;
                }
                logicalEquipment.UpdateMiningShovelInspectionParentsFromChildren(inspect.inspection_auto);
            }

            Params.oldgeuComponent.equipmentid_auto = null;
            Params.oldgeuComponent.module_ucsub_auto = null;
            Params.oldgeuComponent.modified_user = _actionRecord.ActionUser.userStrId;
            _context.Entry(Params.oldgeuComponent).State = System.Data.Entity.EntityState.Modified;

            try
            {
                ActionLog += "Save all changes to the database and get the new Id ..." + Environment.NewLine;
                _context.SaveChanges();
                ActionLog += "Save all succeeded" + Environment.NewLine;
            }
            catch (Exception e)
            {
                ActionLog += "Save all changes FAILED : " + e.Message;
                Status = ActionStatus.Failed;
                Message = "Save all changes FAILED ";
                return Status;
            }
            //Add a life record for the new component
            ActionLog += "Adding Life of the new component";
            DAL.ComponentLife NewComponentLife = new ComponentLife
            {
                ActionDate = _actionRecord.ActionDate,
                ActionId = _actionRecord.Id,
                ActualLife = longNullableToint(geuNew.cmu),
                ComponentId = geuNew.equnit_auto,
                Title = "New component added by replace Action",
                UserId = _actionRecord.ActionUser.Id
            };
            _context.COMPONENT_LIFE.Add(NewComponentLife);
            try
            {
                ActionLog += "Adding Life of the new component";
                _context.SaveChanges();
                ActionLog += "Adding Life of the new component Succeed";
            }
            catch(Exception e1)
            {
                ActionLog += "Adding Life of the new component Filed "+e1.Message;
                string Message = e1.Message;
            }
            UniqueId = geuNew.equnit_auto.LongNullableToInt();
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
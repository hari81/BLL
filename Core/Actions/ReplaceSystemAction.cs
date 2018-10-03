using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using BLL.Persistence.Repositories;
using BLL.Core.Domain;
using System.Data.Entity;
using DAL;

namespace BLL.Core.Repositories
{
    public class ReplaceSystemAction : Domain.Action, IAction
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
        private ReplaceSystemParams Params;
        private LU_Module_Sub DALOldSystem;
        private LU_Module_Sub DALNewSystem;
        private UCSystem logicalOldSystem;
        private UCSystem logicalNewSystem;
        private LU_MMTA MMTA;
        public ReplaceSystemAction(DbContext context, IEquipmentActionRecord actionRecord, ReplaceSystemParams Parameters)
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
                DALOldSystem = _context.LU_Module_Sub.Find(Params.OldSystemId);
                DALNewSystem = _context.LU_Module_Sub.Find(Params.NewSystemId);
                logicalOldSystem = new UCSystem(_context, Params.OldSystemId,true);
                logicalNewSystem = new UCSystem(_context, Params.NewSystemId,true);
                if (_actionRecord != null && _actionRecord.Id != 0)
                {
                    //Step3 Update action record to have component fields
                    ActionLog += "Updating Action History ..." + Environment.NewLine;
                    var dalActionRecord = _context.ACTION_TAKEN_HISTORY.Find(_actionRecord.Id);
                    //TRACK_ACTION_TYPE Table should be updated to show actions related to the new actions and previous ones were unusable 
                    dalActionRecord.action_type_auto = (int)ActionType.ReplaceSystemFromInventory;
                    dalActionRecord.system_auto_id = logicalOldSystem.Id;
                    _context.Entry(dalActionRecord).State = System.Data.Entity.EntityState.Modified;
                    Status = ActionStatus.Started;
                    Message = "Started successfully";
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
            ActionLog += "Starting validation ..." + Environment.NewLine;
            if (Status == ActionStatus.Started)
            {
                if (DALOldSystem == null)
                {
                    ActionLog += "System to be replaced not found!";
                    Status = ActionStatus.Invalid;
                    Message = "System to be replaced not found!";
                    return Status;
                }
                if (DALNewSystem == null)
                {
                    ActionLog += "Replacing system not found!";
                    Status = ActionStatus.Invalid;
                    Message = "Replacing system not found!";
                    return Status;
                }
                if (logicalOldSystem == null || logicalOldSystem.Id == 0 || logicalOldSystem.DALEquipment == null)
                {
                    ActionLog += "System to be replaced is not correct!";
                    Status = ActionStatus.Invalid;
                    Message = "System to be replaced is not correct!";
                    return Status;
                }
                if (logicalNewSystem == null || logicalNewSystem.Id == 0)
                {
                    ActionLog += "System to to replace the old one is not found!";
                    Status = ActionStatus.Invalid;
                    Message = "System to to replace the old one is not found!";
                    return Status;
                }
                //If there is a system replacement in the same side and same system type on this equipment in the future, operation is not allowed
                var replaceActions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == DALOldSystem.equipmentid_auto && m.recordStatus == 0 && m.event_date > _actionRecord.ActionDate && m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory).ToList();
                bool isAny = false;
                foreach (var replace in replaceActions)
                {
                    if(replace.system_auto_id != null)
                    {
                        var k = new UCSystem(_context, (int)replace.system_auto_id);
                        if(k.Id != 0)
                        {
                            if (logicalOldSystem.side == k.side && logicalOldSystem.SystemType == k.SystemType)
                                isAny = true;
                        }
                    }
                }
                if(isAny)
                {
                    ActionLog += "Operation is not allowed! At least one system replacement for the system type and side is recorded for a future date!";
                    Status = ActionStatus.Invalid;
                    Message = "Operation is not allowed! At least one system replacement for the system type and side is recorded for a future date!";
                    return Status;
                }
                MMTA = _context.LU_MMTA.Find(logicalOldSystem.DALEquipment.mmtaid_auto);
                if (MMTA == null)
                {
                    ActionLog += "Make and model of the equipment couln'd be found";
                    Status = ActionStatus.Invalid;
                    Message = "Make and model of the equipment couln'd be found";
                    return Status;
                }

                Status = ActionStatus.Valid;
                Message = "Validation succeed";
                return Status;
            }
            return Status;
        }
        private Component GetComponentBasedOnTypeAndPositionFromOldSystemToTheNewOne(long OldComponentId)
        {
            GENERAL_EQ_UNIT OldDALcmpnt = _context.GENERAL_EQ_UNIT.Find(OldComponentId);
            if (OldDALcmpnt == null)
                return null;
            Component Newcmpnt = null;

            //Finding the component which was in the same type and place of the old component
            foreach (var geuNewComp in logicalNewSystem.Components)
            {
                if (Newcmpnt == null && geuNewComp.LU_COMPART.comparttype_auto == OldDALcmpnt.LU_COMPART.comparttype_auto && geuNewComp.pos == OldDALcmpnt.pos)
                    Newcmpnt = new Component(_context, longNullableToint(geuNewComp.equnit_auto));
            }
            return Newcmpnt;
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
            int tempEQId = longNullableToint(DALOldSystem.equipmentid_auto);

            DALNewSystem.equipmentid_auto = tempEQId;
            DALNewSystem.crsf_auto = logicalOldSystem.DALEquipment.crsf_auto;
            DALNewSystem.equipment_LTD_at_attachment = _actionRecord.EquipmentActualLife;
            DALNewSystem.modifiedDate = _actionRecord.ActionDate.ToLocalTime().Date;
            DALNewSystem.SMU_at_install = _actionRecord.ReadSmuNumber;

            //When systems are created these values are not added!!!
            //So when system is going to be placed in inventory, values are set
            DALOldSystem.crsf_auto = logicalOldSystem.DALEquipment.crsf_auto;
            DALOldSystem.system_LTD_on_removal = logicalOldSystem.GetSystemLife(_actionRecord.ActionDate);
            DALOldSystem.make_auto = MMTA.make_auto;
            DALOldSystem.model_auto = MMTA.model_auto;
            DALOldSystem.type_auto = MMTA.type_auto;
            DALOldSystem.equipmentid_auto = null;

            //Update any recorded life for the old system which actualy should belong to the new one
            var systemRecordedLifes = _context.UCSYSTEM_LIFE.Where(m => m.SystemId == logicalOldSystem.Id && m.ActionDate > _actionRecord.ActionDate).ToList();
            foreach(var life in systemRecordedLifes)
            {
                var athistoryFuture = _context.ACTION_TAKEN_HISTORY.Find(life.ActionId);
                int EqLifeInFutureAction = 0;
                int EqSMUInFutureAction = 0;
                if (athistoryFuture != null)
                {
                    if (athistoryFuture.system_auto_id == DALOldSystem.Module_sub_auto)
                    {
                        athistoryFuture.system_auto_id = DALNewSystem.Module_sub_auto;
                        _context.Entry(athistoryFuture).State = EntityState.Modified;
                    }
                    EqLifeInFutureAction = athistoryFuture.equipment_ltd;
                    EqSMUInFutureAction = athistoryFuture.equipment_smu;
                }
                int newSystemActualLife = logicalNewSystem.GetSystemLife(_actionRecord.ActionDate);
                
                if (EqLifeInFutureAction != 0 && EqLifeInFutureAction > _actionRecord.EquipmentActualLife)
                {
                    newSystemActualLife += (EqLifeInFutureAction - _actionRecord.EquipmentActualLife);
                }
                else if (EqSMUInFutureAction != 0 && EqSMUInFutureAction > _actionRecord.ReadSmuNumber)
                {
                    newSystemActualLife += (EqSMUInFutureAction - _actionRecord.ReadSmuNumber);
                }

                life.ActualLife = newSystemActualLife;
                life.SystemId = logicalNewSystem.Id;
                life.Title = "Updated because of system replacement in the past";
                _context.Entry(life).State = EntityState.Modified;
            }
            foreach (var Comp in logicalOldSystem.Components)
            {
                //Update all life records of the old components which belongs to the new components
                var componentRecordedLifes = _context.COMPONENT_LIFE.Where(m => m.ComponentId == Comp.equnit_auto && m.ActionDate > _actionRecord.ActionDate).ToList();
                foreach(var life in componentRecordedLifes)
                {
                    var newcmpnt = GetComponentBasedOnTypeAndPositionFromOldSystemToTheNewOne(Comp.equnit_auto);
                    //         ↓↓↓↓↓     Means there is no component with the same type and position on the new system 
                    if (newcmpnt == null)
                        continue;
                    var athistoryFuture = _context.ACTION_TAKEN_HISTORY.Find(life.ActionId);
                    int EqLifeInFutureAction = 0;
                    int EqSMUInFutureAction = 0;
                    //     ↓↓↓↓↓ Updating a field in the ACTION_TAKEN_HISTORY Table to point to the new component
                    if (athistoryFuture != null)
                    {
                        if (athistoryFuture.equnit_auto == Comp.equnit_auto)
                        {
                            athistoryFuture.equnit_auto = newcmpnt.Id;
                            _context.Entry(athistoryFuture).State = EntityState.Modified;
                        }
                        EqLifeInFutureAction = athistoryFuture.equipment_ltd;
                        EqSMUInFutureAction = athistoryFuture.equipment_smu;
                    }
                    int newComponentActualLife = newcmpnt.GetComponentLife(_actionRecord.ActionDate);

                    if (EqLifeInFutureAction != 0 && EqLifeInFutureAction > _actionRecord.EquipmentActualLife)
                    {
                        newComponentActualLife += (EqLifeInFutureAction - _actionRecord.EquipmentActualLife);
                    }
                    else if (EqSMUInFutureAction != 0 && EqSMUInFutureAction > _actionRecord.ReadSmuNumber)
                    {
                        newComponentActualLife += (EqSMUInFutureAction - _actionRecord.ReadSmuNumber);
                    }

                    life.ActualLife = newComponentActualLife;
                    life.ComponentId = newcmpnt.Id;
                    life.Title = "Updated because of system replacement in the past";
                    _context.Entry(life).State = EntityState.Modified;
                }
                Comp.equipmentid_auto = null;
                //This line is Commented because of an issue in the inspection page.
                //Comp.cmu = GetComponentLife(longNullableToint(Comp.equnit_auto), _actionRecord.ActionDate);
                _context.Entry(Comp).State = EntityState.Modified;
            }

            foreach (var Comp in logicalNewSystem.Components)
            {
                Comp.equipmentid_auto = DALNewSystem.equipmentid_auto;
                Comp.side = (byte?)logicalOldSystem.side;
                Comp.eq_smu_at_install = GetEquipmentSerialMeterUnit(tempEQId, _actionRecord.ActionDate);
                Comp.system_LTD_at_install = logicalNewSystem.GetSystemLife(_actionRecord.ActionDate);
                Comp.eq_ltd_at_install = GetEquipmentLife(tempEQId, _actionRecord.ActionDate);
                //Comp.cmu = GetComponentLife(longNullableToint(Comp.equnit_auto), _actionRecord.ActionDate);
                Comp.smu_at_install = Comp.eq_smu_at_install;
                Comp.date_installed = _actionRecord.ActionDate;
                _context.Entry(Comp).State = EntityState.Modified;
            }

            //Step3 Update action record to have system fields
            ActionLog += "Updating Action History ..." + Environment.NewLine;
            var dalActionRecord = _context.ACTION_TAKEN_HISTORY.Find(_actionRecord.Id);
            //TRACK_ACTION_TYPE Table should be updated to show actions related to the new actions 
            dalActionRecord.action_type_auto = (int)ActionType.ReplaceSystemFromInventory;
            dalActionRecord.cost = (long)_actionRecord.Cost;
            dalActionRecord.system_auto_id = Params.OldSystemId;
            dalActionRecord.system_auto_id_new = Params.NewSystemId;
            dalActionRecord.recordStatus = (int)RecordStatus.Available;
            _context.Entry(dalActionRecord).State = System.Data.Entity.EntityState.Modified;

            //Update all inspections after this change out 
            var inspections = _context.EQUIPMENTs.Find(_actionRecord.EquipmentId).TRACK_INSPECTION.Where(m => m.inspection_date > _actionRecord.ActionDate).ToList();
            foreach (var inspect in inspections)
            {
                var details = inspect.TRACK_INSPECTION_DETAIL.Where(m => m.UCSystemId == Params.OldSystemId).ToList();
                foreach (var d in details)
                {
                    var Newcmpnt = GetComponentBasedOnTypeAndPositionFromOldSystemToTheNewOne(d.track_unit_auto);
                    // Does the new component found in the inspection details?
                    if (Newcmpnt != null)
                    {
                        InspectionImpact impact = d.TRACK_INSPECTION.impact == 2 ? InspectionImpact.High : InspectionImpact.Low;
                        int ToolId = d.tool_auto == null ? 0 : (int)d.tool_auto;
                        if (Newcmpnt.Id != 0)
                        {
                            d.worn_percentage = Newcmpnt.CalcWornPercentage(ConvertFrom(MeasurementType.Milimeter, d.reading), ToolId, impact);
                            char evl = '-';
                            if (Newcmpnt.GetEvalCodeByWorn(d.worn_percentage, out evl))
                                d.eval_code = evl.ToString();
                            d.hours_on_surface = Newcmpnt.GetComponentLife(inspect.inspection_date);
                        }
                        d.track_unit_auto = Newcmpnt.Id;
                        d.UCSystemId = logicalNewSystem.Id;
                        _context.Entry(d).State = EntityState.Modified;
                    }
                }
                try
                {
                    ActionLog += "Save inspection changes to the database ..." + Environment.NewLine;
                    _context.SaveChanges();
                    ActionLog += "Save all succeeded" + Environment.NewLine;
                }
                catch (Exception e)
                {
                    ActionLog += "Save inspection changes FAILED : " + e.Message;
                    Status = ActionStatus.Failed;
                    Message = "Save inspection changes FAILED ";
                    return Status;
                }
                new Equipment(_context, (int)inspect.equipmentid_auto).UpdateMiningShovelInspectionParentsFromChildren(inspect.inspection_auto);
            }
            _context.Entry(DALNewSystem).State = EntityState.Modified;
            _context.Entry(DALOldSystem).State = EntityState.Modified;
            try
            {
                ActionLog += "Save all changes to the database ..." + Environment.NewLine;
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
            //Add life for new system and all new components
            foreach (var Comp in logicalNewSystem.Components)
            {
                ActionLog += "Adding Life of the new component";
                DAL.ComponentLife NewComponentLife = new ComponentLife
                {
                    ActionDate = _actionRecord.ActionDate,
                    ActionId = _actionRecord.Id,
                    ActualLife = longNullableToint(Comp.cmu),
                    ComponentId = Comp.equnit_auto,
                    Title = "New component life added by replace System Action",
                    UserId = _actionRecord.ActionUser.Id
                };
                _context.COMPONENT_LIFE.Add(NewComponentLife);
            }
            //Add life for the new system
            SystemLife NewSystemLife = new SystemLife
            {
                ActionDate = _actionRecord.ActionDate,
                ActionId = _actionRecord.Id,
                SystemId = logicalNewSystem.Id,
                Title = "New System life added by replace System Action",
                UserId = _actionRecord.ActionUser.Id,
                ActualLife = logicalNewSystem.GetSystemLife(_actionRecord.ActionDate)
            };
            _context.UCSYSTEM_LIFE.Add(NewSystemLife);
            try
            {
                ActionLog += "Adding Life of the new components nad system";
                _context.SaveChanges();
                ActionLog += "Adding Life of the new components and system Succeeded";
            }
            catch (Exception e1)
            {
                ActionLog += "Adding Life of the new components Filed " + e1.Message;
                string Message = e1.Message;
            }


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
            if(Status != ActionStatus.Succeed)
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using DAL;
using BLL.Persistence.Repositories;
using System.Data.Entity;
using BLL.Core.Repositories;
using BLL.Core.Actions;
using BLL.Extensions;
using BLL.Core.ViewModel;

namespace BLL.Core.Domain
{
    public class Action : IDisposable
    {
        public IAction Operation;
        protected UndercarriageContext _context;
        protected GETContext _gContext;
        protected IActionLifeUpdate actionLifeUpdate;
        protected string Message = "";
        private ACTION_TAKEN_HISTORY DALRecord;
        private DAL.EQUIPMENT DALEquipment;
        protected IEquipmentActionRecord _current;

        public Action(DbContext context)
        {
            _context = (UndercarriageContext)context;
        }
        public Action(GETContext context)
        {
            _gContext = context;
        }
        public Action(GETContext gContext, UndercarriageContext UCContext)
        {
            _gContext = gContext;
            _context = UCContext;
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, InsertInspectionParams Parameters)
        {
            _context = (UndercarriageContext)context;
            Operation = new InsertInspectionAction(context, actionRecord, Parameters);
        }

        public Action(DbContext context, IEquipmentActionRecord actionRecord, IGeneralInspectionModel Parameters)
        {
            _context = (UndercarriageContext)context;
            _current = actionRecord;
            if (Parameters.Id == 0)
                Operation = new InsertInspectionGeneralAction(context, actionRecord, Parameters);
            else
                Operation = new UpdateInspectionGeneralAction(context, actionRecord, Parameters);
        }

        public Action(DbContext context, IEquipmentActionRecord actionRecord)
        {
            _context = (UndercarriageContext)context;
            Operation = new SMUReadingAction(context, actionRecord);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, UpdateInspectionParams Parameters)
        {
            _context = (UndercarriageContext)context;
            Operation = new UpdateInspectionAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, ReplaceComponentParams Parameters)
        {
            _context = (UndercarriageContext)context;
            Operation = new ReplaceComponentAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, ReplaceSystemParams Parameters)
        {
            _context = (UndercarriageContext)context;
            Operation = new ReplaceSystemAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, InstallComponentOnSystemParams Parameters)
        {
            _context = (UndercarriageContext)context;
            Operation = new InstallComponentOnSystemAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, InstallSystemParams Parameters)
        {
            _context = (UndercarriageContext)context;
            Operation = new InstallSystemAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, ChangeMeterUnitParams Parameters)
        {
            _context = (UndercarriageContext)context;
            Operation = new ChangeMeterUnitAction(context, actionRecord, Parameters);
        }
        // GET Specific Actions
        public Action(DbContext context, IEquipmentActionRecord actionRecord, GETInspectionParams Parameters)
        {
            _gContext = (GETContext)context;
            Operation = new BLL.GETCore.Repositories.GETInspectionAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, DbContext gContext, IEquipmentActionRecord actionRecord, GETEquipmentSetupParams Parameters)
        {
            _context = (UndercarriageContext)context;
            _gContext = (GETContext)gContext;
            if (Parameters.IsUpdating)
            {
                Operation = new BLL.Core.Actions.UpdateEquipmentSetupAction(context, actionRecord, Parameters);
            }
            else
            {
                Operation = new BLL.GETCore.Repositories.GETEquipmentSetupAction(context, gContext, actionRecord, Parameters);
            }
        }

        public Action(UndercarriageContext context, IEquipmentActionRecord actionRecord, SetupViewModel Parameters)
        {
            _context = context;
            Operation = new UpdateUndercarriageSetupAction(_context, actionRecord, Parameters);
        }
        public Action(DbContext context, DbContext gContext, IEquipmentActionRecord actionRecord, GETImplementSetupParams Parameters)
        {
            _context = (UndercarriageContext)context;
            _gContext = (GETContext)gContext;
            Operation = new BLL.GETCore.Repositories.GETImplementSetupAction(context, gContext, actionRecord, Parameters);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, GETComponentReplacementParams Parameters)
        {
            _gContext = (GETContext)context;
            Operation = new BLL.GETCore.Repositories.GETComponentReplacementAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, GETUndoComponentReplacementParams Parameters)
        {
            _gContext = (GETContext)context;
            Operation = new BLL.GETCore.Repositories.GETUndoComponentReplacementAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, IEquipmentActionRecord actionRecord, GETFlagIgnoredParams Parameters)
        {
            _gContext = (GETContext)context;
            Operation = new BLL.GETCore.Repositories.GETFlagIgnoredAction(context, actionRecord, Parameters);
        }
        public Action(DbContext context, DbContext gContext, IEquipmentActionRecord actionRecord, AttachImplementToEquipmentParams Parameters)
        {
            _context = (UndercarriageContext)context;
            _gContext = (GETContext)gContext;
            Operation = new BLL.GETCore.Repositories.AttachImplementToEquipmentAction(context, gContext, actionRecord, Parameters);
        }
        public Action(DbContext context, DbContext gContext, IEquipmentActionRecord actionRecord, MoveImplementToInventoryParams Parameters)
        {
            _context = (UndercarriageContext)context;
            _gContext = (GETContext)gContext;
            Operation = new BLL.GETCore.Repositories.MoveImplementToInventoryAction(context, gContext, actionRecord, Parameters);
        }


        protected int longNullableToint(long? number)
        {
            if (number == null)
                return 0;
            if (number > Int32.MaxValue) //:) So Stupid if the number is bigger 
                return Int32.MaxValue;
            if (number < Int32.MinValue) // :))
                return Int32.MinValue;
            try { return (int)number; } catch { return 0; }
        }

        protected int GetEquipmentLife(int Id, DateTime date, bool MiddleOfAction = false)
        {
            var Eq = _context.EQUIPMENTs.Find(Id);
            if (Eq == null)
                return -1;
            var lifes = Eq.Life.Where(m => m.ActionDate <= date && (
            (!MiddleOfAction && m.Action.recordStatus == (int)RecordStatus.Available) 
            || (MiddleOfAction && (m.Action.recordStatus == (int)RecordStatus.Available || m.Action.recordStatus == (int)RecordStatus.MiddleOfAction)))).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return longNullableToint(Eq.LTD_at_start);
        }

        protected int GetEquipmentSerialMeterUnit(int Id, DateTime date, bool MiddleOfAction = false)
        {
            var Eq = _context.EQUIPMENTs.Find(Id);
            if (Eq == null)
                return -1;
            var lifes = Eq.Life.Where(m => m.ActionDate <= date && (
            (!MiddleOfAction && m.Action.recordStatus == (int)RecordStatus.Available)
            || (MiddleOfAction && (m.Action.recordStatus == (int)RecordStatus.Available || m.Action.recordStatus == (int)RecordStatus.MiddleOfAction)))).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().SerialMeterReading;

            return longNullableToint(Eq.smu_at_start);
        }
        protected int GetNextEquipmentSerialMeterUnit(int Id, DateTime date)
        {
            var Eq = _context.EQUIPMENTs.Find(Id);
            if (Eq == null)
                return int.MaxValue;
            var lifes = Eq.Life.Where(m => m.ActionDate > date && m.Action.recordStatus == (int)RecordStatus.Available).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.First().SerialMeterReading;

            return int.MaxValue;
        }
        protected int GetSystemLife(int Id, DateTime date, bool MiddleOfAction = false)
        {
            var UCsys = _context.LU_Module_Sub.Find(Id);
            if (UCsys == null)
                return -1;
            var lifes = UCsys.Life.Where(m => m.ActionDate <= date && (
            (!MiddleOfAction && m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.Available)
            || (MiddleOfAction && (m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.Available || m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.MiddleOfAction)))).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return UCsys.CMU.LongNullableToInt();
        }

        protected int GetComponentLife(int Id, DateTime date)
        {
            var Comp = _context.GENERAL_EQ_UNIT.Find(Id);
            if (Comp == null)
                return -1;
            var lifes = Comp.Life.Where(m => m.ActionDate <= date && m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.Available).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return (int)(Comp.cmu ?? 0); // GetComponentLifeOldMethod(Id, date);
        }

        private int GetComponentLifeOldMethod(int Id, DateTime date)
        {
            var geucomp = _context.GENERAL_EQ_UNIT.Find(Id);
            if (Id == 0 || geucomp == null)
                return 0;
            if (geucomp.equipmentid_auto == null || geucomp.equipmentid_auto == 0)
                return longNullableToint(geucomp.cmu);

            if (geucomp.smu_at_install == null)
                geucomp.smu_at_install = 0;

            int FirstEqLtdAfterInstall = 0;
            DateTime ComponentInstalledOnEqDate = geucomp.date_installed == null ? DateTime.MinValue : (DateTime)geucomp.date_installed;
            var EqLifesAfterComponentInstall = _context.EQUIPMENT_LIVES.Where(m => m.EquipmentId == geucomp.equipmentid_auto && m.ActionDate > ComponentInstalledOnEqDate && m.Action.recordStatus == (int)RecordStatus.Available);
            if (EqLifesAfterComponentInstall.Count() > 0)
            {
                FirstEqLtdAfterInstall = EqLifesAfterComponentInstall.OrderBy(m => m.ActionDate).First().ActualLife;
            }
            else
            {
                FirstEqLtdAfterInstall = GetEquipmentLife(longNullableToint(geucomp.equipmentid_auto), ComponentInstalledOnEqDate);
            }
            int ComponentEqLtdAtInstall = longNullableToint(geucomp.eq_ltd_at_install);
            int ComponentCmu = longNullableToint(geucomp.cmu);
            return FirstEqLtdAfterInstall - ComponentEqLtdAtInstall + ComponentCmu;

            //return ComponentCmu;
        }

        private int GetComponentWithNoLifeChanges(int Id, int ActionId)
        {
            var geucomp = _context.GENERAL_EQ_UNIT.Find(Id);
            if (Id == 0 || geucomp == null)
                return 0;
            if (geucomp.equipmentid_auto == null || geucomp.equipmentid_auto == 0 || geucomp.smu_at_install == null || geucomp.smu_at_install == 0)
                return longNullableToint(geucomp.cmu);

            int EqLtdAtInstall = 0;
            DateTime EqInstalled = geucomp.date_installed == null ? DateTime.MinValue : (DateTime)geucomp.date_installed;
            var EqLifes = _context.EQUIPMENT_LIVES.Where(m => m.EquipmentId == geucomp.equipmentid_auto && m.ActionDate > EqInstalled && m.Action.recordStatus == (int)RecordStatus.Available);

            if (EqLifes.Count() > 0)
            {
                EqLtdAtInstall = EqLifes.OrderBy(m => m.ActionDate).First().ActualLife;
                if (EqLifes.OrderBy(m => m.ActionDate).First().ActionId != ActionId)
                {
                    var thisAction = _context.ACTION_TAKEN_HISTORY.Find(ActionId);
                    if (thisAction != null && thisAction.event_date > EqLifes.OrderBy(m => m.ActionDate).First().ActionDate)
                    {
                        return thisAction.equipment_smu - EqLifes.OrderBy(m => m.ActionDate).First().SerialMeterReading;
                    }
                }
            }
            return 0;
        }

        private int GetSystemWithNoLifeChanges(int Id, int ActionId)
        {
            var UCsys = _context.LU_Module_Sub.Find(Id);
            if (UCsys == null)
                return 0;

            var systemLinks = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == UCsys.Module_sub_auto && m.LU_COMPART.comparttype_auto == 230);//230 is the Link type ID
            var systemIdlers = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == UCsys.Module_sub_auto && m.LU_COMPART.comparttype_auto == 233);//233 is the Idler type ID

            if (systemLinks.Count() > 0 && systemIdlers.Count() > 0) //Something is wrong because one system cannot has both link and idler
                return 0;

            if (systemLinks.Count() > 0)
                return GetComponentWithNoLifeChanges(longNullableToint(systemLinks.First().equnit_auto), ActionId);
            else if (systemIdlers.Count() > 0)
                return GetComponentWithNoLifeChanges(longNullableToint(systemIdlers.First().equnit_auto), ActionId);
            return 0;
        }
        protected bool UpdateGETByAction(IEquipmentActionRecord actionRecord, ref string OperationResult)
        {
            bool result = false;

            var getEventsByUC = _gContext.GET_EVENTS.Where(m => m.UCActionHistoryId == actionRecord.Id);
            if (getEventsByUC.Count() > 0) //Means update
            {
                var getEventTobeUpdated = getEventsByUC.First();
                //All the logic for the lifes to be updated based on getEventTobeUpdated
            }
            else
            { //Insert
                GET_EVENTS GetEvents = new GET_EVENTS
                {
                    user_auto = actionRecord.ActionUser.Id,
                    action_auto = (int)GETActionType.UndercarriageAction,
                    event_date = actionRecord.ActionDate,
                    comment = actionRecord.Comment,
                    cost = actionRecord.Cost,
                    recorded_date = actionRecord.ActionDate
                };

                GET_EVENTS_EQUIPMENT GetEventsEquipment = new GET_EVENTS_EQUIPMENT
                {
                    equipment_auto = actionRecord.EquipmentId,
                    smu = actionRecord.ReadSmuNumber,
                    ltd = 0, // Check this.
                    events_auto = 0 // Check this.
                };

                //GET_EVENTS_IMPLEMENT GetEventsImplement = new GET_EVENTS_IMPLEMENT
                //{

                //};

                //GET_EVENTS_COMPONENT GetEventsComponent = new GET_EVENTS_COMPONENT
                //{

                //};
            }
            return result;
        }

        /// <summary>
        /// This method validates current action based on the current recorded history.
        /// Must be called before any operations! Otherwise will be incorrect!
        /// Not suitable for update an action because updating action won't be neglected and 
        /// validation may be invalid!
        /// Not available for inventory actions
        /// </summary>
        /// <returns></returns>
        public ActionPreValidationResult PreValidate(IEquipmentActionRecord actionRecord)
        {
            var result = new ActionPreValidationResult
            {
                Id = actionRecord.Id,
                EquipmentId = actionRecord.EquipmentId,
                IsValid = true,
                Status = ActionValidationStatus.Valid,
                ProvidedDate = actionRecord.ActionDate,
                ProvidedSMU = actionRecord.ReadSmuNumber,
                EarliestValidDateForProvidedSMU = DateTime.MinValue,
                SmallestValidSmuForProvidedDate = 0
            };
            Equipment LogicalEquipment = new Equipment(_context, actionRecord.EquipmentId);
            if (actionRecord.EquipmentId == 0 || LogicalEquipment.Id == 0)
            {
                result.Status = ActionValidationStatus.InvalidEquipment;
                result.IsValid = false;
                return result;
            }

            result.SmallestValidSmuForProvidedDate = LogicalEquipment.GetSerialMeterUnit(actionRecord.ActionDate);
            int NextSMU = GetNextEquipmentSerialMeterUnit(LogicalEquipment.Id, actionRecord.ActionDate);

            result.EarliestValidDateForProvidedSMU = LogicalEquipment.DALEquipment.purchase_date ?? DateTime.MinValue;

            var lives = LogicalEquipment.GetEquipmentAvailableLives().Where(m => m.SerialMeterReading <= actionRecord.ReadSmuNumber).OrderByDescending(m => m.ActionDate);
            if (lives.Count() > 0)
                result.EarliestValidDateForProvidedSMU = lives.FirstOrDefault().ActionDate;

            if (actionRecord.ReadSmuNumber < result.SmallestValidSmuForProvidedDate || actionRecord.ReadSmuNumber > NextSMU)
            {
                result.IsValid = false;
                result.Status = ActionValidationStatus.InvalidSMU;
            }
            return result;
        }

        protected IEquipmentActionRecord UpdateEquipmentByAction(IEquipmentActionRecord actionRecord, ref string OperationResult)
        {
            //Steps in this method:
            //1- Create a record in ACTION_TAKEN_HISTORY Table with no data in EquipmentSMU and EquipmentLTD
            //2- Update Equipment, Systems and Components life
            //3- Update ACTION_TAKEN_HISTORY Table with New EquipmentSMU and EquipmentLTD
            //4- Return an IEquipmentActionRecord to use in the occuring action

            actionLifeUpdate = new ActionLifeUpdate();
            DALEquipment = _context.EQUIPMENTs.Find(actionRecord.EquipmentId);
            if (DALEquipment == null)
                return null;
            //Step1
            int aType = 0;
            try
            {
                aType = (int)actionRecord.TypeOfAction;
            }
            catch { aType = (int)ActionType.NoActionTakenYet; }

            OperationResult += "Start insert into ACTION_TAKEN_HISTORY" + System.Environment.NewLine;
            var k = new ACTION_TAKEN_HISTORY
            {
                action_type_auto = aType,
                cmu = 0,
                event_date = actionRecord.ActionDate,
                entry_date = DateTime.Now,
                entry_user_auto = actionRecord.ActionUser.Id,
                equipmentid_auto = actionRecord.EquipmentId,
                cost = (long)actionRecord.Cost,
                equipment_ltd = 0, //Will be updated in Step3
                equipment_smu = 0, //Will be updated in Step3
                comment = actionRecord.Comment,
                GETActionHistoryId = actionRecord.GETHistoryId,
                recordStatus = (int)RecordStatus.MiddleOfAction
            };
            _context.ACTION_TAKEN_HISTORY.Add(k);
            try
            {
                _context.SaveChanges();
                OperationResult += "Succeded" + System.Environment.NewLine;
                DALRecord = k;
            }
            catch (Exception ex)
            {
                OperationResult += "Error :" + ex.Message + System.Environment.NewLine;
                Message = "Saving action history failed!";
                return null;
            }

            if (longNullableToint(k.history_id) == 0)
            {
                OperationResult += "Error: Returned Id is not valid" + System.Environment.NewLine;
                Message = "Action history has not been saved successfully";
                return null;
            }
            actionLifeUpdate.ActionTakenHistory = k;
            int EquipmentActualLife = actionRecord.EquipmentActualLife;
            int EquipmentCurrentSMU = actionRecord.ReadSmuNumber;
            //End of Step 1
            if (k.action_type_auto != (int)ActionType.UpdateSetupEquipment)
            {
                //Step2
                OperationResult += "Updating Equipment, Systems and Components life " + System.Environment.NewLine;
                if (!UpdateEquipmentLife(actionRecord.EquipmentId, actionRecord.ReadSmuNumber, actionRecord.ActionUser.Id, longNullableToint(k.history_id), actionRecord.ActionDate, ref OperationResult))
                    return null;
                EquipmentActualLife = GetEquipmentLife(actionRecord.EquipmentId, actionRecord.ActionDate, true);
                EquipmentCurrentSMU = GetEquipmentSerialMeterUnit(actionRecord.EquipmentId, actionRecord.ActionDate, true);

                if (EquipmentActualLife < 0 || EquipmentCurrentSMU < 0)
                {
                    OperationResult += "Equipment Actual Life OR Equipment Current SMU is invalid" + System.Environment.NewLine;
                    Message = "Stored Life or SMU of the equipment is less than zero and not correct!";
                    return null;
                }
            }

            //Step3
            k.equipment_ltd = EquipmentActualLife;
            k.equipment_smu = EquipmentCurrentSMU;
            _context.Entry(k).State = System.Data.Entity.EntityState.Modified;

            try
            {
                OperationResult += "Updating Action Taken History..." + Environment.NewLine;
                _context.SaveChanges();
                OperationResult += "Succeded" + Environment.NewLine;
            }
            catch (Exception ex)
            {
                OperationResult += "Error :" + ex.Message + Environment.NewLine;
                Message = "Updating action history failed!";
                return null;
            }

            //Step4
            actionRecord.Id = (int)k.history_id;
            actionRecord.EquipmentActualLife = EquipmentActualLife;
            return actionRecord;
        }

        // Update smu and return new life
        private bool UpdateEquipmentLife(int id, int ReadSmuNumber, int UserId, int ActionId, DateTime ActionDate, ref string OperationResult)
        {
            //1- Insert a record in the EQUIPMENT_LIFE Table
            //2- Find all systems which are currently on this Equipment
            //3- Update all system lifes by inserting a record for each system in the System Life table
            //3-1 Update all components on each system by inserting a record for each component in the Component Life Table

            OperationResult += "Checking action Id " + Environment.NewLine;
            if (_context.ACTION_TAKEN_HISTORY.Find(ActionId) == null)
            {
                OperationResult += " Failed!" + System.Environment.NewLine;
                actionLifeUpdate.ActionTakenHistory = null;
                Message = "Action history has not been recorded successfully";
                return false;
            }

            int currentSMU = GetEquipmentSerialMeterUnit(id, ActionDate);
            int nextSMU = GetNextEquipmentSerialMeterUnit(id, ActionDate);
            int currentLife = GetEquipmentLife(id, ActionDate);
            if (ReadSmuNumber < currentSMU || ReadSmuNumber > nextSMU)
            {
                OperationResult += "Checking SMU Failed: For the " + ActionDate + ", accepted SMU is between " + currentSMU + " and " + nextSMU + Environment.NewLine;
                if (nextSMU == int.MaxValue)
                    Message = "Checking SMU Failed: For the " + ActionDate + ", accepted SMU must be greater than" + currentSMU;
                else
                    Message = "Checking SMU Failed: For the " + ActionDate + ", accepted SMU is between " + currentSMU + " and " + nextSMU;
                return false;
            }

            int increasedSMU = (ReadSmuNumber - currentSMU);
            OperationResult += "Insert Into Equipment Life " + Environment.NewLine;
            _context.EQUIPMENT_LIVES.Add(
                new EQUIPMENT_LIFE
                {
                    ActionDate = ActionDate,
                    ActionId = ActionId,
                    ActualLife = currentLife + increasedSMU,
                    EquipmentId = id,
                    SerialMeterReading = ReadSmuNumber,
                    Title = "Inserted by a normal action",
                    UserId = UserId
                }
                );
            // If it is the most recent action, update current smu of the equipment
            int actionsAfterThisAction = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == id && m.recordStatus == 0 && m.event_date > ActionDate && m.recordStatus == 0).Count();
            if (actionsAfterThisAction == 0)
            {
                var geuEquipment = _context.EQUIPMENTs.Find(id);
                if (geuEquipment != null)
                {
                    actionLifeUpdate.EqCurrentSMU = longNullableToint(geuEquipment.currentsmu);
                    geuEquipment.currentsmu = ReadSmuNumber;
                    geuEquipment.last_reading_date = ActionDate;
                    _context.Entry(geuEquipment).State = EntityState.Modified;
                }
            }

            OperationResult += "Start Svaing Changes In Equipment Life and EQUIPMENT" + Environment.NewLine;
            bool _op = true;
            try
            {
                _context.SaveChanges();
                OperationResult += "Update life of Equipment succeeded" + Environment.NewLine;
            }
            catch (Exception e1)
            {
                OperationResult += "Error: " + e1.Message + Environment.NewLine;
                Message = "Updating the life of EQUIPMENT has been failed!";
                _op = false;
            }

            OperationResult += "Query all systems " + Environment.NewLine;
            var EqSystems = new Equipment(_context).getEquipmentSystems(id, ActionDate).ToList();
            foreach (var s in EqSystems)
            {
                OperationResult += "Insert system life " + s.Module_sub_auto + Environment.NewLine;
                int currentSystemLife = GetSystemLife(longNullableToint(s.Module_sub_auto), ActionDate);

                int SystemIncreasedLife = increasedSMU;

                if (!s.Life.Any(m=> m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.Available))
                SystemIncreasedLife = 0; 

                _context.UCSYSTEM_LIFE.Add(
                    new SystemLife
                    {
                        ActionDate = ActionDate,
                        ActualLife = currentSystemLife + SystemIncreasedLife,
                        ActionId = ActionId,
                        SystemId = s.Module_sub_auto,
                        Title = "Inserted by a normal action",
                        UserId = UserId,
                    }
                    );
                OperationResult += "Query all components " + Environment.NewLine;
                var SYsComponents = new UCSystem(_context).getSystemComponents((int)s.Module_sub_auto, ActionDate).ToList();
                foreach (var comp in SYsComponents)
                {
                    OperationResult += "Insert component life " + comp.equnit_auto + Environment.NewLine;
                    int currentComponentLife = GetComponentLife(longNullableToint(comp.equnit_auto), ActionDate);
                    _context.COMPONENT_LIFE.Add(
                                new ComponentLife
                                {
                                    ActionDate = ActionDate,
                                    ActualLife = currentComponentLife + SystemIncreasedLife,
                                    ActionId = ActionId,
                                    ComponentId = comp.equnit_auto,
                                    Title = "Inserted by a normal action",
                                    UserId = UserId,
                                }
                        );
                }
            }

            OperationResult += "Start Svaing Changes " + Environment.NewLine;
            _op = true;
            try
            {
                _context.SaveChanges();
                OperationResult += "Update life of Equipment, Systems and all components succeeded" + Environment.NewLine;
            }
            catch (Exception e1)
            {
                OperationResult += "Error: " + e1.Message + Environment.NewLine;
                Message = "Updating the life has been failed!";
                _op = false;
            }
            var EqlifeList = _context.EQUIPMENT_LIVES.Where(m => m.ActionId == ActionId);
            if (EqlifeList.Count() > 0)
                actionLifeUpdate.EquipmentLife = EqlifeList.First();
            actionLifeUpdate.SystemsLife = _context.UCSYSTEM_LIFE.Where(m => m.ActionId == ActionId).ToList();
            actionLifeUpdate.ComponentsLife = _context.COMPONENT_LIFE.Where(m => m.ActionId == ActionId).ToList();
            return _op;
        }
        protected void rollBack()
        {
            if (actionLifeUpdate != null)
            {
                if (DALEquipment != null && actionLifeUpdate.EqCurrentSMU != 0)
                {
                    DALEquipment.currentsmu = actionLifeUpdate.EqCurrentSMU;
                    _context.Entry(DALEquipment).State = EntityState.Modified;
                }
                if (actionLifeUpdate.EquipmentLife != null)
                    _context.Entry(actionLifeUpdate.EquipmentLife).State = EntityState.Deleted;
                if (actionLifeUpdate.SystemsLife != null && actionLifeUpdate.SystemsLife.Count() > 0)
                    _context.UCSYSTEM_LIFE.RemoveRange(actionLifeUpdate.SystemsLife);
                if (actionLifeUpdate.ComponentsLife != null && actionLifeUpdate.ComponentsLife.Count() > 0)
                    _context.COMPONENT_LIFE.RemoveRange(actionLifeUpdate.ComponentsLife);
                if (actionLifeUpdate.ActionTakenHistory != null)
                    _context.ACTION_TAKEN_HISTORY.Remove(actionLifeUpdate.ActionTakenHistory);
                try
                {
                    _context.SaveChanges();
                }
                catch (Exception e1)
                {
                    string m = e1.Message;
                };
            }
        }

        protected void rollBackUCByGet(int UCActionId, RecordStatus RollbackReason, DateTime RollbackDate)
        {
            var action = _context.ACTION_TAKEN_HISTORY.Find(UCActionId);
            if (action != null && action.action_type_auto == (int)ActionType.GETAction)
            {
                action.recordStatus = (int)RollbackReason;
                action.last_modified_date = RollbackDate;
                try
                {
                    _context.SaveChanges();
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }
        }
        protected bool rollBackGETAction(IEquipmentActionRecord actionRecord, ref string OperationResult)
        {
            //throw new NotImplementedException();
            return false;
        }
        protected string giveMeRandomString(string allowedChars, int minLength, int maxLength, Random rng)
        {
            char[] chars = new char[maxLength];
            int setLength = allowedChars.Length;
            int length = rng.Next(minLength, maxLength + 1);

            for (int i = 0; i < length; ++i)
            {
                chars[i] = allowedChars[rng.Next(setLength)];
            }

            return new string(chars, 0, length);
        }
        protected decimal ConvertFrom(MeasurementType from, decimal reading)
        {
            if (from == MeasurementType.Milimeter)
                return reading * (decimal)(0.0393701);
            return reading * (decimal)25.4;
        }

        /// <summary>
        /// Updates the action taken history record to have the status "Middle of Action". 
        /// We sometimes need to know if it is in the middle of an action so we can still use the
        /// corresponding life records created, before we commit the action. 
        /// </summary>
        /// <returns>True if success in updating the status. </returns>
        protected bool MiddleOfAction()
        {
            if (DALRecord == null || DALRecord.history_id == 0)
                return false;
            DALRecord.recordStatus = (int)RecordStatus.MiddleOfAction;
            _context.Entry(DALRecord).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                return false;
            }
        }

        protected bool Available()
        {
            if (DALRecord == null || DALRecord.history_id == 0)
                return false;
            DALRecord.recordStatus = (int)RecordStatus.Available;
            _context.Entry(DALRecord).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                return false;
            }
            ///Updating next inspection and smu forcast
            var _equipment = _context.EQUIPMENTs.Find(DALRecord.equipmentid_auto);
            if (_equipment != null)
            {
                var _logicalEq = new Equipment(_context);
                DateTime _forcastedDate = _logicalEq.ForcastNextInspectionDate(DateTime.Now, (int)_equipment.equipmentid_auto);
                if (_forcastedDate > DateTime.MinValue) _equipment.NextInspectionDate = _forcastedDate;
                int _forcastedSMU = _logicalEq.ForcastNextInspectionSMU(DateTime.Now, (int)_equipment.equipmentid_auto);
                if (_forcastedSMU > 0) _equipment.NextInspectionSMU = _forcastedSMU;
                if (_forcastedDate > DateTime.MinValue || _forcastedSMU > 0)
                {
                    _context.Entry(_equipment).State = EntityState.Modified;
                    try
                    {
                        _context.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        string message = ex.Message;
                        return false;
                    }
                }
            }
            return true;
        }
        public void Dispose()
        {
            if (Operation != null)
                Operation.Dispose();
        }
        /// <summary>
        /// Returns true if based on the given date SMU is valid otherwise returns false
        /// </summary>
        /// <param name="EquipmentId">Id of the Equipment</param>
        /// <param name="ReadSmuNumber">The read SMU number</param>
        /// <param name="ActionDate">Date of the action</param>
        /// <returns>If valid return true</returns>
        public bool ActionSMUValidation(int EquipmentId, int ReadSmuNumber, DateTime ActionDate)
        {
            var equipment = _context.EQUIPMENTs.Find(EquipmentId);
            if (equipment == null)
                return false;
            int currentSMU = GetEquipmentSerialMeterUnit(EquipmentId, ActionDate);
            int nextSMU = GetNextEquipmentSerialMeterUnit(EquipmentId, ActionDate);
            int currentLife = GetEquipmentLife(EquipmentId, ActionDate);
            if (ReadSmuNumber < currentSMU || ReadSmuNumber > nextSMU)
            {
                return false;
            }
            return true;
        }
    }
}

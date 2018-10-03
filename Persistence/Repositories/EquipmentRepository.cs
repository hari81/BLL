using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Core.Repositories;
using DAL;
using AutoMapper;
using BLL.Interfaces;
using System.Data.Entity;

namespace BLL.Persistence.Repositories
{
    public class EquipmentRepository : Repository<Equipment>, IEquipmentRepository
    {
        public IEquipment _equipment;
        public List<IUCSystem> _systems;
        public List<IComponent> _components;
        public EquipmentRepository(IUndercarriageContext context) : base(context)
        {
            _equipment = new Equipment(context);
            _systems = new List<IUCSystem>();
            _components = new List<IComponent>();
        }
        public EquipmentRepository(IUndercarriageContext context, IEquipment equipment) : base(context)
        {
            _equipment = equipment;
            _systems = new List<IUCSystem>();
            _components = new List<IComponent>();
        }
        public EquipmentRepository(IUndercarriageContext context, IEquipment equipment, List<IUCSystem> systems) : base(context)
        {
            _equipment = equipment;
            _systems = systems;
            _components = new List<IComponent>();
        }
        public EquipmentRepository(IUndercarriageContext context, IEquipment equipment, List<IUCSystem> systems, List<IComponent> components) : base(context)
        {
            _equipment = equipment;
            _systems = systems;
            _components = components;
        }
        public UndercarriageContext _context
        {
            get { return Context as UndercarriageContext; }
        }
        public int GetEquipmentLife(int Id, DateTime date)
        {
            var Eq = _context.EQUIPMENTs.Find(Id);
            if (Eq == null)
                return -1;
            var lifes = Eq.Life.Where(m => m.ActionDate <= date).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return base.longNullableToint(Eq.currentsmu);
        }

        public int GetEquipmentSerialMeterUnit(int Id, DateTime date)
        {
            var Eq = _context.EQUIPMENTs.Find(Id);
            if (Eq == null)
                return -1;
            var lifes = Eq.Life.Where(m => m.ActionDate <= date).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().SerialMeterReading;

            return base.longNullableToint(Eq.currentsmu);
        }

        public int GetSystemLife(int Id, DateTime date)
        {
            var UCsys = _context.LU_Module_Sub.Find(Id);
            if (UCsys == null)
                return -1;
            var lifes = UCsys.Life.Where(m => m.ActionDate <= date).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return base.longNullableToint(UCsys.LTD);
        }

        public int GetComponentLife(int Id, DateTime date)
        {
            var Comp = _context.GENERAL_EQ_UNIT.Find(Id);
            if (Comp == null)
                return -1;
            var lifes = Comp.Life.Where(m => m.ActionDate <= date).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return base.longNullableToint(Comp.cmu);
        }

        // Update meter unit and return new life
        public bool ResetMeterUnit(int Id, int ReadSmuNumber, int UserId, ActionType TypeOfAction, DateTime date)
        {
            //Should be implemented later
            return false;
        }

        public IEquipmentActionRecord UpdateEquipmentByAction(IEquipmentActionRecord actionRecord, ref string OperationResult)
        {
            //Steps in this method:
            //1- Create a record in ACTION_TAKEN_HISTORY Table with no data in EquipmentSMU and EquipmentLTD
            //2- Update Equipment, Systems and Components life
            //3- Update ACTION_TAKEN_HISTORY Table with New EquipmentSMU and EquipmentLTD
            //4- Return an IEquipmentActionRecord to use in the happening action

            //Step1
            OperationResult += "Start insert into -> ACTION_TAKEN_HISTORY" + System.Environment.NewLine;
            var k = new ACTION_TAKEN_HISTORY
            {
                action_type_auto = (int)ActionType.NoActionTakenYet,
                cmu = 0,
                event_date = actionRecord.ActionDate,
                entry_date = DateTime.Now,
                entry_user_auto = actionRecord.ActionUser.Id,
                equipmentid_auto = actionRecord.EquipmentId,
                cost = (long)actionRecord.Cost,
                equipment_ltd = 0, //Will be updated in Step3
                equipment_smu = 0, //Will be updated in Step3
                comment = actionRecord.Comment
            };
            _context.ACTION_TAKEN_HISTORY.Add(k);
            try
            {
                _context.SaveChanges();
                OperationResult += "Succeded" + System.Environment.NewLine;
            }
            catch (Exception ex)
            {
                OperationResult += "Error :" + ex.Message + System.Environment.NewLine;
                return null;
            }

            if (base.longNullableToint(k.history_id) == 0)
            {
                OperationResult += "Error: Returned Id is not valid" + System.Environment.NewLine;
                return null;
            }

            //End of Step 1

            //Step2
            OperationResult += "Updating Equipment, Systems and Components life -> " + System.Environment.NewLine;
            if (!UpdateEquipmentLife(actionRecord.EquipmentId, actionRecord.ReadSmuNumber, actionRecord.ActionUser.Id, base.longNullableToint(k.history_id), actionRecord.ActionDate, ref OperationResult))
            {
                return null;
            }

            int EquipmentActualLife = GetEquipmentLife(actionRecord.EquipmentId, actionRecord.ActionDate);
            int EquipmentCurrentSMU = GetEquipmentSerialMeterUnit(actionRecord.EquipmentId, actionRecord.ActionDate);

            if (EquipmentActualLife < 0 || EquipmentCurrentSMU < 0)
            {
                OperationResult += "Equipment Actual Life OR Equipment Current SMU is invalid" + System.Environment.NewLine;
                return null;
            }


            //Step3
            k.equipment_ltd = EquipmentActualLife;
            k.equipment_smu = EquipmentCurrentSMU;
            _context.Entry(k).State = System.Data.Entity.EntityState.Modified;

            //Step4
            actionRecord.Id = (int)k.history_id;
            actionRecord.EquipmentActualLife = EquipmentActualLife;
            return actionRecord;
        }

        // Update smu and return new life
        public bool UpdateEquipmentLife(int id, int ReadSmuNumber, int UserId, int ActionId, DateTime ActionDate, ref string OperationResult)
        {
            //1- Insert a record in the EQUIPMENT_LIFE Table
            //2- Find all systems which are currently on this Equipment
            //3- Update all system lifes by inserting a record for each system in the System Life table
            //3-1 Update all components on each system by inserting a record for each component in the Component Life Table

            OperationResult += "Checking action Id " + System.Environment.NewLine;
            if (_context.ACTION_TAKEN_HISTORY.Find(ActionId) == null)
            {
                OperationResult += "-> Failed!" + System.Environment.NewLine;
                return false;
            }

            int currentSMU = GetEquipmentSerialMeterUnit(id, ActionDate);
            int currentLife = GetEquipmentLife(id, ActionDate);
            if (ReadSmuNumber < currentSMU)
            {
                OperationResult += "Checking SMU Failed 'Read SMU is less than latest one before this date'" + Environment.NewLine;
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

            OperationResult += "Query all systems " + Environment.NewLine;
            var EqSystems = _context.LU_Module_Sub.Where(m => m.equipmentid_auto == id).ToList();
            foreach (var s in EqSystems)
            {
                OperationResult += "Insert system life " + s.Module_sub_auto + Environment.NewLine;
                int currentSystemLife = GetSystemLife(base.longNullableToint(s.Module_sub_auto), ActionDate);
                _context.UCSYSTEM_LIFE.Add(
                    new SystemLife
                    {
                        ActionDate = ActionDate,
                        ActualLife = currentSystemLife + increasedSMU,
                        ActionId = ActionId,
                        SystemId = s.Module_sub_auto,
                        Title = "Inserted by a normal action",
                        UserId = UserId,
                    }
                    );
                OperationResult += "Query all components " + Environment.NewLine;
                var SYsComponents = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == s.Module_sub_auto).ToList();
                foreach (var comp in SYsComponents)
                {
                    OperationResult += "Insert component life " + comp.equnit_auto + Environment.NewLine;
                    int currentComponentLife = GetComponentLife(base.longNullableToint(comp.equnit_auto), ActionDate);
                    _context.COMPONENT_LIFE.Add(
                                new ComponentLife
                                {
                                    ActionDate = ActionDate,
                                    ActualLife = currentComponentLife + increasedSMU,
                                    ActionId = ActionId,
                                    ComponentId = comp.equnit_auto,
                                    Title = "Inserted by a normal action",
                                    UserId = UserId,
                                }
                        );
                }
            }

            OperationResult += "Start Svaing Changes " + Environment.NewLine;
            try
            {
                _context.SaveChanges();
                OperationResult += "Update life of Equipment, Systems and all components succeeded" + Environment.NewLine;
                return true;
            }
            catch (Exception e1)
            {
                OperationResult += "Error: " + e1.Message + Environment.NewLine;
                return false;
            }

        }
    }
}
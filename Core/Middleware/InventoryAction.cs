using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Extensions;
namespace BLL.Core.Middleware
{
    public class InventoryActionParam
    {
        public int ComponentId { get; set; }
        public int NewCompartId { get; set; }
        public int CMU { get; set; }
        public int BudgetLife { get; set; }
        public DateTime ActionDate { get; set; }
        public decimal Cost { get; set; }
        public string Comment { get; set; }
        public int userId { get; set; }
    }
    public interface IInventoryOperation
    {
        bool RecordAcrion(InventoryActionParam Param);
        string Message { get; }
        string ActionLog { get; }
    }
    public class InventoryAction
    {
        private DAL.UndercarriageContext _context;
        public IInventoryOperation operation;
        public bool Initialized { get; private set; }
        public InventoryAction(DAL.UndercarriageContext context, ActionType type)
        {
            _context = context;
            Initialized = true;
            switch (type)
            {
                case ActionType.RepairDryJointsLink:
                case ActionType.RepairDryJointsBush:
                    operation = new InventoryRepairDryJoints(_context);
                    break;
                case ActionType.TurnPinsAndBushingsLink:
                case ActionType.TurnPinsAndBushingsBush:
                    operation = new InventoryTurnPinsAndBushing(_context);
                    break;
                case ActionType.Replace:
                    operation = new InventoryReplace(_context);
                    break;
                case ActionType.Regrouser:
                    operation = new InventoryRegrouse(_context);
                    break;
                case ActionType.Reshell:
                    operation = new InventoryReshell(_context);
                    break;
                default:
                    Initialized = false;
                    break;
            }
        }
    }
    public class InventoryRepairDryJoints : IInventoryOperation
    {
        private DAL.UndercarriageContext _context;
        private string _message = "";
        private string _actionLog = "";
        public InventoryRepairDryJoints(DAL.UndercarriageContext context)
        {
            _context = context;
        }
        public string Message { get { return _message; } }
        public string ActionLog { get { return _actionLog; } }
        public bool RecordAcrion(InventoryActionParam Param)
        {
            BLL.Core.Domain.Component LogicalComponent = new Component(_context, Param.ComponentId);
            if (LogicalComponent.Id == 0)
            {
                _message = "Component Not Found!";
                return false;
            }
            var user = new BLL.Core.Domain.TTDevUser(new DAL.SharedContext(), Param.userId).getUCUser();
            if (user == null)
            {
                _message = "User Not Found!";
                return false;
            }

            DAL.ACTION_TAKEN_HISTORY record = new DAL.ACTION_TAKEN_HISTORY
            {
                action_type_auto = (int)ActionType.RepairDryJointsLink,
                comment = Param.Comment,
                compartid_auto = LogicalComponent.DALComponent.compartid_auto,
                cost = (long)Param.Cost,
                event_date = Param.ActionDate,
                entry_date = DateTime.Now,
                entry_user_auto = Param.userId,
                equnit_auto = Param.ComponentId,
                recordStatus = 0,
                system_auto_id = LogicalComponent.DALComponent.module_ucsub_auto
            };
            _context.ACTION_TAKEN_HISTORY.Add(record);
            try
            {
                _context.SaveChanges();
                _message = "Action recorded sucessfully!";
                return true;
            }
            catch (Exception ex)
            {
                _message = "Recording Action Failed! Please check Log for details";
                _actionLog += ex.Message;
                if (ex.InnerException != null)
                    _actionLog += ex.InnerException.Message;
                return false;
            }
        }
    }

    public class InventoryTurnPinsAndBushing : IInventoryOperation
    {
        private DAL.UndercarriageContext _context;
        private string _message = "";
        private string _actionLog = "";
        public InventoryTurnPinsAndBushing(DAL.UndercarriageContext context)
        {
            _context = context;
        }
        public string Message { get { return _message; } }
        public string ActionLog { get { return _actionLog; } }
        public bool RecordAcrion(InventoryActionParam Param)
        {
            BLL.Core.Domain.Component LogicalComponent = new Component(_context, Param.ComponentId);
            if (LogicalComponent.Id == 0)
            {
                _message = "Component Not Found!";
                return false;
            }

            var user = new BLL.Core.Domain.TTDevUser(new DAL.SharedContext(), Param.userId).getUCUser();
            if (user == null)
            {
                _message = "User Not Found!";
                return false;
            }
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Param.ComponentId && m.recordStatus == 0 && (m.action_type_auto == (int)ActionType.TurnPinsAndBushingsLink || m.action_type_auto == (int)ActionType.TurnPinsAndBushingsBush));
            if(actions.Count() > 0)
            {
                _message = "This component has already been tunred!";
                return false;
            }
            DAL.ACTION_TAKEN_HISTORY record = new DAL.ACTION_TAKEN_HISTORY
            {
                action_type_auto = (int)ActionType.TurnPinsAndBushingsLink,
                comment = Param.Comment,
                compartid_auto = LogicalComponent.DALComponent.compartid_auto,
                cost = (long)Param.Cost,
                event_date = Param.ActionDate,
                entry_date = DateTime.Now,
                entry_user_auto = Param.userId,
                equnit_auto = Param.ComponentId,
                recordStatus = 0,
                system_auto_id = LogicalComponent.DALComponent.module_ucsub_auto
            };
            _context.ACTION_TAKEN_HISTORY.Add(record);
            try
            {
                _context.SaveChanges();
                _message = "Action recorded sucessfully!";
                return true;
            }
            catch (Exception ex)
            {
                _message = "Recording Action Failed! Please check Log for details";
                _actionLog += ex.Message;
                if (ex.InnerException != null)
                    _actionLog += ex.InnerException.Message;
                return false;
            }
        }
    }

    public class InventoryRegrouse : IInventoryOperation
    {
        private DAL.UndercarriageContext _context;
        private string _message = "";
        private string _actionLog = "";
        public InventoryRegrouse(DAL.UndercarriageContext context)
        {
            _context = context;
        }
        public string Message { get { return _message; } }
        public string ActionLog { get { return _actionLog; } }
        public bool RecordAcrion(InventoryActionParam Param)
        {
            BLL.Core.Domain.Component LogicalComponent = new Component(_context, Param.ComponentId);
            if (LogicalComponent.Id == 0)
            {
                _message = "Component Not Found!";
                return false;
            }
            var user = new BLL.Core.Domain.TTDevUser(new DAL.SharedContext(), Param.userId).getUCUser();
            if (user == null)
            {
                _message = "User Not Found!";
                return false;
            }
            DAL.ACTION_TAKEN_HISTORY record = new DAL.ACTION_TAKEN_HISTORY
            {
                action_type_auto = (int)ActionType.Regrouser,
                comment = Param.Comment,
                compartid_auto = LogicalComponent.DALComponent.compartid_auto,
                cost = (long)Param.Cost,
                event_date = Param.ActionDate,
                entry_date = DateTime.Now,
                entry_user_auto = Param.userId,
                equnit_auto = Param.ComponentId,
                recordStatus = 0,
                system_auto_id = LogicalComponent.DALComponent.module_ucsub_auto
            };
            _context.ACTION_TAKEN_HISTORY.Add(record);
            try
            {
                _context.SaveChanges();
                _message = "Action recorded sucessfully!";
                return true;
            }
            catch (Exception ex)
            {
                _message = "Recording Action Failed! Please check Log for details";
                _actionLog += ex.Message;
                if (ex.InnerException != null)
                    _actionLog += ex.InnerException.Message;
                return false;
            }
        }
    }

    public class InventoryReplace : IInventoryOperation
    {
        private DAL.UndercarriageContext _context;
        private string _message = "";
        private string _actionLog = "";
        public InventoryReplace(DAL.UndercarriageContext context)
        {
            _context = context;
        }
        public string Message { get { return _message; } }
        public string ActionLog { get { return _actionLog; } }
        public bool RecordAcrion(InventoryActionParam Param)
        {
            BLL.Core.Domain.Component LogicalComponent = new Component(_context, Param.ComponentId);
            if (LogicalComponent.Id == 0)
            {
                _message = "Component Not Found!";
                return false;
            }
            var user = new BLL.Core.Domain.TTDevUser(new DAL.SharedContext(), Param.userId).getUCUser();
            if (user == null)
            {
                _message = "User Not Found!";
                return false;
            }
            _actionLog += "Repalce childs first" + Environment.NewLine;
            if (!LogicalComponent.isAChildBasedOnCompart() && ReplaceChildsFirst(new InventoryActionParam {
                ActionDate = Param.ActionDate,
                BudgetLife = Param.BudgetLife,
                CMU = Param.CMU,
                Comment = Param.Comment,
                ComponentId = Param.ComponentId,
                Cost = Param.Cost,
                NewCompartId = Param.NewCompartId,
                userId = Param.userId
            } ))
                _actionLog += "Repalce childs returned true" + Environment.NewLine;
            else
                _actionLog += "Warning!! Repalce childs returned false!" + Environment.NewLine;

            _actionLog += "Adding New Component ..." + Environment.NewLine;
            var LogicalCompart = new BLL.Core.Domain.Compart(_context, Param.NewCompartId);
            int systemId = LogicalComponent.DALComponent.module_ucsub_auto.LongNullableToInt();
            DAL.GENERAL_EQ_UNIT geuNew = new DAL.GENERAL_EQ_UNIT
            {
                equipmentid_auto = null,
                module_ucsub_auto = systemId,
                date_installed = Param.ActionDate,
                created_user = user.userName,
                compartid_auto = Param.NewCompartId,
                compartsn = LogicalCompart.GetCompartSerialNumber(),
                created_date = Param.ActionDate,
                comp_status = 0,
                pos = LogicalComponent.DALComponent.pos,
                side = LogicalComponent.DALComponent.side,
                eq_ltd_at_install = 0,
                track_0_worn = 0,
                track_100_worn = 0,
                track_120_worn = 0,
                track_budget_life = Param.BudgetLife,
                cmu = Param.CMU,
                cost = Param.Cost,
                eq_smu_at_install = 0,
                smu_at_install = 0,
                system_LTD_at_install = 0,
                component_current_value = 0,
                variable_comp = false,
                insp_uom = 0
            };

            DAL.ACTION_TAKEN_HISTORY record = new DAL.ACTION_TAKEN_HISTORY
            {
                action_type_auto = (int)ActionType.TurnPinsAndBushingsLink,
                comment = Param.Comment,
                compartid_auto = LogicalComponent.DALComponent.compartid_auto,
                cost = (long)Param.Cost,
                event_date = Param.ActionDate,
                entry_date = DateTime.Now,
                entry_user_auto = Param.userId,
                equnit_auto = Param.ComponentId,
                recordStatus = 0,
                system_auto_id = LogicalComponent.DALComponent.module_ucsub_auto
            };
            LogicalComponent.DALComponent.module_ucsub_auto = null;
            _context.Entry(LogicalComponent.DALComponent).State = EntityState.Modified;
            _context.ACTION_TAKEN_HISTORY.Add(record);
            _context.GENERAL_EQ_UNIT.Add(geuNew);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                _message = "Recording Action Failed! Please check Log for details";
                _actionLog += ex.Message;
                if (ex.InnerException != null)
                    _actionLog += ex.InnerException.Message;
                return false;
            }
            _actionLog += "Creating a new life for the new component!"+Environment.NewLine;
            _context.COMPONENT_LIFE.Add(new DAL.ComponentLife {
                ActionDate = Param.ActionDate,
                ActionId = record.history_id,
                ActualLife = Param.CMU,
                ComponentId = geuNew.equnit_auto,
                UserId = Param.userId,
                Title = "Installed on the system in inventory"
            });
            try
            {
                _context.SaveChanges();
                _message = "Action recorded sucessfully!";
                return true;
            }
            catch(Exception ex)
            {
                _message = "Action recorded with 1 warning! please check log";
                _actionLog += ex.Message;
                if (ex.InnerException != null)
                    _actionLog += ex.InnerException.Message;
                return false;
            }
        }
        /// <summary>
        /// Before replace components childs need to be replaced
        /// </summary>
        /// <param name="Param"></param>
        /// <returns></returns>
        private bool ReplaceChildsFirst(InventoryActionParam Param)
        {
            if (Param.NewCompartId == 0)
                return true;
            var LogicalParent = new Domain.Component(_context, Param.ComponentId);
            var childs = LogicalParent.getChildsListForMiningShovel();
            if (childs.Count() == 0)
                return true;
            int k = 0;
            int ParamNewCompartId = 0;
            int ParamComponentId = 0;
            foreach (var child in childs)
            {
                k++;
                ParamNewCompartId = getChildCompartIdSuggestion(Param.NewCompartId, k);
                if (ParamNewCompartId == 0)
                {
                    removeChildIfNewCompartDoesNotHaveIt(child.equnit_auto.LongNullableToInt());
                    continue;
                }
                ParamComponentId = child.equnit_auto.LongNullableToInt();
                var action = new BLL.Core.Middleware.InventoryAction(new DAL.UndercarriageContext(), ActionType.Replace);
                if (action.Initialized)
                    action.operation.RecordAcrion(new InventoryActionParam {
                        ActionDate = Param.ActionDate,
                        BudgetLife = Param.BudgetLife,
                        CMU = Param.CMU,
                        Comment = Param.Comment,
                        ComponentId = ParamComponentId,
                        Cost = 0,
                        NewCompartId = ParamNewCompartId,
                        userId = Param.userId
                    });
            }
            return true;
        }
        /// <summary>
        /// A child component needs to be replaced but in the page there is no option to select child compartId
        /// So based on the numbers of current childs of the current component possible childs will be selected for the 
        /// new child compart Id
        /// </summary>
        /// <param name="parentCompartId">
        /// Compart Id of the new component
        /// </param>
        /// <param name="counter">we need to know if the number of required childs exceeded the available childs
        /// if the counter exceeds method returns 0 as suggested compartId
        /// </param>
        /// <returns></returns>
        private int getChildCompartIdSuggestion(int parentCompartId, int counter)
        {
            var logicalParentCompart = new Domain.Compart(_context, parentCompartId);
            var possibleComparts = logicalParentCompart.getChildComparts().OrderBy(m=>m.compartid_auto);
            int k = 0;
            foreach (var childCompart in possibleComparts)
            {
                k++;
                if (k == counter)
                {
                    return childCompart.compartid_auto;
                }
                else continue;
            }
            return 0; 
        }
        /// <summary>
        /// If new component which is selected doesn't have childs or has less than the previous one
        /// old childs must be removed from this system
        /// </summary>
        /// <param name="childCompId"></param>
        /// <returns></returns>
        private bool removeChildIfNewCompartDoesNotHaveIt(int childCompId)
        {
            var currentChild = _context.GENERAL_EQ_UNIT.Find(childCompId);
            if (currentChild == null)
                return true;
            currentChild.module_ucsub_auto = null;
            try
            {
                _context.SaveChanges();
                return true;
            }catch(Exception ex){
                string message = ex.Message;
                return false;
            }
        }
    }

    public class InventoryReshell : IInventoryOperation
    {
        private DAL.UndercarriageContext _context;
        private string _message = "";
        private string _actionLog = "";
        public InventoryReshell(DAL.UndercarriageContext context)
        {
            _context = context;
        }
        public string Message { get { return _message; } }
        public string ActionLog { get { return _actionLog; } }
        public bool RecordAcrion(InventoryActionParam Param)
        {
            BLL.Core.Domain.Component LogicalComponent = new Component(_context, Param.ComponentId);
            if (LogicalComponent.Id == 0)
            {
                _message = "Component Not Found!";
                return false;
            }
            var user = new BLL.Core.Domain.TTDevUser(new DAL.SharedContext(), Param.userId).getUCUser();
            if (user == null)
            {
                _message = "User Not Found!";
                return false;
            }
            DAL.ACTION_TAKEN_HISTORY record = new DAL.ACTION_TAKEN_HISTORY
            {
                action_type_auto = (int)ActionType.TurnPinsAndBushingsLink,
                comment = Param.Comment,
                compartid_auto = LogicalComponent.DALComponent.compartid_auto,
                cost = (long)Param.Cost,
                event_date = Param.ActionDate,
                entry_date = DateTime.Now,
                entry_user_auto = Param.userId,
                equnit_auto = Param.ComponentId,
                recordStatus = 0,
                system_auto_id = LogicalComponent.DALComponent.module_ucsub_auto
            };
            _context.ACTION_TAKEN_HISTORY.Add(record);
            try
            {
                _context.SaveChanges();
                _message = "Action recorded sucessfully!";
                return true;
            }
            catch (Exception ex)
            {
                _message = "Recording Action Failed! Please check Log for details";
                _actionLog += ex.Message;
                if (ex.InnerException != null)
                    _actionLog += ex.InnerException.Message;
                return false;
            }
        }
    }
}
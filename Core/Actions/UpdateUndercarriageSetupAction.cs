using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Interfaces;
using BLL.Core.ViewModel;
using BLL.Extensions;
using System.Data.Entity;
using DAL;

namespace BLL.Core.Actions
{
    /// <summary>
    /// This action changes all the existing action history records to modified 
    /// Then 
    /// </summary>
    public class UpdateUndercarriageSetupAction : Domain.Action, IAction
    {
        public SetupViewModel Params { get; set; }
        private DAL.LU_Module_Sub DALSystem { get; set; }
        private List<long> OtherSystems { get; set; } = new List<long>();
        private List<ActionHistoryModified> ActionHistoryModifiedIds { get; set; } = new List<ActionHistoryModified>();
        private List<ActionHistoryModified> ActionHistoryKeepIds { get; set; } = new List<ActionHistoryModified>();
        private List<OldCMU> OldComponentsCMU { get; set; } = new List<OldCMU>();
        private List<int> ReplacedComponentIds { get; set; } = new List<int>();
        private OldCMU OldSystemCMU { get; set; }
        private EQUIPMENT DALEquipment { get; set; }
        public UpdateUndercarriageSetupAction(System.Data.Entity.DbContext context, IEquipmentActionRecord actionRecord, SetupViewModel Paramteres)
            : base(context)
        {
            Params = Paramteres;
            Status = ActionStatus.Close;
            _current = actionRecord;
        }

        public string ActionLog { get; set; }

        public ActionStatus Status { get; set; }

        public int UniqueId { get; set; }

        public IEquipmentActionRecord _actionRecord { get; set; }

        public new string Message { get; set; }


        public ActionStatus Start()
        {
            if (Status != ActionStatus.Close)
            {
                Message = "Action cannot be started becasue it's state indicates that it is not closed!";
                ActionLog += Message + Environment.NewLine;
                return Status;
            }

            if (Params.Id <= 0)
            {
                Message = "Action cannot be started becasue it is not an update action!";
                ActionLog += Message + Environment.NewLine;
                return Status;
            }
            DALSystem = _context.LU_Module_Sub.Find(Params.Id);
            if (DALSystem == null)
            {
                Message = "Action failed! System cannot be found!";
                ActionLog += Message + Environment.NewLine;
                return Status;
            }
            DALEquipment = _context.EQUIPMENTs.Find(Params.EquipmentId);
            if (DALEquipment == null)
            {
                Message = "Action failed! Equipment cannot be found!";
                ActionLog += Message + Environment.NewLine;
                return Status;
            }
            if(_context.ACTION_TAKEN_HISTORY.Any(m=> m.equipmentid_auto == Params.EquipmentId && m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory && m.recordStatus == (int)RecordStatus.Available && m.system_auto_id_new == Params.Id))
            {
                Message = "Action failed! This system is installed by replace system from inventory action and was not part of the setup!";
                ActionLog += Message + Environment.NewLine;
                return Status;
            }
            
            if (_context.ACTION_TAKEN_HISTORY.Count(m => m.equipmentid_auto == Params.EquipmentId && m.recordStatus == (int)RecordStatus.Available && (m.action_type_auto == (int)ActionType.InsertInspection || m.action_type_auto == (int)ActionType.UpdateInspection || m.action_type_auto == (int)ActionType.InsertInspectionGeneral || m.action_type_auto == (int)ActionType.UpdateInspectionGeneral) && m.event_date < Params.InstallationDate) != 0)
            {
                Message = "Action failed! There is at least one inspection before setup date!";
                ActionLog += Message + Environment.NewLine;
                return Status;
            }
            
            var exceptedActions = new int[] { (int)ActionType.EquipmentSetup, (int)ActionType.UpdateSetupEquipment, (int)ActionType.InstallSystemOnEquipment, (int)ActionType.InstallComponentOnSystemOnEquipment, (int)ActionType.UpdateUndercarriageSetupOnEquipment };
            var FirstAction = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Params.EquipmentId && m.recordStatus == (int)RecordStatus.Available && !exceptedActions.Any(k => k == m.action_type_auto)).OrderBy(m => m.event_date).FirstOrDefault();
            if (FirstAction != null)
            {
                if (_current.ActionDate > FirstAction.event_date)
                {
                    Message = "Action failed! Date cannot be after " + FirstAction.event_date.ToString("dd MMM yyyy") + "!";
                    ActionLog += Message + Environment.NewLine;
                    return Status;
                }
                if (_current.ReadSmuNumber > FirstAction.equipment_smu)
                {
                    Message = "Action failed! SMU must be less than " + FirstAction.equipment_smu + " !";
                    ActionLog += Message + Environment.NewLine;
                    return Status;
                }
                if (_current.ReadSmuNumber < DALEquipment.smu_at_start)
                {
                    Message = "Action failed! SMU must be more than equipment smu at start " + DALEquipment.smu_at_start + " !";
                    ActionLog += Message + Environment.NewLine;
                    return Status;
                }
                if (_current.ActionDate < DALEquipment.purchase_date)
                {
                    Message = "Action failed! Date cannot be before equipment setup date " + (DALEquipment.purchase_date ?? DateTime.MinValue).ToString("dd MMM yyyy") + " !";
                    ActionLog += Message + Environment.NewLine;
                    return Status;
                }
            }
            ReplacedComponentIds = _context.ACTION_TAKEN_HISTORY.Where(m => m.action_type_auto == (int)ActionType.ReplaceComponentWithNew && m.recordStatus == (int)RecordStatus.Available && m.equipmentid_auto == Params.EquipmentId && m.equnit_auto_new != null).Select(m => (int)(m.equnit_auto_new)).ToList();
            if (!MakeActionsModified() || !UpdateSystemDetails() || !UpdateComponentDetails() || !UninstallOtherSystems()) return Status;
            string _log = "";
            _current = UpdateEquipmentByAction(_current, ref _log);
            ActionLog += _log + Environment.NewLine;
            if (_current == null) return Status;
            Status = ActionStatus.Started;
            return Status;
        }

        private bool MakeActionsModified()
        {
            if (DALSystem == null)
                return false;

            List<int> existingComponentsId = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Params.Id).Select(m => (int)m.equnit_auto).ToList();
            var ActionsOnEquipment = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Params.EquipmentId && m.recordStatus == (int)RecordStatus.Available && (m.action_type_auto != (int)ActionType.EquipmentSetup || m.action_type_auto != (int)ActionType.UpdateSetupEquipment));
            var ActionHistoryToModifiy = ActionsOnEquipment.Where(m => (m.action_type_auto == (int)ActionType.InstallSystemOnEquipment || m.action_type_auto == (int)ActionType.InstallComponentOnSystemOnEquipment || m.action_type_auto == (int)ActionType.UpdateUndercarriageSetupOnEquipment) && (m.system_auto_id == Params.Id || existingComponentsId.Any(k => m.equnit_auto == k)));
            var ActionHistoryToKeep = ActionsOnEquipment.Except(ActionHistoryToModifiy);
            ActionHistoryModifiedIds = ActionHistoryToModifiy.Select(m => new ActionHistoryModified { HistoryId = (int)m.history_id, Status = m.recordStatus }).ToList();
            ActionHistoryKeepIds = ActionHistoryToKeep.Select(m => new ActionHistoryModified { HistoryId = (int)m.history_id, Status = m.recordStatus }).ToList();

            foreach (var act in ActionHistoryToModifiy)
            {
                act.recordStatus = (int)RecordStatus.Modified;
                _context.Entry(act).State = EntityState.Modified;
            }
            foreach (var act in ActionHistoryToKeep)
            {
                act.recordStatus = (int)RecordStatus.TemporarilyDisabled;
                _context.Entry(act).State = EntityState.Modified;
            }
            try
            {
                _context.SaveChanges();
                ActionLog += "Actions have record status as modified now." + Environment.NewLine;
                return true;
            }
            catch (Exception ex)
            {
                ActionLog += "Exception occured in changing actions record status. " + ex.ToDetailedString();
                Message = "Action failed when trying to update existing data! Please check log!";
                return false;
            }
        }

        private bool UpdateSystemDetails()
        {
            DALSystem.Serialno = Params.Serial;
            DALSystem.notes = Params.Comment;
            DALSystem.CMU = Params.HoursAtInstall;
            DALSystem.SMU_at_install = Params.SmuAtInstall;
            DALSystem.modifiedDate = DateTime.Now.ToLocalTime();
            OldSystemCMU = new OldCMU { Id = Params.Id, CMU = (int)(DALSystem.CMU ?? 0), SmuAtInstall = (int)(DALSystem.SMU_at_install ?? 0) };
            _context.Entry(DALSystem).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                ActionLog += "System detials updated successfully!" + Environment.NewLine;
                return true;
            }
            catch (Exception ex)
            {
                ActionLog += "Updating system details failed! " + ex.ToDetailedString() + Environment.NewLine;
                Message = "Action failed when updating system details!";
                return false;
            }
        }

        private bool UninstallOtherSystems()
        {
            var _otherSystems = _context.LU_Module_Sub.Where(m => m.Module_sub_auto != Params.Id && m.equipmentid_auto == Params.EquipmentId);
            OtherSystems = _otherSystems.Select(m => m.Module_sub_auto).ToList();
            foreach (var sys in _otherSystems)
            {
                sys.equipmentid_auto = null;
                _context.Entry(sys).State = EntityState.Modified;
            }
            try
            {
                _context.SaveChanges();
                ActionLog += "Other systems uninstalled successfully!" + Environment.NewLine;
                return true;
            }
            catch (Exception ex)
            {
                ActionLog += "Updating other systems failed! " + ex.ToDetailedString() + Environment.NewLine;
                Message = "Action failed when updating system details!";
                return false;
            }
        }

        /// <summary>
        /// This method needs to be called after all other calls
        /// It updated LTD on the components and system to the updated life of the equipment
        /// </summary>
        private void UpdateLtds()
        {
            int Ltd = GetEquipmentLife(Params.EquipmentId, _current.ActionDate);
            int SystemLtd = GetSystemLife(Params.Id, _current.ActionDate);
            DALSystem.equipment_LTD_at_attachment = Ltd;
            DALSystem.LTD_at_install = SystemLtd;
            DALSystem.LTD = SystemLtd;
            _context.Entry(DALSystem).State = EntityState.Modified;
            var components = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Params.Id && !ReplacedComponentIds.Any(k=> k == m.equnit_auto));
            foreach (var component in components)
            {
                component.eq_ltd_at_install = Ltd;
                component.system_LTD_at_install = SystemLtd;
                _context.Entry(component).State = EntityState.Modified;
            }
            try
            {
                _context.SaveChanges();
                ActionLog += "Ltds updated successfully!" + Environment.NewLine;
            }
            catch (Exception ex)
            {
                ActionLog += "Updating Ltds failed! " + ex.ToDetailedString() + Environment.NewLine;
            }
        }
        private void AdjustLives()
        {
            var _ids = ActionHistoryModifiedIds.Select(m => m.HistoryId);

            if (OldSystemCMU != null && DALSystem != null)
            {
                var _difference = (int)(((DALSystem.CMU ?? 0) - OldSystemCMU.CMU) + (OldSystemCMU.SmuAtInstall - (DALSystem.SMU_at_install ?? 0)));
                var lives = _context.UCSYSTEM_LIFE.Where(m => m.SystemId == OldSystemCMU.Id && !_ids.Any(k => m.ActionId == k) && m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.TemporarilyDisabled);
                foreach (var life in lives)
                {
                    life.ActualLife += _difference;
                    _context.Entry(life).State = EntityState.Modified;
                }
            }
            var components = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Params.Id).ToList();
            var _compIds = components.Select(m => m.equnit_auto);
            foreach (var component in components)
            {
                var _item = OldComponentsCMU.Where(m => m.Id == component.equnit_auto).FirstOrDefault();
                if (_item == null) continue;
                int _difference = 0;
                if (ReplacedComponentIds.Any(k => component.equnit_auto == k))
                    _difference = (int)(((component.cmu ?? 0) - _item.CMU));
                else
                    _difference = (int)(((component.cmu ?? 0) - _item.CMU) + (_item.SmuAtInstall - (component.eq_smu_at_install ?? 0)));
                var lives = _context.COMPONENT_LIFE.Where(m => m.ComponentId == _item.Id && !_ids.Any(k => m.ActionId == k) && m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.TemporarilyDisabled);
                foreach (var life in lives)
                {
                    life.ActualLife += _difference;
                    _context.Entry(life).State = EntityState.Modified;
                }
            }

            var inspections = _context.TRACK_INSPECTION_DETAIL.Where(m => _compIds.Any(k => m.track_unit_auto == k));
            foreach (var inspection in inspections)
            {
                int _difference = 0;
                var _item = OldComponentsCMU.Where(m => m.Id == inspection.track_unit_auto).FirstOrDefault();
                if (_item == null) continue;
                if (ReplacedComponentIds.Any(k => inspection.track_unit_auto == k))
                    _difference = (int)(((inspection.GENERAL_EQ_UNIT.cmu ?? 0) - _item.CMU));
                else 
                    _difference = (int)(((inspection.GENERAL_EQ_UNIT.cmu ?? 0) - _item.CMU) + (_item.SmuAtInstall - (inspection.GENERAL_EQ_UNIT.eq_smu_at_install ?? 0)));
                inspection.hours_on_surface += _difference;

                _context.Entry(inspection).State = EntityState.Modified;
            }
            try
            {
                _context.SaveChanges();
                ActionLog += "lives adjusted successfully!" + Environment.NewLine;
            }
            catch (Exception ex)
            {
                ActionLog += "Updating lives failed! " + ex.ToDetailedString() + Environment.NewLine;
            }
        }
        private bool UpdateComponentDetails()
        {
            var components = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Params.Id);
            foreach (var component in components)
            {
                OldComponentsCMU.Add(new OldCMU
                {
                    Id = (int)component.equnit_auto,
                    SmuAtInstall = (int)(component.eq_smu_at_install ?? 0),
                    CMU = (int)(component.cmu ?? 0)
                });
                component.cmu = Params.Components.Where(m => m.Id == component.equnit_auto).Select(m => m.HoursAtInstall).FirstOrDefault();
                if (!ReplacedComponentIds.Any(k => component.equnit_auto == k))
                {
                    component.smu_at_install = Params.SmuAtInstall;
                    component.eq_smu_at_install = Params.SmuAtInstall;
                    component.date_installed = Params.InstallationDate.ToLocalTime().Date;
                }
                _context.Entry(component).State = EntityState.Modified;
            }
            try
            {
                _context.SaveChanges();
                ActionLog += "Components detials updated successfully!" + Environment.NewLine;
                return true;
            }
            catch (Exception ex)
            {
                ActionLog += "Updating components details failed! " + ex.ToDetailedString() + Environment.NewLine;
                Message = "Action failed when updating components details!";
                return false;
            }
        }

        /// <summary>
        /// This method removes installed given components from database!!
        /// it is not moving them to inventory! use this method for setup components only
        /// </summary>
        /// <param name="removingComponents">Components to be removed from database</param>
        /// <returns></returns>
        private bool removeComponents(List<GENERAL_EQ_UNIT> removingComponents)
        {
            var k = removingComponents.Select(m => m.equnit_auto);
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.action_type_auto == (int)ActionType.InstallComponentOnSystemOnEquipment && k.Any(w => w == m.equnit_auto) && m.recordStatus == 0);
            _context.ACTION_TAKEN_HISTORY.RemoveRange(actions);
            _context.GENERAL_EQ_UNIT.RemoveRange(removingComponents);
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

        private bool MakeOtherSystemsOnline()
        {
            var _otherSystems = _context.LU_Module_Sub.Where(m => OtherSystems.Any(k => k == m.Module_sub_auto));
            foreach (var sys in _otherSystems)
            {
                sys.equipmentid_auto = Params.EquipmentId;
                _context.Entry(sys).State = EntityState.Modified;
            }
            try
            {
                _context.SaveChanges();
                ActionLog += "Other systems updated successfully!" + Environment.NewLine;
                return true;
            }
            catch (Exception ex)
            {
                /*
                 Dear Support team member:
                 If you get any call from anyone with this error message ('Action failed in a critical step')
                 you will possibly see only one system attached to that equipment!
                 This is happened because when we do undercarriage setup update we have to go one system at a time
                 and for that wee need to uninstall other exsiting systems from equipment
                 at the very end of the process we will attach them back to the system
                 If in the last step for any reason operation fails then we cannot bring them back to the equipment
                 this is what you will do manually 
                 set the equipmentid_auto of the lu_module_sub for those systems and you can find system ids the in ACTION_TAKEN_HISTORY
                 */
                ActionLog += "Updating the other systems failed! " + ex.ToDetailedString() + Environment.NewLine;
                Message = "Action failed in a critical step! You may need to contact system support!";
                return false;
            }
        }
        private bool MakeExistingActionsAvailable()
        {
            var _ids = ActionHistoryKeepIds.Select(m => m.HistoryId);
            var _actions = _context.ACTION_TAKEN_HISTORY.Where(m => _ids.Any(k => k == m.history_id)).ToList();
            foreach (var action in _actions)
            {
                var _item = ActionHistoryKeepIds.Where(m => action.history_id == m.HistoryId).FirstOrDefault();
                action.recordStatus = _item == null ? action.recordStatus : _item.Status;
                _context.Entry(action).State = EntityState.Modified;
            }
            var _currentHistory = _context.ACTION_TAKEN_HISTORY.Find(_current.Id);
            if (_currentHistory != null)
            {
                _currentHistory.system_auto_id = Params.Id;
                _context.Entry(_currentHistory).State = EntityState.Modified;
            }
            try
            {
                _context.SaveChanges();
                ActionLog += "Existing actions updated to previous status successfully!" + Environment.NewLine;
                return true;
            }
            catch (Exception ex)
            {
                ActionLog += "Updating the existing actions failed!" + ex.ToDetailedString() + Environment.NewLine;
                Message = "Action completed with warnings! Some actions may be hidden in history page";
                return false;
            }
        }
        public ActionStatus Validate()
        {
            Status = ActionStatus.Valid;
            return Status;
        }
        public ActionStatus Commit()
        {
            if (Status != ActionStatus.Valid)
            {
                ActionLog += "Operation Status is not valid" + Environment.NewLine;
                Status = ActionStatus.Failed;
                return Status;
            }
            var Components = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Params.Id);
            var paramsId = Params.Components.Select(n => n.Id).ToList();
            var removiongComponents = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Params.Id && !paramsId.Any(k => m.equnit_auto == k));
            var childToBeRemoved = new List<GENERAL_EQ_UNIT>();
            foreach (var parent in removiongComponents)
            {
                var childComparts = new Compart(_context, parent.compartid_auto).getChildComparts().Select(m => m.compartid_auto);
                childToBeRemoved.AddRange(Components.Where(m => childComparts.Any(k => k == m.compartid_auto)).ToList());
            }
            removiongComponents.ToList().AddRange(childToBeRemoved);
            //All child components will be removed automatically
            if (removeComponents(removiongComponents.GroupBy(m => m.equnit_auto).Select(m => m.FirstOrDefault()).ToList()))
            {
                //Components which are not in the list of current components should be removed from installed components!
                //Just for monitoring purposes
                string message = "";
                message += "Components removed successfully";
            }


            //↓↓↓↓↓ Adding all child compartments
            List<ComponentSetup> ChildsList = new List<ComponentSetup>();
            foreach (var cmpnt in Params.Components.OrderBy(m => m.InstallDate))
            {
                var childCompartments = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == cmpnt.Compart.Id).Select(m => m.ParentCompartment).ToList();
                int childPos = cmpnt.Pos + 1;
                foreach (var childCompart in childCompartments)
                {
                    ChildsList.Add(new ComponentSetup
                    {
                        Brand = cmpnt.Brand,
                        BudgetLife = cmpnt.BudgetLife,
                        Compart = new CompartV { Id = childCompart.compartid_auto, CompartStr = childCompart.compartid, CompartTitle = childCompart.compart, CompartType = new CompartTypeV { Id = childCompart.comparttype_auto, Title = childCompart.LU_COMPART_TYPE.comparttype, Order = childCompart.LU_COMPART_TYPE.sorder ?? 1 } },
                        EquipmentSMU = cmpnt.EquipmentSMU,
                        Grouser = cmpnt.Grouser,
                        HoursAtInstall = cmpnt.HoursAtInstall,
                        Id = 0,
                        InstallCost = 0,
                        InstallDate = cmpnt.InstallDate.ToLocalTime().Date,
                        Note = cmpnt.Note,
                        Pos = childPos,
                        Result = new ResultMessage(),
                        ShoeSize = cmpnt.ShoeSize,
                        SystemId = cmpnt.SystemId,
                        Validity = cmpnt.Validity,
                        listPosition = -1,
                        Points = -1
                    });
                    childPos++;
                }
            }
            Params.Components.AddRange(ChildsList);
            //↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑

            foreach (var comp in Params.Components.Where(m => m.Id == 0))
            {
                var componentParam = new SetupComponentParams
                {
                    BudgetLife = comp.BudgetLife,
                    CMU = comp.HoursAtInstall,
                    CompartId = comp.Compart.Id,
                    Cost = comp.InstallCost,
                    Id = comp.Id,
                    Life = comp.HoursAtInstall,
                    UserId = Params.UserId,
                    UserName = _current.ActionUser.userName
                };
                if (comp.Id == 0) //New Component Added
                {
                    var LogicalComponent = new Component(new UndercarriageContext());
                    comp.Result = LogicalComponent.CreateNewComponent(comp, _current.ActionUser.Id, _current.ActionUser.userName).Result;
                    if (comp.Result.OperationSucceed)
                    {
                        IEquipmentActionRecord EquipmentActionForComp = new BLL.Core.Domain.EquipmentActionRecord
                        {
                            EquipmentId = Params.EquipmentId,
                            ReadSmuNumber = comp.EquipmentSMU,
                            TypeOfAction = ActionType.InstallComponentOnSystemOnEquipment,
                            ActionDate = comp.InstallDate.ToLocalTime().Date,
                            ActionUser = _current.ActionUser,
                            Cost = comp.InstallCost,
                            Comment = "Component Setup in Update Undercarriage Setup",
                        };
                        using (Domain.Action compAction = new Domain.Action(new UndercarriageContext(), EquipmentActionForComp, new BLL.Core.Domain.InstallComponentOnSystemParams { Id = LogicalComponent.Id, Position = (byte)comp.Pos, SystemId = Params.Id, side = Params.Side }))
                        {
                            if (compAction.Operation.Start() == ActionStatus.Started)
                                if (compAction.Operation.Validate() == ActionStatus.Valid)
                                    if (compAction.Operation.Commit() == ActionStatus.Succeed)
                                    {
                                        comp.Result.Id = compAction.Operation.UniqueId;
                                        comp.Result.OperationSucceed = true;
                                        comp.SystemId = Params.Id;
                                    }
                            comp.Result.LastMessage = compAction.Operation.Message;
                            comp.Result.ActionLog = compAction.Operation.ActionLog;
                        }
                    }
                }
            }
            Available();
            UpdateLtds();
            AdjustLives();
            MakeExistingActionsAvailable();
            if (!MakeOtherSystemsOnline())
            {
                Status = ActionStatus.Failed;
            }
            else
            {
                Status = ActionStatus.Succeed;
            }
            return Status;
        }
        public ActionStatus Cancel()
        {
            throw new NotImplementedException();
        }
        public new void Dispose()
        {
            if (Status != ActionStatus.Succeed)
            {
                rollBack();
                MakeOtherSystemsOnline();
                var _ids = ActionHistoryModifiedIds.Select(k => k.HistoryId).ToList();
                var actions = _context.ACTION_TAKEN_HISTORY.Where(m => _ids.Any(k => k == m.history_id));
                foreach (var act in actions)
                {
                    act.recordStatus = ActionHistoryModifiedIds.Where(m => m.HistoryId == act.history_id).Select(m => m.Status).FirstOrDefault();
                    _context.Entry(act).State = EntityState.Modified;
                }
                _context.SaveChangesAsync();
            }
            else
            {
                _gContext = new DAL.GETContext();
                string _log = "";
                UpdateGETByAction(_current, ref _log);
                ActionLog += _log + Environment.NewLine;
            }
            _context.Dispose();
        }
    }
    internal class ActionHistoryModified
    {
        public int HistoryId { get; set; }
        public int Status { get; set; }
    }
    internal class OldCMU
    {
        public int Id { get; set; }
        public int CMU { get; set; }
        public int SmuAtInstall { get; set; }
    }
}
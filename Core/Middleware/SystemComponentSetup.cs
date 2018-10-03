using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Middleware
{
    public class SystemComponentSetup
    {
        public void storeSetupLogic(BLL.Core.Domain.UndercarriageSetupSystemViewModel systemModel,
            BLL.Core.Domain.EquipmentSystemsExistence eqStat,
            BLL.Core.Domain.SetupSystemParams param,
            int LogicalEquipmentId, BLL.Interfaces.IUser user, BLL.Core.Domain.Side selectedSide
            )
        {
            if (systemModel.Id == 0 && !eqStat.LeftChain) //NEW SYSTEM
            {
                BLL.Core.Domain.UCSystem leftChain = new BLL.Core.Domain.UCSystem(new DAL.UndercarriageContext());
                param.SerialNo = systemModel.SerialNo;
                param.Side = selectedSide;
                param.Life = systemModel.systemLife;
                param.SetupDate = systemModel.InstallationDate;
                if (leftChain.CreateNewSystem(param, LogicalEquipmentId))
                {
                    param.Id = leftChain.Id;
                    BLL.Interfaces.IEquipmentActionRecord EquipmentAction = new BLL.Core.Domain.EquipmentActionRecord
                    {
                        EquipmentId = LogicalEquipmentId,
                        ReadSmuNumber = systemModel.EquipmentSMU,
                        TypeOfAction = BLL.Core.Domain.ActionType.InstallSystemOnEquipment,
                        ActionDate = systemModel.InstallationDate,
                        ActionUser = user,
                        Cost = 0,
                        Comment = "System Setup"
                    };
                    using (BLL.Core.Domain.Action action = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), EquipmentAction, new BLL.Core.Domain.InstallSystemParams { Id = leftChain.Id, EquipmentId = LogicalEquipmentId, side = selectedSide }))
                    {
                        if (action.Operation.Start() == BLL.Core.Domain.ActionStatus.Started &&
                                                        action.Operation.Validate() == BLL.Core.Domain.ActionStatus.Valid &&
                                                        action.Operation.Commit() == BLL.Core.Domain.ActionStatus.Succeed)
                        {
                            foreach (var comp in systemModel.Components)
                            {
                                var componentParam = new BLL.Core.Domain.SetupComponentParams
                                {
                                    BudgetLife = comp.BudgetLife,
                                    CMU = comp.ComponentCurrentLife,
                                    CompartId = comp.CompartId,
                                    Cost = comp.Cost,
                                    Id = comp.Id,
                                    Life = comp.ComponentCurrentLife,
                                    UserId = user.Id,
                                    UserName = user.userName
                                };
                                var LogicalComponent = new BLL.Core.Domain.Component(new DAL.UndercarriageContext());
                                if (LogicalComponent.CreateNewComponent(componentParam))
                                {
                                    BLL.Interfaces.IEquipmentActionRecord EquipmentActionForComp = new BLL.Core.Domain.EquipmentActionRecord
                                    {
                                        EquipmentId = LogicalEquipmentId,
                                        ReadSmuNumber = comp.EquipmetSMU,
                                        TypeOfAction = BLL.Core.Domain.ActionType.InstallComponentOnSystemOnEquipment,
                                        ActionDate = comp.InstallationDate,
                                        ActionUser = user,
                                        Cost = 0,
                                        Comment = "Component Setup"
                                    };
                                    using (BLL.Core.Domain.Action compAction = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), EquipmentActionForComp, new BLL.Core.Domain.InstallComponentOnSystemParams { Id = LogicalComponent.Id, Position = comp.Position, SystemId = leftChain.Id, side = selectedSide }))
                                    {
                                        compAction.Operation.Start();
                                        compAction.Operation.Validate();
                                        compAction.Operation.Commit();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else //UPDATE SYSTEM
            {
                var LogicalSystem = new BLL.Core.Domain.UCSystem(new DAL.UndercarriageContext(), systemModel.Id);
                if (LogicalSystem.Id != 0 && LogicalSystem.Components != null)
                {
                    var removiongComponents = LogicalSystem.Components.Where(m => !systemModel.Components.Any(n => m.equnit_auto == n.Id));
                    if (LogicalSystem.removeComponents(removiongComponents.ToList()))
                    {
                        //Components which are not in the list of current components should be removed from installed components!
                        //Just for monitoring purposes
                        string message = "";
                        message += "Components removed successfully";
                    }

                    foreach (var comp in systemModel.Components)
                    {
                        var componentParam = new BLL.Core.Domain.SetupComponentParams
                        {
                            BudgetLife = comp.BudgetLife,
                            CMU = comp.ComponentCurrentLife,
                            CompartId = comp.CompartId,
                            Cost = comp.Cost,
                            Id = comp.Id,
                            Life = comp.ComponentCurrentLife,
                            UserId = user.Id,
                            UserName = user.userName
                        };
                        if (comp.Id == 0) //New Component Added
                        {
                            var LogicalComponent = new BLL.Core.Domain.Component(new DAL.UndercarriageContext());
                            if (LogicalComponent.CreateNewComponent(componentParam))
                            {
                                BLL.Interfaces.IEquipmentActionRecord EquipmentActionForComp = new BLL.Core.Domain.EquipmentActionRecord
                                {
                                    EquipmentId = LogicalEquipmentId,
                                    ReadSmuNumber = comp.EquipmetSMU,
                                    TypeOfAction = BLL.Core.Domain.ActionType.InstallComponentOnSystemOnEquipment,
                                    ActionDate = comp.InstallationDate,
                                    ActionUser = user,
                                    Cost = 0,
                                    Comment = "Component Setup"
                                };
                                using (BLL.Core.Domain.Action compAction = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), EquipmentActionForComp, new BLL.Core.Domain.InstallComponentOnSystemParams { Id = LogicalComponent.Id, Position = comp.Position, SystemId = systemModel.Id, side = selectedSide }))
                                {
                                    compAction.Operation.Start();
                                    compAction.Operation.Validate();
                                    compAction.Operation.Commit();
                                }
                            }
                        }
                        else
                        {
                            var LogicalComponent = new BLL.Core.Domain.Component(new DAL.UndercarriageContext(), comp.Id);
                            LogicalComponent.removeInstallationRecord();
                            LogicalComponent.UpdateComponentOnSetup(componentParam);
                            BLL.Interfaces.IEquipmentActionRecord EquipmentActionForComp = new BLL.Core.Domain.EquipmentActionRecord
                            {
                                EquipmentId = LogicalEquipmentId,
                                ReadSmuNumber = comp.EquipmetSMU,
                                TypeOfAction = BLL.Core.Domain.ActionType.InstallComponentOnSystemOnEquipment,
                                ActionDate = comp.InstallationDate,
                                ActionUser = user,
                                Cost = 0,
                                Comment = "Component Setup"
                            };
                            using (BLL.Core.Domain.Action compAction = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), EquipmentActionForComp, new BLL.Core.Domain.InstallComponentOnSystemParams { Id = LogicalComponent.Id, Position = comp.Position, SystemId = systemModel.Id, side = selectedSide }))
                            {
                                compAction.Operation.Start();
                                compAction.Operation.Validate();
                                compAction.Operation.Commit();
                            }
                        }
                    }
                }
            }
        }
    }
}
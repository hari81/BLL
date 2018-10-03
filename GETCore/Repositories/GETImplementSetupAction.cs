using BLL.Core.Domain;
using BLL.GETCore.Classes;
using BLL.Interfaces;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Repositories
{
    public class GETImplementSetupAction : BLL.Core.Domain.Action, BLL.Interfaces.IAction
    {
        private IEquipmentActionRecord pvActionRecord;
        private IEquipmentActionRecord removingActionRecord;
        private DAL.ACTION_TAKEN_HISTORY removingActionHistoryRecord;
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

        private GETImplementSetupParams Params;

        public GETImplementSetupAction(DbContext context, DbContext gContext, IEquipmentActionRecord actionRecord, GETImplementSetupParams Parameters)
            : base((GETContext) gContext, (UndercarriageContext)context)
        {
            Params = Parameters;
            _actionRecord = actionRecord;
            Status = ActionStatus.Close;
        }


        public ActionStatus Cancel()
        {
            Status = ActionStatus.Cancelled;
            Message = "Operation cancelled";
            return Status;
        }

        public ActionStatus Commit()
        {
            Status = ActionStatus.Succeed;
            return Status;
        }

        public ActionStatus Start()
        {
            if (Status == ActionStatus.Close)
            {
                int result = -1;
                int changesSaved = 0;

                // Save a new row in GET_EVENTS.
                GET_EVENTS getEvent = new GET_EVENTS
                {
                    user_auto = Params.UserAuto,
                    action_auto = (int) Params.ActionType,
                    recorded_date = Params.RecordedDate,
                    event_date = Params.EventDate,
                    comment = Params.Comment,
                    cost = Params.Cost
                };
                _gContext.GET_EVENTS.Add(getEvent);
                _gContext.SaveChanges();

                // Check whether there are any inspections against this implement.
                var isExists_inspection = _gContext.GET_IMPLEMENT_INSPECTION
                                            .Where(gii => gii.get_auto == Params.GetAuto)
                                            .OrderBy(o => o.inspection_date)
                                            .ThenBy(t => t.inspection_auto)
                                            .Select(s => new
                                            {
                                                s.inspection_auto,
                                                s.ltd
                                            }).FirstOrDefault();

                // Check whether there are any inventory actions against this implement.
                var isExists_inventoryAction = (from inv in _gContext.GET_EVENTS_INVENTORY
                                                join gei in _gContext.GET_EVENTS_IMPLEMENT
                                                    on inv.implement_events_auto equals gei.implement_events_auto
                                                where gei.get_auto == Params.GetAuto
                                                select new
                                                {
                                                    inv.inventory_events_auto,
                                                    inv.implement_event.ltd
                                                }).FirstOrDefault();

                // Check whether there are any implement setup events for this implement.
                var isExists_implementSetup = (from ge in _gContext.GET_EVENTS
                                               join ga in _gContext.GET_ACTIONS
                                                 on ge.action_auto equals ga.actions_auto
                                               join gei in _gContext.GET_EVENTS_IMPLEMENT
                                                 on ge.events_auto equals gei.events_auto
                                               where (ga.actions_auto == (int)GETActionType.Implement_Setup
                                                    || ga.actions_auto == (int)GETActionType.Implement_Updated)
                                                 && gei.get_auto == Params.GetAuto
                                               orderby ge.events_auto descending
                                               select new
                                               {
                                                   ge.events_auto,
                                                   gei.implement_events_auto
                                               }).ToArray();

                // Now update the GET_EVENTS_IMPLEMENT table.
                GET_EVENTS_IMPLEMENT getImplementEvent = new GET_EVENTS_IMPLEMENT
                {
                    events_auto = getEvent.events_auto,
                    get_auto = Params.GetAuto,
                    ltd = Params.ImplementLTD
                };
                _gContext.GET_EVENTS_IMPLEMENT.Add(getImplementEvent);

                // Determine the maximum valid range for the specified implement's LTD at setup.
                int maxValidLTD = 0;
                if(isExists_inspection != null)
                {
                    maxValidLTD = isExists_inspection.ltd;
                }
                if(isExists_inventoryAction != null)
                {
                    if(isExists_inventoryAction.ltd < maxValidLTD)
                    {
                        maxValidLTD = isExists_inventoryAction.ltd;
                    }
                }

                var recentImplement = ((isExists_inspection == null) && (isExists_inventoryAction == null));

                // Check that inspections and inventory actions have not been done for this implement.
                if (recentImplement)
                {
                    // Update the GET_EVENTS_COMPONENT table.
                    if (isExists_implementSetup.Count() > 0)
                    {
                        getEvent.action_auto = (int) GETActionType.Implement_Updated;

                        var componentInfo = (from gc in _gContext.GET_COMPONENT
                                             where gc.get_auto == Params.GetAuto
                                                && gc.active == true
                                             orderby gc.get_component_auto
                                             select new
                                             {
                                                 gc.get_component_auto,
                                                 gc.cmu
                                             });
                        var componentInfoArray = componentInfo.ToArray();

                        for (int i = 0; i < componentInfoArray.Count(); i++)
                        {
                            _gContext.GET_EVENTS_COMPONENT.Add(
                                new GET_EVENTS_COMPONENT
                                {
                                    events_auto = getEvent.events_auto,
                                    get_component_auto = componentInfoArray[i].get_component_auto,
                                    ltd = componentInfoArray[i].cmu
                                });
                        }
                    }

                    try
                    {
                        changesSaved = _gContext.SaveChanges();
                    }
                    catch (Exception ex1)
                    {

                    }

                    result = changesSaved > 0 ? Params.ImplementLTD : -1;
                }

                // Else if this is an existing implement, but LTD or date needs to be updated.
                else if (!recentImplement && (Params.ImplementLTD <= maxValidLTD))
                {
                    if(isExists_implementSetup.Count() > 0)
                    {
                        getEvent.action_auto = (int)GETActionType.Implement_Updated;

                        var componentInfo = (from gc in _gContext.GET_COMPONENT
                                             where gc.get_auto == Params.GetAuto
                                                && gc.active == true
                                             orderby gc.get_component_auto
                                             select new
                                             {
                                                 gc.get_component_auto,
                                                 gc.cmu
                                             });
                        var componentInfoArray = componentInfo.ToArray();

                        for (int i = 0; i < componentInfoArray.Count(); i++)
                        {
                            _gContext.GET_EVENTS_COMPONENT.Add(
                                new GET_EVENTS_COMPONENT
                                {
                                    events_auto = getEvent.events_auto,
                                    get_component_auto = componentInfoArray[i].get_component_auto,
                                    ltd = componentInfoArray[i].cmu
                                });
                        }
                    }

                    try
                    {
                        changesSaved = _gContext.SaveChanges();
                    }
                    catch (Exception ex1)
                    {

                    }

                    result = changesSaved > 0 ? Params.ImplementLTD : -1;
                }

                // Else, rollback to previous LTD
                else
                {
                    if (isExists_implementSetup.Count() > 0)
                    {
                        getEvent.action_auto = (int)GETActionType.Implement_Updated;

                        var prevImplementSetup = isExists_implementSetup[0].implement_events_auto;
                        var prevLTD = _gContext.GET_EVENTS_IMPLEMENT.Find(prevImplementSetup).ltd;

                        var get = _gContext.GET.Find(Params.GetAuto);
                        get.impsetup_hours = prevLTD;

                        getImplementEvent.ltd = prevLTD;

                        try
                        {
                            changesSaved = _gContext.SaveChanges();
                        }
                        catch (Exception ex2)
                        {

                        }

                        result = changesSaved > 0 ? prevLTD : -1;
                    }
                }

                Status = ActionStatus.Started;
                Message = result.ToString();
            }

            return Status;
        }

        public ActionStatus Validate()
        {
            if (Status != ActionStatus.Started)
            {
                return Status;
            }

            /* TO DO: Validation checks
             * 
             */

            Status = ActionStatus.Valid;
            return Status;
        }
    }
}
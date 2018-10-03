using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Interfaces;
using System.Data.Entity;
using DAL;
using BLL.GETCore.Classes;

namespace BLL.GETCore.Repositories
{
    public class GETUndoComponentReplacementAction : BLL.Core.Domain.Action, BLL.Interfaces.IAction
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

        private GETUndoComponentReplacementParams Params;

        public GETUndoComponentReplacementAction(DbContext context, IEquipmentActionRecord actionRecord, GETUndoComponentReplacementParams Parameters)
            : base((GETContext) context)
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
            //throw new NotImplementedException();
            Status = ActionStatus.Succeed;
            return Status;
        }

        public ActionStatus Start()
        {
            if (Status == ActionStatus.Close)
            {
                string result = "0";

                // Undo Component Replacement action
                var action_undoCompReplace = (from ga in _gContext.GET_ACTIONS
                                              where ga.action_name == GET_ACTIONS_constants.GET_EVENT_UndoComponentReplacement
                                              select ga.actions_auto
                                         ).FirstOrDefault();

                // Component Replacement action
                var action_compReplace = (from ga in _gContext.GET_ACTIONS
                                          where ga.action_name == GET_ACTIONS_constants.GET_EVENT_ComponentReplacement
                                          select ga.actions_auto
                                         ).FirstOrDefault();

                // Find the Component Inspection record and the component auto.
                var eqmtImplementComp = _gContext.GET_COMPONENT_INSPECTION.Find(Params.ComponentInspectionAuto);
                int gcAuto = eqmtImplementComp.get_component_auto;

                // Find the Component and the specific Implement.
                var getComp = _gContext.GET_COMPONENT.Find(gcAuto);
                var gs = _gContext.GET.Find(getComp.get_auto);

                // Equipment ID and GET auto.
                long eqmt = gs.equipmentid_auto.Value;
                int gAuto = gs.get_auto;

                int iMeterReading = (int)_gContext.EQUIPMENTs.Find(eqmt).currentsmu.Value;



                // Find the Event Auto for the most recent Component Replacement for this component.
                var replaceEvent = (from ge in _gContext.GET_EVENTS
                                    join ga in _gContext.GET_ACTIONS
                                       on ge.action_auto equals ga.actions_auto
                                    join gec in _gContext.GET_EVENTS_COMPONENT
                                       on ge.events_auto equals gec.events_auto
                                    where ga.action_name == GET_ACTIONS_constants.GET_EVENT_ComponentReplacement
                                       && gec.get_component_auto == gcAuto
                                    orderby ge.events_auto descending
                                    select gec.events_auto).FirstOrDefault();

                // Find the Component Replacement Event autos.
                var replaceComponentEvents = (from gec in _gContext.GET_EVENTS_COMPONENT
                                              where gec.events_auto == replaceEvent
                                              select gec.component_events_auto).ToArray();

                int changesSaved = 0;

                // Create an event for the undo component replacement action.
                GET_EVENTS getEvents = new GET_EVENTS
                {
                    user_auto = Params.UserAuto,
                    action_auto = (int) Params.ActionType,
                    recorded_date = Params.RecordedDate,
                    event_date = Params.EventDate,
                    comment = Params.Comment,
                    cost = Params.Cost
                };
                _gContext.GET_EVENTS.Add(getEvents);
                _gContext.SaveChanges();

                int prevLtd = 0;

                // Find the Component Replacement Events and the Component records.
                if (replaceComponentEvents.Count() == 2)
                {
                    var cEvt_1 = _gContext.GET_EVENTS_COMPONENT.Find(replaceComponentEvents[0]);
                    var cEvt_2 = _gContext.GET_EVENTS_COMPONENT.Find(replaceComponentEvents[1]);

                    var gComp1 = _gContext.GET_COMPONENT.Find(cEvt_1.get_component_auto);
                    var gComp2 = _gContext.GET_COMPONENT.Find(cEvt_2.get_component_auto);

                    // Two scenarios for Undo Component Replacement
                    if (gComp1.active)
                    {
                        //prevLtd = cEvt_2.ltd;
                        gComp1.active = false;
                        gComp2.active = true;
                        prevLtd = eqmtImplementComp.ltd;

                        // Replacement occurred before the inspection
                        if (eqmtImplementComp.get_component_auto == gComp1.get_component_auto)
                        {
                            eqmtImplementComp.get_component_auto = gComp2.get_component_auto;
                        }
                        // Replacement occurred after the inspection
                        else
                        {

                        }
                    }
                    else
                    {
                        //prevLtd = cEvt_1.ltd;
                        gComp1.active = true;
                        gComp2.active = false;
                        prevLtd = eqmtImplementComp.ltd;

                        // Replacement occurred before the inspection
                        if (eqmtImplementComp.get_component_auto == gComp2.get_component_auto)
                        {
                            eqmtImplementComp.get_component_auto = gComp1.get_component_auto;
                        }
                        // Replacement occurred after the inspection
                        else
                        {

                        }
                    }

                    try
                    {
                        changesSaved = _gContext.SaveChanges();

                        if (changesSaved > 0)
                        {
                            // Add events with the SMU before undoing replacement.
                            _gContext.GET_EVENTS_COMPONENT.Add(
                            new GET_EVENTS_COMPONENT
                            {
                                events_auto = getEvents.events_auto,
                                get_component_auto = gComp1.get_component_auto,
                                ltd = gComp1.cmu,
                                recordStatus = 0
                            });

                            _gContext.GET_EVENTS_COMPONENT.Add(
                            new GET_EVENTS_COMPONENT
                            {
                                events_auto = getEvents.events_auto,
                                get_component_auto = gComp2.get_component_auto,
                                ltd = gComp2.cmu,
                                recordStatus = 1
                            });

                            changesSaved = _gContext.SaveChanges();
                        }
                    }
                    catch (Exception ex1)
                    {

                    }
                }

                // Undo replacement for the Component Inspection record.
                eqmtImplementComp.replace = false;
                _gContext.SaveChanges();

                result = changesSaved > 0 ? prevLtd.ToString() : "";
                Message = result;

                Status = ActionStatus.Started;
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
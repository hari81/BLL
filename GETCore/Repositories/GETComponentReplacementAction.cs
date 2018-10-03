using BLL.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using System.Data.Entity;
using DAL;

namespace BLL.GETCore.Repositories
{
    public class GETComponentReplacementAction : BLL.Core.Domain.Action, BLL.Interfaces.IAction
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

        private GETComponentReplacementParams Params;

        public GETComponentReplacementAction(DbContext context, IEquipmentActionRecord actionRecord, GETComponentReplacementParams Parameters)
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
                string result = "";

                // Find the Component Inspection record and the component auto.
                var eqmtImplementComp = _gContext.GET_COMPONENT_INSPECTION.Find(Params.ComponentInspectionAuto);
                int gcAuto = eqmtImplementComp.get_component_auto;

                // Find the Implement Inspection record
                var implementInspectionAuto = eqmtImplementComp.implement_inspection_auto;
                var implementInspection = _gContext.GET_IMPLEMENT_INSPECTION.Find(implementInspectionAuto);
                int inspectionMeterReading = implementInspection.meter_reading;

                // Find the Component and the specific Implement.
                var getComp = _gContext.GET_COMPONENT.Find(gcAuto);
                var gs = _gContext.GET.Find(getComp.get_auto);

                // Equipment ID and GET auto.
                long eqmt = gs.equipmentid_auto.Value;
                int gAuto = gs.get_auto;

                // Implement ltd at time of event.
                var implement_ltd = Params.MeterReading - (int)gs.installsmu + (int)gs.impsetup_hours;

                // Create a new entry for the replaced component.
                GET_COMPONENT newComponent = new GET_COMPONENT
                {
                    make_auto = getComp.make_auto,
                    get_auto = getComp.get_auto,
                    observation_list_auto = getComp.observation_list_auto,
                    cmu = 0,
                    install_date = Params.RecordedDate,
                    ltd_at_setup = 0,
                    req_measure = getComp.req_measure,
                    initial_length = getComp.initial_length,
                    worn_length = getComp.worn_length,
                    price = getComp.price,
                    part_no = getComp.part_no,
                    schematic_component_auto = getComp.schematic_component_auto,
                    active = true
                };
                _gContext.GET_COMPONENT.Add(newComponent);
                _gContext.SaveChanges();

                // Mark the old component as inactive.
                getComp.active = false;
                _gContext.SaveChanges();

                int changesSaved = 0;
                int newLTD = 0;

                // If the component replacement occurs after the inspection
                if (Params.MeterReading >= inspectionMeterReading)
                {
                    // Determine the previous SMU value for the equipment and thus the SMU offset to use.
                    int eqmtPrevSMU = (int)_gContext.EQUIPMENTs.Find(eqmt).currentsmu.Value;
                    int SMU_offset = Params.MeterReading - eqmtPrevSMU;

                    // Create a new Event to record the replacement.
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

                    try
                    {
                        changesSaved = _gContext.SaveChanges();

                        if(changesSaved > 0)
                        {
                            // Add a new event for the implement ltd at this point in time.
                            _gContext.GET_EVENTS_IMPLEMENT.Add(
                                new GET_EVENTS_IMPLEMENT
                                {
                                    events_auto = getEvents.events_auto,
                                    get_auto = gAuto,
                                    ltd = implement_ltd
                                });
                            changesSaved = _gContext.SaveChanges();
                        }

                        if (changesSaved > 0)
                        {
                            // Add an event with the SMU before replacement.
                            _gContext.GET_EVENTS_COMPONENT.Add(
                            new GET_EVENTS_COMPONENT
                            {
                                events_auto = getEvents.events_auto,
                                get_component_auto = getComp.get_component_auto,
                                ltd = getComp.cmu + SMU_offset,
                                recordStatus = 1
                            });

                            // Add an event for the new component
                            _gContext.GET_EVENTS_COMPONENT.Add(
                            new GET_EVENTS_COMPONENT
                            {
                                events_auto = getEvents.events_auto,
                                get_component_auto = newComponent.get_component_auto,
                                ltd = 0,
                                recordStatus = 0
                            });

                            changesSaved = _gContext.SaveChanges();

                            // Update CMU.
                            getComp.cmu += SMU_offset;
                            newComponent.cmu = 0;
                            changesSaved = _gContext.SaveChanges();
                        }
                    }
                    catch (Exception ex1)
                    {

                    }
                }
                // Handle the case where the inspection occurs after the component was replaced.
                else
                {
                    // Determine the SMU offset to use.
                    int SMU_offset = inspectionMeterReading - Params.MeterReading;

                    // Update the component auto in the GET_COMPONENT_INSPECTION table.
                    eqmtImplementComp.get_component_auto = newComponent.get_component_auto;
                    _gContext.SaveChanges();

                    // Create a new Event to record the replacement.
                    GET_EVENTS getEvents = new GET_EVENTS
                    {
                        user_auto = Params.UserAuto,
                        action_auto = (int) Params.ActionType,
                        recorded_date = Params.RecordedDate,
                        event_date = eqmtImplementComp.inspection_date,
                        comment = Params.Comment,
                        cost = Params.Cost
                    };
                    _gContext.GET_EVENTS.Add(getEvents);

                    try
                    {
                        changesSaved = _gContext.SaveChanges();

                        if (changesSaved > 0)
                        {
                            // Add a new event for the implement ltd at this point in time.
                            _gContext.GET_EVENTS_IMPLEMENT.Add(
                                new GET_EVENTS_IMPLEMENT
                                {
                                    events_auto = getEvents.events_auto,
                                    get_auto = gAuto,
                                    ltd = implement_ltd
                                });
                            changesSaved = _gContext.SaveChanges();
                        }

                        if (changesSaved > 0)
                        {
                            // Add an event with the SMU before replacement.
                            _gContext.GET_EVENTS_COMPONENT.Add(
                            new GET_EVENTS_COMPONENT
                            {
                                events_auto = getEvents.events_auto,
                                get_component_auto = getComp.get_component_auto,
                                ltd = getComp.cmu - SMU_offset,
                                recordStatus = 1
                            });

                            // Add an event for the new component
                            _gContext.GET_EVENTS_COMPONENT.Add(
                            new GET_EVENTS_COMPONENT
                            {
                                events_auto = getEvents.events_auto,
                                get_component_auto = newComponent.get_component_auto,
                                ltd = SMU_offset,
                                recordStatus = 0
                            });

                            changesSaved = _gContext.SaveChanges();
                            newLTD = SMU_offset;

                            // Update CMU.
                            getComp.cmu -= SMU_offset;
                            newComponent.cmu = SMU_offset;
                            changesSaved = _gContext.SaveChanges();

                            // Update the Life in the component inspection record.
                            if(changesSaved > 0)
                            {
                                eqmtImplementComp.ltd = newComponent.cmu;
                                changesSaved = _gContext.SaveChanges();
                            }
                        }
                    }
                    catch (Exception ex2)
                    {

                    }
                }

                // Mark the replacement as occurred for the Component Inspection record.
                eqmtImplementComp.replace = true;
                _gContext.SaveChanges();

                result = changesSaved > 0 ? newLTD.ToString() : "";
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
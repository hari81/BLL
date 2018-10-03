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
    public class MoveImplementToInventoryAction : BLL.Core.Domain.Action, BLL.Interfaces.IAction
    {
        private IEquipmentActionRecord pvActionRecord;
        private IEquipmentActionRecord removingActionRecord;
        private DAL.ACTION_TAKEN_HISTORY removingActionHistoryRecord;
        private ActionStatus pvActionStatus;
        private string pvActionLog;
        private string pvMessage;
        private EventManagement eventManagement = new EventManagement();

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

        private MoveImplementToInventoryParams Params;

        public MoveImplementToInventoryAction(DbContext context, DbContext gContext, IEquipmentActionRecord actionRecord, MoveImplementToInventoryParams Parameters)
            : base((GETContext)gContext, (UndercarriageContext)context)
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
            bool SMUValidationPassed = false;

            if (Status == ActionStatus.Close)
            {
                int iEquipmentIdAuto = longNullableToint(Params.EquipmentId);

                // Check that the Action validation passes before updating UC.
                if (ActionSMUValidation(iEquipmentIdAuto, Params.MeterReading, Params.EventDate))
                {
                    string log = "";
                    _actionRecord = UpdateEquipmentByAction(_actionRecord, ref log);
                    ActionLog += log;
                    Message = base.Message;

                    if (_actionRecord != null && _actionRecord.Id != 0)
                    {
                        _actionRecord.TypeOfAction = ActionType.GETAction;
                    }

                    SMUValidationPassed = true;
                }
                else
                {
                    Status = ActionStatus.Invalid;
                    Message = "SMU validation failed!";
                }

                if (SMUValidationPassed)
                {
                    // Create an event record.
                    int actionAuto = (int)GETActionType.Move_Implement_To_Inventory;
                    var removeImplementEvent = eventManagement.createGETEvent(Params.UserId,
                        actionAuto, Params.EventDate, Params.Comment, Params.Cost, Params.RecordedDate);
                    _gContext.GET_EVENTS.Add(removeImplementEvent);
                    int _changesSaved = _gContext.SaveChanges();

                    //Create Events for equipment, implement and components.
                    if (_changesSaved > 0)
                    {
                        GET_EVENTS previousEvent = new GET_EVENTS();
                        GET_EVENTS_EQUIPMENT previousEquipmentEvent = new GET_EVENTS_EQUIPMENT();

                        // Perform a cascading update for equipment, implement and components.
                        long SMUUpdateEventID = eventManagement.cascadeUpdateWithNewSMU(_gContext, Params.EquipmentId,
                            Params.MeterReading, Params.UserId, Params.EventDate, Params.Comment, Params.Cost);

                        // Error occurred.
                        if (SMUUpdateEventID < 0)
                        {
                            Status = IndicateFailedActionStatus(removeImplementEvent);
                            return Status;
                        }
                        else
                        {
                            // Find the most recent (valid) smu and ltd for the equipment.
                            previousEquipmentEvent = eventManagement.findPreviousEquipmentEvent(_gContext, Params.EquipmentId, Params.EventDate);

                            if (previousEquipmentEvent == null)
                            {
                                Status = IndicateFailedActionStatus(removeImplementEvent);
                                Message = "ERROR: No previous events found for this equipment.";
                                return Status;
                            }

                            // No change to meter reading
                            if (SMUUpdateEventID == 0)
                            {
                                previousEvent = _gContext.GET_EVENTS.Find(previousEquipmentEvent.events_auto);
                            }
                            // SMU is changed
                            else
                            {
                                previousEvent = _gContext.GET_EVENTS.Find(SMUUpdateEventID);
                                previousEquipmentEvent = _gContext.GET_EVENTS_EQUIPMENT
                                    .Where(e => e.events_auto == SMUUpdateEventID)
                                    .FirstOrDefault();
                            }
                        }


                        int SMU_difference = Params.MeterReading - previousEquipmentEvent.smu;

                        // Only perform an update for positive SMU changes.
                        if (SMU_difference >= 0)
                        {
                            // Create an equipment event.
                            var equipmentEvent = eventManagement.createEquipmentEvent(removeImplementEvent.events_auto,
                                Params.EquipmentId, Params.MeterReading, (previousEquipmentEvent.ltd + SMU_difference));
                            _gContext.GET_EVENTS_EQUIPMENT.Add(equipmentEvent);
                            int _changesSavedEquipmentEvent = _gContext.SaveChanges();

                            // Find the most recent (valid) ltd for the implement.
                            var previousImplementEvent = eventManagement.findPreviousImplementEvent(_gContext, Params.ImplementId, Params.EventDate);

                            if (previousImplementEvent == null)
                            {
                                // Mark the SMU Update event as invalid.
                                if (SMUUpdateEventID > 0)
                                {
                                    previousEvent.recordStatus = 1;
                                    _gContext.SaveChanges();
                                }

                                Status = IndicateFailedActionStatus(removeImplementEvent);
                                Message = "ERROR: No previous events found for this implement.";
                                return Status;
                            }

                            // Create an implement event.
                            var implementEvent = eventManagement.createImplementEvent((int)Params.ImplementId,
                                previousImplementEvent.ltd + SMU_difference, removeImplementEvent.events_auto);
                            _gContext.GET_EVENTS_IMPLEMENT.Add(implementEvent);
                            int _changesSavedImplementEvent = _gContext.SaveChanges();

                            int? workshop = null;
                            if (Params.WorkshopId != 0)
                            {
                                workshop = Params.WorkshopId;
                            }

                            int? repairer = null;
                            if (Params.RepairerId != 0)
                            {
                                repairer = Params.RepairerId;
                            }

                            if (_changesSavedImplementEvent > 0)
                            {
                                // Create an inventory event.
                                var inventoryEvent = eventManagement.createInventoryEvent(implementEvent.implement_events_auto,
                                    Params.JobsiteId, Params.StatusId, workshop);
                                _gContext.GET_EVENTS_INVENTORY.Add(inventoryEvent);
                                int _changesSavedInventoryEvent = _gContext.SaveChanges();

                                // Create component events.
                                bool componentEventsCreated = eventManagement.updateComponentsForGET(_gContext,
                                    Params.ImplementId, SMU_difference, removeImplementEvent.events_auto, Params.EventDate);
                                if (!componentEventsCreated)
                                {
                                    // Mark the SMU Update event as invalid.
                                    if (SMUUpdateEventID > 0)
                                    {
                                        previousEvent.recordStatus = 1;
                                        _gContext.SaveChanges();
                                    }

                                    Status = IndicateFailedActionStatus(removeImplementEvent);
                                    Message = "ERROR: Unable to process this change for components.";
                                    return Status;
                                }
                            }

                            // Now update the implement and inventory records.
                            if ((_changesSavedEquipmentEvent > 0) && (_changesSavedImplementEvent > 0))
                            {
                                // Remove from equipment
                                var implementRecord = _gContext.GET.Find(Params.ImplementId);
                                implementRecord.equipmentid_auto = null;
                                implementRecord.on_equipment = false;
                                int _changesSavedImplement = _gContext.SaveChanges();

                                // Add an entry to the inventory table since it's now removed from the equipment.
                                if (_changesSavedImplement > 0)
                                {
                                    GET_INVENTORY inventoryRecord = new GET_INVENTORY
                                    {
                                        get_auto = (int) Params.ImplementId,
                                        jobsite_auto = Params.JobsiteId,
                                        status_auto = Params.StatusId,
                                        workshop_auto = workshop,
                                        ltd = previousImplementEvent.ltd + SMU_difference,
                                        modified_date = Params.EventDate,
                                        modified_user = (int) Params.UserId
                                    };
                                    _gContext.GET_INVENTORY.Add(inventoryRecord);
                                    int _changesSavedInventory = _gContext.SaveChanges();

                                    if (_changesSavedInventory > 0)
                                    {
                                        // Link up the GET Event record with the UC Action Taken History record.
                                        int GETEventsId = (int)removeImplementEvent.events_auto;
                                        int ActionTakenHistoryId = _actionRecord.Id;

                                        var UCHistoryRecord = _context.ACTION_TAKEN_HISTORY.Find(ActionTakenHistoryId);
                                        if (UCHistoryRecord != null)
                                        {
                                            UCHistoryRecord.GETActionHistoryId = GETEventsId;
                                        }
                                        removeImplementEvent.UCActionHistoryId = ActionTakenHistoryId;

                                        _context.SaveChanges();
                                        _gContext.SaveChanges();

                                        Status = ActionStatus.Started;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Status = ActionStatus.Failed;
                    }
                }
            }

            return Status;
        }

        private ActionStatus IndicateFailedActionStatus(GET_EVENTS GETEvent)
        {
            GETEvent.recordStatus = 1;
            _gContext.SaveChanges();

            return ActionStatus.Failed;
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
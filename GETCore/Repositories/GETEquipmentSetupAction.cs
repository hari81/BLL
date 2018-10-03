using BLL.Core.Domain;
using BLL.Interfaces;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Repositories
{
    public class GETEquipmentSetupAction : BLL.Core.Domain.Action, BLL.Interfaces.IAction
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

        private GETEquipmentSetupParams Params;

        public GETEquipmentSetupAction(DbContext context, DbContext gContext, IEquipmentActionRecord actionRecord, GETEquipmentSetupParams Parameters)
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
            //throw new NotImplementedException();
            Status = ActionStatus.Succeed;
            return Status;
        }

        public ActionStatus Start()
        {
            bool SMUValidationPassed = false;

            if (Status == ActionStatus.Close)
            {
                int iEquipmentIdAuto = longNullableToint(Params.EquipmentId);

                // Check that the Action validation passes before starting.
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

                if(SMUValidationPassed)
                {
                    int changesSaved = 0;
                    try
                    {
                        // Create a record for the Equipment setup event.
                        GET_EVENTS getEvents = new GET_EVENTS
                        {
                            user_auto = Params.UserAuto,
                            action_auto = (int)Params.ActionType,
                            recorded_date = Params.RecordedDate,
                            event_date = Params.EventDate,
                            comment = Params.Comment,
                            cost = Params.Cost
                        };
                        _gContext.GET_EVENTS.Add(getEvents);
                        changesSaved = _gContext.SaveChanges();

                        if (changesSaved > 0)
                        {
                            // Create a new equipment event record.
                            GET_EVENTS_EQUIPMENT getEventsEquipment = new GET_EVENTS_EQUIPMENT
                            {
                                events_auto = getEvents.events_auto,
                                equipment_auto = Params.EquipmentId,
                                smu = Params.MeterReading,
                                ltd = Params.EquipmentLTD
                            };

                            _gContext.GET_EVENTS_EQUIPMENT.Add(getEventsEquipment);
                            changesSaved = _gContext.SaveChanges();

                            // Link up the GET Event record with the UC Action Taken History record.
                            if (changesSaved > 0)
                            {
                                int GETEventsId = (int)getEvents.events_auto;
                                int ActionTakenHistoryId = _actionRecord.Id;
                                var eqEntity = _context.EQUIPMENTs.Find(Params.EquipmentId);
                                var UCHistoryRecord = _context.ACTION_TAKEN_HISTORY.Find(ActionTakenHistoryId);
                                if (UCHistoryRecord != null)
                                {
                                    UCHistoryRecord.GETActionHistoryId = GETEventsId;
                                    UCHistoryRecord.recordStatus = (int)RecordStatus.Available;
                                    _context.Entry(UCHistoryRecord).State = EntityState.Modified;
                                }
                                if(eqEntity != null)
                                {
                                    eqEntity.ActionTakenHistoryId = _actionRecord.Id;
                                    _context.Entry(eqEntity).State = EntityState.Modified;
                                }
                                getEvents.UCActionHistoryId = ActionTakenHistoryId;

                                _context.SaveChanges();
                                _gContext.SaveChanges();

                                Status = ActionStatus.Started;
                            }
                        }
                    }
                    catch (Exception ex1)
                    {

                    }
                }
                else
                {
                    Status = ActionStatus.Failed;
                }
            }
            Available();
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
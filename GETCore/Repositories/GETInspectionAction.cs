using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Interfaces;
using System.Data.Entity;
using DAL;

namespace BLL.GETCore.Repositories
{
    public class GETInspectionAction : BLL.Core.Domain.Action, BLL.Interfaces.IAction
    {
        private IEquipmentActionRecord pvActionRecord;
        private IEquipmentActionRecord removingActionRecord;
        private DAL.ACTION_TAKEN_HISTORY removingActionHistoryRecord;
        private ActionStatus pvActionStatus;
        private string pvActionLog;
        private string pvMessage;

        private long Event_Equipment_Id { get; set; }
        private long Event_Implement_Id { get; set; }

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

        private GETInspectionParams Params;

        public GETInspectionAction(DbContext context, IEquipmentActionRecord actionRecord, GETInspectionParams Parameters)
            : base((GETContext)context, new UndercarriageContext())
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
            //var currentActionRecord = _gContext.GET_EVENTS.Find(_actionRecord.Id);
            //currentActionRecord.action_auto = (int)GETActionType.Inspection;
            //_gContext.SaveChanges();

            //throw new NotImplementedException();
            Status = ActionStatus.Succeed;
            return Status;
        }

        public ActionStatus Start()
        {
            if (Status == ActionStatus.Close)
            {
                int iEquipmentIdAuto = longNullableToint(Params.EquipmentIdAuto);

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
                }
                

                var GET_EVENT_Inspection = (from ga in _gContext.GET_ACTIONS
                                            where ga.action_name == "Inspection"
                                            select ga.actions_auto).FirstOrDefault();

                var GTs = _gContext.GET.Find(Params.GetAuto);
                // Insert log in the Events table
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
                _gContext.SaveChanges();

                // Save equipment meter reading
                var eqmt = _gContext.EQUIPMENTs.Find(GTs.equipmentid_auto);
                int previousMeterReading = 0;
                if (eqmt != null)
                {
                    previousMeterReading = (int)eqmt.currentsmu.Value;
                    eqmt.currentsmu = Params.MeterReading;
                    eqmt.last_reading_date = Params.EventDate;
                }
                _gContext.SaveChanges();

                

                var GET_EVENT_EquipmentSetup = (from ga in _gContext.GET_ACTIONS
                                                where ga.action_name == "Equipment Setup"
                                                select ga.actions_auto
                                         ).FirstOrDefault();

                // Get the last equipment setup event details.
                var lastEqmtSetupAction_auto = (from es in _gContext.GET_EVENTS_EQUIPMENT
                                                join ev in _gContext.GET_EVENTS
                                                 on es.events_auto equals ev.events_auto
                                                where es.equipment_auto == eqmt.equipmentid_auto
                                                 && ev.action_auto == GET_EVENT_EquipmentSetup
                                                orderby es.equipment_events_auto descending
                                                select es.equipment_events_auto).FirstOrDefault();
                var lastEqmtSetup = _gContext.GET_EVENTS_EQUIPMENT.Find(lastEqmtSetupAction_auto);

                // Update the equipment SMU and LTD.
                int SMU_diff = 0;
                int LTD_diff = 0;
                if (lastEqmtSetup != null)
                {
                    // Use the previous equipment setup if available.
                    var prevLTD = lastEqmtSetup.ltd;
                    var prevSMU = lastEqmtSetup.smu;
                    SMU_diff = Params.MeterReading - prevSMU;
                    LTD_diff = prevLTD + SMU_diff;
                }
                else
                {
                    // Otherwise use the diff between the meter reading and previous SMU.
                    SMU_diff = Params.MeterReading - (int)eqmt.smu_at_start.Value;
                    LTD_diff = (int)eqmt.LTD_at_start.Value + SMU_diff;
                }

                _gContext.GET_EVENTS_EQUIPMENT.Add(
                    new GET_EVENTS_EQUIPMENT
                    {
                        events_auto = getEvents.events_auto,
                        equipment_auto = GTs.equipmentid_auto.Value,
                        smu = Params.MeterReading,
                        ltd = LTD_diff > 0 ? LTD_diff : 0
                    });
                _gContext.SaveChanges();

                var implement_ltd = Params.MeterReading - (int)GTs.installsmu + (int)GTs.impsetup_hours;
                _gContext.GET_EVENTS_IMPLEMENT.Add(
                    new GET_EVENTS_IMPLEMENT
                    {
                        events_auto = getEvents.events_auto,
                        get_auto = (int)Params.GetAuto,
                        ltd = implement_ltd
                    });
                _gContext.SaveChanges();

                Message = getEvents.events_auto.ToString();
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
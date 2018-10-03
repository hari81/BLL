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
    public class GETFlagIgnoredAction : BLL.Core.Domain.Action, BLL.Interfaces.IAction
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

        private GETFlagIgnoredParams Params;

        public GETFlagIgnoredAction(DbContext context, IEquipmentActionRecord actionRecord, GETFlagIgnoredParams Parameters)
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
                bool result = false;

                var ComponentInspection = _gContext.GET_COMPONENT_INSPECTION.Find(Params.ComponentInspectionAuto);
                int gcAuto = ComponentInspection.get_component_auto;

                var GetComponent = _gContext.GET_COMPONENT.Find(gcAuto);
                var gs = _gContext.GET.Find(GetComponent.get_auto);

                // Toggle the flag_ignored option ON.
                ComponentInspection.flag_ignored = true;
                _gContext.SaveChanges();

                // Record the event.
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

                int changesSaved = 0;
                try
                {
                    changesSaved = _gContext.SaveChanges();

                    if(changesSaved > 0)
                    {
                        _gContext.GET_EVENTS_COMPONENT.Add(
                            new GET_EVENTS_COMPONENT
                            {
                                get_component_auto = gcAuto,
                                ltd = ComponentInspection.ltd,
                                events_auto = getEvents.events_auto
                            });
                        changesSaved = _gContext.SaveChanges();
                    }
                }
                catch (Exception ex1)
                {

                }

                Status = ActionStatus.Started;
                result = changesSaved > 0 ? true : false;
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
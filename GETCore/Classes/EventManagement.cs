using BLL.Core.Domain;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Classes
{
    public class EventManagement
    {
        /// <summary>
        /// Creates and returns a new object for the GET_EVENTS table.
        /// </summary>
        /// <param name="userAuto"></param>
        /// <param name="actionAuto"></param>
        /// <param name="eventDate"></param>
        /// <param name="comment"></param>
        /// <param name="cost"></param>
        /// <returns></returns>
        public GET_EVENTS createGETEvent(long userAuto, int actionAuto, DateTime eventDate, string comment, decimal cost)
        {
            GET_EVENTS GETEvent = new GET_EVENTS();
            GETEvent.user_auto = userAuto;
            GETEvent.action_auto = actionAuto;
            GETEvent.event_date = eventDate;
            GETEvent.comment = comment;
            GETEvent.cost = cost;
            GETEvent.recorded_date = DateTime.Now;
            GETEvent.UCActionHistoryId = null;
            GETEvent.recordStatus = 0;

            return GETEvent;
        }

        /// <summary>
        /// Creates and returns a new object for the GET_EVENTS table. 
        /// Also takes in a parameter for the recorded date.
        /// </summary>
        /// <param name="userAuto"></param>
        /// <param name="actionAuto"></param>
        /// <param name="eventDate"></param>
        /// <param name="comment"></param>
        /// <param name="cost"></param>
        /// <param name="recordedDate"></param>
        /// <returns></returns>
        public GET_EVENTS createGETEvent(long userAuto, int actionAuto, DateTime eventDate, string comment, decimal cost, DateTime recordedDate)
        {
            GET_EVENTS GETEvent = new GET_EVENTS();
            GETEvent.user_auto = userAuto;
            GETEvent.action_auto = actionAuto;
            GETEvent.event_date = eventDate;
            GETEvent.comment = comment;
            GETEvent.cost = cost;
            GETEvent.recorded_date = recordedDate;
            GETEvent.UCActionHistoryId = null;
            GETEvent.recordStatus = 0;

            return GETEvent;
        }

        /// <summary>
        /// Creates and returns a new object for the GET_EVENTS_EQUIPMENT table.
        /// </summary>
        /// <param name="eventsAuto"></param>
        /// <param name="equipmentAuto"></param>
        /// <param name="smu"></param>
        /// <param name="ltd"></param>
        /// <returns></returns>
        public GET_EVENTS_EQUIPMENT createEquipmentEvent(long eventsAuto, long equipmentAuto, int smu, int ltd)
        {
            GET_EVENTS_EQUIPMENT equipmentEvent = new GET_EVENTS_EQUIPMENT();
            equipmentEvent.equipment_auto = equipmentAuto;
            equipmentEvent.events_auto = eventsAuto;
            equipmentEvent.smu = smu;
            equipmentEvent.ltd = ltd;

            return equipmentEvent;
        }

        /// <summary>
        /// Creates and returns a new object for the GET_EVENTS_IMPLEMENT table.
        /// </summary>
        /// <param name="getAuto"></param>
        /// <param name="ltd"></param>
        /// <param name="eventsAuto"></param>
        /// <returns></returns>
        public GET_EVENTS_IMPLEMENT createImplementEvent(int getAuto, int ltd, long eventsAuto)
        {
            GET_EVENTS_IMPLEMENT implementEvent = new GET_EVENTS_IMPLEMENT();
            implementEvent.get_auto = getAuto;
            implementEvent.ltd = ltd;
            implementEvent.events_auto = eventsAuto;

            return implementEvent;
        }

        /// <summary>
        /// Creates and returns a new object for the GET_EVENTS_COMPONENT table.
        /// </summary>
        /// <param name="getComponentAuto"></param>
        /// <param name="ltd"></param>
        /// <param name="eventsAuto"></param>
        /// <param name="recordStatus"></param>
        /// <returns></returns>
        public GET_EVENTS_COMPONENT createComponentEvent(int getComponentAuto, int ltd, long eventsAuto, int recordStatus)
        {
            GET_EVENTS_COMPONENT componentEvent = new GET_EVENTS_COMPONENT();
            componentEvent.get_component_auto = getComponentAuto;
            componentEvent.ltd = ltd;
            componentEvent.events_auto = eventsAuto;
            componentEvent.recordStatus = recordStatus;

            return componentEvent;
        }

        /// <summary>
        /// Creates and returns a new object for the GET_EVENTS_INVENTORY table.
        /// </summary>
        /// <param name="implementEventAuto"></param>
        /// <param name="jobsiteAuto"></param>
        /// <param name="statusAuto"></param>
        /// <param name="workshopAuto"></param>
        /// <returns></returns>
        public GET_EVENTS_INVENTORY createInventoryEvent(long implementEventAuto, long jobsiteAuto, int statusAuto, int? workshopAuto)
        {
            GET_EVENTS_INVENTORY inventoryEvent = new GET_EVENTS_INVENTORY();
            inventoryEvent.implement_events_auto = implementEventAuto;
            inventoryEvent.jobsite_auto = jobsiteAuto;
            inventoryEvent.status_auto = statusAuto;
            inventoryEvent.workshop_auto = workshopAuto;

            return inventoryEvent;
        }

        /// <summary>
        /// Find and return the previous equipment event given the equipment Id 
        /// and an event date.
        /// </summary>
        /// <param name="_gContext"></param>
        /// <param name="equipmentAuto"></param>
        /// <param name="eventDate"></param>
        /// <returns></returns>
        public GET_EVENTS_EQUIPMENT findPreviousEquipmentEvent(GETContext _gContext, long equipmentAuto, DateTime eventDate)
        {
            GET_EVENTS_EQUIPMENT result = new GET_EVENTS_EQUIPMENT();
            DateTime eventDate2 = new DateTime(eventDate.Year, eventDate.Month, eventDate.Day, 23, 59, 59);

            result = (from ee in _gContext.GET_EVENTS_EQUIPMENT
                      join ge in _gContext.GET_EVENTS
                        on ee.events_auto equals ge.events_auto
                      where ee.equipment_auto == equipmentAuto
                        && ge.event_date <= eventDate2
                        && ge.recordStatus == 0
                      orderby ge.event_date descending, ge.events_auto descending
                      select ee).FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Find and return the previous implement event given the implement (GET) auto 
        /// and an event date.
        /// </summary>
        /// <param name="_gContext"></param>
        /// <param name="getAuto"></param>
        /// <param name="eventDate"></param>
        /// <returns></returns>
        public GET_EVENTS_IMPLEMENT findPreviousImplementEvent(GETContext _gContext, long GETAuto, DateTime eventDate)
        {
            GET_EVENTS_IMPLEMENT result = new GET_EVENTS_IMPLEMENT();
            DateTime eventDate2 = new DateTime(eventDate.Year, eventDate.Month, eventDate.Day, 23, 59, 59);

            result = (from ie in _gContext.GET_EVENTS_IMPLEMENT
                      join ge in _gContext.GET_EVENTS
                        on ie.events_auto equals ge.events_auto
                      where ie.get_auto == GETAuto
                        && ge.event_date <= eventDate2
                        && ge.recordStatus == 0
                      orderby ge.event_date descending, ge.events_auto descending
                      select ie).FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Find and return the previous component event given the GET Component auto 
        /// and an event date.
        /// </summary>
        /// <param name="_gContext"></param>
        /// <param name="GETComponentAuto"></param>
        /// <param name="eventDate"></param>
        /// <returns></returns>
        public GET_EVENTS_COMPONENT findPreviousComponentEvent(GETContext _gContext, long GETComponentAuto, DateTime eventDate)
        {
            GET_EVENTS_COMPONENT result = new GET_EVENTS_COMPONENT();
            DateTime eventDate2 = new DateTime(eventDate.Year, eventDate.Month, eventDate.Day, 23, 59, 59);

            result = (from c in _gContext.GET_EVENTS_COMPONENT
                      join ge in _gContext.GET_EVENTS
                          on c.events_auto equals ge.events_auto
                      where c.get_component_auto == GETComponentAuto
                          && ge.event_date <= eventDate2
                          && ge.recordStatus == 0
                      orderby ge.event_date descending, ge.events_auto descending
                      select c).FirstOrDefault();

            return result;
        }

        /// <summary>
        /// Update all implements and components with new SMU.
        /// </summary>
        /// <param name="_gContext"></param>
        /// <param name="equipmentAuto"></param>
        /// <param name="meter_reading"></param>
        /// <param name="userId"></param>
        /// <param name="eventDate"></param>
        /// <param name="comment"></param>
        /// <param name="cost"></param>
        /// <returns></returns>
        public long cascadeUpdateWithNewSMU(GETContext _gContext, long equipmentAuto, int meter_reading, 
            long userId, DateTime eventDate, string comment, decimal cost)
        {
            long ERROR_CODE = -1;
            long result = ERROR_CODE;

            // Find the previous equipment event.
            var previousEquipmentEvent = findPreviousEquipmentEvent(_gContext, equipmentAuto, eventDate);
            if (previousEquipmentEvent == null)
            {
                return ERROR_CODE;
            }

            int SMU_difference = meter_reading - previousEquipmentEvent.smu;
            int newLTD = (previousEquipmentEvent.ltd + SMU_difference);

            if(SMU_difference > 0)
            {
                // Create an event record.
                int actionAuto = (int)GETActionType.Equipment_SMU_Changed;
                var SMUChangedEvent = createGETEvent(userId, actionAuto, eventDate, comment, cost);
                _gContext.GET_EVENTS.Add(SMUChangedEvent);
                int _changesSaved = _gContext.SaveChanges();

                if (_changesSaved > 0)
                {
                    // Create an equipment event.
                    var equipmentEvent = createEquipmentEvent(SMUChangedEvent.events_auto, equipmentAuto, meter_reading, newLTD);
                    _gContext.GET_EVENTS_EQUIPMENT.Add(equipmentEvent);
                    int _changesSavedEquipmentEvent = _gContext.SaveChanges();

                    if (_changesSavedEquipmentEvent > 0)
                    {
                        // Create implement events
                        bool update_success = updateImplementsForEquipment(_gContext, equipmentAuto, SMU_difference, SMUChangedEvent.events_auto, eventDate);
                        if (update_success)
                        {
                            result = SMUChangedEvent.events_auto;
                        }
                        else
                        {
                            // Failed to save Implement or Component events.
                            SMUChangedEvent.recordStatus = 1;
                            result = ERROR_CODE;
                        }
                    }
                    else
                    {
                        // Failed to save Equipment event.
                        SMUChangedEvent.recordStatus = 1;
                        result = ERROR_CODE;
                    }

                }
                else
                {
                    // Failed to save new GET_EVENT.
                    SMUChangedEvent.recordStatus = 1;
                    result = ERROR_CODE;
                }
            }
            else if (SMU_difference == 0)
            {
                result = 0;
            }

            return result;
        }

        /// <summary>
        /// Update all implements for an equipment when the SMU is changed.
        /// </summary>
        /// <param name="_gContext"></param>
        /// <param name="equipmentAuto"></param>
        /// <param name="SMU_difference"></param>
        /// <param name="eventAuto"></param>
        /// <param name="eventDate"></param>
        /// <returns></returns>
        public bool updateImplementsForEquipment(GETContext _gContext, long equipmentAuto, int SMU_difference, 
            long eventAuto, DateTime eventDate)
        {
            bool result = false;

            var GETs = _gContext.GET
                .Where(g => g.equipmentid_auto == equipmentAuto && g.on_equipment == true)
                .ToArray();

            // No implements to update, so just return back.
            if(GETs == null)
            {
                return true;
            }
            else if(GETs.Length == 0)
            {
                return true;
            }

            for(int n=0; n<GETs.Count(); n++)
            {
                int GETAuto = GETs[n].get_auto;
                
                var previousImplementEvent = findPreviousImplementEvent(_gContext, GETAuto, eventDate);
                if (previousImplementEvent == null)
                {
                    return false;
                }

                int newLTD = (previousImplementEvent.ltd + SMU_difference);

                var implementEvent = createImplementEvent(GETAuto, newLTD, eventAuto);
                _gContext.GET_EVENTS_IMPLEMENT.Add(implementEvent);
                int _changesSavedImplementEvent = _gContext.SaveChanges();

                if (_changesSavedImplementEvent == 0)
                {
                    return false;
                }
                else
                {
                    result = updateComponentsForGET(_gContext, GETAuto, SMU_difference, eventAuto, eventDate);
                }
            }

            return result;
        }

        /// <summary>
        /// Update all components for an implement when the SMU is changed.
        /// </summary>
        /// <param name="_gContext"></param>
        /// <param name="GETAuto"></param>
        /// <param name="SMU_difference"></param>
        /// <param name="eventAuto"></param>
        /// <param name="eventDate"></param>
        /// <returns></returns>
        public bool updateComponentsForGET(GETContext _gContext, long GETAuto, int SMU_difference, 
            long eventAuto, DateTime eventDate)
        {
            bool result = false;

            var GETComponents = _gContext.GET_COMPONENT
                                    .Where(gc => gc.get_auto == GETAuto && gc.active == true)
                                    .ToArray();

            // Handle case when there are no components.
            if(GETComponents == null)
            {
                return true;
            }
            else if(GETComponents.Length == 0)
            {
                return true;
            }

            for (int n = 0; n < GETComponents.Count(); n++)
            {
                int GETComponentAuto = GETComponents[n].get_component_auto;

                var previousComponentEvent = findPreviousComponentEvent(_gContext, GETComponentAuto, eventDate);
                if(previousComponentEvent == null)
                {
                    return false;
                }

                int newLTD = (previousComponentEvent.ltd + SMU_difference);

                var componentEvent = createComponentEvent(GETComponentAuto, newLTD, eventAuto, 0);
                _gContext.GET_EVENTS_COMPONENT.Add(componentEvent);
                int _changesSavedComponentEvent = _gContext.SaveChanges();

                if(_changesSavedComponentEvent == 0)
                {
                    return false;
                }
                else
                {
                    result = true;
                }
            }

            return result;
        }
    }
}
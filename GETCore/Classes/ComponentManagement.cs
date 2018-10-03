using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.GETCore.Classes.ViewModel;
using System.Threading.Tasks;

namespace BLL.GETCore.Classes
{
    public class ComponentManagement
    {
        private EventManagement eventManagement = new EventManagement();

        public List<GETComponentDetailsVM> ReturnComponentsForGET(int get_auto)
        {
            List<GETComponentDetailsVM> result = new List<GETComponentDetailsVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var get_components = (from gc in dataEntities.GET_COMPONENT
                                  join g in dataEntities.GET
                                    on gc.get_auto equals g.get_auto
                                  join gsc in dataEntities.GET_SCHEMATIC_COMPONENT
                                    on gc.schematic_component_auto equals gsc.schematic_component_auto
                                  join ct in dataEntities.LU_COMPART_TYPE
                                    on gsc.comparttype_auto equals ct.comparttype_auto
                                  join m in dataEntities.MAKE
                                    on gc.make_auto equals m.make_auto
                                  where gc.get_auto == get_auto && gc.active == true
                                  select new 
                                  {
                                      get_component_auto = gc.get_component_auto,
                                      condition = 0,
                                      require_measurement = gc.req_measure,
                                      initial_length = gc.initial_length,
                                      worn_length = gc.worn_length,
                                      component = ct.comparttype,
                                      part_number = gc.part_no,
                                      make = m.makedesc,
                                      cost = (int)gc.price,
                                      life = 0
                                  }).ToList();

                for(int i=0; i< get_components.Count; i++)
                {
                    var gcAuto = get_components[i].get_component_auto;

                    var life_record = dataEntities.GET_EVENTS_COMPONENT
                        .Where(c => c.get_component_auto == gcAuto)
                        .OrderByDescending(e => e.component_events_auto)
                        .FirstOrDefault();

                    var inspection_record = dataEntities.GET_COMPONENT_INSPECTION
                        .Where(c => c.get_component_auto == gcAuto)
                        .OrderByDescending(e => e.inspection_auto)
                        .FirstOrDefault();

                    int ltd = 0;
                    if(life_record != null)
                    {
                        ltd = life_record.ltd;
                    }

                    bool req_measure = get_components[i].require_measurement.Value;
                    decimal current_condition = 0;
                    if (inspection_record != null)
                    {
                        decimal measurement = inspection_record.measurement;
                        decimal initial_length = get_components[i].initial_length.Value;
                        decimal worn_length = get_components[i].worn_length.Value;
                        
                        if ((req_measure == true) && (initial_length - worn_length > 0))
                        {
                            current_condition = ((initial_length - measurement) / (initial_length - worn_length)) * 100;
                        }
                    }

                    if (req_measure == false)
                    {
                        current_condition = -1;
                    }

                    result.Add(new GETComponentDetailsVM
                    {
                        componentId = get_components[i].get_component_auto,
                        condition = (int) current_condition,
                        component = get_components[i].component,
                        part_number = get_components[i].part_number,
                        make = get_components[i].make,
                        cost = get_components[i].cost,
                        life = ltd
                    });

                }
            }

            return result;
        }

        /// <summary>
        /// Record a Component Repair action for a component in Inventory.
        /// </summary>
        /// <param name="componentId"></param>
        /// <param name="cost"></param>
        /// <param name="date"></param>
        /// <param name="comment"></param>
        /// <param name="authUserId"></param>
        /// <returns></returns>
        public bool RecordComponentRepair(int componentId, decimal cost, DateTime date, string comment, long authUserId)
        {
            bool result = false;

            using (var dataEntities = new DAL.GETContext())
            {
                int _changesSaved_GETEvent = 0;
                int _changesSaved_ComponentEvent = 0;
                int _changesSaved_ImplementEvent = 0;

                // Create a new entry in GET_EVENTS.
                var GETEvent = eventManagement.createGETEvent(authUserId, (int) Core.Domain.GETActionType.Component_Repair, date, comment, cost);
                dataEntities.GET_EVENTS.Add(GETEvent);
                _changesSaved_GETEvent = dataEntities.SaveChanges();

                // Create a new entry in GET_EVENTS_COMPONENT.
                if (_changesSaved_GETEvent > 0)
                {
                    var previousComponentEvent = dataEntities.GET_EVENTS_COMPONENT
                        .Where(gc => gc.get_component_auto == componentId)
                        .OrderByDescending(ce => ce.component_events_auto)
                        .FirstOrDefault();
                    int previousComponentLTD = 0;
                    if (previousComponentEvent != null)
                    {
                        previousComponentLTD = previousComponentEvent.ltd;
                    }

                    var componentEvent = eventManagement.createComponentEvent(componentId, previousComponentLTD, GETEvent.events_auto, 0);
                    dataEntities.GET_EVENTS_COMPONENT.Add(componentEvent);
                    _changesSaved_ComponentEvent = dataEntities.SaveChanges();
                }

                // Create a new entry in GET_EVENTS_IMPLEMENT.
                if (_changesSaved_GETEvent > 0)
                {
                    var GETComponent = dataEntities.GET_COMPONENT.Find(componentId);
                    if (GETComponent != null)
                    {
                        var previousImplementEvent = dataEntities.GET_EVENTS_IMPLEMENT
                            .Where(e => e.get_auto == GETComponent.get_auto)
                            .OrderByDescending(ie => ie.implement_events_auto)
                            .FirstOrDefault();
                        int previousImplementLTD = 0;
                        if(previousImplementEvent != null)
                        {
                            previousImplementLTD = previousImplementEvent.ltd;
                        }

                        var implementEvent = eventManagement.createImplementEvent(GETComponent.get_auto, previousImplementLTD, GETEvent.events_auto);
                        dataEntities.GET_EVENTS_IMPLEMENT.Add(implementEvent);
                        _changesSaved_ImplementEvent = dataEntities.SaveChanges();
                    }
                }

                if((_changesSaved_GETEvent > 0) && (_changesSaved_ComponentEvent > 0)
                    && (_changesSaved_ImplementEvent > 0))
                {
                    GETEvent.recordStatus = 0;
                    result = true;
                }
                else
                {
                    GETEvent.recordStatus = 1;
                }
                dataEntities.SaveChanges();
            }

            return result;
        }

        /// <summary>
        /// Record a Component Replace action for a component in Inventory.
        /// </summary>
        /// <param name="componentId"></param>
        /// <param name="cost"></param>
        /// <param name="date"></param>
        /// <param name="comment"></param>
        /// <param name="authUserId"></param>
        /// <returns></returns>
        public bool RecordComponentReplace(int componentId, decimal cost, DateTime date, string comment, long authUserId)
        {
            bool result = false;

            using (var dataEntities = new DAL.GETContext())
            {
                int _changesSaved_GETEvent = 0;
                int _changesSaved_ImplementEvent = 0;
                int _changesSaved_NewComponent = 0;
                int _changesSaved_OldComponentEvent = 0;
                int _changesSaved_NewComponentEvent = 0;

                DateTime recordedDate = DateTime.Now;

                var GETComponent = dataEntities.GET_COMPONENT.Find(componentId);

                // Create a new entry in GET_EVENTS.
                var GETEvent = eventManagement.createGETEvent(authUserId, (int)Core.Domain.GETActionType.Component_Replacement, date, comment, cost, recordedDate);
                dataEntities.GET_EVENTS.Add(GETEvent);
                _changesSaved_GETEvent = dataEntities.SaveChanges();

                // Create a new entry in GET_EVENTS_IMPLEMENT.
                if (_changesSaved_GETEvent > 0)
                {            
                    if (GETComponent != null)
                    {
                        var previousImplementEvent = dataEntities.GET_EVENTS_IMPLEMENT
                            .Where(e => e.get_auto == GETComponent.get_auto)
                            .OrderByDescending(ie => ie.implement_events_auto)
                            .FirstOrDefault();
                        int previousImplementLTD = 0;
                        if (previousImplementEvent != null)
                        {
                            previousImplementLTD = previousImplementEvent.ltd;
                        }

                        var implementEvent = eventManagement.createImplementEvent(GETComponent.get_auto, previousImplementLTD, GETEvent.events_auto);
                        dataEntities.GET_EVENTS_IMPLEMENT.Add(implementEvent);
                        _changesSaved_ImplementEvent = dataEntities.SaveChanges();
                    }
                }

                // Create a new component entry.
                GET_COMPONENT newComponent = new GET_COMPONENT
                {
                    make_auto = GETComponent.make_auto,
                    get_auto = GETComponent.get_auto,
                    observation_list_auto = GETComponent.observation_list_auto,
                    cmu = 0,
                    install_date = recordedDate,
                    ltd_at_setup = 0,
                    req_measure = GETComponent.req_measure,
                    initial_length = GETComponent.initial_length,
                    worn_length = GETComponent.worn_length,
                    price = cost,
                    part_no = GETComponent.part_no,
                    schematic_component_auto = GETComponent.schematic_component_auto,
                    active = true
                };
                dataEntities.GET_COMPONENT.Add(newComponent);
                _changesSaved_NewComponent = dataEntities.SaveChanges();

                // Mark the old component as inactive.
                if(_changesSaved_NewComponent > 0)
                {
                    GETComponent.active = false;
                    dataEntities.SaveChanges();
                }

                // Create an entry in GET_EVENTS_COMPONENT for the component being replaced.
                var oldComponentEvent = eventManagement.createComponentEvent(GETComponent.get_component_auto,
                    GETComponent.cmu, GETEvent.events_auto, 1);
                dataEntities.GET_EVENTS_COMPONENT.Add(oldComponentEvent);
                _changesSaved_OldComponentEvent = dataEntities.SaveChanges();

                // Create an entry in GET_EVENTS_COMPONENT for the new component.
                var newComponentEvent = eventManagement.createComponentEvent(newComponent.get_component_auto,
                    newComponent.cmu, GETEvent.events_auto, 0);
                dataEntities.GET_EVENTS_COMPONENT.Add(newComponentEvent);
                _changesSaved_NewComponentEvent = dataEntities.SaveChanges();

                if ((_changesSaved_GETEvent > 0) && (_changesSaved_OldComponentEvent > 0)
                    && (_changesSaved_NewComponentEvent > 0) && (_changesSaved_ImplementEvent > 0))
                {
                    GETEvent.recordStatus = 0;
                    result = true;
                }
                else
                {
                    oldComponentEvent.recordStatus = 0;
                    newComponentEvent.recordStatus = 1;
                    GETEvent.recordStatus = 1;
                }
                dataEntities.SaveChanges();
            }

            return result;
        }

        /// <summary>
        /// Returns the minimum date for the component repair based on 
        /// the date that the implement was moved to inventory, and whether 
        /// a component was already replaced.
        /// </summary>
        /// <param name="componentId"></param>
        /// <returns></returns>
        public string ReturnMinDateForRepair(int componentId)
        {
            string result = "";

            using (var dataEntities = new DAL.GETContext())
            {
                int ACTION_MovedToInventory = (int) Core.Domain.GETActionType.Move_Implement_To_Inventory;
                int ACTION_ComponentReplacement = (int)Core.Domain.GETActionType.Component_Replacement;

                var implement = dataEntities.GET_COMPONENT.Find(componentId);
                if(implement != null)
                {
                    // Get the last 'moved to inventory' event for this component.
                    var movedToInventoryDate = GetDateOfLastImplementEvent(implement.get_auto, ACTION_MovedToInventory, dataEntities);

                    // Get the last 'component replaced' event.
                    var componentReplaceDate = GetDateOfLastComponentEvent(componentId, ACTION_ComponentReplacement, dataEntities);

                    // The date of the repair must be on the same day or after the implement 
                    // was last moved from equipment to inventory. If that component has 
                    // been replaced, then the date of the repair must be after the date of 
                    // the replacement. 
                    if (movedToInventoryDate != null)
                    {
                        string dateFormat = "yyyy-MM-dd";

                        DateTime? mostRecentEventDate = ReturnMostRecentDateTime(movedToInventoryDate, componentReplaceDate);

                        if (mostRecentEventDate != null)
                        {
                            result = mostRecentEventDate.Value.ToString(dateFormat);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the minimum date for the component replace, based on 
        /// the date when the implement last had a repair, or replacement 
        /// recorded against it.
        /// </summary>
        /// <param name="componentId"></param>
        /// <returns></returns>
        public string ReturnMinDateForReplace(int componentId)
        {
            string result = "";

            using (var dataEntities = new DAL.GETContext())
            {
                int ACTION_MovedToInventory = (int)Core.Domain.GETActionType.Move_Implement_To_Inventory;
                int ACTION_ComponentReplacement = (int)Core.Domain.GETActionType.Component_Replacement;
                int ACTION_ComponentRepair = (int)Core.Domain.GETActionType.Component_Repair;

                var implement = dataEntities.GET_COMPONENT.Find(componentId);
                if (implement != null)
                {
                    // Get the last 'moved to inventory' event for this component.
                    var movedToInventoryDate = GetDateOfLastImplementEvent(implement.get_auto, ACTION_MovedToInventory, dataEntities);

                    // Get the last 'component repair' event.
                    var componentRepairDate = GetDateOfLastComponentEvent(componentId, ACTION_ComponentRepair, dataEntities);

                    // Get the last 'component replaced' event.
                    var componentReplaceDate = GetDateOfLastComponentEvent(componentId, ACTION_ComponentReplacement, dataEntities);

                    // The date of the repair must be on the same day or after the implement 
                    // was last moved from equipment to inventory. If that component has 
                    // been replaced / repaired, then the min date must be after the date of 
                    // the replacement or a repair
                    if(movedToInventoryDate != null)
                    {
                        string dateFormat = "yyyy-MM-dd";

                        DateTime? recentEventDate = ReturnMostRecentDateTime(movedToInventoryDate, componentReplaceDate);
                        DateTime? mostRecentEventDate = ReturnMostRecentDateTime(recentEventDate, componentRepairDate);

                        if(mostRecentEventDate != null)
                        {
                            result = mostRecentEventDate.Value.ToString(dateFormat);
                        }
                    }
                }
            }

            return result;
        }

        private DateTime? GetDateOfLastImplementEvent(int getAuto, int action, GETContext _context)
        {
            DateTime? result = null;

            var lastEvent = (from e in _context.GET_EVENTS
                             join ei in _context.GET_EVENTS_IMPLEMENT
                                 on e.events_auto equals ei.events_auto
                             where ei.get_auto == getAuto
                                 && e.action_auto == action
                             orderby e.event_date descending, e.events_auto descending
                             select new
                             {
                                 eventDate = e.event_date
                             }).FirstOrDefault();

            if (lastEvent != null)
            {
                result = lastEvent.eventDate;
            }

            return result;
        }

        private DateTime? GetDateOfLastComponentEvent(int componentId, int action, GETContext _context)
        {
            DateTime? result = null;

            var lastEvent = (from e in _context.GET_EVENTS
                             join ec in _context.GET_EVENTS_COMPONENT
                                 on e.events_auto equals ec.events_auto
                             where ec.get_component_auto == componentId
                                 && e.action_auto == action
                             orderby e.event_date descending, e.events_auto descending
                             select new
                             {
                                 eventDate = e.event_date
                             }).FirstOrDefault();

            if (lastEvent != null)
            {
                result = lastEvent.eventDate;
            }

            return result;
        }

        private DateTime? ReturnMostRecentDateTime(DateTime? dt1, DateTime? dt2)
        {
            DateTime? result;

            // Both items are null.
            if ((dt1 == null) && (dt2 == null))
            {
                result = null;
            }
            // One of the items is null.
            else if ((dt1 == null) && (dt2 != null))
            {
                result = dt2;
            }
            else if ((dt1 != null) && (dt2 == null))
            {
                result = dt1;
            }
            // Both items have values.
            else
            {
                if(dt1.Value >= dt2.Value)
                {
                    result = dt1;
                }
                else
                {
                    result = dt2;
                }
            }

            return result;
        }

        /// <summary>
        /// Get the component details (or Measurement Points, according to the new nomenclature).
        /// </summary>
        /// <param name="implement_auto"></param>
        /// <param name="get_auto"></param>
        /// <returns></returns>
        public List<MeasurementPointDetails> GetMeasurementPointDetails(int implement_auto, int get_auto)
        {
            List<MeasurementPointDetails> result = new List<MeasurementPointDetails>();

            using (var _context = new DAL.GETContext())
            {
                if ((implement_auto == 0) && (get_auto > 0))
                {
                    implement_auto = (int) _context.GET.Find(get_auto).implement_auto;
                }

                var MPs_Template = (from ic in _context.GET_IMPLEMENT_COMPARTTYPE
                                    join lc in _context.LU_COMPART_TYPE
                                      on ic.comparttype_auto equals lc.comparttype_auto
                                    join gsc in _context.GET_SCHEMATIC_COMPONENT
                                      on lc.comparttype_auto equals gsc.comparttype_auto
                                    where ic.implement_auto == implement_auto
                                      && gsc.active == true
                                    select new
                                    {
                                        Id = gsc.schematic_component_auto,
                                        Name = lc.comparttype,
                                        Make = 0,
                                        ObservationListId = 0,
                                        InitialLength = 0,
                                        WornLength = 0,
                                        PartNo = ""
                                    }).ToList();

                var MPs_Detail = (from gc in _context.GET_COMPONENT
                                  join gsc in _context.GET_SCHEMATIC_COMPONENT
                                    on gc.schematic_component_auto equals gsc.schematic_component_auto
                                  join lc in _context.LU_COMPART_TYPE
                                    on gsc.comparttype_auto equals lc.comparttype_auto
                                  where gc.get_auto == get_auto 
                                    && gc.active == true
                                    && gsc.active == true
                                  select new 
                                  {
                                      Id = gc.get_component_auto,
                                      Name = lc.comparttype,
                                      Make = gc.make_auto == null ? 0 : gc.make_auto.Value,
                                      ObservationListId = gc.observation_list_auto == null ? 0 : gc.observation_list_auto.Value,
                                      InitialLength = gc.initial_length.Value,
                                      WornLength = gc.worn_length.Value,
                                      PartNo = gc.part_no
                                  }).ToList();

                // If the Measurement Points have not been configured yet, show the template.
                if(MPs_Detail.Count == 0)
                {
                    for (int i = 0; i < MPs_Template.Count; i++)
                    {
                        result.Add(new MeasurementPointDetails
                        {
                            Id = MPs_Template[i].Id,
                            Name = MPs_Template[i].Name,
                            Make = MPs_Template[i].Make,
                            ObservationListId = MPs_Template[i].ObservationListId,
                            InitialLength = MPs_Template[i].InitialLength,
                            WornLength = MPs_Template[i].WornLength,
                            PartNo = MPs_Template[i].PartNo
                        });
                    }
                }
                else
                {
                    // Show the details.
                    for (int i = 0; i < MPs_Detail.Count; i++)
                    {
                        result.Add(new MeasurementPointDetails
                        {
                            Id = MPs_Detail[i].Id,
                            Name = MPs_Detail[i].Name,
                            Make = MPs_Detail[i].Make,
                            ObservationListId = MPs_Detail[i].ObservationListId,
                            InitialLength = MPs_Detail[i].InitialLength,
                            WornLength = MPs_Detail[i].WornLength,
                            PartNo = MPs_Detail[i].PartNo
                        });
                    }
                }
            }

            return result;
        }

        public List<GenericIdNameVM> GetObservationLists(long customerId)
        {
            List<GenericIdNameVM> result = new List<GenericIdNameVM>();

            using (var _context = new DAL.GETContext())
            {
                result = (from gol in _context.GET_OBSERVATION_LIST
                           where gol.active == true && gol.customer_auto == customerId
                           select new GenericIdNameVM
                           {
                               Id = gol.observation_list_auto,
                               Name = gol.name
                           }).ToList();
            }

            return result;
        }

        public List<ObservationPointDetails> GetObservationPointDetails(int implement_auto, int get_auto)
        {
            List<ObservationPointDetails> result = new List<ObservationPointDetails>();

            using (var _context = new DAL.GETContext())
            {
                result = (from gop in _context.GET_OBSERVATION_POINTS
                          join gol in _context.GET_OBSERVATION_LIST
                            on gop.observation_list equals gol.observation_list_auto into gol_join
                          from gol2 in gol_join.DefaultIfEmpty()
                          where gop.active == true
                            && gop.get_auto == get_auto
                          select new ObservationPointDetails
                          {
                              Id = gop.observation_point_auto,
                              Name = gop.observation_name,
                              ObservationListId = gop.observation_list != null ? gop.observation_list.Value : 0,
                              InitialLength = gop.initial_length.Value,
                              WornLength = gop.worn_length.Value
                          }).ToList();
            }

            return result;
        }

        public int CreateNewMeasurementPoints(int get_auto, GETContext _context)
        {
            int result = 0;
            int _changesSaved = 0;

            // Check if there are any records already for measurement points.
            var existingRecords = _context.GET_COMPONENT
                .Where(w => w.get_auto == get_auto && w.active == true)
                .ToList();

            // GET record
            var GT = _context.GET.Find(get_auto);
            long implement_auto = GT.implement_auto.Value;

            List<int> comparttypes = (from ic in _context.GET_IMPLEMENT_COMPARTTYPE
                                join g in _context.GET
                                   on ic.implement_auto equals g.implement_auto
                                where g.get_auto == get_auto
                                select ic.comparttype_auto
                                ).ToList();

            List<string> schematics = _context.LU_IMPLEMENT.Find(implement_auto)
                .schematic_auto_multiple
                .Split('_')
                .ToList();

            var measurementPoints = (from gsc in _context.GET_SCHEMATIC_COMPONENT
                                  join lc in _context.LU_COMPART_TYPE
                                    on gsc.comparttype_auto equals lc.comparttype_auto
                                  where comparttypes.Contains(gsc.comparttype_auto)
                                    && schematics.Contains(gsc.schematic_auto.ToString())
                                    && gsc.active == true
                                  select new
                                  {
                                      Id = lc.comparttype,
                                      SchematicComponent = gsc.schematic_component_auto
                                  }).ToList();

            // There are no records, i.e. this is setting up components for a new implement.
            if (existingRecords.Count == 0)
            {
                for (int i = 0; i < measurementPoints.Count; i++)
                {
                    GET_COMPONENT gc = new GET_COMPONENT();
                    gc.get_auto = get_auto;
                    gc.cmu = 0;
                    gc.install_date = GT.created_date;
                    gc.ltd_at_setup = (int)GT.impsetup_hours.Value;
                    gc.req_measure = false;
                    gc.initial_length = 0;
                    gc.worn_length = 0;
                    gc.price = 0;
                    gc.part_no = "";
                    gc.active = true;
                    gc.schematic_component_auto = measurementPoints[i].SchematicComponent;
                    _context.GET_COMPONENT.Add(gc);
                }

                // Save changes
                try
                {
                    _changesSaved = _context.SaveChanges();
                    if (_changesSaved > 0)
                    {
                        result = 1;
                    }
                }
                catch (Exception ex1)
                {
                    result = -1;
                }
            }

            return result;
        }

        /// <summary>
        /// Save the measurement points for an implement.
        /// </summary>
        /// <param name="get_auto"></param>
        /// <param name="measurementPoints"></param>
        /// <returns>Result = 1 for successful save, Result = 0 for no save, Result = -1 for error.</returns>
        public int SaveMeasurementPoints(int get_auto, List<MeasurementPointDetails> measurementPoints)
        {
            int result = 0;
            int _changesSaved = 0;

            using (var _context = new DAL.GETContext())
            {
                // Check if there are any records already for measurement points.
                var existingRecords = _context.GET_COMPONENT
                    .Where(w => w.get_auto == get_auto && w.active == true)
                    .ToList();

                // GET record
                var GT = _context.GET.Find(get_auto);

                if(existingRecords.Count != 0)
                {
                    for(int i=0; i<measurementPoints.Count; i++)
                    {
                        var GETComponentRecord = existingRecords
                            .Where(w => w.get_component_auto == measurementPoints[i].Id)
                            .FirstOrDefault();

                        // Update the existing record.
                        if (GETComponentRecord != null)
                        {
                            GETComponentRecord.make_auto = measurementPoints[i].Make != 0 ? measurementPoints[i].Make : (int?) null;
                            GETComponentRecord.observation_list_auto = measurementPoints[i].ObservationListId;
                            GETComponentRecord.req_measure = ((measurementPoints[i].InitialLength) == 0
                                && (measurementPoints[i].WornLength == 0)) ? false : true;
                            GETComponentRecord.initial_length = measurementPoints[i].InitialLength;
                            GETComponentRecord.worn_length = measurementPoints[i].WornLength;
                            GETComponentRecord.part_no = measurementPoints[i].PartNo;
                        }
                    }
                }

                // Save changes
                try
                {
                    _changesSaved = _context.SaveChanges();
                    if (_changesSaved > 0)
                    {
                        result = 1;
                    }
                }
                catch(Exception ex1)
                {
                    result = -1;
                }
            }

            return result;
        }

        /// <summary>
        /// Save the observation points for an implement.
        /// </summary>
        /// <param name="get_auto"></param>
        /// <param name="observationPoints"></param>
        /// <returns>Result = 1 for successful save, Result = 0 for no save, Result = -1 for error.</returns>
        public int SaveObservationPoints(int get_auto, List<ObservationPointDetails> observationPoints)
        {
            int result = 0;
            int _changesSaved = 0;

            using (var _context = new DAL.GETContext())
            {
                // Check if there are any existing records.
                var existingRecords = _context.GET_OBSERVATION_POINTS
                    .Where(w => w.get_auto == get_auto && w.active == true)
                    .ToList();

                if(existingRecords.Count != 0)
                {
                    for(int i=0; i<observationPoints.Count; i++)
                    {
                        var gop = _context.GET_OBSERVATION_POINTS.Find(observationPoints[i].Id);

                        gop.observation_list = (observationPoints[i].ObservationListId != 0) ? 
                            observationPoints[i].ObservationListId : (int?) null;
                        gop.req_measure = ((observationPoints[i].InitialLength) == 0
                                && (observationPoints[i].WornLength == 0)) ? false : true;
                        gop.initial_length = observationPoints[i].InitialLength;
                        gop.worn_length = observationPoints[i].WornLength;
                    }
                }

                // Save changes
                try
                {
                    _changesSaved = _context.SaveChanges();
                    if (_changesSaved > 0)
                    {
                        result = 1;
                    }
                }
                catch (Exception ex1)
                {
                    result = -1;
                }
            }

            return result;
        }
    }
}
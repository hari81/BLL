using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.GETCore.Classes.ViewModel;
using BLL.GETInterfaces;
using System.Threading.Tasks;
using System.Data.Entity;

namespace BLL.GETCore.Classes
{
    public class GETImplement : IImplementManager
    {
        private GETContext _context;
        private EventManagement eventManagement;

        public GETImplement()
        {
            _context = new GETContext();
            eventManagement = new EventManagement();
        }

        /// <summary>
        /// Returns a list of implement templates which a given customer is allowed to use for their implements. 
        /// </summary>
        /// <param name="customerId">The customer ID which you want to get the list of templates for. </param>
        /// <returns></returns>
        public async Task<Tuple<bool, List<TemplateViewModel>>> GetTemplateListForEquipment(long equipmentId)
        {
            var equipment = _context.EQUIPMENTs.Find(equipmentId);
            long customerId = _context.CRSF.Where(j => j.crsf_auto == equipment.crsf_auto).Select(c => c.customer_auto).FirstOrDefault();
            int modelId = _context.EQUIPMENTs.Where(e => e.equipmentid_auto == equipmentId).Select(m => m.LU_MMTA.model_auto).FirstOrDefault();

            if (equipment == null || customerId == 0 || modelId == 0)
                return Tuple.Create<bool, List<TemplateViewModel>>(false, null);

            List<TemplateViewModel> returnList = new List<TemplateViewModel>();
            


            var result = await _context.LU_IMPLEMENT.Where(i => (i.CustomerId == customerId || i.CustomerId == null) &&
                                                    (i.implement_category_auto == 1 || i.implement_category_auto == 2)).Distinct().ToListAsync();

            List<long> allowedImplementsForModel = await _context.GET_IMPLEMENT_MAKE_MODEL.Where(m => m.model_auto == modelId).Select(m => m.implement_auto).Distinct().ToListAsync();

            foreach(var implement in result)
            {
                if(allowedImplementsForModel.Contains(implement.implement_auto))
                {
                    returnList.Add(new TemplateViewModel()
                    {
                        TemplateId = (int)implement.implement_auto,
                        TemplateName = implement.implementdescription
                    });
                }
            }

            return Tuple.Create(true,returnList);

        }

        /// <summary>
        /// Returns a list of implement templates which a given user is allowed to use for their implements.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, List<TemplateViewModel>>> GetTemplateListForUser(long userId)
        {
            if(userId <= 0)
            {
                return Tuple.Create<bool, List<TemplateViewModel>>(false, null);
            }

            List<TemplateViewModel> returnList = new List<TemplateViewModel>();

            var result = await _context.LU_IMPLEMENT.Where(i => (i.CustomerId == null) &&
                                                    (i.implement_category_auto == 1 || i.implement_category_auto == 2)).Distinct().ToListAsync();
            foreach(var implement in result)
            {
                returnList.Add(new TemplateViewModel()
                {
                    TemplateId = (int)implement.implement_auto,
                    TemplateName = implement.implementdescription
                });
            }

            return Tuple.Create(true, returnList);
        }

        public int implementSetupEvent(string get_auto, string implement_ltd, string user_auto, string setup_date)
        {
            int result = -1;

            int iUserAuto = int.Parse(user_auto);
            int iLTD = int.Parse(implement_ltd);
            int iGetAuto = int.Parse(get_auto);

            var ActionParam = new BLL.Core.Domain.EquipmentActionRecord
            {
                TypeOfAction = BLL.Core.Domain.ActionType.GETAction
            };

            DateTime setupDate;
            try
            {
                var dateParts = setup_date.Split('/');
                int iYear = int.Parse(dateParts[2]);
                int iMonth = int.Parse(dateParts[1]);
                int iDay = int.Parse(dateParts[0]);
                setupDate = new DateTime(iYear, iMonth, iDay);
            }
            catch(Exception ex1)
            {
                setupDate = DateTime.Now;
            }

            var GETs = new DAL.GETContext().GET.Find(iGetAuto);
            if(GETs != null)
            {
                //setupDate = GETs.created_date != null ? GETs.created_date.Value : setupDate;
            }

            var ImplementSetupParams = new BLL.Core.Domain.GETImplementSetupParams
            {
                UserAuto = iUserAuto,
                ActionType = BLL.Core.Domain.GETActionType.Implement_Setup,
                RecordedDate = DateTime.Now,
                EventDate = setupDate,
                Comment = "",
                Cost = 0.0M,
                ImplementLTD = iLTD,
                GetAuto = iGetAuto
            };

            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = " ",
                LastMessage = " ",
                Id = 0
            };

            using (var ImplementSetupAction = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), new DAL.GETContext(), ActionParam, ImplementSetupParams))
            {
                ImplementSetupAction.Operation.Start();

                if (ImplementSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Close)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = ImplementSetupAction.Operation.ActionLog;
                    rm.LastMessage = ImplementSetupAction.Operation.Message;
                }

                if (ImplementSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Started)
                {
                    result = int.TryParse(ImplementSetupAction.Operation.Message, out result) ? result : -1;
                    ImplementSetupAction.Operation.Validate();
                }

                if (ImplementSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Invalid)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = ImplementSetupAction.Operation.ActionLog;
                    rm.LastMessage = ImplementSetupAction.Operation.Message;
                }

                if (ImplementSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Valid)
                {
                    ImplementSetupAction.Operation.Commit();
                }

                if (ImplementSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Failed)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = ImplementSetupAction.Operation.ActionLog;
                    rm.LastMessage = ImplementSetupAction.Operation.Message;
                }

                if (ImplementSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Succeed)
                {
                    rm.OperationSucceed = true;
                    rm.ActionLog = ImplementSetupAction.Operation.ActionLog;
                    rm.LastMessage = ImplementSetupAction.Operation.Message;
                }

                rm.Id = ImplementSetupAction.Operation.UniqueId;
            }

            return result;
        }

        public List<GETImplementCategoriesVM> returnImplementCategories()
        {
            List<GETImplementCategoriesVM> result = new List<GETImplementCategoriesVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var implementCategories = (from ic in dataEntities.IMPLEMENT_CATEGORY
                                           select new
                                           {
                                               ic.implement_category_auto,
                                               ic.category_shortname,
                                               ic.category_name,
                                               ic.category_desc
                                           });

                result = implementCategories.Select(
                    ic => new GETImplementCategoriesVM
                    {
                        implement_category_auto = ic.implement_category_auto,
                        category_shortname = ic.category_shortname,
                        category_name = ic.category_name,
                        category_desc = ic.category_desc
                    }).ToList();
            }

            return result;
        }

        public int returnImplementCategory(int implement_auto)
        {
            int result = 0;

            using (var dataEntities = new DAL.GETContext())
            {
                var implementCategory = (from lc in dataEntities.LU_IMPLEMENT
                                         where lc.implement_auto == implement_auto
                                         select lc.implement_category_auto).FirstOrDefault();

                result = implementCategory;
            }

            return result;
        }

        public bool updateImplementCategory(int implement_auto, int category_selected)
        {
            bool result = false;

            using (var dataEntities = new DAL.GETContext())
            {
                var implementToUpdate = dataEntities.LU_IMPLEMENT.Find(implement_auto);

                if(implementToUpdate != null)
                {
                    implementToUpdate.implement_category_auto = category_selected;
                    dataEntities.SaveChanges();

                    result = true;
                }
            }

            return result;
        }

        public string[] availableSchematics(int implement_auto)
        {
            string[] result;

            using (var dataEntities = new DAL.GETContext())
            {
                var schematics = (from li in dataEntities.LU_IMPLEMENT
                                  where li.implement_auto == implement_auto
                                  select li.schematic_auto_multiple).FirstOrDefault();


                if(schematics == null)
                {
                    result = null;
                }
                else
                {
                    var strSchematics = schematics.Split('_');
                    result = strSchematics;
                }

            }

            return result;
        }

        public string[] availableSchematicsByImplementSerial(string implement_serial)
        {
            string[] result;

            using (var dataEntities = new DAL.GETContext())
            {
                var schematics = (from li in dataEntities.LU_IMPLEMENT
                                  join g in dataEntities.GET
                                  on li.implement_auto equals g.implement_auto
                                  where implement_serial.Contains(g.impserial) 
                                  select li.schematic_auto_multiple).FirstOrDefault();

                var strSchematics = schematics.Split('_');
                result = strSchematics;
            }

            return result;
        }

        public string[] availableSchematicsByImplementId(string implementId)
        {
            string[] result;

            using (var dataEntities = new DAL.GETContext())
            {
                var schematics = (from li in dataEntities.LU_IMPLEMENT
                                  join g in dataEntities.GET
                                  on li.implement_auto equals g.implement_auto
                                  where implementId == g.get_auto.ToString()
                                  select li.schematic_auto_multiple).FirstOrDefault();

                if(schematics != null)
                {
                    var strSchematics = schematics.Split('_');
                    result = strSchematics;
                }
                else
                {
                    result = null;
                }
            }

            return result;
        }

        public async Task<List<SchematicDataVM>> GetSchematicDataForImplement(string implementId)
        {
            List<SchematicDataVM> result = new List<SchematicDataVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var schematics = (from li in dataEntities.LU_IMPLEMENT
                                  join g in dataEntities.GET
                                  on li.implement_auto equals g.implement_auto
                                  where implementId == g.get_auto.ToString()
                                  select li.schematic_auto_multiple).FirstOrDefault();

                if (schematics != null)
                {
                    var strSchematics = schematics.Split('_');
                    
                    for(int i=0; i<strSchematics.Length; i++)
                    {
                        int iSchematicAuto = int.TryParse(strSchematics[i], out iSchematicAuto) ? iSchematicAuto : 0;
                        if(iSchematicAuto <= 0)
                        {
                            continue;
                        }

                        var schematicData = dataEntities.GET_SCHEMATIC_IMAGE.Find(iSchematicAuto);
                        result.Add(new SchematicDataVM
                        {
                            Id = schematicData.schematic_auto,
                            Data = Convert.ToBase64String(schematicData.attachment),
                            GETComponents = await GetComponentsForSchematic(schematicData.schematic_auto.ToString(),
                                implementId),
                            ObservationPoints = await GetObservationPointsForSchematic(schematicData.schematic_auto.ToString(),
                                implementId)
                        });
                    }
                }
            }

            return result;
        }

        public async Task<List<ComponentPointVM>> GetObservationPointsForSchematic(string schematic_auto, string get_auto)
        {
            List<ComponentPointVM> result = new List<ComponentPointVM>();

            using (var _context = new DAL.GETContext())
            {
                var OPs = await _context.GET_OBSERVATION_POINTS
                    .Where(w => w.get_auto.ToString() == get_auto
                        && w.schematic_auto.ToString() == schematic_auto)
                    .Select(s => new ComponentPointVM
                    {
                        Id = s.observation_point_auto,
                        Name = s.observation_name,
                        PositionX = s.positionX.Value,
                        PositionY = s.positionY.Value,
                        WornPct = 0,
                        InspectionDate = "Never"
                    }).ToListAsync();

                for(int i=0; i<OPs.Count; i++)
                {
                    var OPAuto = OPs[i].Id;
                    var OP = _context.GET_OBSERVATION_POINTS.Find(OPAuto);

                    bool requiresMeasurement = (OP.req_measure == true);
                    decimal initialLength = OP.initial_length.Value;
                    decimal wornLength = OP.worn_length.Value;

                    var lastInspection = await _context.GET_OBSERVATION_POINT_INSPECTION
                                            .Where(w => w.observation_point_auto == OPAuto)
                                            .OrderByDescending(o => o.inspection_auto)
                                            .FirstOrDefaultAsync();

                    if(lastInspection != null)
                    {
                        // Prevent divide by zero error
                        if((wornLength - initialLength) > 0)
                        {
                            OPs[i].WornPct = (int)((lastInspection.measurement - initialLength) / (wornLength - initialLength)) * 100;
                        }
                        else
                        {
                            OPs[i].WornPct = 0;
                        }
                        
                        OPs[i].InspectionDate = lastInspection.inspection_date.ToShortDateString();
                    }
                }

                result = OPs;
            }

            return result;
        }

        public async Task<List<ComponentPointVM>> GetComponentsForSchematic(string schematic_auto, string implementId)
        {
            List<ComponentPointVM> result = new List<ComponentPointVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var componentList = await (from gsc in dataEntities.GET_SCHEMATIC_COMPONENT
                                     join gc in dataEntities.GET_COMPONENT
                                        on gsc.schematic_component_auto equals gc.schematic_component_auto
                                     join lc in dataEntities.LU_COMPART_TYPE
                                        on gsc.comparttype_auto equals lc.comparttype_auto
                                     where gsc.active == true
                                        && gc.active == true
                                        && gsc.schematic_auto.ToString() == schematic_auto
                                        && gc.get_auto.ToString() == implementId
                                     select new ComponentPointVM
                                     {
                                         Id = gc.get_component_auto,
                                         Name = lc.comparttype,
                                         PositionX = gsc.positionX,
                                         PositionY = gsc.positionY,
                                         WornPct = 0,
                                         InspectionDate = "Never"
                                     }).ToListAsync();

                for(int i=0; i<componentList.Count; i++)
                {
                    var GETComponentAuto = componentList[i].Id;
                    var GETComponent = dataEntities.GET_COMPONENT.Find(GETComponentAuto);

                    bool requiresMeasurment = (GETComponent.req_measure == true);
                    decimal initialLength = GETComponent.initial_length.Value;
                    decimal wornLength = GETComponent.worn_length.Value;

                    var lastInspection = await dataEntities.GET_COMPONENT_INSPECTION
                                            .Where(ci => ci.get_component_auto == GETComponentAuto)
                                            .OrderByDescending(o => o.inspection_auto)
                                            .FirstOrDefaultAsync();

                    if(lastInspection != null)
                    {
                        // Prevent divide by zero error
                        if((initialLength - wornLength) > 0)
                        {
                            componentList[i].WornPct = (int)(((initialLength - lastInspection.measurement) / (initialLength - wornLength)) * 100);
                        }
                        else
                        {
                            componentList[i].WornPct = 0;
                        }
                        componentList[i].InspectionDate = lastInspection.inspection_date.ToShortDateString();
                    }
                }

                result = componentList;
            }

            return result;
        }

        public string findImplementByGET(string getAuto)
        {
            string result;
            int iGetAuto = int.TryParse(getAuto, out iGetAuto) ? iGetAuto : 0;

            using (var dataEntities = new DAL.GETContext())
            {
                var implement = (from li in dataEntities.LU_IMPLEMENT
                                 join g in dataEntities.GET
                                 on li.implement_auto equals g.implement_auto
                                 where g.get_auto == iGetAuto
                                 select g.implement_auto).FirstOrDefault();

                result = implement.Value.ToString();
            }

            return result;
        }

        public List<GETImplementTypeVM> returnImplementTypes()
        {
            List<GETImplementTypeVM> result = new List<GETImplementTypeVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                result = dataEntities.LU_IMPLEMENT.Select(
                    l => new GETImplementTypeVM
                    {
                        implement_auto = l.implement_auto,
                        implement_description = l.implementdescription
                    }).ToList();
            }

            return result;
        }

        public List<MakeVM> returnMake()
        {
            List<MakeVM> result = new List<MakeVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                result = dataEntities.MAKE.Select(m => new MakeVM { make_auto = m.make_auto, make_desc = m.makedesc }).ToList();
            }

            return result;
        }

        public List<GETComponentPositionsVM> loadAssignedComponents(int schematicAuto, int inspectAuto)
        {
            List<GETComponentPositionsVM> result = new List<GETComponentPositionsVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var schematicComponents = (from gsc in dataEntities.GET_SCHEMATIC_COMPONENT
                                           join gc in dataEntities.GET_COMPONENT
                                           on gsc.schematic_component_auto equals gc.schematic_component_auto
                                           join gci in dataEntities.GET_COMPONENT_INSPECTION
                                           on gc.get_component_auto equals gci.get_component_auto
                                           where gsc.schematic_auto == schematicAuto
                                           && gsc.active == true
                                           && gci.implement_inspection_auto == inspectAuto
                                           orderby gsc.comparttype_auto
                                           select new
                                           {
                                               gci.inspection_auto,
                                               gsc.comparttype_auto,
                                               gsc.positionX,
                                               gsc.positionY
                                           });

                result = schematicComponents.Select(
                    sc => new GETComponentPositionsVM
                    {
                        inspection_auto = sc.inspection_auto,
                        comparttype_auto = sc.comparttype_auto,
                        positionX = sc.positionX,
                        positionY = sc.positionY
                    }).ToList();
            }

            return result;
        }

        public int storeObservationPoint(string name, string make, string observation_list, string requires_measurement,
            string initial_length, string worn_length, string part_number, string price, string getAuto)
        {
            int result = -1;
            int errors = 0;

            bool bReqMeasure = bool.TryParse(requires_measurement, out bReqMeasure) ? bReqMeasure : false;
            decimal dPrice, dInitialLength, dWornLength;
            int iMake, iObservationList, iGetAuto;

            using (var dataEntities = new DAL.GETContext())
            {
                var ObservationPoint = new DAL.GET_OBSERVATION_POINTS();

                // Observation Name
                ObservationPoint.observation_name = name;

                // Observation Make
                if(int.TryParse(make, out iMake))
                {
                    if(iMake > 0)
                    {
                        ObservationPoint.make = iMake;
                    }
                }

                // Observation List
                if(int.TryParse(observation_list, out iObservationList))
                {
                    if(iObservationList > 0)
                    {
                        ObservationPoint.observation_list = iObservationList;
                    }
                }
                
                // Requires measurement
                ObservationPoint.req_measure = bReqMeasure;

                // Ensure both initial and worn lengths are supplied if measurement is required.
                if (bReqMeasure)
                {
                    // Initial length
                    if (decimal.TryParse(initial_length, out dInitialLength))
                    {
                        ObservationPoint.initial_length = dInitialLength;
                    }
                    else
                    {
                        errors++;
                    }

                    // Worn length
                    if (decimal.TryParse(worn_length, out dWornLength))
                    {
                        ObservationPoint.worn_length = dWornLength;
                    }
                    else
                    {
                        errors++;
                    }
                }

                // Part Number
                ObservationPoint.part_number = part_number;

                // Price
                if(decimal.TryParse(price, out dPrice))
                {
                    ObservationPoint.price = dPrice;
                }
                
                // GET Auto
                if(int.TryParse(getAuto, out iGetAuto))
                {
                    ObservationPoint.get_auto = iGetAuto;
                }
                else
                {
                    errors++;
                }
                
                // Only save if there are no errors.
                if(errors == 0)
                {
                    dataEntities.GET_OBSERVATION_POINTS.Add(ObservationPoint);
                    int _changesSaved = dataEntities.SaveChanges();

                    if (_changesSaved > 0)
                    {
                        result = ObservationPoint.observation_point_auto;
                    }
                }
            }

            return result;
        }

        public bool updateObservationPoint(string op_auto, string make, string observation_list, string requires_measurement,
            string initial_length, string worn_length, string part_number, string price)
        {
            bool result = false;
            int errors = 0;

            bool bReqMeasure = bool.TryParse(requires_measurement, out bReqMeasure) ? bReqMeasure : false;
            decimal dPrice, dInitialLength, dWornLength;
            int iObservationPointAuto, iMake, iObservationList;

            using (var dataEntities = new DAL.GETContext())
            {
                // Observation Point Auto
                if(int.TryParse(op_auto, out iObservationPointAuto))
                {
                    var ObservationPoint = dataEntities.GET_OBSERVATION_POINTS.Find(iObservationPointAuto);

                    // Observation Make
                    if (int.TryParse(make, out iMake))
                    {
                        if (iMake > 0)
                        {
                            ObservationPoint.make = iMake;
                        }
                    }

                    // Observation List
                    if (int.TryParse(observation_list, out iObservationList))
                    {
                        if (iObservationList > 0)
                        {
                            ObservationPoint.observation_list = iObservationList;
                        }
                    }

                    // Requires measurement
                    ObservationPoint.req_measure = bReqMeasure;

                    // Ensure both initial and worn lengths are supplied if measurement is required.
                    if (bReqMeasure)
                    {
                        // Initial length
                        if (decimal.TryParse(initial_length, out dInitialLength))
                        {
                            ObservationPoint.initial_length = dInitialLength;
                        }
                        else
                        {
                            errors++;
                        }

                        // Worn length
                        if (decimal.TryParse(worn_length, out dWornLength))
                        {
                            ObservationPoint.worn_length = dWornLength;
                        }
                        else
                        {
                            errors++;
                        }
                    }

                    // Part Number
                    ObservationPoint.part_number = part_number;

                    // Price
                    if (decimal.TryParse(price, out dPrice))
                    {
                        ObservationPoint.price = dPrice;
                    }

                    // Only save if there are no errors.
                    if (errors == 0)
                    {
                        int _changesSaved = dataEntities.SaveChanges();

                        if (_changesSaved > 0)
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }


        public List<GETObservationPoint> loadObservationPoints(string get_auto)
        {
            List<GETObservationPoint> result = new List<GETObservationPoint>();

            int iGetAuto = int.TryParse(get_auto, out iGetAuto) ? iGetAuto : 0;

            using (var dataEntities = new DAL.GETContext())
            {
                var observationPoints = (from op in dataEntities.GET_OBSERVATION_POINTS
                                         where op.get_auto == iGetAuto && op.active == true
                                         select new
                                         {
                                             op.observation_point_auto,
                                             op.observation_name,
                                             op.schematic_auto,
                                             op.positionX,
                                             op.positionY
                                         });

                result = observationPoints.Select(
                    op => new GETObservationPoint
                    {
                        observation_point_auto = op.observation_point_auto,
                        name = op.observation_name,
                        schematic_auto = (op.schematic_auto != null ? op.schematic_auto.Value.ToString() : "0"),
                        positionX = (op.positionX).Value.ToString(),
                        positionY = (op.positionY).Value.ToString()
                    }).ToList();
            }

            return result;
        }

        public List<GETObservationPointDetail> loadObservationPointsDetail(string get_auto)
        {
            List<GETObservationPointDetail> result = new List<GETObservationPointDetail>();
            int iGetAuto = int.TryParse(get_auto, out iGetAuto) ? iGetAuto : 0;

            using (var dataEntities = new DAL.GETContext())
            {
                var observationPoints = dataEntities.GET_OBSERVATION_POINTS
                    .Where(o => o.get_auto == iGetAuto && o.schematic_auto != null && o.active == true);

                result = observationPoints.Select(
                    op => new GETObservationPointDetail
                    {
                        observation_point_auto = op.observation_point_auto,
                        name = op.observation_name,
                        make = op.make.Value.ToString(),
                        observation_list = op.observation_list.Value.ToString(),
                        requires_measurement = op.req_measure,
                        initial_length = op.initial_length.Value.ToString(),
                        worn_length = op.worn_length.Value.ToString(),
                        part_number = op.part_number.ToString(),
                        price = op.price.Value.ToString(),
                        schematic_auto = op.schematic_auto.Value.ToString(),
                        positionX = op.positionX.Value.ToString(),
                        positionY = op.positionY.Value.ToString()
                    }).ToList();
            }

            return result;
        }

        public bool updateSchematicObservationPoints(string get_auto, string schematic_auto, string[] observation_points, string[][] positions)
        {
            bool result = false;
            int errors = 0;

            using (var dataEntities = new DAL.GETContext())
            {
                for(int i=0; i<positions.Length; i++)
                {
                    int iSchematicAuto = int.TryParse(schematic_auto, out iSchematicAuto) ? iSchematicAuto : 0;
                    int iObservationPoint = int.TryParse(observation_points[i], out iObservationPoint) ? iObservationPoint : 0;
                    int iPositionX = int.TryParse(positions[i][0], out iPositionX) ? iPositionX : -1;
                    int iPositionY = int.TryParse(positions[i][1], out iPositionY) ? iPositionY : -1;

                    var inspectionPoint = dataEntities.GET_OBSERVATION_POINTS.Find(iObservationPoint);

                    if ((iSchematicAuto != 0) && (iPositionX >= 0) && (iPositionY >= 0))
                    {
                        inspectionPoint.schematic_auto = iSchematicAuto;
                        inspectionPoint.positionX = iPositionX;
                        inspectionPoint.positionY = iPositionY;

                        try
                        {
                            dataEntities.SaveChanges();
                            result = true;
                        }
                        catch (Exception ex1)
                        {
                            errors++;
                        }
                    }
                    else
                    {
                        errors++;
                    }
                }
            }

            if(errors > 0)
            {
                result = false;
            }

            return result;
        }

        public bool deleteObservationPoint(string get_auto, string observation_point_auto)
        {
            bool result = false;
            int iObservationPoint = int.TryParse(observation_point_auto, out iObservationPoint) ? iObservationPoint : 0;

            using (var dataEntities = new DAL.GETContext())
            {
                var inspection_point = dataEntities.GET_OBSERVATION_POINTS.Find(iObservationPoint);
                inspection_point.schematic_auto = null;
                inspection_point.positionX = null;
                inspection_point.positionY = null;

                int _changesSaved = dataEntities.SaveChanges();
                if (_changesSaved > 0)
                {
                    result = true;
                }
            }

            return result;
        }

        public List<GETObservationPointPositionsVM> loadObservationPointsBySchematic(int schematic_auto, int inspect_auto)
        {
            List<GETObservationPointPositionsVM> result = new List<GETObservationPointPositionsVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var opPositions = (from r in dataEntities.GET_OBSERVATION_POINT_INSPECTION
                                   join o in dataEntities.GET_OBSERVATION_POINTS
                                   on r.observation_point_auto equals o.observation_point_auto
                                   where r.inspection_auto == inspect_auto
                                   && o.schematic_auto == schematic_auto
                                   select new
                                   {
                                       r.observation_point_inspection_auto,
                                       o.observation_point_auto,
                                       o.positionX,
                                       o.positionY
                                   });

                result = opPositions.Select(
                    op => new GETObservationPointPositionsVM
                    {
                        inspection_auto = op.observation_point_inspection_auto,
                        observations_point_auto = op.observation_point_auto,
                        positionX = (op.positionX).Value,
                        positionY = (op.positionY).Value
                    }).ToList();
            }

            return result;
        }

        public int deleteSchematic(int schematic_auto, int implement_auto)
        {
            int result = -1;
            bool foundSchematic = false;
            string originalSchematicList = "", updatedSchematicList = "";

            using (var dataEntities = new DAL.GETContext())
            {
                // Check that no observation points have been configured.
                var observationPoints = dataEntities.GET_OBSERVATION_POINTS.Where(o => o.schematic_auto == schematic_auto).ToList();
                if(observationPoints.Count() > 0)
                {
                    result = 0;
                }

                else
                {
                    // Check for the implement.
                    var implement = dataEntities.LU_IMPLEMENT.Where(i => i.implement_auto == implement_auto).FirstOrDefault();
                    var schematicsForImplement = implement.schematic_auto_multiple.Split('_');
                    originalSchematicList = implement.schematic_auto_multiple;

                    for (int m = 0; m < schematicsForImplement.Count(); m++)
                    {
                        int iSchematic = int.TryParse(schematicsForImplement[m], out iSchematic) ? iSchematic : 0;

                        if (iSchematic == schematic_auto)
                        {
                            foundSchematic = true;
                            break;
                        }
                    }

                    if(foundSchematic)
                    {
                        // Check that no GETs have been assigned this schematic.
                        var GETs = dataEntities.GET.Where(g => g.implement_auto == implement_auto).ToList();
                        var SchematicComponents = dataEntities.GET_SCHEMATIC_COMPONENT.Where(sc => sc.schematic_auto == schematic_auto).ToList();
                        if ((GETs.Count() > 0) && (SchematicComponents.Any()))
                        {
                            result = 0;
                        }

                        // Perform the delete.
                        else
                        {
                            // First, generate the updated schematic list for the implement.
                            for (int k = 0; k < schematicsForImplement.Count(); k++)
                            {
                                int schematicID;

                                // Only process if the schematic ID is an integer.
                                if(int.TryParse(schematicsForImplement[k], out schematicID))
                                {
                                    if (schematicsForImplement[k] != schematic_auto.ToString())
                                    {
                                        updatedSchematicList += schematicsForImplement[k] + "_";
                                    }
                                }
                                
                            }
                            if(updatedSchematicList.Length > 1)
                            {
                                updatedSchematicList = updatedSchematicList.Substring(0, updatedSchematicList.Length - 1);
                            }
                            implement.schematic_auto_multiple = updatedSchematicList;

                            // Try to update.
                            int _changesSaved = dataEntities.SaveChanges();
                            if (_changesSaved > 0)
                            {
                                var schematicImage = dataEntities.GET_SCHEMATIC_IMAGE.Where(s => s.schematic_auto == schematic_auto).FirstOrDefault();
                                if(schematicImage != null)
                                {
                                    dataEntities.GET_SCHEMATIC_IMAGE.Remove(schematicImage);

                                    // Now try to remove the schematic image.
                                    _changesSaved = dataEntities.SaveChanges();
                                    if (_changesSaved > 0)
                                    {
                                        result = 1;
                                    }
                                    else
                                    {
                                        // Roll back changes due to error.
                                        implement.schematic_auto_multiple = originalSchematicList;
                                        dataEntities.SaveChanges();

                                        result = -1;
                                    }
                                }
                            }
                            // Update failed, so return error.
                            else
                            {
                                result = -1;
                            }
                        }
                    }
                }
            }

            return result;
        }

        // Return details for an implement given the get_auto
        public GETImplementDetailsVM ReturnGETById(int Id)
        {
            GETImplementDetailsVM result = new GETImplementDetailsVM();

            // Default condition of implement when no inspection data is available.
            int INITIAL_IMPLEMENT_CONDITION = 0;    // 0 % worn.

            using (var dataEntities = new DAL.GETContext())
            {
                var get = dataEntities.GET.Find(Id);

                if(get != null)
                {
                    // Make
                    var make_auto = get.make_auto;
                    if(make_auto != null)
                    {
                        result.make = dataEntities.MAKE.Find(make_auto).makedesc;
                    }
                    else
                    {
                        result.make = "N/A";
                    }

                    // Implement type
                    var implement_type = dataEntities.LU_IMPLEMENT.Find(get.implement_auto).implementdescription;
                    if(implement_type != null)
                    {
                        result.implementType = implement_type;
                    }
                    else
                    {
                        result.implementType = "N/A";
                    }
                    
                    // Serial No
                    result.serialNo = get.impserial;

                    // Last Inspection
                    var last_inspection = dataEntities.GET_IMPLEMENT_INSPECTION
                        .Where(i => i.get_auto == Id)
                        .OrderByDescending(i2 => i2.inspection_auto)
                        .FirstOrDefault();
                    if(last_inspection != null)
                    {
                        result.lastInspection = last_inspection.inspection_date.ToShortDateString();
                    }
                    else
                    {
                        result.lastInspection = "N/A";
                    }

                    // Life
                    var life_record = eventManagement.findPreviousImplementEvent(dataEntities, Id, DateTime.Now);
                    if(life_record != null)
                    {
                        result.life = life_record.ltd;
                    }
                    else
                    {
                        result.life = 0;
                    }

                    // Condition of each implement.
                    var inspectionRecord = dataEntities.GET_IMPLEMENT_INSPECTION
                        .Where(g => g.get_auto == Id)
                        .OrderByDescending(h => h.inspection_auto)
                        .FirstOrDefault();
                    if (inspectionRecord != null)
                    {
                        result.condition = inspectionRecord.eval;
                    }
                    else
                    {
                        result.condition = INITIAL_IMPLEMENT_CONDITION;
                    }

                    // Customer and job site
                    bool currentlyOnEquipment = get.on_equipment;
                    // Handle the case where an implement is installed on an equipment.
                    if (currentlyOnEquipment)
                    {
                        var get_equipment = dataEntities.EQUIPMENTs.Find(get.equipmentid_auto);
                        if(get_equipment != null)
                        {
                            var get_jobsite = dataEntities.CRSF.Find(get_equipment.crsf_auto);
                            if (get_jobsite != null)
                            {
                                result.jobsite = get_jobsite.site_name;
                                result.jobsiteId = get_jobsite.crsf_auto;

                                var get_customer = dataEntities.CUSTOMERs.Find(get_jobsite.customer_auto);
                                if(get_customer != null)
                                {
                                    result.customer = get_customer.cust_name;
                                }
                                else
                                {
                                    result.customer = "N/A";
                                }
                            }
                            else
                            {
                                result.jobsite = "N/A";
                                result.jobsiteId = 0;
                            }
                        }
                        else
                        {
                            result.customer = "N/A";
                            result.jobsite = "N/A";
                            result.jobsiteId = 0;
                        }

                        result.repairerName = "";
                        result.workshopName = "";

                        int ON_EQUIPMENT_STATUS = (int) GETInterfaces.Enum.InventoryStatus.On_Equipment;
                        result.status = dataEntities.GET_INVENTORY_STATUS.Find(ON_EQUIPMENT_STATUS).status_desc;
                    }
                    // Handle the case where an implement is in inventory.
                    else
                    {
                        var get_inventory = dataEntities.GET_INVENTORY
                            .Where(j => j.get_auto == Id)
                            .FirstOrDefault();
                        if(get_inventory != null)
                        {
                            var get_inventory_jobsite = dataEntities.CRSF.Find(get_inventory.jobsite_auto);

                            if(get_inventory_jobsite != null)
                            {
                                result.jobsite = get_inventory_jobsite.site_name;
                                result.jobsiteId = get_inventory_jobsite.crsf_auto;

                                var get_inventory_customer = dataEntities.CUSTOMERs.Find(get_inventory_jobsite.customer_auto);
                                if (get_inventory_customer != null)
                                {
                                    result.customer = get_inventory_customer.cust_name;
                                }
                                else
                                {
                                    result.customer = "N/A";
                                }
                            }
                            else
                            {
                                result.jobsite = "N/A";
                                result.jobsiteId = 0;
                            }

                            // Obtain the repairer and workshop name if available.
                            string workshopName = "";
                            string repairerName = "";
                            var workshop = get_inventory.workshop;
                            if(workshop != null)
                            {
                                workshopName = get_inventory.workshop.name;
                                var repairer = get_inventory.workshop.repairer;
                                if(repairer != null)
                                {
                                    repairerName = get_inventory.workshop.repairer.name;
                                }
                            }
                            result.repairerName = repairerName;
                            result.workshopName = workshopName;                       
                        }
                        else
                        {
                            result.customer = "N/A";
                            result.jobsite = "N/A";
                            result.jobsiteId = 0;
                            result.repairerName = "";
                            result.workshopName = "";
                        }

                        var inventory_status = dataEntities.GET_INVENTORY_STATUS.Find(get_inventory.status_auto);
                        result.status = (inventory_status != null) ? inventory_status.status_desc : "N/A";
                    }
                }

                // Model
                result.model = "N/A";
               
            }

            return result;
        }

        public GETEquipmentDetailsVM ReturnEquipmentForGET(int get_auto)
        {
            GETEquipmentDetailsVM result = new GETEquipmentDetailsVM();

            using (var dataEntitiesShared = new DAL.SharedContext())
            {
                long equipment_auto = dataEntitiesShared.GET
                    .Where(g => g.on_equipment == true && g.get_auto == get_auto)
                    .Select(s => s.equipmentid_auto.Value).FirstOrDefault();

                if(equipment_auto > 0)
                {
                    var intermediate_result = (from e in dataEntitiesShared.EQUIPMENT
                              join c in dataEntitiesShared.CRSF
                                 on e.crsf_auto equals c.crsf_auto
                              join lm in dataEntitiesShared.LU_MMTA
                                 on e.mmtaid_auto equals lm.mmtaid_auto
                              join mk in dataEntitiesShared.MAKE
                                 on lm.make_auto equals mk.make_auto
                              join md in dataEntitiesShared.MODEL
                                 on lm.model_auto equals md.model_auto
                              join t in dataEntitiesShared.TYPE
                                 on lm.type_auto equals t.type_auto
                              join ee in dataEntitiesShared.GET_EVENTS_EQUIPMENT
                                 on e.equipmentid_auto equals ee.equipment_auto
                              join ge in dataEntitiesShared.GET_EVENTS
                                 on ee.events_auto equals ge.events_auto
                              where e.equipmentid_auto == equipment_auto
                                 && ge.action_auto == (int)BLL.Core.Domain.GETActionType.Equipment_Setup
                              select new 
                              {
                                  id = e.equipmentid_auto,
                                  serialno = e.serialno,
                                  unitno = e.unitno,
                                  site_name = c.site_name,
                                  meter_reading = 0,
                                  makedesc = mk.makedesc,
                                  modeldesc = md.modeldesc,
                                  ltd = 0,
                                  typedesc = t.typedesc,
                                  setup_date = ge.event_date
                              }).FirstOrDefault();
      

                    if (intermediate_result != null)
                    {
                        result.id = intermediate_result.id;
                        result.serialno = intermediate_result.serialno;
                        result.unitno = intermediate_result.unitno;
                        result.site_name = intermediate_result.site_name;
                        result.meter_reading = intermediate_result.meter_reading;
                        result.makedesc = intermediate_result.makedesc;
                        result.modeldesc = intermediate_result.modeldesc;
                        result.ltd = intermediate_result.ltd;
                        result.typedesc = intermediate_result.typedesc;
                        result.setup_date = intermediate_result.setup_date.ToShortDateString();

                        var mostRecentEquipmentEvent = eventManagement.findPreviousEquipmentEvent(_context, equipment_auto, DateTime.Now);

                        if (mostRecentEquipmentEvent != null)
                        {
                            result.meter_reading = mostRecentEquipmentEvent.smu;
                            result.ltd = mostRecentEquipmentEvent.ltd;
                        }
                    }
                }
            }

            return result;
        }

        public string ChangeImplementStatus(int implementId, long jobsiteId, string comment, int statusId, int repairerId, int workshopId, long authUserId)
        {
            string result = "";
            DateTime date = DateTime.Now;

            using (var dataEntities = new DAL.GETContext())
            {
                // Grab the inventory record for this implement.
                var inventoryRecord = dataEntities.GET_INVENTORY.Where(i => i.get_auto == implementId).FirstOrDefault();

                int? workshop = null;
                if(workshopId != 0)
                {
                    workshop = workshopId;
                }
                else
                {
                    workshop = null;
                }

                int? repairer = null;
                if(repairerId != 0)
                {
                    repairer = repairerId;
                }
                else
                {
                    repairer = null;
                }


                if (inventoryRecord != null)
                {
                    // Update the inventory record.
                    var update_timestamp = DateTime.Now;
                    inventoryRecord.jobsite_auto = jobsiteId;
                    inventoryRecord.workshop_auto = workshop;
                    inventoryRecord.status_auto = statusId;
                    inventoryRecord.modified_date = update_timestamp;
                    inventoryRecord.modified_user = (int)authUserId;

                    var _changesSaved = dataEntities.SaveChanges();
                    if(_changesSaved > 0)
                    {
                        var actionID = (int)Core.Domain.GETActionType.Change_Implement_Status;

                        // Save a GET_EVENT record for this change.
                        GET_EVENTS newEvent = eventManagement.createGETEvent((int)authUserId,
                            actionID, date, comment, 0, update_timestamp);
                        dataEntities.GET_EVENTS.Add(newEvent);
                        _changesSaved = dataEntities.SaveChanges();

                        if (_changesSaved > 0)
                        {
                            // Save an implement event.
                            GET_EVENTS_IMPLEMENT implementEvent = eventManagement.createImplementEvent(
                                implementId, inventoryRecord.ltd, newEvent.events_auto);
                            dataEntities.GET_EVENTS_IMPLEMENT.Add(implementEvent);
                            _changesSaved = dataEntities.SaveChanges();

                            if (_changesSaved > 0)
                            {
                                // Save an inventory event.
                                GET_EVENTS_INVENTORY inventoryEvent = eventManagement.createInventoryEvent(
                                    implementEvent.implement_events_auto, jobsiteId, statusId, workshop);
                                dataEntities.GET_EVENTS_INVENTORY.Add(inventoryEvent);
                                _changesSaved = dataEntities.SaveChanges();

                                if (_changesSaved > 0)
                                {
                                    result = "Successfully updated the implement jobsite.";
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "Error: Unable to update the status for this implement!";
                    }
                }
                // If this is not in inventory then return an error.
                else
                {
                    result = "Error: The implement was not found in inventory!";
                }
            }

            return result;
        }

        public string ChangeImplementJobsite(int implementId, int customerId, long jobsiteId, decimal cost, DateTime date, string comment, long authUserId)
        {
            string result = "";

            using (var dataEntities = new DAL.GETContext())
            {
                // Grab the inventory record for this implement.
                var inventoryRecord = dataEntities.GET_INVENTORY.Where(i => i.get_auto == implementId).FirstOrDefault();

                if(inventoryRecord != null)
                {
                    // Update the inventory record.
                    var update_timestamp = DateTime.Now;
                    inventoryRecord.jobsite_auto = jobsiteId;
                    inventoryRecord.modified_date = update_timestamp;
                    inventoryRecord.modified_user = (int) authUserId;

                    var _changesSaved = dataEntities.SaveChanges();
                    if(_changesSaved > 0)
                    {
                        var actionID = (int) Core.Domain.GETActionType.Change_Implement_Jobsite;

                        // Save a GET_EVENT record for this change.
                        GET_EVENTS newEvent = eventManagement.createGETEvent((int)authUserId, actionID,
                            date, comment, cost, update_timestamp);
                        dataEntities.GET_EVENTS.Add(newEvent);
                        _changesSaved = dataEntities.SaveChanges();

                        if (_changesSaved > 0)
                        {
                            // Save an implement event.
                            GET_EVENTS_IMPLEMENT implementEvent = eventManagement.createImplementEvent(
                                implementId, inventoryRecord.ltd, newEvent.events_auto);
                            dataEntities.GET_EVENTS_IMPLEMENT.Add(implementEvent);
                            _changesSaved = dataEntities.SaveChanges();

                            if(_changesSaved > 0)
                            {
                                // Save an inventory event.
                                GET_EVENTS_INVENTORY inventoryEvent = eventManagement.createInventoryEvent(
                                    implementEvent.implement_events_auto, jobsiteId, inventoryRecord.status_auto,
                                    inventoryRecord.workshop_auto);
                                dataEntities.GET_EVENTS_INVENTORY.Add(inventoryEvent);
                                _changesSaved = dataEntities.SaveChanges();

                                if (_changesSaved > 0)
                                {
                                    result = "Successfully updated the implement jobsite.";
                                }
                            } 
                        }
                    }
                    else
                    {
                        result = "Error: Unable to update the jobsite for this implement!";
                    }
                }
                // If this is not in inventory then return an error.
                else
                {
                    result = "Error: The implement was not found in inventory!";
                }
            }

            return result;
        }

        public bool AttachImplementToEquipment(int implementId, int equipmentId, int jobsiteId, int smu, decimal cost, DateTime date, string comment, long authUserId)
        {
            BLL.Core.Domain.User actionUser = new BLL.Core.Domain.User();
            actionUser.Id = (int) authUserId;
            using (var dataEntities = new DAL.GETContext())
            {
                var user = dataEntities.USER_TABLE.Find(authUserId);
                actionUser.userName = user.username;
                actionUser.userStrId = user.userid;
            }

            var ActionParam = new BLL.Core.Domain.EquipmentActionRecord
            {
                EquipmentId = equipmentId,
                Cost = cost,
                Comment = comment,
                ReadSmuNumber = smu,
                ActionDate = date,
                ActionUser = actionUser,
                TypeOfAction = BLL.Core.Domain.ActionType.GETAction
            };

            var AttachImplementToEquipmentParams = new BLL.Core.Domain.AttachImplementToEquipmentParams
            {
                ImplementId = implementId,
                EquipmentId = equipmentId,
                JobsiteId = jobsiteId,
                UserId = authUserId,
                ActionType = BLL.Core.Domain.GETActionType.Attach_Implement_to_Equipment,
                RecordedDate = DateTime.Now,
                EventDate = date,
                Comment = comment,
                Cost = cost,
                MeterReading = smu
            };

            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = " ",
                LastMessage = " ",
                Id = 0
            };

            using (var AttachImplementToEquipmentAction = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), new DAL.GETContext(), ActionParam, AttachImplementToEquipmentParams))
            {
                AttachImplementToEquipmentAction.Operation.Start();

                if (AttachImplementToEquipmentAction.Operation.Status == BLL.Core.Domain.ActionStatus.Close)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = AttachImplementToEquipmentAction.Operation.ActionLog;
                    rm.LastMessage = AttachImplementToEquipmentAction.Operation.Message;
                }

                if (AttachImplementToEquipmentAction.Operation.Status == BLL.Core.Domain.ActionStatus.Started)
                {
                    AttachImplementToEquipmentAction.Operation.Validate();
                }

                if (AttachImplementToEquipmentAction.Operation.Status == BLL.Core.Domain.ActionStatus.Invalid)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = AttachImplementToEquipmentAction.Operation.ActionLog;
                    rm.LastMessage = AttachImplementToEquipmentAction.Operation.Message;
                }

                if (AttachImplementToEquipmentAction.Operation.Status == BLL.Core.Domain.ActionStatus.Valid)
                {
                    AttachImplementToEquipmentAction.Operation.Commit();
                }

                if (AttachImplementToEquipmentAction.Operation.Status == BLL.Core.Domain.ActionStatus.Failed)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = AttachImplementToEquipmentAction.Operation.ActionLog;
                    rm.LastMessage = AttachImplementToEquipmentAction.Operation.Message;
                }

                if (AttachImplementToEquipmentAction.Operation.Status == BLL.Core.Domain.ActionStatus.Succeed)
                {
                    rm.OperationSucceed = true;
                    rm.ActionLog = AttachImplementToEquipmentAction.Operation.ActionLog;
                    rm.LastMessage = AttachImplementToEquipmentAction.Operation.Message;
                }

                rm.Id = AttachImplementToEquipmentAction.Operation.UniqueId;
            }

            return rm.OperationSucceed;
        }

        public bool MoveImplementToInventory(int implementId, int equipmentId, int jobsiteId, int smu, decimal cost, DateTime date, string comment,
            int statusId, int repairerId, int workshopId, long authUserId)
        {
            BLL.Core.Domain.User actionUser = new BLL.Core.Domain.User();
            actionUser.Id = (int)authUserId;
            using (var dataEntities = new DAL.GETContext())
            {
                var user = dataEntities.USER_TABLE.Find(authUserId);
                actionUser.userName = user.username;
                actionUser.userStrId = user.userid;
            }

            var ActionParam = new BLL.Core.Domain.EquipmentActionRecord
            {
                EquipmentId = equipmentId,
                Cost = cost,
                Comment = comment,
                ReadSmuNumber = smu,
                ActionDate = date,
                ActionUser = actionUser,
                TypeOfAction = BLL.Core.Domain.ActionType.GETAction
            };

            var MoveImplementToInventoryParams = new BLL.Core.Domain.MoveImplementToInventoryParams
            {
                ImplementId = implementId,
                EquipmentId = equipmentId,
                JobsiteId = jobsiteId,
                UserId = authUserId,
                ActionType = BLL.Core.Domain.GETActionType.Move_Implement_To_Inventory,
                RecordedDate = DateTime.Now,
                EventDate = date,
                Comment = comment,
                Cost = cost,
                MeterReading = smu,
                StatusId = statusId,
                RepairerId = repairerId,
                WorkshopId = workshopId
            };

            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = " ",
                LastMessage = " ",
                Id = 0
            };

            using (var MoveImplementToInventoryAction = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), new DAL.GETContext(), ActionParam, MoveImplementToInventoryParams))
            {
                MoveImplementToInventoryAction.Operation.Start();

                if (MoveImplementToInventoryAction.Operation.Status == BLL.Core.Domain.ActionStatus.Close)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = MoveImplementToInventoryAction.Operation.ActionLog;
                    rm.LastMessage = MoveImplementToInventoryAction.Operation.Message;
                }

                if (MoveImplementToInventoryAction.Operation.Status == BLL.Core.Domain.ActionStatus.Started)
                {
                    MoveImplementToInventoryAction.Operation.Validate();
                }

                if (MoveImplementToInventoryAction.Operation.Status == BLL.Core.Domain.ActionStatus.Invalid)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = MoveImplementToInventoryAction.Operation.ActionLog;
                    rm.LastMessage = MoveImplementToInventoryAction.Operation.Message;
                }

                if (MoveImplementToInventoryAction.Operation.Status == BLL.Core.Domain.ActionStatus.Valid)
                {
                    MoveImplementToInventoryAction.Operation.Commit();
                }

                if (MoveImplementToInventoryAction.Operation.Status == BLL.Core.Domain.ActionStatus.Failed)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = MoveImplementToInventoryAction.Operation.ActionLog;
                    rm.LastMessage = MoveImplementToInventoryAction.Operation.Message;
                }

                if (MoveImplementToInventoryAction.Operation.Status == BLL.Core.Domain.ActionStatus.Succeed)
                {
                    rm.OperationSucceed = true;
                    rm.ActionLog = MoveImplementToInventoryAction.Operation.ActionLog;
                    rm.LastMessage = MoveImplementToInventoryAction.Operation.Message;
                }

                rm.Id = MoveImplementToInventoryAction.Operation.UniqueId;
            }

            return rm.OperationSucceed;
        }

        /// <summary>
        /// Determine the min date for implement events such as 'Attach to Equipment',
        /// 'Move to Inventory' and 'Change Implement Jobsite'.
        /// </summary>
        /// <param name="implementId"></param>
        /// <returns></returns>
        public string ReturnMinDateForImplementEvent(int implementId)
        {
            string result = "";

            using (var dataEntities = new DAL.GETContext())
            {
                var mostRecentImplementEvent = eventManagement.findPreviousImplementEvent(dataEntities, implementId, DateTime.Now);

                if(mostRecentImplementEvent != null)
                {
                    string dateFormat = "yyyy-MM-dd";
                    result = dataEntities.GET_EVENTS.Find(mostRecentImplementEvent.events_auto).event_date.ToString(dateFormat);
                }
            }

            return result;
        }


        /// <summary>
        /// Save an implement's image.
        /// </summary>
        /// <param name="getAuto"></param>
        /// <param name="image"></param>
        /// <returns></returns>
        public bool saveImplementImage(int getAuto, string image)
        {
            bool result = false;

            using (var dataEntities = new DAL.GETContext())
            {
                // Strip the 'data:*/*;base64,' string and return the Base-64 encoded part. 
                string base64String;
                try
                {
                    base64String = image.Split(new char[] { ',' })[1];
                }
                catch(Exception ex1)
                {
                    return false;
                }

                // Check if there is already an entry for this implement.
                var existingRecord = dataEntities.GET_IMPLEMENT_IMAGE
                    .Where(g => g.get_auto == getAuto).FirstOrDefault();
                if (existingRecord != null)
                {
                    existingRecord.attachment = Convert.FromBase64String(base64String);
                }

                // Else, create a new record.
                else
                {
                    GET_IMPLEMENT_IMAGE implementImage = new GET_IMPLEMENT_IMAGE
                    {
                        get_auto = getAuto,
                        attachment = Convert.FromBase64String(base64String)
                    };
                    dataEntities.GET_IMPLEMENT_IMAGE.Add(implementImage);
                }

                var _changesSaved = 0;
                try
                {
                    _changesSaved = dataEntities.SaveChanges();
                    result = true;
                }
                catch(Exception)
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Load an implement's image.
        /// </summary>
        /// <param name="get_auto"></param>
        /// <returns></returns>
        public string loadImplementImage(int get_auto)
        {
            string result;

            using (var dataEntities = new DAL.GETContext())
            {
                var img = dataEntities.GET_IMPLEMENT_IMAGE
                    .Where(g => g.get_auto == get_auto).FirstOrDefault();
                if((img != null) && (img.attachment != null))
                {
                    result = Convert.ToBase64String(img.attachment);
                }
                else
                {
                    result = "";
                }
            }

            return result;
        }

        // Delete an implement's image.
        public bool removeImplementImage(int get_auto)
        {
            bool result = false;

            using (var dataEntities = new DAL.GETContext())
            {
                var img = dataEntities.GET_IMPLEMENT_IMAGE
                    .Where(g => g.get_auto == get_auto).FirstOrDefault();
                dataEntities.GET_IMPLEMENT_IMAGE.Remove(img);

                var _changesSaved = 0;
                try
                {
                    _changesSaved = dataEntities.SaveChanges();
                    result = true;
                }
                catch (Exception)
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the list of implements (GETs) that are installed on a given equipment.
        /// </summary>
        /// <param name="equipmentId"></param>
        /// <returns></returns>
        public async Task<List<GenericIdNameVM>> GetImplementsOnEquipment(long equipmentId)
        {
            List<GenericIdNameVM> result = new List<GenericIdNameVM>();

            using (var _context = new DAL.GETContext())
            {
                result = await _context.GET
                    .Where(w => w.equipmentid_auto == equipmentId && w.on_equipment == true)
                    .Join(_context.LU_IMPLEMENT,
                        g => g.implement_auto,
                        i => i.implement_auto,
                        (g, i) => new { Gs = g, IMPs = i })
                    .Select(s => new GenericIdNameVM
                    {
                        Id = s.Gs.get_auto,
                        Name = (s.IMPs.implementdescription + " : " + s.Gs.impserial)
                    }).ToListAsync();
            }

            return result;
        }

        /// <summary>
        /// Gets the list of implements (GETs) that are in inventory at the specified jobsite.
        /// </summary>
        /// <param name="equipmentId"></param>
        /// <returns></returns>
        public async Task<List<GenericIdNameVM>> GetImplementsInInventory(int jobsiteId)
        {
            List<GenericIdNameVM> result = new List<GenericIdNameVM>();

            using (var _context = new DAL.GETContext())
            {
                result = await _context.GET_INVENTORY
                    .Where(w => w.jobsite_auto == jobsiteId)
                    .Join(_context.LU_IMPLEMENT,
                        g => g.GETs.implement_auto,
                        i => i.implement_auto,
                        (g, i) => new { Gs = g, IMPs = i })
                    .Select(s => new GenericIdNameVM
                    {
                        Id = s.Gs.get_auto,
                        Name = (s.IMPs.implementdescription + " : " + s.Gs.GETs.impserial)
                    }).ToListAsync();
            }

            return result;
        }

        /// <summary>
        /// Gets the details for a given implement.
        /// </summary>
        /// <param name="get_auto"></param>
        /// <returns></returns>
        public async Task<ImplementDetails> GetImplementDetails(int get_auto)
        {
            ImplementDetails result = new ImplementDetails();

            using (var _context = new DAL.GETContext())
            {
                var result2 = await _context.GET.Where(w => w.get_auto == get_auto)
                    .Select(s => new
                    {
                        Id = s.get_auto,
                        Make = s.make_auto.Value,
                        ImplementType = s.implement_auto.Value,
                        SerialNo = s.impserial,
                        SetupDate = s.created_date.Value,
                        ImplementHoursAtSetup = s.impsetup_hours.Value,
                        EquipmentSMUAtSetup = s.installsmu.Value
                    }).FirstOrDefaultAsync();

                result = new ImplementDetails
                {
                    Id = result2.Id,
                    Make = result2.Make,
                    ImplementType = result2.ImplementType,
                    SerialNo = result2.SerialNo,
                    SetupDate = result2.SetupDate.ToShortDateString(),
                    ImplementHoursAtSetup = result2.ImplementHoursAtSetup,
                    EquipmentSMUAtSetup = result2.EquipmentSMUAtSetup
                };
            }

            return result;
        }

        /// <summary>
        /// Insert a new GET record in the dbo.GET table.
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        private int InsertNewImplement(ImplementDetails details, int equipmentId, int jobsiteId, GETContext _context, long authUserId)
        {
            // If the insert fails, return -1 instead of the Id.
            int result = -1;

            try
            {
                GET newGET = new GET();
                newGET.make_auto = details.Make;
                newGET.implement_auto = details.ImplementType;
                newGET.impserial = details.SerialNo;
                newGET.created_date = DateTime.Parse(details.SetupDate);
                newGET.created_user = authUserId.ToString();
                newGET.isinuse = true;
                newGET.impsetup_hours = details.ImplementHoursAtSetup;
                
                // Inventory
                if((equipmentId == 0) && (jobsiteId > 0))
                {
                    newGET.equipmentid_auto = null;
                    newGET.installsmu = 0;
                    newGET.on_equipment = false;
                }
                // Equipment
                else
                {
                    newGET.equipmentid_auto = equipmentId;
                    newGET.installsmu = details.EquipmentSMUAtSetup;
                    newGET.on_equipment = true;
                }

                _context.GET.Add(newGET);

                int _changesSaved = _context.SaveChanges();
                if (_changesSaved > 0)
                {
                    // It saved. Great, now return the get_auto.
                    result = newGET.get_auto;
                }
            }
            catch (Exception ex1)
            {
                result = -1;
            }

            return result;
        }

        /// <summary>
        /// Update implement details in the dbo.GET table.
        /// </summary>
        /// <param name="updatedDetails"></param>
        /// <returns></returns>
        private int UpdateExistingImplement(ImplementDetails updatedDetails, int equipmentId, int jobsiteId, GETContext _context, long authUserId)
        {
            // Default code for no changes made.
            int result = 0;

            var GET_details = _context.GET.Find(updatedDetails.Id);
            if (GET_details != null)
            {
                try
                {
                    GET_details.make_auto = updatedDetails.Make;
                    GET_details.implement_auto = updatedDetails.ImplementType;
                    GET_details.impserial = updatedDetails.SerialNo;
                    GET_details.created_date = DateTime.Parse(updatedDetails.SetupDate);
                    GET_details.impsetup_hours = updatedDetails.ImplementHoursAtSetup;

                    // Inventory
                    if ((equipmentId == 0) && (jobsiteId > 0))
                    {
                        GET_details.equipmentid_auto = null;
                        GET_details.installsmu = 0;
                        GET_details.on_equipment = false;
                    }
                    // Equipment
                    else
                    {
                        GET_details.equipmentid_auto = equipmentId;
                        GET_details.installsmu = updatedDetails.EquipmentSMUAtSetup;
                        GET_details.on_equipment = true;
                    }
                    _context.Entry(GET_details).State = EntityState.Modified;

                    int _changesSaved = _context.SaveChanges();
                    if (_changesSaved > 0)
                    {
                        // Return the get_auto for the entry that was saved.
                        result = GET_details.get_auto;
                    }

                }
                catch (Exception ex1)
                {
                    // Return the error code -1 instead of the Id.
                    result = -1;
                }
            }

            return result;
        }

        /// <summary>
        /// Update GET details.
        /// </summary>
        /// <param name="details"></param>
        /// <returns></returns>
        public int UpdateImplement(SaveImplementDetailParams implementDetailParams, long authUserId)
        {
            int result = 0;

            using (var _context = new DAL.GETContext())
            {
                // Insert
                if(implementDetailParams.Details.Id == 0)
                {
                    result = InsertNewImplement(implementDetailParams.Details, implementDetailParams.EquipmentId, implementDetailParams.JobsiteId, _context, authUserId);
                    if(result > 0)
                    {
                        // Create Measurement points
                        var componentManagement = new ComponentManagement();
                        int result2 = componentManagement.CreateNewMeasurementPoints(result, _context);
                    }
                }
                // Update
                else if (implementDetailParams.Details.Id != 0)
                {
                    result = UpdateExistingImplement(implementDetailParams.Details, implementDetailParams.EquipmentId, implementDetailParams.JobsiteId, _context, authUserId);
                }

                // If this is for an implement in inventory and the GET record was updated successfully, then 
                // update the GET_INVENTORY record.
                if((implementDetailParams.EquipmentId == 0) && (implementDetailParams.JobsiteId > 0) && (result > 0))
                {
                    InventoryManagement inventory = new InventoryManagement();
                    implementDetailParams.Details.Id = result;
                    inventory.UpdateImplementInInventory(implementDetailParams.Details, implementDetailParams.JobsiteId, authUserId, _context);
                }
            }

            return result;
        }

        /// <summary>
        /// Validate the implement serial number entered into the system.
        /// </summary>
        /// <param name="implementId"></param>
        /// <param name="implementSerial"></param>
        /// <returns></returns>
        public async Task<bool> ValidateImplementSerial(int implementId, string implementSerial)
        {
            bool result = false;

            using (var _context = new DAL.GETContext())
            {
                // Get the implement for the provided Id.
                var implement = _context.GET.Find(implementId);

                // Find any record with a matching serial number.
                var recordWithSameSerialNo = await _context.GET
                            .Where(w => w.impserial.Equals(implementSerial) && w.get_auto != implementId)
                            .FirstOrDefaultAsync();

                // Check whether the implement exists.
                if (implement != null)
                {
                    // The implement serial already belongs to this GET, so ofcourse it's valid :)
                    if (implement.impserial.Equals(implementSerial))
                    {
                        result = true;
                    }

                    // Another GET has this serial no.
                    else if (recordWithSameSerialNo != null)
                    {
                        result = false;
                    }

                    // Serial number is available.
                    else
                    {
                        result = true;
                    }
                }

                // Implement does not exist.
                else
                {
                    // Another GET has this serial no.
                    if (recordWithSameSerialNo != null)
                    {
                        result = false;
                    }

                    // Serial number is available.
                    else
                    {
                        result = true;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Check and return true if the implement setup LTD is less than or equal to the 
        /// implement's first non-setup event LTD.
        /// </summary>
        /// <param name="getAuto"></param>
        /// <param name="implementLTD"></param>
        /// <returns></returns>
        public async Task<bool> ValidateImplementSetupLTD(int getAuto, int implementLTD)
        {
            bool result = false;

            // Guard
            if((getAuto < 0) || (implementLTD < 0))
            {
                return result;
            }
            // New implement
            else if (getAuto == 0)
            {
                result = true;
                return result;
            }

            using (var _context = new DAL.GETContext())
            {
                var firstNonSetupEvent = await _context.GET_EVENTS_IMPLEMENT
                    .Join(_context.GET_EVENTS,
                        gei => gei.events_auto,
                        ge => ge.events_auto,
                        (gei, ge) => new { Events = ge, ImplementEvents = gei })
                    .Where(w => w.Events.recordStatus == 0 && w.ImplementEvents.get_auto == getAuto
                        && w.Events.action_auto != (int)Core.Domain.GETActionType.Implement_Setup
                        && w.Events.action_auto != (int)Core.Domain.GETActionType.Implement_Updated)
                    .OrderBy(o => o.Events.event_date)
                    .ThenBy(o2 => o2.Events.events_auto)
                    .FirstOrDefaultAsync();

                // Events other than 'Setup' exist, so check that the LTD is in the valid range.
                if (firstNonSetupEvent != null)
                {
                    if (implementLTD <= firstNonSetupEvent.ImplementEvents.ltd)
                    {
                        result = true;
                    }
                }
                // No other events except 'Setup' currently exist. 
                else
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Check and return true if the implement setup date is earlier than or equal to the 
        /// implement's first event date.
        /// </summary>
        /// <param name="implementId"></param>
        /// <param name="setupDate"></param>
        /// <returns></returns>
        public async Task<bool> ValidateImplementSetupDate(int getAuto, string setupDate)
        {
            bool result = false;

            // Date variables
            string[] dateParts;
            int iDay;
            int iMonth;
            int iYear;
            DateTime dateOfReading;

            // Guard
            if(getAuto < 0)
            {
                return result;
            }

            // Parse date of reading.
            try
            {
                dateParts = setupDate.Split('/');
                iDay = int.Parse(dateParts[0]);
                iMonth = int.Parse(dateParts[1]);
                iYear = int.Parse(dateParts[2]);

                dateOfReading = new DateTime(iYear, iMonth, iDay);
            }
            catch (Exception ex1)
            {
                return result;
            }

            // New implement, reading date is fine, return true.
            if (getAuto == 0)
            {
                result = true;
                return result;
            }

            using (var _context = new DAL.GETContext())
            {
                var firstNonSetupEvent = await _context.GET_EVENTS_IMPLEMENT
                    .Join(_context.GET_EVENTS,
                        gei => gei.events_auto,
                        ge => ge.events_auto,
                        (gei, ge) => new { Events = ge, ImplementEvents = gei })
                    .Where(w => w.Events.recordStatus == 0 && w.ImplementEvents.get_auto == getAuto
                        && w.Events.action_auto != (int)Core.Domain.GETActionType.Implement_Setup
                        && w.Events.action_auto != (int)Core.Domain.GETActionType.Implement_Updated)
                    .OrderBy(o => o.Events.event_date)
                    .ThenBy(o2 => o2.Events.events_auto)
                    .FirstOrDefaultAsync();

                // Events other than 'Setup' exist, so check that the setup date is in the valid range.
                if (firstNonSetupEvent != null)
                {
                    if (dateOfReading <= firstNonSetupEvent.Events.event_date)
                    {
                        result = true;
                    }
                }
                // No other events except 'Setup' currently exist. 
                else
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Validate the the SMU reading provided for the implement setup screen.
        /// </summary>
        /// <param name="getAuto"></param>
        /// <param name="equipmentId"></param>
        /// <param name="equipmentSMU"></param>
        /// <returns></returns>
        public async Task<bool> ValidateEquipmentSMUAtSetup(int getAuto, int equipmentId, int equipmentSMU, string setupDate)
        {
            bool result = false;

            // Date variables
            string[] dateParts;
            int iDay;
            int iMonth;
            int iYear;
            DateTime actionDate;

            // Guard
            if ((getAuto < 0) || (equipmentId <= 0) || (equipmentSMU < 0))
            {
                return result;
            }

            // Parse date of reading.
            try
            {
                dateParts = setupDate.Split('/');
                iDay = int.Parse(dateParts[0]);
                iMonth = int.Parse(dateParts[1]);
                iYear = int.Parse(dateParts[2]);

                actionDate = new DateTime(iYear, iMonth, iDay);
            }
            catch (Exception ex1)
            {
                return result;
            }

            using (var _context = new DAL.GETContext())
            {
                var minSMU_event = await _context.EQUIPMENT_LIVES
                    .Join(_context.ACTION_TAKEN_HISTORY,
                        el => el.ActionId,
                        ath => ath.history_id,
                        (el, ath) => new { EquipmentLife = el, ActionTakenHistory = ath })
                    .Where(w => w.EquipmentLife.EquipmentId == equipmentId
                        && w.EquipmentLife.ActionDate <= actionDate
                        && w.ActionTakenHistory.recordStatus == 0)
                    .OrderByDescending(o => o.EquipmentLife.Id)
                    .FirstOrDefaultAsync();

                var maxSMU_event = await _context.EQUIPMENT_LIVES
                    .Join(_context.ACTION_TAKEN_HISTORY,
                        el => el.ActionId,
                        ath => ath.history_id,
                        (el, ath) => new { EquipmentLife = el, ActionTakenHistory = ath })
                    .Where(w => w.EquipmentLife.EquipmentId == equipmentId
                        && w.EquipmentLife.ActionDate > actionDate
                        && w.ActionTakenHistory.recordStatus == 0)
                    .OrderBy(o => o.EquipmentLife.Id)
                    .FirstOrDefaultAsync();

                // Both previous and next events are available for that action date.
                if((minSMU_event != null) && (maxSMU_event != null))
                {
                    if((equipmentSMU >= minSMU_event.EquipmentLife.SerialMeterReading) 
                        && (equipmentSMU <= maxSMU_event.EquipmentLife.SerialMeterReading)
                        && (minSMU_event.EquipmentLife.SerialMeterReading >= 0))
                    {
                        result = true;
                    }
                }
                // No valid newer event exists, therefore there is no upper bound for the SMU to check against.
                else if ((minSMU_event != null) && (maxSMU_event == null))
                {
                    if ((equipmentSMU >= minSMU_event.EquipmentLife.SerialMeterReading) && (minSMU_event.EquipmentLife.SerialMeterReading >= 0))
                    {
                        result = true;
                    }
                }
                // No valid earlier event exists, therefore the lower bound for the SMU is zero.
                else if ((minSMU_event != null) && (maxSMU_event != null))
                {
                    if ((equipmentSMU >= 0) && (equipmentSMU <= maxSMU_event.EquipmentLife.SerialMeterReading))
                    {
                        result = true;
                    }
                }
                // No valid events exists for that time period to check against.
                else
                {
                    // NOTE: Should be false to enforce validation. But it will likely fail in 
                    // legitimate scenarios where the SMU is valid but no action record or events 
                    // exist for that equipment. (Need to review this further)
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Method which allows the specified implement (GET) and all dependent 
        /// records (GET COMPONENTS, EVENTS, etc...) to be deleted permanently.
        /// </summary>
        /// <param name="getAuto"></param>
        /// <returns></returns>
        public bool DeleteImplement(int getAuto)
        {
            bool result = false;
            int changesSaved = 0;

            using (var context = new DAL.GETContext())
            {
                var GET_record = context.GET.Find(getAuto);
                if(GET_record != null)
                {
                    // Check that there are no inspections before proceeding with delete.
                    bool hasInspections = context.GET_IMPLEMENT_INSPECTION.Where(w => w.get_auto == getAuto).Any();
                    if(hasInspections)
                    {
                        result = false;
                    }

                    else
                    {
                        context.GET.Remove(GET_record);

                        try
                        {
                            changesSaved = context.SaveChanges();

                            if (changesSaved > 0)
                            {
                                result = true;
                            }
                        }
                        catch { }
                    }  
                }
            }

            return result;
        }
    }
}
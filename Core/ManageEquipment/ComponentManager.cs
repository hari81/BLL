using BLL.Core.Domain;
using DAL;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using BLL.Core.ViewModel;
using System.Threading.Tasks;
using System;
using System.Data.Entity;

namespace BLL.Core.ManageEquipment
{
    public class ComponentManager
    {
        private UndercarriageContext _context;


        /// <summary>
        /// This is used to retrieve and manipulate data on the manage equipment page in the undercarriage ui. 
        /// </summary>
        /// <param name="context">New instance of DAL.UndercarriageContext</param>
        public ComponentManager(UndercarriageContext context)
        {
            this._context = context;
        }




        public List<CompartSearchViewModel> GetComponentsByCompartId(int compartId)
        {
            var comparts = _context.LU_COMPART.Where(e => e.compartid_auto == compartId).ToList();
            return ConvertToCompartViewModels(comparts);
        }

        public List<CompartSearchViewModel> GetComponentsAdvanceSearch(List<SearchItem> SearchItems)
        {
            var comparts = _context.LU_COMPART.ToList();

            //Compart Type
            var compartTypeSearchId = SearchItems.Where(item => item.Id == (int)SearchItemType.CompartType).Select(m => m.SearchId).FirstOrDefault();
            if (compartTypeSearchId != 0)
                comparts = comparts.Where(m => m.comparttype_auto == compartTypeSearchId).ToList();

            //Make
            var makeSearchId = SearchItems.Where(item => item.Id == (int)SearchItemType.Make && item.SearchId != 0).Select(m => m.SearchId).FirstOrDefault();
            comparts = GetLUCompartsByMake(makeSearchId, comparts);

            //Model
            var modelSearchId = SearchItems.Where(item => item.Id == (int)SearchItemType.Model && item.SearchId != 0).Select(m => m.SearchId).FirstOrDefault();
            comparts = GetLUCompartsByModel(modelSearchId, comparts);

            return ConvertToCompartViewModels(comparts);

        }

        public List<CompartSearchViewModel> GetComponentsWithMeasurementPointConfig()
        {
            var comparts = _context.LU_COMPART.Where(c => c.MeasurementPoints.Count() > 0).ToList();
            return ConvertToCompartViewModels(comparts);
        }

        public List<CompartSearchViewModel> GetComponentsByCompartType(CompartTypeEnum compartType)
        {
            var comparts = _context.LU_COMPART.Where(c => c.comparttype_auto == (int)compartType).ToList();
            return ConvertToCompartViewModels(comparts);
        }

        public List<LU_COMPART> GetLUCompartsByMake(int makeId, List<LU_COMPART> filteredList = null)//make
        {
            if (makeId == 0 && filteredList != null) return filteredList;
            var trackcompart = _context.TRACK_COMPART_EXT.Where(e => e.make_auto == makeId).Select(id => id.compartid_auto).ToList();
            var comparts = new List<LU_COMPART>();

            if (filteredList != null)
                comparts = filteredList.Where(e => trackcompart.Contains(e.compartid_auto)).ToList();
            else
                comparts = _context.LU_COMPART.Where(e => trackcompart.Contains(e.compartid_auto)).ToList();

            return comparts;
        }

        public List<LU_COMPART> GetLUCompartsByModel(int modelId, List<LU_COMPART> filteredList = null)//model
        {
            if (modelId == 0 && filteredList != null) return filteredList;
            var modelMapping = _context.TRACK_COMPART_MODEL_MAPPING.Where(e => e.model_auto == modelId).Select(m => m.compartid_auto).ToList();
            var comparts = new List<LU_COMPART>();
            if (filteredList != null)
                comparts = filteredList.Where(e => modelMapping.Contains(e.compartid_auto)).ToList();
            else
                comparts = _context.LU_COMPART.Where(e => modelMapping.Contains(e.compartid_auto)).ToList();
            return comparts;
        }

        public List<CompartSearchViewModel> GetComponentsByMake(int makeId, List<LU_COMPART> filteredList = null)//make
        {
            var trackcompart = _context.TRACK_COMPART_EXT.Where(e => e.make_auto == makeId).Select(id => id.compartid_auto).ToList();
            var comparts = new List<LU_COMPART>();

            if (filteredList != null)
                comparts = filteredList.Where(e => trackcompart.Contains(e.compartid_auto)).ToList();
            else
                comparts = _context.LU_COMPART.Where(e => trackcompart.Contains(e.compartid_auto)).ToList();

            return ConvertToCompartViewModels(comparts);
        }

        public List<CompartSearchViewModel> GetComponentsByModelId(int modelId, List<LU_COMPART> filteredList = null)//model
        {
            var modelMapping = _context.TRACK_COMPART_MODEL_MAPPING.Where(e => e.model_auto == modelId).Select(m => m.compartid_auto).ToList();
            var comparts = new List<LU_COMPART>();
            if (filteredList != null)
                comparts = filteredList.Where(e => modelMapping.Contains(e.compartid_auto)).ToList();
            else
                comparts = _context.LU_COMPART.Where(e => modelMapping.Contains(e.compartid_auto)).ToList();
            return ConvertToCompartViewModels(comparts);
        }

        private List<CompartSearchViewModel> ConvertToCompartViewModels(List<LU_COMPART> comparts)
        {
            var result = new List<CompartSearchViewModel>();
            foreach (var item in comparts)
            {
                result.Add(CompartViewModelConverter(item));
            }
            return result;
        }

        private CompartSearchViewModel CompartViewModelConverter(LU_COMPART item)
        {
            var make = item.TRACK_COMPART_EXT.FirstOrDefault(t => t.compartid_auto == item.compartid_auto)?.MAKE;
            var makeName = "";
            var makeid = 0;
            if (make != null)
            {
                makeName = make.makedesc;
                makeid = make.make_auto;
            }

            return new CompartSearchViewModel
            {
                CompartId = item.compartid_auto,
                CompartName = item.compart,
                CompartType = ((CompartTypeEnum)item.comparttype_auto).ToString(),
                Make = makeName,
                Models = _context.TRACK_COMPART_MODEL_MAPPING.FirstOrDefault(m => m.compartid_auto == item.compartid_auto)?.Model?.modeldesc,
                MakeId = makeid,
            };
        }

        private List<MeasurementPonitsViewModel> ConvertToMeasurementPointsViewModels(List<COMPART_MEASUREMENT_POINT> measurementPoints)
        {
            var result = new List<MeasurementPonitsViewModel>();
            foreach (var item in measurementPoints)
            {
                var bugetlifeObj = _context.TRACK_COMPART_EXT.FirstOrDefault(t => t.compartid_auto == item.CompartId);
                var allCalculs = FindAllCalculationMethodsBasedOnCompartIdAndMPId(item.CompartId, item.Id);
                if (bugetlifeObj != null && bugetlifeObj.budget_life.HasValue)
                    result.Add(MeasurementPointsViewModelConverter(item, allCalculs, bugetlifeObj.budget_life.Value));
                else
                {
                    var generalEq = _context.GENERAL_EQ_UNIT.FirstOrDefault(g => g.compartid_auto == item.CompartId);
                    if(generalEq != null && generalEq.track_budget_life.HasValue)
                        result.Add(MeasurementPointsViewModelConverter(item, allCalculs, generalEq.track_budget_life.Value));
                    else
                        result.Add(MeasurementPointsViewModelConverter(item, allCalculs));
                }
            }
            return result;
        }

        private MeasurementPonitsViewModel MeasurementPointsViewModelConverter(COMPART_MEASUREMENT_POINT measurementPoint, List<CalculationMethod> calculationMethods, int bugetLife = 0)
        {
            return new MeasurementPonitsViewModel
            {
                CompartId = measurementPoint.CompartId,
                Name = measurementPoint.Name,
                DefaultNumberOfMeasurements = measurementPoint.DefaultNumberOfMeasurements,
                DefaultToolId = measurementPoint.DefaultToolId,
                MeasurementPonitId = measurementPoint.Id,
                Order = measurementPoint.Order,
                isDisabled = measurementPoint.Disabled,
                BugetLife = bugetLife,
                CalculationMethods = calculationMethods
            };
        }

        public CompartMeasurementPonitsViewModel GetCompartMeasurementPonitsViewModel(int compartId)
        {
            var compart = _context.LU_COMPART.FirstOrDefault(c => c.compartid_auto == compartId);
            var measurementPoints = _context.COMPART_MEASUREMENT_POINT.Where(m => m.CompartId == compartId).OrderBy(c => c.Order).ToList();
            if (compart == null) throw new System.Exception("no compart Id found in database  " + compartId);
            var compartViewmodel = CompartViewModelConverter(compart);
            var measurementPonitsViewModels = ConvertToMeasurementPointsViewModels(measurementPoints);


            return new CompartMeasurementPonitsViewModel
            {
                Compart = compartViewmodel,
                MeasurementPointsViewModels = measurementPonitsViewModels,
            };
        }

        public List<CompartSearchViewModel> GetCompartHasMeasurementPointsConfigured()
        {
            return ConvertToCompartViewModels(_context.LU_COMPART.Where(e => e.MeasurementPoints.Count() > 0).ToList());
        }

        public List<MeasurementToolsViewModel> GetAllMeasurementTools()
        {
            var tools = _context.TRACK_TOOL.Where(t => t.tool_auto > 0).ToList();
            var result = new List<MeasurementToolsViewModel>();
            foreach (var item in tools)
            {
                result.Add(new MeasurementToolsViewModel
                {
                    ToolCode = item.tool_code,
                    ToolName = item.tool_name,
                    ToolId = item.tool_auto,
                });
            }
            return result;
        }



        public async Task<Tuple<int, string>> CreateNewMeasurementPoint(CreateMeasurementPointModel model)
        {
            var LogoArr = model.Image.Split(',');
            string meausurementPhoto = "";
            if (LogoArr.Length > 1)
                meausurementPhoto = LogoArr[1].Trim();

            //making sure it is the correct order
            //var lastOrder = _context.COMPART_MEASUREMENT_POINT.OrderBy(c => c.Order).LastOrDefault();
            //if (lastOrder != null && lastOrder.Order != model.Order)
            //    model.Order = lastOrder.Order + 1;



            var newMeasurement = new COMPART_MEASUREMENT_POINT
            {
                CompartId = model.CompartId,
                DefaultNumberOfMeasurements = model.DefaultNumberOfMeasurements,
                DefaultToolId = model.DefaultToolId,
                Image = meausurementPhoto.Length > 0 ? Convert.FromBase64String(meausurementPhoto) : null,
                Order = model.Order,
                Name = model.Name,
                Disabled = model.isDisabled,
            };

            _context.COMPART_MEASUREMENT_POINT.Add(newMeasurement);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Tuple.Create(-1, "Failed to create new measurement ponit records. " + ex.Message + ". " + ex.InnerException != null ? ex.InnerException.Message : "");
            }

            return Tuple.Create(newMeasurement.Id, "New measurement point has been added successfully. ");
        }

        public Tuple<int, string> ChangeMeasurementPointStatus(int measurementPointId, bool status)
        {
            var measurementToChange = _context.COMPART_MEASUREMENT_POINT.FirstOrDefault(m => m.Id == measurementPointId);
            if (measurementToChange == null) throw new Exception("There is no measurement point with Id  " + measurementPointId);
            measurementToChange.Disabled = status;
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                return Tuple.Create(-1, "Failed to create new measurement ponit records. " + ex.Message + ". " + ex.InnerException != null ? ex.InnerException.Message : "");
            }
            var message = measurementToChange.Disabled ? "Measurement Points " + measurementToChange.Name + " disabled" : "Measurement Points  " + measurementToChange.Name + " enabled";
            return Tuple.Create(1, message);
        }

        public List<CalculationMethod> FindAllCalculationMethodsBasedOnCompartIdAndMPId(int compartId, int measurementId)
        {
            var result = new List<CalculationMethod>();
            var compartExt = _context.TRACK_COMPART_EXT.Where(t => t.LU_COMPART.compartid_auto == compartId && t.CompartMeasurePointId == measurementId).ToList();
            foreach (var item in compartExt)
            {
                switch (item.track_compart_worn_calc_method_auto)
                {
                    case (int)MenuRepresentationMPCalculationMethodName.STEPPED:
                        var itmCal = _context.TRACK_COMPART_WORN_LIMIT_ITM.Where(s => s.MeasurePointId == item.CompartMeasurePointId
                        && s.compartid_auto == item.compartid_auto
                         && s.track_tools_auto == item.TRACK_TOOL.tool_auto
                        ).ToList();
                        foreach (var itm in itmCal)
                        {
                            result.Add(ConvertITMEntityToViewModel(itm, (int)MenuRepresentationMPCalculationMethodName.STEPPED, (int)item.track_compart_ext_auto));
                        }
                        break;
                    case (int)MenuRepresentationMPCalculationMethodName.Inflection:
                        var catCal = _context.TRACK_COMPART_WORN_LIMIT_CAT.Where(s => s.MeasurePointId == item.CompartMeasurePointId
                        && s.compartid_auto == item.compartid_auto
                        && s.track_tools_auto == item.TRACK_TOOL.tool_auto
                        ).ToList();
                        foreach (var cat in catCal)
                        {
                            result.Add(ConvertCATEntityToViewModel(cat, (int)MenuRepresentationMPCalculationMethodName.Inflection, (int)item.track_compart_ext_auto));
                        }
                        break;
                    case (int)MenuRepresentationMPCalculationMethodName.Polynomial:
                        var koCal = _context.TRACK_COMPART_WORN_LIMIT_KOMATSU.Where(s => s.MeasurePointId == item.CompartMeasurePointId
                        && s.compartid_auto == item.compartid_auto
                         && s.track_tools_auto == item.TRACK_TOOL.tool_auto
                        ).ToList();
                        foreach (var ko in koCal)
                        {
                            result.Add(ConvertKomatsuEntityToViewModel(ko, (int)MenuRepresentationMPCalculationMethodName.Polynomial, (int)item.track_compart_ext_auto));
                        }
                        break;
                    case (int)MenuRepresentationMPCalculationMethodName.JD:
                        //todo 
                        break;
                    case (int)MenuRepresentationMPCalculationMethodName.Linear:
                        var liCal = _context.TRACK_COMPART_WORN_LIMIT_LIEBHERR.Where(s => s.MeasurePointId == item.CompartMeasurePointId
                        && s.compartid_auto == item.compartid_auto
                        && s.track_tools_auto == item.TRACK_TOOL.tool_auto
                        ).ToList();
                        foreach (var lin in liCal)
                        {
                            result.Add(ConvertLinearEntityToViewModel(lin, (int)MenuRepresentationMPCalculationMethodName.Linear, (int)item.track_compart_ext_auto));
                        }
                        break;
                    default:

                        break;
                }
            }
            return result;
        }

        private SteppedCalMethodViewModel ConvertITMEntityToViewModel(TRACK_COMPART_WORN_LIMIT_ITM itm, int WornCalculationMethodTypeId, int extId)
        {
            return new SteppedCalMethodViewModel
            {
                WornCalculationMethodTypeId = WornCalculationMethodTypeId,
                WornCalculationMethodTableAutoId = itm.track_compart_worn_limit_itm_auto,
                CompartId = itm.compartid_auto,
                MeasurementPointId = itm.MeasurePointId,
                ToolId = itm.track_tools_auto,
                CompartEXTId = extId,


                StartDepthNew = itm.start_depth_new==null ? 0 : itm.start_depth_new,
                StartDepth_10 = itm.wear_depth_10_percent == null ? 0 : itm.wear_depth_10_percent,
                StartDepth_20 = itm.wear_depth_20_percent == null ? 0 : itm.wear_depth_20_percent,
                StartDepth_30 = itm.wear_depth_30_percent== null ? 0 : itm.wear_depth_30_percent,
                StartDepth_40 = itm.wear_depth_40_percent == null ? 0 : itm.wear_depth_40_percent,
                StartDepth_50 = itm.wear_depth_50_percent == null ? 0 : itm.wear_depth_50_percent,
                StartDepth_60 = itm.wear_depth_60_percent == null ? 0 : itm.wear_depth_60_percent,
                StartDepth_70 = itm.wear_depth_70_percent == null ? 0 : itm.wear_depth_70_percent,
                StartDepth_80 = itm.wear_depth_80_percent == null ? 0 : itm.wear_depth_80_percent,
                StartDepth_90 = itm.wear_depth_90_percent == null ? 0 : itm.wear_depth_90_percent,
                StartDepth_100 = itm.wear_depth_100_percent == null ? 0 : itm.wear_depth_100_percent,
                StartDepth_110 = itm.wear_depth_110_percent == null ? 0 : itm.wear_depth_110_percent,
                StartDepth_120 = itm.wear_depth_120_percent == null ? 0 : itm.wear_depth_120_percent,
            
            };
        }

        private InflectionCalMethodViewModel ConvertCATEntityToViewModel(TRACK_COMPART_WORN_LIMIT_CAT cat, int WornCalculationMethodTypeId, int extId)
        {
            return new InflectionCalMethodViewModel
            {
                WornCalculationMethodTypeId = WornCalculationMethodTypeId,
                WornCalculationMethodTableAutoId = cat.track_compart_worn_limit_cat_auto,
                CompartId = cat.compartid_auto,
                ToolId = cat.track_tools_auto,
                MeasurementPointId = cat.MeasurePointId,
                CompartEXTId = extId,

                Adjust_base = cat.adjust_base == null ? 0 : cat.adjust_base,
                Hi_InflectionPoint = cat.hi_inflectionPoint == null ? 0 : cat.hi_inflectionPoint,
                Hi_Intercept1 = cat.hi_intercept1 == null ? 0 : cat.hi_intercept1,
                Hi_Intercept2 = cat.hi_intercept2 == null ? 0 : cat.hi_intercept2,
                Hi_Slope1 = cat.hi_slope1 == null ? 0 : cat.hi_slope1,
                Hi_Slope2 = cat.hi_slope2 == null ? 0 : cat.hi_slope2,
                Mi_Intercept1 = cat.mi_intercept1 == null ? 0 : cat.mi_intercept1,
                Mi_Intercept2 = cat.mi_intercept2 == null ? 0 : cat.mi_intercept2,
                Mi_Slope1 = cat.mi_slope1 == null ? 0 : cat.mi_slope1,
                Mi_Slope2 = cat.mi_slope2 == null ? 0 : cat.mi_slope2,
                Slope = cat.slope == null ? 0 : cat.slope,
                Mi_InflectionPoint = cat.mi_inflectionPoint == null ? 0 : cat.mi_inflectionPoint,
              
            };
        }

        private PolynomialCalMethodViewModel ConvertKomatsuEntityToViewModel(TRACK_COMPART_WORN_LIMIT_KOMATSU ko, int WornCalculationMethodTypeId, int extId)
        {
            return new PolynomialCalMethodViewModel
            {
                WornCalculationMethodTypeId = WornCalculationMethodTypeId,
                WornCalculationMethodTableAutoId = ko.track_compart_worn_limit_komatsu_auto,
                CompartId = ko.compartid_auto,
                Impact_Intercept = ko.impact_intercept,
                Impact_Secondorder = ko.impact_secondorder,
                Impact_Slope = ko.impact_slope,
                MeasurementPointId = ko.MeasurePointId,
                Normal_Intercept = ko.normal_intercept,
                Normal_Secondorder = ko.normal_secondorder,
                Normal_Slope = ko.normal_slope,
                Slope_Impact = ko.slope_impact,
                Slope_Normal = ko.slope_normal,
                ToolId = ko.track_tools_auto,

                CompartEXTId = extId,
            };
        }

        private LinearCalMethodViewModel ConvertLinearEntityToViewModel(TRACK_COMPART_WORN_LIMIT_LIEBHERR linear, int WornCalculationMethodTypeId, int extId)
        {
            return new LinearCalMethodViewModel
            {
                WornCalculationMethodTypeId = WornCalculationMethodTypeId,
                WornCalculationMethodTableAutoId = linear.track_compart_worn_limit_liebherr_auto,
                ToolId = linear.track_tools_auto,
                CompartId = linear.compartid_auto,
                Impact_Intercept = linear.impact_intercept,
                Impact_Slope = linear.impact_slope,
                MeasurementPointId = linear.MeasurePointId,
                Normal_Intercept = linear.normal_intercept,
                Normal_Slope = linear.normal_slope,
                CompartEXTId = extId
            };
        }



        public async Task<Tuple<int, string>> UpdateMeasurementPoints(UpdateAllCalculationMethodsViewModel model)
        {

            foreach (var item in model.SteppedMethods)
            {
                UpdateCalculationSteppedMethod(item);
            }
            foreach (var item in model.InflectionMethods)
            {
                UpdateCalculationInflectionMethod(item);
            }           
            foreach (var item in model.PolyMethods)
            {
                UpdatepolyCalculationMethod(item);
            }
            foreach (var item in model.LinearMethods)
            {
                UpdateLinearCalculationMethod(item);
            }
            foreach (var item in model.JDMethods)
            {

            }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Tuple.Create(-1, "update measurement points. " + ex.Message + ". " + ex.InnerException != null ? ex.InnerException.Message : "");
            }

            return Tuple.Create(1, "Inserted and updated all");

            //foreach (var item in measurementPointModel.CalculationMethods)
            //{
            //   

            //    switch (item.WornCalculationMethodTypeId)
            //    {
            //        case ((int)MenuRepresentationMPCalculationMethodName.STEPPED):
            //            var stepresult = await UpdateCalculationSteppedMethod((SteppedCalMethodViewModel)item, isUpdating);
            //            results.Add(stepresult);
            //            break;
            //        case ((int)MenuRepresentationMPCalculationMethodName.Inflection):
            //            var inresult = await UpdateCalculationInflectionMethod((InflectionCalMethodViewModel)item, isUpdating);
            //            results.Add(inresult);
            //            break;
            //        case ((int)MenuRepresentationMPCalculationMethodName.Polynomial):
            //            var polyresult = await UpdatepolyCalculationMethod((PolynomialCalMethodViewModel)item, isUpdating);
            //            results.Add(polyresult);
            //            break;
            //        case ((int)MenuRepresentationMPCalculationMethodName.Linear):
            //            var linearresult = await UpdateLinearCalculationMethod((LinearCalMethodViewModel)item, isUpdating);
            //            results.Add(linearresult);
            //            break;
            //        case ((int)MenuRepresentationMPCalculationMethodName.JD):
            //            break;
            //        default: results.Add(Tuple.Create(-1, "Did not find any matching calculation method"));
            //            break;

            //    }

            //}

            //if (item is SteppedCalMethodViewModel)
            //{
            //   var result = await UpdateCalculationSteppedMethod((SteppedCalMethodViewModel)item, isUpdating);
            //    results.Add(result);
            //}
            //else if (item is InflectionCalMethodViewModel)
            //{
            //    var result = await UpdateCalculationInflectionMethod((InflectionCalMethodViewModel)item, isUpdating);
            //    results.Add(result);
            //}
            //else if (item is PolynomialCalMethodViewModel)
            //{
            //    var result = await UpdatepolyCalculationMethod((PolynomialCalMethodViewModel)item, isUpdating);
            //    results.Add(result);
            //}
            //else if (item is LinearCalMethodViewModel)
            //{
            //    var result = await UpdateLinearCalculationMethod((LinearCalMethodViewModel)item, isUpdating);
            //    results.Add(result);
            //}
            //else if (item is JDCalMehtodViewModel)
            //{
            //    return null;

            //}
            //else {
            //    results.Add(Tuple.Create(-1, "Did not find any matching calculation method"));
            //}


        }


        private void InsertNewTRACKCOMPARTExtRecord(CalculationMethod newcal)
        {
            var newExt = new TRACK_COMPART_EXT
            {
                budget_life = newcal.BugetLife,
                compartid_auto = newcal.CompartId,
                CompartMeasurePointId = newcal.MeasurementPointId,
                make_auto = newcal.MakeId,
                tools_auto = newcal.ToolId,
                track_compart_worn_calc_method_auto = newcal.WornCalculationMethodTypeId,
            };
            _context.TRACK_COMPART_EXT.Add(newExt);
           
        }

        private void InsertNewITM(SteppedCalMethodViewModel item)
        {
            var stepped = new TRACK_COMPART_WORN_LIMIT_ITM
            {
                compartid_auto = item.CompartId,
                MeasurePointId = item.MeasurementPointId,               
                track_tools_auto = item.ToolId,
                track_compart_worn_limit_itm_auto = item.WornCalculationMethodTypeId,

                start_depth_new = item.StartDepthNew == null ? 0 : item.StartDepthNew,
                wear_depth_10_percent = item.StartDepth_10 == null ? 0 : item.StartDepth_10,
                wear_depth_20_percent = item.StartDepth_20 == null ? 0 : item.StartDepth_20,
                wear_depth_30_percent = item.StartDepth_30 == null ? 0 : item.StartDepth_30,
                wear_depth_40_percent = item.StartDepth_40 == null ? 0 : item.StartDepth_40,
                wear_depth_50_percent = item.StartDepth_50 == null ? 0 : item.StartDepth_50,
                wear_depth_60_percent = item.StartDepth_60 == null ? 0 : item.StartDepth_60,
                wear_depth_70_percent = item.StartDepth_70 == null ? 0 : item.StartDepth_70,
                wear_depth_80_percent = item.StartDepth_80 == null ? 0 : item.StartDepth_80,
                wear_depth_90_percent = item.StartDepth_90 == null ? 0 : item.StartDepth_90,
                wear_depth_100_percent = item.StartDepth_100 == null ? 0 : item.StartDepth_100,
                wear_depth_110_percent = item.StartDepth_110 == null ? 0 : item.StartDepth_110,
                wear_depth_120_percent = item.StartDepth_120 == null ? 0 : item.StartDepth_120,
            };
            _context.TRACK_COMPART_WORN_LIMIT_ITM.Add(stepped);

        }

        private void UpdateITM(SteppedCalMethodViewModel item, TRACK_COMPART_WORN_LIMIT_ITM stepped)
        {
            stepped.start_depth_new = item.StartDepthNew == null ? 0 : item.StartDepthNew;

            stepped.wear_depth_10_percent = item.StartDepth_10 == null ? 0 : item.StartDepth_10;
            stepped.wear_depth_20_percent = item.StartDepth_20 == null ? 0 : item.StartDepth_20;
            stepped.wear_depth_30_percent = item.StartDepth_30 == null ? 0 : item.StartDepth_30;
            stepped.wear_depth_40_percent = item.StartDepth_40 == null ? 0 : item.StartDepth_40;
            stepped.wear_depth_50_percent = item.StartDepth_50 == null ? 0 : item.StartDepth_50;
            stepped.wear_depth_60_percent = item.StartDepth_60 == null ? 0 : item.StartDepth_60;
            stepped.wear_depth_70_percent = item.StartDepth_70 == null ? 0 : item.StartDepth_70;
            stepped.wear_depth_80_percent = item.StartDepth_80 == null ? 0 : item.StartDepth_80;
            stepped.wear_depth_90_percent = item.StartDepth_90 == null ? 0 : item.StartDepth_90;
            stepped.wear_depth_100_percent = item.StartDepth_100 == null ? 0 : item.StartDepth_100;
            stepped.wear_depth_110_percent = item.StartDepth_110 == null ? 0 : item.StartDepth_110;
            stepped.wear_depth_120_percent = item.StartDepth_120 == null ? 0 : item.StartDepth_120;

            stepped.track_tools_auto = item.ToolId;
            stepped.MeasurePointId = item.MeasurementPointId;

            _context.Entry(stepped).State = EntityState.Modified;



        }

        private void UpdateCalculationSteppedMethod(SteppedCalMethodViewModel item)
        {
            var checker = CheckEXTRecordExsistence(item.CompartId, item.ToolId, item.MeasurementPointId.Value);
            if (checker != null)
            {                
                checker.track_compart_worn_calc_method_auto = item.WornCalculationMethodTypeId;
                _context.Entry(checker).State = EntityState.Modified;
                var stepped = _context.TRACK_COMPART_WORN_LIMIT_ITM.FirstOrDefault(t => t.track_compart_worn_limit_itm_auto == item.WornCalculationMethodTableAutoId);
                if (stepped == null)
                    InsertNewITM(item);
                else
                    UpdateITM(item, stepped);
            }
            else
            {
                InsertNewTRACKCOMPARTExtRecord(item);
                InsertNewITM(item);
            }

        }

        private TRACK_COMPART_EXT CheckEXTRecordExsistence(int compartId,int toolId,int measurementPointId )
        {
            return _context.TRACK_COMPART_EXT.FirstOrDefault(t => t.compartid_auto == compartId && t.tools_auto == toolId && t.CompartMeasurePointId == measurementPointId);
        }

        private void InsertNewInflectionCalMethod(InflectionCalMethodViewModel item)
        {
            var newcat = new TRACK_COMPART_WORN_LIMIT_CAT
            {
                compartid_auto = item.CompartId,
                MeasurePointId = item.MeasurementPointId,
                track_tools_auto = item.ToolId,
                track_compart_worn_limit_cat_auto = item.WornCalculationMethodTypeId,

                adjust_base = item.Adjust_base == null ? 0 : item.Adjust_base,
                hi_inflectionPoint = item.Hi_InflectionPoint == null ? 0 : item.Hi_InflectionPoint,
                hi_intercept1 = item.Hi_Intercept1 == null ? 0 : item.Hi_Intercept1,
                hi_intercept2 = item.Hi_Intercept2 == null ? 0 : item.Hi_Intercept2,
                hi_slope1 = item.Hi_Slope1 == null ? 0 : item.Hi_Slope1,
                hi_slope2 = item.Hi_Slope2 == null ? 0 : item.Hi_Slope2,
                mi_inflectionPoint = item.Mi_InflectionPoint == null ? 0 : item.Mi_InflectionPoint,
                mi_intercept1 = item.Mi_Intercept1 == null ? 0 : item.Mi_Intercept1,
                mi_intercept2 = item.Mi_Intercept2 == null ? 0 : item.Mi_Intercept2,
                mi_slope1 = item.Mi_Slope1 == null ? 0 : item.Mi_Slope1,
                mi_slope2 = item.Mi_Slope2 == null ? 0 : item.Mi_Slope2,
                slope = item.Slope == null ? 0 : item.Slope,


            };
            _context.TRACK_COMPART_WORN_LIMIT_CAT.Add(newcat);


        }

        private void UpdateCalculationInflectionMethod(InflectionCalMethodViewModel item)
        {
            var checker = CheckEXTRecordExsistence(item.CompartId, item.ToolId, item.MeasurementPointId.Value);
            if (checker != null)
            {               
                checker.track_compart_worn_calc_method_auto = item.WornCalculationMethodTypeId;
                _context.Entry(checker).State = EntityState.Modified;
                var stepped = _context.TRACK_COMPART_WORN_LIMIT_CAT.FirstOrDefault(t => t.track_compart_worn_limit_cat_auto == item.WornCalculationMethodTableAutoId);
                if (stepped == null)
                    InsertNewInflectionCalMethod(item);
                else
                    UpdateExsistingInflectionCalculationMethod(item, stepped);
            }
            else
            {
                InsertNewTRACKCOMPARTExtRecord(item);
                InsertNewInflectionCalMethod(item);
            }

        }

        private void UpdateExsistingInflectionCalculationMethod(InflectionCalMethodViewModel item, TRACK_COMPART_WORN_LIMIT_CAT cat)
        {
            cat.adjust_base = item.Adjust_base == null ? 0 : item.Adjust_base;
            cat.hi_inflectionPoint = item.Hi_InflectionPoint == null ? 0 : item.Hi_InflectionPoint;
            cat.hi_intercept1 = item.Hi_Intercept1 == null ? 0 : item.Hi_Intercept1;
            cat.hi_intercept2 = item.Hi_Intercept2 == null ? 0 : item.Hi_Intercept2;
            cat.hi_slope1 = item.Hi_Slope1 == null ? 0 : item.Hi_Slope1;
            cat.hi_slope2 = item.Hi_Slope2 == null ? 0 : item.Hi_Slope2;
            cat.mi_inflectionPoint = item.Mi_InflectionPoint == null ? 0 : item.Mi_InflectionPoint;
            cat.mi_intercept1 = item.Mi_Intercept1 == null ? 0 : item.Mi_Intercept1;
            cat.mi_intercept2 = item.Mi_Intercept2 == null ? 0 : item.Mi_Intercept2;
            cat.mi_slope1 = item.Mi_Slope1 == null ? 0 : item.Mi_Slope1;
            cat.mi_slope2 = item.Mi_Slope2 == null ? 0 : item.Mi_Slope2;
            _context.Entry(cat).State = EntityState.Modified;
        }

        private void UpdatepolyCalculationMethod(PolynomialCalMethodViewModel item)
        {
            var checker = CheckEXTRecordExsistence(item.CompartId, item.ToolId, item.MeasurementPointId.Value);
            if (checker != null)
            {
                checker.track_compart_worn_calc_method_auto = item.WornCalculationMethodTypeId;              
                _context.Entry(checker).State = EntityState.Modified;
                var poly = _context.TRACK_COMPART_WORN_LIMIT_KOMATSU.FirstOrDefault(t => t.track_compart_worn_limit_komatsu_auto == item.WornCalculationMethodTableAutoId);
                if (poly == null)
                    InsertNewPolynomialCalMethod(item);
                else
                    UpdateExsistingPolynomialCalculationMethod(item, poly);
            }
            else
            {
                InsertNewTRACKCOMPARTExtRecord(item);

                InsertNewPolynomialCalMethod(item);

            }
        }

        private void InsertNewPolynomialCalMethod(PolynomialCalMethodViewModel item)
        {
            var newPoly = new TRACK_COMPART_WORN_LIMIT_KOMATSU
            {
                compartid_auto = item.CompartId,
                MeasurePointId = item.MeasurementPointId,
                track_tools_auto = item.ToolId,
                track_compart_worn_limit_komatsu_auto = item.WornCalculationMethodTypeId,

                impact_intercept = item.Impact_Intercept == null ? 0 : item.Impact_Intercept,
                impact_secondorder = item.Impact_Secondorder == null ? 0 : item.Impact_Secondorder,
                impact_slope = item.Impact_Slope == null ? 0 : item.Impact_Slope,
                normal_intercept = item.Normal_Intercept == null ? 0 : item.Normal_Intercept,
                normal_secondorder = item.Normal_Secondorder == null ? 0 : item.Normal_Secondorder,
                normal_slope = item.Normal_Slope == null ? 0 : item.Normal_Slope,
                slope_impact = item.Slope_Impact == null ? 0 : item.Slope_Impact,
                slope_normal = item.Slope_Normal == null ? 0 : item.Slope_Normal,

            };

            _context.TRACK_COMPART_WORN_LIMIT_KOMATSU.Add(newPoly);

        }

        private void UpdateExsistingPolynomialCalculationMethod(PolynomialCalMethodViewModel item, TRACK_COMPART_WORN_LIMIT_KOMATSU poly)
        {

            poly.compartid_auto = item.CompartId;
            poly.MeasurePointId = item.MeasurementPointId;
            poly.track_tools_auto = item.ToolId;
            //poly.track_compart_worn_limit_komatsu_auto = item.WornCalculationMethodTypeId;

            poly.impact_intercept = item.Impact_Intercept == null ? 0 : item.Impact_Intercept;
            poly.impact_secondorder = item.Impact_Secondorder == null ? 0 : item.Impact_Secondorder;
            poly.impact_slope = item.Impact_Slope == null ? 0 : item.Impact_Slope;
            poly.normal_intercept = item.Normal_Intercept == null ? 0 : item.Normal_Intercept;
            poly.normal_secondorder = item.Normal_Secondorder == null ? 0 : item.Normal_Secondorder;
            poly.normal_slope = item.Normal_Slope == null ? 0 : item.Normal_Slope;
            poly.slope_impact = item.Slope_Impact == null ? 0 : item.Slope_Impact;
            poly.slope_normal = item.Slope_Normal == null ? 0 : item.Slope_Normal;


            _context.Entry(poly).State = EntityState.Modified;

        }


        private void UpdateLinearCalculationMethod(LinearCalMethodViewModel item)
        {
            var checker = CheckEXTRecordExsistence(item.CompartId, item.ToolId, item.MeasurementPointId.Value);
            if (checker != null)
            {
                checker.track_compart_worn_calc_method_auto = item.WornCalculationMethodTypeId;
                _context.Entry(checker).State = EntityState.Modified;
                var linear = _context.TRACK_COMPART_WORN_LIMIT_LIEBHERR.FirstOrDefault(t => t.track_compart_worn_limit_liebherr_auto == item.WornCalculationMethodTableAutoId);
                if (linear == null)
                    InsertNewLinearCalMethod(item);
                else
                    UpdateExsistingLinearCalculationMethod(item, linear);
            }
            else
            {
                InsertNewTRACKCOMPARTExtRecord(item);
                InsertNewLinearCalMethod(item);
            }
        }

        private void InsertNewLinearCalMethod(LinearCalMethodViewModel item)
        {
            var newLinear = new TRACK_COMPART_WORN_LIMIT_LIEBHERR
            {
                compartid_auto = item.CompartId,
                MeasurePointId = item.MeasurementPointId,
                track_tools_auto = item.ToolId,
                track_compart_worn_limit_liebherr_auto = item.WornCalculationMethodTypeId,

                impact_intercept = item.Impact_Intercept == null ? 0 : item.Impact_Intercept,
                impact_slope = item.Impact_Slope == null ? 0 : item.Impact_Slope,
                normal_intercept = item.Normal_Intercept == null ? 0 : item.Normal_Intercept,
                normal_slope = item.Normal_Slope == null ? 0 : item.Normal_Slope,


            };

            _context.TRACK_COMPART_WORN_LIMIT_LIEBHERR.Add(newLinear);

        }

        private void UpdateExsistingLinearCalculationMethod(LinearCalMethodViewModel item, TRACK_COMPART_WORN_LIMIT_LIEBHERR linear)
        {
            linear.impact_intercept = item.Impact_Intercept == null ? 0 : item.Impact_Intercept;
            linear.impact_slope = item.Impact_Slope == null ? 0 : item.Impact_Slope;
            linear.normal_intercept = item.Normal_Intercept == null ? 0 : item.Normal_Intercept;
            linear.normal_slope = item.Normal_Slope == null ? 0 : item.Normal_Slope;
            _context.Entry(linear).State = EntityState.Modified;
        }

        private void UpdateJDCalculationMethod(JDCalMehtodViewModel item)
        {
           
        }

    }

    public enum MenuRepresentationMPCalculationMethodName
    {
        STEPPED = 1,//ITM
        Inflection = 2,//CAT
        Polynomial = 3,//Komatsu
        //Hitachi deleted =4
        Linear = 5,//liebherr
        JD = 6, //JD
        None = 7,//None
    }


}
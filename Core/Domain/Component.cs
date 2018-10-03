using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using BLL.Services;
using DAL;
using BLL.Persistence.Repositories;
using System.Data.Entity;
using BLL.Extensions;
using System.Threading.Tasks;
using BLL.Core.ViewModel;

namespace BLL.Core.Domain
{
    public class Component : UCSystem, IComponent
    {
        private int pvId;
        private int pvLife;
        private GENERAL_EQ_UNIT pvComponent;
        private ICompart pvCompart;
        public string Message
        {
            get; set;
        }
        public new int Id
        {
            get { return pvId; }
            set { pvId = value; }
        }
        public int ComponentLatestLife
        {
            get { return pvLife; }
            private set { pvLife = value; }
        }
        public GENERAL_EQ_UNIT DALComponent
        {
            get { return pvComponent; }
            set { pvComponent = value; }
        }
        private UndercarriageContext _context
        {
            get { return Context as UndercarriageContext; }
        }

        public ICompart Compart
        {
            get
            { return pvCompart; }

            set
            { pvCompart = value; }
        }

        public Component(IUndercarriageContext context) : base(context)
        {
            Id = 0;
            Message = "Logical component created without Id";
        }
        public Component(IUndercarriageContext context, int id) : base(context)
        {
            Id = 0;
            Message = "Logical component created with Id";
            Init(id);
        }
        public Component(IUndercarriageContext context, int id, bool InitEquipmentAsWell) : base(context, getSystemId(context, id), InitEquipmentAsWell)
        {
            Id = 0;
            Message = "Logical component created with Id";
            Init(id);
        }
        private static int getSystemId(IUndercarriageContext ctx, int sysId)
        {
            var ucContext = (UndercarriageContext)ctx;
            var sys = ucContext.LU_Module_Sub.Find(sysId);
            if (sys == null) return 0;
            return (int)(sys.Module_sub_auto);
        }
        private void Init(int id)
        {
            DALComponent = _context.GENERAL_EQ_UNIT.Find(id);
            if (DALComponent != null)
            {
                Id = id;
                Compart = new Compart(_context, DALComponent.compartid_auto);
                if (DALComponent.module_ucsub_auto != null)
                    DALSystem = _context.LU_Module_Sub.Find(DALComponent.module_ucsub_auto);
                if (DALSystem != null && DALSystem.equipmentid_auto != null)
                    DALEquipment = _context.EQUIPMENTs.Find(DALSystem.equipmentid_auto);
                if (DALEquipment != null)
                {
                    DALSystems = _context.LU_Module_Sub.Where(m => m.equipmentid_auto == DALEquipment.equipmentid_auto).ToList();
                    //DALComponents = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == DALEquipment.equipmentid_auto).ToList();
                }
                Message = "Logical component created having Id";
            }
        }
        /// <summary>
        /// Update DALComponent to the provided id and return updated DALComponent
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private DAL.GENERAL_EQ_UNIT getDALComponent(int id)
        {
            if (DALComponent != null || DALComponent.equnit_auto == Id) return DALComponent;
            Init(id);
            return DALComponent;
        }

        public DAL.LU_COMPART getCompart(int ComponentId)
        {
            var component = _context.GENERAL_EQ_UNIT.Find(ComponentId);
            if (component != null) return component.LU_COMPART;
            return null;
        }
        public DAL.LU_Module_Sub getComponentDALSystem()
        {
            if (Id == 0 || DALComponent == null)
                return null;
            if (DALSystem != null)
                return DALSystem;
            DALSystem = _context.LU_Module_Sub.Find(DALComponent.module_ucsub_auto);
            return DALSystem;
        }


        /// <summary>
        /// Returns a formatted string of the component type and description. 
        /// Also includes the shoe size and grouser if the component is a type of shoe. 
        /// 
        /// For example component with LU_COMPART_TYPE Link with LU_COMPART Optional Tall would return:
        /// "Link (Optional Tall)"
        /// </summary>
        /// <returns></returns>
        public string GetComponentDescription()
        {
            string description;
            description = pvComponent.LU_COMPART.LU_COMPART_TYPE.comparttype + " (" + pvComponent.LU_COMPART.compart + ")";
            if (pvComponent.LU_COMPART.LU_COMPART_TYPE.comparttype == "Shoe")
            {
                if (pvComponent.ShoeSizeId == null || pvComponent.ShoeGrouserNo == null)
                    return description;

                description += " : " + pvComponent.SHOE_SIZE.Title;
                description += " : " + GetGrouserType();
            }
            return description;
        }

        public string GetGrouserType()
        {
            if (pvComponent.ShoeGrouserNo != null)
                return GetGrouserType((int)pvComponent.ShoeGrouserNo);
            else
                return GetGrouserType(0);
        }

        public string GetGrouserType(int grouserId)
        {
            switch (grouserId)
            {
                case 1:
                    return "Single Grouser";
                case 2:
                    return "Double Grouser";
                case 3:
                    return "Triple Grouser";
                default:
                    return "Undefined";
            }
        }

        /// <summary>
        /// This is the dbo.fn_track_eval_code implemented using EF
        /// </summary>
        /// <param name="worn"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool GetEvalCodeByWorn(decimal worn, out char result)
        {
            if (DALComponent == null)
            {
                result = '-';
                return false;
            }
            var limits = _context.TRACK_EQ_LIMITS.Where(m => m.compartid_auto == DALComponent.compartid_auto);
            if (GetEvalCodeByLimitList(limits, worn, out result))
                return true;
            limits = _context.TRACK_EQ_LIMITS.Where(m => m.equipmentid_auto == DALComponent.equipmentid_auto);
            if (GetEvalCodeByLimitList(limits, worn, out result))
                return true;
            if (DALEquipment == null)
            {
                result = worn.toEvalChar();
                return false;
            }
            var mmta = _context.LU_MMTA.Find(DALEquipment.mmtaid_auto);
            var mmtaModeId = mmta == null ? 0 : mmta.model_auto;

            var limitModels = _context.TRACK_MODEL_LIMITS.Where(m => m.compartid_auto == DALComponent.compartid_auto && m.model_auto == mmtaModeId);
            if (GetEvalCodeByLimitList(limitModels, worn, out result))
                return true;
            limitModels = _context.TRACK_MODEL_LIMITS.Where(m => m.compartid_auto == null && m.model_auto == mmtaModeId);
            if (GetEvalCodeByLimitList(limitModels, worn, out result))
                return true;
            var dealershipLimits = _context.TRACK_DEALERSHIP_LIMITS.Where(m => m.compartid_auto == DALComponent.compartid_auto);
            if (GetEvalCodeByLimitList(dealershipLimits, worn, out result))
                return true;
            dealershipLimits = _context.TRACK_DEALERSHIP_LIMITS.Where(m => m.compartid_auto == null);
            if (GetEvalCodeByLimitList(dealershipLimits, worn, out result))
                return true;
            result = '-';
            return false;
        }

        /// <summary>
        /// Gets the total cost of a component. Includes initial setup cost and the cost of all 
        /// actions recorded against it. 
        /// </summary>
        /// <returns>Returns total cost of component</returns>
        public decimal GetComponentTotalCost()
        {
            decimal cost = 0;
            if (Id == 0)
                return cost;
            cost = GetComponentActionsCost();
            cost += pvComponent.cost == null ? 0 : (decimal)pvComponent.cost;
            return cost;
        }

        /// <summary>
        /// Gets the total cost per hour of a component. Includes initial setup cost and the
        /// cost of all actions recorded against it.
        /// </summary>
        /// <returns></returns>
        public decimal GetComponentTotalCostPerHour()
        {
            if (Id == 0)
                return 0;
            decimal cost = GetComponentTotalCost();
            int life = GetComponentLife(DateTime.Now);
            if (life > 0)
                return cost / life;
            return 0;
        }

        /// <summary>
        /// Returns the components remaining life (hours) before it is considered 100% worn. 
        /// </summary>
        /// <returns></returns>
        public decimal GetComponentRemainingLife100()
        {
            decimal componentWornPercent = GetComponentWorn(DateTime.Now);
            decimal remainingLife = 0;
            decimal lifeLivedPercent = componentWornPercent / 100;
            decimal remainingLifePercent = 1 - lifeLivedPercent;

            if (lifeLivedPercent < (decimal).3)
                remainingLife = (int)pvComponent.track_budget_life - GetComponentLife(DateTime.Now);
            else
                remainingLife = GetComponentLife(DateTime.Now) * remainingLifePercent / lifeLivedPercent;

            return remainingLife;
        }

        /// <summary>
        /// Gets the photos stored for the type of component this is. (For example, if this component is a link will return photo of a link). 
        /// </summary>
        /// <returns>A photo of the component type</returns>
        public Byte[] GetComponentPhoto()
        {
            return _context.COMPART_ATTACH_FILESTREAM.Where(c => c.comparttype_auto == pvComponent.LU_COMPART.comparttype_auto).Where(c => c.compart_attach_type_auto == 5).Select(c => c.attachment).FirstOrDefault();
        }

        /// <summary>
        /// Returns the components remaining life (hours) before it is considered 120% worn. 
        /// </summary>
        /// <returns></returns>
        public decimal GetComponentRemainingLife120()
        {
            decimal componentWornPercent = GetComponentWorn(DateTime.Now);
            decimal remainingLife = 0;
            decimal lifeLivedPercent = componentWornPercent / 100;
            decimal remainingLifePercent = (decimal)1.2 - lifeLivedPercent;

            if (lifeLivedPercent < (decimal).3)
                remainingLife = ((int)pvComponent.track_budget_life * (decimal)1.2) - GetComponentLife(DateTime.Now);
            else
                remainingLife = GetComponentLife(DateTime.Now) * remainingLifePercent / lifeLivedPercent;

            return remainingLife;
        }

        /// <summary>
        /// Returns the component actions cost
        /// </summary>
        /// <returns></returns>
        public decimal GetComponentActionsCost()
        {
            if (Id == 0)
                return 0;
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Id && m.recordStatus == 0);
            if (actions.Count() == 0)
                return 0;
            return actions.Select(m => m.cost).Sum();
        }

        /// <summary>
        /// Returns the component actions cost
        /// </summary>
        /// <returns></returns>
        public decimal GetComponentActionsCost(DateTime date)
        {
            if (Id == 0)
                return 0;
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Id && m.recordStatus == 0 && m.event_date <= date);
            if (actions.Count() == 0)
                return 0;
            return actions.Select(m => m.cost).Sum();
        }

        /// <summary>
        /// Returns the component actions cost
        /// </summary>
        /// <returns></returns>
        public decimal GetComponentActionsCost(int Id)
        {
            if (Id == 0)
                return 0;
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Id && m.recordStatus == 0);
            if (actions.Count() == 0)
                return 0;
            return actions.Select(m => m.cost).Sum();
        }

        /// <summary>
        /// Returns the cost per hour of the component based on the given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public decimal GetComponentSetupCostPerHour(DateTime date)
        {
            if (Id == 0)
                return 0;
            decimal cost = DALComponent.cost == null ? 0 : (decimal)DALComponent.cost;
            int life = GetComponentLife(date);
            if (life > 0)
                return cost / life;
            return 0;
        }
        public decimal GetComponentWorn(DateTime date)
        {
            if (Id == 0)
                return -1;
            var details = _context.TRACK_INSPECTION_DETAIL.Where(m => m.track_unit_auto == Id && m.TRACK_INSPECTION.inspection_date <= date);
            if (details.Count() == 0)
                return 0;
            var inspections = details.Select(m => m.TRACK_INSPECTION).OrderByDescending(m => m.inspection_date);
            if (inspections.Count() == 0)
                return 0;
            var selectedTID = inspections.First().TRACK_INSPECTION_DETAIL.Where(m => m.track_unit_auto == Id);
            if (selectedTID.Count() == 0)
                return 0;
            return selectedTID.First().worn_percentage;
        }

        public decimal GetComponentWorn(DateTime date, int id)
        {
            if (getDALComponent(id) == null)
                return -1;
            var details = _context.TRACK_INSPECTION_DETAIL.Where(m => m.track_unit_auto == Id && m.TRACK_INSPECTION.inspection_date <= date);
            if (details.Count() == 0)
                return 0;
            var inspections = details.Select(m => m.TRACK_INSPECTION).OrderByDescending(m => m.inspection_date);
            if (inspections.Count() == 0)
                return 0;
            var selectedTID = inspections.First().TRACK_INSPECTION_DETAIL.Where(m => m.track_unit_auto == Id);
            if (selectedTID.Count() == 0)
                return 0;
            return selectedTID.First().worn_percentage;
        }

        private bool GetEvalCodeByLimitList(IEnumerable<TRACK_DEALERSHIP_LIMITS> limits, decimal worn, out char result)
        {
            if (limits != null && limits.Count() > 0 && limits.First().a_limit != null && limits.First().b_limit != null && limits.First().c_limit != null)
            {
                if (worn <= limits.First().a_limit)
                {
                    result = 'A';
                    return true;
                }
                if (worn <= limits.First().b_limit)
                {
                    result = 'B';
                    return true;
                }
                if (worn <= limits.First().c_limit)
                {
                    result = 'C';
                    return true;
                }
                result = 'X';
                return true;
            }
            result = '-';
            return false;
        }

        private bool GetEvalCodeByLimitList(IEnumerable<TRACK_EQ_LIMITS> limits, decimal worn, out char result)
        {
            if (limits != null && limits.Count() > 0 && limits.First().a_limit != null && limits.First().b_limit != null && limits.First().c_limit != null)
            {
                if (worn <= limits.First().a_limit)
                {
                    result = 'A';
                    return true;
                }
                if (worn <= limits.First().b_limit)
                {
                    result = 'B';
                    return true;
                }
                if (worn <= limits.First().c_limit)
                {
                    result = 'C';
                    return true;
                }
                result = 'X';
                return true;
            }
            result = ' ';
            return false;
        }
        private bool GetEvalCodeByLimitList(IEnumerable<TRACK_MODEL_LIMITS> limits, decimal worn, out char result)
        {
            if (limits != null && limits.Count() > 0 && limits.First().a_limit != null && limits.First().b_limit != null && limits.First().c_limit != null)
            {
                if (worn <= limits.First().a_limit)
                {
                    result = 'A';
                    return true;
                }
                if (worn <= limits.First().b_limit)
                {
                    result = 'B';
                    return true;
                }
                if (worn <= limits.First().c_limit)
                {
                    result = 'C';
                    return true;
                }
                result = 'X';
                return true;
            }
            result = ' ';
            return false;
        }

        /// <summary>
        /// To be used when storing a new action which would include altering a components life. 
        /// This will also retrieve component life records which are in the process of saving but have not been
        /// committed yet. 
        /// </summary>
        /// <param name="date">The date we want to get the life for. </param>
        /// <returns>Component life at the given date</returns>
        public int GetComponentLifeMiddleOfNewAction(DateTime date)
        {
            if (Id == 0)
                return -1;
            var Comp = _context.GENERAL_EQ_UNIT.Find(Id);
            if (Comp == null)
                return -1;
            var lifes = Comp.Life.Where(m => m.ActionDate <= date
                    && (m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.MiddleOfAction
                        || m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.Available))
                    .OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return (int)(Comp.cmu ?? 0); //GetComponentLifeOldMethod(Id, date);
        }

        public int GetComponentLife(DateTime date)
        {
            if (Id == 0)
                return -1;
            var Comp = _context.GENERAL_EQ_UNIT.Find(Id);
            if (Comp == null)
                return -1;
            var lifes = Comp.Life.Where(m => m.ActionDate <= date && m.ACTION_TAKEN_HISTORY.recordStatus == 0).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return (int)(Comp.cmu ?? 0); //GetComponentLifeOldMethod(Id, date);
        }

        public decimal CalcWornPercentage(decimal reading, int toolId, InspectionImpact? impact)
        {
            if (reading == 0) //TT-520 in comments 
                return (decimal)-0.0001;
            if (DALComponent == null)
                return (decimal)-0.0002;
            var tcx = _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == DALComponent.compartid_auto && m.tools_auto == toolId && (m.CompartMeasurePointId == null || m.CompartMeasurePointId == 0));
            if (tcx == null || tcx.Count() == 0)
                return (decimal)-0.0003;
            WornCalculationMethod method;
            try { method = (WornCalculationMethod)tcx.First().track_compart_worn_calc_method_auto; } catch { method = WornCalculationMethod.None; };
            switch (method)
            {
                case WornCalculationMethod.ITM: //ITM
                    var kITM = _context.TRACK_COMPART_WORN_LIMIT_ITM.Where(m => m.compartid_auto == DALComponent.compartid_auto && m.track_tools_auto == toolId && (m.MeasurePointId == null || m.MeasurePointId == 0));
                    if (kITM.Count() > 0)
                    {
                        var newReading = reading.InchToMilimeter();
                        var k = WornCalculationExtension.ITMReadingMapper(kITM.First(), newReading); // Fix for ITM limits not calulating properly. 
                        var returnValue = k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                        return returnValue;
                    }
                    break;
                case WornCalculationMethod.CAT: //CAT
                    var kCAT = _context.TRACK_COMPART_WORN_LIMIT_CAT.Where(m => m.compartid_auto == DALComponent.compartid_auto && m.track_tools_auto == toolId && (m.MeasurePointId == null || m.MeasurePointId == 0));
                    if (kCAT.Count() > 0)
                    {
                        var k = WornCalculationExtension.CATReadingMapper(kCAT.First(), reading, impact);
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
                case WornCalculationMethod.Komatsu: //Komatsu
                    var kKomatsu = _context.TRACK_COMPART_WORN_LIMIT_KOMATSU.Where(m => m.compartid_auto == DALComponent.compartid_auto && m.track_tools_auto == toolId && (m.MeasurePointId == null || m.MeasurePointId == 0));
                    if (kKomatsu.Count() > 0)
                    {
                        var k = WornCalculationExtension.KomatsuReadingMapper(kKomatsu.First(), reading, impact);
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
                case WornCalculationMethod.Hitachi: //Hitachi
                    var kHitach = _context.TRACK_COMPART_WORN_LIMIT_HITACHI.Where(m => m.compartid_auto == DALComponent.compartid_auto && m.track_tools_auto == toolId && (m.MeasurePointId == null || m.MeasurePointId == 0));
                    if (kHitach.Count() > 0)
                    {
                        var k = WornCalculationExtension.HitachiReadingMapper(kHitach.First(), reading, impact);
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
                case WornCalculationMethod.Liebherr: //Liebherr
                    var kLiebherr = _context.TRACK_COMPART_WORN_LIMIT_LIEBHERR.Where(m => m.compartid_auto == DALComponent.compartid_auto && m.track_tools_auto == toolId && (m.MeasurePointId == null || m.MeasurePointId == 0));
                    if (kLiebherr.Count() > 0)
                    {
                        var k = WornCalculationExtension.LiebherrReadingMapper(kLiebherr.First(), reading, impact);
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
            }
            return (decimal)-0.0004;//Method not found
        }

        internal decimal GetComponentHistoryCost(DateTime date, int id, int equipmentId)
        {
            var _costExclusions = new int[] { (int)ActionType.ReplaceSystemFromInventory }.ToList();
            return GetComponentHistoryOnEquipmentQuery(date, id, equipmentId).Query.Where(m=> !_costExclusions.Any(k=> m.ActionTypeId == k)).Select(m=> m.cost).Sum();
        }

        private decimal LiebherrReadingMapper(TRACK_COMPART_WORN_LIMIT_LIEBHERR r, decimal reading, InspectionImpact? i)
        {
            if (i == null)
                return (decimal)-0.0009;
            InspectionImpact impact = (InspectionImpact)i;
            decimal? A;
            decimal? B;
            if (i == InspectionImpact.High)
            {
                A = r.impact_slope;
                B = r.impact_intercept;
            }
            else
            {
                A = r.normal_slope;
                B = r.normal_intercept;
            }
            return firstOrder(A, B, reading);
        }

        private decimal HitachiReadingMapper(TRACK_COMPART_WORN_LIMIT_HITACHI r, decimal reading, InspectionImpact? i)
        {
            if (i == null)
                return (decimal)-0.0008;
            InspectionImpact impact = (InspectionImpact)i;
            decimal? A;
            decimal? B;
            if (i == InspectionImpact.High)
            {
                A = r.impact_slope;
                B = r.impact_intercept;
            }
            else
            {
                A = r.normal_slope;
                B = r.normal_intercept;
            }
            return firstOrder(A, B, reading);
        }

        private decimal KomatsuReadingMapper(TRACK_COMPART_WORN_LIMIT_KOMATSU r, decimal reading, InspectionImpact? i)
        {
            if (i == null)
                return (decimal)-0.0007;
            InspectionImpact impact = (InspectionImpact)i;
            decimal? A;
            decimal? B;
            decimal? C;
            if (i == InspectionImpact.High)
            {
                A = r.impact_secondorder;
                B = r.impact_slope;
                C = r.impact_intercept;
            }
            else
            {
                A = r.normal_secondorder;
                B = r.normal_slope;
                C = r.normal_intercept;
            }
            return secondOrder(A, B, C, reading);
        }

        private decimal linearFormula(decimal? Ax, decimal? Ay, decimal? Bx, decimal? By, decimal reading)
        {
            decimal ax = Ax == null ? 0 : (decimal)Ax;
            decimal bx = Bx == null ? 0 : (decimal)Bx;
            decimal ay = Ay == null ? 0 : (decimal)Ay;
            decimal by = By == null ? 0 : (decimal)By;

            var m = ((by - ay) / (bx - ax));
            var c = (ay - (ax * m));
            return Math.Round((m * reading) + c, 3);
        }
        private decimal secondOrder(decimal? A, decimal? B, decimal? C, decimal reading)
        {
            decimal a = A == null ? 0 : (decimal)A;
            decimal b = B == null ? 0 : (decimal)B;
            decimal c = C == null ? 0 : (decimal)C;

            decimal k = (decimal)Math.Pow((double)reading, 2);

            return Math.Round((a * k) + (b * reading) + c, 3);
        }
        private decimal firstOrder(decimal? A, decimal? B, decimal reading)
        {
            decimal a = A == null ? 0 : (decimal)A;
            decimal b = B == null ? 0 : (decimal)B;

            return Math.Round((a * reading) + b, 3);
        }

        private decimal ITMReadingMapper(TRACK_COMPART_WORN_LIMIT_ITM r, decimal reading)
        {
            if (r.start_depth_new > r.wear_depth_100_percent)
            {
                if (reading <= r.start_depth_new && reading > r.wear_depth_10_percent)
                    return linearFormula(r.start_depth_new, 0, r.wear_depth_10_percent, 10, reading);
                if (reading <= r.wear_depth_10_percent && reading > r.wear_depth_20_percent)
                    return linearFormula(r.wear_depth_10_percent, 10, r.wear_depth_20_percent, 20, reading);
                if (reading <= r.wear_depth_20_percent && reading > r.wear_depth_30_percent)
                    return linearFormula(r.wear_depth_20_percent, 20, r.wear_depth_30_percent, 30, reading);
                if (reading <= r.wear_depth_30_percent && reading > r.wear_depth_40_percent)
                    return linearFormula(r.wear_depth_30_percent, 30, r.wear_depth_40_percent, 40, reading);
                if (reading <= r.wear_depth_40_percent && reading > r.wear_depth_50_percent)
                    return linearFormula(r.wear_depth_40_percent, 40, r.wear_depth_50_percent, 50, reading);
                if (reading <= r.wear_depth_50_percent && reading > r.wear_depth_60_percent)
                    return linearFormula(r.wear_depth_50_percent, 50, r.wear_depth_60_percent, 60, reading);
                if (reading <= r.wear_depth_60_percent && reading > r.wear_depth_70_percent)
                    return linearFormula(r.wear_depth_60_percent, 60, r.wear_depth_70_percent, 70, reading);
                if (reading <= r.wear_depth_70_percent && reading > r.wear_depth_80_percent)
                    return linearFormula(r.wear_depth_70_percent, 70, r.wear_depth_80_percent, 80, reading);
                if (reading <= r.wear_depth_80_percent && reading > r.wear_depth_90_percent)
                    return linearFormula(r.wear_depth_80_percent, 80, r.wear_depth_90_percent, 90, reading);
                if (reading <= r.wear_depth_90_percent && reading > r.wear_depth_100_percent)
                    return linearFormula(r.wear_depth_90_percent, 90, r.wear_depth_100_percent, 100, reading);
                if (reading <= r.wear_depth_100_percent && reading > r.wear_depth_110_percent)
                    return linearFormula(r.wear_depth_100_percent, 100, r.wear_depth_110_percent, 110, reading);
                if (reading <= r.wear_depth_110_percent && reading > r.wear_depth_120_percent)
                    return linearFormula(r.wear_depth_110_percent, 110, r.wear_depth_120_percent, 120, reading);
                return (decimal)-0.0005;
            }
            else
            {
                if (reading >= r.start_depth_new && reading < r.wear_depth_10_percent)
                    return linearFormula(r.start_depth_new, 0, r.wear_depth_10_percent, 10, reading);
                if (reading >= r.wear_depth_10_percent && reading < r.wear_depth_20_percent)
                    return linearFormula(r.wear_depth_10_percent, 10, r.wear_depth_20_percent, 20, reading);
                if (reading >= r.wear_depth_20_percent && reading < r.wear_depth_30_percent)
                    return linearFormula(r.wear_depth_20_percent, 20, r.wear_depth_30_percent, 30, reading);
                if (reading >= r.wear_depth_30_percent && reading < r.wear_depth_40_percent)
                    return linearFormula(r.wear_depth_30_percent, 30, r.wear_depth_40_percent, 40, reading);
                if (reading >= r.wear_depth_40_percent && reading < r.wear_depth_50_percent)
                    return linearFormula(r.wear_depth_40_percent, 40, r.wear_depth_50_percent, 50, reading);
                if (reading >= r.wear_depth_50_percent && reading < r.wear_depth_60_percent)
                    return linearFormula(r.wear_depth_50_percent, 50, r.wear_depth_60_percent, 60, reading);
                if (reading >= r.wear_depth_60_percent && reading < r.wear_depth_70_percent)
                    return linearFormula(r.wear_depth_60_percent, 60, r.wear_depth_70_percent, 70, reading);
                if (reading >= r.wear_depth_70_percent && reading < r.wear_depth_80_percent)
                    return linearFormula(r.wear_depth_70_percent, 70, r.wear_depth_80_percent, 80, reading);
                if (reading >= r.wear_depth_80_percent && reading < r.wear_depth_90_percent)
                    return linearFormula(r.wear_depth_80_percent, 80, r.wear_depth_90_percent, 90, reading);
                if (reading >= r.wear_depth_90_percent && reading < r.wear_depth_100_percent)
                    return linearFormula(r.wear_depth_90_percent, 90, r.wear_depth_100_percent, 100, reading);
                if (reading >= r.wear_depth_100_percent && reading < r.wear_depth_110_percent)
                    return linearFormula(r.wear_depth_100_percent, 100, r.wear_depth_110_percent, 110, reading);
                if (reading >= r.wear_depth_110_percent && reading < r.wear_depth_120_percent)
                    return linearFormula(r.wear_depth_110_percent, 110, r.wear_depth_120_percent, 120, reading);
                return (decimal)-0.0005;
            }
        }
        private decimal CATReadingMapper(TRACK_COMPART_WORN_LIMIT_CAT r, decimal reading, InspectionImpact? i)
        {
            if (i == null)
                return (decimal)-0.0006;
            InspectionImpact impact = (InspectionImpact)i;
            decimal? slope;
            decimal? intercept;
            if (impact == InspectionImpact.High)
            {
                if (r.slope == 0)
                {
                    if (reading >= r.hi_inflectionPoint)
                    {
                        slope = r.hi_slope1;
                        intercept = r.hi_intercept1;
                    }
                    else
                    {
                        slope = r.hi_slope2;
                        intercept = r.hi_intercept2;
                    }
                }
                else
                {
                    if (reading >= r.hi_inflectionPoint)
                    {
                        slope = r.hi_slope2;
                        intercept = r.hi_intercept2;
                    }
                    else
                    {
                        slope = r.hi_slope1;
                        intercept = r.hi_intercept1;
                    }
                }
            }
            else
            {
                if (r.slope == 0)
                {
                    if (reading >= r.mi_inflectionPoint)
                    {
                        slope = r.mi_slope1;
                        intercept = r.mi_intercept1;
                    }
                    else
                    {
                        slope = r.mi_slope2;
                        intercept = r.mi_intercept2;
                    }
                }
                else
                {
                    if (reading >= r.mi_inflectionPoint)
                    {
                        slope = r.mi_slope2;
                        intercept = r.mi_intercept2;
                    }
                    else
                    {
                        slope = r.mi_slope1;
                        intercept = r.mi_intercept1;
                    }
                }
            }

            if (slope == null || intercept == null)
                return (decimal)-0.00069;
            return Math.Round(((decimal)slope * reading) + ((decimal)intercept), 3);
        }

        public ResultMessage UpdateComponentSetupDetails(UpdateComponentInstallationDetailParams Param)
        {
            var result = new ResultMessage
            {
                Id = 0,
                ActionLog = "Start of UpdateComponentSetupDetails in BLL.Component",
                LastMessage = "Start of UpdateComponentSetupDetails",
                OperationSucceed = false
            };
            if (Param == null)
            {
                Message = "Parameter cannot be null !";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            if (Id == 0 || DALComponent == null)
            {
                Message = "Component to be updated not found";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            DALSystem = _context.LU_Module_Sub.Find(DALComponent.module_ucsub_auto);
            if (DALSystem == null || DALSystem.equipmentid_auto == null)
            {
                Message = "Component is not on a system";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            //For the frames in the page, serial number of the system is not available and I send this string to identify that
            if (Param.SystemSerialNumber == "INVALID_SERIAL_NO_CHANGE")
            {
                Param.SystemSerialNumber = DALSystem.Serialno;
            }
            if (DALSystem.equipmentid_auto != DALComponent.equipmentid_auto)
            {
                Message = "Component is on a system which is not attached to this equipment";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            DALEquipment = _context.EQUIPMENTs.Find(DALSystem.equipmentid_auto);
            if (DALEquipment == null)
            {
                Message = "Components are not on an equipment cannot get updated";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            //If there is no date for purchase date and created date how can we validate user inputs??
            //How some people decided to make both of these to be nullable on the design time??
            //Who knows who did it in more than 10 years ago!!?? :| 
            //Don't remove this validation otherwise in the next part exceptions might occur
            if (DALEquipment.purchase_date == null && DALEquipment.created_date == null)
            {
                Message = "Equipment setup date is not valid!";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            var inspections = _context.ACTION_TAKEN_HISTORY.Where(m => (m.action_type_auto == (int)ActionType.InsertInspection || m.action_type_auto == (int)ActionType.UpdateInspection) && m.equipmentid_auto == DALEquipment.equipmentid_auto && m.recordStatus == 0);
            bool FirstInspectionExist = inspections.Count() > 0 ? true : false;

            DateTime EquipmentSetupDate = (DateTime)(DALEquipment.purchase_date == null ? DALEquipment.created_date : DALEquipment.purchase_date);

            if (EquipmentSetupDate > Param.InstalledDate || (FirstInspectionExist && inspections.OrderBy(m => m.entry_date).First().entry_date < Param.InstalledDate))
            {
                Message = "Installed date is invaid! between " + DALEquipment.purchase_date.Value.ToString("dd-MMM-yyyy") + " and " + inspections.OrderBy(m => m.entry_date).First().entry_date.ToString("dd-MMM-yyyy") + " is accepted.";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }

            if (inspections.Count() > 1)
            {
                Message = "You cannot change setup details if there is more than one inspection recorded";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }

            int EquipmentSMUatSetup = longNullableToint(DALEquipment.smu_at_start);
            if (Param.InstalledDate < EquipmentSetupDate && Param.SMUatInstallation > EquipmentSMUatSetup)
            {
                Message = "SMU should not be more than equipment smu (" + EquipmentSMUatSetup + ") while date is before than " + EquipmentSetupDate.ToShortDateString();
                return result;
            }
            if (Param.InstalledDate > EquipmentSetupDate && Param.SMUatInstallation < EquipmentSMUatSetup)
            {
                Message = "SMU should not be less than equipment smu (" + EquipmentSMUatSetup + ") while date is after than " + EquipmentSetupDate.ToShortDateString();
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            if (DALSystem.Serialno != Param.SystemSerialNumber && _context.LU_Module_Sub.Where(m => m.Serialno == Param.SystemSerialNumber.Trim()).Count() > 0)
            {
                Message = "Serial number you enterd is already taken!";
                return result;
            }
            if (_context.ACTION_TAKEN_HISTORY.Where(m => m.action_type_auto != (int)ActionType.InstallSystemOnEquipment && m.action_type_auto != (int)ActionType.InstallComponentOnSystemOnEquipment && m.action_type_auto != (int)ActionType.InsertInspection && m.action_type_auto != (int)ActionType.UpdateInspection && m.equipmentid_auto == DALEquipment.equipmentid_auto && m.recordStatus == 0).Count() > 1)
            {
                Message = "At least one action to this equipment has been recorded and operation is not allowed";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            if (DALSystem.Serialno != Param.SystemSerialNumber)
            {
                DALSystem.Serialno = Param.SystemSerialNumber;
                _context.Entry(DALSystem).State = EntityState.Modified;
            }
            BLL.Core.Domain.Compart newLogicalCompart = new Compart(_context, Param.CompartId);
            if (Param.CompartId != Compart.Id && newLogicalCompart.Id == 0)
            {
                Message = "Part no. is not valid!";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            var actionUser = _context.USER_TABLE.Find(Param.UserId);
            if (actionUser == null)
            {
                Message = "Something is wrong and user cannot be found!";
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            int changedLife = Param.ComponentLifeAtInstallation - longNullableToint(DALComponent.cmu) - (Param.SMUatInstallation - DALComponent.eq_smu_at_install.LongNullableToInt());
            result.ActionLog += "Start setting previous actions as modified!";
            var setupActions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Id && m.recordStatus == (int)RecordStatus.Available && m.action_type_auto == (int)ActionType.InstallComponentOnSystemOnEquipment);
            foreach (var act in setupActions)
            {
                act.recordStatus = (int)RecordStatus.Modified;
                _context.Entry(act).State = EntityState.Modified;
            }
            try
            {
                _context.SaveChanges();
                result.ActionLog += "Previous actions set as modified!";
            }
            catch (Exception e)
            {
                Message = e.Message;
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            using (BLL.Core.Domain.Action compAction =
                new BLL.Core.Domain.Action(
                    new DAL.UndercarriageContext(),
                    new EquipmentActionRecord
                    {
                        ActionDate = Param.InstalledDate,
                        ActionUser = new User
                        {
                            Id = actionUser.user_auto.LongNullableToInt(),
                            userName = actionUser.username,
                            userStrId = actionUser.userid
                        },
                        Comment = "Setup Component Updated!",
                        Cost = 0,
                        EquipmentId = DALComponent.equipmentid_auto.LongNullableToInt(),
                        ReadSmuNumber = Param.SMUatInstallation,
                        TypeOfAction = ActionType.InstallComponentOnSystemOnEquipment
                    },
                    new BLL.Core.Domain.InstallComponentOnSystemParams
                    {
                        Id = Id,
                        Position = DALComponent.pos == null ? (byte)0 : (byte)DALComponent.pos,
                        SystemId = DALComponent.module_ucsub_auto.LongNullableToInt(),
                        side = (Side)DALComponent.side
                    }))
            {

                compAction.Operation.Start();
                compAction.Operation.Validate();
                compAction.Operation.Commit();
                result.LastMessage = compAction.Operation.Message;
                result.ActionLog += compAction.Operation.ActionLog;
                result.OperationSucceed = compAction.Operation.Status == ActionStatus.Succeed;
            }
            if (!result.OperationSucceed)
            {
                foreach (var act in setupActions)
                {
                    act.recordStatus = (int)RecordStatus.Available;
                    _context.Entry(act).State = EntityState.Modified;
                }
                try
                {
                    _context.SaveChanges();
                    result.ActionLog += "Previous actions set back as available!";
                }
                catch (Exception e)
                {
                    Message = e.Message;
                    result.ActionLog += Message;
                    result.LastMessage = Message;
                    return result;
                }
                return result;
            }



            DALComponent.compartid_auto = newLogicalCompart.Id;
            DALComponent.compartsn = newLogicalCompart.GetCompartSerialNumber();
            DALComponent.date_installed = Param.InstalledDate;
            DALComponent.smu_at_install = Param.SMUatInstallation;
            DALComponent.eq_ltd_at_install = Param.LTDatInstallation;
            DALComponent.cmu = Param.ComponentLifeAtInstallation;
            DALComponent.cost = Param.ComponentCost;
            DALComponent.track_budget_life = Param.BudgetLife;
            try
            {
                _context.SaveChanges();
                Message = "Operation succeded!";
            }
            catch (Exception e)
            {
                Message = e.Message;
                result.ActionLog += Message;
                result.LastMessage = Message;
                return result;
            }
            //After become sure that data is updated sucessfully all inspection details of the component everywhere will be updated
            updateInspectionsBySetupChanges(changedLife, Param.InstalledDate);
            result.ActionLog += Message;
            return result;
        }

        //This componentIstallDate parameter should be validated before coming here otherwise all the lifes will be incorrect :O !!!!!
        private bool updateInspectionsBySetupChanges(int changedLife, DateTime componentInstallDate)
        {
            foreach (var k in DALComponent.Life.Where(m => m.ActionDate >= componentInstallDate && m.ACTION_TAKEN_HISTORY.recordStatus == (int)RecordStatus.Available))
            {
                if (changedLife > 0 || (changedLife < 0 && k.ActualLife > changedLife))
                {
                    k.ActualLife += changedLife;
                    _context.Entry(k).CurrentValues.SetValues(k);
                }
            }
            var tidsAfter = DALComponent.TRACK_INSPECTION_DETAIL.Where(m => m.TRACK_INSPECTION.inspection_date >= componentInstallDate);
            foreach (var k in tidsAfter)
            {
                if (changedLife > 0 || (changedLife < 0 && k.hours_on_surface > changedLife))
                {
                    k.hours_on_surface += changedLife;
                    k.worn_percentage = new Component(_context, k.track_unit_auto.LongNullableToInt()).CalcWornPercentage(k.reading.ConvertMMToInch(), k.tool_auto == null ? 0 : (int)k.tool_auto, (InspectionImpact)k.TRACK_INSPECTION.impact);
                    _context.Entry(k).CurrentValues.SetValues(k);
                }
            }
            try
            {
                _context.SaveChanges();
                Message = "Operation succeded!";
                return true;
            }
            catch (Exception e)
            {
                Message = "Warning : Operation succeeded uncompletely! " + e.Message;
                return false;
            }
        }


        /// <summary>
        /// This recursive method returns the history of a componenId while is installed on equipment. Because in many cases component is replaced with a new one and component Id is different then this method returns those replaced one as well.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="systemId"></param>
        /// <param name="compartTypeId"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public ComponentHistoryQueryViewModel GetComponentHistoryOnEquipmentQuery(DateTime date, int componentId, int equipmentId = 0)
        {
            var _component = _context.GENERAL_EQ_UNIT.Find(componentId);
            if (_component == null) return new ComponentHistoryQueryViewModel { Query = new List<ComponentHistoryOldViewModel>().AsQueryable(), ComponentIds = new List<Tuple<int, int, DateTime>>() } ;
            var _logicalComponent = new Component(_context, (int)_component.equnit_auto);
            bool isAchild = _logicalComponent.isAChildBasedOnCompart();
            int _compartId = _component.compartid_auto;
            int _childNo = 0;
            if (isAchild)
            {
                var _parentCompartId = _context.COMPART_PARENT_RELATION.Where(m => m.ChildCompartId == _compartId).Select(m => m.ParentCompartId).FirstOrDefault() ?? 0;
                _childNo = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == _parentCompartId).Select(m => m.ChildCompartId).ToList().IndexOf(_compartId);
            }
            int _equipmentId = (int)(_component.equipmentid_auto ?? equipmentId);
            int _systemId = (int)(_component.module_ucsub_auto ?? 0);
            var _systemIdsTuple = getSystemIdsReplacement(new List<Tuple<int,int,DateTime>>(), _equipmentId, _systemId, date);
            var _compsReplaced = getComponentIdsReplacement(new List<Tuple<int, int, DateTime>>(), _equipmentId, componentId, date);
            var _compsForSysReplaced = getComponentIdsOnSystems(_systemIdsTuple, _equipmentId, _component.LU_COMPART.comparttype_auto, (_component.pos ?? 0), date, isAchild, _childNo);
            var _componentIdsTuple = _compsReplaced.Concat(_compsForSysReplaced);
            var _componentIds = _componentIdsTuple.Select(m => m.Item2).Concat(new int[] { componentId });
            var _systemIds = _systemIdsTuple.Select(m => m.Item2).Concat(new int[] { _systemId });
            var eqTrackingActions = new int[]{
                (int)ActionType.InsertInspection,
                (int)ActionType.UpdateInspection,
                (int)ActionType.InsertInspectionGeneral,
                (int)ActionType.UpdateInspectionGeneral,
                (int)ActionType.SMUReadingAction,
                (int)ActionType.ChangeMeterUnit,
                (int)ActionType.GETAction
            };

            var sysTrackingActions = new int[] {
                (int)ActionType.InstallSystemOnEquipment,
                (int)ActionType.ReplaceSystemFromInventory
            };

            var compTrackingActions = new int[] {
                (int)ActionType.Replace,
                (int)ActionType.Weld,
                (int)ActionType.TurnPinsAndBushingsLink,
                (int)ActionType.RepairDryJointsLink,
                (int)ActionType.AdjustTensionLink,
                (int)ActionType.RepairDryJointsBush,
                (int)ActionType.TurnPinsAndBushingsBush,
                (int)ActionType.AdjustTensionBush,
                (int)ActionType.Regrouser,
                (int)ActionType.SwapFrontRear,
                (int)ActionType.Reshell,
                (int)ActionType.ReplaceComponentWithNew,
                (int)ActionType.InstallComponentOnSystemOnEquipment
            };

            return new ComponentHistoryQueryViewModel
            {
                Query = _context.ACTION_TAKEN_HISTORY.Where(m =>
            m.recordStatus == (int)RecordStatus.Available && m.event_date <= date && m.equipmentid_auto == _equipmentId &&
            (eqTrackingActions.Any(k => m.action_type_auto == k) ||
            (sysTrackingActions.Any(k => m.action_type_auto == k) && _systemIds.Any(p => m.system_auto_id == p)) ||
            (compTrackingActions.Any(k => m.action_type_auto == k) && _componentIds.Any(p => m.equnit_auto == p)))
            ).OrderByDescending(m => m.event_date).Select(_record => new ComponentHistoryOldViewModel
            {
                Actiondate = _record.event_date,
                ActionTakenId = _record.history_id,
                ActionTypeId = (_record.action_type_auto),
                ActionType = ActionType.NoActionTakenYet,
                action_description = _record.TRACK_ACTION_TYPE.action_description,
                cmu = 0,
                comment = _record.comment,
                compartTypeId = 0,
                ComponentId = _record.equnit_auto != null ? (int)_record.equnit_auto : (int)_component.equnit_auto,
                cost = _record.cost,
                equipmentSMU = _record.equipment_smu,
                eval = "",
                event_date = "",
                isBushingTurned = false,
                isChildComponent = false,
                makeId = 0,
                makeSymbol = "",
                projectedHours = 0,
                RelatedLinkUrl = "",
                side = 0,
                type = "",
                worn = 0,
                InspectionId = _record.TRACK_INSPECTION.FirstOrDefault() != null ? _record.TRACK_INSPECTION.FirstOrDefault().inspection_auto : 0
            }),
                ComponentIds = _componentIdsTuple.GroupBy(m=> m.Item1).Select(m=> m.FirstOrDefault()).ToList()
            };
        }

        public List<ComponentHistoryOldViewModel> GetComponentHistoryOnEquipment(ComponentHistoryQueryViewModel query, int componentId)
        {
            var result = new List<ComponentHistoryOldViewModel>();
            var _component = _context.GENERAL_EQ_UNIT.Find(componentId);

            if (_component == null) return result;
            var _history = query.Query.ToList();
            var _logicalComponent = new Component(_context, (int)_component.equnit_auto);
            foreach (var _record in _history)
            {
                try{ _record.ActionType = (ActionType)_record.ActionTypeId;}catch{_record.ActionType = ActionType.OldActionType;}
                _record.event_date = _record.Actiondate.ToString("dd-MMM-yyyy");
                _record.side = (Side)(_component.side ?? 0);
                _record.type = _record.ActionType.Label();
                int _compId = (int)_component.equnit_auto;
                var _current = query.ComponentIds.Where(m => _record.Actiondate < m.Item3).OrderByDescending(m=> m.Item3).FirstOrDefault();
                if (_current != null)
                    _compId = _current.Item2;

                if (_compId == _component.equnit_auto) //Means still on this equipment
                {
                    char eval = ' ';
                    _logicalComponent.GetEvalCodeByWorn(_logicalComponent.GetComponentWorn(_record.Actiondate), out eval);
                    var _make = _logicalComponent.getMake(_record.ComponentId);

                    _record.cmu = _logicalComponent.GetComponentLife(_record.Actiondate);
                    _record.compartTypeId = _logicalComponent.DALComponent.LU_COMPART.comparttype_auto;
                    _record.RelatedLinkUrl = "./TrackDetails.aspx?inspec_auto=" + _record.InspectionId;
                    _record.eval = eval.ToString();
                    _record.isBushingTurned = _logicalComponent.isBushingTurned(_record.Actiondate);
                    _record.isChildComponent = _logicalComponent.isAChildBasedOnCompart();
                    _record.makeId = _make.Id;
                    _record.makeSymbol = _make.Symbol;
                    _record.projectedHours = _logicalComponent.GetProjectedHours(_record.Actiondate);
                    _record.worn = _logicalComponent.GetComponentWorn(_record.Actiondate);
                    result.Add(_record);
                }
                else //Means not on this equipment anymore
                {
                    char eval = ' ';
                    var _logicalReplacedComponent = new Component(_context, _compId);
                    _logicalReplacedComponent.GetEvalCodeByWorn(_logicalReplacedComponent.GetComponentWorn(_record.Actiondate), out eval);
                    var _make = _logicalReplacedComponent.getMake(_compId);
                    _record.cmu = _logicalReplacedComponent.GetComponentLife(_record.Actiondate);
                    _record.compartTypeId = _logicalReplacedComponent.getDALComponent(_compId) != null ? _logicalReplacedComponent.getDALComponent(_compId).LU_COMPART.comparttype_auto : 0;
                    _record.RelatedLinkUrl = "./TrackDetails.aspx?inspec_auto=" + _record.InspectionId;
                    _record.eval = eval.ToString();
                    _record.isBushingTurned = _logicalReplacedComponent.isBushingTurned(_record.Actiondate);
                    _record.isChildComponent = _logicalReplacedComponent.isAChildBasedOnCompart();
                    _record.makeId = _make.Id;
                    _record.makeSymbol = _make.Symbol;
                    _record.projectedHours = _logicalReplacedComponent.GetProjectedHours(_record.Actiondate);
                    _record.worn = _logicalReplacedComponent.GetComponentWorn(_record.Actiondate);
                    result.Add(_record);
                }
            }
            return result;
        }


        /// <summary>
        /// Returns a list of systems ids which was replaced on an equipment. This is a recursive method
        /// </summary>
        /// <param name="systemId">new system Id</param>
        /// <param name="equipmentId"></param>
        /// <returns></returns>
        private List<Tuple<int,int, DateTime>> getSystemIdsReplacement(List<Tuple<int,int,DateTime>> result, int equipmentId, int systemId, DateTime date)
        {
            var _action = _context.ACTION_TAKEN_HISTORY.Where(m => m.recordStatus == (int)RecordStatus.Available && m.equipmentid_auto == equipmentId && m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory &&
            m.system_auto_id_new == systemId && m.event_date <= date).FirstOrDefault();
            if (_action == null || _action.system_auto_id == null) return result;
            result.Add(new Tuple<int,int, DateTime>((int)_action.history_id,(int)(_action.system_auto_id ?? 0),_action.event_date));
            return getSystemIdsReplacement(result, equipmentId, (int)(_action.system_auto_id ?? 0), _action.event_date);
        }
        /// <summary>
        /// This recursive method returns all component Ids which are replaced by new one
        /// </summary>
        /// <param name="result"></param>
        /// <param name="equipmentId"></param>
        /// <param name="componentId"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private List<Tuple<int, int, DateTime>> getComponentIdsReplacement(List<Tuple<int, int, DateTime>> result, int equipmentId, int componentId, DateTime date)
        {
            var _action = _context.ACTION_TAKEN_HISTORY.Where(m => m.recordStatus == (int)RecordStatus.Available && m.equipmentid_auto == equipmentId && m.action_type_auto == (int)ActionType.ReplaceComponentWithNew &&
            m.equnit_auto_new == componentId && m.event_date <= date).FirstOrDefault();
            if (_action == null || _action.equnit_auto == null) return result;
            result.Add(new Tuple<int, int, DateTime>((int)_action.history_id, (int)(_action.equnit_auto ?? 0), _action.event_date));
            return getComponentIdsReplacement(result, equipmentId, (int)(_action.equnit_auto ?? 0), _action.event_date);
        }
        /// <summary>
        /// Returns component Ids for a list of systems having compart type Id and position
        /// </summary>
        /// <param name="systems"></param>
        /// <param name="compartTypeId"></param>
        /// <param name="position"></param>
        /// <param name="childNo">A zero based number of the child component based on the order by compart Id</param>
        /// <returns></returns>
        private List<Tuple<int, int, DateTime>> getComponentIdsOnSystems(List<Tuple<int,int,DateTime>> systems, int equipmentId, int compartTypeId, int position, DateTime date, bool isChild = false, int childNo = 0)
        {
            var _comps = new List<Tuple<int,int, DateTime>>();
            var _result = new List<Tuple<int, int, DateTime>>();
            if (isChild)
            {
                foreach (var _system in systems)
                {
                    var _comp = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == _system.Item2 && m.LU_COMPART.comparttype_auto == compartTypeId && m.pos == position).Where(m => m.LU_COMPART.CHILD_RELATION_LIST.Count() > 0).OrderBy(m => m.compartid_auto).Select(m => new Tuple<int,int,DateTime>(_system.Item1,(int)m.equnit_auto, _system.Item3)).ToList();
                    if (_comp.Count() > childNo)
                        _comps.Add(_comp[childNo]);
                }
                return _result;
            }
            else
            {
                foreach (var _system in systems)
                {
                    var _comp = _context.GENERAL_EQ_UNIT.Where(m => _system.Item2 == m.module_ucsub_auto && m.LU_COMPART.comparttype_auto == compartTypeId && m.pos == position).Where(m => m.LU_COMPART.CHILD_RELATION_LIST.Count() == 0).Select(m => (int)m.equnit_auto).FirstOrDefault();
                    if (_comp != 0)
                        _comps.Add(new Tuple<int,int, DateTime>(_system.Item1, _comp, _system.Item3));
                }
            }
            
            foreach (var _item in _comps.DistinctBy(m=> m.Item2))
            {
                _result.Add(_item);
                _result.AddRange(getComponentIdsReplacement(_result, equipmentId, _item.Item2, date));
            }
                
            return _result.GroupBy(m => m.Item1).Select(m => m.FirstOrDefault()).ToList();
        }

        /// <summary>
        /// This method returns all the actions related to this component type and pos
        /// means that if any replace component happened in the past it will return them as well even
        /// if that action is not related to this component
        /// </summary>
        /// <param name="date"> all actions are before the given date </param>
        /// <returns></returns>
        
            
            ///                             This method is now retired :) 
            /// (You will love this comment ↑↑↑↑↑ @Eric)


        public IEnumerable<ComponentHistoryOldViewModel> GetActionsOnThisEquipmentBasedOnThisComponent(DateTime date)
        {
            List<ComponentHistoryOldViewModel> historyList = new List<ComponentHistoryOldViewModel>();
            if (Id == 0 || DALComponent == null || DALEquipment == null) //This component should be installed on an equipment
                return historyList;
            Equipment LogicalEquipment = new Equipment(_context, longNullableToint(DALEquipment.equipmentid_auto));
            if (LogicalEquipment.Id == 0)
                return historyList;
            var systemIds = _context.LU_Module_Sub.Where(m => m.equipmentid_auto == LogicalEquipment.Id).Select(m => m.Module_sub_auto);
            var equipmentActions = _context.ACTION_TAKEN_HISTORY.Where(m => (m.equipmentid_auto == DALEquipment.equipmentid_auto || systemIds.Any(k => k == m.system_auto_id) || m.equnit_auto == Id) && m.event_date <= date && m.recordStatus == (int)RecordStatus.Available).OrderByDescending(m => m.event_date).ToList();
            bool installedAtSetup = true;
            bool installedUsingNewMethod = false;
            foreach (var act in equipmentActions)
            {
                ComponentHistoryOldViewModel historyRecord = new ComponentHistoryOldViewModel();
                historyRecord.ComponentId = 0; //If at the end of this loop ComponentId is still zero means should not be add to the list
                historyRecord.ActionTakenId = act.history_id;
                ActionType actionType = ActionType.NoActionTakenYet;
                try
                {
                    actionType = (ActionType)act.action_type_auto;
                }
                catch
                {
                    actionType = ActionType.OldActionType;
                }

                switch (actionType)
                {
                    case ActionType.InsertInspection:
                    case ActionType.UpdateInspection:
                        var inspectionDetails = _context.TRACK_INSPECTION_DETAIL.Where(m => m.TRACK_INSPECTION.ActionHistoryId == act.history_id).ToList();
                        TRACK_INSPECTION_DETAIL tid = null;
                        bool found = false;
                        if (inspectionDetails.Where(m => m.track_unit_auto == Id).Count() > 0) // This number should be %99.99 equal to 0  or 1 otherwise something was wrong in this inspection that we don't care about it
                        {
                            found = true;
                            tid = inspectionDetails.Where(m => m.track_unit_auto == Id).First();
                        }
                        else //Means in the selected inspection this component was not present so the related component should be found
                        {
                            if (!isAChildBasedOnCompart())
                                foreach (var t in inspectionDetails)
                                {
                                    if (!found && t.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto == DALComponent.LU_COMPART.comparttype_auto && t.GENERAL_EQ_UNIT.pos == DALComponent.pos && t.SIDE.Side == DALComponent.side)
                                    {
                                        found = true;
                                        tid = t;
                                    }
                                }
                        }
                        if (found && tid != null)
                        {
                            historyRecord.ActionType = actionType == ActionType.InsertInspection ? ActionType.InsertInspection : ActionType.UpdateInspection;
                            historyRecord.action_description = actionType == ActionType.InsertInspection ? "Undercarriage Inspection" : "Undercarriage Inspection (U)";
                            historyRecord.cmu = tid.hours_on_surface == null ? 0 : (int)tid.hours_on_surface;
                            historyRecord.comment = tid.comments;
                            historyRecord.ComponentId = tid.track_unit_auto.LongNullableToInt();
                            //var make = getComponentMake();
                            //historyRecord.makeId = make.makeId;
                            //historyRecord.makeSymbol = make.Symbol;
                            historyRecord.cost = 0;
                            historyRecord.eval = tid.eval_code;
                            historyRecord.Actiondate = act.event_date;
                            historyRecord.type = "NewAction";
                            historyRecord.worn = tid.worn_percentage;
                            historyRecord.compartTypeId = tid.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto;
                            historyRecord.RelatedLinkUrl = "./TrackDetails.aspx?inspec_auto=" + tid.inspection_auto;

                        }
                        break;
                    case ActionType.ReplaceSystemFromInventory:
                        UCSystem LogicalOldSystem = new UCSystem(_context, longNullableToint(act.system_auto_id));
                        if (LogicalOldSystem.Id != 0)
                        {
                            Component LogicalOldComponent = null;
                            bool foundgeu = false;
                            foreach (var geucomp in LogicalOldSystem.Components)
                            {
                                if (!foundgeu && geucomp.LU_COMPART.comparttype_auto == DALComponent.LU_COMPART.comparttype_auto && geucomp.pos == DALComponent.pos && DALComponent.side == geucomp.side)
                                {
                                    foundgeu = true;
                                    installedAtSetup = false;
                                    LogicalOldComponent = new Component(_context, longNullableToint(geucomp.equnit_auto));
                                }
                            }

                            if (foundgeu && LogicalOldComponent != null)
                            {
                                decimal worn = GetComponentWorn(act.event_date);
                                char eval = ' ';
                                
                                GetEvalCodeByWorn(worn, out eval);
                                historyRecord.ActionType = actionType;
                                historyRecord.action_description = "System Replacement";
                                historyRecord.cmu = GetComponentLife(act.event_date);
                                historyRecord.comment = act.comment;
                                historyRecord.ComponentId = Id;
                                //var make = LogicalOldComponent.getComponentMake();
                                //historyRecord.makeId = make.makeId;
                                //historyRecord.makeSymbol = make.Symbol;
                                historyRecord.cost = act.cost;
                                historyRecord.eval = eval.ToString();
                                historyRecord.Actiondate = act.event_date;
                                historyRecord.type = "action";
                                historyRecord.worn = worn;
                                historyRecord.compartTypeId = DALComponent.LU_COMPART.comparttype_auto; //LogicalOldComponent.Id != 0 ? LogicalOldComponent.DALComponent.LU_COMPART.comparttype_auto : 0;
                            }
                        }
                        break;
                    case ActionType.ReplaceComponentWithNew:
                        if (act.equnit_auto > 0)
                        {
                            Component LogicalReplacedComponent = new Component(_context, longNullableToint(act.equnit_auto));
                            if (LogicalReplacedComponent.Id != 0)
                            {
                                if (LogicalReplacedComponent.DALComponent.LU_COMPART.comparttype_auto == DALComponent.LU_COMPART.comparttype_auto && LogicalReplacedComponent.DALComponent.pos == DALComponent.pos && LogicalReplacedComponent.DALComponent.side == DALComponent.side)
                                {
                                    installedAtSetup = false;
                                    decimal worn = LogicalReplacedComponent.GetComponentWorn(act.event_date);
                                    char eval = ' ';
                                    LogicalReplacedComponent.GetEvalCodeByWorn(worn, out eval);
                                    historyRecord.ActionType = actionType;
                                    historyRecord.action_description = "Component Replacement";
                                    historyRecord.cmu = GetComponentLife(act.event_date);
                                    historyRecord.comment = act.comment;
                                    historyRecord.ComponentId = Id;
                                    historyRecord.cost = act.cost;
                                    historyRecord.eval = eval.ToString();
                                    historyRecord.Actiondate = act.event_date;
                                    historyRecord.type = "action";
                                    historyRecord.worn = worn;
                                    historyRecord.compartTypeId = LogicalReplacedComponent.DALComponent.LU_COMPART.comparttype_auto;
                                }
                            }
                        }
                        break;
                    case ActionType.InstallComponentOnSystemOnEquipment:
                        if (act.equnit_auto == Id)
                        {
                            installedUsingNewMethod = true;
                            decimal worn = GetComponentWorn(act.event_date);
                            char eval = ' ';
                            GetEvalCodeByWorn(worn, out eval);
                            historyRecord.ActionType = actionType;
                            historyRecord.action_description = "Component Setup";
                            historyRecord.cmu = GetComponentLife(act.event_date);
                            historyRecord.comment = act.comment;
                            historyRecord.ComponentId = Id;
                            //var make = getComponentMake();
                            //historyRecord.makeId = make.makeId;
                            //historyRecord.makeSymbol = make.Symbol;
                            historyRecord.cost = DALComponent.cost ?? 0; //act.cost;
                            historyRecord.eval = eval.ToString();
                            historyRecord.Actiondate = act.event_date;
                            historyRecord.type = "Installation";
                            historyRecord.worn = worn;
                            historyRecord.compartTypeId = DALComponent.LU_COMPART.comparttype_auto;
                        }
                        break;
                    default:
                        if (act.equnit_auto == DALComponent.equnit_auto) //This component was part of the action in the past which has not been done with our new functions
                        {
                            Component LogicalOldActionComponent = new Component(_context, longNullableToint(act.equnit_auto));

                            historyRecord.ActionType = ActionType.OldActionType;
                            historyRecord.action_description = actionType.Label();//"Other Actions";
                            historyRecord.cmu = LogicalOldActionComponent.GetComponentLife(act.event_date);
                            historyRecord.comment = act.comment;
                            historyRecord.ComponentId = LogicalOldActionComponent.Id;
                            //var make = LogicalOldActionComponent.getComponentMake();
                            //historyRecord.makeId = make.makeId;
                            //historyRecord.makeSymbol = make.Symbol;
                            historyRecord.cost = act.cost;
                            historyRecord.eval = "-";
                            historyRecord.Actiondate = act.event_date;
                            historyRecord.type = "action";
                            historyRecord.worn = 0;
                            historyRecord.compartTypeId = LogicalOldActionComponent.Id != 0 ? LogicalOldActionComponent.DALComponent.LU_COMPART.comparttype_auto : 0;
                        }
                        break;
                } //End of Switch actionType

                if (historyRecord.ComponentId != 0)
                {
                    historyRecord.event_date = historyRecord.Actiondate.ToString("dd MMM yyy");
                    historyRecord.side = Side.Unknown;
                    historyRecord.equipmentSMU = LogicalEquipment.GetSerialMeterUnit(act.event_date);
                    var k = new Component(_context, historyRecord.ComponentId);
                    historyRecord.side = k.GetComponentSide();
                    var make = k.getComponentMake();

                    historyRecord.makeId = make.makeId;
                    historyRecord.makeSymbol = make.Symbol;
                    historyRecord.isBushingTurned = k.isBushingTurned(historyRecord.Actiondate);
                    historyRecord.projectedHours = k.GetProjectedHours(historyRecord.Actiondate);
                    historyRecord.isChildComponent = k.isAChildBasedOnCompart();
                    historyList.Add(historyRecord);
                }
                //End of this action //End of foreach
            }
            DateTime InstallationDate = DALComponent.date_installed == null ? DateTime.MinValue : (DateTime)DALComponent.date_installed;






            if (InstallationDate > DateTime.MinValue && !installedUsingNewMethod)
            {

                ComponentHistoryOldViewModel InstallationRecord = new ComponentHistoryOldViewModel();
                decimal worn = GetComponentWorn(InstallationDate);
                char eval = ' ';
                GetEvalCodeByWorn(worn, out eval);
                InstallationRecord.ActionType = ActionType.EquipmentSetup;
                InstallationRecord.action_description = "Installation";
                InstallationRecord.cmu = longNullableToint(DALComponent.cmu);
                InstallationRecord.comment = "Component Setup";
                InstallationRecord.ComponentId = Id;
                InstallationRecord.cost = DALComponent.cost == null ? 0 : (decimal)DALComponent.cost;
                InstallationRecord.eval = eval.ToString();
                InstallationRecord.Actiondate = InstallationDate;
                InstallationRecord.type = "Installation";
                InstallationRecord.worn = worn;
                InstallationRecord.event_date = InstallationDate.ToString("dd MMM yyy");
                InstallationRecord.equipmentSMU = (int)(DALComponent.eq_smu_at_install ?? 0);
                InstallationRecord.side = GetComponentSide();
                var make = getComponentMake();
                InstallationRecord.makeId = make.makeId;
                InstallationRecord.makeSymbol = make.Symbol;
                InstallationRecord.isBushingTurned = isBushingTurned(InstallationDate);
                InstallationRecord.compartTypeId = DALComponent.LU_COMPART.comparttype_auto;
                InstallationRecord.projectedHours = GetProjectedHours(InstallationDate);
                InstallationRecord.ActionTakenId = -1; // Action is a component setup event
                historyList.Add(InstallationRecord);
            }
            return historyList;
        }
        
        public string GetComponentSideLabel()
        {
            switch (GetComponentSide())
            {
                case Side.Left:
                    return "Left";
                case Side.Right:
                    return "Right";
                case Side.Both:
                    return "Both";
                default:
                    return "Unknown";
            }
        }

        public Side GetComponentSide()
        {
            if (Id == 0 || DALComponent == null || DALComponent.side == null)
                return Side.Unknown;
            if (DALComponent.side == 1)
                return Side.Left;
            if (DALComponent.side == 2)
                return Side.Right;
            return Side.Unknown;
        }
        private int getDefaultBudgetLife()
        {
            if (Id == 0)
                return 0;
            BLL.Core.Domain.Compart LogicalCompart = new Domain.Compart(_context, DALComponent.compartid_auto);
            return LogicalCompart.getCompartDefaultBudgetLife();
        }
        /// <summary>
        /// Returns projected hours which is the remaining hours based on worn + life of the component
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public int GetProjectedHours(DateTime date)
        {

            if (Id == 0 || DALComponent == null)
                return 0;
            decimal worn = GetComponentWorn(date);
            int cmu = GetComponentLife(date);
            int budget_life = DALComponent.track_budget_life == null ? getDefaultBudgetLife() : (int)DALComponent.track_budget_life;

            if (worn < 1) worn = 1;
            decimal lifeRemainingBasedOnWorn = (cmu * 100 / worn) - cmu;
            int projectedHours = budget_life;
            if (worn > 30)
                projectedHours = (int)lifeRemainingBasedOnWorn + cmu;
            // TT-606: Removed the part where we double the projected life of the bushing if it hasn't been turned. 
            /*if (DALComponent.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Bushing)
            {
                if (!isBushingTurned(date))
                {
                    projectedHours = projectedHours * 2;
                }
                else
                {
                    var turnDate = getRecentActionDateBasedOnType(ActionType.TurnPinsAndBushings, date);
                    if (turnDate != DateTime.MinValue)
                    {
                        projectedHours = GetComponentLife(turnDate) + projectedHours;
                    }
                }
            }*/
            return projectedHours;
        }
        /// <summary>
        /// Return extended projected hours for the component
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public int GetExtProjectedHours(DateTime date)
        {
            return (int)(GetProjectedHours(date) * 1.2);
        }

        /// <summary>
        /// Returns a date if action type occured on this component before the given date otherwise returns DateTime.MinValue
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public DateTime getRecentActionDateBasedOnType(ActionType type, DateTime date)
        {
            if (Id == 0)
                return DateTime.MinValue;
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Id && m.action_type_auto == (int)type && m.event_date <= date && m.recordStatus == 0);
            if (actions.Count() > 0)
                return actions.OrderBy(m => m.event_date).First().event_date;
            return DateTime.MinValue;
        }

        /// <summary>
        /// Returns compartId if this component does not required to have child comparts
        /// otherwise if any child with the same compart Id is registered for this equipment it will return compartId
        /// otherwise returns 0 means childs should be selected in the page
        /// </summary>
        /// <returns></returns>
        public int getLastSavedCompartIdForMiningShovel()
        {
            var components = GetEquipmentComponents();
            if (Id == 0 || components == null)
                return 0;
            var childs = Compart.getChildComparts();
            Side side = GetComponentSide();
            if (childs.Count() > 0 && DALEquipment != null)
            {
                var childsSelected = components.Where(m => m.side == (int)side && childs.Any(k => k.compartid_auto == m.compartid_auto));
                if (childsSelected.Count() == 0)
                    return 0;
            }
            return Compart.Id;
        }
        /// <summary>
        /// Returns list of all components which are childs ot this one
        /// </summary>
        /// <returns></returns>
        public List<DAL.GENERAL_EQ_UNIT> getChildsListForMiningShovel()
        {
            List<DAL.GENERAL_EQ_UNIT> result = new List<GENERAL_EQ_UNIT>();
            if (Id == 0)
                return result;
            var childs = Compart.getChildComparts().Select(m => m.compartid_auto);
            if (childs.Count() > 0)
            {
                var systemComponents = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == DALComponent.module_ucsub_auto);
                return systemComponents.Where(m => childs.Any(k => k == m.compartid_auto)).ToList();
            }
            return result;
        }
        /// <summary>
        /// If this component's compartment is a child of any other compartments
        /// returns true otherwise false
        /// </summary>
        /// <returns></returns>
        public bool isAChildBasedOnCompart()
        {
            if (Id == 0 || DALComponent == null || DALComponent.LU_COMPART == null)
                return false;
            var k = _context.COMPART_PARENT_RELATION.Where(m => m.ChildCompartId == DALComponent.compartid_auto);
            if (k.Count() == 0)
                return false;
            return true;
        }

        /// <summary>
        /// If this component's compartment is a parent of any other compartments
        /// returns true otherwise false
        /// </summary>
        /// <returns></returns>
        public bool isAParentBasedOnCompart()
        {
            if (Id == 0 || DALComponent == null || DALComponent.LU_COMPART == null)
                return false;
            var k = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == DALComponent.compartid_auto);
            if (k.Count() == 0)
                return false;
            return true;
        }

        /// <summary>
        /// Results are based on the current system
        /// This method looks on the current system and if there is any component which 
        /// can be a match as a parent for this component compart will return it immediately!
        /// 
        /// Assumptions: 
        /// 1- There is only one 'Link' and only one 'Sprockt' on the system which this component belongs to!
        /// 2- If this component is not a child based on the compart type then returns 0
        /// 3- In the compart setup page there must be only one parent assigned for this component!
        /// If any part of the assumptions break then results won't be correct!
        /// </summary>
        /// <returns>Parent Component Id</returns>
        public int getParentComponentId()
        {
            if (!isAChildBasedOnCompart() || getComponentDALSystem() == null)
                return 0;
            var componentsOnSystem = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == DALSystem.Module_sub_auto).ToList();
            foreach (var comp in componentsOnSystem)
            {
                var logicalComponent = new Component(_context, comp.equnit_auto.LongNullableToInt());
                if (logicalComponent.getChildsListForMiningShovel().Where(m => m.equnit_auto == Id).Count() > 0)
                    return logicalComponent.Id;
            }
            return 0;
        }

        /// <summary>
        /// Returns 0 if this component was installed in setup time otherwise if this component installed based on replace with new action 
        /// returns replaced componentId.
        /// Old component will be found comparing its compartId, side and pos with the current one.
        /// </summary>
        /// <returns>Old cmponent Id if exist</returns>
        public int getReplacedComponentIdByThis()
        {
            if (Id == 0 || DALComponent == null || DALComponent.equipmentid_auto == null || DALEquipment == null)
                return 0;

            List<long?> replacedComponents = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == DALEquipment.equipmentid_auto && m.recordStatus == 0 && m.action_type_auto == (int)(ActionType.ReplaceComponentWithNew)).Select(m => m.equnit_auto).ToList();
            foreach (var cmpId in replacedComponents)
            {
                var LogicalComponent = new Component(_context, longNullableToint(cmpId));
                if (LogicalComponent.Id == 0 || LogicalComponent.DALComponent == null)
                    continue;
                if (DALComponent.side == LogicalComponent.DALComponent.side
                    && DALComponent.pos == LogicalComponent.DALComponent.pos
                    && DALComponent.LU_COMPART.comparttype_auto == LogicalComponent.DALComponent.LU_COMPART.comparttype_auto
                    && (LogicalComponent.DALComponent.equipmentid_auto == null || LogicalComponent.DALComponent.equipmentid_auto == 0))
                    return LogicalComponent.Id;
            }
            return 0;
        }
        /// <summary>
        /// Returns component make view model 
        /// </summary>
        /// <returns>ComponentMakeViewModel</returns>
        public ComponentMakeViewModel getComponentMake()
        {
            ComponentMakeViewModel result = new ComponentMakeViewModel
            {
                Id = 0,
                makeId = 0,
                Symbol = "UN",
                Description = "Unknown"
            };
            if (Id == 0 || DALComponent == null)
                return result;
            result.Id = Id;
            if (DALComponent.Make != null)
            {
                result.Symbol = DALComponent.Make.makeid;
                result.Description = DALComponent.Make.makedesc;
                result.makeId = DALComponent.Make.make_auto;
                return result;
            }
            var compartExtIEn = _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == DALComponent.compartid_auto);
            if (compartExtIEn.Count() == 0)
                return result;
            int makeId = longNullableToint(compartExtIEn.First().make_auto);
            var make = _context.MAKE.Find(makeId);
            if (make == null)
                return result;
            result.Symbol = make.makeid;
            result.Description = make.makedesc;
            result.makeId = make.make_auto;
            return result;
        }
        /// <summary>
        /// Returns true if component type is bushing and has already been turned!
        /// </summary>
        /// <returns></returns>
        public bool isBushingTurned(DateTime date)
        {
            if (Id == 0 || DALComponent == null)
                return false;
            if (DALComponent.LU_COMPART.comparttype_auto != (int)CompartTypeEnum.Bushing)
                return false;
            return _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Id && m.recordStatus == 0 && (m.action_type_auto == (int)ActionType.TurnPinsAndBushingsLink || m.action_type_auto == (int)ActionType.TurnPinsAndBushingsBush) && m.event_date <= date).Count() > 0;
        }

        public bool CreateNewComponent(SetupComponentParams Params)
        {
            if (Id != 0)
                return false;
            var compart = _context.LU_COMPART.Find(Params.CompartId);
            if (compart == null)
                return false;
            GENERAL_EQ_UNIT newComponent = new GENERAL_EQ_UNIT
            {
                compartid_auto = Params.CompartId,
                compartsn = compart.compartid,
                created_date = DateTime.Now,
                created_user = Params.UserName,
                pos = 0,
                side = 0,
                track_budget_life = Params.BudgetLife,
                cmu = Params.Life,
                cost = Params.Cost,
                max_rebuild = 20, // :? Not enough knowledge about its usage
                insp_item = false,// :? Not enough knowledge about its usage
                insp_uom = 0, // :? Not enough knowledge about its usage
                comp_status = 0,// :? Not enough knowledge about its usage
                comp_uniq_id = Guid.NewGuid(),
                track_0_worn = 0,
                track_100_worn = 0,
                track_120_worn = 0,
                variable_comp = false,
                component_current_value = 0
            };
            _context.GENERAL_EQ_UNIT.Add(newComponent);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                string message = e.Message;
                return false;
            }
            Id = longNullableToint(newComponent.equnit_auto);
            Init(Id);
            return true;
        }

        public ComponentSetup CreateNewComponent(ComponentSetup component, int userId, string userName)
        {
            if (Id != 0)
            {
                component.Result.OperationSucceed = false;
                component.Result.LastMessage = "Component has Id and cannot be created again!";
                component.Result.ActionLog = "Component has Id and cannot be created again!";
                return component;
            }
            var compart = _context.LU_COMPART.Find(component.Compart.Id);
            if (compart == null)
            {
                component.Result.OperationSucceed = false;
                component.Result.LastMessage = "Compart is not valid!";
                component.Result.ActionLog = "Compart is not valid!";
                return component;
            }
            int? shoeId = null;
            if (component.ShoeSize.Id != 0)
                shoeId = component.ShoeSize.Id;

            GENERAL_EQ_UNIT newComponent = new GENERAL_EQ_UNIT
            {
                compartid_auto = component.Compart.Id,
                compartsn = compart.compartid,
                created_date = DateTime.Now,
                created_user = userName,
                pos = 0,
                side = 0,
                track_budget_life = component.BudgetLife,
                cmu = component.HoursAtInstall,
                cost = component.InstallCost,
                max_rebuild = 20, // :? Not enough knowledge about its usage
                insp_item = false,// :? Not enough knowledge about its usage
                insp_uom = 0, // :? Not enough knowledge about its usage
                comp_status = 0,// :? Not enough knowledge about its usage
                comp_uniq_id = Guid.NewGuid(),
                track_0_worn = 0,
                track_100_worn = 0,
                track_120_worn = 0,
                variable_comp = false,
                component_current_value = 0,
                make_auto = component.Brand.Id,
                ShoeSizeId = shoeId,
                ShoeGrouserNo = component.Grouser.Id,
                compart_descr = component.Compart.CompartTitle,
                module_ucsub_auto = component.SystemId
            };
            _context.GENERAL_EQ_UNIT.Add(newComponent);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                string message = e.Message;
                component.Result.OperationSucceed = false;
                component.Result.LastMessage = "Creating component failed! please check log.";
                component.Result.ActionLog = "log: " + message;
                return component;
            }
            Id = longNullableToint(newComponent.equnit_auto);
            Init(Id);
            component.Id = Id;
            component.Result.OperationSucceed = true;
            component.Result.LastMessage = "Creating component succeeded!";
            component.Result.ActionLog = "Creating component succeeded!";
            return component;
        }

        public ComponentSetup InstallComponentOnSystemNonAction(ComponentSetup component, EquipmentActionRecord _actionRecord, int UCSystemId, int SystemLife)
        {

            DAL.GENERAL_EQ_UNIT currentComponent = null;
            if (component.Id != 0 && DALComponent.equnit_auto == component.Id)
                currentComponent = DALComponent;
            else currentComponent = _context.GENERAL_EQ_UNIT.Find(component.Id);

            if (currentComponent == null)
            {
                component.Result.OperationSucceed = false;
                component.Result.LastMessage = "Component cannot be found to be installed on system!";
                component.Result.ActionLog = "Component cannot be found to be installed on system!";
                return component;
            }

            currentComponent.date_installed = _actionRecord.ActionDate;
            currentComponent.smu_at_install = _actionRecord.ReadSmuNumber;
            currentComponent.eq_smu_at_install = _actionRecord.ReadSmuNumber;
            currentComponent.pos = (byte)component.Pos;
            currentComponent.eq_ltd_at_install = _actionRecord.EquipmentActualLife;
            currentComponent.module_ucsub_auto = UCSystemId;
            currentComponent.system_LTD_at_install = SystemLife;
            _context.Entry(currentComponent).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                component.Result.OperationSucceed = true;
                component.Result.LastMessage = "Component attached to the system successfully!";
                component.Result.ActionLog = "Component attached to the system successfully!";
                component.SystemId = UCSystemId;
                return component;
            }
            catch (Exception ex)
            {
                component.Result.OperationSucceed = false;
                component.Result.LastMessage = "Component failed to be attached to the system!";
                component.Result.ActionLog = ex.Message;
                return component;
            }
        }

        public async Task<ComponentSetup> CreateNewComponentAsync(ComponentSetup component, int userId, string userName)
        {
            return await Task.Run(() => CreateNewComponent(component, userId, userName));
        }
        /// <summary>
        /// This method updates component details in the setup time 
        /// It won't update life and other objects
        /// </summary>
        /// <param name="Params"></param>
        /// <returns></returns>
        public bool UpdateComponentOnSetup(SetupComponentParams Params)
        {
            if (Id == 0 || DALComponent == null)
                return false;
            var compart = _context.LU_COMPART.Find(Params.CompartId);
            if (compart == null)
                return false;
            DALComponent.compartid_auto = Params.CompartId;
            DALComponent.track_budget_life = Params.BudgetLife;
            DALComponent.cost = Params.Cost;
            DALComponent.cmu = Params.CMU;
            _context.Entry(DALComponent).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// This method updates component details in the setup time 
        /// It won't update life and other objects
        /// </summary>
        /// <param name="Params"></param>
        /// <returns></returns>
        public ComponentSetup UpdateComponentOnSetup(ComponentSetup Params)
        {
            if (Id == 0 || DALComponent == null)
            {
                Params.Result.LastMessage = "Component cannot be found for update!";
                Params.Result.OperationSucceed = false;
                return Params;
            }
            DALComponent.compartid_auto = Params.Compart.Id;
            DALComponent.track_budget_life = Params.BudgetLife;
            DALComponent.cost = Params.InstallCost;
            DALComponent.cmu = Params.HoursAtInstall;
            DALComponent.ShoeGrouserNo = Params.Grouser.Id;
            DALComponent.ShoeSizeId = Params.ShoeSize.Id == 0 ? null : (int?)Params.ShoeSize.Id;
            DALComponent.make_auto = Params.Brand.Id;
            _context.Entry(DALComponent).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                Params.Result.OperationSucceed = true;
                Params.Result.LastMessage = "Component updated successfully.";
                return Params;
            }
            catch (Exception ex)
            {
                Params.Result.OperationSucceed = true;
                Params.Result.LastMessage = "Component update failed!.";
                Params.Result.ActionLog = ex.Message;
                if (ex.InnerException != null)
                    Params.Result.ActionLog += "-> Inner Exception: " + ex.InnerException.Message;
                return Params;
            }
        }

        public bool removeInstallationRecord()
        {
            if (Id == 0)
                return false;
            var installationRecord = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Id && m.recordStatus == 0 && m.action_type_auto == (int)ActionType.InstallComponentOnSystemOnEquipment);
            var componentLifeRecords = DALComponent.Life.Where(m => m.ACTION_TAKEN_HISTORY.action_type_auto == (int)ActionType.InstallSystemOnEquipment && m.ACTION_TAKEN_HISTORY.recordStatus == 0);
            if (installationRecord.Count() != 0)
                _context.ACTION_TAKEN_HISTORY.RemoveRange(installationRecord);
            if (componentLifeRecords.Count() != 0)
                _context.COMPONENT_LIFE.RemoveRange(componentLifeRecords);
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                return false;
            }
        }

        public ComponentActionVwMdl getComponentLatestAction()
        {
            ComponentActionVwMdl result = new ComponentActionVwMdl
            {
                Id = 0,
                ActionType = ActionType.NoActionTakenYet,
                ActionDate = DateTime.MinValue,
                ActionDStr = "-"
            };
            if (Id == 0)
                return result;
            var records = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == Id && m.recordStatus == 0).OrderByDescending(m => m.event_date);
            if (records.Count() == 0)
                return result;
            result.Id = Id;
            result.ActionDate = records.First().event_date;
            result.ActionDStr = records.First().event_date.ToString("dd MMM yyyy");
            result.ActionType = (ActionType)records.First().action_type_auto;
            result.ActionTypeStr = result.ActionType.Label();
            result.Cost = records.First().cost;
            return result;
        }
        /// <summary>
        /// WARNING: I am not kidding! DONOT call this method if you don't know what you are doing!
        /// This method detach component from system and equipment and leaves it in nowhere
        /// This will cause hundresds of issues if the component is in use somewhere
        /// just call for removing crappy components from equipment
        /// ANOTHER WARNING :O|-|== !!  It will remove inspection details records as well!!!
        /// 
        /// </summary>
        /// <returns>a boolean indicates if operations succeeded</returns>
        public bool throwAway(string AreYouSure)
        {
            if (Id == 0 || AreYouSure.ToLower() != "Yes, I am sure.".ToLower())
                return false;
            try
            {
                DALComponent.equipmentid_auto = null;
                DALComponent.module_ucsub_auto = null;

                var images = _context.TRACK_INSPECTION_IMAGES.Where(m => m.TRACK_INSPECTION_DETAIL.track_unit_auto == Id);
                _context.TRACK_INSPECTION_IMAGES.RemoveRange(images);

                var tids = _context.TRACK_INSPECTION_DETAIL.Where(m => m.track_unit_auto == Id);
                _context.TRACK_INSPECTION_DETAIL.RemoveRange(tids);

                _context.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                string SeeWhatsWrongHere = ex.Message;
            }
            return false;
        }
        public IEnumerable<SHOE_SIZE> getShoeSizeList()
        {
            return _context.SHOE_SIZE;
        }

        public async Task<IEnumerable<SHOE_SIZE>> getShoeSizeListAsync()
        {
            return await Task.Run(() => getShoeSizeList());
        }

        public string GetPositionLabel()
        {
            if (Id == 0 || DALComponent == null)
                return "";
            return ((int)(DALComponent.pos ?? 0)).PositionLabel(DALComponent.LU_COMPART.comparttype_auto);
        }

        /// <summary>
        /// Updates the component CMU with the given new cmu. Also goes through and modifies all life records 
        /// and undercarriage inspection details component life records. 
        /// BE CAREFUL USING THIS! 
        /// There is currently no way to revert this after doing it, as we save no history of the old
        /// component life! 
        /// </summary>
        /// <param name="newCmu"></param>
        /// <returns>Returns true if it all succeeded. </returns>
        public bool UpdateComponentCmuAtSetup(int newCmu)
        {
            int currentSetupLife = pvComponent.Life.OrderBy(l => l.ActionDate).Select(l => l.ActualLife).FirstOrDefault();
            int cmuDifference = newCmu - currentSetupLife;
            var life = pvComponent.Life.ToList();
            bool failed = false;
            pvComponent.cmu = newCmu;
            life.ForEach(l =>
            {
                int newLife = l.ActualLife + cmuDifference;
                if (newLife < 0)
                    failed = true;
                l.ActualLife = newLife;
            });

            var inspections = pvComponent.TRACK_INSPECTION_DETAIL.ToList();
            inspections.ForEach(i =>
            {
                int newLife = (int)i.hours_on_surface + cmuDifference;
                if (newLife < 0)
                    failed = true;
                i.hours_on_surface = newLife;
                i.projected_hours = i.projected_hours + cmuDifference;
                i.remaining_hours = i.remaining_hours + cmuDifference;
                i.ext_projected_hours = i.ext_projected_hours + cmuDifference;
                i.ext_remaining_hours = i.ext_remaining_hours + cmuDifference;
            });
            if (failed)
                return false;
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public int GetEquipmentSmuWhenComponentInstalled()
        {
            return (int)(DALComponent.smu_at_install ?? 0);
        }

        public Tuple<bool, string> UpdateComponentCost(decimal newCost)
        {
            if (newCost < 0)
                return Tuple.Create(false, "Failed to update cost. The value can't be less than 0. ");
            try
            {
                DALComponent.cost = Convert.ToInt64(newCost);
                _context.SaveChanges();
                return Tuple.Create(true, "Cost updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update cost. " + e.ToDetailedString());
            }
        }

        public decimal GetPercentageWearPerHourByInspection(int inspectionId)
        {
            int startCmu = 0;
            decimal startWorn = 0;
            int endCmu = 0;
            decimal endWorn = 0;
            int cmuDifference = 0;
            decimal wearDifference = 0;

            var inspection = _context.TRACK_INSPECTION.Find(inspectionId);
            var inspectionDetail = DALComponent.TRACK_INSPECTION_DETAIL.Where(i => i.inspection_auto == inspectionId).FirstOrDefault();
            var previousInspection = GetPreviousInspection(inspectionId);
            if (previousInspection == null)
            {
                startCmu = (int)(DALComponent.cmu ?? 0); // Cmu at install
                startWorn = 0;
            }
            else
            {
                var previousComponentInspectionRecord = previousInspection.TRACK_INSPECTION_DETAIL.Where(d => d.track_unit_auto == DALComponent.equnit_auto).FirstOrDefault();
                if (previousComponentInspectionRecord == null)
                {
                    startCmu = (int)(DALComponent.cmu ?? 0); // Cmu at install
                    startWorn = 0;
                }
                else
                {
                    startCmu = previousComponentInspectionRecord.hours_on_surface ?? (int)(DALComponent.cmu ?? 0);
                    startWorn = previousComponentInspectionRecord.worn_percentage;
                }
            }
            endCmu = inspectionDetail.hours_on_surface ?? startCmu;
            endWorn = inspectionDetail.worn_percentage;
            cmuDifference = endCmu - startCmu;
            wearDifference = endWorn - startWorn;
            if (wearDifference > 0 && cmuDifference > 0)
                return wearDifference / cmuDifference;
            else
                return 0;
        }

        private TRACK_INSPECTION GetPreviousInspection(int inspectionId)
        {
            var inspection = _context.TRACK_INSPECTION.Find(inspectionId);
            return _context.TRACK_INSPECTION
                .Where(i => i.equipmentid_auto == inspection.equipmentid_auto)
                .Where(i => i.inspection_date <= inspection.inspection_date)
                .Where(i => i.smu < inspection.smu)
                .OrderByDescending(i => i.inspection_date)
                .ThenByDescending(i => i.smu)
                .FirstOrDefault();
        }

        /// <summary>
        /// Returns LU.COMPART.AcceptEvalAsReading for this component
        /// </summary>
        /// <returns></returns>
        public bool getAcceptReadAsEvalStatus()
        {
            if (DALComponent != null && DALComponent.LU_COMPART != null)
                return DALComponent.LU_COMPART.AcceptEvalAsReading;
            return false;
        }
        /// <summary>
        /// Updates the parent component worn in any inspection after the given date by the worst child component
        /// After component replacement when it has child components we need to update parent component worn percentage with the worst child worn
        /// </summary>
        /// <param name="Date">All the inspections equal or after this date will be updated</param>
        /// <returns></returns>
        public ResultMessage RefreshParentWorn(DateTime Date)
        {
            if (Id == 0)
                return new ResultMessage { Id = 0, OperationSucceed = false, LastMessage = "Component couldn't be found!", ActionLog = "Component Id needs to be passed as a parameter to the Component constructor!" };
            int EqId = getEquipmentId();
            var InspectionIds = _context.TRACK_INSPECTION.Where(m => m.equipmentid_auto == EqId && m.ActionTakenHistory.recordStatus == (int)RecordStatus.Available && m.inspection_date >= Date).Select(m => m.inspection_auto).ToList();
            foreach (var _id in InspectionIds)
                UpdateMiningShovelInspectionParentsFromChildren(_id);
            return new ResultMessage { Id = 0, OperationSucceed = true, LastMessage = "Opertation is completed!", ActionLog = "Operation is completed but we don't know it has completed successfully." };
        }


      

        //♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦ END OF COMPONENT CLASS ♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦♦
    }













    public class GeneralComponent
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public int UCSystemId { get; set; }
        public int CompartId { get; set; }
        public int brandId { get; set; }
        public DateTime InstallDate { get; set; }
        public int ComponentLifeAtInstall { get; set; }
        public int UCSystemLifeAtInstall { get; set; }
        public int EquipmentSMUatInstall { get; set; }
        public int EquipmentLifeAtInstall { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
        public ComponentStatus ComponentStatus { get; set; }
        public int Position { get; set; }
        public int Side { get; set; }
        public decimal Worn { get; set; }
        public decimal Worn100 { get; set; }
        public decimal Worn120 { get; set; }
        public int BudgetedLife { get; set; }
        public decimal Cost { get; set; }
    }

}
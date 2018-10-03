using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using DAL;
using System.Data.Entity;
using BLL.Persistence.Repositories;
using BLL.Extensions;
using System.Threading.Tasks;
using BLL.Core.ViewModel;

namespace BLL.Core.Domain
{
    public class Equipment : Repository<EQUIPMENT>, IEquipment
    {
        private int pvId;
        private int pvSerialMeterUnit;
        private int pvLife;
        private EQUIPMENT pvDALEquipment;
        private IList<LU_Module_Sub> pvSystems;
        private IList<GENERAL_EQ_UNIT> pvComponents;
        public int Id
        {
            get { return pvId; }
            private set { pvId = value; }
        }
        public int LatestSerialMeterUnit
        {
            get { return pvSerialMeterUnit; }
            private set { pvSerialMeterUnit = value; }
        }
        public int EquipmentLatestLife
        {
            get { return pvLife; }
            private set { pvLife = value; }
        }
        public EQUIPMENT DALEquipment
        {
            get { return pvDALEquipment; }
            set { pvDALEquipment = value; }
        }
        public IList<LU_Module_Sub> DALSystems
        {
            get { return pvSystems; }
            set { pvSystems = value; }
        }
        public IList<GENERAL_EQ_UNIT> DALComponents
        {
            get { return pvComponents; }
            set { pvComponents = value; }
        }
        private IUndercarriageContext _context
        {
            get { return Context as IUndercarriageContext; }
        }
        public Equipment(IUndercarriageContext context) : base(context)
        {
            Id = 0;
        }
        public Equipment(IUndercarriageContext context, int id) : base(context)
        {
            Id = 0;
            DALEquipment = _context.EQUIPMENTs.Find(id);
            if (DALEquipment != null)
            {
                Id = id;
                DALSystems = GetEquipmentSystemListOrdered();
                //DALComponents = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == DALEquipment.equipmentid_auto).ToList();
                EquipmentLatestLife = GetEquipmentLife(DateTime.Now);
                LatestSerialMeterUnit = GetSerialMeterUnit(DateTime.Now);
            }
        }
        /// <summary>
        /// Warning: Calling this will update DALEquipment!
        /// If there is DALEquipment which is the same with the previous one it won't pull from database!
        /// </summary>
        /// <param name="EquipmentId"></param>
        /// <returns></returns>
        public DAL.EQUIPMENT getDALEquipment(int EquipmentId)
        {
            if (DALEquipment != null && DALEquipment.equipmentid_auto == EquipmentId)
                return DALEquipment;
            DALEquipment = _context.EQUIPMENTs.Find(EquipmentId);
            return DALEquipment;
        }

        public CUSTOMER getDALCustomer()
        {
            if (Id == 0)
                return null;
            var jobsite = _context.CRSF.Find(DALEquipment.crsf_auto);
            if (jobsite == null)
                return null;
            return _context.CUSTOMERs.Find(jobsite.customer_auto);
        }
        private struct SystemWithTypes
        {
            public LU_Module_Sub DALSys { get; set; }
            public UCSystemType Type { get; set; }
        }

        private IList<LU_Module_Sub> GetEquipmentSystemListOrdered()
        {
            var systemList = _context.LU_Module_Sub.Where(m => m.equipmentid_auto == DALEquipment.equipmentid_auto).ToList();
            var orderedList = new List<SystemWithTypes>();
            foreach (var s in systemList)
            {
                var UnknownlogicalSystem = new UCSystem(_context);
                orderedList.Add(new SystemWithTypes { DALSys = s, Type = UnknownlogicalSystem.GetSystemType((int)(s.Module_sub_auto)) });
            }
            return orderedList.OrderBy(m => m.Type).Select(m => m.DALSys).ToList();
        }

        public int GetEquipmentLife(DateTime date)
        {
            if (Id == 0)
                return -1;
            var Eq = _context.EQUIPMENTs.Find(Id);
            if (Eq == null)
                return -1;
            var lifes = Eq.Life.Where(m => m.ActionDate <= date && m.Action.recordStatus == 0).OrderBy(field => field.ActionDate).ThenBy(m => m.Id);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return longNullableToint(Eq.LTD_at_start);
        }

        public int GetEquipmentLife(int EquipmentId, DateTime date)
        {
            var Eq = _context.EQUIPMENTs.Find(EquipmentId);
            if (Eq == null)
                return -1;
            var lifes = Eq.Life.Where(m => m.ActionDate <= date && m.Action.recordStatus == 0).OrderBy(field => field.ActionDate).ThenBy(m => m.Id);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            return longNullableToint(Eq.LTD_at_start);
        }

        public DAL.EQUIPMENT findEquipment(string serial, string unit)
        {
            var equipments = _context.EQUIPMENTs.Where(m => m.serialno.ToLower().Contains(serial.ToLower()) || m.unitno.ToLower().Contains(unit.ToLower()));
            if (equipments.Count() == 1)
                return equipments.FirstOrDefault();
            var equipmentsBySerial = equipments.Where(m => m.serialno.ToLower().Contains(serial.ToLower()));
            if (equipmentsBySerial.Count() == 1)
                return equipmentsBySerial.FirstOrDefault();
            var equipmentsByUnit = equipments.Where(m => m.unitno.ToLower().Contains(unit.ToLower()));
            if (equipmentsByUnit.Count() == 1)
                return equipmentsByUnit.FirstOrDefault();
            return null;
        }
        //protected int GetEquipmentLife(int Id, DateTime date)
        //{
        //    var Eq = _context.EQUIPMENTs.Find(Id);
        //    if (Eq == null)
        //        return -1;
        //    var lifes = Eq.Life.Where(m => m.ActionDate <= date && m.Action.recordStatus == 0).OrderBy(field => field.ActionDate);
        //    if (lifes.Count() > 0)
        //        return lifes.Last().ActualLife;

        //    return longNullableToint(Eq.LTD_at_start);
        //}

        public int getEquipmentSmuAtSetup()
        {
            if (Id == 0)
                return 0;
            if (DALEquipment == null)
                return 0;

            if (DALEquipment.purchase_date == null)
                return 0;
            return GetSerialMeterUnit((DateTime)DALEquipment.purchase_date);
        }

        public async Task<int> getEquipmentSmuAtSetupAsync()
        {
            return await Task.Run(() => getEquipmentSmuAtSetup());
        }

        public string GetEquipmentApplication()
        {
            return pvDALEquipment.LU_MMTA.APPLICATION.appdesc;
        }

        public DateTime getEquipmentDateAtSetup()
        {
            if (Id == 0)
                return DateTime.MinValue;
            if (DALEquipment == null)
                return DateTime.MinValue;

            if (DALEquipment.purchase_date == null)
                return DateTime.MinValue;
            return (DateTime)DALEquipment.purchase_date;
        }
        public async Task<DateTime> getEquipmentDateAtSetupAsync()
        {
            return await Task.Run(() => getEquipmentDateAtSetup());
        }
        protected int GetComponentLifeOldMethod(int Id, DateTime date)
        {
            var geucomp = _context.GENERAL_EQ_UNIT.Find(Id);
            if (Id == 0 || geucomp == null)
                return 0;
            if (geucomp.equipmentid_auto == null || geucomp.equipmentid_auto == 0 || geucomp.smu_at_install == null || geucomp.smu_at_install == 0)
                return longNullableToint(geucomp.cmu);

            int EqLtdAtInstall = 0;
            DateTime EqInstalled = geucomp.date_installed == null ? DateTime.MinValue : (DateTime)geucomp.date_installed;
            var EqLifes = _context.EQUIPMENT_LIVES.Where(m => m.EquipmentId == geucomp.equipmentid_auto && m.ActionDate >= EqInstalled && m.Action.recordStatus == (int)RecordStatus.Available);
            if (EqLifes.Where(m => m.Action.action_type_auto == (int)ActionType.InstallSystemOnEquipment).Count() > 0)
                return longNullableToint(geucomp.cmu);
            if (EqLifes.Count() > 0)
            {
                EqLtdAtInstall = EqLifes.OrderBy(m => m.ActionDate).First().ActualLife;
            }
            else
            {
                EqLtdAtInstall = GetEquipmentLife(longNullableToint(geucomp.equipmentid_auto), EqInstalled);
            }
            int ComponentEqLtdAtInstall = longNullableToint(geucomp.eq_ltd_at_install);
            int ComponentCmu = longNullableToint(geucomp.cmu);
            return EqLtdAtInstall - ComponentEqLtdAtInstall + ComponentCmu;
        }
        /// <summary>
        /// This method returns those components which are available to do an action on them
        /// Basically because of Mining Shovel temporary implementations there are some components 
        /// which are not a real component so they are deducted from list of equipment components
        /// This method is likely to be removed in the future when mining shovel functionality gets implemented correctly
        /// </summary>
        /// <param name="_side">If side is unknown returns both side!</param>
        /// <returns></returns>
        public List<GENERAL_EQ_UNIT> GetComponentsAvailableForAction(Side _side)
        {
            List<GENERAL_EQ_UNIT> resultList = new List<GENERAL_EQ_UNIT>();
            if (Id == 0)
                return resultList;
            int side = 0;
            if (_side == Side.Left) side = 1;
            if (_side == Side.Right) side = 2;

            List<GENERAL_EQ_UNIT> componentList;
            componentList = side == 0 ? _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == Id).ToList()
                    : _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == Id && m.side == side).ToList();

            foreach (var DALComp in componentList)
            {
                if (!new Component(_context, longNullableToint(DALComp.equnit_auto)).isAChildBasedOnCompart())
                    resultList.Add(DALComp);
            }
            return resultList;
        }

        public int GetSerialMeterUnit(DateTime date)
        {
            if (Id == 0)
                return -1;
            var Eq = _context.EQUIPMENTs.Find(Id);
            if (Eq == null)
                return -1;
            var lifes = Eq.Life.Where(m => m.ActionDate <= date && m.Action.recordStatus == (int)RecordStatus.Available).OrderBy(field => field.ActionDate).ThenBy(m => m.Id);
            if (lifes.Count() > 0)
                return lifes.Last().SerialMeterReading;

            return longNullableToint(Eq.smu_at_start);
        }

        public int GetSerialMeterUnit(long equipmentId, DateTime date)
        {
            if (equipmentId == 0)
                return -1;
            var Eq = _context.EQUIPMENTs.Find(equipmentId);
            if (Eq == null)
                return -1;
            var lifes = Eq.Life.Where(m => m.ActionDate <= date && m.Action.recordStatus == (int)RecordStatus.Available).OrderBy(field => field.ActionDate).ThenBy(m => m.Id);
            if (lifes.Count() > 0)
                return lifes.Last().SerialMeterReading;

            return longNullableToint(Eq.smu_at_start);
        }
        /// <summary>
        /// Equipment purchase cost + current components cost + replaced components cost + actions cost
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public decimal GetEquipmentCost(DateTime date)
        {
            if (Id == 0)
                return -1;
            decimal cost = DALEquipment.purchase_cost == null ? 0 : (decimal)DALEquipment.purchase_cost;
            var components = GetEquipmentComponents();
            foreach (var comp in components)
            {
                cost += comp.cost == null ? 0 : (decimal)comp.cost;
            }
            var EqActioncosts = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Id && m.event_date <= date && m.recordStatus == 0);
            foreach (var cst in EqActioncosts)
            {
                cost += cst.cost;
            }
            var componentReplacements = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Id && m.recordStatus == 0 && m.event_date <= date && m.action_type_auto == 35).Select(m => m.equnit_auto).ToList();
            foreach (var oldComponentId in componentReplacements)
            {
                var c = _context.GENERAL_EQ_UNIT.Find(oldComponentId);
                if (c != null)
                    cost += c.cost == null ? 0 : (decimal)c.cost;
            }
            return cost;
        }

        public string getEquipmentSerialNo()
        {
            return DALEquipment == null ? "Unknown" : DALEquipment.serialno;
        }

        public string getEquipmentUnitNo()
        {
            return DALEquipment == null ? "Unknown" : DALEquipment.unitno;
        }

        /// <summary>
        /// Returns total cost of all components currently on the chain. (The cost entered at setup). 
        /// </summary>
        /// <returns></returns>
        public decimal GetEquipmentCurrentComponentsCost()
        {
            if (Id == 0)
                return -1;
            decimal cost = 0;
            var components = GetEquipmentComponents();
            foreach (var comp in components)
            {
                cost += comp.cost == null ? 0 : (decimal)comp.cost;
            }
            return cost;
        }
        /// <summary>
        /// returns all actions cost before or equal to the given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public decimal GetEquipmentActionsCost(DateTime date)
        {
            if (Id == 0)
                return -1;
            decimal cost = 0;
            var EqActioncosts = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Id && m.event_date <= date && m.recordStatus == 0);
            foreach (var cst in EqActioncosts)
            {
                cost += cst.cost;
            }
            return cost;
        }

        /// <summary>
        /// Returns the total cost of all actions recorded against this equipment within the given month, 
        /// this should not include the cost of the initial setup of a new equipment. Only the cost of repairs (other actions performed). 
        /// </summary>
        /// <param name="date">The date which we will take the month from to get costs for. </param>
        /// <returns>Total cost of actions within the given month</returns>
        public decimal GetEquipmentRepairsCostForGivenMonth(DateTime date)
        {
            if (Id == 0)
                return -1;
            var costs = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Id && m.event_date.Year == date.Year && m.event_date.Month == date.Month && m.recordStatus == 0).Select(m => m.cost);
            decimal totalCost = 0;
            foreach (var c in costs)
            {
                totalCost += c;
            }
            return totalCost;
        }

        /// <summary>
        /// Returns the total cost of all actions recorded against this equipment for the given date until current time, 
        /// this should not include the cost of the initial setup of a new equipment. Only the cost of repairs (other actions performed). 
        /// </summary>
        /// <param name="date">The date which we will take the month from to get costs for. </param>
        /// <returns>Total cost of actions within the given month</returns>
        public List<Widgets.EquipmentCost> GetEquipmentRepairsCostForGivenYear(DateTime date)
        {
            if (Id == 0)
                return null;
            return _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Id && m.event_date >= date && m.recordStatus == 0).Select(m => new Widgets.EquipmentCost()
            {
                Cost = m.cost,
                Date = m.event_date
            }).ToList();
        }

        /// <summary>
        /// returns equipment purchase cost + current components cost + replaced components cost + actions cost per hour 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public decimal GetEquipmentCostPerLifeHour(DateTime date)
        {
            if (Id == 0)
                return -1;
            int k = GetEquipmentLife(date);
            if (k != 0)
                return GetEquipmentCost(date) / k;
            return 0;
        }
        /// <summary>
        /// Returns current components cost per hour based on the life of the equipment on the given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public decimal GetEquipmentCurrentComponentsCostPerEquipmentLifeHour(DateTime date)
        {
            if (Id == 0)
                return -1;
            int k = GetEquipmentLife(date);
            if (k != 0)
                return GetEquipmentCurrentComponentsCost() / k;
            return 0;
        }
        /// <summary>
        /// Returns current components cost per hour based on the life of the component by itself on the given date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public decimal GetEquipmentCurrentComponentsCostPerComponentLifeHour(DateTime date)
        {
            if (Id == 0)
                return -1;
            decimal costPerHour = 0;
            var components = GetEquipmentComponents();
            foreach (var comp in components)
            {
                costPerHour += new Component(_context, longNullableToint(comp.equnit_auto)).GetComponentSetupCostPerHour(date);
            }
            return costPerHour;
        }
        /// <summary>
        /// Returns cost per hour for the all acions has been done on this equipment based on the equipment life
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public decimal GetEquipmentActionsCostPerEquipmentLifeHour(DateTime date)
        {
            if (Id == 0)
                return -1;
            decimal cost = GetEquipmentActionsCost(date);
            int life = GetEquipmentLife(date);
            if (life > 0)
                return cost / life;
            return cost;
        }

        public List<COMPART_TOOL_IMAGE> GetEquipmentCompartToolImageList()
        {
            List<COMPART_TOOL_IMAGE> resultList = new List<COMPART_TOOL_IMAGE>();
            if (Id == 0)
                return resultList;
            var components = GetEquipmentComponents();
            foreach (var cmp in components)
            {
                var k = _context.COMPART_TOOL_IMAGE.Where(m => m.CompartId == cmp.compartid_auto);
                resultList.AddRange(k);
            }
            return resultList.GroupBy(m => m.Id).Select(m => m.First()).ToList();
        }

        /// <summary>
        /// This method will be used in UndercarriageHistory Page of the old UI
        /// </summary>
        /// <param name="date"> all the return data will be just before or equal to this date </param>
        /// <returns></returns>
        public IEnumerable<EquipmentComponentHistoryOldViewModel> GetEquipmentComponentHistory(DateTime date, Side side)
        {
            if (Id == 0)
                return new List<EquipmentComponentHistoryOldViewModel>();
            var _template = getEquipmentHistoryTemplate(Id);
            List<EquipmentComponentHistoryOldViewModel> ComponentHistoryList = new List<EquipmentComponentHistoryOldViewModel>();
            foreach (var syst in _template.SystemsHistory.Where(m=> m.Side == side).OrderBy(m=> m._order))
            {
                UCSystem LogicalSystem = new UCSystem(_context, syst.Id);
                foreach (var comp in syst.ComponentsHistory.OrderBy(m=> m._order).ThenBy(m=>m.Pos))
                {
                    Component LogicalComponent = new Component(_context, longNullableToint(comp.Id));
                    if (LogicalComponent.Id != 0)
                    {
                        //Create new View Model and put component history
                        var historyRecord = new EquipmentComponentHistoryOldViewModel();
                        historyRecord.ComponentId = LogicalComponent.Id;
                        historyRecord.Serialno = LogicalSystem.DALSystem != null ? LogicalSystem.DALSystem.Serialno : "";
                        historyRecord.comparttype = LogicalComponent.Compart.Id != 0 ? LogicalComponent.Compart.DALType.comparttype : "";
                        historyRecord.equnit_auto = LogicalComponent.Id;
                        historyRecord.budget_life = LogicalComponent.DALComponent == null ? 0 : (int)LogicalComponent.DALComponent.track_budget_life;
                        historyRecord.compart = LogicalComponent.Compart.Id != 0 ? LogicalComponent.Compart.DALCompart.compart : "";
                        historyRecord.compartid = LogicalComponent.Compart.Id != 0 ? LogicalComponent.Compart.DALCompart.compartid : "";
                        historyRecord.pos = (LogicalComponent.DALComponent != null && LogicalComponent.DALComponent.pos != null) ? (byte)LogicalComponent.DALComponent.pos : byte.MinValue;
                        historyRecord.lifeAfterInstallation = LogicalComponent.GetComponentLife(date) - longNullableToint(LogicalComponent.DALComponent.cmu);
                        historyRecord.worn = LogicalComponent.GetComponentWorn(date);
                        historyRecord.cmu = LogicalComponent.GetComponentLife(date);
                        historyRecord.projectedHours = LogicalComponent.GetProjectedHours(date);
                        historyRecord.setup_cost = (LogicalComponent.DALComponent != null && LogicalComponent.DALComponent.cost != null) ? (decimal)LogicalComponent.DALComponent.cost : 0;
                        historyRecord.isChildComponent = LogicalComponent.isAChildBasedOnCompart();
                        
                        historyRecord.total_cost = LogicalComponent.GetComponentHistoryCost(date, comp.Id, Id);
                        historyRecord.side = (LogicalComponent.DALComponent == null || LogicalComponent.DALComponent.side == null || LogicalComponent.DALComponent.side == 0) ? Side.Unknown : ((LogicalComponent.DALComponent.side == 1) ? Side.Left : Side.Right);
                        ComponentHistoryList.Add(historyRecord);
                    }
                }
            }
            return ComponentHistoryList;
        }
        /// <summary>
        /// This method returns EquipmentFamily which is loading from Type table in database 
        /// </summary>
        /// <returns></returns>
        public EquipmentFamily GetEquipmentFamily()
        {
            if (Id == 0 || DALEquipment == null)
                return EquipmentFamily.Unknown;
            var EquipmentMMTA = _context.LU_MMTA.Find(DALEquipment.mmtaid_auto);
            if (EquipmentMMTA == null)
                return EquipmentFamily.Unknown;
            try
            {
                return (EquipmentFamily)EquipmentMMTA.type_auto;
            }
            catch
            {
                return EquipmentFamily.Unknown;
            }
        }
        /// <summary>
        /// Returns a string of the family for the family parameter
        /// </summary>
        /// <param name="Family"></param>
        /// <returns></returns>
        public string GetFamilyName(EquipmentFamily Family)
        {
            switch (Family)
            {
                case EquipmentFamily.Rope_Shovel:
                    return "RSH Rope Shovel";
                case EquipmentFamily.Dump_Body:
                    return "DUMP Dump Body";
                case EquipmentFamily.Off_Highway_Truck:
                    return "OHT Off Highway Truck";
                case EquipmentFamily.Underground_Loader:
                    return "LOAD Underground Loader";
                case EquipmentFamily.CH_Cane_Harvesters:
                    return "CH Cane Harvesters";
                case EquipmentFamily.DOZO_Oval_Config_Dozer:
                    return "DOZO Oval Config Dozer";
                case EquipmentFamily.DOZ_Dozer_TTT_Elevated_Sprocket:
                    return "DOZ Dozer TTT Elevated Sprocket";
                case EquipmentFamily.DRI_Drill_Rig:
                    return "DRI Drill Rig";
                case EquipmentFamily.EXC_Excavator:
                    return "EXC Excavator";
                case EquipmentFamily.FP_Forestry_Product:
                    return "FP Forestry Product";
                case EquipmentFamily.MEX_Mining_Shovel:
                    return "MEX Mining Shovel";
                case EquipmentFamily.PIP_Pipelayer:
                    return "PIP Pipelayer";
                case EquipmentFamily.TRAC_Crawler_Tractor:
                    return "TRAC Crawler Tractor";
                default:
                    var fam = _context.TYPEs.Where(t => t.type_auto == (int)Family).FirstOrDefault();
                    if (fam != null)
                        return fam.typedesc;
                    return "Unknown";
            }
        }
        /// <summary>
        /// This is a Temporary method for mining shovel
        /// This method should be call after insert or update inspection and it will update parent components to 
        /// the worst child component worn percentage and eval code!
        /// TT-92 Mason 23 Aug 2017
        /// </summary>
        /// <param name="InspectionId">Inspection Id which will be updated</param>
        /// <returns></returns>
        public void UpdateMiningShovelInspectionParentsFromChildren(int InspectionId)
        {
            if (Id == 0 || InspectionId < 1)
                return;
            var Inspection = _context.TRACK_INSPECTION.Find(InspectionId);
            if (Inspection == null)
                return;
            var TIDList = Inspection.TRACK_INSPECTION_DETAIL.ToList();
            foreach (var Tid in TIDList)
            {
                var currentCompartChilds = new Compart(_context, Tid.GENERAL_EQ_UNIT.compartid_auto).getChildComparts();
                if (currentCompartChilds.Count() > 0)
                {
                    int side = Tid.GENERAL_EQ_UNIT.side == null ? 0 : (int)Tid.GENERAL_EQ_UNIT.side;
                    var TidsSameSide = TIDList.Where(m => m.SIDE.Side == side);
                    var AllCompartsSameSide = TidsSameSide.Select(m => m.GENERAL_EQ_UNIT.LU_COMPART);
                    var currentTidChilds = TidsSameSide.Where(m => currentCompartChilds.Any(k => k.compartid_auto == m.GENERAL_EQ_UNIT.compartid_auto)).ToList();
                    decimal wornOverall = 0;
                    char evalOverAll = 'A';
                    int remainingHoursOverall = Tid.remaining_hours == null ? 0 : (int)Tid.remaining_hours;
                    int remainingHoursExtOverall = Tid.ext_remaining_hours == null ? 0 : (int)Tid.ext_remaining_hours;
                    int projectedHoursOverall = Tid.projected_hours == null ? 0 : (int)Tid.projected_hours;
                    int projectedHoursExtOverall = Tid.ext_projected_hours == null ? 0 : (int)Tid.ext_projected_hours;
                    foreach (var child in currentTidChilds)
                    {
                        if (child.worn_percentage > wornOverall)
                            wornOverall = child.worn_percentage;
                        if (child.eval_code != null && child.eval_code.Length > 0 && child.eval_code.ToCharArray()[0] > evalOverAll)
                            evalOverAll = child.eval_code.ToCharArray()[0];
                        if (child.remaining_hours != null && child.remaining_hours < remainingHoursOverall)
                            remainingHoursOverall = (int)child.remaining_hours;
                        if (child.ext_remaining_hours != null && child.ext_remaining_hours < remainingHoursExtOverall)
                            remainingHoursExtOverall = (int)child.ext_remaining_hours;
                        if (child.projected_hours != null && child.projected_hours < projectedHoursOverall)
                            projectedHoursOverall = (int)child.projected_hours;
                        if (child.ext_projected_hours != null && child.ext_projected_hours < projectedHoursExtOverall)
                            projectedHoursExtOverall = (int)child.ext_projected_hours;
                    }
                    Tid.eval_code = evalOverAll.ToString();
                    Tid.worn_percentage = wornOverall;
                    Tid.remaining_hours = remainingHoursOverall;
                    Tid.ext_remaining_hours = remainingHoursExtOverall;
                    Tid.projected_hours = projectedHoursOverall;
                    Tid.ext_projected_hours = projectedHoursExtOverall;
                    _context.MarkAsModified(Tid);
                }
            }
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e1)
            {
                string Message = e1.Message;
            }
        }

        public List<CompartWornExtViewModel> getWornLimitList()
        {
            List<CompartWornExtViewModel> result = new List<CompartWornExtViewModel>();
            if (Id == 0 || DALEquipment == null)
                return result;
            var mmta = _context.LU_MMTA.Find(DALEquipment.mmtaid_auto);
            if (mmta == null)
                return result;
            var components = GetEquipmentComponents();
            foreach (var DALcomp in components.GroupBy(m => m.compartid_auto).Select(m => m.First()))
            {
                var LogicalCompart = new Compart(_context, DALcomp.compartid_auto);
                if (LogicalCompart.Id != 0)
                    result.AddRange(LogicalCompart.getCompartWornDataAllMethods());
            }
            return result;
        }
        /// <summary>
        /// Returns a model of current installed systems for this equipment
        /// </summary>
        /// <returns></returns>
        public EquipmentSystemsExistence getSystemsInstallationStatus()
        {
            EquipmentSystemsExistence result = new EquipmentSystemsExistence
            {
                LeftFrame = false,
                LeftChain = false,
                RightFrame = false,
                RightChain = false
            };
            if (Id == 0 || DALEquipment == null || DALEquipment.UCSystems == null || DALEquipment.UCSystems.Count == 0)
                return result;
            var systemIds = DALEquipment.UCSystems.Select(m => m.Module_sub_auto).ToList();
            result.LeftChain = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == Id && m.side == (int)Side.Left && m.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Link && systemIds.Any(s => m.module_ucsub_auto == s)).Count() > 0;
            result.RightChain = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == Id && m.side == (int)Side.Right && m.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Link && systemIds.Any(s => m.module_ucsub_auto == s)).Count() > 0;
            result.LeftFrame = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == Id && m.side == (int)Side.Left && m.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Idler && systemIds.Any(s => m.module_ucsub_auto == s)).Count() > 0;
            result.RightFrame = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == Id && m.side == (int)Side.Right && m.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Idler && systemIds.Any(s => m.module_ucsub_auto == s)).Count() > 0;
            return result;
        }

        public List<SystemDetailsViewModel> getSystemDetailsList(DateTime date)
        {
            List<SystemDetailsViewModel> result = new List<SystemDetailsViewModel>();
            if (Id == 0 || DALEquipment == null || DALEquipment.UCSystems == null || DALEquipment.UCSystems.Count == 0)
                return result;
            var systems = DALEquipment.UCSystems.ToList();
            foreach (var ucSystem in systems)
            {
                result.Add(new UCSystem(_context, longNullableToint(ucSystem.Module_sub_auto)).getSystemDetails(date));
            }
            return result;
        }

        public MakeModelFamily getMakeModelFamily()
        {
            var make = GetEquipmentMake();
            var model = GetEquipmentModel();
            var family = GetEquipmentFamily();


            return new MakeModelFamily
            {
                Id = Id,
                Model = new ModelForSelectionVwMdl
                {
                    Id = model.Id,
                    Title = model.Description,
                    FamilyId = (int)family,
                    MakeId = make.Id
                },
                Family = new FamilyForSelectionVwMdl
                {
                    Id = (int)family,
                    Symbol = GetFamilyName(family),
                    Title = GetFamilyName(family)
                },
                Make = new MakeForSelectionVwMdl
                {
                    Id = make.Id,
                    Symbol = make.Symbol,
                    Title = make.Description
                }
            };
        }
        public async Task<MakeModelFamily> getMakeModelFamilyAsync()
        {
            return await Task.Run(() => getMakeModelFamily());
        }

        public async Task<List<SystemDetailsViewModel>> getSystemDetailsListAsync(DateTime date)
        {
            return await Task.Run(() => getSystemDetailsList(date));
        }
        public List<ComponentWornVwMdl> getEquipmentComponentsWorn(DateTime date)
        {
            var results = new List<ComponentWornVwMdl>();
            if (Id == 0)
                return results;
            var components = GetEquipmentComponents();
            foreach (var cmp in components)
            {
                var logicalComponent = new BLL.Core.Domain.Component(_context, cmp.equnit_auto.LongNullableToInt());
                if (logicalComponent.Id != 0)
                {
                    var result = new ComponentWornVwMdl
                    {
                        Id = logicalComponent.Id,
                        side = logicalComponent.GetComponentSide(),
                        wornPercentage = logicalComponent.GetComponentWorn(date)
                    };
                    char eval = '-';
                    logicalComponent.GetEvalCodeByWorn(result.wornPercentage, out eval);
                    result.Eval = eval.ToString();
                    results.Add(result);
                }
            }
            return results;
        }
        /// <summary>
        /// This method is an improved version of the previous one
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public List<ComponentWornVwMdl> getEquipmentComponentsWornVwMdl(DateTime date)
        {
            var results = new List<ComponentWornVwMdl>();
            if (Id == 0)
                return results;
            var lastInspection = _context.TRACK_INSPECTION.Where(m => m.equipmentid_auto == Id && m.inspection_date <= date).OrderByDescending(m => m.inspection_date).FirstOrDefault();
            if (lastInspection == null)
                return results;
            return lastInspection.TRACK_INSPECTION_DETAIL.Select(m => new ComponentWornVwMdl { Id = (int)m.track_unit_auto, side = m.SIDE != null ? (Side)m.SIDE.Side : (Side)m.Side, wornPercentage = m.worn_percentage, Eval = m.eval_code }).ToList();
        }

        protected decimal ConvertFrom(MeasurementType from, decimal reading)
        {
            if (from == MeasurementType.Milimeter)
                return reading * (decimal)(0.0393701);
            return reading * (decimal)25.4;
        }

        protected string giveMeRandomString(string allowedChars, int minLength, int maxLength, Random rng)
        {
            char[] chars = new char[maxLength];
            int setLength = allowedChars.Length;
            int length = rng.Next(minLength, maxLength + 1);

            for (int i = 0; i < length; ++i)
            {
                chars[i] = allowedChars[rng.Next(setLength)];
            }

            return new string(chars, 0, length);
        }

        public TRACK_INSPECTION GetLatestInspection(DateTime date)
        {
            if (Id == 0)
                return null;
            var inspections = _context.TRACK_INSPECTION.Where(m => m.equipmentid_auto == Id && m.inspection_date <= date);
            if (inspections.Count() == 0)
                return null;
            return inspections.OrderByDescending(m => m.inspection_date).First();
        }

        public char toEvalChar(EvalCode code)
        {
            if (code == EvalCode.A) return 'A';
            if (code == EvalCode.B) return 'B';
            if (code == EvalCode.C) return 'C';
            if (code == EvalCode.X) return 'X';
            return 'U';
        }

        public EvalCode toEvalCode(string eval)
        {
            if (eval == null) return EvalCode.U;
            if (eval.ToUpper() == "A") return EvalCode.A;
            if (eval.ToUpper() == "B") return EvalCode.B;
            if (eval.ToUpper() == "C") return EvalCode.C;
            if (eval.ToUpper() == "X") return EvalCode.X;
            return EvalCode.U;
        }

        public EqMakeVwMdl GetEquipmentMake()
        {
            var Result = new EqMakeVwMdl { Id = 662, Symbol = "UN", Description = "UNKNOWN" };
            if (Id == 0)
                return Result;
            if (DALEquipment != null && DALEquipment.LU_MMTA != null && DALEquipment.LU_MMTA.MAKE != null)
                return new EqMakeVwMdl { Id = DALEquipment.LU_MMTA.MAKE.make_auto, Symbol = DALEquipment.LU_MMTA.MAKE.makeid, Description = DALEquipment.LU_MMTA.MAKE.makedesc };
            if (DALEquipment != null && DALEquipment.LU_MMTA != null)
            {
                var k = _context.MAKE.Find(DALEquipment.LU_MMTA.make_auto);
                if (k != null)
                    return new EqMakeVwMdl { Id = k.make_auto, Symbol = k.makeid, Description = k.makedesc };
            }
            var p = _context.LU_MMTA.Find(DALEquipment.mmtaid_auto);
            if (p == null)
                return Result;
            var u = _context.MAKE.Find(p.make_auto);
            if (u == null)
                return Result;
            return new EqMakeVwMdl { Id = u.make_auto, Symbol = u.makeid, Description = u.makedesc };
        }
        public EqModelVwMdl GetEquipmentModel()
        {
            var Result = new EqModelVwMdl { Id = 6382, Symbol = "UN", Description = "UNKNOWN" };
            if (Id == 0)
                return Result;
            if (DALEquipment != null && DALEquipment.LU_MMTA != null && DALEquipment.LU_MMTA.MODEL != null)
                return new EqModelVwMdl { Id = DALEquipment.LU_MMTA.MODEL.model_auto, Symbol = DALEquipment.LU_MMTA.MODEL.modelid, Description = DALEquipment.LU_MMTA.MODEL.modeldesc };
            if (DALEquipment != null && DALEquipment.LU_MMTA != null)
            {
                var k = _context.MODELs.Find(DALEquipment.LU_MMTA.model_auto);
                if (k != null)
                    return new EqModelVwMdl { Id = k.model_auto, Symbol = k.modelid, Description = k.modeldesc };
            }
            var p = _context.LU_MMTA.Find(DALEquipment.mmtaid_auto);
            if (p == null)
                return Result;
            var u = _context.MODELs.Find(p.model_auto);
            if (u == null)
                return Result;
            return new EqModelVwMdl { Id = u.model_auto, Symbol = u.modelid, Description = u.modeldesc };
        }
        public List<EqActionsVwMdl> GetEquipmentActions()
        {
            List<EqActionsVwMdl> result = new List<EqActionsVwMdl>();
            if (Id == 0)
                return result;
            return _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == Id && m.recordStatus == 0).Select(m => new EqActionsVwMdl
            {
                Id = longNullableToint(m.history_id),
                Comment = m.comment,
                ComponentId = longNullableToint(m.equnit_auto),
                Cost = m.cost
                ,
                Date = m.event_date,
                DateAsStr = m.event_date.ToString("dd-MMM-YYYY"),
                EquipmentId = Id,
                LTD = m.equipment_ltd,
                recordStatus = m.recordStatus,
                SMU = m.equipment_smu,
                SystemId = longNullableToint(m.system_auto_id),
                Type = m.action_type_auto.ToActionType(),
                TypeAsString = Enum.GetName(typeof(ActionType), (int)m.action_type_auto.ToActionType()),
            }
            ).ToList();
        }

        public DAL.CRSF getEquipmentJobSite()
        {
            if (Id == 0)
                return null;
            return _context.CRSF.Find(DALEquipment.crsf_auto);
        }

        public DAL.CRSF getEquipmentJobSite(int EquipmentId)
        {
            if (getDALEquipment(EquipmentId) == null) return null;
            return _context.CRSF.Find(getDALEquipment(EquipmentId).crsf_auto);
        }


        public List<ComponentOverViewVwMdl> GetEquipmentComponentOverView()
        {
            var results = new List<ComponentOverViewVwMdl>();
            if (Id == 0)
                return results;
            var components = GetEquipmentComponents();
            foreach (var cmp in components)
            {
                var logicalComponent = new BLL.Core.Domain.Component(_context, cmp.equnit_auto.LongNullableToInt(), false);
                if (logicalComponent.Id == 0)
                    continue;
                int compCMU = logicalComponent.GetComponentLife(DateTime.Now);
                decimal compWorn = logicalComponent.GetComponentWorn(DateTime.Now);
                char compEval = 'X';
                logicalComponent.GetEvalCodeByWorn(compWorn, out compEval);
                var result = new ComponentOverViewVwMdl
                {
                    Id = cmp.equnit_auto.LongNullableToInt(),
                    CompartId = logicalComponent.DALComponent.compartid_auto,
                    CompartStr = logicalComponent.DALComponent.LU_COMPART.compart,
                    CompartPart = logicalComponent.DALComponent.LU_COMPART.compartid,
                    CompartTypeId = logicalComponent.DALComponent.LU_COMPART.comparttype_auto,
                    CompartTypeStr = logicalComponent.DALComponent.LU_COMPART.LU_COMPART_TYPE.comparttype,
                    CMU = compCMU,
                    RemainingLife100 = logicalComponent.GetProjectedHours(DateTime.Now) - compCMU,
                    RemainingLife120 = (int)(logicalComponent.GetProjectedHours(DateTime.Now) * 1.2) - compCMU,
                    ComponentInstallationDate = logicalComponent.DALComponent.date_installed == null ? DateTime.MinValue : (DateTime)logicalComponent.DALComponent.date_installed,
                    ComponentInstallationDateStr = logicalComponent.DALComponent.date_installed == null ? DateTime.MinValue.ToString("dd MMM yyyy") : ((DateTime)logicalComponent.DALComponent.date_installed).ToString("dd MMM yyyy"),
                    CmuAtInstall = logicalComponent.DALComponent.cmu.LongNullableToInt(),
                    EquipmentSMU = logicalComponent.DALComponent.eq_smu_at_install.LongNullableToInt(),
                    Side = (int)logicalComponent.GetComponentSide(),
                    EvalCode = compEval.ToString(),
                    SystemId = logicalComponent.DALComponent.module_ucsub_auto.LongNullableToInt(),
                    SystemSerial = logicalComponent.GetSystemSerial(logicalComponent.DALComponent.module_ucsub_auto.LongNullableToInt()),
                    WornPercentage = compWorn,
                    EquipmentId = Id
                };
                results.Add(result);
            }
            return results;
        }

        public List<GENERAL_EQ_UNIT> GetEquipmentComponents()
        {
            if (Id == 0)
                return new List<GENERAL_EQ_UNIT>();
            if (DALComponents != null)
                return DALComponents.ToList();
            DALComponents = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == Id).OrderBy(m => m.LU_COMPART.LU_COMPART_TYPE.sorder).ThenBy(m => m.pos).ToList();
            return DALComponents.ToList();
        }

        public IEnumerable<EQUIPMENT_LIFE> GetEquipmentAvailableLives()
        {
            var result = new List<EQUIPMENT_LIFE>();
            if (Id == 0)
                return result.AsEnumerable();
            return _context.EQUIPMENT_LIVES.Where(m => m.EquipmentId == Id && m.Action.recordStatus == (int)RecordStatus.Available);
        }

        public List<ComponentTableInspectionViewModel> getEquipmentsComponentView(List<IdAndDate> Ids)
        {
            var result = new List<ComponentTableInspectionViewModel>();
            var currentIds = Ids.Select(m => m.Id).ToList();

            var lastInspections = _context.TRACK_INSPECTION.Where(m => currentIds.Any(id => id == m.equipmentid_auto)).GroupBy(m => m.equipmentid_auto, (key, group) => group.OrderByDescending(k => k.inspection_date).FirstOrDefault());

            var equipments = _context.EQUIPMENTs.Where(m => currentIds.Any(id => id == m.equipmentid_auto) && lastInspections.Any(k => k.equipmentid_auto == m.equipmentid_auto));
            var jobsites = _context.CRSF.Where(m => equipments.Any(k => k.crsf_auto == m.crsf_auto));

            var lastInspectionQuoteIds = lastInspections.Select(m => m.quote_auto).Where(m => m.HasValue);
            //            var componentIds = _context.TRACK_INSPECTION_DETAIL.Where(m => lastInspections.Any(k => k.inspection_auto == m.inspection_auto)).Select(m => m.track_unit_auto);
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => lastInspections.Any(k => k.inspection_date <= m.event_date && k.equipmentid_auto == m.equipmentid_auto));

            var quotes = _context.TRACK_QUOTE.Where(m => lastInspectionQuoteIds.Any(k => k == m.quote_auto));
            var quoteDetails = _context.TRACK_QUOTE_DETAIL.Where(m => lastInspectionQuoteIds.Any(k => k == m.quote_auto)).GroupBy(m => new { m.track_unit_auto, m.op_type_auto }, (key, group) => group.FirstOrDefault());
            List<ComponentActionViewModel> recommendedActions = new List<ComponentActionViewModel>();
            foreach (var k in quoteDetails.ToList())
            {
                recommendedActions.Add(
                    new ComponentActionViewModel
                    {
                        Id = k.quote_detail_auto,
                        ComponentId = int.Parse(k.track_unit_auto),
                        ActionType = k.op_type_auto,
                        Date = k.created_date ?? DateTime.Now,
                        Title = k.created_user
                    });
            }


            var equipmentViewModel = equipments.Select(m => new EquipmentViewModel
            {
                Id = (int)m.equipmentid_auto,
                Serial = m.serialno,
                Unit = m.unitno,
                Life = (int)(m.currentsmu ?? 0),
                SMU = (int)(m.currentsmu ?? 0),
                Customer = new CustomerForSelectionVwMdl
                {
                    Id = (int)jobsites.Where(k => k.crsf_auto == m.crsf_auto).FirstOrDefault().customer_auto,
                    Title = jobsites.Where(k => k.crsf_auto == m.crsf_auto).FirstOrDefault().Customer.cust_name
                },
                JobSite = new JobSiteForSelectionVwMdl
                {
                    Id = (int)m.crsf_auto,
                    Title = jobsites.Where(k => k.crsf_auto == m.crsf_auto).FirstOrDefault().site_name,
                    CustomerId = (int)jobsites.Where(k => k.crsf_auto == m.crsf_auto).FirstOrDefault().customer_auto
                },
                MakeModelFamily = new MakeModelFamily
                {
                    Id = (int)m.equipmentid_auto,
                    Family = new FamilyForSelectionVwMdl
                    {
                        Id = m.LU_MMTA.type_auto,
                        Symbol = m.LU_MMTA.TYPE.typeid,
                        Title = m.LU_MMTA.TYPE.typedesc
                    },
                    Make = new MakeForSelectionVwMdl
                    {
                        Id = m.LU_MMTA.make_auto,
                        Symbol = m.LU_MMTA.MAKE.makeid,
                        Title = m.LU_MMTA.MAKE.makedesc
                    },
                    Model = new ModelForSelectionVwMdl
                    {
                        Id = m.LU_MMTA.model_auto,
                        Title = m.LU_MMTA.MODEL.modeldesc,
                        FamilyId = m.LU_MMTA.type_auto,
                        MakeId = m.LU_MMTA.make_auto
                    }
                }
            });

            var InspectionViewModel = lastInspections.ToList().Select(m => new InspectionViewModel
            {
                Id = m.inspection_auto,
                Date = m.inspection_date,
                SMU = m.smu ?? 0,
                EquipmentId = (int)m.equipmentid_auto,
                QuoteId = m.quote_auto ?? 0,
                RecommendedActions = recommendedActions.Where(k => m.TRACK_INSPECTION_DETAIL.Any(t => t.track_unit_auto == k.ComponentId)).ToList()
            }).ToList();


            //var lastInspectionIds = lastInspections.Select(m => m.inspection_auto);
            var tids = _context.TRACK_INSPECTION_DETAIL.Where(m => lastInspections.Any(k => k.inspection_auto == m.inspection_auto));
            var components = tids.ToList().Select(k => new ComponentViewViewModel
            {
                Id = k.inspection_detail_auto,
                Compart = new CompartV
                { Id = k.GENERAL_EQ_UNIT.compartid_auto, CompartNote = k.GENERAL_EQ_UNIT.compart_note, CompartStr = k.GENERAL_EQ_UNIT.compartsn, CompartTitle = k.GENERAL_EQ_UNIT.compart_descr, CompartType = new CompartTypeV { Id = k.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto, Order = k.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.sorder ?? 10, Title = k.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.comparttype }, DefaultBudgetLife = (int)(k.GENERAL_EQ_UNIT.LU_COMPART.expected_life ?? 0), MeasurementPointsNo = k.GENERAL_EQ_UNIT.LU_COMPART.PARENT_RELATION_LIST.Count(), Model = new ModelForSelectionVwMdl { Id = k.TRACK_INSPECTION.EQUIPMENT.LU_MMTA.model_auto, FamilyId = k.TRACK_INSPECTION.EQUIPMENT.LU_MMTA.type_auto, MakeId = k.TRACK_INSPECTION.EQUIPMENT.LU_MMTA.make_auto, Title = k.TRACK_INSPECTION.EQUIPMENT.LU_MMTA.MODEL.modeldesc } },
                Worn = k.worn_percentage,
                Date = k.TRACK_INSPECTION.inspection_date,
                Life = k.hours_on_surface ?? 0,
                Side = (Side)(k.GENERAL_EQ_UNIT.side ?? 0),
                EquipmentId = (int)(k.TRACK_INSPECTION.equipmentid_auto),
                Position = (k.GENERAL_EQ_UNIT.pos ?? 1) == 0 ? 1 : (k.GENERAL_EQ_UNIT.pos ?? 1),
                Actions = actions.Where(q => q.equnit_auto == k.track_unit_auto).Select(q => new ComponentActionViewModel { Id = (int)q.history_id, ActionType = (q.action_type_auto), ComponentId = (int)q.equnit_auto, Date = q.event_date, Title = q.comment }).ToList()
            }).ToList();

            foreach (var equipment in equipmentViewModel.ToList())
            {
                result.Add(new ComponentTableInspectionViewModel
                {
                    Equipment = equipment,
                    LastInspection = InspectionViewModel.Where(m => m.EquipmentId == equipment.Id).FirstOrDefault() ?? new InspectionViewModel(),
                    Components = components.Where(m => m.EquipmentId == equipment.Id).AsQueryable(),
                    Id = equipment.Id
                });
            }

            return result.OrderByDescending(m => m.LastInspection.Date).ToList();
        }
        public List<ReclacCmuInspectionHistory> GetInspectionHistoryForRecalcCmu()
        {
            var res = _context.TRACK_INSPECTION.Where(i => i.equipmentid_auto == pvId).Select(i => new
            {
                DateOfInspection = i.inspection_date,
                SmuAtInspection = i.smu == null ? 0 : (int)i.smu,
                InspectionNumber = 0
            }).ToList();

            return res.Select(i => new ReclacCmuInspectionHistory()
            {
                DateOfInspection = i.DateOfInspection.ToShortDateString(),
                InspectionNumber = 0,
                SmuAtInspection = i.SmuAtInspection
            }).ToList();
        }

        public List<ReclacCmuComponentRecord> GetComponentsWithCalculatedCmu()
        {
            var components = _context.GENERAL_EQ_UNIT.Where(c => c.equipmentid_auto == pvId).Where(c => c.LU_COMPART.CHILD_RELATION_LIST.Count == 0).ToList();
            var inspections = _context.TRACK_INSPECTION.Where(i => i.equipmentid_auto == pvId).OrderBy(i => i.inspection_date).ToList();
            if (inspections.Count != 2)
                return null;

            var inspection1Details = inspections[0].TRACK_INSPECTION_DETAIL.ToList();
            var inspection2Details = inspections[1].TRACK_INSPECTION_DETAIL.ToList();
            List<ReclacCmuComponentRecord> returnList = new List<ReclacCmuComponentRecord>();
            double averageChainL = 0;
            int averageChainCountL = 0;
            double averageFrameL = 0;
            int averageFrameCountL = 0;
            double averageIdlerL = 0;
            int averageIdlerCountL = 0;

            double averageChainR = 0;
            int averageChainCountR = 0;
            double averageFrameR = 0;
            int averageFrameCountR = 0;
            double averageIdlerR = 0;
            int averageIdlerCountR = 0;

            components.ForEach(c =>
            {
                var component = new BLL.Core.Domain.Component(new UndercarriageContext(), Convert.ToInt32(c.equnit_auto));
                var cmuAtSetup = c.Life.OrderBy(l => l.ActionDate).Select(l => l.ActualLife).FirstOrDefault();
                if (c.Life.Count() > 0)
                {
                    var firstDate = c.Life.OrderBy(l => l.ActionDate).Select(l => l.ActionDate).FirstOrDefault();
                    cmuAtSetup = c.Life.Where(m => m.ActionDate.Date == firstDate.Date).Select(m => m.ActualLife).Max();
                }
                else
                {
                    cmuAtSetup = c.cmu.LongNullableToInt();
                }
                var position = component.GetPositionLabel();
                var worn1 = inspection1Details.Where(d => d.track_unit_auto == c.equnit_auto).Select(d => d.worn_percentage).FirstOrDefault();
                var worn2 = inspection2Details.Where(d => d.track_unit_auto == c.equnit_auto).Select(d => d.worn_percentage).FirstOrDefault();
                var smu1 = inspections[0].smu;
                var smu2 = inspections[1].smu;
                var wornDifference = worn2 - worn1;
                var calculatedCmu = 0.00;

                if (worn2 - worn1 != 0)
                    calculatedCmu = Math.Round(Convert.ToDouble(((smu2 - smu1) * worn1) / (worn2 - worn1)));

                var sortOrder = (int)c.LU_COMPART.LU_COMPART_TYPE.sorder;

                if (wornDifference > 0)
                {
                    // Compute averages as we go through the components
                    switch (c.LU_COMPART.LU_COMPART_TYPE.comparttype)
                    {

                        case "Link":
                        case "Bushing":
                        case "Shoe":
                            if (c.side == (int)Side.Left)
                            {
                                averageChainL += calculatedCmu;
                                averageChainCountL++;
                            }
                            else
                            {
                                averageChainR += calculatedCmu;
                                averageChainCountR++;
                            }
                            break;
                        case "Idler":
                            if (c.side == (int)Side.Left)
                            {
                                averageIdlerL += calculatedCmu;
                                averageIdlerCountL++;
                            }
                            else
                            {
                                averageIdlerR += calculatedCmu;
                                averageIdlerCountR++;
                            }
                            break;
                        case "Sprocket":
                        case "Carrier Roller":
                        case "Track Roller":
                        case "Guard":
                        case "Track Elongation":
                            if (c.side == (int)Side.Left)
                            {
                                averageFrameL += calculatedCmu;
                                averageFrameCountL++;
                            }
                            else
                            {
                                averageFrameR += calculatedCmu;
                                averageFrameCountR++;
                            }
                            break;
                    }
                }

                returnList.Add(new ReclacCmuComponentRecord()
                {
                    ComponentName = c.LU_COMPART.LU_COMPART_TYPE.comparttype,
                    AverageCmuForComponentSystem = 0,
                    CalculatedCmu = calculatedCmu,
                    CmuAtSetup = cmuAtSetup,
                    ComponentId = c.equnit_auto,
                    PercentWornDifference = Math.Round(wornDifference, 1),
                    PercentWornFirstInspection = Math.Round(worn1, 1),
                    PercentWornSecondInspection = Math.Round(worn2, 1),
                    Position = position,
                    Side = (byte)c.side,
                    SortOrder = sortOrder
                });
            });
            returnList.ForEach(i =>
            {
                switch (i.ComponentName)
                {
                    case "Link":
                    case "Bushing":
                    case "Shoe":
                        if (i.Side == (int)Side.Left)
                        {
                            i.AverageCmuForComponentSystem = averageChainCountL == 0 ? 0 : Math.Round(averageChainL / averageChainCountL);
                        }
                        else
                        {
                            i.AverageCmuForComponentSystem = averageChainCountR == 0 ? 0 : Math.Round(averageChainR / averageChainCountR);
                        }
                        break;
                    case "Idler":
                        if (i.Side == (int)Side.Left)
                        {
                            i.AverageCmuForComponentSystem = averageIdlerCountL == 0 ? 0 : Math.Round(averageIdlerL / averageIdlerCountL);
                        }
                        else
                        {
                            i.AverageCmuForComponentSystem = averageIdlerCountR == 0 ? 0 : Math.Round(averageIdlerR / averageIdlerCountR);
                        }
                        break;
                    case "Sprocket":
                    case "Carrier Roller":
                    case "Track Roller":
                    case "Guard":
                    case "Track Elongation":
                        if (i.Side == (int)Side.Left)
                        {
                            i.AverageCmuForComponentSystem = averageFrameCountL == 0 ? 0 : Math.Round(averageFrameL / averageFrameCountL);
                        }
                        else
                        {
                            i.AverageCmuForComponentSystem = averageFrameCountR == 0 ? 0 : Math.Round(averageFrameR / averageFrameCountR);
                        }
                        break;
                }
            });
            var res = returnList.OrderBy(l => l.SortOrder).ToList();
            return res;
        }

        public async Task<Byte[]> GetEquipmentImageAsync()
        {
            if (DALEquipment.EquipmentPhoto != null)
                return DALEquipment.EquipmentPhoto;
            var model = new BLL.Core.Domain.UCModel(_context);
            return await model.getModelImageAsync(pvDALEquipment.LU_MMTA.model_auto);
        }

        public Byte[] GetEquipmentImage()
        {
            if (DALEquipment != null && DALEquipment.EquipmentPhoto != null && DALEquipment.EquipmentPhoto.Length > 0)
                return DALEquipment.EquipmentPhoto;
            var model = new BLL.Core.Domain.UCModel(_context);
            if (DALEquipment != null)
                return model.getModelImage(pvDALEquipment.LU_MMTA.model_auto);

            return model.getModelImage(0);
        }

        /// <summary>
        /// This is for test only. Don't use this method for adding a new equipment!
        /// </summary>
        /// <param name="equipment"></param>
        /// <returns></returns>
        public EQUIPMENT AddNewEuipment(DAL.EQUIPMENT equipment)
        {
            _context.EQUIPMENTs.Add(equipment);
            try
            {
                _context.SaveChanges();
                return equipment;
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                return null;
            }
        }

        public int getEquipmentId()
        {
            return Id;
        }

        public Core.ViewModel.EquipmentViewModel getEquipmentForInspection()
        {
            return new EquipmentViewModel
            {
                Id = getEquipmentId(),
                Customer = getDALCustomer() == null ? new CustomerForSelectionVwMdl { Id = 0, Title = "Unknown" } : new CustomerForSelectionVwMdl { Id = (int)getDALCustomer().customer_auto, Title = getDALCustomer().cust_name },
                JobSite = getEquipmentJobSite() == null ? new JobSiteForSelectionVwMdl { Id = 0, Title = "Unknown", CustomerId = 0 } : new JobSiteForSelectionVwMdl { Id = (int)getEquipmentJobSite().crsf_auto, Title = getEquipmentJobSite().site_name, CustomerId = getEquipmentJobSite().customer_auto.LongNullableToInt() },
                MakeModelFamily = getMakeModelFamily(),
                Serial = getEquipmentSerialNo(),
                Unit = getEquipmentUnitNo(),
                SMU = GetSerialMeterUnit(DateTime.Now.ToLocalTime()),
                Life = GetEquipmentLife(DateTime.Now.ToLocalTime())
            };
        }

        public IQueryable<LU_Module_Sub> getEquipmentSystems(int EquipmentId, DateTime Date)
        {
            var _dalEquipment = getDALEquipment(EquipmentId);
            if (_dalEquipment == null) return new List<LU_Module_Sub>().AsQueryable();
            var systemIds = _dalEquipment.UCSystems.Select(m => m.Module_sub_auto).ToList();
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory && m.event_date > Date && m.equipmentid_auto == EquipmentId && m.recordStatus == (int)RecordStatus.Available).OrderByDescending(m => m.event_date);
            var resultIds = new List<long>();
            foreach (var id in systemIds)
            {
                int _replacedId = (int)id;
                bool _repeat = true;
                while (_repeat)
                {
                    var _action = actions.Where(m => m.system_auto_id_new == _replacedId).FirstOrDefault();
                    if (_action != null)
                    {
                        _replacedId = (int)_action.system_auto_id;
                    }
                    else
                        _repeat = false;
                }
                resultIds.Add(_replacedId);

            }
            return _context.LU_Module_Sub.Where(m => resultIds.Any(p => m.Module_sub_auto == p));
        }

        public DateTime ForcastNextInspectionDate(DateTime _date, int equipmentId)
        {
            var _equipment = _context.EQUIPMENTs.Find(equipmentId);
            if (_equipment == null)
                return DateTime.MinValue;
            var _lastUndercarriageSetup = _context.ACTION_TAKEN_HISTORY.Where(m => m.recordStatus == (int)RecordStatus.Available && m.equipmentid_auto == equipmentId && (m.action_type_auto == (int)ActionType.InstallSystemOnEquipment || m.action_type_auto == (int)ActionType.UpdateUndercarriageSetupOnEquipment)).OrderBy(m => m.event_date).FirstOrDefault();
            if (_lastUndercarriageSetup == null)
                return DateTime.MinValue;

            var _inspections = _equipment.TRACK_INSPECTION.Where(m => m.ActionHistoryId != null && m.ActionTakenHistory.recordStatus == (int)RecordStatus.Available).OrderByDescending(m => m.inspection_date);

            if (_inspections.Count() == 0)
                return _lastUndercarriageSetup.event_date;

            var _equipmentSetupDate = _equipment.ActionTakenHistoryId == null ? (_equipment.purchase_date ?? DateTime.MinValue) : _equipment.ActionTakenHistory.event_date;

            double averageWorkingPerHour = 1;
            int currentLife = GetEquipmentLife(equipmentId, _date);

            int _totalWorkedHours = (int)(currentLife - (_equipment.LTD_at_start ?? 0));
            var _lastLife = _equipment.Life.Where(m => m.Action.recordStatus == (int)RecordStatus.Available && m.ActionDate.Date <= _date.Date).OrderByDescending(m => m.ActionDate).FirstOrDefault();
            double _totalPassedHours = ((_lastLife != null ? _lastLife.ActionDate : _date) - _lastUndercarriageSetup.event_date).TotalHours;

            var _daysWorking = (new bool[] { _equipment.UsedMonday, _equipment.UsedTuesday, _equipment.UsedWednesday, _equipment.UsedThursday, _equipment.UsedFriday, _equipment.UsedSaturday, _equipment.UsedSunday }).Count(m => m);
            _daysWorking = _daysWorking == 0 ? 5 : _daysWorking;
            averageWorkingPerHour = (_equipment.EnableAutoInspectionPlanner ? ((_totalWorkedHours * 100) / _totalPassedHours) : (((_equipment.op_hrs_per_day ?? 1) * _daysWorking) * 100) / 168) / 100;
            if (averageWorkingPerHour == 0) averageWorkingPerHour = 1;
            var _lastInspection = _inspections.FirstOrDefault();

            switch (_equipment.InspectEveryUnitTypeId)
            {
                case (int)TimeDistances.Hours:
                    return (_lastInspection.inspection_date.AddHours(_equipment.InspectEvery * (1 / averageWorkingPerHour)));
                case (int)TimeDistances.Days:
                    return (_lastInspection.inspection_date.AddDays(_equipment.InspectEvery));
                case (int)TimeDistances.Weeks:
                    return (_lastInspection.inspection_date.AddDays(_equipment.InspectEvery * 7));
                case (int)TimeDistances.Months:
                    return (_lastInspection.inspection_date.AddMonths(_equipment.InspectEvery));
                case (int)TimeDistances.Weekly:
                    return (_lastInspection.inspection_date.AddDays(7 + Math.Abs(((int)_lastInspection.inspection_date.DayOfWeek) - (_equipment.InspectEvery % 7))));
                case (int)TimeDistances.Monthly:
                    return (_lastInspection.inspection_date.AddDays(_lastInspection.inspection_date.AddMonths(1).Day + Math.Abs((_lastInspection.inspection_date.Day) - (_equipment.InspectEvery % 31))));
                case (int)TimeDistances.Quarterly:
                    return (_lastInspection.inspection_date.AddDays(_lastInspection.inspection_date.AddMonths(3).Day + Math.Abs((_lastInspection.inspection_date.Day) - (_equipment.InspectEvery % 31))));
                case (int)TimeDistances.Yearly:
                    return (_lastInspection.inspection_date.AddDays(_lastInspection.inspection_date.AddMonths(12).Day + Math.Abs((_lastInspection.inspection_date.Day) - (_equipment.InspectEvery % 31))));
                default:
                    return DateTime.MinValue;
            }
        }

        public int ForcastNextInspectionSMU(DateTime _date, int equipmentId)
        {
            var _equipment = _context.EQUIPMENTs.Find(equipmentId);
            if (_equipment == null)
                return 0;
            var _lastUndercarriageSetup = _context.ACTION_TAKEN_HISTORY.Where(m => m.recordStatus == (int)RecordStatus.Available && m.equipmentid_auto == equipmentId && (m.action_type_auto == (int)ActionType.InstallSystemOnEquipment || m.action_type_auto == (int)ActionType.UpdateUndercarriageSetupOnEquipment)).OrderBy(m => m.event_date).FirstOrDefault();
            if (_lastUndercarriageSetup == null)
                return (int)(_equipment.currentsmu ?? 0);

            var _inspections = _equipment.TRACK_INSPECTION.Where(m => m.ActionHistoryId != null && m.ActionTakenHistory.recordStatus == (int)RecordStatus.Available).OrderByDescending(m => m.inspection_date);

            if (_inspections.Count() == 0)
                return _lastUndercarriageSetup.equipment_smu;

            var _equipmentSetupDate = _equipment.ActionTakenHistoryId == null ? (_equipment.purchase_date ?? DateTime.MinValue) : _equipment.ActionTakenHistory.event_date;

            double averageWorkingPerHour = 1;
            int currentLife = GetEquipmentLife(equipmentId, _date);

            int _totalWorkedHours = (int)(currentLife - (_equipment.LTD_at_start ?? 0));
            var _lastLife = _equipment.Life.Where(m => m.Action.recordStatus == (int)RecordStatus.Available && m.ActionDate.Date <= _date.Date).OrderByDescending(m => m.ActionDate).FirstOrDefault();
            double _totalPassedHours = ((_lastLife != null ? _lastLife.ActionDate : _date) - _lastUndercarriageSetup.event_date).TotalHours;

            var _daysWorking = (new bool[] { _equipment.UsedMonday, _equipment.UsedTuesday, _equipment.UsedWednesday, _equipment.UsedThursday, _equipment.UsedFriday, _equipment.UsedSaturday, _equipment.UsedSunday }).Count(m => m);
            _daysWorking = _daysWorking == 0 ? 5 : _daysWorking;
            averageWorkingPerHour = (_equipment.EnableAutoInspectionPlanner ? ((_totalWorkedHours * 100) / _totalPassedHours) : (((_equipment.op_hrs_per_day ?? 1) *  _daysWorking) * 100) / 168) / 100;
            
            var _lastInspection = _inspections.FirstOrDefault();

            switch (_equipment.InspectEveryUnitTypeId)
            {
                case (int)TimeDistances.Hours:
                    return ((_lastInspection.smu ?? 0) + (int)(_equipment.InspectEvery * (averageWorkingPerHour)));
                case (int)TimeDistances.Days:
                    return ((_lastInspection.smu ?? 0) + (int)(_equipment.InspectEvery * (24 * averageWorkingPerHour)));
                case (int)TimeDistances.Weeks:
                    return ((_lastInspection.smu ?? 0) + (int)(_equipment.InspectEvery * (168 * averageWorkingPerHour)));
                case (int)TimeDistances.Months:
                    return ((_lastInspection.smu ?? 0) + (int)(_equipment.InspectEvery * (5040 * averageWorkingPerHour)));
                case (int)TimeDistances.Weekly:
                    return ((_lastInspection.smu ?? 0) + (int)(((_lastInspection.inspection_date.AddDays(7 + Math.Abs(((int)_lastInspection.inspection_date.DayOfWeek) - (_equipment.InspectEvery % 7)))) - _lastInspection.inspection_date).TotalHours * averageWorkingPerHour));
                case (int)TimeDistances.Monthly:
                    return ((_lastInspection.smu ?? 0) + (int)(((_lastInspection.inspection_date.AddDays(_lastInspection.inspection_date.AddMonths(1).Day + Math.Abs((_lastInspection.inspection_date.Day) - (_equipment.InspectEvery % 31)))) - _lastInspection.inspection_date).TotalHours * averageWorkingPerHour));
                case (int)TimeDistances.Quarterly:
                    return ((_lastInspection.smu ?? 0) + (int)(((_lastInspection.inspection_date.AddDays(_lastInspection.inspection_date.AddMonths(3).Day + Math.Abs((_lastInspection.inspection_date.Day) - (_equipment.InspectEvery % 31)))) - _lastInspection.inspection_date).TotalHours * averageWorkingPerHour));
                case (int)TimeDistances.Yearly:
                    return ((_lastInspection.smu ?? 0) + (int)(((_lastInspection.inspection_date.AddDays(_lastInspection.inspection_date.AddMonths(12).Day + Math.Abs((_lastInspection.inspection_date.Day) - (_equipment.InspectEvery % 31)))) - _lastInspection.inspection_date).TotalHours * averageWorkingPerHour));
                default:
                    return _lastUndercarriageSetup.equipment_smu;
            }
        }
        /// <summary>
        /// Returns a query for additional measurements by the given parameters
        /// Hint 1: if equipmentId is provided model and family will be overriden by the equipment model and family
        /// Hint 2: priority from highest to lowest: customer -> model -> family
        /// If there are records with FamilyId, ModelId and CustomerId will be returned. else
        /// If there are records with ModelId and CustomerId while FamilyId is null will be returned. else
        /// if there are records with FamilyId and CustomerId while ModelId is null will be returned. else
        /// if there are records with CustomerId while ModelId and FamilyId are null will be returned. else
        /// Will return the same records while customer is null
        /// </summary>
        /// <param name="equipmentId"></param>
        /// <param name="familyId"></param>
        /// <param name="modelId"></param>
        /// <param name="customerId"></param>
        /// <param name="compartTypeId"></param>
        /// <returns></returns>
        public IQueryable<CUSTOMER_MODEL_COMPARTTYPE_ADDITIONAL> getCustomerModelFamilyCompartAdditional(int equipmentId = 0, int familyId = 0, int modelId = 0, int customerId = 0, int compartTypeId = 0)
        {
            if (equipmentId != 0)
            {
                var _equipment = _context.EQUIPMENTs.Find(equipmentId);
                if (_equipment != null)
                {
                    familyId = _equipment.LU_MMTA.type_auto;
                    modelId = _equipment.LU_MMTA.model_auto;
                }
            }
            var result = _context.CUSTOMER_MODEL_COMPARTTYPE_ADDITIONAL.AsQueryable();
            bool found = false;
            if (customerId != 0)
            {
                if (modelId != 0 && familyId != 0 && result.Count(m => m.FamilyId == familyId && m.ModelId == modelId && m.CustomerId == customerId) > 0)
                {
                    found = true;
                    result = result.Where(m => m.FamilyId == familyId && m.ModelId == modelId && m.CustomerId == customerId);
                }
                else if (modelId != 0 && result.Count(m => m.ModelId == modelId && m.FamilyId == null && m.CustomerId == customerId) > 0)
                {
                    found = true;
                    result = result.Where(m => m.ModelId == modelId && m.FamilyId == null && m.CustomerId == customerId);
                }
                else if (familyId != 0 && result.Count(m => m.FamilyId == familyId && m.ModelId == null && m.CustomerId == customerId) > 0)
                {
                    found = true;
                    result = result.Where(m => m.FamilyId == familyId && m.ModelId == null && m.CustomerId == customerId);
                }
                else if (result.Count(m => m.CustomerId == customerId && m.FamilyId == null && m.ModelId == null) > 0)
                {
                    found = true;
                    result = result.Where(m => m.CustomerId == customerId && m.ModelId == null && m.FamilyId == null);
                }
            }
            if (!found)
            {
                if (modelId != 0 && familyId != 0 && result.Count(m => m.FamilyId == familyId && m.ModelId == modelId && m.CustomerId == null) > 0)
                {
                    result = result.Where(m => m.FamilyId == familyId && m.ModelId == modelId && m.CustomerId == null);
                }
                else if (modelId != 0 && result.Count(m => m.ModelId == modelId && m.FamilyId == null && m.CustomerId == null) > 0)
                {
                    result = result.Where(m => m.ModelId == modelId && m.FamilyId == null && m.CustomerId == null);
                }
                else if (familyId != 0 && result.Count(m => m.FamilyId == familyId && m.ModelId == null && m.CustomerId == null) > 0)
                {
                    result = result.Where(m => m.FamilyId == familyId && m.ModelId == null && m.CustomerId == null);
                }
                else
                {
                    result = result.Where(m => m.CustomerId == null && m.ModelId == null && m.FamilyId == null);
                }
            }
            if (compartTypeId != 0) result = result.Where(m => m.CompartTypeId == compartTypeId);
            return result;
        }
        /// <summary>
        /// Returns a template for the equipment history
        /// Why do we need template? Because it is possible for a system to be replaced by a new one. In this case if the new system has less components on it 
        /// then history will be built based on the existing components on the new one.
        /// For that reason if we have a template which covers all possible "Type, Side and Position" combinations during the history of equipment, we can show a complete history of that equipment.
        /// Hint 1: if there is a system replacement before version 5.8 then system_auto_id_new in the ACTION_TAKEN_HISTORY table needs to be manually set for that equipment and system.
        /// Hint 2: if there is any component replacement before version 5.8 then equnit_auto_new may needs to be set manually.
        /// </summary>
        /// <param name="equipmentId"></param>
        /// <returns></returns>
        public EquipmentHistoryTemplate getEquipmentHistoryTemplate(int equipmentId)
        {
            var _equipment = _context.EQUIPMENTs.Find(equipmentId);
            var result = new EquipmentHistoryTemplate { Id = equipmentId, SystemsHistory = new List<SystemHistoryTemplate>() };
            if (_equipment == null)
                return result;
            result.Id = equipmentId;
            var _replacements_new_old = _context.ACTION_TAKEN_HISTORY.Where(m => m.recordStatus == (int)RecordStatus.Available && m.equipmentid_auto == equipmentId && m.action_type_auto == (int)(ActionType.ReplaceSystemFromInventory)).Select(m => new { a = (int)(m.system_auto_id_new ?? 0), b = (int)(m.system_auto_id ?? 0) }).ToList().Select(m=> new int[] { m.a, m.b }).ToList();
            
            foreach (var _systemId in _equipment.UCSystems.Select(m=> (int)m.Module_sub_auto).ToList())
            {
                var _currentSystemTemplate = new UCSystem(_context).getSystemHistoryTemplate(_systemId, true);
                if(_replacements_new_old.Count() > 0)
                {
                    var _linked_ids = getLinkedIds(_replacements_new_old, _systemId, new List<int>());
                    var _componentTemplates = _currentSystemTemplate.ComponentsHistory;
                    foreach (var _id in _linked_ids)
                        _componentTemplates.AddRange(new UCSystem(_context).getSystemHistoryTemplate(_id, true).ComponentsHistory);
                    _currentSystemTemplate.ComponentsHistory = flattenComponentHistoryTemplate(_componentTemplates);
                }
                result.SystemsHistory.Add(_currentSystemTemplate);
            }
            return result;
        }
        /// <summary>
        /// Returns a list of ids which are connected to eachother in the master list for the provided id
        /// </summary>
        /// <param name="list"> A list of 2d int array </param>
        /// <param name="id">Starting id</param>
        /// <returns></returns>
        private List<int> getLinkedIds(List<int[]> masterList, int id, List<int> result)
        {
            var _current = masterList.Where(m => m[0] == id && !result.Any(k=> k == m[0])).FirstOrDefault();
            if (_current == null) return result;
            result.Add(_current[1]);
            return getLinkedIds(masterList, _current[1], result);
        }
        
        private List<ComponentHistoryTemplate> flattenComponentHistoryTemplate(List<ComponentHistoryTemplate> templates)
        {
            var result = new List<ComponentHistoryTemplate>();
            foreach(var _template in templates)
            {
                if (!result.Any(m => m.CompartTypeId == _template.CompartTypeId && m.Pos == _template.Pos))
                    result.Add(_template);
            }
            return result;
        }

        // ↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔ END OF EQUIPMENT CLASS ↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔
    }
}
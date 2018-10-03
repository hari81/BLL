using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using DAL;
using BLL.Persistence.Repositories;
using System.Data.Entity;
using BLL.Extensions;
using BLL.Core.ViewModel;
using System.Threading.Tasks;

namespace BLL.Core.Domain
{
    public class UCSystem : Equipment, IUCSystem
    {
        private int pvId;
        private int pvLife;
        private LU_Module_Sub pvSystems;
        private IList<GENERAL_EQ_UNIT> pvComponents;
        private UCSystemType pvSystemType;
        private Side pvside;
        public Side side
        { get { return pvside; } private set { pvside = value; } }
        public UCSystemType SystemType
        {
            get { return pvSystemType; }
            set { pvSystemType = value; }
        }
        public new int Id
        {
            get { return pvId; }
            private set { pvId = value; }
        }

        public int SystemLatestLife
        {
            get { return pvLife; }
            private set { pvLife = value; }
        }
        public LU_Module_Sub DALSystem
        {
            get { return pvSystems; }
            set { pvSystems = value; }
        }
        public IList<GENERAL_EQ_UNIT> Components
        {
            get { return pvComponents; }
            set { pvComponents = value; }
        }
        private UndercarriageContext _context
        {
            get { return Context as UndercarriageContext; }
        }
        public UCSystem(IUndercarriageContext context) : base(context)
        {
            Id = 0;
            side = Side.Unknown;
        }

        public UCSystem(IUndercarriageContext context, int id) : base(context)
        {
            Id = id;
            Init(id, false);
        }
        public UCSystem(IUndercarriageContext context, int id, bool InitEquipment) : base(context, getEqId(context, id))
        {
            Id = id;
            Init(id, InitEquipment);
        }
        private static int getEqId(IUndercarriageContext ctx, int sysId)
        {
            var ucContext = (UndercarriageContext)ctx;
            var sys = ucContext.LU_Module_Sub.Find(sysId);
            if (sys == null || sys.equipmentid_auto == null) return 0;
            return (int)(sys.equipmentid_auto);
        }
        private void Init(int id, bool InitEquipment)
        {
            DALSystem = _context.LU_Module_Sub.Find(id);
            if (DALSystem != null)
            {
                Id = id;
                pvId = id;
                Components = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Id).ToList();
                SystemType = GetSystemTypePv(Id);
                side = GetSystemSide();
                if (DALSystem.equipmentid_auto != null && InitEquipment)
                {
                    DALEquipment = _context.EQUIPMENTs.Find(DALSystem.equipmentid_auto);
                    if (DALEquipment != null)
                    {

                        DALSystems = _context.LU_Module_Sub.Where(m => m.equipmentid_auto == DALEquipment.equipmentid_auto).ToList();
                        //DALComponents = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == DALEquipment.equipmentid_auto).ToList();
                    }
                }
            }
        }

        public DAL.LU_Module_Sub getDALSystem(int SystemId, bool doInit = false)
        {
            if (DALSystem != null && DALSystem.Module_sub_auto == SystemId)
                return DALSystem;
            if (doInit) { Init(SystemId, false); }
            DALSystem = _context.LU_Module_Sub.Find(SystemId);
            return DALSystem;
        }

        /// <summary>
        /// Returns the total setup cost of all components on this system. 
        /// </summary>
        /// <returns></returns>
        public decimal GetTotalSetupCostOfComponentsOnSystem()
        {
            return Components.Sum(c => c.cost == null ? 0 : (decimal)c.cost);
        }

        private Side GetSystemSide()
        {
            if (Id == 0)
                return Side.Unknown;
            // Currently there is no column to show the side of the system in LU_Module_sub table
            // So I have to check what is the previous system side based on it's components side which is not reliable
            // I count all the component sides, then the side which has the bigger number is accepted
            byte? sideL = 0;
            byte? sideR = 0;
            foreach (var Comp in Components)
            {
                if (Comp.side != null && Comp.side == 1)
                    sideL++;
                else if (Comp.side != null && Comp.side == 2)
                    sideR++;
            }
            if (sideL > sideR) return Side.Left;
            if (sideR > sideL) return Side.Right;
            return Side.Unknown;
        }
        public UCSystemType GetSystemType(int Id)
        {
            if (Id == 0)
                return UCSystemType.Unknown;
            var DALSystem = _context.LU_Module_Sub.Find(Id);
            if (DALSystem != null)
            {
                if (DALSystem.systemTypeEnumIndex == (int)UCSystemType.Chain)
                    return UCSystemType.Chain;
                if (DALSystem.systemTypeEnumIndex == (int)UCSystemType.Frame)
                    return UCSystemType.Frame;
            }
            return GetSystemTypePv(Id);
        }
        public UCSystemType GetSystemType()
        {
            if (Id == 0)
                return UCSystemType.Unknown;
            if (DALSystem != null)
            {
                if (DALSystem.systemTypeEnumIndex == (int)UCSystemType.Chain)
                    return UCSystemType.Chain;
                if (DALSystem.systemTypeEnumIndex == (int)UCSystemType.Frame)
                    return UCSystemType.Frame;
            }
            return GetSystemTypePv(Id);
        }
        private UCSystemType GetSystemTypePv(int SystemId)
        {
            List<GENERAL_EQ_UNIT> CompList;
            if (SystemId == Id)
                CompList = Components.ToList();
            else
                CompList = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == SystemId).ToList();
            int isChain = 0;
            int isFrame = 0;
            foreach (var comp in CompList)
            {
                if (comp.LU_COMPART.comparttype_auto == 230) // It is Link
                    isChain++;
                if (comp.LU_COMPART.comparttype_auto == 233) // It is Idler
                    isFrame++;
            }
            if (isChain > isFrame) return UCSystemType.Chain;
            if (isFrame > isChain) return UCSystemType.Frame;
            return UCSystemType.Unknown;
        }
        public int GetSystemLife(DateTime date)
        {
            if (Id == 0)
                return -1;
            var UCsys = _context.LU_Module_Sub.Find(Id);
            if (UCsys == null)
                return -1;
            var lifes = UCsys.Life.Where(m => m.ActionDate <= date && m.ACTION_TAKEN_HISTORY.recordStatus == 0).OrderBy(field => field.ActionDate);
            if (lifes.Count() > 0)
                return lifes.Last().ActualLife;

            var systemLinks = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == UCsys.Module_sub_auto && m.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Link).ToList();//230 is the Link type ID
            var systemIdlers = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == UCsys.Module_sub_auto && m.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Idler).ToList();//233 is the Idler type ID

            if (systemLinks.Count() > 0 && systemIdlers.Count() > 0) //Something is wrong because one system cannot has both link and idler
                return longNullableToint(UCsys.LTD_at_install);

            if (systemLinks.Count() > 0)
                return new Component(_context, (longNullableToint(systemLinks.First().equnit_auto))).GetComponentLife(date);
            else if (systemIdlers.Count() > 0)
                return new Component(_context, (longNullableToint(systemIdlers.First().equnit_auto))).GetComponentLife(date);
            return longNullableToint(UCsys.LTD_at_install);
        }
        /// <summary>
        /// Returns cost of all actions that this system was part of plus cost of components currently are installed
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public decimal getSystemCost(DateTime date)
        {
            if (Id == 0) return 0;
            try
            {
                var actionsToDateCostList = _context.ACTION_TAKEN_HISTORY.Where(m => m.system_auto_id == Id && m.event_date <= date && m.recordStatus == 0).ToList();
                decimal actionsToDateCost = actionsToDateCostList.Count() > 0 ? actionsToDateCostList.Sum(m => m.cost) : 0;
                var componentsCostList = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Id && m.cost != null);
                //var componentsCost2 = componentsCost1.Select(m => m.cost);
                //var componentsCost = componentsCost2.ToList();
                var sumOfComps = componentsCostList.Count() > 0 ? componentsCostList.Select(m => m.cost).Sum(m => (decimal)m) : 0;
                return actionsToDateCost + sumOfComps;
            }
            catch (Exception e)
            {
                string message = e.Message;
                return 0;
            }
        }

        /// <summary>
        /// Gets the total cost of all actions which have been recorded on components which were on this sytem. 
        /// </summary>
        /// <returns>The total cost</returns>
        public decimal GetTotalCostOfAllComponentActions()
        {
            if (Id == 0) return 0;
            try
            {
                var actionsToDateCostList = _context.ACTION_TAKEN_HISTORY.Where(m => m.system_auto_id == Id && m.recordStatus == 0).ToList();
                return actionsToDateCostList.Count() > 0 ? actionsToDateCostList.Sum(m => m.cost) : 0;
            }
            catch (Exception e)
            {
                string message = e.Message;
                return 0;
            }
        }

        public SystemDetailsViewModel getSystemDetails(DateTime date)
        {
            SystemDetailsViewModel result = new SystemDetailsViewModel { Id = 0, SystemType = UCSystemType.Unknown, Side = Side.Unknown };
            if (Id == 0) return result;
            result.Id = Id;
            result.SystemType = SystemType;
            result.Side = side;
            result.Life = GetSystemLife(date);
            result.Cost = getSystemCost(date);
            result.TotalCostPerHour = 0;
            result.Eval = GetWorstEval(date);
            result.Serial = GetSystemSerial();
            var mainComponent = getSystemMainComponent();
            if (mainComponent != null && mainComponent.equnit_auto != 0)
            {
                var LogicalComponent = new Component(_context, longNullableToint(mainComponent.equnit_auto));
                int projectedHours = LogicalComponent.GetProjectedHours(date);
                if (projectedHours > 0)
                    result.TotalCostPerHour = result.Cost / projectedHours;
            }
            return result;
        }
        /// <summary>
        /// Returns the GENERAL_EQ_UNIT Object of the Link or Idler based on the system type
        /// </summary>
        /// <returns></returns>
        public GENERAL_EQ_UNIT getSystemMainComponent()
        {
            if (Id == 0)
                return null;
            if (GetSystemType(Id) == UCSystemType.Chain)
            {
                var links = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Id && m.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Link);
                if (links.Count() == 0) return null;
                if (links.Count() == 1) return (links.First());
                foreach (var link in links.ToList())
                {
                    if (link.LU_COMPART.PARENT_RELATION_LIST.Count() == 0)
                        return link;
                }
            }
            else if (GetSystemType(Id) == UCSystemType.Frame)
            {
                var idlers = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Id && m.LU_COMPART.comparttype_auto == (int)CompartTypeEnum.Idler);
                if (idlers.Count() == 0) return null;
                if (idlers.Count() == 1) return idlers.First();
                foreach (var idler in idlers.ToList())
                {
                    if (idler.LU_COMPART.PARENT_RELATION_LIST.Count() == 0)
                        return idler;
                }
            }
            return null;
        }

        public bool CreateNewSystem(SetupSystemParams Params, int? eqId)
        {
            if (Id != 0)
                return false;
            if (!ValidateSystemSetup(Params, eqId))
                return false;
            LU_Module_Sub newSystem = new LU_Module_Sub
            {
                Serialno = Params.SerialNo,
                CreatedBy = Params.UserId,
                CreatedDate = Params.SetupDate,
                Status = 0,
                crsf_auto = Params.JobSiteId,
                make_auto = Params.MakeId,
                model_auto = Params.ModelId,
                type_auto = (int)Params.Family,
                systemTypeEnumIndex = (int)Params.SystemType
            };
            _context.LU_Module_Sub.Add(newSystem);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                string message = e.Message;
                return false;
            }
            Id = longNullableToint(newSystem.Module_sub_auto);
            Init(Id, true);
            return true;
        }
        /// <summary>
        /// This method is the newer version of the previous one!
        /// There is a difference on return type
        /// we are upgrading!
        /// </summary>
        /// <param name="Params"></param>
        /// <param name="eqId"></param>
        /// <returns></returns>
        public SetupViewModel CreateNewSystem(SetupViewModel Params, int? eqId)
        {
            if (Id != 0)
            {
                Params.Result = new ResultMessage
                {
                    Id = 0,
                    ActionLog = "System Id must be zero! Cannot create a system which has already been created!",
                    LastMessage = "This System has already been created!",
                    OperationSucceed = false
                };
                return Params;
            }
            var param = new SetupSystemParams
            {
                Id = Params.Id,
                Family = (EquipmentFamily)Params.Family.Id,
                JobSiteId = Params.JobsiteId,
                MakeId = Params.Make.Id,
                ModelId = Params.Model.Id,
                SerialNo = Params.Serial,
                SetupDate = Params.InstallationDate.ToLocalTime().Date,
                SystemType = Params.Type,
                UserId = Params.UserId,
                Side = Params.Side,
                Life = Params.HoursAtInstall,
            };
            if (!ValidateSystemSetup(param, eqId))
            {
                Params.Result = new ResultMessage
                {
                    Id = 0,
                    ActionLog = "Validation failed! Please check system serial and jobsite inputs and try again!: !ValidateSystemSetup returned false ",
                    LastMessage = "Validation failed! Please check system serial and jobsite inputs and try again!",
                    OperationSucceed = false
                };
                return Params;
            }
            Params.Serial = GenerateSerialString(param, Params.EquipmentId);
            LU_Module_Sub newSystem = new LU_Module_Sub
            {
                Serialno = Params.Serial,
                CreatedBy = Params.UserId,
                CreatedDate = Params.InstallationDate.ToLocalTime().Date,
                Status = 0,
                crsf_auto = Params.JobsiteId,
                make_auto = Params.Make.Id,
                model_auto = Params.Model.Id,
                type_auto = Params.Family.Id,
                systemTypeEnumIndex = (int)Params.Type,
                CMU = Params.HoursAtInstall,
            };
            _context.LU_Module_Sub.Add(newSystem);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                string message = e.Message;
                Params.Result = new ResultMessage
                {
                    Id = 0,
                    ActionLog = "Operation failed due to an exception! log:" + message,
                    LastMessage = "Operation failed! Please check log and try again",
                    OperationSucceed = false
                };
                return Params;
            }
            Id = longNullableToint(newSystem.Module_sub_auto);
            Init(Id, true);
            Params.Id = Id;
            Params.Result = new ResultMessage
            {
                Id = 0,
                ActionLog = "Operation Succeded!",
                LastMessage = "Operation Succeded!",
                OperationSucceed = true
            };
            return Params;
        }

        public async Task<SetupViewModel> CreateNewSystemAsync(SetupViewModel Params, int? eqId)
        {
            return await Task.Run(() => CreateNewSystem(Params, eqId));
        }

        private bool ValidateSystemSetup(SetupSystemParams Params, int? EquipmentId)
        {
            var Jobsite = _context.CRSF.Find(Params.JobSiteId);
            if (Jobsite == null)
            {
                return false;
            }

            bool serialIsValid = true;
            if (Params.SerialNo == null || Params.SerialNo.Trim().Length == 0 || _context.LU_Module_Sub.Where(m => m.Serialno == Params.SerialNo).Count() > 0)
                serialIsValid = false;
            int count = 0;
            string _serial = Params.SerialNo;
            DAL.EQUIPMENT eq = EquipmentId != null ? _context.EQUIPMENTs.Find(EquipmentId) : null;
            while (!serialIsValid && count < 100)
            {
                if (_serial.Trim().Length == 0)
                {
                    var Jsite = Jobsite;
                    _serial = giveMeSerial(eq, Jsite, count, Params);
                    continue;
                }

                if (_context.LU_Module_Sub.Where(m => m.Serialno == _serial).Count() > 0)
                {
                    _serial = giveMeRandomString("ABCDEFGHJKLMPRTWXYZ", 4, 4, new Random()); ;
                    continue;
                }

                {
                    Params.SerialNo = _serial;
                    serialIsValid = true;
                }
                count++;
            }//End of while -> after 100 times if it cannot generate a unique serial number returns an error
            if (!serialIsValid)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private string GenerateSerialString(SetupSystemParams Params, int? EquipmentId)
        {
            var Jobsite = _context.CRSF.Find(Params.JobSiteId);
            bool serialIsValid = true;
            if (Params.SerialNo == null || Params.SerialNo.Trim().Length == 0 || _context.LU_Module_Sub.Where(m => m.Serialno == Params.SerialNo).Count() > 0)
                serialIsValid = false;
            int count = 0;
            string _serial = Params.SerialNo;
            DAL.EQUIPMENT eq = EquipmentId != null ? _context.EQUIPMENTs.Find(EquipmentId) : null;
            while (!serialIsValid && count < 100)
            {
                if (_serial.Trim().Length == 0)
                {
                    var Jsite = Jobsite;
                    _serial = giveMeSerial(eq, Jsite, count, Params);
                    continue;
                }

                if (_context.LU_Module_Sub.Where(m => m.Serialno == _serial).Count() > 0)
                {
                    _serial = giveMeRandomString("ABCDEFGHJKLMPRTWXYZ", 4, 4, new Random());
                    continue;
                }

                {
                    Params.SerialNo = _serial;
                    serialIsValid = true;
                }
                count++;
            }//End of while -> after 100 times if it cannot generate a unique serial number returns an error
            if (!serialIsValid)
            {
                return Guid.NewGuid().ToString();
            }
            else
            {
                return Params.SerialNo;
            }
        }

        private string giveMeSerial(DAL.EQUIPMENT equipment, DAL.CRSF JobSite, int Counter, SetupSystemParams Params)
        {
            string EquipmentSerial = "";
            string EquipmentUnit = "";
            string JobSiteName = "";
            int EquipmentId = 0;
            int JobSiteId = 0;
            string SystemTypeString = "TU";
            string SideString = "SU";
            string result = "";
            string AllowedChars = "ABCDEFGHJKLMPRTWXYZ";
            if (equipment != null)
            {
                EquipmentSerial = equipment.serialno;
                EquipmentUnit = equipment.unitno;
                EquipmentId = longNullableToint(equipment.equipmentid_auto);
            }
            if (JobSite != null)
            {
                JobSiteName = JobSite.site_name;
                JobSiteId = longNullableToint(JobSite.crsf_auto);
            }
            if (Params.SystemType == UCSystemType.Chain) SystemTypeString = "Chain";
            else if (Params.SystemType == UCSystemType.Frame) SystemTypeString = "Frame";

            if (Params.Side == Side.Left) SideString = "Left";
            else if (Params.Side == Side.Right) SideString = "Right";

            if (EquipmentId > 0)
            {
                result += EquipmentSerial;
            }
            else if (JobSiteId > 0)
            {
                result += JobSiteName;
            }
            else
            {
                result += giveMeRandomString(AllowedChars, 4, 4, new Random());
            }
            result += SystemTypeString + SideString;
            if (Counter > 0)
                result += Counter;
            return result;
        }
        /// <summary>
        /// This method removes installed given components from database!!
        /// it is not moving them to inventory! use this method for setup components only
        /// </summary>
        /// <param name="removingComponents">Components to be removed from database</param>
        /// <returns></returns>
        public bool removeComponents(List<GENERAL_EQ_UNIT> removingComponents)
        {
            var k = removingComponents.Select(m => m.equnit_auto);
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.action_type_auto == (int)ActionType.InstallComponentOnSystemOnEquipment && k.Any(w => w == m.equnit_auto) && m.recordStatus == 0);
            _context.ACTION_TAKEN_HISTORY.RemoveRange(actions);
            _context.GENERAL_EQ_UNIT.RemoveRange(removingComponents);
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
        public EvalCode GetWorstEval(DateTime date)
        {
            if (Id == 0)
                return EvalCode.U;
            var inspection = GetLatestInspection(date);
            if (inspection == null)
                return EvalCode.U;
            var evals = inspection.TRACK_INSPECTION_DETAIL.Where(m => m.UCSystemId == Id).Select(m => m.eval_code);
            if (evals.Count() == 0)
                return EvalCode.U;
            return toEvalCode(evals.Max());
        }
        /// <summary>
        /// This method checks if there is any action on a component on this system then returns true
        /// </summary>
        /// <returns></returns>
        public bool checkActionOnComponents()
        {
            if (Id == 0)
                return false;
            foreach (var cmp in Components)
            {
                if (_context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == cmp.equnit_auto && m.recordStatus == 0).Count() > 0)
                    return true;
            }
            return false;
        }

        public List<ComponentActionVwMdl> getComponentsActions()
        {
            var resultList = new List<ComponentActionVwMdl>();
            if (Id == 0)
                return resultList;
            foreach (var cmp in Components)
            {
                var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == cmp.equnit_auto && m.recordStatus == 0).ToList();
                foreach (var act in actions)
                {
                    ComponentActionVwMdl result = new ComponentActionVwMdl();
                    result.Id = act.history_id.LongNullableToInt();
                    result.ActionDate = act.event_date;
                    result.ActionDStr = act.event_date.ToString("dd MMM yyyy");
                    result.ActionType = (ActionType)act.action_type_auto;
                    result.ActionTypeStr = result.ActionType.Label();
                    result.Comment = act.comment;
                    result.Cost = act.cost;
                    result.ComponentStrTitle = cmp.LU_COMPART.LU_COMPART_TYPE.comparttypeid + " - " + cmp.LU_COMPART.compart;
                    resultList.Add(result);
                }
            }
            return resultList;
        }
        public string GetSystemSerial()
        {
            if (DALSystem != null)
                return DALSystem.Serialno;
            return "-";
        }
        public string GetSystemSerial(int SystemId)
        {
            if (DALSystem != null)
                return DALSystem.Serialno;
            var sys = _context.LU_Module_Sub.Find(SystemId);
            if (sys == null)
                return "-";
            return sys.Serialno;
        }

        public IEnumerable<SystemTemplateVwMdl> getSystemCompartTypeTemplate(int ModelId)
        {
            var k = _context.SystemModelTemplate.Where(m => m.ModelId == ModelId).Select(m => new SystemTemplateVwMdl { Id = m.Id, CompartTypeId = m.CompartTypeId, ModelId = m.ModelId, Name = m.Name, Min = m.Min, Max = m.Max });
            if (k.Count() != 0)
                return k;
            var mmta = _context.LU_MMTA.Where(m => m.model_auto == ModelId).FirstOrDefault();
            if (mmta == null)
                return DefaultTemplate.getUndercarriageSystemTemplate(ModelId);
            var p = _context.SystemFamilyTemplate.Where(m => m.FamilyId == mmta.type_auto).Select(m => new SystemTemplateVwMdl { Id = m.Id, CompartTypeId = m.CompartTypeId, ModelId = ModelId, Name = m.Name, Min = m.Min, Max = m.Max });
            if (p.Count() != 0)
                return p;
            return DefaultTemplate.getUndercarriageSystemTemplate(ModelId);
        }

        public async Task<IEnumerable<SystemTemplateVwMdl>> getSystemCompartTypeTemplateAsync(int ModelId)
        {
            return await Task.Run(() => getSystemCompartTypeTemplate(ModelId));
        }

        public IEnumerable<SystemTemplateVwMdl> getSystemCompartTypeTemplateForEquipment(int EquipmentId)
        {
            var mblEquipment = _context.Mbl_NewEquipment.Where(m => m.pc_equipmentid_auto == EquipmentId).FirstOrDefault();
            var equipment = _context.EQUIPMENTs.Find(EquipmentId);
            if (equipment == null)
                return new List<SystemTemplateVwMdl>();

            if (mblEquipment == null)
                return getSystemCompartTypeTemplate(equipment.LU_MMTA.MODEL.model_auto);

            var matchedComponents = new Inspection(_context).getMatchingForInspectionSync(EquipmentId, mblEquipment.equipmentid_auto.LongNullableToInt());
            var oneSideMatched = matchedComponents.Where(m => m.Side == (int)Side.Left).Count() >= matchedComponents.Where(m => m.Side == (int)Side.Right).Count() ? matchedComponents.Where(m => m.Side == (int)Side.Left) : matchedComponents.Where(m => m.Side == (int)Side.Right);

            var result = new List<SystemTemplateVwMdl>();
            int k = 1;
            foreach (var component in oneSideMatched)
            {
                var existing = result.Where(m => m.CompartTypeId == component.TypeId).FirstOrDefault();
                if (existing == null)
                {
                    result.Add(new SystemTemplateVwMdl
                    {
                        CompartTypeId = component.TypeId,
                        Id = k,
                        Min = 1,
                        Max = 1,
                        ModelId = equipment.LU_MMTA.MODEL.model_auto,
                        Name = "Template Based on Mobile Inspection"
                    });
                    k++;
                }
                else
                {
                    existing.Min++;
                    existing.Max++;
                }

            }
            return result.AsEnumerable();
        }
        public FamilyForSelectionVwMdl getFamily()
        {
            if (Id == 0 || DALSystem == null)
                return new FamilyForSelectionVwMdl();

            if (DALSystem.type_auto == null)
                return new FamilyForSelectionVwMdl();

            var type = _context.TYPEs.Find((int)DALSystem.type_auto);
            if (type == null)
                return new FamilyForSelectionVwMdl();
            return new FamilyForSelectionVwMdl
            {
                Id = type.type_auto,
                Symbol = type.typeid,
                Title = type.typedesc
            };
        }

        public MakeForSelectionVwMdl getMake()
        {
            if (Id == 0 || DALSystem == null)
                return new MakeForSelectionVwMdl();

            if (DALSystem.make_auto == null)
                return new MakeForSelectionVwMdl();

            var make = _context.MAKE.Find((int)DALSystem.make_auto);
            if (make == null)
                return new MakeForSelectionVwMdl();
            return new MakeForSelectionVwMdl
            {
                Id = make.make_auto,
                Symbol = make.makeid,
                Title = make.makedesc
            };
        }

        public MakeForSelectionVwMdl getMake(int componentId)
        {
            var _component = _context.GENERAL_EQ_UNIT.Find(componentId);

            if (_component.make_auto != null && _component.Make != null) return new MakeForSelectionVwMdl
            {
                Id = _component.Make.make_auto,
                Symbol = _component.Make.makeid,
                Title = _component.Make.makedesc
            };

            if (_component.module_ucsub_auto != null && _component.UCSystem != null && _component.UCSystem.Make != null)
            {
                return new MakeForSelectionVwMdl
                {
                    Id = _component.UCSystem.Make.make_auto,
                    Symbol = _component.UCSystem.Make.makeid,
                    Title = _component.UCSystem.Make.makedesc
                };
            }
            return new MakeForSelectionVwMdl();

        }

        public ModelForSelectionVwMdl getModel()
        {
            if (Id == 0 || DALSystem == null)
                return new ModelForSelectionVwMdl();

            if (DALSystem.make_auto == null)
                return new ModelForSelectionVwMdl();

            var model = _context.MODELs.Find((int)DALSystem.model_auto);
            if (model == null)
                return new ModelForSelectionVwMdl();
            return new ModelForSelectionVwMdl
            {
                Id = model.model_auto,
                FamilyId = getFamily().Id,
                MakeId = getMake().Id,
                Title = model.modeldesc
            };
        }

        public SetupViewModel GetSystemForSetupUpdate()
        {
            if (Id == 0 || DALSystem == null)
                return new SetupViewModel();
            decimal cost = 0;
            DateTime installationDate = DALSystem.CreatedDate ?? DateTime.MinValue;
            int userId = 0;
            int SmuAtInstall = DALSystem.SMU_at_install.LongNullableToInt();
            bool isReplaced = _context.ACTION_TAKEN_HISTORY.Where(m => m.system_auto_id_new == Id && m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory && m.recordStatus == (int)RecordStatus.Available).Count() > 0;
            var actionUpdatedUndercarriageSetup = _context.ACTION_TAKEN_HISTORY.Where(m => m.system_auto_id == Id && m.action_type_auto == (int)ActionType.UpdateUndercarriageSetupOnEquipment && m.recordStatus == (int)RecordStatus.Available);
            var actionInstallSystemRecords = _context.ACTION_TAKEN_HISTORY.Where(m => m.system_auto_id == Id && m.action_type_auto == (int)ActionType.InstallSystemOnEquipment && m.recordStatus == (int)RecordStatus.Available);
            if (!isReplaced && actionUpdatedUndercarriageSetup.Count() > 0)
            {
                cost = actionUpdatedUndercarriageSetup.First().cost;
                installationDate = actionUpdatedUndercarriageSetup.First().event_date;
                userId = actionUpdatedUndercarriageSetup.First().entry_user_auto.LongNullableToInt();
            }
            else if (!isReplaced && actionInstallSystemRecords.Count() > 0)
            {
                cost = actionInstallSystemRecords.First().cost;
                installationDate = actionInstallSystemRecords.First().event_date;
                userId = actionInstallSystemRecords.First().entry_user_auto.LongNullableToInt();
                SmuAtInstall = actionInstallSystemRecords.First().equipment_smu;
            }
            else //It seems that this system is installed by replace system action and doesn't have installation action
            {
                var systemReplacementRecords = _context.ACTION_TAKEN_HISTORY.Where(m => m.system_auto_id_new == Id && m.action_type_auto == (int)ActionType.ReplaceSystemFromInventory && m.recordStatus == (int)RecordStatus.Available);
                if (systemReplacementRecords.Count() > 0)
                {
                    cost = systemReplacementRecords.First().cost;
                    installationDate = systemReplacementRecords.First().event_date;
                    userId = systemReplacementRecords.First().entry_user_auto.LongNullableToInt();
                    SmuAtInstall = systemReplacementRecords.First().equipment_smu;
                }
            }

            return new SetupViewModel
            {
                Id = Id,
                Comment = DALSystem.notes,
                Components = Components.Where(m => !m.compartid_auto.isAChildCompart()).Select(m => m.ToComponentSetup()).ToList(),
                Cost = cost,
                EquipmentId = DALSystem.equipmentid_auto.LongNullableToInt(),
                Family = getFamily(),
                InstallationDate = installationDate,
                InstallOnEquipment = DALSystem.equipmentid_auto == null ? false : true,
                JobsiteId = DALSystem.crsf_auto.LongNullableToInt(),
                Make = getMake(),
                Model = getModel(),
                Result = new ResultMessage { Id = 0, OperationSucceed = true, LastMessage = "", ActionLog = "" },
                Serial = DALSystem.Serialno,
                SetupDate = installationDate,
                Side = GetSystemSide(),
                SmuAtInstall = SmuAtInstall,
                Type = GetSystemType(),
                UserId = userId,
                HoursAtInstall = GetSystemLife(installationDate),// (DALSystem.CMU ?? 0).LongNullableToInt(),
            };
        }

        public async Task<SetupViewModel> GetSystemForSetupUpdateAsync()
        {
            return await Task.Run(() => GetSystemForSetupUpdate());
        }
        private SetupViewModel UpdateSystemDetails(SetupViewModel SystemSetup)
        {
            if (Id == 0 || DALSystem == null)
            {
                SystemSetup.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    ActionLog = "System cannot be found!",
                    LastMessage = "System cannot be found!"
                };
                return SystemSetup;
            }

            DALSystem.Serialno = SystemSetup.Serial;
            DALSystem.notes = SystemSetup.Comment;
            DALSystem.CMU = SystemSetup.HoursAtInstall;
            _context.Entry(DALSystem).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                SystemSetup.Result = new ResultMessage
                {
                    Id = Id,
                    OperationSucceed = true,
                    ActionLog = "Updating system details succeeded",
                    LastMessage = "Updating system details succeeded"
                };
                return SystemSetup;
            }
            catch (Exception ex)
            {
                SystemSetup.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    ActionLog = "Updating system details failed!",
                    LastMessage = "Updating system details failed!" + ex.Message
                };
                return SystemSetup;
            }
        }

        public DateTime GetSystemDateOfInstallOnCurrentEquipment()
        {
            return _context.ACTION_TAKEN_HISTORY.Where(h => h.system_auto_id == pvId && h.equipmentid_auto == pvSystems.equipmentid_auto && h.action_type_auto == (int)ActionType.InstallSystemOnEquipment).Select(h => h.event_date).OrderByDescending(h => h).FirstOrDefault();
        }

        public SetupViewModel UpdateSystemInSetup(SetupViewModel SystemSetup, IUser _User)
        {
            if (Id == 0 || Components == null)
            {
                SystemSetup.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    ActionLog = "System cannot be found!",
                    LastMessage = "System cannot be found!"
                };
                return SystemSetup;
            }

            using (var action = new Action(_context, new EquipmentActionRecord
            {
                EquipmentId = SystemSetup.EquipmentId,
                ReadSmuNumber = SystemSetup.SmuAtInstall,
                TypeOfAction = ActionType.UpdateUndercarriageSetupOnEquipment,
                ActionDate = SystemSetup.InstallationDate.ToLocalTime().Date,
                ActionUser = _User,
                Cost = SystemSetup.Cost,
                Comment = SystemSetup.Serial + " Update Undercarriage",
            }, SystemSetup))
            {
                if (action.Operation.Start() == ActionStatus.Started && action.Operation.Validate() == ActionStatus.Valid && action.Operation.Commit() == ActionStatus.Succeed)
                {
                    SystemSetup.Result = new ResultMessage
                    {
                        Id = action.Operation.UniqueId,
                        OperationSucceed = true,
                        ActionLog = action.Operation.ActionLog,
                        LastMessage = action.Operation.Message
                    };
                }
                else
                {
                    SystemSetup.Result = new ResultMessage
                    {
                        Id = 0,
                        OperationSucceed = false,
                        ActionLog = action.Operation.ActionLog,
                        LastMessage = action.Operation.Message
                    };
                }
                return SystemSetup;
            }

            if (_context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == SystemSetup.EquipmentId && ((ActionType)m.action_type_auto == ActionType.InsertInspection || (ActionType)m.action_type_auto == ActionType.UpdateInspection)).Count() > 0)
            {
                SystemSetup.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    ActionLog = "This equipment has been inspected and cannot be updated!",
                    LastMessage = "This equipment has been inspected and cannot be updated!"
                };
                return SystemSetup;
            }

            SystemSetup = UpdateSystemDetails(SystemSetup);
            if (!SystemSetup.Result.OperationSucceed)
                return SystemSetup;
            var removiongComponents = Components.Where(m => !SystemSetup.Components.Any(n => m.equnit_auto == n.Id));
            var childToBeRemoved = new List<GENERAL_EQ_UNIT>();
            foreach (var parent in removiongComponents)
            {
                var childComparts = new Compart(_context, parent.compartid_auto).getChildComparts();
                childToBeRemoved.AddRange(Components.Where(m => childComparts.Any(k => k.compartid_auto == m.compartid_auto)).ToList());
            }
            removiongComponents.ToList().AddRange(childToBeRemoved);
            //All child components will be removed automatically
            if (removeComponents(removiongComponents.GroupBy(m => m.equnit_auto).Select(m => m.FirstOrDefault()).ToList()))
            {
                //Components which are not in the list of current components should be removed from installed components!
                //Just for monitoring purposes
                string message = "";
                message += "Components removed successfully";
            }


            //↓↓↓↓↓ Adding all child compartments
            List<ComponentSetup> ChildsList = new List<ComponentSetup>();
            foreach (var cmpnt in SystemSetup.Components.OrderBy(m => m.InstallDate))
            {
                var childCompartments = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == cmpnt.Compart.Id).Select(m => m.ParentCompartment).ToList();
                int childPos = cmpnt.Pos + 1;
                foreach (var childCompart in childCompartments)
                {
                    ChildsList.Add(new ComponentSetup
                    {
                        Brand = cmpnt.Brand,
                        BudgetLife = cmpnt.BudgetLife,
                        Compart = new CompartV { Id = childCompart.compartid_auto, CompartStr = childCompart.compartid, CompartTitle = childCompart.compart, CompartType = new CompartTypeV { Id = childCompart.comparttype_auto, Title = childCompart.LU_COMPART_TYPE.comparttype, Order = childCompart.LU_COMPART_TYPE.sorder ?? 1 } },
                        EquipmentSMU = cmpnt.EquipmentSMU,
                        Grouser = cmpnt.Grouser,
                        HoursAtInstall = cmpnt.HoursAtInstall,
                        Id = 0,
                        InstallCost = 0,
                        InstallDate = cmpnt.InstallDate.ToLocalTime().Date,
                        Note = cmpnt.Note,
                        Pos = childPos,
                        Result = new ResultMessage(),
                        ShoeSize = cmpnt.ShoeSize,
                        SystemId = cmpnt.SystemId,
                        Validity = cmpnt.Validity,
                        listPosition = -1,
                        Points = -1
                    });
                    childPos++;
                }
            }
            SystemSetup.Components.AddRange(ChildsList);
            //↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑

            foreach (var comp in SystemSetup.Components)
            {
                var componentParam = new SetupComponentParams
                {
                    BudgetLife = comp.BudgetLife,
                    CMU = comp.HoursAtInstall,
                    CompartId = comp.Compart.Id,
                    Cost = comp.InstallCost,
                    Id = comp.Id,
                    Life = comp.HoursAtInstall,
                    UserId = SystemSetup.UserId,
                    UserName = _User.userName
                };
                if (comp.Id == 0) //New Component Added
                {
                    var LogicalComponent = new Component(new UndercarriageContext());
                    comp.Result = LogicalComponent.CreateNewComponent(comp, SystemSetup.UserId, _User.userName).Result;
                    if (comp.Result.OperationSucceed)
                    {
                        IEquipmentActionRecord EquipmentActionForComp = new BLL.Core.Domain.EquipmentActionRecord
                        {
                            EquipmentId = SystemSetup.EquipmentId,
                            ReadSmuNumber = comp.EquipmentSMU,
                            TypeOfAction = ActionType.InstallComponentOnSystemOnEquipment,
                            ActionDate = comp.InstallDate.ToLocalTime().Date,
                            ActionUser = _User,
                            Cost = comp.InstallCost,
                            Comment = "Component Setup",
                        };
                        using (Action compAction = new Action(new UndercarriageContext(), EquipmentActionForComp, new BLL.Core.Domain.InstallComponentOnSystemParams { Id = LogicalComponent.Id, Position = (byte)comp.Pos, SystemId = SystemSetup.Id, side = SystemSetup.Side }))
                        {
                            if (compAction.Operation.Start() == ActionStatus.Started)
                                if (compAction.Operation.Validate() == ActionStatus.Valid)
                                    if (compAction.Operation.Commit() == ActionStatus.Succeed)
                                    {
                                        comp.Result.Id = compAction.Operation.UniqueId;
                                        comp.Result.OperationSucceed = true;
                                        comp.SystemId = SystemSetup.Id;
                                    }
                            comp.Result.LastMessage = compAction.Operation.Message;
                            comp.Result.ActionLog = compAction.Operation.ActionLog;
                        }
                    }
                }
                else
                {
                    var LogicalComponent = new Component(new DAL.UndercarriageContext(), comp.Id);
                    LogicalComponent.removeInstallationRecord();
                    LogicalComponent.UpdateComponentOnSetup(componentParam);
                    BLL.Interfaces.IEquipmentActionRecord EquipmentActionForComp = new BLL.Core.Domain.EquipmentActionRecord
                    {
                        EquipmentId = SystemSetup.EquipmentId,
                        ReadSmuNumber = comp.EquipmentSMU,
                        TypeOfAction = ActionType.InstallComponentOnSystemOnEquipment,
                        ActionDate = comp.InstallDate.ToLocalTime().Date,
                        ActionUser = _User,
                        Cost = 0,
                        Comment = "Component Setup"
                    };
                    using (Action compAction = new Action(new UndercarriageContext(), EquipmentActionForComp, new BLL.Core.Domain.InstallComponentOnSystemParams { Id = LogicalComponent.Id, Position = (byte)comp.Pos, SystemId = SystemSetup.Id, side = SystemSetup.Side }))
                    {
                        if (compAction.Operation.Start() == ActionStatus.Started)
                            if (compAction.Operation.Validate() == ActionStatus.Valid)
                                if (compAction.Operation.Commit() == ActionStatus.Succeed)
                                {
                                    comp.Result.Id = compAction.Operation.UniqueId;
                                    comp.Result.OperationSucceed = true;
                                    comp.SystemId = SystemSetup.Id;
                                }
                        comp.Result.LastMessage = compAction.Operation.Message;
                        comp.Result.ActionLog = compAction.Operation.ActionLog;
                    }
                }
            }
            return SystemSetup;
        }

        public SetupViewModel UpdateSystemInInventory(SetupViewModel SystemSetup, IUser _User)
        {
            if (Id == 0 || Components == null)
            {
                SystemSetup.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    ActionLog = "System cannot be found!",
                    LastMessage = "System cannot be found!"
                };
                return SystemSetup;
            }

            if (_context.ACTION_TAKEN_HISTORY.Where(m => m.system_auto_id == Id).Count() > 0)
            {
                SystemSetup.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    ActionLog = "At least one action applied to this system and cannot be updated!",
                    LastMessage = "At least one action applied to this system and cannot be updated!"
                };
                return SystemSetup;
            }

            SystemSetup = UpdateSystemDetails(SystemSetup);
            if (!SystemSetup.Result.OperationSucceed)
                return SystemSetup;
            var removiongComponents = Components.Where(m => !SystemSetup.Components.Any(n => m.equnit_auto == n.Id));
            var childToBeRemoved = new List<GENERAL_EQ_UNIT>();
            foreach (var parent in removiongComponents)
            {
                var childComparts = new Compart(_context, parent.compartid_auto).getChildComparts();
                childToBeRemoved.AddRange(Components.Where(m => childComparts.Any(k => k.compartid_auto == m.compartid_auto)).ToList());
            }
            removiongComponents.ToList().AddRange(childToBeRemoved);
            //All child components will be removed automatically
            if (removeComponents(removiongComponents.GroupBy(m => m.equnit_auto).Select(m => m.FirstOrDefault()).ToList()))
            {
                //Components which are not in the list of current components should be removed from installed components!
                //Just for monitoring purposes
                string message = "";
                message += "Components removed successfully";
            }

            //↓↓↓↓↓ Adding all child compartments
            List<ComponentSetup> ChildsList = new List<ComponentSetup>();
            foreach (var cmpnt in SystemSetup.Components.OrderBy(m => m.InstallDate))
            {
                var childCompartments = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == cmpnt.Compart.Id).Select(m => m.ParentCompartment).ToList();
                int childPos = cmpnt.Pos + 1;
                foreach (var childCompart in childCompartments)
                {
                    childCompart.LU_COMPART_TYPE = _context.LU_COMPART_TYPE.Find(childCompart.comparttype_auto);

                    ChildsList.Add(new ComponentSetup
                    {
                        Brand = cmpnt.Brand,
                        BudgetLife = cmpnt.BudgetLife,
                        Compart = new CompartV { Id = childCompart.compartid_auto, CompartStr = childCompart.compartid, CompartTitle = childCompart.compart, CompartType = new CompartTypeV { Id = childCompart.comparttype_auto, Title = childCompart.LU_COMPART_TYPE.comparttype, Order = childCompart.LU_COMPART_TYPE.sorder ?? 1 } },
                        EquipmentSMU = cmpnt.EquipmentSMU,
                        Grouser = cmpnt.Grouser,
                        HoursAtInstall = cmpnt.HoursAtInstall,
                        Id = 0,
                        InstallCost = 0,
                        InstallDate = cmpnt.InstallDate,
                        Note = cmpnt.Note,
                        Pos = childPos,
                        Result = new ResultMessage(),
                        ShoeSize = cmpnt.ShoeSize,
                        SystemId = cmpnt.SystemId,
                        Validity = cmpnt.Validity,
                        listPosition = -1,
                        Points = -1
                    });
                    childPos++;
                }
            }
            SystemSetup.Components.AddRange(ChildsList);
            //↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑

            foreach (var comp in SystemSetup.Components)
            {
                var componentParam = new SetupComponentParams
                {
                    BudgetLife = comp.BudgetLife,
                    CMU = comp.HoursAtInstall,
                    CompartId = comp.Compart.Id,
                    Cost = comp.InstallCost,
                    Id = comp.Id,
                    Life = comp.HoursAtInstall,
                    UserId = SystemSetup.UserId,
                    UserName = _User.userName
                };
                if (comp.Id == 0) //New Component Added
                {
                    comp.Result = new Component(new DAL.UndercarriageContext()).CreateNewComponent(comp, SystemSetup.UserId, _User.userName).Result;
                }
                else
                {
                    comp.Result = new Component(new DAL.UndercarriageContext(), comp.Id).UpdateComponentOnSetup(comp).Result;
                }
            }
            return SystemSetup;
        }

        public async Task<SetupViewModel> UpdateSystemInSetupAsync(SetupViewModel SystemSetup, IUser _User)
        {
            return await Task.Run(() => UpdateSystemInSetup(SystemSetup, _User));
        }

        public async Task<SetupViewModel> CreateAndUpdateSystemForNewUIAsync(SetupViewModel SystemSetup, IUser _user)
        {
            return await Task.Run(() => CreateAndUpdateSystemForNewUI(SystemSetup, _user));
        }

        public SetupViewModel CreateAndUpdateSystemForNewUI(SetupViewModel SystemSetup, IUser _user)
        {
            SystemSetup.Result.OperationSucceed = true;
            SystemSetup.UserId = _user.Id;
            bool newSystem = false;
            if (SystemSetup.Id == 0)
            {
                SystemSetup = CreateNewSystem(SystemSetup, SystemSetup.EquipmentId);
                newSystem = true;
            }
            if (!SystemSetup.Result.OperationSucceed)
                return SystemSetup;

            if (newSystem)
            {
                SystemSetup = CreateAndInstallComponentsNonAction(SystemSetup, _user);
                SystemSetup = InstallSystemOnEquipmentUsingAction(SystemSetup, _user);
            }
            else
            {
                Init(SystemSetup.Id, false);
                SystemSetup = UpdateSystemInSetup(SystemSetup, _user);
            }
            SystemSetup.Components = SystemSetup.Components.Where(m => !m.Compart.Id.isAChildCompart()).ToList();
            return SystemSetup;
        }

        public SetupViewModel CreateAndUpdateSystemForInventory(SetupViewModel SystemSetup, IUser _user)
        {
            SystemSetup.Result.OperationSucceed = true;
            SystemSetup.UserId = _user.Id;
            bool newSystem = false;
            if (SystemSetup.Id == 0)
            {
                SystemSetup = CreateNewSystem(SystemSetup, 0);
                newSystem = true;
            }
            if (!SystemSetup.Result.OperationSucceed)
                return SystemSetup;

            if (newSystem)
            {
                SystemSetup = CreateAndInstallComponentsNonAction(SystemSetup, _user);
            }
            else
            {
                Init(SystemSetup.Id, false);
                SystemSetup = UpdateSystemInInventory(SystemSetup, _user);
            }
            SystemSetup.Components = SystemSetup.Components.Where(m => !m.Compart.Id.isAChildCompart()).ToList();
            return SystemSetup;
        }

        public SetupViewModel InstallSystemOnEquipmentUsingAction(SetupViewModel SystemSetup, IUser _user)
        {
            BLL.Interfaces.IEquipmentActionRecord EquipmentAction = new BLL.Core.Domain.EquipmentActionRecord
            {
                EquipmentId = SystemSetup.EquipmentId,
                ReadSmuNumber = SystemSetup.SmuAtInstall,
                TypeOfAction = ActionType.InstallSystemOnEquipment,
                ActionDate = SystemSetup.InstallationDate.ToLocalTime().Date,
                ActionUser = _user,
                Cost = SystemSetup.Cost,
                Comment = SystemSetup.Comment
            };

            var _installSystemParam = new InstallSystemParams
            {
                Id = SystemSetup.Id,
                EquipmentId = SystemSetup.EquipmentId,
                side = SystemSetup.Side
            };
            using (var action = new Action(new UndercarriageContext(), EquipmentAction, _installSystemParam))
            {

                if (action.Operation.Start() == ActionStatus.Started)
                    if (action.Operation.Validate() == ActionStatus.Valid)
                        if (action.Operation.Commit() == ActionStatus.Succeed)
                        {
                            SystemSetup.Result.Id = action.Operation.UniqueId;
                            SystemSetup.Result.OperationSucceed = true;
                        }
                SystemSetup.Result.LastMessage = action.Operation.Message;
                SystemSetup.Result.ActionLog = action.Operation.ActionLog;
            }
            return SystemSetup;
        }

        private SetupViewModel CreateAndInstallComponentsNonAction(SetupViewModel SystemSetup, IUser user)
        {
            List<ComponentSetup> ChildsList = new List<ComponentSetup>();
            foreach (var cmpnt in SystemSetup.Components.OrderBy(m => m.InstallDate))
            {
                var childCompartments = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == cmpnt.Compart.Id).Select(m => m.ParentCompartment).ToList();
                int childPos = cmpnt.Pos + 1;
                foreach (var childCompart in childCompartments)
                {
                    ChildsList.Add(new ComponentSetup
                    {
                        Brand = cmpnt.Brand,
                        BudgetLife = cmpnt.BudgetLife,
                        Compart = new CompartV { Id = childCompart.compartid_auto, CompartStr = childCompart.compartid, CompartTitle = childCompart.compart, CompartType = new CompartTypeV { Id = childCompart.comparttype_auto, Title = childCompart.LU_COMPART_TYPE.comparttype, Order = childCompart.LU_COMPART_TYPE.sorder ?? 1 } },
                        EquipmentSMU = cmpnt.EquipmentSMU,
                        Grouser = cmpnt.Grouser,
                        HoursAtInstall = cmpnt.HoursAtInstall,
                        Id = 0,
                        InstallCost = 0,
                        InstallDate = cmpnt.InstallDate.ToLocalTime().Date,
                        Note = cmpnt.Note,
                        Pos = childPos,
                        Result = new ResultMessage(),
                        ShoeSize = cmpnt.ShoeSize,
                        SystemId = cmpnt.SystemId,
                        Validity = cmpnt.Validity,
                        listPosition = -1,
                        Points = -1
                    });
                    childPos++;
                }
            }
            SystemSetup.Components.AddRange(ChildsList);
            foreach (var cmpnt in SystemSetup.Components.OrderBy(m => m.InstallDate))
            {
                if (cmpnt.Id != 0)
                {
                    cmpnt.Result.Id = cmpnt.Id;
                    cmpnt.Result.LastMessage = "This component is already created!";
                    cmpnt.Result.OperationSucceed = false;
                    continue;
                }
                var component = new Component(new DAL.UndercarriageContext(), cmpnt.Id);
                var created = component.CreateNewComponent(cmpnt, user.Id, user.userName);
                if (!created.Result.OperationSucceed)
                {
                    cmpnt.Result = created.Result;
                    continue;
                }
                cmpnt.Id = component.Id;

                EquipmentActionRecord _nonActionRecord = new EquipmentActionRecord
                {
                    EquipmentId = SystemSetup.EquipmentId,
                    ReadSmuNumber = cmpnt.EquipmentSMU,
                    TypeOfAction = ActionType.InstallComponentOnSystemOnEquipment,
                    ActionDate = cmpnt.InstallDate.ToLocalTime().Date,
                    ActionUser = user,
                    Cost = cmpnt.InstallCost,
                    Comment = cmpnt.Note
                };

                var _installComponentParam = new InstallComponentOnSystemParams
                {
                    Id = cmpnt.Id,
                    SystemId = SystemSetup.Id,
                    side = SystemSetup.Side,
                    Position = (byte)cmpnt.Pos
                };

                cmpnt.Result = component.InstallComponentOnSystemNonAction(cmpnt, _nonActionRecord, SystemSetup.Id, SystemSetup.HoursAtInstall).Result;
                if (cmpnt.Result.OperationSucceed)
                    cmpnt.SystemId = SystemSetup.Id;
            }
            SystemSetup.Components = SystemSetup.Components.Where(m => m.listPosition > -1 && m.Points > -1).ToList(); //Child compartments doesn't need to be returned
            return SystemSetup;
        }
        /// <summary>
        /// Warning! This method just makes equipmentid_auto NULL not calling any function to update life or doing it properly!
        /// </summary>
        /// <returns></returns>
        public bool detachSystemNoAction()
        {
            DALSystem.equipmentid_auto = null;
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IQueryable<GENERAL_EQ_UNIT> getSystemComponents(int SystemId, DateTime Date)
        {
            var _dalSystem = getDALSystem(SystemId);
            if (_dalSystem == null) return new List<GENERAL_EQ_UNIT>().AsQueryable();
            var componentIds = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == SystemId).Select(m => m.equnit_auto).ToList();
            var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.action_type_auto == (int)ActionType.ReplaceComponentWithNew && m.event_date > Date && m.system_auto_id == SystemId && m.recordStatus == (int)RecordStatus.Available).OrderByDescending(m => m.event_date);
            var resultIds = new List<long>();
            foreach (var id in componentIds)
            {
                int _replacedId = (int)id;
                bool _repeat = true;
                while (_repeat)
                {
                    var _action = actions.Where(m => m.equnit_auto_new == _replacedId).FirstOrDefault();
                    if (_action != null)
                    {
                        _replacedId = (int)_action.system_auto_id;
                    }
                    else
                        _repeat = false;
                }
                resultIds.Add(_replacedId);
            }
            return _context.GENERAL_EQ_UNIT.Where(m => resultIds.Any(p => m.equnit_auto == p));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public SystemHistoryTemplate getSystemHistoryTemplate(int systemId, bool doInit = false)
        {
            var _system = getDALSystem(systemId, doInit);
            if (_system == null) return new SystemHistoryTemplate { Id = 0, Side = Side.Unknown, SystemTypeId = UCSystemType.Unknown, _order = 9, ComponentsHistory = new List<ComponentHistoryTemplate>() };
            var _components = _context.GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == systemId).Select(m => new ComponentHistoryTemplate { Id = (int)m.equnit_auto, Pos = m.pos ?? 0, Side = (Side)(m.side ?? 0), CompartTypeId = m.LU_COMPART.comparttype_auto, _order = (m.LU_COMPART.LU_COMPART_TYPE.sorder ?? 99) }).ToList();
            var _side = GetSystemSide();
            var _type = GetSystemType();
            return new SystemHistoryTemplate { Id = systemId, Side = _side, SystemTypeId = _type, _order = getSystemOrder(_side, _type), ComponentsHistory = _components };
        }

        public int getSystemOrder(Side side, UCSystemType type)
        {
            if (side == Side.Left && type == UCSystemType.Chain) return 1;
            if (side == Side.Left && type == UCSystemType.Frame) return 2;
            if (side == Side.Right && type == UCSystemType.Chain) return 3;
            if (side == Side.Right && type == UCSystemType.Frame) return 4;
            return 99;
        }

        public DateTime getSystemSetupDate(int equipmentId, int systemId)
        {
            var _setup = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == equipmentId && m.system_auto_id == systemId && m.recordStatus == (int)RecordStatus.Available && (m.action_type_auto == (int)ActionType.InstallSystemOnEquipment || m.action_type_auto == (int)ActionType.UpdateUndercarriageSetupOnEquipment)).FirstOrDefault();
            if (_setup == null) return DateTime.MinValue;
            return _setup.event_date;
        }
        //↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔ END OF UCSYSTEM CLASS ↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔↔
    }
}
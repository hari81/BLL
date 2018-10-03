using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using DAL;
using System.Security.Claims;
using BLL.Extensions;
using BLL.Interfaces;
using BLL.Core.ViewModel;

namespace BLL.Core.Domain
{
    public class UserRequest
    {
        private string AspNetId;
        private int userTableId;
        private SharedContext _context;
        private bool Initialized = false;
        private USER_TABLE UserTable;
        public UserRequest(DbContext context, System.Security.Principal.IPrincipal LoggedInUser)
        {
            _context = (SharedContext)context;
            Initialized = Init(LoggedInUser);
        }

        bool Init(System.Security.Principal.IPrincipal CurrentUser)
        {
            if (!CurrentUser.Identity.IsAuthenticated)
                return false;
            var identity = (ClaimsIdentity)CurrentUser.Identity;
            IEnumerable<Claim> claims = identity.Claims.Where(m => m.Type == "sub");

            IEnumerable<Claim> nameIdentifiers = identity.Claims.Where(m => m.Type == ClaimTypes.NameIdentifier);
            if (claims.Count() == 0 && nameIdentifiers.Count() == 0)
                return false;

            if (claims.Count() > 0)
                AspNetId = claims.First().Value;
            else AspNetId = nameIdentifiers.First().Value;

            AspNetId = claims.First().Value;
            var users = _context.USER_TABLE.Where(m => m.AspNetUserId == AspNetId);
            if (users.Count() == 0)
                return false;
            UserTable = users.First();
            userTableId = longNullableToint(users.First().user_auto);
            return true;
        }

        public int getUserTableId()
        {
            return userTableId;
        }

        public string getUserName()
        {
            if (UserTable == null)
                return "Guest";
            return UserTable.username;
        }
        public IUser getUser()
        {

            if (UserTable == null)
                return new User
                {
                    Id = 0,
                    userName = "UNKNOWN",
                    userStrId = "UNKNOWN"
                };

            return new User
            {
                Id = UserTable.user_auto.LongNullableToInt(),
                userName = UserTable.username,
                userStrId = UserTable.userid
            };
        }
        public int longNullableToint(long? number)
        {
            if (number == null)
                return 0;
            if (number > Int32.MaxValue) //:) So Stupid
                return Int32.MaxValue;
            if (number < Int32.MinValue) // :))
                return Int32.MinValue;
            try { return (int)number; } catch { return 0; }
        }



        public List<ucDashbordViewModel> getEquipmentDetailsList(int PageNo, int PageSize, System.Security.Principal.IPrincipal User)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<ucDashbordViewModel>();
            if (!Initialized)
                return result;
            var customerIds = new GETCore.Classes.CustomerManagement().getListOfActiveCustomersForLoggedInUser(userTableId).Select(m => m.customerId);
            var GETEquipments = new List<Core.Domain.IdAndDate>();
            foreach (var id in customerIds)
            {
                GETEquipments.AddRange(new GETCore.Classes.GETEquipment().getEquipmentIdAndDateByCustomer(id, User));
            }
            GETEquipments = GETEquipments.GroupBy(m => m).Select(m => m.First()).OrderByDescending(m => m.Id).OrderByDescending(m => m.Date).Skip(PageNo * PageSize).Take(PageSize).ToList();
            foreach (var equipmentIdAndDate in GETEquipments)
            {
                var logicalEquipment = new Equipment(new DAL.UndercarriageContext(), longNullableToint(equipmentIdAndDate.Id));
                var latestInspection = logicalEquipment.GetLatestInspection(DateTime.Now);
                //var systemDetails = logicalEquipment.getSystemDetailsList(DateTime.Now);
                try
                {
                    var customer = logicalEquipment.getDALCustomer();
                    var Eqmake = logicalEquipment.GetEquipmentMake();
                    var jSite = logicalEquipment.getEquipmentJobSite();
                    string EqjobsiteName = jSite == null ? "-" : jSite.site_name;
                    var componentsOverView = logicalEquipment.getEquipmentComponentsWorn(DateTime.Now);

                    var k = new ucDashbordViewModel
                    {
                        Id = logicalEquipment.Id,
                        customerId = customer == null ? 0 : customer.customer_auto.LongNullableToInt(),
                        customerName = customer == null ? "-" : customer.cust_name,
                        jobsiteId = logicalEquipment.DALEquipment.crsf_auto.LongNullableToInt(),
                        jobsiteName = EqjobsiteName,
                        family = logicalEquipment.GetFamilyName(logicalEquipment.GetEquipmentFamily()),
                        familyId = (int)logicalEquipment.GetEquipmentFamily(),
                        lastInspectionId = latestInspection == null ? 0 : latestInspection.inspection_auto,
                        lastInspectionDate = latestInspection == null ? "Not inspected yet!" : latestInspection.inspection_date.ToString("dd MMM yyyy"),
                        quoteId = latestInspection == null ? 0 : (latestInspection.quote_auto.HasValue ? (int)latestInspection.quote_auto : 0),
                        ltd = logicalEquipment.GetEquipmentLife(DateTime.Now),
                        make = Eqmake.Description,
                        makeId = Eqmake.Id,
                        model = logicalEquipment.DALEquipment.LU_MMTA.MODEL.modeldesc,
                        modelId = logicalEquipment.DALEquipment.LU_MMTA.MODEL.model_auto,
                        serial = logicalEquipment.DALEquipment.serialno,
                        unit = logicalEquipment.DALEquipment.unitno,
                        smu = logicalEquipment.GetSerialMeterUnit(DateTime.Now),
                        EvalL = componentsOverView.Where(m => m.side == Side.Left).Select(m => m.Eval).Count() > 0 ? componentsOverView.Where(m => m.side == Side.Left).Select(m => m.Eval).Max() : "U", //logicalEquipment.toEvalChar(systemDetails.Where(m => m.Side == Side.Left).Count() > 0 ? systemDetails.Where(m => m.Side == Side.Left).Max(m => m.Eval) : EvalCode.U),
                        EvalR = componentsOverView.Where(m => m.side == Side.Right).Select(m => m.Eval).Count() > 0 ? componentsOverView.Where(m => m.side == Side.Right).Select(m => m.Eval).Max() : "U",//logicalEquipment.toEvalChar(systemDetails.Where(m => m.Side == Side.Right).Count() > 0 ? systemDetails.Where(m => m.Side == Side.Right).Max(m => m.Eval) : EvalCode.U),
                        overAllEvalNumber = componentsOverView.Select(m => m.wornPercentage).Count() > 0 ? componentsOverView.Select(m => m.wornPercentage).Max() : -1,
                    };
                    result.Add(k);
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }
            return result;
        }
        /// <summary>
        /// This method is the newer version of the getEquipmentDetailsList with less parameters
        /// </summary>
        /// <param name="PageNo"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>
        public List<ucDashbordViewModel> getEquipmentDetailsList(int PageNo, int PageSize, int CustomerId, int JobSiteId, int EquipmentId, int FamilyId, int MakeId, int ModelId)
        {
            var result = new List<ucDashbordViewModel>();
            var FilteredEquipments = new GETCore.Classes.GETEquipment().getEquipmentIdAndDateByCondition(PageNo, PageSize, new UserSelectedIds { CustomerId = CustomerId, JobSiteId = JobSiteId, EquipmentId = EquipmentId, FamilyId = FamilyId, MakeId = MakeId, ModelId = ModelId}, getUserTableId());
            foreach (var equipment in FilteredEquipments)
            {
                var logicalEquipment = new Equipment(new DAL.UndercarriageContext(), longNullableToint(equipment.EquipmentId));
                try
                {
                    var jSite = logicalEquipment.getEquipmentJobSite();
                    var Eqmake = logicalEquipment.GetEquipmentMake();
                    var latestInspection = logicalEquipment.GetLatestInspection(DateTime.Now);
                    var customer = logicalEquipment.getDALCustomer();
                    
                    string EqjobsiteName = jSite == null ? "-" : jSite.site_name;
                    var componentsOverView = logicalEquipment.getEquipmentComponentsWorn(DateTime.Now);

                    var k = new ucDashbordViewModel
                    {
                        Id = logicalEquipment.Id,
                        customerId = customer == null ? 0 : customer.customer_auto.LongNullableToInt(),
                        customerName = customer == null ? "-" : customer.cust_name,
                        jobsiteId = logicalEquipment.DALEquipment.crsf_auto.LongNullableToInt(),
                        jobsiteName = EqjobsiteName,
                        family = logicalEquipment.GetFamilyName(logicalEquipment.GetEquipmentFamily()),
                        familyId = (int)logicalEquipment.GetEquipmentFamily(),
                        lastInspectionId = latestInspection == null ? 0 : latestInspection.inspection_auto,
                        lastInspectionDate = latestInspection == null ? "Not inspected yet!" : latestInspection.inspection_date.ToString("dd MMM yyyy"),
                        quoteId = latestInspection == null ? 0 : (latestInspection.quote_auto.HasValue ? (int)latestInspection.quote_auto : 0),
                        ltd = logicalEquipment.GetEquipmentLife(DateTime.Now),
                        make = Eqmake.Description,
                        makeId = Eqmake.Id,
                        model = logicalEquipment.DALEquipment.LU_MMTA.MODEL.modeldesc,
                        modelId = logicalEquipment.DALEquipment.LU_MMTA.MODEL.model_auto,
                        serial = logicalEquipment.DALEquipment.serialno,
                        unit = logicalEquipment.DALEquipment.unitno,
                        smu = logicalEquipment.GetSerialMeterUnit(DateTime.Now),
                        EvalL = componentsOverView.Where(m => m.side == Side.Left).Select(m => m.Eval).Count() > 0 ? componentsOverView.Where(m => m.side == Side.Left).Select(m => m.Eval).Max() : "U", //logicalEquipment.toEvalChar(systemDetails.Where(m => m.Side == Side.Left).Count() > 0 ? systemDetails.Where(m => m.Side == Side.Left).Max(m => m.Eval) : EvalCode.U),
                        EvalR = componentsOverView.Where(m => m.side == Side.Right).Select(m => m.Eval).Count() > 0 ? componentsOverView.Where(m => m.side == Side.Right).Select(m => m.Eval).Max() : "U",//logicalEquipment.toEvalChar(systemDetails.Where(m => m.Side == Side.Right).Count() > 0 ? systemDetails.Where(m => m.Side == Side.Right).Max(m => m.Eval) : EvalCode.U),
                        overAllEvalNumber = componentsOverView.Select(m => m.wornPercentage).Count() > 0 ? componentsOverView.Select(m => m.wornPercentage).Max() : -1,
                    };
                    result.Add(k);
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }
            return result;
        }

        public ComponentViewResult getDashboardComponentView(int PageNo, int PageSize, List<SearchItem> SearchItems, int _clientReqId) {
            var searchResult = new GETCore.Classes.GETEquipment().getEquipmentIdAndDateAdvancedSearch(PageNo, PageSize, SearchItems, getUserTableId(), true);
            var componentView = new Equipment(new DAL.UndercarriageContext()).getEquipmentsComponentView(searchResult.Result);
            return new ComponentViewResult {
                ResultList = componentView,
                SearchResult = searchResult,
                _clientReqId =  _clientReqId
            };
        }

        public ComponentSearchViewModel getDashboardEquipmentView(int PageNo, int PageSize, List<SearchItem> SearchItems, int _clientReqId)
        {
            var searchResult = new GETCore.Classes.GETEquipment().getEquipmentIdAndDateAdvancedSearch(PageNo, PageSize, SearchItems, getUserTableId());
            var result = new List<ucDashbordViewModel>();
            foreach (var equipment in searchResult.Result)
            {
                var logicalEquipment = new Equipment(new DAL.UndercarriageContext(), longNullableToint(equipment.Id));
                try
                {
                    var jSite = logicalEquipment.getEquipmentJobSite();
                    var Eqmake = logicalEquipment.GetEquipmentMake();
                    var latestInspection = logicalEquipment.GetLatestInspection(DateTime.Now);
                    var customer = logicalEquipment.getDALCustomer();

                    string EqjobsiteName = jSite == null ? "-" : jSite.site_name;
                    var componentsOverView = logicalEquipment.getEquipmentComponentsWornVwMdl(DateTime.Now.ToLocalTime());

                    var k = new ucDashbordViewModel
                    {
                        Id = logicalEquipment.Id,
                        customerId = customer == null ? 0 : customer.customer_auto.LongNullableToInt(),
                        customerName = customer == null ? "-" : customer.cust_name,
                        jobsiteId = logicalEquipment.DALEquipment.crsf_auto.LongNullableToInt(),
                        jobsiteName = EqjobsiteName,
                        family = logicalEquipment.GetFamilyName(logicalEquipment.GetEquipmentFamily()),
                        familyId = (int)logicalEquipment.GetEquipmentFamily(),
                        lastInspectionId = latestInspection == null ? 0 : latestInspection.inspection_auto,
                        lastInspectionDate = latestInspection == null ? "Not inspected yet!" : latestInspection.inspection_date.ToString("dd MMM yyyy"),
                        quoteId = latestInspection == null ? 0 : (latestInspection.quote_auto.HasValue ? (int)latestInspection.quote_auto : 0),
                        ltd = logicalEquipment.GetEquipmentLife(DateTime.Now),
                        make = Eqmake.Description,
                        makeId = Eqmake.Id,
                        model = logicalEquipment.DALEquipment.LU_MMTA.MODEL.modeldesc,
                        modelId = logicalEquipment.DALEquipment.LU_MMTA.MODEL.model_auto,
                        serial = logicalEquipment.DALEquipment.serialno,
                        unit = logicalEquipment.DALEquipment.unitno,
                        smu = logicalEquipment.GetSerialMeterUnit(DateTime.Now),
                        EvalL = componentsOverView.Where(m => m.side == Side.Left).Select(m => m.Eval).Count() > 0 ? componentsOverView.Where(m => m.side == Side.Left).Select(m => m.Eval).Max() : "U", //logicalEquipment.toEvalChar(systemDetails.Where(m => m.Side == Side.Left).Count() > 0 ? systemDetails.Where(m => m.Side == Side.Left).Max(m => m.Eval) : EvalCode.U),
                        EvalR = componentsOverView.Where(m => m.side == Side.Right).Select(m => m.Eval).Count() > 0 ? componentsOverView.Where(m => m.side == Side.Right).Select(m => m.Eval).Max() : "U",//logicalEquipment.toEvalChar(systemDetails.Where(m => m.Side == Side.Right).Count() > 0 ? systemDetails.Where(m => m.Side == Side.Right).Max(m => m.Eval) : EvalCode.U),
                        overAllEvalNumber = componentsOverView.Select(m => m.wornPercentage).Count() > 0 ? componentsOverView.Select(m => m.wornPercentage).Max() : -1,
                    };
                    result.Add(k);
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }
            return new ComponentSearchViewModel
            {
                ResultList = result,
                SearchResult = searchResult,
                _clientReqId = _clientReqId
            };
        }

        /// <summary>
        /// returns the number of items for a search condition
        /// </summary>
        /// <returns>number of result</returns>
        public int getEquipmentDetailsCount(int CustomerId, int JobSiteId, int EquipmentId, int FamilyId, int MakeId, int ModelId)
        {
            return new GETCore.Classes.GETEquipment().getEquipmentIdAndDateByCondition(1, 10000, new UserSelectedIds { CustomerId = CustomerId, JobSiteId = JobSiteId, EquipmentId = EquipmentId, FamilyId = FamilyId, MakeId = MakeId, ModelId = ModelId }, getUserTableId()).Count();   
        }

        public List<int> getEquipmentIdsForThisUser()
        {
            return new BLL.Core.Domain.UserAccess(new SharedContext(), userTableId).getAccessibleEquipments().Select(m => m.equipmentid_auto).ToList().LongNullableToInt();
        }

        public List<ComponentTypeVwMdl> getGetComponentList()
        {
            int getSystem = 8;

            var result = new List<ComponentTypeVwMdl>();
            if (!Initialized)
                return result;
            return _context.LU_COMPART_TYPE.Where(m => m.system_auto == getSystem).Select(m => new ComponentTypeVwMdl { Id = m.comparttype_auto, Name = m.comparttype, CategoryId = m.system_auto }).ToList();
        }

        public List<ComponentTypeVwMdl> getDumpBodyComponentList()
        {
            int dumpBodySystem = 11;

            var result = new List<ComponentTypeVwMdl>();
            if (!Initialized)
                return result;
            return _context.LU_COMPART_TYPE.Where(m => m.system_auto == dumpBodySystem).Select(m => new ComponentTypeVwMdl { Id = m.comparttype_auto, Name = m.comparttype, CategoryId = m.system_auto }).ToList();
        }

        public List<CustomerForSelectionVwMdl> getCustomerList()
        {
            var result = new List<CustomerForSelectionVwMdl>();
            if (!Initialized)
                return result;
            var customers = new GETCore.Classes.CustomerManagement().getListOfActiveCustomersForLoggedInUser(userTableId);
            return customers.Select(m => new CustomerForSelectionVwMdl { Id = longNullableToint(m.customerId), Title = m.customerName }).ToList();
        }

        public List<ModelForSelectionVwMdl> getModelList()
        {
            var result = new List<ModelForSelectionVwMdl>();
            if (!Initialized)
                return result;
            return _context.LU_MMTA.Select(m => new ModelForSelectionVwMdl { Id = m.model_auto, Title = m.MODEL.modeldesc, FamilyId = m.type_auto, MakeId = m.make_auto }).GroupBy(m => new { m.MakeId, m.FamilyId, m.Id }).Select(m => m.FirstOrDefault()).Where(m => !m.Equals(null)).ToList();
        }

        public List<CustomerForSelectionVwMdl> getCustomerListForSelection(int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<CustomerForSelectionVwMdl>();
            if (!Initialized)
                return result;
            var customers = new GETCore.Classes.CustomerManagement().getListOfActiveCustomersForLoggedInUser(userTableId);
            return customers.Select(m => new CustomerForSelectionVwMdl { Id = longNullableToint(m.customerId), Title = m.customerName }).GroupBy(m => m.Id).Select(m => m.First()).Skip(PageNo * PageSize).Take(PageSize).OrderBy(m=>m.Title).ToList();
        }

        public List<JobSiteForSelectionVwMdl> getJobSiteListForSelection(int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<JobSiteForSelectionVwMdl>();
            if (!Initialized)
                return result;
            
            var jobsites = new BLL.Core.Domain.UserAccess(new SharedContext(), userTableId).getAccessibleJobsites().ToList();
            return jobsites.Select(m => new JobSiteForSelectionVwMdl { Id = longNullableToint(m.crsf_auto), Title = m.site_name, CustomerId = m.customer_auto.LongNullableToInt() }).GroupBy(m => m.Id).Select(m => m.First()).Skip(PageNo * PageSize).Take(PageSize).OrderBy(m=> m.Title).ToList();
        }

        public List<EquipmentForSelectionVwMdl> getEquipmentListForSelection(int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<EquipmentForSelectionVwMdl>();
            if (!Initialized)
                return result;

            return new BLL.Core.Domain.UserAccess(new SharedContext(), userTableId).getAccessibleEquipments().ToList().Select(m => new EquipmentForSelectionVwMdl { Id = m.equipmentid_auto.LongNullableToInt(), JobSiteId = m.crsf_auto.LongNullableToInt(), Serial = m.serialno, Unit = m.unitno }).GroupBy(m => m.Id).Select(m => m.First()).Skip(PageNo * PageSize).Take(PageSize).OrderBy(m=>m.Serial).ToList();
        }

        public List<EquipmentForSetupVwMdl> getEquipmentListForSetup(int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<EquipmentForSetupVwMdl>();
            if (!Initialized)
                return result;
            
            return new BLL.Core.Domain.UserAccess(new SharedContext(), userTableId).getAccessibleEquipments().ToList().Select(m => new EquipmentForSetupVwMdl { Id = m.equipmentid_auto.LongNullableToInt(), JobSiteId = m.crsf_auto.LongNullableToInt(), Serial = m.serialno, Unit = m.unitno, SetupDate = m.purchase_date ?? DateTime.MinValue, SmuAtSetup = m.smu_at_start.LongNullableToInt() }).GroupBy(m => m.Id).Select(m => m.First()).Skip(PageNo * PageSize).Take(PageSize).OrderBy(m=> m.Serial).ToList();
        }
        /// <summary>
        /// Returns a list of all systems in a specific jobsite which user has access to
        /// </summary>
        /// <param name="JobSiteId"></param>
        /// <param name="PageNo"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>
        public List<SystemForSetupVwMdl> getSystemListForSetupInInventory(int JobSiteId, int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<SystemForSetupVwMdl>();
            if (!Initialized)
                return result;
            var _access = new BLL.Core.Domain.UserAccess(new SharedContext(), userTableId);
            if (!_access.hasAccessToJobsite(JobSiteId))
                return result;
            var jobsites = _access.getAccessibleJobsites().Select(k => k.crsf_auto).ToList();
            var systems = _context.LU_Module_Sub.Where(m => jobsites.Any(k => m.crsf_auto == k) && (m.equipmentid_auto == null || m.equipmentid_auto == 0)).ToList();
            result = systems.Select(m => new SystemForSetupVwMdl { Id = m.Module_sub_auto.LongNullableToInt(), JobsiteId = m.crsf_auto.LongNullableToInt(), Serial = m.Serialno, EquipmentId = m.equipmentid_auto.LongNullableToInt(), Side = Side.Unknown, SystemType = (UCSystemType)m.systemTypeEnumIndex }).GroupBy(m => m.Id).Select(m => m.First()).Skip(PageNo * PageSize).Take(PageSize).ToList();
            foreach (var system in result)
            {
                var logicalSystem = new UCSystem(new DAL.UndercarriageContext(), system.Id);
                system.Make = logicalSystem.getMake();
                system.Model = logicalSystem.getModel();
                system.Family = logicalSystem.getFamily();
                system.Side = logicalSystem.side;
                system.SystemType = logicalSystem.GetSystemType();
            }
            return result.OrderBy(m=>m.Serial).ToList();
        }
        /// <summary>
        /// Rerturns a system if user has access to and system is not installed on any equipment
        /// </summary>
        /// <param name="SystemId"></param>
        /// <returns></returns>
        public SystemForSetupVwMdl GetSelectedSystemForInventorySelection(int SystemId)
        {
            var result = new SystemForSetupVwMdl();
            if (!Initialized)
                return result;

            var m = _context.LU_Module_Sub.Find(SystemId);
            if (m == null || m.equipmentid_auto != null || m.equipmentid_auto > 0) return result;
            result = new SystemForSetupVwMdl { Id = m.Module_sub_auto.LongNullableToInt(), JobsiteId = m.crsf_auto.LongNullableToInt(), Serial = m.Serialno, EquipmentId = m.equipmentid_auto.LongNullableToInt(), Side = Side.Unknown, SystemType = (UCSystemType)m.systemTypeEnumIndex };
            var logicalSystem = new UCSystem(new DAL.UndercarriageContext(), SystemId);
            result.Make = logicalSystem.getMake();
            result.Model = logicalSystem.getModel();
            result.Family = logicalSystem.getFamily();
            result.Side = logicalSystem.side;
            result.SystemType = logicalSystem.GetSystemType();
            return result;
        }

        public List<FamilyForSelectionVwMdl> getFamilyListForSelection(int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<FamilyForSelectionVwMdl>();
            if (!Initialized)
                return result;
            var all_types = _context.TYPE.Select(m => new FamilyForSelectionVwMdl { Id = m.type_auto, Title = m.typedesc, Symbol = m.typeid }).GroupBy(m => m.Id).Select(m => m.FirstOrDefault()).OrderBy(m => m.Id).Skip(PageNo * PageSize).Take(PageSize).OrderBy(m => m.Title);
            var mmta_types = _context.LU_MMTA.Where(m => all_types.Any(k => k.Id == m.type_auto));
            var equipments = _context.EQUIPMENT.Where(m => mmta_types.Any(k => k.mmtaid_auto == m.mmtaid_auto));
            foreach (var type in all_types.ToList())
            {
                result.Add(
                    new FamilyForSelectionVwMdl
                    {
                        Id = type.Id,
                        Title = type.Title,
                        Symbol = type.Symbol,
                        ExistingCount = equipments.Where(m => mmta_types.Where(k=> k.type_auto == type.Id).Any(k => k.mmtaid_auto == m.mmtaid_auto)).Count()
                    }
                    );
            }
            return result;
        }

        public List<MakeForSelectionVwMdl> getMakeListForSelection(int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<MakeForSelectionVwMdl>();
            if (!Initialized)
                return result;
            var all_makes = _context.MAKE.Where(m => m.Undercarriage == true).Select(m => new MakeForSelectionVwMdl { Id = m.make_auto, Title = m.makedesc, Symbol = m.makeid }).GroupBy(m => m.Id).Select(m => m.FirstOrDefault()).OrderBy(m => m.Id).Skip(PageNo * PageSize).Take(PageSize).OrderBy(m => m.Title);
            var mmta_makes = _context.LU_MMTA.Where(m => all_makes.Any(k => k.Id == m.make_auto));
            var equipments = _context.EQUIPMENT.Where(m => mmta_makes.Any(k => k.mmtaid_auto == m.mmtaid_auto));
            foreach (var make in all_makes.ToList())
            {
                result.Add(
                    new MakeForSelectionVwMdl
                    {
                        Id = make.Id,
                        Title = make.Title,
                        Symbol = make.Symbol,
                        ExistingCount = equipments.Where(m => mmta_makes.Where(k => k.make_auto == make.Id).Any(k => k.mmtaid_auto == m.mmtaid_auto)).Count()
                    }
                    );
            }
            return result;
        }

        public List<MakeForSelectionVwMdl> getActiveMakeListForSelection(int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<MakeForSelectionVwMdl>();
            if (!Initialized)
                return result;
            var activeMakes = (from e in _context.EQUIPMENT join l in _context.LU_MMTA on e.mmtaid_auto equals l.mmtaid_auto select l.make_auto).Distinct().ToList();
            return _context.MAKE.Where(m => m.Undercarriage == true).Select(m => new MakeForSelectionVwMdl { Id = m.make_auto, Title = m.makedesc, Symbol = m.makeid }).Where(m => activeMakes.Contains(m.Id)).GroupBy(m => m.Id).Select(m => m.FirstOrDefault()).OrderBy(m => m.Id).Skip(PageNo * PageSize).Take(PageSize).ToList();
        }

        public List<ModelForSelectionVwMdl> getModelListForSelection(int PageNo, int PageSize)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var result = new List<ModelForSelectionVwMdl>();
            if (!Initialized)
                return result;
            var all_models = _context.MODEL.Select(m => new ModelForSelectionVwMdl { Id = m.model_auto, Title = m.modeldesc}).OrderBy(m => m.Id).Skip(PageNo * PageSize).Take(PageSize).OrderBy(m=>m.Title);
            var mmta_models = _context.LU_MMTA.Where(m => all_models.Any(k => k.Id == m.model_auto));//.Select(m => new ModelForSelectionVwMdl { Id = m.model_auto, Title = m.MODEL.modeldesc, FamilyId = m.type_auto, MakeId = m.make_auto}).GroupBy(m => new { m.MakeId, m.FamilyId, m.Id }).Select(m => m.FirstOrDefault()).OrderBy(m => m.Id).Skip(PageNo * PageSize).Take(PageSize).OrderBy(m => m.Title);
            var equipments = _context.EQUIPMENT.Where(m => mmta_models.Any(k => k.mmtaid_auto == m.mmtaid_auto));
            foreach (var model in all_models.ToList()) {
                result.Add(
                    new ModelForSelectionVwMdl {
                        Id = model.Id,
                        Title = model.Title,
                        FamilyId = mmta_models.Where(m=> m.model_auto == model.Id).Count() > 0 ? mmta_models.Where(m => m.model_auto == model.Id).First().type_auto : 0,
                        MakeId = mmta_models.Where(m => m.model_auto == model.Id).Count() > 0 ? mmta_models.Where(m => m.model_auto == model.Id).First().make_auto : 0,
                        ExistingCount = equipments.Where(m=> mmta_models.Where(k => k.model_auto == model.Id).Any(k=> k.mmtaid_auto == m.mmtaid_auto)).Count()
                    }
                    );
            }
            return result;
        }

        public List<ModelSelectedViewModel> getModelListForImplementTemplateSetup()
        {
            var result = new List<ModelSelectedViewModel>();
            if (!Initialized)
                return result;
            return _context.MODEL.Select(m => new ModelSelectedViewModel { Id = m.model_auto, Name = m.modeldesc, Selected = false }).ToList();
        }
    }
}
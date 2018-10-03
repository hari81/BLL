using BLL.Core.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Extensions;
using System.Threading.Tasks;
using System.Data.Entity;
using BLL.Core.Widgets;
using System.Security.Principal;
using DAL;

namespace BLL.Core.Domain
{
    public class Dashboard
    {
        private List<BLL.Core.ViewModel.SearchItem> _searchItems = (new BLL.Core.ViewModel.SearchItem[] {
                   new BLL.Core.ViewModel.SearchItem { Id = 8, SearchId = 1, SearchStr = "", Title = "Evaluation A" },
                   new BLL.Core.ViewModel.SearchItem { Id = 9, SearchId = 1, SearchStr = "", Title = "Evaluation B" },
                   new BLL.Core.ViewModel.SearchItem { Id = 10, SearchId = 1, SearchStr = "", Title = "Evaluation C" },
                   new BLL.Core.ViewModel.SearchItem { Id = 11, SearchId = 1, SearchStr = "", Title = "Evaluation X" },
                }).ToList();
        public IQueryable<DAL.EQUIPMENT> getEquipmentAdvancedSearch(SharedContext _context, List<SearchItem> SearchItems, IPrincipal user, bool hasInspection = false, int userId = 0)
        {
            var accessibleEquipments = userId == 0 ? new UserAccess(_context, user).getAccessibleEquipments() : new UserAccess(_context, userId).getAccessibleEquipments();
            var customerSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Customer && item.SearchId != 0).Select(m => m.SearchId);
            var customerSearchStrs = SearchItems.Where(item => item.Id == (int)SearchItemType.Customer && item.SearchId == 0 && item.SearchStr != null && item.SearchStr.Trim().Length > 0).Select(m => m.SearchStr.Trim());

            if (customerSearchIds.Count() > 0 && customerSearchStrs.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => customerSearchIds.Any(k => k == m.Jobsite.customer_auto) || customerSearchStrs.Any(k => m.Jobsite.Customer.cust_name.Contains(k)));
            else if (customerSearchIds.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => customerSearchIds.Any(k => k == m.Jobsite.customer_auto));
            else if (customerSearchStrs.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => customerSearchStrs.Any(k => m.Jobsite.Customer.cust_name.Contains(k)));

            var jobsiteSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Jobsite && item.SearchId != 0).Select(m => m.SearchId);
            var jobsiteSearchStrs = SearchItems.Where(item => item.Id == (int)SearchItemType.Jobsite && item.SearchId == 0 && item.SearchStr != null && item.SearchStr.Trim().Length > 0).Select(m => m.SearchStr.Trim());

            if (jobsiteSearchIds.Count() > 0 && jobsiteSearchStrs.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => jobsiteSearchIds.Any(k => k == m.crsf_auto) || jobsiteSearchStrs.Any(k => m.Jobsite.site_name.Contains(k)));
            else if (jobsiteSearchIds.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => jobsiteSearchIds.Any(k => k == m.crsf_auto));
            else if (jobsiteSearchStrs.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => jobsiteSearchStrs.Any(k => m.Jobsite.site_name.Contains(k)));

            var equipmentSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Equipment && item.SearchId != 0).Select(m => m.SearchId);
            var equipmentSearchStrs = SearchItems.Where(item => item.Id == (int)SearchItemType.Equipment && item.SearchId == 0 && item.SearchStr != null && item.SearchStr.Trim().Length > 0).Select(m => m.SearchStr.Trim());

            if (equipmentSearchIds.Count() > 0 && equipmentSearchStrs.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => equipmentSearchIds.Any(k => k == m.equipmentid_auto) || equipmentSearchStrs.Any(k => m.serialno.Contains(k)) || equipmentSearchStrs.Any(k => m.unitno.Contains(k)));
            else if (equipmentSearchIds.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => equipmentSearchIds.Any(k => k == m.equipmentid_auto));
            else if (equipmentSearchStrs.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => equipmentSearchStrs.Any(k => m.serialno.Contains(k)) || equipmentSearchStrs.Any(k => m.unitno.Contains(k)));



            //Family
            var familySearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Family && item.SearchId != 0).Select(m => m.SearchId);
            if (familySearchIds.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => familySearchIds.Any(k => k == m.LU_MMTA.type_auto));
            //Make
            var makeSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Make && item.SearchId != 0).Select(m => m.SearchId);
            if (makeSearchIds.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => makeSearchIds.Any(k => k == m.LU_MMTA.make_auto));

            //Model
            var modelSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Model && item.SearchId != 0).Select(m => m.SearchId);
            if (modelSearchIds.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => modelSearchIds.Any(k => k == m.LU_MMTA.model_auto));

            if (hasInspection)
                accessibleEquipments = accessibleEquipments.Where(m => m.TRACK_INSPECTION.Count(k => k.ActionTakenHistory.recordStatus == (int)RecordStatus.Available) > 0);

            var evaluationExcludedA = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationA && item.SearchId == 1).Count() == 0;
            if (evaluationExcludedA)
                accessibleEquipments = accessibleEquipments.Where(m =>
                m.TRACK_INSPECTION.OrderByDescending(order => order.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Max(k => k.worn_percentage) > (int)WornLimit.A);

            var evaluationExcludedB = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationB && item.SearchId == 1).Count() == 0;
            if (evaluationExcludedB)
                accessibleEquipments = accessibleEquipments.Where(m =>
                m.TRACK_INSPECTION.OrderByDescending(order => order.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Max(k => k.worn_percentage) <= (int)WornLimit.A ||
                m.TRACK_INSPECTION.OrderByDescending(order => order.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Max(k => k.worn_percentage) > (int)WornLimit.B);

            var evaluationExcludedC = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationC && item.SearchId == 1).Count() == 0;
            if (evaluationExcludedC)
                accessibleEquipments = accessibleEquipments.Where(m =>
                m.TRACK_INSPECTION.OrderByDescending(order => order.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Max(k => k.worn_percentage) <= (int)WornLimit.B ||
                m.TRACK_INSPECTION.OrderByDescending(order => order.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Max(k => k.worn_percentage) > (int)WornLimit.C);

            var evaluationExcludedX = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationX && item.SearchId == 1).Count() == 0;
            if (evaluationExcludedX)
                accessibleEquipments = accessibleEquipments.Where(m =>
                m.TRACK_INSPECTION.OrderByDescending(order => order.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Max(k => k.worn_percentage) <= (int)WornLimit.C)/*.ToList()*/;

            return accessibleEquipments;
        }

        public int SaveSearchItemsFleetViewForExport(IUndercarriageContext _context, int pageNo, int pageSize, List<SearchItem> searchItems, IPrincipal user, int _clientReqId, string sortName, bool ascendingOrder)
        {
            var items = searchItems.Select(m => new SEARCH_ITEM { ItemId = m.Id, SearchId = m.SearchId, SearchStr = m.SearchStr, Title = m.Title }).ToArray();
            var item = new DASHBOARD_SEARCH
            {
                ascendingOrder = ascendingOrder,
                memberName = "",
                PageNo = pageNo,
                PageSize = pageSize,
                sortName = sortName,
                ViewId = 1, //1 -> Component View
                SearchItems = items
            };
            _context.DASHBOARD_SEARCH.Add(item);
            try
            {
                _context.SaveChanges();
                return item.Id;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToDetailedString());
            }
        }

        public int SaveSearchItemsComponentViewForExport(IUndercarriageContext _context, int pageNo, int pageSize, List<SearchItem> searchItems, IPrincipal user, int _clientReqId, string memberName, string sortName, bool ascendingOrder)
        {
            var items = searchItems.Select(m => new SEARCH_ITEM { ItemId = m.Id, SearchId = m.SearchId, SearchStr = m.SearchStr, Title = m.Title }).ToArray();
            var item = new DASHBOARD_SEARCH
            {
                ascendingOrder = ascendingOrder,
                memberName = memberName,
                PageNo = pageNo,
                PageSize = pageSize,
                sortName = sortName,
                ViewId = 1, //1 -> Component View
                SearchItems = items
            };
            _context.DASHBOARD_SEARCH.Add(item);
            try
            {
                _context.SaveChanges();
                return item.Id;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToDetailedString());
            }
        }


        public bool UpdatePrintItem(IUndercarriageContext _context, int printId, string htmlText, string htmlElement)
        {
            var _item = _context.DASHBOARD_SEARCH.Find(printId);
            if (_item == null) return false;

            _item.Html = htmlText;
            _item.Element = htmlElement;
            _context.MarkAsModified(_item);
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

        public async Task<List<CostOfRepairsViewModel>> GetTotalCostOfRepairs(SharedContext _context, List<SearchItem> searchItems, IPrincipal user, int months, int userId = 0)
        {
            var result = new List<CostOfRepairsViewModel>();
            var _tempDate = DateTime.Now;
            var equipments = await getEquipmentAdvancedSearch(_context, searchItems, user, false, userId).Where(m => m.equip_status != 0).Select(m => m.equipmentid_auto).ToListAsync();
            var allActions = await _context.ACTION_TAKEN_HISTORY.Where(m => m.recordStatus == (int)RecordStatus.Available).Select(m => new CostOfRepairsViewModel { Id = m.equipmentid_auto, Cost = m.cost, date = m.event_date }).ToListAsync();
            allActions = allActions.Where(m => equipments.Any(k => m.Id == k)).ToList();
            DateTime nextMonth = DateTime.Today;
            DateTime thisMonth = DateTime.Today.AddMonths(-1);
            for (int k = 1; k <= months; k++)
            {
                result.Add(new CostOfRepairsViewModel { date = thisMonth, Cost = allActions.Where(m => m.date >= thisMonth && m.date < nextMonth).Select(m => m.Cost).DefaultIfEmpty(0).Sum(), Month = thisMonth.ToString("MMM yy") });
                nextMonth = nextMonth.AddMonths(-1);
                thisMonth = thisMonth.AddMonths(-1);
            }
            result.Reverse();
            return result;
        }

        /// <summary>
        /// Returns due dates for the inspection from this week starting from Monday till next week Monday and for the number of weeks moving backward
        /// </summary>
        /// <param name="searchItems"></param>
        /// <param name="user"></param>
        /// <param name="weeks"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<InspectionDueViewModel>> GetInspectionsDueByToday(SharedContext _context, List<SearchItem> searchItems, System.Security.Principal.IPrincipal user, int weeks, int userId = 0)
        {
            var result = new List<InspectionDueViewModel>();
            DateTime defaulDate = new DateTime(1900, 1, 1);
            var dueDates = getEquipmentAdvancedSearch(_context, searchItems, user, false, userId).Where(m => m.equip_status != 0 && m.NextInspectionDate > defaulDate).Select(m => m.NextInspectionDate);
            DateTime tomorrow = DateTime.Today.AddDays(1);
            int daysUntilMonday = ((int)DayOfWeek.Monday - (int)tomorrow.DayOfWeek + 7) % 7;
            DateTime nextMonday = tomorrow.AddDays(daysUntilMonday);
            DateTime thisMonday = nextMonday.AddDays(-7);
            for (int k = 1; k < weeks; k++)
            {
                result.Add(new InspectionDueViewModel { Date = thisMonday, NumberOfInspections = await dueDates.Where(duedate => duedate >= thisMonday && duedate < nextMonday).CountAsync(), WeekNumber = k });
                nextMonday = nextMonday.AddDays(-7);
                thisMonday = thisMonday.AddDays(-7);
            }
            result.Reverse();
            return result;
        }
        public async Task<ComponentSearchViewModel> getDashboardEquipmentView(IUndercarriageContext _context, int printId, int PageNo, int PageSize, IPrincipal user, int userId = 0)
        {
            var _printSearch = _context.DASHBOARD_SEARCH.Find(printId);
            string _sortName = "lastInspectionDateAsDate";
            bool _asc = true;
            if (_printSearch != null)
            {
                _sortName = _printSearch.sortName;
                _asc = _printSearch.ascendingOrder;
                _searchItems = _printSearch.SearchItems.Select(m => new SearchItem { Id = m.ItemId, SearchId = m.SearchId, SearchStr = m.SearchStr, Title = m.Title }).ToList();
            }
            return await getDashboardEquipmentView(new SharedContext(), PageNo, PageSize, _searchItems, user, _clientReqId: 0, sortName: _sortName, asc: _asc, userId: userId);
        }
        public async Task<ComponentSearchViewModel> getDashboardEquipmentView(SharedContext _context, int PageNo, int PageSize, List<SearchItem> SearchItems, IPrincipal user, int _clientReqId, string sortName = "lastInspectionDateAsDate", bool asc = true, int userId = 0)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var equipments = getEquipmentAdvancedSearch(_context, SearchItems, user, false, userId).Where(m => m.equip_status != 0);
            var res = new ComponentSearchViewModel { _clientReqId = _clientReqId };
            res.SearchResult = new SearchResult { Total = await equipments.CountAsync(), Result = new List<IdAndDate>() };//await equipments.Select(m => new IdAndDate { Id = (int)m.equipmentid_auto, Date = m.TRACK_INSPECTION.OrderByDescending(k => k.inspection_date).FirstOrDefault() != null ? m.TRACK_INSPECTION.OrderByDescending(k => k.inspection_date).FirstOrDefault().inspection_date : DateTime.MinValue }).ToListAsync() };
            res.ResultList = await equipments.Select(m =>
            new ucDashbordViewModel
            {
                Id = (int)m.equipmentid_auto,
                customerId = (int)m.Jobsite.customer_auto,
                customerName = m.Jobsite.Customer.cust_name,
                serial = m.serialno,
                jobsiteId = (int)m.crsf_auto,
                jobsiteName = m.Jobsite.site_name,
                family = m.LU_MMTA.TYPE.typedesc,
                familyId = m.LU_MMTA.type_auto,
                make = m.LU_MMTA.MAKE.makedesc,
                makeId = m.LU_MMTA.make_auto,
                model = m.LU_MMTA.MODEL.modeldesc,
                modelId = m.LU_MMTA.model_auto,
                unit = m.unitno,
                NextInspectionDate = m.NextInspectionDate,
                NextInspectionSMU = m.NextInspectionSMU,
                lastInspectionId = m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).Count() > 0 ? m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().inspection_auto : 0,
                lastInspectionDateAsDate = m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).Count() > 0 ? m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().inspection_date : DateTime.MinValue,
                smu = (int)(m.currentsmu ?? 0),
                ltd = m.Life.Where(k => k.Action.recordStatus == 0).OrderByDescending(k => k.ActionDate).Count() > 0 ? m.Life.Where(k => k.Action.recordStatus == 0).OrderByDescending(k => k.ActionDate).FirstOrDefault().ActualLife : (int)(m.LTD_at_start ?? 0),
                EvalL = m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).Count() > 0 ? m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Where(k => k.SIDE.Side == 1).Max(k => k.eval_code) : "U",
                EvalR = m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).Count() > 0 ? m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Where(k => k.SIDE.Side == 2).Max(k => k.eval_code) : "U",
                overAllEvalNumber = m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0 && k.TRACK_INSPECTION_DETAIL.Count() > 0).Count() > 0 ? m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Max(k => k.worn_percentage) : 0,
                quoteId = (m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).Count() > 0 ? m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().quote_auto : 0) ?? 0,
            }
            ).OrderByField(sortName, asc).Skip(PageNo * PageSize).Take(PageSize).ToListAsync();
            return res;
        }

        public async Task<ComponentViewResult> getDashboardComponentView(IUndercarriageContext _context, int printId, int PageNo, int PageSize, IPrincipal user, int userId = 0)
        {
            var _printSearch = _context.DASHBOARD_SEARCH.Find(printId);
            string _sortName = "Id";
            string _member = "Equipment";
            bool _asc = true;
            if (_printSearch != null)
            {
                _sortName = _printSearch.sortName;
                _member = _printSearch.memberName;
                _asc = _printSearch.ascendingOrder;
                _searchItems = _printSearch.SearchItems.Select(m => new SearchItem { Id = m.ItemId, SearchId = m.SearchId, SearchStr = m.SearchStr, Title = m.Title }).ToList();
            }
            return await getDashboardComponentView(new SharedContext(), PageNo, PageSize, _searchItems, user, _clientReqId: 0, member: _member, sortName: _sortName, asc: _asc, userId: userId);
        }

        public async Task<ComponentViewResult> getDashboardComponentView(SharedContext _context, int PageNo, int PageSize, List<SearchItem> SearchItems, IPrincipal user, int _clientReqId, string member = "Equipment", string sortName = "Id", bool asc = true, int userId = 0, bool hasInspection = true)
        {
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            var equipments = getEquipmentAdvancedSearch(_context, SearchItems, user, hasInspection, userId).Where(m => m.equip_status != 0);
            //var equipments = preEquipments.OrderByDescending(m => m.last_reading_date).Skip(PageNo * PageSize).Take(PageSize);
            var res = new ComponentViewResult { _clientReqId = _clientReqId };
            res.SearchResult = new SearchResult { Total = await equipments.CountAsync(), Result = new List<IdAndDate>() }; //await equipments.Select(m => new IdAndDate { Id = (int)m.equipmentid_auto, Date = m.TRACK_INSPECTION.OrderByDescending(k => k.inspection_date).FirstOrDefault() != null ? m.TRACK_INSPECTION.OrderByDescending(k => k.inspection_date).FirstOrDefault().inspection_date : DateTime.MinValue }).ToListAsync() };
            res.ResultList = await equipments.Select(m => new ComponentTableInspectionViewModel
            {
                Id = (int)m.equipmentid_auto,
                Equipment = new EquipmentViewModel
                {
                    Id = (int)m.equipmentid_auto,
                    Serial = m.serialno,
                    Unit = m.unitno,
                    NextInspectionDate = m.NextInspectionDate,
                    NextInspectionSMU = m.NextInspectionSMU,
                    ModelDesc = m.LU_MMTA.MODEL.modeldesc,
                    SiteName = m.Jobsite.site_name,
                    JobSite = new JobSiteForSelectionVwMdl { Id = (int)m.crsf_auto, CustomerId = (int)m.Jobsite.customer_auto, Title = m.Jobsite.site_name },
                    Customer = new CustomerForSelectionVwMdl { Id = (int)m.Jobsite.customer_auto, Title = m.Jobsite.Customer.cust_name },
                    MakeModelFamily = new MakeModelFamily { Id = (int)m.equipmentid_auto, Family = new FamilyForSelectionVwMdl { Id = m.LU_MMTA.type_auto, Title = m.LU_MMTA.TYPE.typedesc, Symbol = m.LU_MMTA.TYPE.typeid, ExistingCount = 0 }, Make = new MakeForSelectionVwMdl { Id = m.LU_MMTA.make_auto, Title = m.LU_MMTA.MAKE.makedesc, Symbol = m.LU_MMTA.MAKE.makeid, ExistingCount = 0 }, Model = new ModelForSelectionVwMdl { Id = m.LU_MMTA.model_auto, Title = m.LU_MMTA.MODEL.modeldesc, MakeId = m.LU_MMTA.make_auto, FamilyId = m.LU_MMTA.type_auto, ExistingCount = 0 } },
                    Life = m.Life.Where(k => k.Action.recordStatus == 0).OrderByDescending(k => k.ActionDate).Count() > 0 ? m.Life.Where(k => k.Action.recordStatus == 0).OrderByDescending(k => k.ActionDate).FirstOrDefault().ActualLife : (int)(m.LTD_at_start ?? 0),
                    SMU = (int)(m.currentsmu ?? 0),
                },
                LastInspection = new InspectionViewModel
                {
                    Id = m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).Count() > 0 ? m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().inspection_auto : 0,
                    Date = m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).Count() > 0 ? m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().inspection_date : DateTime.MinValue,
                    QuoteId = (m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().quote_auto ?? 0),
                    SMU = (m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().smu ?? 0),
                    EquipmentId = (int)m.equipmentid_auto,
                },
                Components = m.TRACK_INSPECTION.Where(k => k.ActionTakenHistory.recordStatus == 0).OrderByDescending(k => k.inspection_date).FirstOrDefault().TRACK_INSPECTION_DETAIL.Select(k =>
                new ComponentViewViewModel
                {
                    Id = k.inspection_detail_auto,
                    Compart = new CompartV
                    { Id = k.GENERAL_EQ_UNIT.compartid_auto, CompartNote = k.GENERAL_EQ_UNIT.compart_note, CompartStr = k.GENERAL_EQ_UNIT.compartsn, CompartTitle = k.GENERAL_EQ_UNIT.compart_descr, CompartType = new CompartTypeV { Id = k.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto, Order = k.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.sorder ?? 10, Title = k.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.comparttype }, DefaultBudgetLife = (int)(k.GENERAL_EQ_UNIT.LU_COMPART.expected_life ?? 0), MeasurementPointsNo = k.GENERAL_EQ_UNIT.LU_COMPART.PARENT_RELATION_LIST.Count(), Model = new ModelForSelectionVwMdl { Id = k.TRACK_INSPECTION.EQUIPMENT.LU_MMTA.model_auto, Title = k.TRACK_INSPECTION.EQUIPMENT.LU_MMTA.MODEL.modeldesc, MakeId = k.TRACK_INSPECTION.EQUIPMENT.LU_MMTA.make_auto, FamilyId = k.TRACK_INSPECTION.EQUIPMENT.LU_MMTA.type_auto, ExistingCount = 0 } },
                    Worn = k.worn_percentage,
                    Date = k.TRACK_INSPECTION.inspection_date,
                    Life = k.hours_on_surface ?? 0,
                    Side = (Side)(k.GENERAL_EQ_UNIT.side ?? 0),
                    EquipmentId = (int)(k.TRACK_INSPECTION.equipmentid_auto),
                    Position = (k.GENERAL_EQ_UNIT.pos ?? 1) == 0 ? 1 : (k.GENERAL_EQ_UNIT.pos ?? 1),
                }).AsQueryable()
            }).OrderByField(member, sortName, asc).Skip(PageNo * PageSize).Take(PageSize).ToListAsync();
            return res;
        }
        public async Task<List<RecommendedActionsViewModel>> getEquipmentRecommendedActions(int[] EqIds)
        {
            var result = new List<RecommendedActionsViewModel>();
            using (var _context = new DAL.UndercarriageContext())
            {
                var eqs = await _context.TRACK_QUOTE_DETAIL.Where(m => EqIds.Any(k => m.Quote.Inspection.equipmentid_auto == k)).GroupBy(m => m.Quote.Inspection.equipmentid_auto).Select(m => new RecommendedActionsViewModel { EquipmentId = (int)m.Key, RecommendedActions = m.Select(k => new ComponentActionViewModel { Id = k.quote_detail_auto, ActionType = k.op_type_auto, ComponentId = (int)k.ComponentId, Date = k.Quote.due_date ?? DateTime.MinValue, EquipmentId = (int)k.Quote.Inspection.equipmentid_auto, Title = k.Comment }), CompletedActions = m.Select(k => new ComponentActionViewModel { Id = 0, ActionType = k.op_type_auto, ComponentId = (int)k.ComponentId, Date = k.Quote.Inspection.inspection_date, EquipmentId = (int)k.Quote.Inspection.equipmentid_auto, Title = k.Comment }) }).ToListAsync();
                var _actions = await _context.ACTION_TAKEN_HISTORY.Where(m => EqIds.Any(k => m.equipmentid_auto == k && m.recordStatus == (int)RecordStatus.Available)).Select(m => new ComponentActionViewModel { Id = (int)m.history_id, ActionType = m.action_type_auto, ComponentId = (int)(m.equnit_auto ?? 0), Date = m.event_date, EquipmentId = (int)m.equipmentid_auto, Title = m.comment }).ToListAsync();
                foreach (var eq in eqs)
                {
                    result.Add(new RecommendedActionsViewModel
                    {
                        EquipmentId = eq.EquipmentId,
                        RecommendedActions = eq.RecommendedActions.ToList(),
                        CompletedActions = eq.RecommendedActions.Join(_actions, p => p.ComponentId, q => q.ComponentId, (recs, acts) => new { recs, acts }).Where(m => m.recs.Date < m.acts.Date).Select(m => m.acts)
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// Return all compart types by having number of eval in the most recent inspection
        /// </summary>
        /// <param name="searchItems"></param>
        /// <param name="user"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<List<ComponentsFleetConditionEvalsViewModel>> GetSearchedEquipmentFleetConditionEvals(SharedContext _context, List<SearchItem> searchItems, IPrincipal user, int userId = 0)
        {
            var raw = await getEquipmentAdvancedSearch(_context, searchItems, user, true, userId: userId).Select(m => m.TRACK_INSPECTION.OrderByDescending(k => k.inspection_date).FirstOrDefault()).SelectMany(m => m.TRACK_INSPECTION_DETAIL).GroupBy(m => m.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto).Select(m => new ComponentsFleetConditionEvalsViewModel { Id = m.Key, CompartName = m.FirstOrDefault().GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.comparttype, EvalA = m.Count(k => k.worn_percentage <= 30), EvalB = m.Count(k => k.worn_percentage > 30 && k.worn_percentage <= 50), EvalC = m.Count(k => k.worn_percentage > 50 && k.worn_percentage <= 75), EvalX = m.Count(k => k.worn_percentage > 75), EvalU = 0 }).ToListAsync();
            //foreach (var res in raw)
            //    res.EvalsAndCount = (new EvalOverViewViewModel[] { new EvalOverViewViewModel { Eval = "A", Count = res.EvalA }, new EvalOverViewViewModel { Eval = "B", Count = res.EvalB }, new EvalOverViewViewModel { Eval = "C", Count = res.EvalC }, new EvalOverViewViewModel { Eval = "X", Count = res.EvalX } }).ToList();
            return raw;
        }

        public async Task<List<InspectionsPerformedViewModel>> GetInspectionsPerformed(SharedContext _context, List<SearchItem> searchItems, IPrincipal user, int userId = 0)
        {
            var inspections = await getEquipmentAdvancedSearch(_context, searchItems, user, true, userId: userId).SelectMany(m => m.TRACK_INSPECTION).Where(m => m.ActionTakenHistory.recordStatus == (int)RecordStatus.Available).Select(m => new InspectionsPerformedViewModel { Date = m.inspection_date, EquipmentId = m.equipmentid_auto }).ToListAsync();
            List<InspectionsPerformedViewModel> response = new List<InspectionsPerformedViewModel>();
            var currentYear = new InspectionsPerformedViewModel();
            currentYear.Year = DateTime.Now.Year;
            var previousYear = new InspectionsPerformedViewModel();
            previousYear.Year = DateTime.Now.Year - 1;
            for (int i = 0; i < 12; i++)
            {
                currentYear.InspectionCountMonth[i] += inspections.Where(ins => ins.Date.Year == currentYear.Year).Where(ins => ins.Date.Month == (i + 1)).Count();
                previousYear.InspectionCountMonth[i] += inspections.Where(ins => ins.Date.Year == previousYear.Year).Where(ins => ins.Date.Month == (i + 1)).Count();
            }
            response.Add(currentYear);
            response.Add(previousYear);
            return response;
        }

        public async Task<FleetConditionViewModel> GetActionsRequired(SharedContext _context, List<SearchItem> searchItems, IPrincipal user, int userId = 0)
        {
            var inspections = await getEquipmentAdvancedSearch(_context, searchItems, user, true, userId: userId).SelectMany(m => m.TRACK_INSPECTION).Where(m => m.ActionTakenHistory.recordStatus == (int)RecordStatus.Available).Select(m => m.evalcode).ToListAsync();
            return new FleetConditionViewModel
            {
                A = inspections.Count(m => m == "A"),
                B = inspections.Count(m => m == "B"),
                C = inspections.Count(m => m == "C"),
                X = inspections.Count(m => m == "X"),
                Unknown = 0
            };
        }
    }
}
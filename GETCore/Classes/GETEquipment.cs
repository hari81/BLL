using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.GETCore.Classes.ViewModel;
using BLL.Core.Domain;
using BLL.Extensions;
using BLL.Core.ViewModel;

namespace BLL.GETCore.Classes
{
    public class GETEquipment
    {
        private EventManagement eventManagement;

        public GETEquipment()
        {
            eventManagement = new EventManagement();
        }

        public bool equipmentSetupEvent(long equipmentId, int equipment_smu, 
            int equipment_ltd, long user_auto)
        {
            //bool result = false;
            long lUserAuto = user_auto;
            int iMeterReading = equipment_smu;
            int iEquipmentLTD = equipment_ltd;

            var eventDate = DateTime.Now;
            long eqmtIDAuto = 0;
            using (var dataContext = new DAL.GETContext())
            {
                var eqmt = dataContext.EQUIPMENTs
                    .Where(e => e.equipmentid_auto == equipmentId)
                    .FirstOrDefault();
                if(eqmt != null)
                {
                    eqmtIDAuto = eqmt.equipmentid_auto;
                    eventDate = eventDate = eqmt.purchase_date != null ? eqmt.purchase_date.Value : eventDate;
                }
            }

            if(eqmtIDAuto == 0)
            {
                return false;
            }
                
            var ActionParam = new BLL.Core.Domain.EquipmentActionRecord
            {
                ActionUser = new User { Id = (int) lUserAuto },
                EquipmentId = (int)eqmtIDAuto,
                ReadSmuNumber = iMeterReading,
                EquipmentActualLife = 0,
                TypeOfAction = ActionType.EquipmentSetup,
                Cost = 0.0M,
                Comment = "Equipment Setup",
                ActionDate = eventDate
            };

            var EquipmentSetupParams = new BLL.Core.Domain.GETEquipmentSetupParams
            {
                UserAuto = lUserAuto,
                ActionType = BLL.Core.Domain.GETActionType.Equipment_Setup,
                RecordedDate = DateTime.Now,
                EventDate = eventDate,
                Comment = "Equipment Setup",
                Cost = 0.0M,
                MeterReading = iMeterReading,
                EquipmentLTD = iEquipmentLTD,
                EquipmentId = equipmentId
            };

            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = " ",
                LastMessage = " ",
                Id = 0
            };

            using (var EquipmentSetupAction = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), new DAL.GETContext(), ActionParam, EquipmentSetupParams))
            {
                EquipmentSetupAction.Operation.Start();

                if (EquipmentSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Close)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = EquipmentSetupAction.Operation.ActionLog;
                    rm.LastMessage = EquipmentSetupAction.Operation.Message;
                }

                if (EquipmentSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Started)
                {
                    EquipmentSetupAction.Operation.Validate();
                }

                if (EquipmentSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Invalid)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = EquipmentSetupAction.Operation.ActionLog;
                    rm.LastMessage = EquipmentSetupAction.Operation.Message;
                }

                if (EquipmentSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Valid)
                {
                    EquipmentSetupAction.Operation.Commit();
                }

                if (EquipmentSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Failed)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = EquipmentSetupAction.Operation.ActionLog;
                    rm.LastMessage = EquipmentSetupAction.Operation.Message;
                }

                if (EquipmentSetupAction.Operation.Status == BLL.Core.Domain.ActionStatus.Succeed)
                {
                    rm.OperationSucceed = true;
                    rm.ActionLog = EquipmentSetupAction.Operation.ActionLog;
                    rm.LastMessage = EquipmentSetupAction.Operation.Message;
                }

                rm.Id = EquipmentSetupAction.Operation.UniqueId;
            }

            return rm.OperationSucceed;
        }

        public long currentSMU(string equipmentid_auto)
        {
            long result = 0;
            long iEqmtIDAuto = long.TryParse(equipmentid_auto, out iEqmtIDAuto) ? iEqmtIDAuto : 0;

            using (var dataEntities = new DAL.SharedContext())
            {
                var equipmentSMU = dataEntities.EQUIPMENT.Find(iEqmtIDAuto).currentsmu;

                if(equipmentSMU != null)
                {
                    result = equipmentSMU.Value;
                }
            }

            return result;
        }

        public List<GETEquipmentListVM> getEquipmentListByCustomer(long customerAuto, System.Security.Principal.IPrincipal User)
        {
            List<GETEquipmentListVM> result = new List<GETEquipmentListVM>();

            if(customerAuto == 0)
            {
                return result;
            }

            using (var dataEntitiesShared = new DAL.SharedContext())
            {
                result = (from eqmt in dataEntitiesShared.EQUIPMENT
                          join crsf in dataEntitiesShared.CRSF
                             on eqmt.crsf_auto equals crsf.crsf_auto
                          join customer in dataEntitiesShared.CUSTOMER
                             on crsf.customer_auto equals customer.customer_auto
                          join lu_mmta in dataEntitiesShared.LU_MMTA
                             on eqmt.mmtaid_auto equals lu_mmta.mmtaid_auto
                          join model in dataEntitiesShared.MODEL
                             on lu_mmta.model_auto equals model.model_auto
                          join make in dataEntitiesShared.MAKE
                             on lu_mmta.make_auto equals make.make_auto
                          where customer.customer_auto == customerAuto
                          select new GETEquipmentListVM
                          {
                              equipmentid_auto = eqmt.equipmentid_auto,
                              serialno = eqmt.serialno,
                              unitno = eqmt.unitno,
                              modeldesc = model.modeldesc,
                              makedesc = make.makedesc,
                              customer_auto = customer.customer_auto,
                              cust_name = customer.cust_name
                          }).ToList();
            }
            var accessible = new BLL.Core.Domain.UserAccess(new SharedContext(), User).getAccessibleEquipments().Select(m=> m.equipmentid_auto);
            return result.Where(m=> accessible.Any(k=> k ==  m.equipmentid_auto)).ToList();
        }

        public List<GETEquipmentListVM> getEquipmentListByCustomer(long customerAuto, int UserId)
        {
            List<GETEquipmentListVM> result = new List<GETEquipmentListVM>();

            if (customerAuto == 0)
            {
                return result;
            }

            using (var dataEntitiesShared = new DAL.SharedContext())
            {
                result = (from eqmt in dataEntitiesShared.EQUIPMENT
                          join crsf in dataEntitiesShared.CRSF
                             on eqmt.crsf_auto equals crsf.crsf_auto
                          join customer in dataEntitiesShared.CUSTOMER
                             on crsf.customer_auto equals customer.customer_auto
                          join lu_mmta in dataEntitiesShared.LU_MMTA
                             on eqmt.mmtaid_auto equals lu_mmta.mmtaid_auto
                          join model in dataEntitiesShared.MODEL
                             on lu_mmta.model_auto equals model.model_auto
                          join make in dataEntitiesShared.MAKE
                             on lu_mmta.make_auto equals make.make_auto
                          where customer.customer_auto == customerAuto
                          select new GETEquipmentListVM
                          {
                              equipmentid_auto = eqmt.equipmentid_auto,
                              serialno = eqmt.serialno,
                              unitno = eqmt.unitno,
                              modeldesc = model.modeldesc,
                              makedesc = make.makedesc,
                              customer_auto = customer.customer_auto,
                              cust_name = customer.cust_name
                          }).ToList();
            }
            var accessible = new BLL.Core.Domain.UserAccess(new SharedContext(), UserId).getAccessibleEquipments().Select(m => m.equipmentid_auto);
            return result.Where(m => accessible.Any(k => k == m.equipmentid_auto)).ToList();
        }

        public List<int> getEquipmentIdsByCustomer(long customerAuto, System.Security.Principal.IPrincipal User)
        {
            var _access = new BLL.Core.Domain.UserAccess(new SharedContext(), User);
            var accessibleJobsiteIds = _access.getAccessibleJobsites().Where(m=> m.customer_auto == customerAuto).Select(m=> m.crsf_auto);
            return  _access.getAccessibleEquipments().Where(m=> accessibleJobsiteIds.Any(k=> m.crsf_auto == k)).Select(m=> m.equipmentid_auto).ToList().LongNullableToInt();
        }

        public List<Core.Domain.IdAndDate> getEquipmentIdAndDateByCustomer(long customerAuto, System.Security.Principal.IPrincipal User)
        {
            List<Core.Domain.IdAndDate> result = new List<Core.Domain.IdAndDate>();

            if (customerAuto == 0)
            {
                return result;
            }
            List<int> Ids = getEquipmentIdsByCustomer(customerAuto, User);
            
            using (var _context = new DAL.UndercarriageContext())
            {
                foreach(var EqId in Ids)
                {
                    var inspections = _context.ACTION_TAKEN_HISTORY.Where(m => m.equipmentid_auto == EqId && m.recordStatus == (int)BLL.Core.Domain.RecordStatus.Available && (m.action_type_auto == (int)Core.Domain.ActionType.InsertInspection || m.action_type_auto == (int)Core.Domain.ActionType.UpdateInspection));
                    if (inspections.Count() == 0)
                        result.Add(new Core.Domain.IdAndDate { Id = EqId, Date = DateTime.MinValue });
                    else
                        result.Add(new Core.Domain.IdAndDate { Id = EqId, Date = inspections.OrderByDescending(m=>m.event_date).Select(m=>m.event_date).FirstOrDefault() });
                }
            }
            return result;
        }

        public List<UserSelectedIds> getEquipmentIdAndDateByCondition(int PageNo, int PageSize, UserSelectedIds SelectedIds, int userId)
        {
            var customerIds = new CustomerManagement().getListOfActiveCustomersForLoggedInUser(userId).Select(m => m.customerId);
            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            using (var _context = new DAL.UndercarriageContext())
            {
                return _context.EQUIPMENTs.Join(_context.CRSF, equipment => equipment.crsf_auto, jobsite => jobsite.crsf_auto, (equipment, jobsite) => new UserSelectedIds { CustomerId = (int)jobsite.customer_auto, EquipmentId = (int)equipment.equipmentid_auto, FamilyId = equipment.LU_MMTA.type_auto, JobSiteId = (int)jobsite.crsf_auto, MakeId = equipment.LU_MMTA.make_auto, ModelId = equipment.LU_MMTA.model_auto, LastReadingDate = equipment.last_reading_date ?? DateTime.MinValue }).Where(joined => customerIds.Any(cId => joined.CustomerId == cId) &&  (SelectedIds.CustomerId != 0 ? joined.CustomerId == SelectedIds.CustomerId : true) && (SelectedIds.JobSiteId != 0 ? joined.JobSiteId == SelectedIds.JobSiteId : true) && (SelectedIds.EquipmentId != 0 ? joined.EquipmentId == SelectedIds.EquipmentId : true) && (SelectedIds.MakeId != 0 ? joined.MakeId == SelectedIds.MakeId : true) && (SelectedIds.FamilyId != 0 ? joined.FamilyId == SelectedIds.FamilyId : true) && (SelectedIds.ModelId != 0 ? joined.ModelId == SelectedIds.ModelId : true)).OrderByDescending(m => m.LastReadingDate).Skip(PageNo * PageSize).Take(PageSize).ToList();
            }
        }

        private class EquipmentSearchModel
        {
            public long EquipmentId { get; set; }
            public long CustomerId { get; set; }
            public string CustomerName { get; set; }
            public long JobsiteId { get; set; }
            public string SiteName { get; set; }
            public string serialno { get; set; }
            public string unitno { get; set; }
            public int FamilyId { get; set; }
            public string FamilyTitle { get; set; }
            public int MakeId { get; set; }
            public string MakeTitle { get; set; }
            public int ModelId { get; set; }
            public string ModelTitle { get; set; }
            public DateTime LastReadingDate { get; set; }
            public List<InspectionSearchModel> Inspections { get; set; }
        }
        private class InspectionSearchModel
        {
            public int InspectionId { get; set; }
            public DateTime InspectionDate { get; set; }
            public List<InspectoinDetailSearchModel> Details { get; set; }
        }
        private class InspectoinDetailSearchModel
        {
            public int DetailId { get; set; }
            public decimal worn { get; set; }
        }
        public SearchResult getEquipmentIdAndDateAdvancedSearch(int PageNo, int PageSize, List<SearchItem> SearchItems, int userId, bool hasInspection = false)
        {
            var _access = new UserAccess(new SharedContext(), userId);
            var accessibleEquipments = _access.getAccessibleEquipments().Select(m => new EquipmentSearchModel { EquipmentId = m.equipmentid_auto, CustomerId = m.Jobsite.customer_auto, CustomerName = m.Jobsite.Customer.cust_name, JobsiteId = m.crsf_auto, SiteName = m.Jobsite.site_name, FamilyId = m.LU_MMTA.type_auto, FamilyTitle = m.LU_MMTA.TYPE.typedesc, MakeId = m.LU_MMTA.make_auto, MakeTitle = m.LU_MMTA.MAKE.makedesc, ModelId = m.LU_MMTA.model_auto, ModelTitle = m.LU_MMTA.MODEL.modeldesc, serialno = m.serialno, unitno = m.unitno, Inspections = m.TRACK_INSPECTION.Select(k => new InspectionSearchModel { InspectionId = k.inspection_auto, InspectionDate = k.inspection_date, Details = k.TRACK_INSPECTION_DETAIL.Select(p => new InspectoinDetailSearchModel { DetailId = p.inspection_detail_auto, worn = p.worn_percentage }).ToList() }).ToList(), LastReadingDate = m.last_reading_date ?? DateTime.MinValue }).ToList();

            var customerSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Customer && item.SearchId != 0).Select(m => m.SearchId);
            var customerSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Customer && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
            if (customerSearchIds.Count() > 0 || customerSearchTexts.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => customerSearchIds.Any(k => k == m.CustomerId) || customerSearchTexts.Any(k => m.CustomerName.ToLower().Contains(k.ToLower()))).ToList();

            var jobsiteSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Jobsite && item.SearchId != 0).Select(m => m.SearchId);
            var jobsiteSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Jobsite && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
            if (jobsiteSearchIds.Count() > 0 || jobsiteSearchTexts.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => jobsiteSearchIds.Any(k => k == m.JobsiteId)).ToList();

            var limitedEquipments = accessibleEquipments.ToList();

            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            using (var _context = new UndercarriageContext())
            {
                //var equipments = limitedEquipments.EQUIPMENTs.Where(m => limitedEquipmentIds.Any(k => m.equipmentid_auto == k));
                if (hasInspection)
                    limitedEquipments = limitedEquipments.Where(m => m.Inspections.Count() > 0).ToList();
                var equipmentSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Equipment && item.SearchId != 0).Select(m => m.SearchId);
                var equipmentSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Equipment && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
                if (equipmentSearchIds.Count() > 0 || equipmentSearchTexts.Count() > 0)
                    limitedEquipments = limitedEquipments.Where(m => equipmentSearchIds.Any(k => k == m.EquipmentId) || equipmentSearchTexts.Any(k => m.serialno.ToLower().Contains(k.ToLower())) || equipmentSearchTexts.Any(k => m.unitno.ToLower().Contains(k.ToLower()))).ToList();

                //Family
                var familySearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Family && item.SearchId != 0).Select(m => m.SearchId);
                var familySearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Family && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
                if (familySearchIds.Count() > 0 || familySearchTexts.Count() > 0)
                    limitedEquipments = limitedEquipments.Where(m => familySearchIds.Any(k => k == m.FamilyId) || familySearchTexts.Any(k => m.FamilyTitle.ToLower().Contains(k.ToLower()))).ToList();
                //Make
                var makeSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Make && item.SearchId != 0).Select(m => m.SearchId);
                var makeSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Make && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
                if (makeSearchIds.Count() > 0 || makeSearchTexts.Count() > 0)
                    limitedEquipments = limitedEquipments.Where(m => makeSearchIds.Any(k => k == m.MakeId) || makeSearchTexts.Any(k => m.MakeTitle.ToLower().Contains(k.ToLower()))).ToList();

                //Model
                var modelSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Model && item.SearchId != 0).Select(m => m.SearchId);
                var modelSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Model && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
                if (modelSearchIds.Count() > 0 || modelSearchTexts.Count() > 0)
                    limitedEquipments = limitedEquipments.Where(m => modelSearchIds.Any(k => k == m.ModelId) || modelSearchTexts.Any(k => m.ModelTitle.ToLower().Contains(k.ToLower()))).ToList();

                var evaluationExcludedA = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationA && item.SearchId == 1).Count() == 0;
                if (evaluationExcludedA)
                    limitedEquipments = limitedEquipments.Where(m =>
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) > (int)WornLimit.A).ToList();

                var evaluationExcludedB = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationB && item.SearchId == 1).Count() == 0;
                if (evaluationExcludedB)
                    limitedEquipments = limitedEquipments.Where(m =>
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) <= (int)WornLimit.A ||
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) > (int)WornLimit.B).ToList();

                var evaluationExcludedC = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationC && item.SearchId == 1).Count() == 0;
                if (evaluationExcludedC)
                    limitedEquipments = limitedEquipments.Where(m =>
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) <= (int)WornLimit.B ||
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) > (int)WornLimit.C).ToList();

                var evaluationExcludedX = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationX && item.SearchId == 1).Count() == 0;
                if (evaluationExcludedX)
                    limitedEquipments = limitedEquipments.Where(m =>
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) <= (int)WornLimit.C).ToList();

                var IdAndDates = limitedEquipments.Select(m => new IdAndDate { Id = (int)m.EquipmentId, Date = (m.LastReadingDate) });

                return new SearchResult
                {
                    Total = IdAndDates.Count(),
                    Result = IdAndDates.OrderByDescending(m => m.Date).Skip(PageNo * PageSize).Take(PageSize).ToList().Select(m => new IdAndDate { Id = m.Id, Date = m.Date.ToLocalTime().Date }).ToList()
                };
            }
        }

        public SearchResult getEquipmentIdForInspectionsAdvancedSearch(int PageNo, int PageSize, List<SearchItem> SearchItems, int userId, bool hasInspection = false)
        {
            var _access = new UserAccess(new SharedContext(), userId);
            var accessibleEquipments = _access.getAccessibleEquipments().Select(m => new EquipmentSearchModel { EquipmentId = m.equipmentid_auto, CustomerId = m.Jobsite.customer_auto, CustomerName = m.Jobsite.Customer.cust_name, JobsiteId = m.crsf_auto, SiteName = m.Jobsite.site_name, FamilyId = m.LU_MMTA.type_auto, FamilyTitle = m.LU_MMTA.TYPE.typedesc, MakeId = m.LU_MMTA.make_auto, MakeTitle = m.LU_MMTA.MAKE.makedesc, ModelId = m.LU_MMTA.model_auto, ModelTitle = m.LU_MMTA.MODEL.modeldesc, serialno = m.serialno, unitno = m.unitno, Inspections = m.TRACK_INSPECTION.Select(k => new InspectionSearchModel { InspectionId = k.inspection_auto, InspectionDate = k.inspection_date, Details = k.TRACK_INSPECTION_DETAIL.Select(p => new InspectoinDetailSearchModel { DetailId = p.inspection_detail_auto, worn = p.worn_percentage }).ToList() }).ToList(), LastReadingDate = m.last_reading_date ?? DateTime.MinValue }).ToList();

            var customerSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Customer && item.SearchId != 0).Select(m => m.SearchId);
            var customerSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Customer && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
            if (customerSearchIds.Count() > 0 || customerSearchTexts.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => customerSearchIds.Any(k => k == m.CustomerId) || customerSearchTexts.Any(k => m.CustomerName.ToLower().Contains(k.ToLower()))).ToList();

            var jobsiteSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Jobsite && item.SearchId != 0).Select(m => m.SearchId);
            var jobsiteSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Jobsite && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
            if (jobsiteSearchIds.Count() > 0 || jobsiteSearchTexts.Count() > 0)
                accessibleEquipments = accessibleEquipments.Where(m => jobsiteSearchIds.Any(k => k == m.JobsiteId)).ToList();

            var limitedEquipments = accessibleEquipments.ToList();

            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            using (var _context = new UndercarriageContext())
            {
                //var equipments = limitedEquipments.EQUIPMENTs.Where(m => limitedEquipmentIds.Any(k => m.equipmentid_auto == k));
                if (hasInspection)
                    limitedEquipments = limitedEquipments.Where(m => m.Inspections.Count() > 0).ToList();
                var equipmentSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Equipment && item.SearchId != 0).Select(m => m.SearchId);
                var equipmentSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Equipment && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
                if (equipmentSearchIds.Count() > 0 || equipmentSearchTexts.Count() > 0)
                    limitedEquipments = limitedEquipments.Where(m => equipmentSearchIds.Any(k => k == m.EquipmentId) || equipmentSearchTexts.Any(k => m.serialno.ToLower().Contains(k.ToLower())) || equipmentSearchTexts.Any(k => m.unitno.ToLower().Contains(k.ToLower()))).ToList();

                //Family
                var familySearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Family && item.SearchId != 0).Select(m => m.SearchId);
                var familySearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Family && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
                if (familySearchIds.Count() > 0 || familySearchTexts.Count() > 0)
                    limitedEquipments = limitedEquipments.Where(m => familySearchIds.Any(k => k == m.FamilyId) || familySearchTexts.Any(k => m.FamilyTitle.ToLower().Contains(k.ToLower()))).ToList();
                //Make
                var makeSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Make && item.SearchId != 0).Select(m => m.SearchId);
                var makeSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Make && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
                if (makeSearchIds.Count() > 0 || makeSearchTexts.Count() > 0)
                    limitedEquipments = limitedEquipments.Where(m => makeSearchIds.Any(k => k == m.MakeId) || makeSearchTexts.Any(k => m.MakeTitle.ToLower().Contains(k.ToLower()))).ToList();

                //Model
                var modelSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Model && item.SearchId != 0).Select(m => m.SearchId);
                var modelSearchTexts = SearchItems.Where(item => item.Id == (int)SearchItemType.Model && item.SearchId == 0 && item.SearchStr.Length > 0).Select(m => m.SearchStr);
                if (modelSearchIds.Count() > 0 || modelSearchTexts.Count() > 0)
                    limitedEquipments = limitedEquipments.Where(m => modelSearchIds.Any(k => k == m.ModelId) || modelSearchTexts.Any(k => m.ModelTitle.ToLower().Contains(k.ToLower()))).ToList();

                var IdAndDates = limitedEquipments.Select(m => new IdAndDate { Id = (int)m.EquipmentId, Date = (m.LastReadingDate) });

                return new SearchResult
                {
                    Total = IdAndDates.Count(),
                    Result = IdAndDates.OrderByDescending(m => m.Date).Skip(PageNo * PageSize).Take(PageSize).ToList().Select(m => new IdAndDate { Id = m.Id, Date = m.Date.ToLocalTime().Date }).ToList()
                };
            }
        }

        public SearchResult getSystemIdAndDateAdvancedSearch(int PageNo, int PageSize, List<SearchItem> SearchItems, int userId, bool hasInspection = false)
        {
            var _access = new UserAccess(new SharedContext(), userId);
            var accessableSystems = _access.GetAccessibleSystems();

            var customerSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Customer && item.SearchId != 0).Select(m => m.SearchId).ToList();
            if (customerSearchIds.Count() > 0)
                accessableSystems = accessableSystems
                    .Where(m => 
                        (m.equipmentid_auto != null && customerSearchIds.Contains((int)m.EQUIPMENT.Jobsite.customer_auto))
                        || (m.crsf_auto != null && customerSearchIds.Contains((int)m.Jobsite.customer_auto))
                    );

            var jobsiteSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Jobsite && item.SearchId != 0).Select(m => m.SearchId).ToList();
            if (jobsiteSearchIds.Count() > 0)
                accessableSystems = accessableSystems.Where(m => jobsiteSearchIds.Contains((int)(m.crsf_auto ?? 0)));

            var limitedSystems = accessableSystems.ToList();

            PageNo = PageNo <= 1 ? 0 : PageNo - 1;
            using (var _context = new UndercarriageContext())
            {
                var equipmentSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Equipment && item.SearchId != 0).Select(m => m.SearchId).ToList();
                if (equipmentSearchIds.Count() > 0)
                    limitedSystems = limitedSystems.Where(m => equipmentSearchIds.Contains((int)(m.equipmentid_auto ?? 0))).ToList();

                //Family
                var familySearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Family && item.SearchId != 0).Select(m => m.SearchId).ToList();
                if (familySearchIds.Count() > 0)
                    limitedSystems = limitedSystems
                        .Where(m => m.make_auto != null)
                        .Where(m => familySearchIds.Contains(m.Make.LU_MMTA.FirstOrDefault().type_auto)).ToList();
                //Make
                var makeSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Make && item.SearchId != 0).Select(m => m.SearchId).ToList();
                if (makeSearchIds.Count() > 0)
                    limitedSystems = limitedSystems.Where(m => makeSearchIds.Contains(m.make_auto ?? 0)).ToList();

                //Model
                var modelSearchIds = SearchItems.Where(item => item.Id == (int)SearchItemType.Model && item.SearchId != 0).Select(m => m.SearchId).ToList();
                if (modelSearchIds.Count() > 0)
                    limitedSystems = limitedSystems.Where(m => modelSearchIds.Contains(m.model_auto ?? 0)).ToList();

                var evaluationExcludedA = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationA && item.SearchId == 1).Count() == 0;
                /*if (evaluationExcludedA) {
                    limitedSystems = limitedSystems.Where(m =>
                    m.TRACK_INSPECTION_DETAIL.OrderByDescending(order => order.TRACK_INSPECTION.inspection_date).FirstOrDefault().worn_percentage.Max(k => k.worn) > (int)WornLimit.A).ToList();

                var evaluationExcludedB = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationB && item.SearchId == 1).Count() == 0;
                if (evaluationExcludedB)
                    limitedEquipments = limitedEquipments.Where(m =>
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) <= (int)WornLimit.A ||
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) > (int)WornLimit.B).ToList();

                var evaluationExcludedC = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationC && item.SearchId == 1).Count() == 0;
                if (evaluationExcludedC)
                    limitedEquipments = limitedEquipments.Where(m =>
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) <= (int)WornLimit.B ||
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) > (int)WornLimit.C).ToList();

                var evaluationExcludedX = SearchItems.Where(item => item.Id == (int)SearchItemType.EvaluationX && item.SearchId == 1).Count() == 0;
                if (evaluationExcludedX)
                    limitedEquipments = limitedEquipments.Where(m =>
                    m.Inspections.OrderByDescending(order => order.InspectionDate).FirstOrDefault().Details.Max(k => k.worn) <= (int)WornLimit.C).ToList();
                    */
                var IdAndDates = limitedSystems.Select(m => new IdAndDate { Id = (int)m.Module_sub_auto, Date = DateTime.Now });

                return new SearchResult
                {
                    Total = IdAndDates.Count(),
                    Result = IdAndDates.OrderByDescending(m => m.Date).Skip(PageNo * PageSize).Take(PageSize).ToList().Select(m => new IdAndDate { Id = m.Id, Date = m.Date.ToLocalTime().Date }).ToList()
                };
            }
        }

        public List<GETEquipmentListVM> getEquipmentListByCustomerAndJobsite(long customerAuto, int jobsiteAuto, System.Security.Principal.IPrincipal User)
        {
            var Equipments = getEquipmentListByCustomer(customerAuto, User);
            using (var dataEntitiesShared = new DAL.SharedContext())
            {
                var jobsiteEquipments = dataEntitiesShared.EQUIPMENT.Where(m=> m.crsf_auto == jobsiteAuto).Select(m=> m.equipmentid_auto);
                return Equipments.Where(m => jobsiteEquipments.Any(k => k == m.equipmentid_auto)).ToList();
            }
        }

        public List<GETEquipmentListVM> getEquipmentListByCustomerAndJobsite(long customerAuto, int jobsiteAuto, int UserId)
        {
            var Equipments = getEquipmentListByCustomer(customerAuto, UserId);
            using (var dataEntitiesShared = new DAL.SharedContext())
            {
                var jobsiteEquipments = dataEntitiesShared.EQUIPMENT.Where(m => m.crsf_auto == jobsiteAuto).Select(m => m.equipmentid_auto);
                return Equipments.Where(m => jobsiteEquipments.Any(k => k == m.equipmentid_auto)).OrderBy(l => l.serialno).ToList();
            }
        }

        public List<GETEquipmentListVM> getEquipmentListByJobsite(long jobsiteAuto, System.Security.Principal.IPrincipal User)
        {
            List<GETEquipmentListVM> result = new List<GETEquipmentListVM>();

            if (jobsiteAuto == 0)
            {
                return result;
            }

            using (var dataEntitiesShared = new DAL.SharedContext())
            {
                result = (from eqmt in dataEntitiesShared.EQUIPMENT
                          join lu_mmta in dataEntitiesShared.LU_MMTA
                             on eqmt.mmtaid_auto equals lu_mmta.mmtaid_auto
                          join model in dataEntitiesShared.MODEL
                             on lu_mmta.model_auto equals model.model_auto
                          join make in dataEntitiesShared.MAKE
                             on lu_mmta.make_auto equals make.make_auto
                          where eqmt.crsf_auto == jobsiteAuto
                          select new GETEquipmentListVM
                          {
                              equipmentid_auto = eqmt.equipmentid_auto,
                              serialno = eqmt.serialno,
                              unitno = eqmt.unitno,
                              model_auto = model.model_auto,
                              modeldesc = model.modeldesc,
                              makedesc = make.makedesc
                          }).ToList();
            }
            var accessible = new BLL.Core.Domain.UserAccess(new SharedContext(), User).getAccessibleEquipments().Select(m => m.equipmentid_auto);
            return result.Where(m => accessible.Any(k => k == m.equipmentid_auto)).ToList();
        }

        public List<GETEquipmentListVM> getEquipmentListByJobsite(long jobsiteAuto, int User)
        {
            List<GETEquipmentListVM> result = new List<GETEquipmentListVM>();

            if (jobsiteAuto == 0)
            {
                return result;
            }

            using (var dataEntitiesShared = new DAL.SharedContext())
            {
                result = (from eqmt in dataEntitiesShared.EQUIPMENT
                          join lu_mmta in dataEntitiesShared.LU_MMTA
                             on eqmt.mmtaid_auto equals lu_mmta.mmtaid_auto
                          join model in dataEntitiesShared.MODEL
                             on lu_mmta.model_auto equals model.model_auto
                          join make in dataEntitiesShared.MAKE
                             on lu_mmta.make_auto equals make.make_auto
                          where eqmt.crsf_auto == jobsiteAuto
                          select new GETEquipmentListVM
                          {
                              equipmentid_auto = eqmt.equipmentid_auto,
                              serialno = eqmt.serialno,
                              unitno = eqmt.unitno,
                              model_auto = model.model_auto,
                              modeldesc = model.modeldesc,
                              makedesc = make.makedesc
                          }).ToList();
            }
            var accessible = new BLL.Core.Domain.UserAccess(new SharedContext(), User).getAccessibleEquipments().Select(m => m.equipmentid_auto);
            return result.Where(m => accessible.Any(k => k == m.equipmentid_auto)).ToList();
        }

        public List<EquipmentListVM> getCompatibleEquipmentForImplementByJobsite(long jobsiteId, int getAuto, System.Security.Principal.IPrincipal User)
        {
            List<EquipmentListVM> result = new List<EquipmentListVM>();

            using (var context = new DAL.GETContext())
            {
                // Get implement id
                long implementAuto = context.GET.Find(getAuto).implement_auto ?? 0;
                if(implementAuto == 0)
                {
                    return result;
                }

                // Find valid models for implement.
                var validModels = context.GET_IMPLEMENT_MAKE_MODEL.Where(w => w.implement_auto == implementAuto)
                    .Select(s => new
                    {
                        equipmentModel = s.model_auto
                    }).ToList();

                // Find all equipment at the specified jobsite for which the user has permissions to access.
                var allEquipmentAtJobsite = new BLL.Core.Domain.UserAccess(new SharedContext(), User)
                    .getAccessibleEquipments().Where(e => e.crsf_auto == jobsiteId)
                    .Select(s => new 
                    {
                        equipmentId = s.equipmentid_auto,
                        equipmentSerialNo = s.serialno,
                        equipmentSMU = s.currentsmu.Value,
                        equipmentModel = s.LU_MMTA.model_auto
                    }).ToList();

                // Filter results by valid models for the specified jobsite.
                for(int i=0; i<allEquipmentAtJobsite.Count; i++)
                {
                    for(int j=0; j<validModels.Count; j++)
                    {
                        if(allEquipmentAtJobsite[i].equipmentModel == validModels[j].equipmentModel)
                        {
                            result.Add(new EquipmentListVM
                            {
                                equipmentId = allEquipmentAtJobsite[i].equipmentId,
                                equipmentSerialNo = allEquipmentAtJobsite[i].equipmentSerialNo,
                                equipmentSMU = allEquipmentAtJobsite[i].equipmentSMU
                            });
                        }
                    }
                }
            }

            return result;
        }

        public List<EquipmentListVM> returnEquipmentByJobsite(long jobsiteId, System.Security.Principal.IPrincipal User)
        {
            return new BLL.Core.Domain.UserAccess(new SharedContext(), User).getAccessibleEquipments().Where(e => e.crsf_auto == jobsiteId).Select(s => new EquipmentListVM
            {
                equipmentId = s.equipmentid_auto,
                equipmentSerialNo = s.serialno,
                equipmentSMU = s.currentsmu.Value
            }).ToList();
        }

        public string returnPreviousEquipmentEventDate(long equipmentId, System.Security.Principal.IPrincipal User)
        {
            string result = "";
            if (!new BLL.Core.Domain.UserAccess(new SharedContext(), User).hasAccessToEquipment(equipmentId))
                return "Access Denied!";

            using (var dataEntities = new DAL.GETContext())
            {
                var previousEvent = eventManagement.findPreviousEquipmentEvent(dataEntities, equipmentId, DateTime.Now);
                if(previousEvent != null)
                {
                    var previousGETEvent = dataEntities.GET_EVENTS.Find(previousEvent.events_auto);
                    if(previousGETEvent != null)
                    {
                        result = previousGETEvent.event_date.Date.ToShortDateString();
                    }
                }
            }

            return result;
        }
    }
}
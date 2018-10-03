using BLL.Core.ViewModel;
using DAL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace BLL.Core.Domain
{
    public class FindInspection
    {
        private UndercarriageContext _context;
        private SharedContext _sharedContext;
        private USER_TABLE _user;

        public FindInspection(UndercarriageContext undercarriageContext, SharedContext sharedContext, long userId)
        {
            _context = undercarriageContext;
            _sharedContext = sharedContext;
            _user = _context.USER_TABLE.Find(userId);
        }

        public InspectionSearchResultsCount GetInspectionIds(int pageNumber, int inspectionsPerPage, List<SearchItem> searchItems)
        {
            var filteredEquipment = new GETCore.Classes.GETEquipment().getEquipmentIdForInspectionsAdvancedSearch(0, 99999999, searchItems, (int)_user.user_auto);
            List<long> equipmentIds = filteredEquipment.Result.Select(r => (long)r.Id).ToList();

            var excludeA = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationA && item.SearchId == 1).Count() == 0;
            var excludeB = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationB && item.SearchId == 1).Count() == 0;
            var excludeC = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationC && item.SearchId == 1).Count() == 0;
            var excludeX = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationX && item.SearchId == 1).Count() == 0;
            var startDateString = searchItems.Where(item => item.Id == (int)SearchItemType.StartDate).FirstOrDefault();
            var endDateString = searchItems.Where(item => item.Id == (int)SearchItemType.EndDate).FirstOrDefault();

            DateTime startDate;
            DateTime endDate;
            DateTime.TryParseExact(
                startDateString.SearchStr,
                @"yyyy-MM-dd\THH:mm:ss.fff\Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out startDate);
            DateTime.TryParseExact(
                endDateString.SearchStr,
                @"yyyy-MM-dd\THH:mm:ss.fff\Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out endDate);

            
            int totalResults = _context.TRACK_INSPECTION
                .Where(i => equipmentIds.Contains(i.equipmentid_auto))
                .Where(i => excludeA ? i.evalcode != "A" : true)
                .Where(i => excludeB ? i.evalcode != "B" : true)
                .Where(i => excludeC ? i.evalcode != "C" : true)
                .Where(i => excludeX ? i.evalcode != "X" : true)
                .Where(i => i.inspection_date >= startDate)
                .Where(i => i.inspection_date <= endDate)
                .Where(i=> i.ActionTakenHistory.recordStatus ==0)
                .Count();

            var inspectionIds = _context.TRACK_INSPECTION
                .Where(i => equipmentIds.Contains(i.equipmentid_auto))
                .Where(i => excludeA ? i.evalcode != "A" : true)
                .Where(i => excludeB ? i.evalcode != "B" : true)
                .Where(i => excludeC ? i.evalcode != "C" : true)
                .Where(i => excludeX ? i.evalcode != "X" : true)
                .Where(i => i.inspection_date >= startDate)
                .Where(i => i.inspection_date <= endDate)
                .Where(i => i.ActionTakenHistory.recordStatus == 0)
                .OrderByDescending(i => i.inspection_date)
                .Skip(inspectionsPerPage * pageNumber)
                .Take(inspectionsPerPage)
                .Select(i => i.inspection_auto)
                .ToList();

            return new InspectionSearchResultsCount()
            {
                CurrentPageInspectionIds = inspectionIds,
                TotalRecords = totalResults
            }; 
        }

        public InspectionSearchResultsCount GetRepairEstimateIds(int pageNumber, int inspectionsPerPage, List<SearchItem> searchItems)
        {
            var filteredSystems = new GETCore.Classes.GETEquipment().getSystemIdAndDateAdvancedSearch(0, 99999999, searchItems, (int)_user.user_auto);
            List<long> systemIds = filteredSystems.Result.Select(r => (long)r.Id).ToList();

            var excludeA = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationA && item.SearchId == 1).Count() == 0;
            var excludeB = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationB && item.SearchId == 1).Count() == 0;
            var excludeC = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationC && item.SearchId == 1).Count() == 0;
            var excludeX = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationX && item.SearchId == 1).Count() == 0;
            var startDateString = searchItems.Where(item => item.Id == (int)SearchItemType.StartDate).FirstOrDefault();
            var endDateString = searchItems.Where(item => item.Id == (int)SearchItemType.EndDate).FirstOrDefault();

            DateTime startDate;
            DateTime endDate;
            DateTime.TryParseExact(
                startDateString.SearchStr,
                @"yyyy-MM-dd\THH:mm:ss.fff\Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out startDate);
            DateTime.TryParseExact(
                endDateString.SearchStr,
                @"yyyy-MM-dd\THH:mm:ss.fff\Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out endDate);

            int totalResults = _context.WSRE
                .Where(i => systemIds.Contains(i.SystemId))
                .Where(i => excludeA ? i.OverallEval != "A" : true)
                .Where(i => excludeB ? i.OverallEval != "B" : true)
                .Where(i => excludeC ? i.OverallEval != "C" : true)
                .Where(i => excludeX ? i.OverallEval != "X" : true)
                .Where(i => i.Date >= startDate)
                .Where(i => i.Date <= endDate)
                .Count();

            var inspectionIds = _context.WSRE
                .Where(i => systemIds.Contains(i.SystemId))
                .Where(i => excludeA ? i.OverallEval != "A" : true)
                .Where(i => excludeB ? i.OverallEval != "B" : true)
                .Where(i => excludeC ? i.OverallEval != "C" : true)
                .Where(i => excludeX ? i.OverallEval != "X" : true)
                .Where(i => i.Date >= startDate)
                .Where(i => i.Date <= endDate)
                .OrderByDescending(i => i.Date)
                .Skip(inspectionsPerPage * pageNumber)
                .Take(inspectionsPerPage)
                .Select(i => i.Id)
                .ToList();

            return new InspectionSearchResultsCount()
            {
                CurrentPageInspectionIds = inspectionIds,
                TotalRecords = totalResults
            };
        }

        public InspectionSearchResultsCount GetUnsyncedInspectionIds(int pageNumber, int inspectionsPerPage, List<SearchItem> searchItems, long userId)
        {
            var excludeA = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationA && item.SearchId == 1).Count() == 0;
            var excludeB = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationB && item.SearchId == 1).Count() == 0;
            var excludeC = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationC && item.SearchId == 1).Count() == 0;
            var excludeX = searchItems.Where(item => item.Id == (int)SearchItemType.EvaluationX && item.SearchId == 1).Count() == 0;
            var startDateString = searchItems.Where(item => item.Id == (int)SearchItemType.StartDate).FirstOrDefault();
            var endDateString = searchItems.Where(item => item.Id == (int)SearchItemType.EndDate).FirstOrDefault();

            DateTime startDate;
            DateTime endDate;
            DateTime.TryParseExact(
                startDateString.SearchStr,
                @"yyyy-MM-dd\THH:mm:ss.fff\Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out startDate);
            DateTime.TryParseExact(
                endDateString.SearchStr,
                @"yyyy-MM-dd\THH:mm:ss.fff\Z",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out endDate);



            var currentUser = _context.USER_TABLE.FirstOrDefault(u => u.user_auto == userId);//unreliable string comparison
          
            int totalResults = _context.Mbl_Track_Inspection
                .Where(i => excludeA ? i.evalcode != "A" : true)
                .Where(i => excludeB ? i.evalcode != "B" : true)
                .Where(i => excludeC ? i.evalcode != "C" : true)
                .Where(i => excludeX ? i.evalcode != "X" : true)
                .Where(i => i.inspection_date >= startDate)
                .Where(i => i.inspection_date <= endDate)
                .Where(i => i.examiner == currentUser.username)//added for only inspector can view 
                .Count();

            var inspectionIds = _context.Mbl_Track_Inspection
                .Where(i => excludeA ? i.evalcode != "A" : true)
                .Where(i => excludeB ? i.evalcode != "B" : true)
                .Where(i => excludeC ? i.evalcode != "C" : true)
                .Where(i => excludeX ? i.evalcode != "X" : true)
                .Where(i => i.inspection_date >= startDate)
                .Where(i => i.inspection_date <= endDate)
                .Where(i => i.examiner == currentUser.username)//added for only inspector can view 
                .OrderByDescending(i => i.inspection_date)
                .Skip(inspectionsPerPage * pageNumber)
                .Take(inspectionsPerPage)
                .Select(i => i.inspection_auto)
                .ToList();


         

            
            return new InspectionSearchResultsCount()
            {
                CurrentPageInspectionIds = inspectionIds,
                TotalRecords = totalResults
            };
        }

        public InspectionRowViewModel GetInspectionRow(int inspectionId)
        {
            var insp = _context.TRACK_INSPECTION.Find(inspectionId);
            if (insp == null)
                return new InspectionRowViewModel()
                {
                    Evaluation = "?"
                };
            return new InspectionRowViewModel()
            {
                CustomerName = insp.EQUIPMENT.Jobsite.Customer.cust_name,
                Evaluation = insp.evalcode,
                Family = insp.EQUIPMENT.LU_MMTA.TYPE.typedesc,
                InspectionDate = insp.inspection_date,
                JobsiteName = insp.EQUIPMENT.Jobsite.site_name,
                Make = insp.EQUIPMENT.LU_MMTA.MAKE.makedesc,
                Model = insp.EQUIPMENT.LU_MMTA.MODEL.modeldesc,
                SerialNumber = insp.EQUIPMENT.serialno,
                UnitNumber = insp.EQUIPMENT.unitno,
                Status = GetInspectionStatus(insp)
            };
        }

        public InspectionRowViewModel GetRepairEstimateRow(int inspectionId)
        {
            var insp = _context.WSRE.Find(inspectionId);
            if (insp == null)
                return new InspectionRowViewModel()
                {
                    Evaluation = "?"
                };
            return new InspectionRowViewModel()
            {
                CustomerName = insp.Jobsite.Customer.cust_name,
                Evaluation = insp.OverallEval,
                Family = insp.System.Make.LU_MMTA.FirstOrDefault().TYPE.typedesc,
                InspectionDate = insp.Date,
                JobsiteName = insp.Jobsite.site_name,
                Make = insp.System.Make.makedesc,
                Model = insp.System.Model.modeldesc,
                SerialNumber = insp.System.Serialno,
                Status = "New"
            };
        }

        public InspectionRowViewModel GetUnsyncedInspectionRow(int inspectionId)
        {
            var insp = _context.Mbl_Track_Inspection.Where(u => u.inspection_auto == inspectionId).FirstOrDefault();
            if (insp == null)
                return new InspectionRowViewModel()
                {
                    Evaluation = "?"
                };
            var equip = _context.Mbl_NewEquipment.Where(e => e.equipmentid_auto == insp.equipmentid_auto).FirstOrDefault();


            return new InspectionRowViewModel()
            {
                CustomerName = equip.customer_name,
                Evaluation = insp.evalcode == null ? "?" : insp.evalcode,
                Family = "",
                InspectionDate = insp.inspection_date,
                JobsiteName = equip.jobsite_name,
                Make = "",
                Model = equip.model,
                SerialNumber = equip.serialno,
                UnitNumber = equip.unitno,
                Status = GetUnsyncedInspectionStatus(equip)
            };
        }

        private string GetUnsyncedInspectionStatus(Mbl_NewEquipment e)
        {
            if(e.pc_equipmentid_auto == null)
            {
                return "Awaiting Equipment Match";
            } else
            {
                return "Ready to Sync";
            }
        }

        private string GetInspectionStatus(TRACK_INSPECTION i)
        {
            if (i.last_interp_date == null)
                return "Awaiting Interpretation";
            else if (i.released_date != null)
                return "Released to Customer";
            else
                return "Interpreted";
        }





      
    }
}
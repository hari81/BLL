using BLL.Core.Domain;
using BLL.Core.ViewModel;
using BLL.Extensions;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.Widgets
{
    public class WidgetManager
    {
        private UndercarriageContext _context;

        /// <summary>
        /// This is used to retrieve data displayed in the widgets on the dashboard
        /// </summary>
        /// <param name="context">New instance of DAL.UndercarriageContext</param>
        public WidgetManager(UndercarriageContext context)
        {
            this._context = context;
        }
        /// <summary>
        /// This method is obsolete please use Dashboard.cs/GetTotalCostOfRepairs
        /// </summary>
        /// <param name="searchItems"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<CostOfRepairsViewModel> GetTotalCostOfRepairs(List<ViewModel.SearchItem> searchItems, long userId)
        {
            SearchResult eq = new SearchResult();
            if (searchItems != null)
                eq = new GETCore.Classes.GETEquipment().getEquipmentIdAndDateAdvancedSearch(1, 999999, searchItems, Convert.ToInt32(userId));
            CostOfRepairsViewModel[] repairs = new CostOfRepairsViewModel[12];

            var initialDate = DateTime.Now.AddYears(-1).AddMonths(1);
            for(int i = 0; i < 12; i++)
            {
                repairs[i] = new CostOfRepairsViewModel()
                {
                    Cost = 0,
                    Month = initialDate.AddMonths(i).ToString("MMM yy")
                };
            }

            eq.Result.ForEach(id =>
            {
                var equip = new BLL.Core.Domain.Equipment(new UndercarriageContext(), id.Id);
                var costs = equip.GetEquipmentRepairsCostForGivenYear(initialDate);
                for (int i = 0; i < 12; i++)
                {
                    repairs[i].Cost += costs.Where(c => c.Date.Month == initialDate.AddMonths(i).Month && c.Date.Year == initialDate.AddMonths(i).Year).Select(c => c.Cost).Sum();  //equip.GetEquipmentRepairsCostForGivenMonth(initialDate.AddMonths(i));
                }
            });
            return repairs.ToList();
        }



        public FleetConditionViewModel GetFleetCondition(List<ViewModel.SearchItem> searchItems, long userId)
        {
            SearchResult eq = new SearchResult();
            if(searchItems != null)
                eq = new GETCore.Classes.GETEquipment().getEquipmentIdAndDateAdvancedSearch(1, 999999, searchItems, Convert.ToInt32(userId));

            FleetConditionViewModel response = new FleetConditionViewModel();
            eq.Result.ForEach(r =>
            {
                var equipment = _context.EQUIPMENTs.Find(r.Id);
                var eval = equipment.TRACK_INSPECTION.OrderByDescending(i => i.inspection_date).Select(i => i.evalcode).FirstOrDefault();
                switch (eval)
                {
                    case "A":
                        response.A++;
                        break;
                    case "B":
                        response.B++;
                        break;
                    case "C":
                        response.C++;
                        break;
                    case "X":
                        response.X++;
                        break;
                    default:
                        response.Unknown++;
                        break;
                }
            });
            return response;
        }



        public async void ExtractAllEquipmentsComponentsEval( int userId)
        {
            var _access = new UserAccess(new SharedContext(), userId);
            var accessibleEquipments = _access.getAccessibleEquipments();
            var allEquipmentsComponents = accessibleEquipments.SelectMany(a=>a.Components).DistinctBy(dis=>dis.compartid_auto);
            var grouped = allEquipmentsComponents.GroupBy(a => (CompartTypeEnum)a.LU_COMPART.comparttype_auto);

            foreach (var group in grouped)
            {
                var compartType = group.Key;

                foreach (var item in group)
                {
                  
                }

            }

       
        }

    }

    public class FleetConditionViewModel
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int X { get; set; }
        public int Unknown { get; set; }
    }

    public class InspectionsPerformedViewModel
    {
        public long EquipmentId { get; set; }
        public int Year { get; set; } = DateTime.Now.Year;
        public DateTime Date { get; set; }
        public int[] InspectionCountMonth { get; set; } = new int[12];
    }

    public class CostOfRepairsViewModel
    {
        public long Id { get; set; }
        public DateTime date { get; set; }
        public string Month { get; set; } = "";
        public decimal Cost { get; set; } = 0;
    }

    public class InspectionDueViewModel
    {
        public DateTime Date { get; set; }
        public int WeekNumber { get; set; }
        public int NumberOfInspections { get; set; }
    }

    public class EquipmentCost
    {
        public decimal Cost { get; set; }
        public DateTime Date { get; set; }
    }




    public class ComponentsFleetConditionEvalsViewModel
    {
        public int Id { get; set; }
        public int EvalU { get; set; }
        public int EvalA { get; set; }
        public int EvalB { get; set; }
        public int EvalC { get; set; }
        public int EvalX { get; set; }
        public string CompartName { get; set; }
        public List<EvalOverViewViewModel> EvalsAndCount { get; set; }

    }


    public class EvalOverViewViewModel
    {
        public int Count { get; set; }
        public string Eval { get; set; }
    }
}

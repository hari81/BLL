using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;

namespace BLL.Core.ViewModel
{
    public class ComponentViewViewModel
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public CompartV Compart { get; set; }
        public Side Side { get; set; }
        public int Life { get; set; }
        public DateTime Date { get; set; }
        public decimal Worn { get; set; }
        public int Position { get; set; }
        public List<ComponentActionViewModel> Actions { get; set; } = new List<ComponentActionViewModel>();
    }
    public class ComponentTableInspectionViewModel
    {
        public int Id { get; set; }
        public EquipmentViewModel Equipment { get; set; }
        public InspectionViewModel LastInspection { get; set; }
        public IQueryable<ComponentViewViewModel> Components { get; set; }
    }

    public class ComponentViewResult
    {
        public List<ComponentTableInspectionViewModel> ResultList { get; set; }
        public SearchResult SearchResult { get; set; }
        public int _clientReqId { get; set; }
    }

    public class RecommendedActionsViewModel
    {
        public int EquipmentId { get; set; }
        public IEnumerable<ComponentActionViewModel> RecommendedActions { get; set; }
        public IEnumerable<ComponentActionViewModel> CompletedActions { get; set; }
    }

    public class ComponentSearchViewModel
    {
        public List<ucDashbordViewModel> ResultList { get; set; }
        public SearchResult SearchResult { get; set; }
        public int _clientReqId { get; set; }
    }

    public class CompartSearchViewModel
    {
        public int CompartId { get; set; }
        public string CompartName { get; set; }
        public string CompartType { get; set; }
        public string Models { get; set; }
        public string Make { get; set; }
        public int MakeId { get; set; }
    }


    public class MeasurementToolsViewModel
    {
        public int ToolId { get; set; }
        public string ToolName { get; set; }
        public string ToolCode { get; set; }

    }


    public class CompartMeasurementPonitsViewModel
    {
        public CompartSearchViewModel Compart { get; set; }
        public List<MeasurementPonitsViewModel> MeasurementPointsViewModels { get; set; }
    }

    public class MeasurementPonitsViewModel
    {
        public int MeasurementPonitId { get; set; }
        public int CompartId { get; set; }
        public string Name { get; set; }
        public int Order { get; set; }
        public int DefaultNumberOfMeasurements { get; set; }
        public int? DefaultToolId { get; set; }
        public bool isDisabled { get; set; }
        public int? BugetLife { get; set; }
        //public SteppedCalMethodViewModel SteppedMethod { get; set; }
        //public InflectionCalMethodViewModel InflectionMethod { get; set; }
        public List<CalculationMethod> CalculationMethods { get; set; }
    }





    public class CreateMeasurementPointModel : MeasurementPonitsViewModel
    {
        public string Image { get; set; }
    }


    public class SearchItemsParam
    {
        public List<SearchItem> SearchItems { get; set; } = new List<SearchItem>();
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public int _clientReqId { get; set; }
        public string memberName { get; set; }
        public string sortName { get; set; }
        public bool ascendingOrder { get; set; }
    }

    public class PrintItem
    {
        public int PrintId { get; set; }
        public string HtmlText { get; set; }
        public string HtmlElement { get; set; }
    }

    public class CalculationMethod
    {
        public int? WornCalculationMethodTableAutoId { get; set; }
        public int CompartId { get; set; }
        public int ToolId { get; set; }
        public int? BugetLife { get; set; }
        public int? MeasurementPointId { get; set; }
        public int WornCalculationMethodTypeId { get; set; }
        public int MakeId { get; set; }
        public int? CompartEXTId { get; set; }
    }


    public class SteppedCalMethodViewModel : CalculationMethod
    {
        public decimal? StartDepthNew { get; set; }
        public decimal? StartDepth_10 { get; set; }
        public decimal? StartDepth_20 { get; set; }
        public decimal? StartDepth_30 { get; set; }
        public decimal? StartDepth_40 { get; set; }
        public decimal? StartDepth_50 { get; set; }
        public decimal? StartDepth_60 { get; set; }
        public decimal? StartDepth_70 { get; set; }
        public decimal? StartDepth_80 { get; set; }
        public decimal? StartDepth_90 { get; set; }
        public decimal? StartDepth_100 { get; set; }
        public decimal? StartDepth_110 { get; set; }
        public decimal? StartDepth_120 { get; set; }
    }


    public class InflectionCalMethodViewModel : CalculationMethod
    {
        public decimal? Hi_InflectionPoint { get; set; }
        public decimal? Hi_Slope1 { get; set; }
        public decimal? Hi_Intercept1 { get; set; }
        public decimal? Hi_Slope2 { get; set; }
        public decimal? Hi_Intercept2 { get; set; }
        public decimal? Mi_InflectionPoint { get; set; }
        public decimal? Mi_Slope1 { get; set; }
        public decimal? Mi_Intercept1 { get; set; }
        public decimal? Mi_Slope2 { get; set; }
        public decimal? Mi_Intercept2 { get; set; }
        public decimal? Adjust_base { get; set; }
        public int? Slope { get; set; }

    }

    public class PolynomialCalMethodViewModel : CalculationMethod
    {
        public decimal? Slope_Impact { get; set; }
        public decimal? Slope_Normal { get; set; }
        public decimal? Impact_Slope { get; set; }
        public decimal? Normal_Slope { get; set; }
        public decimal? Impact_Intercept { get; set; }
        public decimal? Normal_Intercept { get; set; }
        public decimal? Impact_Secondorder { get; set; }
        public decimal? Normal_Secondorder { get; set; }

    }


    public class LinearCalMethodViewModel : CalculationMethod
    {
        public decimal? Impact_Slope { get; set; }
        public decimal? Normal_Slope { get; set; }
        public decimal? Impact_Intercept { get; set; }
        public decimal? Normal_Intercept { get; set; }
    }


    public class JDCalMehtodViewModel : CalculationMethod
    {
        public decimal? Impact_Slope { get; set; }
        public decimal? Normal_Slope { get; set; }
        public decimal? Impact_Intercept { get; set; }
        public decimal? Normal_Intercept { get; set; }
    }


    public class UpdateAllCalculationMethodsViewModel
    {
        public List<SteppedCalMethodViewModel> SteppedMethods { get; set; }
        public List<InflectionCalMethodViewModel> InflectionMethods { get; set; }
        public List<PolynomialCalMethodViewModel> PolyMethods { get; set; }
        public List<LinearCalMethodViewModel> LinearMethods { get; set; }
        public List<JDCalMehtodViewModel> JDMethods { get; set; }

    }


}
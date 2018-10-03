using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.WSRE.Models
{

    /// <summary>
    /// Initial data displayed at the top of the WSRE inspection results page. 
    /// </summary>
    public class WsreOverviewModel
    {
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string SerialNumber { get; set; }
        public DateTime InspectionDate { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int LifeLived { get; set; }
        public string JobNumber { get; set; }
        public string CustomerReference { get; set; }
        public string OldJobNumber { get; set; }
        public bool ReportPrepared { get; set; }
    }

    public class WsrePhoto
    {
        public string ImageData { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
    }

    public class WsreArrivalPhotoOverview
    {
        public int Id { get; set; }
        public string ImageData { get; set; }
        public WsreInitialImageType Type { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
    }

    public class WsreComponentTab
    {
        public int Id { get; set; }
        public decimal WornPercentage { get; set; }
        public string Component { get; set; }
        public string Brand { get; set; }
        public int Cmu { get; set; }
        public int RemainingLife { get; set; }
        public decimal Measurement { get; set; }
        public List<string> Recommendations { get; set; }
        public string Comment { get; set; }
        public List<WsreComponentPhoto> Photos { get; set; }
    }

    public class WsreComponentPhoto
    {
        public int Id { get; set; }
        public string ImageData { get; set; }
        public string Title { get; set; }
    }

    public class WsreCrackTestTab
    {
        public bool TestPassed { get; set; }
        public string Comment { get; set; }
        public List<WsreComponentPhoto> Photos { get; set; }
    }

    public class WsreDipTest
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public decimal Level { get; set; }
        public string Condition { get; set; }
        public string Colour { get; set; }
        public string Comment { get; set; }
        public string Recommendation { get; set; }
        public List<WsreComponentPhoto> Photos { get; set; }
    }

    public enum WsreDipTestCondition
    {
        Good = 1,
        Dry,
        Contaminated,
        Pressurised
    }

    public enum WsreInitialImageType
    {
        OldTag = 1,
        CustomerReference,
        Arrival
    }

    public class WsreSummaryModel
    {
        public string OverallEval { get; set; }
        public string OverallComment { get; set; }
        public string OverallRecommendation { get; set; }
        public List<ComponentSummaryModel> Components { get; set; }
        public CrackTestSummaryModel CrackTest { get; set; }
        public List<DipTestSummaryModel> DipTests { get; set; }
    }

    public class ComponentSummaryModel
    {
        public string Type { get; set; }
        public List<string> Recommendations { get; set; }
        public string Comment { get; set; }
        public decimal WornPercentage { get; set; }
    }

    public class CrackTestSummaryModel
    {
        public bool Passed { get; set; }
        public string Comment { get; set; }
    }

    public class DipTestConditionType
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    public class DipTestSummaryModel
    {
        public int Number { get; set; }
        public string Problem { get; set; }
        public string Colour { get; set; }
        public string Comment { get; set; }
    }

    public class WsreComponentTabForReport
    {
        public int Id { get; set; }
        public decimal WornPercentage { get; set; }
        public string Component { get; set; }
        public string Brand { get; set; }
        public int Cmu { get; set; }
        public int RemainingLife { get; set; }
        public string Tool { get; set; }
        public string ComponentImage { get; set; }
        public decimal Measurement { get; set; }
        public List<string> Recommendations { get; set; }
        public string Comment { get; set; }
        public List<WsrePhoto> Photos { get; set; }
    }

    public class WsreCrackTestTabForReport
    {
        public bool TestPassed { get; set; }
        public string Comment { get; set; }
        public List<WsrePhoto> Photos { get; set; }
    }

    public class WsreDipTestForReport
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public decimal Level { get; set; }
        public string Condition { get; set; }
        public string Colour { get; set; }
        public string Comment { get; set; }
        public string Recommendation { get; set; }
        public List<WsrePhoto> Photos { get; set; }
    }
}
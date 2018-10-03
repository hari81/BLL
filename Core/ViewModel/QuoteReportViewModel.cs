using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ViewModel
{

    public class QuoteReportOverview
    {
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string SerialNumber { get; set; }
        public string UnitNumber { get; set; }
        public DateTime InspectionDate { get; set; }
        public string Inspector { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int LifeLived { get; set; }
        public int MeterReading { get; set; }
        public string Eval { get; set; }
        public string QuoteNumber { get; set; }
        public string Summary { get; set; }
    }

    public class QuoteReportRecommendation
    {
        public string ComponentType { get; set; }
        public string Component { get; set; }
        public string Side { get; set; }
        public string RecommendationOperationType { get; set; }
        public string RecommendationComments { get; set; }
        public int RecommendationStart { get; set; }
        public int RecommendationDuration { get; set; }
        public decimal Price { get; set; }
        public decimal PartsCost { get; set; }
        public decimal LabourCost { get; set; }
        public decimal MiscCost { get; set; }
    }

    public class QuoteReportDealership
    {
        public string Name { get; set; }
        public string PhoneNo { get; set; }
        public string Email { get; set; }
        public string Logo { get; set; }
    }
}
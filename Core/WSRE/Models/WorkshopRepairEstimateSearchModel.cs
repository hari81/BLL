using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.WSRE.Models
{
    public class WorkshopRepairEstimateSearchResultModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string SerialNumber { get; set; }
        public string JobNumber { get; set; }
        public string CustomerReference { get; set; }
        public DateTime InspectionDate { get; set; }
        public string InspectorName { get; set; }
        public string Status { get; set; }
    }

    public class WorkshopRepairEstimateSearchRequestModel
    {
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string SerialNumber { get; set; }
        public string JobNumber { get; set; }
        public string CustomerReference { get; set; }
        public string InspectorName { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
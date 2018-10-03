using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ViewModel
{
    public class InspectionRowViewModel
    {
        public string Evaluation { get; set; }
        public DateTime InspectionDate { get; set; }
        public string SerialNumber { get; set; }
        public string UnitNumber { get; set; }
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Family { get; set; }
        public string Status { get; set; }
    }

    public class InspectionSearchResultsCount
    {
        public int TotalRecords { get; set; }
        public List<int> CurrentPageInspectionIds { get; set; }
    }

    public class InspectionSearchRequest
    {
        public int PageNumber { get; set; }
        public int InspectionsPerPage { get; set; }
        public List<SearchItem> SearchItems { get; set; }
    }
}
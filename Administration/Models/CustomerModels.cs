using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BLL.Administration.Models
{
    public class CustomerOverviewModel
    {
        public long CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public int JobsiteCount { get; set; }
        public string DealershipName { get; set; }
    }

    public class NewCustomerModel
    {
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Logo { get; set; }
        public int ReportStyleId { get; set; }
        public int DealershipId { get; set; }
        public long CreatedByUserId { get; set; }
        public int DealerGroupId { get; set; }
        public int QuoteReportStyleId { get; set; }
        public Decimal HourlyLabourCost { get; set; }
    }

    public class UpdateCustomerModel: NewCustomerModel
    {
        public long CustomerId { get; set; }
    }
}
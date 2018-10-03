using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace BLL.Administration.Models
{
    public class JobsiteOverviewModel
    {
        public long JobsiteId { get; set; }
        public string JobsiteName { get; set; }
        public string CustomerName { get; set; }
        public string Country { get; set; }
    }

    public class NewJobsiteModel
    {
        [Required]
        public long CustomerId { get; set; }
        [Required]
        public string JobsiteName { get; set; }
        [Required]
        public string FullAddress { get; set; }
        [Required]
        public string City { get; set; }

        public string PostCode { get; set; }

        public string State { get; set; }
        [Required]
        public string Country { get; set; }
        public long CreatedByUserId { get; set; }
        public int JobsiteTypeId { get; set; }
    }

    public class UpdateJobsiteModel: NewJobsiteModel
    {
        public long JobsiteId { get; set; }
    }
}
using BLL.Extensions;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Classes
{
    public class JobsiteManagement
    {
        public List<BasicJobsiteDataSet> getListOfJobsitesForCustomer(long customerId, System.Security.Principal.IPrincipal User)
        {
            using(var context = new SharedContext())
            {
                var accessibleJobsites = new BLL.Core.Domain.UserAccess(context, User).getAccessibleJobsites().Select(m => m.crsf_auto);

                return context.CRSF.OrderBy(j => j.site_name).Where(j => j.customer_auto == customerId && accessibleJobsites.Any(k=> j.crsf_auto == k))
                    .Select(j => new BasicJobsiteDataSet
                    {
                        jobsiteId = j.crsf_auto,
                        jobsiteName = j.site_name,
                        equipmentCount = context.EQUIPMENT.Where(e => e.crsf_auto == j.crsf_auto).Count(),
                        implementCount = context.GET.Where(i => i.EQUIPMENT.crsf_auto == j.crsf_auto).Count()

                    }).ToList();
            }
        }

        public List<BasicJobsiteDataSet> getListOfJobsitesForCustomer(long customerId, int User)
        {
            var accessibleJobsites = new BLL.Core.Domain.UserAccess(new SharedContext(), User).getAccessibleJobsites().Select(m => m.crsf_auto).ToList();
            using (var context = new SharedContext())
            {
                return context.CRSF.OrderBy(j => j.site_name).Where(j => j.customer_auto == customerId && accessibleJobsites.Any(k => j.crsf_auto == k))
                    .Select(j => new BasicJobsiteDataSet
                    {
                        jobsiteId = j.crsf_auto,
                        jobsiteName = j.site_name,
                        equipmentCount = context.EQUIPMENT.Where(e => e.crsf_auto == j.crsf_auto).Count(),
                        implementCount = context.GET.Where(i => i.EQUIPMENT.crsf_auto == j.crsf_auto).Count()

                    }).ToList();
            }
        }

        public List<DAL.CRSF> getListOfJobsitesForCustomer(List<int> CustomerIds, System.Security.Principal.IPrincipal User)
        {
            return new BLL.Core.Domain.UserAccess(new SharedContext(), User).getAccessibleJobsites().Where(m => CustomerIds.Any(k => k == m.customer_auto)).Distinct().ToList();
        }
        public bool doesCustomerExist(long customerId)
        {
            using (var context = new SharedContext())
            {
                var customerExists = context.CUSTOMER.Find(customerId);
                if (customerExists == null)
                    return false;
            }
            return true;
        }

        public JobsiteDetailsDataSet getJobsiteDetails(long jobsiteId, System.Security.Principal.IPrincipal User)
        {
            if (!new BLL.Core.Domain.UserAccess(new SharedContext(), User).hasAccessToJobsite(jobsiteId.LongNullableToInt()))
                return null;
            using (var context = new SharedContext())
            {
                return context.CRSF.Where(j => j.crsf_auto == jobsiteId).Select(
                    j => new JobsiteDetailsDataSet
                    {
                        jobsiteName = j.site_name,
                        fullAddress = j.FullAddress,
                        city = j.site_suburb,
                        country = j.site_country,
                        postCode = j.site_postcode,
                        state = j.site_state
                    }).First();
            }
        }

        public GETResponseMessage createNewJobsite(CreateNewJobsiteDataSet jobsiteData)
        {
            if (!doesCustomerExist(jobsiteData.customerId))
                return new GETResponseMessage(ResponseTypes.Failed, "Customer ID not found. ");

            if (jobsiteData.jobsiteName == "" || jobsiteData.fullAddress == "")
                return new GETResponseMessage(ResponseTypes.InvalidInputs, "Missing required data. ");

            CRSF newJobsite = new CRSF()
            {
                site_name = jobsiteData.jobsiteName,
                customer_auto = jobsiteData.customerId,
                site_street = jobsiteData.streetNumber + " " + jobsiteData.streetAddress,
                site_suburb = jobsiteData.city,
                site_postcode = jobsiteData.postCode,
                site_state = jobsiteData.state,
                site_country = jobsiteData.country,
                created_date = DateTime.UtcNow,
                CreatedByUserId = jobsiteData.authUserId,
                FullAddress = jobsiteData.fullAddress,
                DealerId = 4 // TrackTreads
            };

            using (var context = new SharedContext())
            {
                context.CRSF.Add(newJobsite);

                try
                {
                    context.SaveChanges();
                    return new GETResponseMessage(ResponseTypes.Success, newJobsite.crsf_auto.ToString());
                }
                catch (Exception e)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, e.Message);
                }
            }
        }

        public GETResponseMessage updateJobsite(UpdateJobsiteDataSet jobsiteData)
        {
            if (jobsiteData.jobsiteName == "" || jobsiteData.fullAddress == "" || jobsiteData.city == "" || 
                jobsiteData.state == "" || jobsiteData.postCode == "" || jobsiteData.country == "")
                return new GETResponseMessage(ResponseTypes.InvalidInputs, "Missing required data. ");

            using (var context = new SharedContext())
            {
                var jobsite = context.CRSF.Find(jobsiteData.jobsiteId);
                jobsite.site_name = jobsiteData.jobsiteName;
                jobsite.site_street = jobsiteData.streetNumber + " " + jobsiteData.streetAddress;
                jobsite.site_suburb = jobsiteData.city;
                jobsite.site_postcode = jobsiteData.postCode;
                jobsite.site_state = jobsiteData.state;
                jobsite.site_country = jobsiteData.country;
                jobsite.modified_date = DateTime.UtcNow;
                jobsite.modified_user = context.USER_TABLE.Find(jobsiteData.authUserId).username;
                jobsite.FullAddress = jobsiteData.fullAddress;

                try
                {
                    context.SaveChanges();
                    return new GETResponseMessage(ResponseTypes.Success, "Jobsite updated successfully. ");
                }
                catch (Exception e)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, e.Message);
                }
            }
        }
    }

    public class BasicJobsiteDataSet
    {
        public long jobsiteId { get; set; }
        public string jobsiteName { get; set; }
        public int equipmentCount { get; set; }
        public int implementCount { get; set; }
    }

    public class JobsiteDetailsDataSet
    {
        public string jobsiteName { get; set; }
        public string fullAddress { get; set; }
        public string city { get; set; }
        public string postCode { get; set; }
        public string state { get; set; }
        public string country { get; set; }
    }

    public class CreateNewJobsiteDataSet
    {
        public string jobsiteName { get; set; }
        public string fullAddress { get; set; }
        public int? subPremise { get; set; }
        public int streetNumber { get; set; }
        public string streetAddress { get; set; }
        public string city { get; set; }
        public string postCode { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string emailAddress { get; set; }
        public int customerId { get; set; }
        public long authUserId { get; set; }
    }

    public class UpdateJobsiteDataSet
    {
        public string jobsiteName { get; set; }
        public string fullAddress { get; set; }
        public int? subPremise { get; set; }
        public int streetNumber { get; set; }
        public string streetAddress { get; set; }
        public string city { get; set; }
        public string postCode { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string emailAddress { get; set; }
        public int jobsiteId { get; set; }
        public long authUserId { get; set; }
    }
}
using BLL.Administration.Models;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.ModelBinding;

namespace BLL.Administration
{
    public class JobsiteManager
    {
        private SharedContext _context;
        private CustomerManager _customerManager;

        public JobsiteManager()
        {
            this._context = new SharedContext();
            this._customerManager = new CustomerManager();
        }

        public async Task<NewJobsiteModel> GetJobsiteDetails(long jobsiteId)
        {
            var jobsite = await _context.CRSF.Where(j => j.crsf_auto == jobsiteId).FirstOrDefaultAsync();
            if (jobsite == null)
                return null;

            var response = new NewJobsiteModel()
            {
                JobsiteName = jobsite.site_name,
                CustomerId = jobsite.customer_auto,
                City = jobsite.site_suburb,
                PostCode = jobsite.site_postcode,
                State = jobsite.site_state,
                Country = jobsite.site_country,
                CreatedByUserId = jobsite.CreatedByUserId == null ? 0 : (long)jobsite.CreatedByUserId,
                FullAddress = jobsite.FullAddress,
                JobsiteTypeId = jobsite.type_auto ?? 0
            };
            return response;
        }

        public List<JobsiteOverviewModel> GetAllJobsitesForUser(long userId)
        {
            var customerList = _customerManager.GetAllCustomersForUser(userId);
            List<JobsiteOverviewModel> jobsiteList = new List<JobsiteOverviewModel>();

            foreach(var customer in customerList)
            {
                jobsiteList.AddRange(_context.CRSF.Where(c => c.customer_auto == customer.CustomerId).Select(c => new JobsiteOverviewModel()
                {
                    JobsiteName = c.site_name,
                    Country = c.site_country,
                    CustomerName = customer.CustomerName,
                    JobsiteId = c.crsf_auto
                }));
            }

            return jobsiteList.OrderBy(j => j.JobsiteName).ToList();
        }

        public async Task<Tuple<long, string>> UpdateJobsite(UpdateJobsiteModel jobsite)
        {
            var jobsiteEntity = await _context.CRSF.Where(j => j.crsf_auto == jobsite.JobsiteId).FirstOrDefaultAsync();
            jobsiteEntity.site_name = jobsite.JobsiteName;
            jobsiteEntity.customer_auto = jobsite.CustomerId;
            jobsiteEntity.site_suburb = jobsite.City;
            jobsiteEntity.site_postcode = jobsite.PostCode;
            jobsiteEntity.site_state = jobsite.State;
            jobsiteEntity.site_country = jobsite.Country;
            jobsiteEntity.FullAddress = jobsite.FullAddress;
            jobsiteEntity.type_auto = jobsite.JobsiteTypeId;
            _context.Entry(jobsiteEntity).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(jobsiteEntity.crsf_auto, "Jobsite updated successfully. ");
            } catch
            {
                return Tuple.Create(Convert.ToInt64(-1), "Failed to update jobsite"); 
            }
        }

        public Tuple<long, string> AddNewJobsite(NewJobsiteModel jobsite)
        {

            CRSF newJobsite = new CRSF()
            {
                site_name = jobsite.JobsiteName,
                customer_auto = jobsite.CustomerId,
                site_suburb = jobsite.City,
                site_postcode = jobsite.PostCode,
                site_state = jobsite.State,
                site_country = jobsite.Country,
                created_date = DateTime.UtcNow,
                CreatedByUserId = jobsite.CreatedByUserId,
                FullAddress = jobsite.FullAddress,
                DealerId = 4, // TrackTreads
                type_auto = jobsite.JobsiteTypeId
            };

            using (var context = new SharedContext())
            {
                context.CRSF.Add(newJobsite);
                try
                {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    return Tuple.Create(Convert.ToInt64(-1), "Failed to create jobsite. ");
                }
            }

            return Tuple.Create(newJobsite.crsf_auto, "Jobsite added successfully. ");
        }
    }
}
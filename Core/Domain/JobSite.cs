using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using DAL;
using BLL.Extensions;
using System.Threading.Tasks;

namespace BLL.Core.Domain
{
    public class JobSite:SharedDomain
    {
        private int Id = 0;
        private CRSF DALJobsite;
        public JobSite(DAL.SharedContext context, int Id):base(context)
        {
            this.Id = Id;
        }
        public JobSite(DAL.SharedContext context) : base(context)
        {
            
        }
        public CRSF getJobSite()
        {
            DALJobsite = _domainContext.CRSF.Find(Id);
            return DALJobsite;
        }

        public List<SystemDetailsViewModel> getSystemDetailsList(DateTime date)
        {
            List<SystemDetailsViewModel> result = new List<SystemDetailsViewModel>();
            if (getJobSite() == null)
                return result;

            var systems = _domainContext.LU_Module_Sub.Where(m => m.crsf_auto == Id && m.equipmentid_auto == null).ToList();
            foreach (var ucSystem in systems)
            {
                result.Add(new UCSystem(new DAL.UndercarriageContext(), ucSystem.Module_sub_auto.LongNullableToInt()).getSystemDetails(date));
            }
            return result.OrderBy(m=> m.Serial).ToList();
        }
        public async Task<List<SystemDetailsViewModel>> getSystemDetailsListAsync(DateTime date)
        {
            return await Task.Run(() => getSystemDetailsList(date));
        }

        public IQueryable<DAL.CRSF_TYPE> getJobsiteTypes()
        {
            return _domainContext.CRSF_TYPE;
        }
    }
}
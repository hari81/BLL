using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using System.Data.Entity;
using DAL;
using System.Threading.Tasks;

namespace BLL.Core.Domain
{
    public class Customer : UCDomain, ICustomer
    {
        public Customer(IUndercarriageContext context):base(context)
        {
            
        }
        /// <summary>
        /// returns all the reports available for this customer
        /// </summary>
        /// <param name="CustomerId"></param>
        /// <returns></returns>
        public IEnumerable<ReportVwMdl> getCustomerAvailableReports(int CustomerId)
        {
            return _domainContext.CustomerReports.Where(m => m.CustomerId == CustomerId)
                .Select(m => new ReportVwMdl { ReportId = m.ReportId, ReportName = m.Report.report_display_name });
        }
       

        /// <summary>
        /// returns all the available reports for this customer's dealership
        /// </summary>
        /// <param name="CustomerId"></param>
        /// <returns></returns>
        public IEnumerable<ReportVwMdl> getCustomerDealershipAvailableReports(int CustomerId)
        {
            var customer = _domainContext.CUSTOMERs.Find(CustomerId);
            if (customer == null)
                return new List<ReportVwMdl>().AsEnumerable();

            return _domainContext.DealershipReports.Where(m => m.DealershipId == customer.DealershipId)
                .Select(m => new ReportVwMdl { ReportId = m.ReportId, ReportName = m.Report.report_display_name });
        }
        /// <summary>
        /// Returns a customer by quoteId
        /// </summary>
        /// <param name="QuoteId"></param>
        /// <returns></returns>
        public CUSTOMER getCustomerByQuoteId(int QuoteId)
        {
            var quote = _domainContext.TRACK_QUOTE.Find(QuoteId);
            if (quote == null)
                return null;
            var inspection = _domainContext.TRACK_INSPECTION.Find(quote.inspection_auto);
            if (inspection == null || inspection.EQUIPMENT == null)
                return null;

            var jobsite = _domainContext.CRSF.Find(inspection.EQUIPMENT.crsf_auto);
            if (jobsite == null)
                return null;
            return _domainContext.CUSTOMERs.Find(jobsite.customer_auto);
        }

        public byte[] GetCustomerLogoById(long customerId)
        {
            return _domainContext.CUSTOMERs.Find(customerId).logo;
        }
    }
}
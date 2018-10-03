using System.Collections.Generic;
using System.Linq;
using BLL.Core.Domain;
using DAL;

namespace BLL.Core.UCReport
{
    public class Report:UCDomain
    {
        public Report(UndercarriageContext context):base(context)
        {

        }
        /// <summary>
        /// Returns available reports for customer and dealership
        /// if dealership is zero returns current customer's dealership available reports
        /// </summary>
        /// <param name="CustomerId"></param>
        /// <param name="DealershipId"></param>
        /// <returns></returns>
        public IEnumerable<ReportVwMdl> getAvailableReports(int CustomerId, int DealershipId)
        {
            if(DealershipId != 0)
            return new Customer(_domainContext).getCustomerAvailableReports(CustomerId).Concat(new BLL.Core.Domain.Dealership(_domainContext).getDealershipAvailableReports(DealershipId)).GroupBy(m => m.ReportId).Select(m => m.First());

            return new Customer(_domainContext).getCustomerAvailableReports(CustomerId).Concat(new BLL.Core.Domain.Customer(_domainContext).getCustomerDealershipAvailableReports(CustomerId)).GroupBy(m => m.ReportId).Select(m => m.First());
        }


        /// <summary>
        /// This method returns all the reports for all the dealers in parameter list
        /// </summary>
        /// <param name="dealers"></param>
        /// <returns></returns>
        public IEnumerable<ReportVwMdl> getAvailableDealersReports(IQueryable<int> dealers)
        {
            var result = new List<ReportVwMdl>();
            foreach (var dealer in dealers)
                result.AddRange(new BLL.Core.Domain.Dealership(_domainContext).getDealershipAvailableReports(dealer));
            return result.GroupBy(m => m.ReportId).Select(m => m.First());
        }

        public IEnumerable<ReportVwMdl> getAvailabeDealerQuoteReports(IQueryable<int> dealers)
        {
            var result = new List<ReportVwMdl>();
            foreach (var dealer in dealers)
                result.AddRange(new BLL.Core.Domain.Dealership(_domainContext).getDealershipAvailableQuoteReports(dealer));
            return result.GroupBy(m => m.ReportId).Select(m => m.First());
        }
        /// <summary>
        /// Returns the report file name by finding the customer of the current equipment based
        /// on the given quoteId and then returns the selected report for the customer.
        /// returns default report name if anything goes wrong
        /// </summary>
        /// <param name="QuoteId"></param>
        /// <returns></returns>
        public string getReportFileNameBasedOnQuoteId(int QuoteId, int dealerId)
        {
            var customer = new Customer(_domainContext).getCustomerByQuoteId(QuoteId);
            if(customer == null)
                return "UC_TTSummary.rpt"; //just the default report -> We don't know who is the customer!!!
            if (customer.SelectedReport != null)
                return customer.SelectedReport.report_display_desc;
            var dealershipReports = _domainContext.DealershipReports.Where(m => m.DealershipId == dealerId);

            if(dealershipReports.Count() == 0)
                return "UC_TTSummary.rpt";
            return dealershipReports.First().Report.report_display_desc ?? "UC_TTSummary.rpt";
        }

        public DAL.EQUIPMENT getEquipmentByQuoteId(int QuoteId) {
            var quote = _domainContext.TRACK_QUOTE.Find(QuoteId);
            if (quote == null)
                return null;
            var inspection = _domainContext.TRACK_INSPECTION.Find(quote.inspection_auto);
            if (inspection == null || inspection.EQUIPMENT == null)
                return null;
            return inspection.EQUIPMENT;
        }
        /// <summary>
        /// Returns the report record from FLUID_REPORT_LU_REPORTS by finding the customer of the current equipment based
        /// on the given quoteId and then returns the selected report for the customer.
        /// returns default report name if anything goes wrong
        /// </summary>
        /// <param name="QuoteId"></param>
        /// <returns></returns>
        public string getLUReportToolNameBasedOnQuoteId(int QuoteId, int dealerId)
        {
            var customer = new Customer(_domainContext).getCustomerByQuoteId(QuoteId);
            if (customer == null)
                return "rtTTUndercarriageReport"; //just the default report -> We don't know who is the customer!!!
            if (customer.SelectedReport != null)
                return customer.SelectedReport.report_tool_name;
            var dealershipReports = _domainContext.DealershipReports.Where(m => m.DealershipId == dealerId);

            if (dealershipReports.Count() == 0)
                return "rtTTUndercarriageReport";
            return dealershipReports.First().Report.report_tool_name ?? "rtTTUndercarriageReport"; ;
        }

        /// <summary>
        /// Returns dealership logo based on the QuoteId
        /// </summary>
        /// <param name="QuoteId"></param>
        /// <returns></returns>
        public byte[] getDealershipLogoBasedOnQuoteId(int QuoteId)
        {
            var customer = new Customer(_domainContext).getCustomerByQuoteId(QuoteId);
            if (customer == null)
                return _domainContext.DealershipBranding.FirstOrDefault().DealershipLogo ?? new byte[0]; //just the default logo -> We don't know who is the customer!!!
            var brandings = _domainContext.DealershipBranding.Where(m => m.DealershipId == customer.DealershipId);
            if(brandings.Count() == 0)
                return _domainContext.DealershipBranding.FirstOrDefault().DealershipLogo ?? new byte[0]; //just the default logo 
            return brandings.FirstOrDefault().DealershipLogo;
        }



        /// <summary>
        /// base on customer is belonged to which dealership returns this dealer's selected quote report options for customer to choose which one to use
        /// </summary>
        /// <param name="CustomerId"></param>
        /// <param name="DealershipId"></param>
        /// <returns></returns>
        public List<ReportVwMdl> GetAllDealerSelectedStyles(int CustomerId, int DealershipId)
        {
            if (DealershipId != 0)
            {
                //todo
            }
            var availableOptions = _domainContext.DealershipQuoteReports.Where(d => d.DealershipId == DealershipId).ToList();
            var results = new List<ReportVwMdl>();
            foreach (var item in availableOptions)
            {
                results.Add(new ReportVwMdl { ReportId = item.QuoteReportId, ReportName = item.QuoteReport.QuoteReportDesc });
            }
            return results;
        }


        public void InsertNewQuoteReportStyle(ReportVwMdl model)
        {
            _domainContext.LU_QuoteReports.Add(new LU_QuoteReport() { QuoteReportDesc = model.ReportName });

            _domainContext.SaveChanges();
        }


       

    }
}
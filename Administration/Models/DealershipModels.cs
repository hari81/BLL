using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Administration.Models
{
    public class DealershipOverviewModel
    {
        public long DealershipId { get; set; }
        public string DealershipName { get; set; }
        public List<BrandedStyleModel> BrandedStyles { get; set; }
    }

    public class NewDealershipModel {
        public string DealershipName { get; set; }
        public int DealerGroupId { get; set; }
        public List<ReportModel> ReportStyles{ get; set; }
        public List<ReportModel> QuoteReportStyles { get; set; }

    }

    public class UpdateDealershipModel: NewDealershipModel
    {
        public int DealershipId { get; set; }
    }

    public class ReportModel
    {
        public int ReportId { get; set; }
        public string ReportName { get; set; }
    }

    public class BrandedStyleModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ColourSchemeModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class BrandedStyleDetailsModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DealershipId { get; set; }
        public int ColourSchemeId { get; set; }
        public string LogoutRedirectUrl { get; set; }
        public string IdentityServerUrl { get; set; }
        public string UndercarriageUrl { get; set; }
        public string UndercarriageUiUrl { get; set; }
        public string GetUrl { get; set; }
        public string GetUiUrl { get; set; }
        public string Logo { get; set; }
    }
}
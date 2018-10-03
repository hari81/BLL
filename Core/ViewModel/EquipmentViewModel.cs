using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
namespace BLL.Core.ViewModel
{
    public class EquipmentViewModel
    {
        public int Id { get; set; }
        public string Serial { get; set; }
        public string Unit { get; set; }
        public int Life { get; set; }
        public int SMU { get; set; }
        public DateTime NextInspectionDate { get; set; }
        public int NextInspectionSMU { get; set; }
        public string ModelDesc { get; set; }
        public string SiteName { get; set; }
        public JobSiteForSelectionVwMdl JobSite { get; set; }
        public CustomerForSelectionVwMdl Customer { get; set; }
        public MakeModelFamily MakeModelFamily { get; set; }
    }
}
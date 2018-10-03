using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Core.WSRE.Models;

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

    public class TemplateViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int FamilyId { get; set; }
        public int CompartTypeId { get; set; }
        public string TypeName { get; set; }
        public DateTime modifiedDate { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
        
        public Boolean Assigned { get; set; }
        
    }

    public class PartTemplateViewModel
    {
        public List<TemplateViewModel> TemplateViewModel { get; set; }
        public List<DownloadLU_COMPART_TYPE> UnassignedParts { get; set; } // parts without templates assigned to them
    }

}
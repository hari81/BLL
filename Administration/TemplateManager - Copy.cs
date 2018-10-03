using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

// copied from Equipment.cs
using BLL.Interfaces;
using DAL;
using System.Data.Entity;
using BLL.Persistence.Repositories;
using BLL.Extensions;
using System.Threading.Tasks;
using BLL.Core.Domain;
using BLL.Core.ViewModel;
using BLL.Core.WSRE.Models;


// ...

namespace BLL.Administration
{


    public class TemplateManager
    {
        private UndercarriageContext _undercarriageContext;
        public TemplateManager()
        {
            this._undercarriageContext = new UndercarriageContext();
        }

        public List<FamilyForSelectionVwMdl> GetFamilies()
        {
            return _undercarriageContext.TYPEs
                
                .Select(m => new FamilyForSelectionVwMdl
                {
                    Id = m.type_auto, Symbol = m.typeid, Title = m.typedesc
                })
                .ToList();
        }

        // To fetch models, use like: await CONTEXT.EF_MODEL.<Linq_Query>
        // 
        // Get definitions/templates of what parts the main/primary machine can use
        //  Each primary machine has a min and max amount of parts it can use
        public List<TemplateViewModel> GetFamilyTemplates(int Id)
        {
            List<int> partIds = new List<int>(new int[] { 230, 231, 232, 233, 234, 235, 236, 237, 240, 446 });
            
            return _undercarriageContext.SystemFamilyTemplate
                .Where(m => m.FamilyId == Id)
                .Where(m => partIds.Any(k => k == m.CompartTypeId))
                //.Where(m => m.CompartType.comparttype_auto == 235)
                .Select(m => new TemplateViewModel
                {
                    Id = m.Id,
                    CompartTypeId = m.CompartTypeId,
                    FamilyId = m.FamilyId,
                    Name = m.Name,
                    TypeName = m.CompartType.comparttype,
                    Min = m.Min,
                    Max = m.Max,
                    Assigned = true // not a database field
                    //modifiedDate = m.CompartType.modified_date ?? DateTime.Now,                    
                })
                .ToList();
        }

        public List<DownloadLU_COMPART_TYPE> GetPartTypes()
        {
            List<int> partIds = new List<int>(new int[] { 230, 231, 232, 233, 234, 235, 236, 237, 240, 446 });
            return _undercarriageContext.LU_COMPART_TYPE
                    .Where(m => partIds.Any(k => k == m.comparttype_auto))
                    .Select(m => new DownloadLU_COMPART_TYPE
                    {
                        comparttype_auto = m.comparttype_auto,
                        comparttypeid = m.comparttypeid,
                        comparttype = m.comparttype
                    })
                    .ToList();
        }

        //public Tuple<int, string> UpdateFamilyTemplates(List<TemplateViewModel> templateUpdates)
        public string UpdateFamilyTemplates(List<TemplateViewModel> templateUpdates)
        {
            // if id == null, create
            // else update

            // create
            // required: part type id (comparttype_auto), FamilyId

            try
            {
                foreach (var template in templateUpdates)
                {
                    var updatethis = _undercarriageContext.SystemFamilyTemplate.Where(t => t.Id == template.Id);
                    foreach (var item in updatethis)
                    {
                        item.Name = template.Name;
                        item.Min = template.Min;
                        item.Max = template.Max;
                    }
                }
                _undercarriageContext.SaveChanges();
                return "1";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToDetailedString());
            }
        }

        public List<SystemFamilyTemplate> CreateFamilyTemplates(List<TemplateViewModel> newTemplates)
        {
            // if id == null, create
            // else update

            // create
            // required: part type id (comparttype_auto), FamilyId

            try
            {
                List<SystemFamilyTemplate> createdTemplates = new List<SystemFamilyTemplate>();
                foreach (var template in newTemplates)
                {
                    var newTemplate = new SystemFamilyTemplate
                    {
                        Name = template.Name,
                        Min = template.Min,
                        Max = template.Max,
                        CompartTypeId = template.CompartTypeId,                        
                        FamilyId  = template.FamilyId,
                        TypeName = template.TypeName
                    };
                    _undercarriageContext.SystemFamilyTemplate.Add(newTemplate);
                    createdTemplates.Add(newTemplate);
                }
                _undercarriageContext.SaveChanges();
                // get each generated value
                
                return createdTemplates;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToDetailedString());
            }
        }

    }

    /*
        * Example Data Access
        * 
        var reports = await _undercarriageContext.DealershipReports.Where(r => r.DealershipId == dealershipId).Select(r => r.ReportId).ToListAsync();
        List<ReportModel> reportStyles = new List<ReportModel>();
        reports.ForEach(r =>
        {
            var report = _undercarriageContext.FLUID_REPORT_LU_REPORTS.Where(x => x.report_auto == r).FirstOrDefault();
            reportStyles.Add(new ReportModel()
            {
                ReportId = report.report_auto,
                ReportName = report.report_display_name
            });
        });
    */
}
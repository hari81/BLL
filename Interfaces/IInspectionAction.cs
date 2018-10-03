using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Core.Repositories;
using BLL.Core.Domain;
using DAL;
namespace BLL.Interfaces
{
    public interface IGeneralInspectionModel
    {
        //Inspection Id
        int Id { get; set; } 
        int EquipmentId { get; set; }
        DateTime Date { get; set; } 
        int SMU { get; set; } 
        int Life { get; set; } 
        string DocketNo { get; set; }
        int TrammingHours { get; set; }
        string CustomerContact { get; set; }
        string InspectionNotes { get; set; }

        JobSiteForSelectionVwMdl JobSite { get; set; }
        decimal TrackSagLeft { get; set; }
        decimal TrackSagRight { get; set; }
        decimal DryJointsLeft { get; set; }
        decimal DryJointsRight { get; set; }
        decimal ExtCannonLeft { get; set; }
        decimal ExtCannonRight { get; set; }
        int Impact { get; set; }
        int Abrasive { get; set; }
        int Moisture { get; set; }
        int Packing { get; set; }
        string JobSiteNotes { get; set; }
        bool SMUValidationFailed { get; set; }
        string SMUMessage { get; set; }
    }
}

using BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ViewModel
{
    public class InspectionViewModel
    {
        public int Id { get; set; } = 0;
        public int EquipmentId { get; set; } = 0;
        public DateTime Date { get; set; } = DateTime.Now.ToLocalTime().Date;
        public int SMU { get; set; } = 0;
        public int QuoteId { get; set; } = 0;
        public List<ComponentActionViewModel> RecommendedActions { get; set; } = new List<ComponentActionViewModel>();
    }
    public class ComponentWornReadingViewModel {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public int ComponentId { get; set; }
        public decimal Reading { get; set; }
        public int ToolId { get; set; }
        public string ToolSymbol { get; set; }
        public decimal WornPercentage { get; set; }
    }

    public class EquipmentInspectionV
    {
        public int InspectionId { get; set; } = 0;
        public UserViewModel Inspector { get; set; }
        public EquipmentViewModel Equipment { get; set; }
        public EvalDetailsV Eval { get; set; }
        public IQueryable<Domain.CompartTypeV> CompartTypes { get; set; }
    }

    public class GeneralInspectionViewModel: IGeneralInspectionModel
    {
        public int Id { get; set; } = 0;
        public int EquipmentId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now.ToLocalTime().Date;
        public int SMU { get; set; } = 0;
        public int Life { get; set; } = 0;
        public int TrammingHours { get; set; }
        public string CustomerContact { get; set; }
        public string InspectionNotes { get; set; }
        public string DocketNo { get; set; }
        public Domain.JobSiteForSelectionVwMdl JobSite { get; set; }
        public decimal TrackSagLeft { get; set; }
        public decimal TrackSagRight { get; set; }
        public decimal DryJointsLeft { get; set; }
        public decimal DryJointsRight { get; set; }
        public decimal ExtCannonLeft { get; set; }
        public decimal ExtCannonRight { get; set; }
        public int Impact { get; set; }
        public int Abrasive { get; set; }
        public int Moisture { get; set; }
        public int Packing { get; set; }
        public string JobSiteNotes { get; set; }
        public bool SMUValidationFailed { get; set; }
        public string SMUMessage { get; set; }
    }

    public class EvalDetailsV
    {
        public decimal Reading { get; set; } = 0;
        public string ObservationNote { get; set; }
        public string EvalCode { get; set; } = "U";
    }

    public class EquipmentPhotosViewModel
    {
        public EquipmentPhotosViewModel()
        {
            this.Id = -1;
        }
        public int CustomerModelMandatoryImageId { get; set; }
        public int Id { get; set; }
        public string Photo { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class ComponentRecord
    {
        public ComponentRecord()
        {
            MeasurementPoints = new List<MeasurementPointRecord>();
        }
        public int Id { get; set; }
        public string Photo { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public decimal WornPercentage { get; set; }
        public List<MeasurementPointRecord> MeasurementPoints { get; set; }
    }

    public class MeasurementPointRecord
    {
        public int CompartMeasurementPointId { get; set; }
        public string Photo { get; set; }
        public decimal WornPercentage { get; set; }
        public string Name { get; set; }
        public string AverageReading { get; set; }
        public string Comment { get; set; }
        public List<MeasurementPointPhoto> Photos { get; set; }
        public List<MeasurementPointReading> Readings { get; set; }
    }

    public class MeasurementPointReading
    {
        public MeasurementPointReading()
        {
            Id = -1;
        }
        public int Id { get; set; }
        public decimal WornPercentage { get; set; }
        public decimal Measurement { get; set; }
        public int ToolId { get; set; }
    }

    public class MeasurementPointPhoto
    {
        public int Id { get; set; }
        public string Photo { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
    }

    public class AdditionalRecordOverviewModel
    {
        public int Id { get; set; }
        public int RecordId { get; set; }
        public string Type { get; set; }
    }

    public class AdditionalRecordModel
    {
        public string Name { get; set; }
        public string Data { get; set; } //observation text or measurement
    }

    public class RopeShovelInspectionSearchRequestModel
    {
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string SerialNumber { get; set; }
        public string UnitNumber { get; set; }
        public string InspectorName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class RopeShovelInspectionSearchResultModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string SerialNumber { get; set; }
        public string UnitNumber { get; set; }
        public string Evaluation { get; set; }
        public DateTime InspectionDate { get; set; }
        public string InspectorName { get; set; }
        public long EquipmentId { get; set; }
    }

    public class MandatoryEquipmentPhotoModel
    {
        public int InspectionId { get; set; }
        public int MandatoryImageId { get; set; }
        public string Photo { get; set; }
        public int PhotoRecordId { get; set; }
    }

    public class MandatoryCompartTypePhotoModel
    {
        public int InspectionId { get; set; }
        public int Side { get; set; }
        public int MandatoryImageId { get; set; }
        public string Photo { get; set; }
        public int PhotoRecordId { get; set; }
    }

    public class MeasurementPointPhotoModel
    {
        public int RecordId { get; set; }
        public string PhotoData { get; set; }
    }

    public class NewMeasurementPointPhotoModel
    {
        public int InspectionDetailId { get; set; }
        public int CompartMeasurePointId { get; set; }
        public string Photo { get; set; }
    }
}
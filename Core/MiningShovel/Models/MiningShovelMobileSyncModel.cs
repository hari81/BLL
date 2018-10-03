using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.MiningShovel.Models
{
    public class SyncImage
    {
        public int? UploadInspectionId { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
        public string FileName { get; set; }
        public string Data { get; set; }
    }

    // Sync function on mobile app
    public class SyncModel
    {
        public int serverInspectionId { get; set; }
        public String currentDateandTime { get; set; }
        public long equipmentid_auto { get; set; }
        public String examiner { get; set; }
        public String CustomerContact { get; set; }
        public String notes { get; set; }
        public String GeneralNotes { get; set; }
        public String Jobsite_Comms { get; set; }
        public int TrammingHours { get; set; }
        public short abrasive { get; set; }
        public short impact { get; set; }
        public short moisture { get; set; }
        public short packing { get; set; }
        public int smu { get; set; }
        public int? leftShoeNo { get; set; }
        public int? rightShoeNo { get; set; }
        public List<UploadImage> EquipmentImages { get; set; }
        public List<JobsiteImage> JobsiteImages { get; set; }
        public List<AdditionalImage> AdditionalImages { get; set; }
        public List<AdditionalImage> MandatoryImages { get; set; }
        public List<InspectionDetail> InspectionDetails { get; set; }
    }

    public class UploadImage
    {
        public string ImageFileName { get; set; }
        public string ImageTitle { get; set; }
        public string ImageComment { get; set; }
        public int ServerId { get; set; }
    }

    public class JobsiteImage
    {
        public string ImageFileName { get; set; }
        public string ImageTitle { get; set; }
        public string ImageComment { get; set; }
    }

    public class AdditionalImage
    {
        public int ServerId { get; set; }
        public int InspectionId { get; set; }
        public string Reading { get; set; }
        public string ToolCode { get; set; }
        public int Side { get; set; }
        public string ImageFileName { get; set; }
        public string ImageTitle { get; set; }
        public string ImageComment { get; set; }
    }

    public class MandatoryImage
    {
        public int ServerId { get; set; }
        public int InspectionId { get; set; }
        public int Side { get; set; }
        public string ImageFileName { get; set; }
        public string ImageTitle { get; set; }
        public string ImageComment { get; set; }
    }

    public class InspectionDetail
    {
        public int EqunitAuto { get; set; }
        public List<MeasurementPoint> MeasurementPoints { get; set; }
    }

    public class MeasurementPoint
    {
        public int MeasurementPointId { get; set; }
        public string ToolCode { get; set; }
        public string Notes { get; set; }
        public List<MeasurementPointReading> Measures { get; set; }
        public List<UploadImage> Images { get; set; }
    }

    public class MeasurementPointReading
    {
        public string reading { get; set; }
        public int measureNo { get; set; }
    }

}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.MiningShovel.Models
{
    public class MiningShovelInspectionOverview
    {
        public DateTime dateOfInspection { get; set; }
        public string customerName { get; set; }
        public long equipmentId { get; set; }
        public string equipmentMake { get; set; }
        public string equipmentModel { get; set; }
        public string unitNumber { get; set; }
        public string undercarriageSMU { get; set; }
        public string UCInspector { get; set; }
        public string equipmentJobsite { get; set; }
        public string equipmentCurrentSMU { get; set; }

        public string ReportDateStringFormat { get; set; }


        public List<SystemPrefillContentViewModel> systemPrefillContentViewModels { get; set; }
    }


    public class ReportPrefillContentViewModel
    {
        public List<SystemPrefillContentViewModel> Systems { get; set; }
    }

    public class SystemPrefillContentViewModel
    {
        public long SystemId { get; set; }
        public string SystemSerialNumber { get; set; }
        public long? CMU { get; set; }
        public int SystemTypeEnum { get; set; }
        public int Side { get; set; }
    }

    public class CompartMeasurementPoint
    {
        public int measurePointId { get; set; }
        public string measurePointName { get; set; }
        public int numberOfMeasurements { get; set; }
    }

    //public class MeasurementPointImage
    //{
    //    public int measurePointId { get; set; }
    //    public string imageData { get; set; }
    //    public string imageTitle { get; set; }
    //    public string imageComment { get; set; }
    //}

    public class ReadingValue
    {
        public string location { get; set; }
        public decimal left { get; set; }
        public decimal right { get; set; }
    }

    public class MeasurementPointReadings
    {
        public MeasurementPointReadings()
        {
            isHidden = true;
            isHiddenAll = false;
        }

        public int measurePointId { get; set; }
        public List<ReadingValue> listOfReadings { get; set; }
        public int totalCount { get; set; }
        public string tool { get; set; }
        public bool isHidden { get; set; }
        public bool isHiddenAll { get; set; }
    }

    public class MeasurementPointObservation
    {
        public int measurePointId { get; set; }
        public string observation { get; set; }
        public string side { get; set; }
        public string desc { get; set; }
    }

    public class SaveReportParams
    {
        public int MSInspectionID { get; set; }
        public long createdUser { get; set; }
        public string createdDate { get; set; }
    }

    public class SaveRecommendationPhotoParams
    {
        public int recommendationId { get; set; }
        public int photoType { get; set; }
        public int photoId { get; set; }
    }

    public class RecommendationParams
    {
        public int ReportId { get; set; }
        public int RecommendationId { get; set; }
        public string RecommendationTitle { get; set; }
        public string RecommendationText { get; set; }
    }

    public class InspectionPhoto
    {
        public InspectionPhoto()
        {
            photoType = (int)InspectionPhotoType.Undefined;
            isHidden = true;
            isLarge = false;
        }

        public int id { get; set; }
        public string data { get; set; }
        public string title { get; set; }
        public string comment { get; set; }
        public int photoType { get; set; }
        public bool isHidden { get; set; }
        public bool isLarge { get; set; }
        public int CompartTypeId { get; set; }
    }

    public class IntroductionParams
    {
        public int ReportId { get; set; }
        public int IntroId { get; set; }
        public int CoverImage { get; set; }
        public string IntroText1 { get; set; }
        public string IntroText2 { get; set; }
    }

    public class AdditionalRecordDetails
    {
        public string Title { get; set; }
        public string ReadingL { get; set; }
        public string ReadingR { get; set; }
        public string Tool { get; set; }
    }

    public class SummaryParams
    {
        public int ReportId { get; set; }
        public int SummaryId { get; set; }
        public string SummaryText { get; set; }
        public string RecommendationOverview { get; set; }
    }



    public class ToggleHideAndShowResult
    {
        public int Id { get; set; }
        public bool IsHiding { get; set; }
        public bool SavedResult { get; set; }
    }

    public enum InspectionPhotoType
    {
        Undefined = 0,
        Inspection_Mandatory_Photo = 1,
        Comparttype_Mandatory_Photo = 2,
        Comparttype_Additional_Photo = 3,
        Measurement_Point_Photo = 4
    }

    public class OverallComments
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public int CompartTypeId { get; set; }
        public string Comments { get; set; }
    }

    public class MeasurementPointDetails
    {
        public int MeasurePointId;
        public string MeasurePointName;
        public int NumberOfMeasurements;
        public bool isHidden;
        public List<MeasurementPointReadings> Readings;
        public List<InspectionPhoto> Images;
        public List<MeasurementPointObservation> Observations;
    }

    public class CompartInspection
    {
        public int Id;
        public string Name;
        public List<MeasurementPointDetails> MeasurementPoints;
        public List<InspectionPhoto> MandatoryImages;
        public List<InspectionPhoto> AdditionalImages;
        public List<AdditionalRecordDetails> AdditionalRecords;
        public List<AdditionalRecordDetails> CalculatedRecords;
        public OverallComments OverallComments;
        public bool isAdditionalRecordsHidden;
        public bool hideAll;
    }

    public enum ReportCompartTypes
    {
        TrackShoes = Domain.CompartTypeEnum.Link,
        TrackRollers = Domain.CompartTypeEnum.TrackRoller,
        Tumblers = Domain.CompartTypeEnum.Sprocket,
        FrontIdlers = Domain.CompartTypeEnum.Idler,
        CrawlerFrameGuide = Domain.CompartTypeEnum.CrawlerFrameGuide
    }
}
using BLL.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ViewModel
{
    public class InterpretationOverviewModel
    {
        public int InspectionId { get; set; }
        public DateTime InspectionDate { get; set; }
        public string InspectorName { get; set; }
        public bool Released { get; set; }
        public EquipmentModel Equipment { get; set; }
    }

    public class EquipmentModel
    {
        public long EquipmentId { get; set; }
        public int SMU { get; set; }
        public int LTD { get; set; }
        public string SerialNumber { get; set; }
        public string UnitNumber { get; set; }
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Family { get; set; }
        public string EquipmentPhoto { get; set; }
        //public bool TravelledKms { get; set; }
        //public int ForwardTravel { get; set; }
        //public int ReverseTravel { get; set; }
        public decimal TrackSagL { get; set; }
        public string TrackSagPhotoL { get; set; }
        public string TrackSagCommentL { get; set; }
        public decimal TrackSagR { get; set; }
        public string TrackSagPhotoR { get; set; }
        public string TrackSagCommentR { get; set; }
        public decimal DryJointsL { get; set; }
        public string DryJointsCommentsOnLeft { get; set; }
        public string DryJointsPhotoOnLeft { get; set; }
        public decimal DryJointsR { get; set; }
        public string DryJointsCommentsOnRight { get; set; }
        public string DryJointsPhotoOnRight { get; set; }
        public decimal CannonExtL { get; set; }
        public string CannonExtPhotoL { get; set; }
        public string CannonExtCommentL { get; set; }
        public decimal CannonExtR { get; set; }
        public string CannonExtPhotoR { get; set; }
        public string CannonExtCommentR { get; set; }
        public decimal ScallopL { get; set; }
        public string ScallopCommentOnLeft { get; set; }
        public string ScallopPhotoOnLeft { get; set; }
        public decimal ScallopR { get; set; }
        public string ScallopCommentOnRight { get; set; }
        public string ScallopPhotoOnRight { get; set; }
        //public int MyProperty { get; set; }
        public int ForwardTravelKM { get; set; }
        public int ReverseTravelKM { get; set; }
        public int ForwardTravelHrs { get; set; }
        public int ReverseTravelHrs { get; set; }
    }

    public class SystemModel
    {
        public long Id { get; set; }
        public Side Side { get; set; }
        public UCSystemType Type { get; set; }
        public string SerialNumber { get; set; }
        public DateTime DateInstalled { get; set; }
        public int SmuAtInstall { get; set; }
        public List<ComponentModel> Components { get; set; }
    }

    public class ComponentModel
    {
        public long ComponentId { get; set; }
        public MeasurementType MeasurementType { get; set; }
        public int InspectionDetailId { get; set; }
        public string ComponentTypeImage { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public decimal WornPercentage { get; set; }
        public decimal Measurement { get; set; }
        public string Tool { get; set; }
        public int Cmu { get; set; }
        public int RisidualLife100 { get; set; }
        public int RisidualLife120 { get; set; }
        public string PhotoThumbnail { get; set; }
        public long PhotoId { get; set; }
        public string Comment { get; set; }
        public int ComponentTypeId { get; set; }
        public bool IsChild { get; set; }
        public int SortOrder { get; set; }
    }

    public class SystemGraphModel
    {
        public long Id { get; set; }
        public Side Side { get; set; }
        public UCSystemType Type { get; set; }
        public string SerialNumber { get; set; }
        public DateTime DateInstalled { get; set; }
        public int SmuAtInstall { get; set; }
        public int InspectionId { get; set; }
        public int InspectionSmu { get; set; }
        public List<ComponentGraphModel> Components { get; set; }
    }

    public class ComponentGraphModel
    {
        public long ComponentId { get; set; }
        public int InspectionDetailId { get; set; }
        public string ComponentTypeImage { get; set; }
        public string Name { get; set; }
        public string Position { get; set; }
        public decimal WornPercentage { get; set; }
        public int Cmu { get; set; }
        public int RisidualLife100 { get; set; }
        public int RisidualLife120 { get; set; }
        public int SmuAtInstall { get; set; }
        public int CmuAtInstall { get; set; }
        public List<RecommendationModel> Recommendations { get; set; }
    }

    public class QuoteOverviewModel
    {
        public int QuoteId { get; set; }
        public List<int> RecommendationIds { get; set; }
        public List<RecommendationModel> Recommendations { get; set; }
    }

    public class RecommendationModel
    {
        public int RecommendationId { get; set; }
        public int ComponentId { get; set; }
        public string ComponentName { get; set; }
        public string Side { get; set; }
        public string Position { get; set; }
        public string RecommendationName { get; set; }
        public int ActionId { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PartsCost { get; set; }
        public decimal LabourCost { get; set; }
        public decimal MiscCost { get; set; }
        public int StartActionAtSmu { get; set; }
        public int CompleteActionBySmu { get; set; }
        public string Comment { get; set; }
        public int QuoteId { get; set; }
    }

    public class UpdateComponentPhotoModel
    {
        public int InspectionId { get; set; }
        public int PhotoId { get; set; }
        public string NewPhoto { get; set; }
    }

    public class UpdateConditionPhotoModel
    {
        public int InspectionId { get; set; }
        public Condition Condition { get; set; }
        public string NewPhoto { get; set; }
    }

    public class UpdateConditionCommentModel
    {
        public int InspectionId { get; set; }
        public Condition Condition { get; set; }
        public string NewComment { get; set; }
    }

    public class AddComponentPhotoModel
    {
        public int InspectionId { get; set; }
        public int InspectionDetailId { get; set; }
        public string NewPhoto { get; set; }
    }

    public class AuditModel
    {
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public DateTime Date { get; set; }
        public string EventDescription { get; set; }
    }

    public enum Condition
    {
        TrackSagL,
        TrackSagR,
        CannonExtL,
        CannonExtR,
        DryJointsL,
        DryJointsR,
        ScallopL,
        ScallopR
    }


    public enum ForwardAndReverseOptions
    {
        ForwardTravelKm,
        ReverseTravelKm,
        ForwardTravelHrs,
        ReverseTravelHrs,
    }

    public class UpdateInspectionForwardAndReverseTravelModel
    {
        public int InspectionId { get; set; }
        public ForwardAndReverseOptions ForwardAndReverseOptions { get; set; }
        public int NewReading { get; set; }
    }



    public class RecommendationTypeModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    public class AddRecommendationModel
    {
        public int InspectionId { get; set; }
        public int ComponentId { get; set; }
        public int QuoteId { get; set; }
        public int ActionId { get; set; }
        public decimal LabourCost { get; set; }
        public decimal PartsCost { get; set; }
        public decimal MiscCost { get; set; }
        public int SmuToTakeAction { get; set; }
        public int SmuToCompleteActionBy { get; set; }
        public string Comment { get; set; }
    }

    public class UpdateComponentMeasurementModel
    {
        public int InspectionId { get; set; }
        public int InspectionDetailId { get; set; }
        public decimal Measurement { get; set; }
        public MeasurementType MeasurementType { get; set; }
        public int ToolId { get; set; }
    }


    public class UpdateInspectionReadingBasedOnMeasurementClassModel
    {
        public int InspectionId { get; set; }
        public int Condition { get; set; }
        public decimal Reading { get; set; }
    }

    public class UpdateComponentToolUsed
    {
        public int InspectionId { get; set; }
        public int InspectionDetailId { get; set; }
        public int ToolId { get; set; }
    }

    public class UpdateRecommendationModel
    {
        public int InspectionId { get; set; }
        public int RecommendationId { get; set; }
        public int ComponentId { get; set; }
        public int QuoteId { get; set; }
        public int ActionId { get; set; }
        public decimal LabourCost { get; set; }
        public decimal PartsCost { get; set; }
        public decimal MiscCost { get; set; }
        public int SmuToTakeAction { get; set; }
        public int SmuToCompleteActionBy { get; set; }
        public string Comment { get; set; }
    }

    public class RecipientModel
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }




    public class AvailableInspectorsViewMode
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using DAL;
namespace BLL.Core.Domain
{
    public class ActionParams
    {
    }
    ///
    /// <summary>
    /// This class is used in any action 
    /// </summary>
    public class EquipmentActionRecord : IEquipmentActionRecord
    {
        public int Id { get; set; }
        public int GETHistoryId { get; set; }
        public int EquipmentId { get; set; }
        public int ReadSmuNumber { get; set; }
        public int EquipmentActualLife { get; set; }
        public ActionType TypeOfAction { get; set; }
        public decimal Cost { get; set; }
        public IUser ActionUser { get; set; }
        public DateTime ActionDate { get; set; }
        public string Comment { get; set; }
        public decimal PartsCost { get; set; }
        public decimal LabourCost { get; set; }
        public decimal MiscCost { get; set; }
    }
    public class InsertInspectionParams
    {
        public TRACK_INSPECTION EquipmentInspection { get; set; }
        public char EvaluationOverall { get; set; }
        public List<InspectionDetailWithSide> ComponentsInspection { get; set; }
    }
    public class UpdateInspectionParams
    {
        public TRACK_INSPECTION EquipmentInspection { get; set; }
        public char EvaluationOverall { get; set; }
        public List<InspectionDetailWithSide> ComponentsInspection { get; set; }
    }
    public class InspectionDetailWithSide
    {
        public TRACK_INSPECTION_DETAIL ComponentInspectionDetail { get; set; }
        public List<COMPART_ATTACH_FILESTREAM> CompartAttachFileStreamImage { get; set; }
        public int side { get; set; }
        public InspectionDetailWithSide()
        {
            side = 9;
        }
    }
    public class ReplaceComponentParams
    {
        public int currentComponentId { get; set; }
        public GENERAL_EQ_UNIT oldgeuComponent { get; set; }
        public GeneralComponent newComponent { get; set; }
    }
    public class ReplaceSystemParams
    {
        public int OldSystemId { get; set; }
        public int NewSystemId { get; set; }
    }
    public class UpdateComponentInstallationDetailParams
    {
        public int CompartId { get; set; }
        public string SystemSerialNumber { get; set; }
        public DateTime InstalledDate { get; set; }
        public int SMUatInstallation { get; set; }
        public int LTDatInstallation { get; set; }
        public int ComponentLifeAtInstallation { get; set; }
        public decimal ComponentCost { get; set; }
        public int BudgetLife { get; set; }
        public int UserId { get; set; }
    }
    public class SetupSystemParams
    {
        public int Id { get; set; }
        public string SerialNo { get; set; }
        public int JobSiteId { get; set; }
        public UCSystemType SystemType { get; set; }
        public int MakeId { get; set; }
        public int ModelId { get; set; }
        public EquipmentFamily Family { get; set; }
        public Side Side { get; set; }
        public int Life { get; set; }
        public int UserId { get; set; }
        public DateTime SetupDate { get; set; }
    }

    public class SetupComponentParams
    {
        public int Id { get; set; }
        public int CompartId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int BudgetLife { get; set; }
        public int Life { get; set; }
        public decimal Cost { get; set; }
        public int CMU { get; set; }
    }
    public class InstallComponentOnSystemParams
    {
        public int Id { get; set; }
        public int SystemId { get; set; }
        public byte Position { get; set; }
        public Side side { get; set; }
    }

    public class InstallSystemParams
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public Side side { get; set; }
    }

    public class GETInspectionParams
    {
        public int Id { get; set; }
        public long UserAuto { get; set; }
        public GETActionType ActionType { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime EventDate { get; set; }
        public string Comment { get; set; }
        public decimal Cost { get; set; }
        public int MeterReading { get; set; }
        public int GetAuto { get; set; }
        public long EquipmentIdAuto { get; set; }
    }

    public class GETEquipmentSetupParams
    {
        public int Id { get; set; }
        public long UserAuto { get; set; }
        public GETActionType ActionType { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime EventDate { get; set; }
        public string Comment { get; set; }
        public decimal Cost { get; set; }
        public int MeterReading { get; set; }
        public int EquipmentLTD { get; set; }
        public long EquipmentId { get; set; }
        public bool IsUpdating { get; set; } = false;
    }

    public class GETImplementSetupParams
    {
        public int Id { get; set; }
        public long UserAuto { get; set; }
        public GETActionType ActionType { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime EventDate { get; set; }
        public string Comment { get; set; }
        public decimal Cost { get; set; }
        public int ImplementLTD { get; set; }
        public int GetAuto { get; set; }
    }

    public class GETComponentReplacementParams
    {
        public int Id { get; set; }
        public long UserAuto { get; set; }
        public GETActionType ActionType { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime EventDate { get; set; }
        public string Comment { get; set; }
        public decimal Cost { get; set; }
        public int ComponentInspectionAuto { get; set; }
        public int MeterReading { get; set; }
    }

    public class GETUndoComponentReplacementParams
    {
        public int Id { get; set; }
        public long UserAuto { get; set; }
        public GETActionType ActionType { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime EventDate { get; set; }
        public string Comment { get; set; }
        public decimal Cost { get; set; }
        public int ComponentInspectionAuto { get; set; }
    }

    public class GETFlagIgnoredParams
    {
        public int Id { get; set; }
        public long UserAuto { get; set; }
        public GETActionType ActionType { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime EventDate { get; set; }
        public string Comment { get; set; }
        public decimal Cost { get; set; }
        public int ComponentInspectionAuto { get; set; }
        public int MeterReading { get; set; }
    }

    public class ChangeMeterUnitParams
    {
        public int Id { get; set; }
        public int SMUnew { get; set; }
    }

    public class AttachImplementToEquipmentParams
    {
        public int Id { get; set; }
        public long ImplementId { get; set; }
        public long EquipmentId { get; set; }
        public long JobsiteId { get; set; }
        public decimal Cost { get; set; }
        public int MeterReading { get; set; }
        public string Comment { get; set; }
        public long UserId { get; set; }
        public GETActionType ActionType { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime EventDate { get; set; }
    }

    public class MoveImplementToInventoryParams
    {
        public int Id { get; set; }
        public long ImplementId { get; set; }
        public long EquipmentId { get; set; }
        public long JobsiteId { get; set; }
        public decimal Cost { get; set; }
        public int MeterReading { get; set; }
        public string Comment { get; set; }
        public long UserId { get; set; }
        public GETActionType ActionType { get; set; }
        public DateTime RecordedDate { get; set; }
        public DateTime EventDate { get; set; }
        public int StatusId { get; set; }
        public int RepairerId { get; set; }
        public int WorkshopId { get; set; }
    }
}
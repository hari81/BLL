using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Domain
{
    public class ViewModel
    {
    }

    public class EquipmentComponentHistoryOldViewModel
    {
        public int ComponentId { get; set; }
        public string Serialno { get; set; }
        public string comparttype { get; set; }
        public long equnit_auto { get; set; }
        public int budget_life { get; set; }
        public string compart { get; set; }
        public string compartid { get; set; }
        public byte pos { get; set; }
        public int cmu { get; set; }
        public int lifeAfterInstallation { get; set; }
        public int projectedHours { get; set; }
        public decimal setup_cost { get; set; }
        public decimal total_cost { get; set; }
        public decimal worn { get; set; }
        public Side side { get; set; }
        public bool isChildComponent { get; set; } = false;
    }

    public class ComponentHistoryOldViewModel
    {
        public int ComponentId { get; set; }
        public ActionType ActionType { get; set; }
        public int ActionTypeId { get; set; }
        public string type { get; set; }
        public DateTime Actiondate { get; set; }
        public string event_date { get; set; }
        public int cmu { get; set; }
        public int equipmentSMU { get; set; }
        public int makeId { get; set; }
        public string makeSymbol { get; set; }
        public decimal cost { get; set; }
        public string action_description { get; set; }
        public string comment { get; set; }
        public decimal worn { get; set; }
        public string eval { get; set; }
        public Side side { get; set; }
        public bool isBushingTurned { get; set; }
        public int compartTypeId { get; set; }
        public int projectedHours { get; set; }
        public bool isChildComponent { get; set; } = false;
        public string RelatedLinkUrl { get; set; }
        public long ActionTakenId { get; set; }
        public int InspectionId { get; set; }
    }

    public class CompartToolImageViewModel
    {
        public int Id { get; set; }
        public int CompartId { get; set; }
        public int ToolId { get; set; }
        public string ToolName { get; set; }
        public string Title { get; set; }
        public string CreatedDate { get; set; }
        public string ImageDataBase64 { get; set; }
        public string ImageType { get; set; }
    }

    public class ServerReturnMessage
    {
        public int Id { get; set; }
        public bool Succeed { get; set; }
        public string Message { get; set; }
        public string ExceptionMessage { get; set; }
        public string InnerExceptionMessage { get; set; }
    }
    public class InspectionRowsTemporaryViewModel
    {
        public string compart { get; set; }
        public int compartid_auto { get; set; }
        public string track_comp_cts_maintype { get; set; }
        public string track_comp_cts_subtype { get; set; }
        public string pos { get; set; }
        public int side { get; set; }
        public string eval_code { get; set; }
        public string tool_name { get; set; }
        public decimal reading { get; set; }
        public decimal worn_percentage { get; set; }
        public int worn_percentage_120 { get; set; }
        public int component_hours { get; set; }
        public int remaining_hours { get; set; }
        public int ext_remaining_hours { get; set; }
        public int expected_life { get; set; }
        public int ext_expected_life { get; set; }
        public int equnit_auto { get; set; }
        public string smcs_code { get; set; }
        public int smu_at_install { get; set; }
        public int tool_auto { get; set; }
        public string comments { get; set; }
        public int track_0_worn { get; set; }
        public int track_100_worn { get; set; }
        public int track_120_worn { get; set; }
        public int eq_ltd_at_install { get; set; }
        public int imgCountLeft { get; set; }
        public int imgCountRight { get; set; }
        public int inspection_detail_auto { get; set; }
        public string compartid { get; set; }
        public int equipmentid_auto { get; set; }
        public int historytable_auto { get; set; }
        public int cmu { get; set; }
        public string comparttypeid { get; set; }
        public string Serialno { get; set; }
        public int comparttype_auto { get; set; }
        public int sorder { get; set; }
    }
    /// <summary>
    /// This view model is whatever should be part of the result for the mobile service
    /// </summary>
    public class CompartWornExtViewModel
    {
        public int Id { get; set; }
        public WornCalculationMethod method { get; set; }
        public List<DAL.TRACK_COMPART_WORN_LIMIT_ITM> ITMExtList { get; set; }
        public List<DAL.TRACK_COMPART_WORN_LIMIT_CAT> CATExtList { get; set; }
        public List<DAL.TRACK_COMPART_WORN_LIMIT_KOMATSU> KomatsuExtList { get; set; }
        public List<DAL.TRACK_COMPART_WORN_LIMIT_HITACHI> HitachiExtList { get; set; }
        public List<DAL.TRACK_COMPART_WORN_LIMIT_LIEBHERR> LiebherrExtList { get; set; }
    }

    public class HistoryFullEquipmentDetailsViewModel
    {
        public EquipmentDetailsViewModel EquipmentDetails { get; set; }
        public SystemDetailsViewModel LeftFrame { get; set; }
        public SystemDetailsViewModel LeftChain { get; set; }
        public SystemDetailsViewModel RightFrame { get; set; }
        public SystemDetailsViewModel RightChain { get; set; }
    }
    public class EquipmentDetailsViewModel
    {
        public int Id { get; set; }
        public string make { get; set; }
        public string model { get; set; }
        public string family { get; set; }
        public string serial { get; set; }
        public string unit { get; set; }
        public int smu { get; set; }
        public int ltd { get; set; }
        public string lastInspectionDate { get; set; }
        public decimal totalCost { get; set; }
        public decimal totalCostPerHour { get; set; }
    }
    public class SystemDetailsViewModel
    {
        public int Id { get; set; }
        public UCSystemType SystemType { get; set; }
        public Side Side { get; set; }
        public int Life { get; set; }
        public decimal Cost { get; set; }
        public decimal TotalCostPerHour { get; set; }
        public EvalCode Eval { get; set; }
        public string Serial { get; set; }
    }

    public class ComponentMakeViewModel
    {
        public int Id { get; set; }
        public int makeId { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
    }

    public class UndercarriageSetupViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int JobSiteId { get; set; }
        public int CustomerId { get; set; }
        public int MakeId { get; set; }
        public int ModelId { get; set; }
        public int FamilyId { get; set; }
        public string UserNameStr { get; set; }
        public List<UndercarriageSetupSystemViewModel> Systems { get; set; }
    }
    public class UndercarriageSetupSystemViewModel
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public string SerialNo { get; set; }
        public UCSystemType Type { get; set; }
        public Side side { get; set; }
        public int systemLife { get; set; }
        public int EquipmentSMU { get; set; }
        public DateTime InstallationDate { get; set; }
        public List<UndercarriageSetupComponentViewModel> Components { get; set; }
    }
    public class UndercarriageSetupComponentViewModel
    {
        public int Id { get; set; }
        public int SystemId { get; set; }
        public int CompartId { get; set; }
        public string CompartStr { get; set; }
        public int CompartTypeId { get; set; }
        public string CompartTypeStr { get; set; }
        public int EquipmetSMU { get; set; }
        public int BudgetLife { get; set; }
        public int ComponentCurrentLife { get; set; }
        public DateTime InstallationDate { get; set; }
        public decimal Cost { get; set; }
        public byte Position { get; set; }
        public Side side { get; set; }
    }
    public enum MenuDivisionType
    {
        Divider = 1,
        SubHeader = 2,
        Link = 3
    }

    public class MenuDivider
    {
        public string cssClass { get; set; }
        public string text { get; set; }
    }
    public class MenuModel
    {
        public int Id { get; set; }
        public bool IsSelected { get; set; }
        public MenuDivisionType DivisionType { get; set; }
        public MenuDivider Divider { get; set; }
        public MenuSubHeader SubHeader { get; set; }
        public MenuLink Link { get; set; }
    }
    public class MenuSubHeader
    {
        public string cssClass { get; set; }
        public string text { get; set; }
    }
    public class MenuLink
    {
        public string linkData { get; set; }
        public string aCssClass { get; set; }
        public string href { get; set; }
        public string iconCssClass { get; set; }
        public string iconText { get; set; }
        public string aText { get; set; }
    }

    public class ucDashbordViewModel
    {
        public int Id { get; set; }
        public int customerId { get; set; }
        public string customerName { get; set; }
        public int jobsiteId { get; set; }
        public string jobsiteName { get; set; }
        public string make { get; set; }
        public int makeId { get; set; }
        public string model { get; set; }
        public int modelId { get; set; }
        public string family { get; set; }
        public int familyId { get; set; }
        public string serial { get; set; }
        public string unit { get; set; }
        public int smu { get; set; }
        public int ltd { get; set; }
        public int lastInspectionId { get; set; }
        public string lastInspectionDate { get; set; }
        public DateTime lastInspectionDateAsDate { get; set; }
        public int quoteId { get; set; }
        public string EvalL { get; set; }
        public string EvalR { get; set; }
        public decimal? overAllEvalNumber { get; set; }
        public string Status { get; set; }
        public DateTime NextInspectionDate { get; set; }
        public int NextInspectionSMU { get; set; }
    }
    public class EqMakeVwMdl
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
    }
    public class EqModelVwMdl
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string Description { get; set; }
    }
    public class EqActionsVwMdl
    {
        public int Id { get; set; }
        public ActionType Type { get; set; }
        public string TypeAsString { get; set; }
        public DateTime Date { get; set; }
        public string DateAsStr { get; set; }
        public decimal Cost { get; set; }
        public string Comment { get; set; }
        public int EquipmentId { get; set; }
        public int SystemId { get; set; }
        public int ComponentId { get; set; }
        public int SMU { get; set; }
        public int LTD { get; set; }
        public int recordStatus { get; set; }
    }
    public class TopMenuLink
    {
        public int Id { get; set; }
        public int OrderIndex { get; set; }
        public string Href { get; set; }
        public string Text { get; set; }
        public bool OpenInNewWindow { get; set; }
    }
    public class TopMenuSpan
    {
        public int Id { get; set; }
        public int OrderIndex { get; set; }
        public string SpanText { get; set; }
        public string IconCssClass { get; set; }
        public string IconText { get; set; }
    }
    public class TopMenu
    {
        public int Id { get; set; }
        public string DivCssClass { get; set; }
        public List<TopMenuDivision> Divisions { get; set; }
    }
    public class TopMenuDivision
    {
        public int Id { get; set; }
        public int OrderIndex { get; set; }
        public string DivCssClass { get; set; }
        public string UlCssClass { get; set; }
        public List<LevelOne> levelOneList { get; set; }
    }
    public class LevelOne
    {
        public int Id { get; set; }
        public int OrderIndex { get; set; }
        public string LiCssClass { get; set; }
        public bool isMenuNotLink { get; set; }
        public TopMenuLink Link { get; set; }
        public TopMenuSpan Span { get; set; }
        public List<LevelTwo> levelTwoList { get; set; }
    }

    public class LevelTwo
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public TopMenuSpan Span { get; set; }
        public string UlCssClass { get; set; }
        public string LiCssClass { get; set; }
        public bool isMenuNotLink { get; set; }
        public TopMenuLink Link { get; set; }
        public List<LevelThree> levelThreeList { get; set; }
    }

    public class LevelThree
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string UlCssClass { get; set; }
        public string LiCssClass { get; set; }
        public TopMenuLink Link { get; set; }
        public TopMenuSpan Span { get; set; }
    }

    public class CustomerForSelectionVwMdl
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }
    public class JobSiteForSelectionVwMdl
    {
        public int Id = 0;
        public int CustomerId = 0;
        public string Title = "";
    }
    public class EquipmentForSelectionVwMdl
    {
        public int Id = 0;
        public int JobSiteId = 0;
        public string Serial = "";
        public string Unit = "";
    }

    public class EquipmentForSetupVwMdl
    {
        public int Id = 0;
        public int JobSiteId = 0;
        public string Serial = "";
        public string Unit = "";
        public DateTime SetupDate = DateTime.MinValue;
        public int SmuAtSetup = 0;
    }

    public class SystemForSetupVwMdl
    {
        public int Id = 0;
        public UCSystemType SystemType = UCSystemType.Unknown;
        public Side Side = Side.Unknown;
        public string Serial = "";
        public int EquipmentId = 0;
        public int JobsiteId = 0;
        public MakeForSelectionVwMdl Make = new MakeForSelectionVwMdl();
        public ModelForSelectionVwMdl Model = new ModelForSelectionVwMdl();
        public FamilyForSelectionVwMdl Family = new FamilyForSelectionVwMdl();
    }

    public class FamilyForSelectionVwMdl
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Symbol { get; set; }
        public int ExistingCount { get; set; }
    }
    public class MakeForSelectionVwMdl
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Symbol { get; set; }
        public int ExistingCount { get; set; }
    }
    public class ModelForSelectionVwMdl
    {
        public int Id { get; set; }
        public int MakeId { get; set; }
        public int FamilyId { get; set; }
        public int ExistingCount { get; set; }
        public string Title { get; set; }
    }

    public class ComponentTypeVwMdl
    {
        public int Id { get; set; }
        public short? CategoryId { get; set; }
        public string Name { get; set; }
    }

    public class ComponentActionVwMdl
    {
        public int Id { get; set; }
        public string ComponentStrTitle { get; set; }
        public DateTime ActionDate { get; set; }
        public string ActionDStr { get; set; }
        public ActionType ActionType { get; set; }
        public string ActionTypeStr { get; set; }
        public string Comment { get; set; }
        public decimal Cost { get; set; }
    }
    public class ComponentWornVwMdl
    {
        public int Id { get; set; }
        public string Eval { get; set; }
        public Side side { get; set; }
        public decimal wornPercentage { get; set; }
    }

    public class ComponentOverViewVwMdl
    {
        public int Id { get; set; }
        public int CompartId { get; set; }
        public string CompartPart { get; set; }
        public string CompartStr { get; set; }
        public int CompartTypeId { get; set; }
        public string CompartTypeStr { get; set; }
        public int SystemId { get; set; }
        public int EquipmentId { get; set; }
        public string SystemSerial { get; set; }
        public int Side { get; set; }
        public decimal WornPercentage { get; set; }
        public string EvalCode { get; set; }
        public int CMU { get; set; }
        public int RemainingLife100 { get; set; }
        public int RemainingLife120 { get; set; }
        public DateTime ComponentInstallationDate { get; set; }
        public string ComponentInstallationDateStr { get; set; }
        public int EquipmentSMU { get; set; }
        public int CmuAtInstall { get; set; }
    }

    public class IdAndDate
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
    }

    public class ComponentUnsyncedVwMdl
    {
        public int Id { get; set; }
        /// <summary>
        /// GENERAL_EQ_UNIT Id -> will be used for syncing 
        /// </summary>
        public int ComponentId { get; set; }
        public string Component { get; set; }
        public string Position { get; set; }
        public decimal Reading { get; set; }
        public string Image { get; set; }
        public string Comments { get; set; }
        public Side Side { get; set; }
    }
    public class JobSiteDetailsUnsyncedVwMdl
    {
        public int Id { get; set; }
        public string JobSiteName { get; set; }
        public decimal TrakSagLeft { get; set; }
        public decimal TrakSagRight { get; set; }
        public decimal DryJointsLeft { get; set; }
        public decimal DryJointsRight { get; set; }
        public decimal ExtCannonLeft { get; set; }
        public decimal ExtCannonRight { get; set; }
        public string Impact { get; set; }
        public string Abrasive { get; set; }
        public string Moisture { get; set; }
        public string Packing { get; set; }
        public string JobSiteNotes { get; set; }

    }
    public class InspectionUnsyncedVwMdl
    {
        public int Id { get; set; }
        public int MatchedEquipmentId { get; set; }
        public int MobileEqId { get; set; }
        public string Customer { get; set; }
        public string Serial { get; set; }
        public string UnitNumber { get; set; }
        public string Model { get; set; }
        public string InspectionDate { get; set; }
        public int SMU { get; set; }
        public string InspectionNotes { get; set; }
        public JobSiteDetailsUnsyncedVwMdl JobSiteDetails { get; set; }
        public List<ComponentUnsyncedVwMdl> InspectionDetails { get; set; }

    }
    public class ReportVwMdl
    {
        public int ReportId { get; set; }
        public string ReportName { get; set; }
    }

    public class QuoteReportStyleViewModel
    {
        public int QuoteReportId { get; set; }
        public string QuoteReportName { get; set; }
    }

    public class DealershipBarndingIdName
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class ApplicationStyleIdName
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class DealershipBrandingVwMdl
    {
        public int Id { get; set; } = 0;
        public string Name { get; set; } = "Unknown";
        public int DealershipId { get; set; } = 0;
        public int ApplicationStyleId { get; set; } = 0;
        public string DealershipLogo { get; set; } = "";
        public string IdentityHost { get; set; } = "";
        public string UCHost { get; set; } = "";
        public string UCUIHost { get; set; } = "";
        public string GETHost { get; set; } = "";
        public string GETUIHost { get; set; } = "";
        public string ReturnUrl { get; set; } = "http://www.tracktreads.com";
        public int authUserId { get; set; } = 0;
    }

    public class ModelSelectedViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Selected { get; set; }
    }

    public class MakeModelFamily
    {
        public int Id { get; set; }
        public FamilyForSelectionVwMdl Family { get; set; }
        public MakeForSelectionVwMdl Make { get; set; }
        public ModelForSelectionVwMdl Model { get; set; }
    }

    public class CompartV
    {
        public int Id { get; set; } = 0;
        public CompartTypeV CompartType = new CompartTypeV();
        public string CompartStr { get; set; } = "";
        public string CompartTitle { get; set; } = "";
        public int MeasurementPointsNo { get; set; } = 1;
        public string CompartNote { get; set; } = "";
        public int DefaultBudgetLife { get; set; } = 0;
        public ModelForSelectionVwMdl Model { get; set; }
        public MakeForSelectionVwMdl DefaultMake { get; set; }
    }

    public class CompartTypeV
    {
        public int Id { get; set; } = 0;
        public string Title { get; set; } = "";
        public int Order { get; set; } = 1;
    }

    public class IdTitleV
    {
        public int Id { get; set; } = 0;
        public string Title { get; set; } = "";
    }

    public class ShoeSizeV
    {
        public int Id { get; set; } = 0;
        public string Title { get; set; } = "";
        public float Size { get; set; } = 0;
    }

    public class SystemTemplateVwMdl
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ModelId { get; set; }
        public int CompartTypeId { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }

    public class UserSelectedIds
    {
        public int CustomerId { get; set; }
        public int JobSiteId { get; set; }
        public int EquipmentId { get; set; }
        public int FamilyId { get; set; }
        public int MakeId { get; set; }
        public int ModelId { get; set; }
        public DateTime LastReadingDate { get; set; }
    }

    /// <summary>
    /// Inspection history to be displayed in the recalc component CMU popup on UC inspection page. 
    /// </summary>
    public class ReclacCmuInspectionHistory
    {
        public int InspectionNumber { get; set; }
        public int SmuAtInspection { get; set; }
        public string DateOfInspection { get; set; }
    }

    public class NewComponentCmu
    {
        public int ComponentId { get; set; }
        public int Cmu { get; set; }
    }

    /// <summary>
    /// This model is returned for use in the recalculate component CMU popup on the uc inspection page. 
    /// It contains all the relevent data for the user to see the calculated component CMU and how it is calculated. 
    /// </summary>
    public class ReclacCmuComponentRecord
    {
        public long ComponentId { get; set; }
        public string ComponentName { get; set; }
        public string Position { get; set; }
        public int CmuAtSetup { get; set; }
        public decimal PercentWornFirstInspection { get; set; }
        public decimal PercentWornSecondInspection { get; set; }
        public decimal PercentWornDifference { get; set; }
        public double CalculatedCmu { get; set; }
        public double AverageCmuForComponentSystem { get; set; }
        public Byte Side { get; set; }
        public int SortOrder { get; set; }
    }
}
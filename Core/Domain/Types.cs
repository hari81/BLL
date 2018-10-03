using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Domain
{
    public enum ActionType
    {
        OldActionType = 1,
        Replace = 4,
        Weld = 2,
        TurnPinsAndBushingsLink = 9,
        RepairDryJointsLink = 8,
        AdjustTensionLink = 10,
        RepairDryJointsBush = 11,
        TurnPinsAndBushingsBush = 12,
        AdjustTensionBush = 13,
        Regrouser = 14,
        SwapFrontRear = 17,
        Reshell = 20,
        InsertInspection = 32, //DONE
        UpdateInspection = 33,//DONE
        ReplaceSystemFromInventory = 34,
        ReplaceComponentWithNew = 35,//DONE
        NoActionTakenYet = 36,
        EquipmentSetup = 37,
        InstallComponentOnSystemOnEquipment = 38,
        InstallSystemOnEquipment = 39,
        ChangeMeterUnit = 40,
        SMUReadingAction = 41,
        InsertInspectionGeneral = 42,
        UpdateInspectionGeneral = 43,
        UpdateSetupEquipment = 44,
        UpdateUndercarriageSetupOnEquipment = 45,
        GETAction = 99
    }

    public enum RecordStatus
    {
        Available = 0,
        Modified = 1,
        Undone = 2,
        Deleted = 3,
        MiddleOfAction = 4,
        TemporarilyDisabled = 5,
        NoContent = 204,
    }

    public enum GETActionType
    {
        NoActionTakenYet = 36,
        Inspection = 1,
        Equipment_Setup = 2,
        Implement_Setup = 3,
        Component_Replacement = 4,
        Undo_Component_Replacement = 5,
        Flag_Ignored = 6,
        Change_Implement_Jobsite = 7,
        Attach_Implement_to_Equipment = 8,
        Move_Implement_To_Inventory = 9,
        Change_Implement_Status = 10,
        Component_Repair = 11,
        Equipment_SMU_Changed = 12,
        Implement_Updated = 13,
        UpdateSetupEquipment = 44,
        UndercarriageAction = 98
    }
    public enum ComponentStatus
    {
        NewComponent = 0,
        UsableComponent = 1,
        UnUsableComponent = 2
    }
    public enum ActionStatus
    {
        Started,
        Valid,
        Invalid,
        Succeed,
        Failed,
        Cancelled,
        Close
    }
    public enum InspectionImpact
    {
        Low = 1,
        Normal = 1,
        High = 2
    }
    public enum LowNormalHigh
    {
        Low = 0,
        Normal = 1,
        High = 2
    }
    public enum WornLimit {
        A = 30,
        B = 50,
        C = 70,
        X = 100
    }

    public enum UCSystemType
    {
        Unknown = 0,
        Chain = 1,
        Frame = 2
    }
    public enum Side
    {
        Unknown = 0,
        Left = 1,
        Right = 2,
        Both = 3
    }
    public enum EvalCode
    {
        U = 0, //Unknown
        A = 1,
        B = 2,
        C = 3,
        X = 4
    }
    public class ResultMessage
    {
        public int Id { get; set; }
        public bool OperationSucceed { get; set; } = false;
        public string LastMessage { get; set; }
        public string ActionLog { get; set; }
    }
    public class ResultMessageExtended
    {
        public int Id { get; set; }
        public bool OperationSucceed { get; set; }
        public string LastMessage { get; set; }
        public string ActionLog { get; set; }
        public ActionPreValidationResult PreValidation { get; set; } = new ActionPreValidationResult();
    }
    public class ActionPreValidationResult
    {
        public int Id { get; set; } = 0;
        public int EquipmentId { get; set; } = 0;
        public bool IsValid { get; set; } = false;
        public int ProvidedSMU { get; set; } = 0;
        public DateTime ProvidedDate { get; set; } = DateTime.MinValue;
        public int SmallestValidSmuForProvidedDate { get; set; } = 0;
        public DateTime EarliestValidDateForProvidedSMU { get; set; } = DateTime.MinValue;
        public ActionValidationStatus Status { get; set; } = ActionValidationStatus.Unknown;
    }
    public enum ActionValidationStatus
    {
        Unknown = 0,
        Valid = 1,
        InvalidSMU = 2,
        InvalidEquipment = 3,
    }
    public enum CompartTypeEnum
    {
        Unknown = 0,
        Link = 230,
        Bushing = 231,
        Shoe = 232,
        Idler = 233,
        CarrierRoller = 234,
        TrackRoller = 235,
        Sprocket = 236,
        Guard = 237,
        TrackElongation = 240,
        CrawlerFrameGuide = 417
    }

    /*  EquipmentFamily
     *  163	TRAC Crawler Tractor
        167	DOZ	Dozer / TTT / Elevated Sprocket
        169	EXC	Excavator
        170	FP	Forestry Product
        172	DRI	Drill Rig
        177	MEX	Mining Shovel
        184	PIP	Pipelayer
        196	Unknown	Unknown
        197	DOZO	Oval Config Dozer
        198	CH	Cane Harvesters
        199	LOAD	Underground Loader
        200	OHT	Off Highway Truck
        201	DUMP	Dump Body
        202	RSH	Rope Shovel
     */
    public enum EquipmentFamily
    {
        Unknown = 196,
        TRAC_Crawler_Tractor = 163,
        DOZ_Dozer_TTT_Elevated_Sprocket = 167,
        EXC_Excavator = 169,
        FP_Forestry_Product = 170,
        DRI_Drill_Rig = 172,
        MEX_Mining_Shovel = 177,
        PIP_Pipelayer = 184,
        DOZO_Oval_Config_Dozer = 197,
        CH_Cane_Harvesters = 198,
        Underground_Loader = 199,
        Off_Highway_Truck = 200,
        Dump_Body = 201,
        Rope_Shovel = 202
    }

    /*  FROM TRACK_COMPART_WORN_CALC_METHOD TABLE
     *  1	ITM
        2	CAT
        3	Komatsu
        4	Hitachi
        5	Liebherr
        6	None
     */
    public enum WornCalculationMethod
    {
        ITM = 1,
        CAT = 2,
        Komatsu = 3,
        Hitachi = 4,
        Liebherr = 5,
        None = 6
    }
    public enum MeasurementType
    {
        Milimeter = 1,
        Inch = 2
    }
    public enum InfotrakApplications
    {
        IdentityServer = 1,
        UC = 2,
        UCUI = 3,
        GET = 4,
        GETUI = 5,
        OilCommander = 6
    }
    /// <summary>
    /// A client is an application that gets service from InfotrakIdentity.
    /// A the moment there are only two clients UCUI and GETUI
    /// </summary>
    public enum InfotrakClientApps
    {
        UCUI = 1,
        GETUI = 2
    }

    public enum SearchItemType
    {
        Customer = 1,
        Jobsite = 2,
        Equipment = 3,
        Family = 4, Make = 5, Model = 6,
        System = 7,
        EvaluationA = 8, EvaluationB = 9, EvaluationC = 10, EvaluationX = 11,
        StartDate = 12,
        EndDate = 13,
        CompartType = 14,
    }

    public class EquipmentSystemsExistence
    {
        public bool LeftFrame { get; set; }
        public bool LeftChain { get; set; }
        public bool RightFrame { get; set; }
        public bool RightChain { get; set; }
    }

    public enum AccessCategory
    {
        SupportTeam = 1,
        DealerGroup = 2,
        Dealer = 3,
        Customer = 4,
        Jobsite = 5,
        Equipment = 6,
        Unknown = 7
    }
    public enum ToolType
    {
        InvalidTool = -1,
        Ruler = 1,
        DepthGauge = 2,
        UltraSound = 3,
        Caliper = 4,
        KeeperPinObservation = 5,
        DriveLugsEngaged = 6,
        YesNo = 7,
        Observation = 8
    }

    public enum ImageIcon
    {
        Camera = 1,
        Comment = 2,
    }

    public enum TimeDistances
    {
        Hours = 0,
        Days = 1,
        Weeks = 2,
        Months = 3,
        Weekly = 4,
        Monthly = 5,
        Quarterly = 6,
        Yearly = 7
    }


    public enum LabourCostOption
    {
        Dollar = 0,
        Hourly = 1,
    }
}
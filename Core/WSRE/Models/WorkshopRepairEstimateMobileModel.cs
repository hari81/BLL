using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.WSRE.Models
{
    // Sync function on mobile app
    public class SyncImage
    {
        public int? UploadInspectionId { get; set; }
        public string Title { get; set; }
        public string Comment { get; set; }
        public string FileName { get; set; }
        public string Data { get; set; }
    }

    public class WRESSyncModel
    {
        public int serverInspectionId { get; set; }
        public long equipmentid_auto { get; set; }

        public long SystemId { get; set; }

        public long JobsiteId { get; set; }

        public String InspectorId { get; set; }

        public string JobNumber { get; set; }

        public string OldTagNumber { get; set; }

        public string OverallComment { get; set; }

        public string OverallRecommendation { get; set; }

        public string CustomerReference { get; set; }

        public int CrackTests_TestPassed { get; set; }

        public string CrackTests_Comment { get; set; }

        public List<WSREImage> CrackTestImages { get; set; }
        public List<WSREImage> InitialImages { get; set; }
        public List<WSREComponentRecordModel> ComponentRecords { get; set; }
        public List<WSREDiptestModel> DipTestRecords { get; set; }
    }

    public class WSREImage
    {
        public string Data { get; set; }
        public string ImageTitle { get; set; }
        public string ImageComment { get; set; }
        public string ImageTypeDescription { get; set; }
    }

    public class WSREComponentRecordModel
    {
        public string Comment { get; set; }
        public long ComponentId { get; set; }
        public decimal Measurement { get; set; }
        public string MeasurementToolId { get; set; }
        public string WornPercentage { get; set; }
        public List<WSREImage> Images { get; set; }
        public List<int> RecommendationId { get; set; }
    }

    public class WSREDiptestModel
    {
        public int Measurement { get; set; }
        public int ConditionId { get; set; }
        public string Comment { get; set; }
        public string Recommendation { get; set; }
        public int Number { get; set; }
        public List<WSREImage> Images { get; set; }
    }

    // Equipment selection screen on mobile
    public class WSREChainEquipmentModel
    {
        public long Module_sub_auto { get; set; }
        public string Serialno { get; set; }
        public Nullable<long> crsf_auto { get; set; }
        public Nullable<long> equipmentid_auto { get; set; }
        public Nullable<int> make_auto { get; set; }
        public Nullable<int> model_auto { get; set; }
        public int systemTypeEnumIndex { get; set; }
        public int LinksInChain { get; set; }
    }

    public class WSRENewChain
    {
        public string UserId { get; set; }
        public int CustomerId { get; set; }
        public int JobsiteId { get; set; }
        public string Serial { get; set; }
        public int HoursAtInstall { get; set; }
        public int MakeAuto { get; set; }
        public int ModelAuto { get; set; }
        public LinkComponent LinkComponent { get; set; }
        public BushingComponent BushingComponent { get; set; }
        public ShoeComponent ShoeComponent { get; set; }
    }

    public class LinkComponent
    {
        public int compartid_auto { get; set; }
        public int brand_auto { get; set; }
        public int budget_life { get; set; }
        public int hours_on_surface { get; set; }
        public int cost { get; set; }
    }

    public class BushingComponent
    {
        public int compartid_auto { get; set; }
        public int brand_auto { get; set; }
        public int budget_life { get; set; }
        public int hours_on_surface { get; set; }
        public int cost { get; set; }
    }

    public class ShoeComponent
    {
        public int compartid_auto { get; set; }
        public int brand_auto { get; set; }
        public int budget_life { get; set; }
        public int hours_on_surface { get; set; }
        public int cost { get; set; }
        public int shoe_size_id { get; set; }
        public String grouser { get; set; }
    }
}
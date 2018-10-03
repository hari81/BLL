using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.MiningShovel.Models
{
    /// <summary>
    /// Additional screen (Track Roller)
    /// </summary>
    public class AdditionalRecordModel
    {
        public string title { get; set; }
        public string record_type { get; set; }
        public string record_tool { get; set; }
        public int compart_type_additional_id { get; set; }
    }

    /// <summary>
    /// Measurement Points
    /// </summary>
    public class MeasurementPointModel
    {
        public long measurementpoint_id { get; set; }
        public String title { get; set; }
        public List<PossibleTool> tools { get; set; }
        public long number_of_reading { get; set; }
        public String default_tool_id { get; set; }
    }

    public class PossibleTool
    {
        public String tool { get; set; }
        public String image { get; set; }
        public String method { get; set; }
    }

    /// <summary>
    /// Mandatory images (Tumblers, Front Idlers, Crawler Frame Guide)
    /// </summary>
    public class MandatoryImageModel
    {
        public string title { get; set; }
        public int number_of_image { get; set; }
        public int compart_type_mandatory_image_id { get; set; }
    }

    /// <summary>
    /// Introduction images
    /// </summary>
    public class EquipmentImageModel
    {
        public string title { get; set; }
        public int number_of_image { get; set; }
        public int customer_model_mandatory_image_id { get; set; }
    }
}
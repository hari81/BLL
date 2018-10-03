using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.WSRE.Models
{
    // Sync function on mobile app
    public class DownloadMake
    {
        public int make_auto { get; set; }
        public string makeid { get; set; }
        public string makedesc { get; set; }
        public string dbs_id { get; set; }
        public DateTime? created_date { get; set; }
        public string created_user { get; set; }
        public DateTime? modified_date { get; set; }
        public string modified_user { get; set; }
        public int? cs_make_auto { get; set; }
        public bool cat { get; set; }
        public bool? Oil { get; set; }
        public bool? Components { get; set; }
        public bool? Undercarriage { get; set; }
        public bool? Tyre { get; set; }
        public bool? Rim { get; set; }
        public bool? Hydraulic { get; set; }
        public bool? Body { get; set; }
        public bool? OEM { get; set; }
    }

    public class DownloadModel
    {
        public int model_auto { get; set; }
        public string modelid { get; set; }
        public string modeldesc { get; set; }
        public byte? tt_prog_auto { get; set; }
        public byte? gb_prog_auto { get; set; }
        public byte? axle_no { get; set; }
        public DateTime created_date { get; set; }
        public string created_user { get; set; }
        public DateTime? modified_date { get; set; }
        public string modified_user { get; set; }
        public int? track_sag_maximum { get; set; }
        public int? track_sag_minimum { get; set; }
        public bool? isPSC { get; set; }
        public short? model_size_auto { get; set; }
        public int? cs_model_auto { get; set; }
        public bool? cat { get; set; }
        public short? model_pricing_level_auto { get; set; }
        public short? equip_reg_industry_auto { get; set; }
        public string ModelNote { get; set; }
        public int LinksInChain { get; set; }
        public decimal? UCSystemCost { get; set; }
        public byte[] ModelImage { get; set; }
    }

    public class DownloadLU_MMTA
    {
        public int mmtaid_auto { get; set; }

        public int make_auto { get; set; }

        public int model_auto { get; set; }

        public int type_auto { get; set; }

        public int? arrangement_auto { get; set; }

        public short? app_auto { get; set; }

        public int? service_cycle_type_auto { get; set; }

        public DateTime expiry_date { get; set; }

        public DateTime? created_date { get; set; }

        public string created_user { get; set; }

        public DateTime? modified_date { get; set; }

        public string modified_user { get; set; }

        public int? cs_mmtaid_auto { get; set; }
    }

    public class DownloadLU_COMPART_TYPE
    {
        public int comparttype_auto { get; set; }
        
        public string comparttypeid { get; set; }
        
        public string comparttype { get; set; }

        public int? sorder { get; set; }

        public bool _protected { get; set; }

        public long? modified_user_auto { get; set; }

        public DateTime? modified_date { get; set; }

        public long? implement_auto { get; set; }

        public bool? multiple { get; set; }

        public int? max_no { get; set; }

        public byte? progid { get; set; }

        public int? fixedamount { get; set; }

        public int? min_no { get; set; }

        public bool? getmesurement { get; set; }

        public short? system_auto { get; set; }

        public int? cs_comparttype_auto { get; set; }

        public long? standard_compart_type_auto { get; set; }

        public string comparttype_shortkey { get; set; }
    }

    public class DownloadLU_COMPART
    {
        public int compartid_auto { get; set; }
        public string compartid { get; set; }
        public string compart { get; set; }
        public int? smcs_code { get; set; }
        public string modifier_code { get; set; }
        public int? hrs { get; set; }
        public byte progid { get; set; }
        public bool? Left { get; set; }
        public int? parentid_auto { get; set; }
        public string parentid { get; set; }
        public short? childoptid { get; set; }
        public bool? multiple { get; set; }
        public int? fixedamount { get; set; }
        public long? implement_auto { get; set; }
        public bool? core { get; set; }
        public string group_id { get; set; }
        public decimal? expected_life { get; set; }
        public decimal? expected_cost { get; set; }
        public int comparttype_auto { get; set; }
        public string companyname { get; set; }
        public short sumpcapacity { get; set; }
        public short max_rebuilt { get; set; }
        public short oilsample_interval { get; set; }
        public short oilchg_interval { get; set; }
        public bool? insp_item { get; set; }
        public short? insp_interval { get; set; }
        public short? insp_uom { get; set; }
        public DateTime? created_date { get; set; }
        public string created_user { get; set; }
        public DateTime? modified_date { get; set; }
        public string modified_user { get; set; }
        public short? bowldisplayorder { get; set; }
        public short? track_comp_row { get; set; }
        public string track_comp_cts_maintype { get; set; }
        public string track_comp_cts_subtype { get; set; }
        public string compart_note { get; set; }
        public int? sorder { get; set; }
        public string hydraulic_inspect_symptoms { get; set; }
        public int? cs_compart_auto { get; set; }
        public int? positionid_auto { get; set; }
        public bool? allow_duplicate { get; set; }
        public bool AcceptEvalAsReading { get; set; }
        public long? standard_compartid_auto { get; set; }
        public int? ranking_auto { get; set; }
    }

    public class DownloadTRACK_COMPART_EXT
    {
        public long track_compart_ext_auto { get; set; }
        public int compartid_auto { get; set; }
        public int? CompartMeasurePointId { get; set; }
        public int? make_auto { get; set; }
        public int? tools_auto { get; set; }
        public int? budget_life { get; set; }
        public int? track_compart_worn_calc_method_auto { get; set; }
    }

    public class DownloadTRACK_COMPART_WORN_CALC_METHOD
    {
        public int track_compart_worn_calc_method_auto { get; set; }
        public string track_compart_worn_calc_method_name { get; set; }
    }

    public class DownloadSHOE_SIZE
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public float Size { get; set; }
    }

    public class DownloadTRACK_COMPART_MODEL_MAPPING
    {
        public int compart_model_mapping_auto { get; set; }
        public int compartid_auto { get; set; }
        public int model_auto { get; set; }
    }

    public class DownloadTYPE
    {
        public int type_auto { get; set; }
        public string typeid { get; set; }
        public string typedesc { get; set; }
        public DateTime? created_date { get; set; }
        public string created_user { get; set; }
        public DateTime? modified_date { get; set; }
        public string modified_user { get; set; }
        public int? cs_type_auto { get; set; }
        public int? blob_auto { get; set; }
        public int? blob_large_auto { get; set; }
        public long? default_smu { get; set; }
    }

    public class DownloadTRACK_TOOL
    {
        public int tool_auto { get; set; }
        public string tool_name { get; set; }
        public string tool_code { get; set; }
    }

    public class DownloadTRACK_ACTION_TYPE
    {
        public int action_type_auto { get; set; }
        public string action_description { get; set; }
        public string compartment_type { get; set; }
    }
}
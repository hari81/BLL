using BLL.Administration;
using BLL.Core.WSRE.Models;
using BLL.GETCore.Classes;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using BLL.Extensions;

namespace BLL.Core.Domain
{
    public class WSREMobileDownloadDB
    {

        private UndercarriageContext _context;

        public WSREMobileDownloadDB(UndercarriageContext context)
        {
            this._context = context;
        }

        public List<DownloadMake> GetMAKERecords()
        {
            List<DownloadMake> records = _context.MAKE.Select(m => new DownloadMake()
            {
                make_auto = m.make_auto,
                makeid = m.makeid,
                makedesc = m.makedesc,
                dbs_id = m.dbs_id,
                created_date = m.created_date,
                created_user = m.created_user,
                modified_date = m.modified_date,
                modified_user = m.modified_user,
                cs_make_auto = m.cs_make_auto,
                cat = m.cat,
                Oil = m.Oil,
                Components = m.Components,
                Undercarriage = m.Undercarriage,
                Tyre = m.Tyre,
                Rim = m.Rim,
                Hydraulic = m.Hydraulic,
                Body = m.Body,
                OEM = m.OEM,
            }).ToList();
            return records;
        }

        public List<DownloadModel> GetMODELRecords()
        {
            List<DownloadModel> records = _context.MODELs.Select(m => new DownloadModel()
            {
               model_auto = m.model_auto,
               modelid = m.modelid,
               modeldesc = m.modeldesc,
               tt_prog_auto = m.tt_prog_auto,
               gb_prog_auto = m.gb_prog_auto,
               axle_no = m.axle_no,
               created_date = m.created_date,
               created_user = m.created_user,
               modified_date = m.modified_date,
               modified_user = m.modified_user,
               track_sag_maximum = m.track_sag_maximum,
               track_sag_minimum = m.track_sag_minimum,
               isPSC = m.isPSC,
               model_size_auto = m.model_size_auto,
               cs_model_auto = m.cs_model_auto,
               cat = m.cat,
               model_pricing_level_auto = m.model_pricing_level_auto,
               equip_reg_industry_auto = m.equip_reg_industry_auto,
               ModelNote = m.ModelNote,
               LinksInChain = m.LinksInChain,
               UCSystemCost = m.UCSystemCost,
               ModelImage = m.ModelImage
            }).ToList();
            return records;
        }

        public List<DownloadLU_MMTA> GetLU_MMTARecords()
        {
            List<DownloadLU_MMTA> records = _context.LU_MMTA.Select(m => new DownloadLU_MMTA()
            {
                mmtaid_auto = m.mmtaid_auto,
                make_auto = m.make_auto,
                model_auto = m.model_auto,
                type_auto = m.type_auto,
                arrangement_auto = m.arrangement_auto,
                app_auto = m.app_auto,
                service_cycle_type_auto = m.service_cycle_type_auto,
                expiry_date = m.expiry_date,
                created_date = m.created_date,
                created_user = m.created_user,
                modified_date = m.modified_date,
                modified_user = m.modified_user,
                cs_mmtaid_auto = m.cs_mmtaid_auto,
            }).ToList();
            return records;
        }

        public List<DownloadLU_COMPART_TYPE> GetLU_COMPART_TYPERecords()
        {
            List<DownloadLU_COMPART_TYPE> records = _context.LU_COMPART_TYPE.Select(m => new DownloadLU_COMPART_TYPE()
            {
                comparttype_auto = m.comparttype_auto,
                comparttypeid = m.comparttypeid,
                comparttype = m.comparttype,
                sorder = m.sorder,
                _protected = m._protected,
                modified_user_auto = m.modified_user_auto,
                modified_date = m.modified_date,
                implement_auto = m.implement_auto,
                multiple = m.multiple,
                max_no = m.max_no,
                progid = m.progid,
                fixedamount = m.fixedamount,
                min_no = m.min_no,
                getmesurement = m.getmesurement,
                system_auto = m.system_auto,
                cs_comparttype_auto = m.cs_comparttype_auto,
                standard_compart_type_auto = m.standard_compart_type_auto,
                comparttype_shortkey = m.comparttype_shortkey,
            }).ToList();
            return records;
        }

        public List<DownloadLU_COMPART> GetLU_COMPARTRecords()
        {
            List<DownloadLU_COMPART> records = _context.LU_COMPART.Select(m => new DownloadLU_COMPART()
            {
                compartid_auto = m.compartid_auto,
                compartid = m.compartid,
                compart = m.compart,
                smcs_code = m.smcs_code,
                modifier_code = m.modifier_code,
                hrs = m.hrs,
                progid = m.progid,
                Left = m.Left,
                parentid_auto = m.parentid_auto,
                parentid = m.parentid,
                childoptid = m.childoptid,
                multiple = m.multiple,
                fixedamount = m.fixedamount,
                implement_auto = m.implement_auto,
                core = m.core,
                group_id = m.group_id,
                expected_life = m.expected_life,
                expected_cost = m.expected_cost,
                comparttype_auto = m.comparttype_auto,
                companyname = m.companyname,
                sumpcapacity = m.sumpcapacity,
                max_rebuilt = m.max_rebuilt,
                oilsample_interval = m.oilsample_interval,
                oilchg_interval = m.oilchg_interval,
                insp_item = m.insp_item,
                insp_interval = m.insp_interval,
                insp_uom = m.insp_uom,
                created_date = m.created_date,
                created_user = m.created_user,
                modified_date = m.modified_date,
                modified_user = m.modified_user,
                bowldisplayorder = m.bowldisplayorder,
                track_comp_row = m.track_comp_row,
                track_comp_cts_maintype = m.track_comp_cts_maintype,
                track_comp_cts_subtype = m.track_comp_cts_subtype,
                compart_note = m.compart_note,
                sorder = m.sorder,
                hydraulic_inspect_symptoms = m.hydraulic_inspect_symptoms,
                cs_compart_auto = m.cs_compart_auto,
                positionid_auto = m.positionid_auto,
                allow_duplicate = m.allow_duplicate,
                AcceptEvalAsReading = m.AcceptEvalAsReading,
                standard_compartid_auto = m.standard_compartid_auto,
                ranking_auto = m.ranking_auto,
            }).ToList();
            return records;
        }

        public List<DownloadTRACK_COMPART_EXT> GetTRACK_COMPART_EXTRecords()
        {
            List<DownloadTRACK_COMPART_EXT> records = _context.TRACK_COMPART_EXT.Select(m => new DownloadTRACK_COMPART_EXT()
            {
                track_compart_ext_auto = m.track_compart_ext_auto,
                compartid_auto = m.compartid_auto,
                CompartMeasurePointId = m.CompartMeasurePointId,
                make_auto = m.make_auto,
                tools_auto = m.tools_auto,
                budget_life = m.budget_life,
                track_compart_worn_calc_method_auto = m.track_compart_worn_calc_method_auto,
            }).ToList();
            return records;
        }

        public List<DownloadTRACK_COMPART_WORN_CALC_METHOD> GetTRACK_COMPART_WORN_CALC_METHODRecords()
        {
            List<DownloadTRACK_COMPART_WORN_CALC_METHOD> records = _context.TRACK_COMPART_WORN_CALC_METHOD.Select(m => new DownloadTRACK_COMPART_WORN_CALC_METHOD()
            {
                track_compart_worn_calc_method_auto = m.track_compart_worn_calc_method_auto,
                track_compart_worn_calc_method_name = m.track_compart_worn_calc_method_name,
            }).ToList();
            return records;
        }

        public List<DownloadSHOE_SIZE> GetSHOE_SIZERecords()
        {
            List<DownloadSHOE_SIZE> records = _context.SHOE_SIZE.Select(m => new DownloadSHOE_SIZE()
            {
                Id = m.Id,
                Title = m.Title,
                Size = m.Size,
            }).ToList();
            return records;
        }

        public List<DownloadTRACK_COMPART_MODEL_MAPPING> GetTRACK_COMPART_MODEL_MAPPINGRecords()
        {
            List<DownloadTRACK_COMPART_MODEL_MAPPING> records = 
                _context.TRACK_COMPART_MODEL_MAPPING.Select(m => new DownloadTRACK_COMPART_MODEL_MAPPING()
            {
                compart_model_mapping_auto = m.compart_model_mapping_auto,
                compartid_auto = m.compartid_auto,
                model_auto = m.model_auto,
            }).ToList();
            return records;
        }

        public List<DownloadTYPE> GetTYPERecords()
        {
            List<DownloadTYPE> records =
                _context.TYPEs.Select(m => new DownloadTYPE()
                {
                    type_auto = m.type_auto,
                    typeid = m.typeid,
                    typedesc = m.typedesc,
                    created_date = m.created_date,
                    created_user = m.created_user,
                    modified_date = m.modified_date,
                    modified_user = m.modified_user,
                    cs_type_auto = m.cs_type_auto,
                    blob_auto = m.blob_auto,
                    blob_large_auto = m.blob_large_auto,
                    default_smu = m.default_smu
                }).ToList();
            return records;
        }

        public List<DownloadTRACK_TOOL> GetTRACK_TOOLRecords()
        {
            List<DownloadTRACK_TOOL> records =
                _context.TRACK_TOOL.Select(m => new DownloadTRACK_TOOL()
                {
                    tool_auto = m.tool_auto,
                    tool_name = m.tool_name,
                    tool_code = m.tool_code
                }).ToList();
            return records;
        }

        public List<DownloadTRACK_ACTION_TYPE> GetTRACK_ACTION_TYPERecords()
        {
            List<DownloadTRACK_ACTION_TYPE> records =
                _context.TRACK_ACTION_TYPE.Select(m => new DownloadTRACK_ACTION_TYPE()
                {
                    action_type_auto = m.action_type_auto,
                    action_description = m.action_description,
                    compartment_type = m.compartment_type
                }).ToList();
            return records;
        }

    }
}
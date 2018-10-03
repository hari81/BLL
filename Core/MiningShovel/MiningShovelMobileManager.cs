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
using BLL.Core.MiningShovel.Models;

namespace BLL.Core.Domain
{
    public class MiningShovelMobileManager
    {
        private UndercarriageContext _context;

        public MiningShovelMobileManager(UndercarriageContext context)
        {
            this._context = context;
        }

        /// <summary>
        /// Additional records
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="modelId"></param>
        /// <param name="compartTypeId"></param>
        /// <returns></returns>

        public List<AdditionalRecordModel> GetAdditionalRecords(long customerId, long modelId, long compartTypeId)
        {
            List<AdditionalRecordModel> returnList = new List<AdditionalRecordModel>();
            var records = _context.CUSTOMER_MODEL_COMPARTTYPE_ADDITIONAL
                .Where(b => b.CompartTypeId == compartTypeId
                    && (b.CustomerId == null || b.CustomerId == customerId)
                    && (b.ModelId == null || b.ModelId == modelId)
                    && b.RecordStatus == 0)
                .OrderBy(b => b.Order).ToList();
            if (records == null)
                return null;

            foreach (var record in records)
            {
                string defaultToolId = "";
                if (record.DefaultToolId != null)
                {
                    defaultToolId = record.DefaultTool.tool_code;
                }

                returnList.Add(new AdditionalRecordModel
                {
                    title = record.Title,
                    record_type = record.AdditionalType.Description,
                    record_tool = defaultToolId,
                    compart_type_additional_id = record.Id
                });
            }

            return returnList;
        }

        /// <summary>
        /// Measurement points
        /// </summary>
        /// <param name="compartId"></param>
        /// <returns></returns>
        public List<MeasurementPointModel> GetMeasurementPointsByCompartId(long compartId)
        {
            List<MeasurementPointModel> returnList = new List<MeasurementPointModel>();
            var records = _context.COMPART_MEASUREMENT_POINT
                .Where(b => b.CompartId == compartId)
                .OrderBy(b => b.Order).ToList();
            if (records == null)
                return null;

            foreach (var record in records)
            {

                // Tools
                List<PossibleTool> list_tools = new List<PossibleTool>();
                var toolRecords = _context.MEASUREMENT_POSSIBLE_TOOLS
                    .Where(b => b.MeasurePointId == record.Id)
                    .ToList();
                if (toolRecords == null)
                    continue;
                foreach (var tool in toolRecords)
                {
                    // Method
                    String method = "";
                    var methodRecord = _context.TRACK_COMPART_EXT
                            .Where(
                                b => b.compartid_auto == compartId 
                                && b.CompartMeasurePointId == record.Id
                                && b.tools_auto == tool.ToolId)
                            .FirstOrDefault();
                    if (methodRecord != null)
                        method = methodRecord.TRACK_COMPART_WORN_CALC_METHOD.track_compart_worn_calc_method_name;

                    PossibleTool possibleTool = new PossibleTool();
                    possibleTool.tool = tool.Tool.tool_code;
                    possibleTool.method = method;
                    possibleTool.image = Convert.ToBase64String(tool.HowToUseImage);
                    list_tools.Add(possibleTool);
                }

                // Measurement points
                returnList.Add(new MeasurementPointModel
                {
                    measurementpoint_id = record.Id,
                    title = record.Name,
                    default_tool_id = record.DefaultTool.tool_code,
                    tools = list_tools,
                    number_of_reading = record.DefaultNumberOfMeasurements,
                });
            }

            return returnList;
        }

        /// <summary>
        /// Mandatory Images
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="modelId"></param>
        /// <param name="compartTypeId"></param>
        /// <returns></returns>
        public List<MandatoryImageModel> GetMandatoryImageRecords(long customerId, long modelId, long compartTypeId)
        {
            List<MandatoryImageModel> returnList = new List<MandatoryImageModel>();
            var records = _context.CUSTOMER_MODEL_COMPARTTYPE_MANDATORY_IMAGE
                .Where(b => b.CompartTypeId == compartTypeId
                    && (b.CustomerId == null || b.CustomerId == customerId)
                    && (b.ModelId == null || b.ModelId == modelId)
                    && b.RecordStatus == 0)
                .OrderBy(b => b.Order).ToList();
            if (records == null)
                return null;

            foreach (var record in records)
            {
                returnList.Add(new MandatoryImageModel
                {
                    title = record.Title,
                    number_of_image = record.DefaultNumberOfImages,
                    compart_type_mandatory_image_id = record.Id
                });
            }

            return returnList;
        }

        /// <summary>
        /// Equipment Images
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="modelId"></param>
        /// <param name="compartTypeId"></param>
        /// <returns></returns>
        public List<EquipmentImageModel> GetEquipmentImageRecords(long customerId, long modelId)
        {
            List<EquipmentImageModel> returnList = new List<EquipmentImageModel>();

            // Filter by customer + model + compart type
            returnList = GetEquipmentImagesByCustomerModel(customerId, modelId);
            if ((returnList != null) && (returnList.Count > 0))
            {
                return returnList;
            }

            // Filter by customer + compart type
            returnList = GetEquipmentImagesByCustomer(customerId);
            if ((returnList != null) && (returnList.Count > 0))
            {
                return returnList;
            }

            // Filter by model + compart type
            returnList = GetEquipmentImagesByModel(modelId);
            if ((returnList != null) && (returnList.Count > 0))
            {
                return returnList;
            }

            return returnList;
        }

        public List<EquipmentImageModel> GetEquipmentImagesByModel(long modelId)
        {
            List<EquipmentImageModel> returnList = new List<EquipmentImageModel>();
            var records = _context.CUSTOMER_MODEL_MANDATORY_IMAGE
                .Where(b => b.ModelId == modelId && b.RecordStatus == 0)
                .OrderBy(b => b.Order).ToList();
            if (records == null)
                return null;

            foreach (var record in records)
            {
                returnList.Add(new EquipmentImageModel
                {
                    title = record.Title,
                    number_of_image = record.DefaultNumberOfImages,
                    customer_model_mandatory_image_id = record.Id
                });
            }

            return returnList;
        }

        public List<EquipmentImageModel> GetEquipmentImagesByCustomer(long customerId)
        {
            List<EquipmentImageModel> returnList = new List<EquipmentImageModel>();
            var records = _context.CUSTOMER_MODEL_MANDATORY_IMAGE
                .Where(b => b.CustomerId == customerId && b.RecordStatus == 0)
                .OrderBy(b => b.Order).ToList();
            if (records == null)
                return null;

            foreach (var record in records)
            {
                returnList.Add(new EquipmentImageModel
                {
                    title = record.Title,
                    number_of_image = record.DefaultNumberOfImages,
                    customer_model_mandatory_image_id = record.Id
                });
            }

            return returnList;
        }

        public List<EquipmentImageModel> GetEquipmentImagesByCustomerModel(long customerId, long modelId)
        {
            List<EquipmentImageModel> returnList = new List<EquipmentImageModel>();
            var records = _context.CUSTOMER_MODEL_MANDATORY_IMAGE
                .Where(b => b.CustomerId == customerId && b.ModelId == modelId && b.RecordStatus == 0)
                .OrderBy(b => b.Order).ToList();
            if (records == null)
                return null;

            foreach (var record in records)
            {
                returnList.Add(new EquipmentImageModel
                {
                    title = record.Title,
                    number_of_image = record.DefaultNumberOfImages,
                    customer_model_mandatory_image_id = record.Id
                });
            }

            return returnList;
        }
    }
}
using BLL.Core.Domain;
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
    public class MiningShovelMobileSyncManager
    {
        private UndercarriageContext _context;
        private int _InspectionAuto = 0;
        private InspectionImpact _Impact = new InspectionImpact();
        private char _overallEval = 'A';

        private DbSet<DAL.TRACK_INSPECTION> _TRACK_INSPECTION;
        private DbSet<DAL.TEMP_UPLOAD_IMAGE> _TEMP_UPLOAD_IMAGE;
        private DbSet<DAL.INSPECTION_MANDATORY_IMAGES> _INSPECTION_MANDATORY_IMAGES;
        private DbSet<DAL.INSPECTION_COMPARTTYPE_RECORD> _INSPECTION_COMPARTTYPE_RECORD;
        private DbSet<DAL.INSPECTION_COMPARTTYPE_RECORD_IMAGES> _INSPECTION_COMPARTTYPE_RECORD_IMAGES;
        private DbSet<DAL.INSPECTION_COMPARTTYPE_IMAGES> _INSPECTION_COMPARTTYPE_IMAGES;
        private DbSet<DAL.TRACK_INSPECTION_DETAIL> _TRACK_INSPECTION_DETAIL;
        private DbSet<DAL.MEASUREMENT_POINT_RECORD> _MEASUREMENT_POINT_RECORD;
        private DbSet<DAL.MEASUREPOINT_RECORD_IMAGES> _MEASUREPOINT_RECORD_IMAGES;

        public class WornPercentage
        {
            public WornPercentage(int tool_auto, decimal reading, decimal wornPercentage, char evalCode)
            {
                this.tool_auto = tool_auto;
                this.reading = reading;
                this.wornPercentage = wornPercentage;
                this.evalCode = evalCode;
            }

            public int tool_auto { get; set; } = -1;
            public decimal reading { get; set; } = 0;
            public decimal wornPercentage { get; set; } = 0;
            public char evalCode { get; set; } = 'U';
        }

        private WornPercentage CompareWornPercentage(WornPercentage oldObj, WornPercentage newObj)
        {
            if (newObj.wornPercentage > oldObj.wornPercentage)
                return newObj;
            else
                return oldObj;
        }

        public MiningShovelMobileSyncManager(UndercarriageContext context)
        {
            this._context = context;
            this._TRACK_INSPECTION = _context.Set<DAL.TRACK_INSPECTION>();
            this._TEMP_UPLOAD_IMAGE = _context.Set<DAL.TEMP_UPLOAD_IMAGE>();
            this._INSPECTION_MANDATORY_IMAGES = _context.Set<DAL.INSPECTION_MANDATORY_IMAGES>();
            this._INSPECTION_COMPARTTYPE_RECORD = _context.Set<DAL.INSPECTION_COMPARTTYPE_RECORD>();
            this._INSPECTION_COMPARTTYPE_RECORD_IMAGES = _context.Set<DAL.INSPECTION_COMPARTTYPE_RECORD_IMAGES>();
            this._INSPECTION_COMPARTTYPE_IMAGES = _context.Set<DAL.INSPECTION_COMPARTTYPE_IMAGES>();
            this._TRACK_INSPECTION_DETAIL = _context.Set<DAL.TRACK_INSPECTION_DETAIL>();
            this._MEASUREMENT_POINT_RECORD = _context.Set<DAL.MEASUREMENT_POINT_RECORD>();
            this._MEASUREPOINT_RECORD_IMAGES = _context.Set<DAL.MEASUREPOINT_RECORD_IMAGES>();
        }

        public Boolean SaveImage(SyncImage Image)
        {
            TEMP_UPLOAD_IMAGE tempUploadImg = new TEMP_UPLOAD_IMAGE();
            int ExistingImg = tempUploadImg.CheckRecordExist(Image.UploadInspectionId, Image.FileName);
            if (ExistingImg > 0)
                // No need to upload
                return false;
            try
            {
                ///////////////////////////
                // TEMP_UPLOAD_IMAGE table
                DAL.TEMP_UPLOAD_IMAGE record = new DAL.TEMP_UPLOAD_IMAGE();
                record.UploadInspectionId = Image.UploadInspectionId;
                record.Title = Image.Title;
                record.Comment = Image.Comment;
                record.FileName = Image.FileName;
                record.Data = Convert.FromBase64String(Image.Data);
                record.UploadDate = DateTime.Now;

                // COMMIT
                _TEMP_UPLOAD_IMAGE.Add(record);
                _context.SaveChanges();

            }
            catch (Exception e)
            {
                // Sync failed, roll back
                return false;
            }

            return true;
        }

        private InspectionImpact GetInspectionImpact(short value)
        {
            if (value == 0)
            {
                return InspectionImpact.Low;
            } else if (value == 1)
            {
                return InspectionImpact.Normal;
            } else if (value == 2)
            {
                return InspectionImpact.High;
            }
            return InspectionImpact.Low;
        }

        public Boolean SaveMiningShovelInfo(SyncModel equip)
        {
            try
            {
                _InspectionAuto = equip.serverInspectionId;
                _Impact = GetInspectionImpact(equip.impact);

                /////////////////////////
                // _TRACK_INSPECTION
                UpdateTrackInspection(equip);

                ///////////////////////////////////////
                // _INSPECTION_MANDATORY_IMAGES table
                foreach (var item in equip.EquipmentImages)
                {
                    // Equipment photos
                    SaveEquipmentImages(item);
                }

                ///////////////////////////////////////
                // _INSPECTION_MANDATORY_IMAGES table
                foreach (var item in equip.JobsiteImages)
                {
                    // Jobsite photos
                    SaveJobsiteImages(item);
                }

                ////////////////////////////////////////////////////////////////////////////////////
                // INSPECTION_COMPARTTYPE_RECORD, INSPECTION_COMPARTTYPE_RECORD_IMAGES tables
                foreach (var item in equip.AdditionalImages)
                {
                    // Additional photos
                    SaveAdditionalImages(item);
                }

                ///////////////////////////////////////
                // _INSPECTION_COMPARTTYPE_IMAGES table
                foreach (var item in equip.MandatoryImages)
                {
                    // Equipment photos
                    SaveMandatoryImages(item);
                }

                ////////////////////////////////////////////////////////////////////////////////////
                // TRACK_INSPECTION_DETAIL, MEASUREMENT_POINT_RECORD, MEASUREPOINT_RECORD_IMAGES
                foreach (var item in equip.InspectionDetails)
                {
                    // Inspection Detail
                    SaveInspectionDetails(item);
                }

                // COMMIT
                _context.SaveChanges();

                ///////////////////////////////////////////////////////////////
                // Update eval code for equipment in TRACK_INSPECTION table
                WornPercentage equipWorn = GetEquipmentWorn(_InspectionAuto);
                if (equipWorn != null && equipWorn.wornPercentage != 0 && !equipWorn.evalCode.Equals('U'))
                {
                    var inspectionRecord =
                        _context.TRACK_INSPECTION.Where(m => m.inspection_auto == _InspectionAuto).FirstOrDefault();
                    if (inspectionRecord == null)
                        return false;

                    inspectionRecord.evalcode = equipWorn.evalCode.ToString();
                    _context.SaveChanges();
                }
            }
            catch (Exception e)
            {
                // Sync failed, roll back
                //RollbackWSREInfo();
                return false;
            }

            return true;
        }

        public Boolean UpdateTrackInspection(SyncModel equip)
        {
            DAL.TRACK_INSPECTION record = new DAL.TRACK_INSPECTION();
            record.LeftShoeNo = equip.leftShoeNo;
            record.RightShoeNo = equip.rightShoeNo;

            var inspectionRecord =
                _context.TRACK_INSPECTION.Where(m => m.inspection_auto == _InspectionAuto).FirstOrDefault();
            if (inspectionRecord == null)
                return false;

            inspectionRecord.LeftShoeNo = equip.leftShoeNo;
            inspectionRecord.RightShoeNo = equip.rightShoeNo;
            _context.SaveChanges();

            return true;
        }

        public Boolean SaveEquipmentImages(UploadImage item)
        {
            DAL.INSPECTION_MANDATORY_IMAGES record = new DAL.INSPECTION_MANDATORY_IMAGES();

            // FK ID
            record.CustomerModelMandatoryImageId = item.ServerId;

            // FK ID
            record.InspectionId = _InspectionAuto;

            // FileName
            byte[] image = GetUploadImageData(item.ImageFileName);
            record.Data = image;

            // Comment
            record.Comment = item.ImageComment;

            // Title
            record.Title = item.ImageTitle;

            // Side
            record.Side = 0;

            // INSERT
            _INSPECTION_MANDATORY_IMAGES.Add(record);

            return true;
        }

        public Boolean SaveJobsiteImages(JobsiteImage item)
        {
            DAL.INSPECTION_MANDATORY_IMAGES record = new DAL.INSPECTION_MANDATORY_IMAGES();

            // FK ID
            CUSTOMER_MODEL_MANDATORY_IMAGE serverTbl = new CUSTOMER_MODEL_MANDATORY_IMAGE();
            int Id = serverTbl.GetIdByTitle(item.ImageTitle);
            record.CustomerModelMandatoryImageId = Id;

            // FK ID
            record.InspectionId = _InspectionAuto;

            // ID
            record.InspectionId = _InspectionAuto;

            // FileName
            byte[] image = GetUploadImageData(item.ImageFileName);
            record.Data = image;

            // Comment
            record.Comment = item.ImageComment;

            // Title
            record.Title = item.ImageTitle;

            // INSERT
            _INSPECTION_MANDATORY_IMAGES.Add(record);

            return true;
        }

        public Boolean SaveAdditionalImages(AdditionalImage item)
        {
            ////////////////////////////////////
            // INSPECTION_COMPARTTYPE_RECORD
            DAL.INSPECTION_COMPARTTYPE_RECORD record = new DAL.INSPECTION_COMPARTTYPE_RECORD();

            // FK ID
            record.CompartTypeAdditionalId = item.ServerId;

            // FK ID
            record.InspectionId = _InspectionAuto;

            // MeasureNumber
            record.MeasureNumber = 1;

            // ToolId
            TrackTool tool = new TrackTool();
            int Id = -1;
            if (item.ToolCode.ToLower().Equals("observation"))
            {       
                Id = tool.GetIdByToolCode("OB");
            } else if (item.ToolCode.ToLower().Equals("yes/no"))
            {
                Id = tool.GetIdByToolCode("YES/NO");
            } else {
                Id = tool.GetIdByToolCode("R");
            }
            record.ToolId = Id;

            // Reading
            if (item.ToolCode.ToLower().Equals("observation"))
            {
                record.ObservationNote = item.Reading;
            }
            else
            {
                record.Reading = System.Convert.ToDecimal(item.Reading);
            }

            // Side
            record.Side = item.Side;

            // INSERT
            _INSPECTION_COMPARTTYPE_RECORD.Add(record);
            _context.SaveChanges();

            //////////////////
            // Image record
            if (item.ImageFileName == null || item.ImageFileName.Equals(""))
                return true;

            DAL.INSPECTION_COMPARTTYPE_RECORD_IMAGES recordImg = new DAL.INSPECTION_COMPARTTYPE_RECORD_IMAGES();

            // FK
            recordImg.RecordId = record.Id;

            // FileName
            byte[] image = GetUploadImageData(item.ImageFileName);
            recordImg.Data = image;

            // Comment
            recordImg.Comment = item.ImageComment;

            // Title
            recordImg.Title = item.ImageTitle;

            // INSERT
            _INSPECTION_COMPARTTYPE_RECORD_IMAGES.Add(recordImg);

            return true;
        }

        public Boolean SaveMandatoryImages(AdditionalImage item)
        {
            ////////////////////////////////////
            // INSPECTION_COMPARTTYPE_IMAGES
            DAL.INSPECTION_COMPARTTYPE_IMAGES record = new DAL.INSPECTION_COMPARTTYPE_IMAGES();

            // FK ID
            record.CompartTypeMandatoryImageId = item.ServerId;

            // FK ID
            record.InspectionId = _InspectionAuto;

            // Side
            record.Side = item.Side;

            // FileName
            if (item.ImageFileName != null && !item.ImageFileName.Equals(""))
            {
                byte[] image = GetUploadImageData(item.ImageFileName);
                record.Data = image;
            }

            // Comment
            record.Comment = item.ImageComment;

            // Title
            record.Title = item.ImageTitle;

            // INSERT
            _INSPECTION_COMPARTTYPE_IMAGES.Add(record);

            return true;
        }

        public Boolean SaveInspectionDetails(InspectionDetail item)
        {
            ////////////////////////////////////
            // Update TRACK_INSPECTION_DETAIL
            DAL.TRACK_INSPECTION_DETAIL recordInspectDetail = new DAL.TRACK_INSPECTION_DETAIL();
            var cmpntd = new Component(_context, longNullableToint(item.EqunitAuto));
            var component = _context.GENERAL_EQ_UNIT.Where(m => m.equnit_auto == item.EqunitAuto).FirstOrDefault();
            if (component == null)
                return false;

            // FK ID
            recordInspectDetail.inspection_auto = _InspectionAuto;

            // FK ID
            recordInspectDetail.track_unit_auto = item.EqunitAuto;

            // tool_auto ???
            recordInspectDetail.tool_auto = -1;

            // reading ????
            recordInspectDetail.reading = 0;

            // worn percentage ????
            recordInspectDetail.worn_percentage = 0;

            // eval_code ????
            recordInspectDetail.eval_code = "U";

            // ReadingEnteredByEval
            recordInspectDetail.ReadingEnteredByEval = false;

            // hours_on_surface
            recordInspectDetail.hours_on_surface = cmpntd.GetComponentLifeMiddleOfNewAction(DateTime.Now);

            // Side
            recordInspectDetail.Side = component.side ?? 0;

            // UCSystemId
            recordInspectDetail.UCSystemId = component.module_ucsub_auto;

            // INSERT
            _TRACK_INSPECTION_DETAIL.Add(recordInspectDetail);
            _context.SaveChanges();

            /////////////////////////////////////////////////////////
            // MEASUREMENT_POINT_RECORD, MEASUREPOINT_RECORD_IMAGES
            foreach (var measurementPoint in item.MeasurementPoints)
            {
                ///////////////////////////////
                // MEASUREMENT_POINT_RECORD
                int newMeasurePointId = 0;
                int firstMeasurePointId = 0;
                int countReading = 0;
                foreach (var reading in measurementPoint.Measures)
                {
                    newMeasurePointId = SaveInspectionReading(
                        recordInspectDetail.inspection_detail_auto,
                        measurementPoint.MeasurementPointId,
                        measurementPoint.ToolCode,
                        measurementPoint.Notes,
                        reading,
                        countReading);

                    if (countReading == 0)
                        firstMeasurePointId = newMeasurePointId;
                    countReading++;
                }

                ////////////////////////////////////////////////////////////
                // MEASUREPOINT_RECORD_IMAGES
                foreach (var image in measurementPoint.Images)
                {
                    SaveInspectionImg(
                        firstMeasurePointId,
                        image);
                }
            }
            _context.SaveChanges();

            ///////////////////////////
            // Update component worn
            WornPercentage componentWorn = GetComponentWorn(_InspectionAuto, item.EqunitAuto);
            if (componentWorn != null && componentWorn.wornPercentage != 0 && !componentWorn.evalCode.Equals('U'))
            {
                // tool_auto
                recordInspectDetail.tool_auto = componentWorn.tool_auto;

                // reading
                recordInspectDetail.reading = componentWorn.reading;

                // worn percentage
                recordInspectDetail.worn_percentage = componentWorn.wornPercentage;

                // eval_code
                recordInspectDetail.eval_code = componentWorn.evalCode.ToString();

                // Update
                //_TRACK_INSPECTION_DETAIL   .Upda.Add(recordInspectDetail);
                _context.SaveChanges();
            }

            return true;
        }

        public int SaveInspectionReading(
            int inspection_detail_auto, 
            int MeasurementPointId, 
            String ToolCode,
            String Notes,
            MeasurementPointReading reading, 
            int countReading)
        {
            ////////////////////////////////////
            // MEASUREMENT_POINT_RECORD
            DAL.MEASUREMENT_POINT_RECORD record = new DAL.MEASUREMENT_POINT_RECORD();

            // FK ID
            record.InspectionDetailId = inspection_detail_auto;

            // FK ID
            record.CompartMeasurePointId = MeasurementPointId;

            // InboardOutboard
            record.InboardOutborad = 0;

            // ToolId
            TrackTool tool = new TrackTool();
            int Id = -1;
            if (ToolCode != null)
                Id = tool.GetIdByToolCode(ToolCode);
            record.ToolId = Id;

            // Reading
            record.Reading = System.Convert.ToDecimal(reading.reading);

            // MeasureNumber
            record.MeasureNumber = reading.measureNo;

            // Worn
            BLL.Core.Domain.MiningShovelDomain.MeasurementPoint pointRecord = new MiningShovelDomain.MeasurementPoint(_context, MeasurementPointId);
            decimal worn = pointRecord.CalcWornPercentage(
                record.Reading.ConvertMMToInch(),
                record.ToolId,
                _Impact,
                MeasurementPointId
            );
            record.Worn = worn;

            // Eval code
            char charEval = record.Worn.toEvalChar();
            record.EvalCode = charEval.ToString();

            // Notes
            if (countReading == 0)
                record.Notes = Notes;

            // INSERT
            _MEASUREMENT_POINT_RECORD.Add(record);
            _context.SaveChanges();

            return record.Id;
        }

        public long SaveInspectionImg(int newMeasurePointId, UploadImage image)
        {
            ////////////////////////////////////
            // MEASUREPOINT_RECORD_IMAGES
            DAL.MEASUREPOINT_RECORD_IMAGES record = new DAL.MEASUREPOINT_RECORD_IMAGES();

            // FK ID
            record.MeasurePointRecordId = newMeasurePointId;

            // FileName
            byte[] imageData = GetUploadImageData(image.ImageFileName);
            record.Data = imageData;

            // Comment
            record.Comment = image.ImageComment;

            // Title
            record.Title = image.ImageTitle;

            // INSERT
            _MEASUREPOINT_RECORD_IMAGES.Add(record);
            _context.SaveChanges();

            return record.Id;
        }

        public WornPercentage GetComponentWorn(long inspectionId, long eqUnitAuto)
        {
            WornPercentage worstPointWorn = new WornPercentage(-1, 0, 0, 'U');

            // GENERAL_EQ_UNIT
            var cmpntd = new Component(_context, longNullableToint(eqUnitAuto));
            var component = _context.GENERAL_EQ_UNIT.Where(m => m.equnit_auto == eqUnitAuto).FirstOrDefault();
            if (component == null)
            {
                return null;
            }

            // TRACK_INSPECTION_DETAIL
            var inspectionDetail = 
                _context.TRACK_INSPECTION_DETAIL.Where(m => m.inspection_auto == inspectionId && m.track_unit_auto == eqUnitAuto).FirstOrDefault();
            if (inspectionDetail == null)
                return null;

            // COMPART_MEASUREMENT_POINT
            var compartMeasureRecords =
                _context.COMPART_MEASUREMENT_POINT.Where(m => m.CompartId == component.compartid_auto).ToList();
            if (compartMeasureRecords == null)
                return null;

            int countPoint = 0;
            foreach (var point in compartMeasureRecords)
            {
                // Readings (MEASUREMENT_POINT_RECORD)
                List<MEASUREMENT_POINT_RECORD> readingGroup =
                    _context.MEASUREMENT_POINT_RECORD.Where(
                        m => m.InspectionDetailId == inspectionDetail.inspection_detail_auto
                        && m.CompartMeasurePointId == point.Id
                        && m.ToolId != -1 && m.ToolId != 5 && m.ToolId != 7 && m.ToolId != 8    // invalid, kpo, yesno, observation
                    ).ToList();
                if (readingGroup == null || readingGroup.Count == 0)
                    continue;

                WornPercentage pointWorn = GetMeasurementPointWorn(readingGroup, point.Id, inspectionDetail.inspection_detail_auto);
                if (pointWorn == null) continue;

                // Get worst worn
                if (countPoint == 0)
                    worstPointWorn = pointWorn;
                else
                    worstPointWorn = CompareWornPercentage(worstPointWorn, pointWorn);

                // Reset
                countPoint++;
            }

            // Component worn = Worst measurement point
            return worstPointWorn;
        }

        private WornPercentage GetMeasurementPointWorn(List<MEASUREMENT_POINT_RECORD> readingGroup, int pointId, int inspectionDetailId)
        {
            // Average reading
            decimal sumReading = 0;
            var countReading = 0;
            decimal average = 0;
            int toolId = -1;
            decimal totalWorn = 0;
            decimal averageWorn = 0;
            BLL.Core.Domain.MiningShovelDomain.MeasurementPoint pointRecord = new MiningShovelDomain.MeasurementPoint(_context, pointId);
            foreach (var reading in readingGroup)
            {
                countReading++;
                sumReading = sumReading + reading.Reading;
                toolId = reading.ToolId;                
                totalWorn += pointRecord.CalcWornPercentage(
                    reading.Reading.ConvertMMToInch(),
                    toolId,
                    _Impact, 
                    pointId
                );
            }
            if (countReading > 0)
            {
                average = sumReading / countReading;
                averageWorn = totalWorn / countReading;
            }
            else return null;

            return new WornPercentage(toolId, average, averageWorn, averageWorn.toEvalChar());
        }

        public WornPercentage GetEquipmentWorn(long inspectionId)
        {
            // TRACK_INSPECTION_DETAIL
            var inspectionDetails =
                _context.TRACK_INSPECTION_DETAIL
                .Where(m => m.inspection_auto == inspectionId)
                .Where(m => m.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.comparttype == "Link") // Only take %worn from the link for rope shovel inspections
                .ToList();
            if (inspectionDetails == null)
                return null;

            WornPercentage worstComponentWorn = new WornPercentage(-1, 0, 0, 'U');
            int count = 0;
            foreach(var inspectionDetail in inspectionDetails)
            {
                int tool = -1;
                if (inspectionDetail.tool_auto.HasValue)
                    tool = inspectionDetail.tool_auto.Value;

                WornPercentage componentWorn = new WornPercentage(
                    tool, inspectionDetail.reading, inspectionDetail.worn_percentage, inspectionDetail.eval_code[0]);

                if (count == 0)
                    worstComponentWorn = componentWorn;
                else
                    worstComponentWorn = CompareWornPercentage(worstComponentWorn, componentWorn);
                
                // Reset
                count++;
            }

            return worstComponentWorn;
        }

        private byte[] GetUploadImageData(string fileName)
        {
            if (fileName == null || fileName.Equals(""))
                return null;

            var record = _context.TEMP_UPLOAD_IMAGE
                .Where(b => b.FileName == fileName && b.UploadInspectionId == _InspectionAuto).FirstOrDefault();
            if (record == null)
                return null;

            // Get raw data
            byte[] data = record.Data;

            // Delete image
            try
            {
                _context.TEMP_UPLOAD_IMAGE.Remove(record);
                _context.SaveChanges();
            }
            catch
            {
                // Errored and couldn't delete the image.
            }

            return data;
        }

        private int longNullableToint(long? number)
        {
            if (number == null)
                return 0;
            if (number > Int32.MaxValue) //:) So Stupid if the number is bigger 
                return Int32.MaxValue;
            if (number < Int32.MinValue) // :))
                return Int32.MinValue;
            try { return (int)number; } catch { return 0; }
        }

        private void removeTempImage()
        {
            var records = _context.TEMP_UPLOAD_IMAGE
                .Where(b => b.UploadInspectionId == _InspectionAuto).ToList();
            if (records == null)
                return;

            _context.TEMP_UPLOAD_IMAGE.RemoveRange(records);
            _context.SaveChanges();
        }
    }
}
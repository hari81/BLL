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
    public class WSREMobileManager
    {
        private UndercarriageContext _context;
        private DbSet<DAL.WSRE> _WSREs;
        private DbSet<DAL.WSREInitialImage> _WSREInitialImages;
        private DbSet<DAL.WSREComponentRecord> _WSREComponentRecords;
        private DbSet<DAL.WSREComponentImage> _WSREComponentImages;
        private DbSet<DAL.WSREComponentRecordRecommendation> _WSREComponentRecordRecommendations;
        private DbSet<DAL.WSRECrackTest> _WSRECrackTests;
        private DbSet<DAL.WSRECrackTestImage> _WSRECrackTestImages;
        private DbSet<DAL.WSREDipTest> _WSREDipTests;
        private DbSet<DAL.WSREDipTestImage> _WSREDipTestImages;
        private DbSet<DAL.WSRE_TEMP_UPLOAD_IMAGE> _WSRE_TEMP_UPLOAD_IMAGE;

        private int _WSREId = 0;
        private char _overallEval = 'A';
        private List<long> _arrComponentRecordId = new List<long>();
        private List<long> _arrCrackTestId = new List<long>();
        private List<long> _arrDipTestId = new List<long>();

        public WSREMobileManager(UndercarriageContext context)
        {
            this._context = context;
            this._WSREs = _context.Set<DAL.WSRE>();
            this._WSREInitialImages = _context.Set<DAL.WSREInitialImage>();
            this._WSREComponentRecords = _context.Set<DAL.WSREComponentRecord>();
            this._WSREComponentImages = _context.Set<DAL.WSREComponentImage>();
            this._WSREComponentRecordRecommendations = _context.Set<DAL.WSREComponentRecordRecommendation>();
            this._WSRECrackTests = _context.Set<DAL.WSRECrackTest>();
            this._WSRECrackTestImages = _context.Set<DAL.WSRECrackTestImage>();
            this._WSREDipTests = _context.Set<DAL.WSREDipTest>();
            this._WSREDipTestImages = _context.Set<DAL.WSREDipTestImage>();
            this._WSRE_TEMP_UPLOAD_IMAGE = _context.Set<DAL.WSRE_TEMP_UPLOAD_IMAGE>();
        }

        /// <summary>
        /// SYNC
        /// </summary>
        private int GetEvalNumber(char eval)
        {
            int evalNumber = 0;
            if (eval == 'A')
                evalNumber = 1;
            else if (eval == 'B')
                evalNumber = 2;
            else if (eval == 'C')
                evalNumber = 3;
            else if (eval == 'X')
                evalNumber = 4;

            return evalNumber;
        }

        private char GetOverallEval(char componentEval, char overallEval)
        {
            int intComponentEval = GetEvalNumber(componentEval);
            int intOverallEval = GetEvalNumber(overallEval);
            if (intComponentEval > intOverallEval)
                return componentEval;
            else
                return overallEval;
        }

        public long InsertWSREInspectionRecord(WRESSyncModel equip)
        {
            try
            {
                BLL.Core.Domain.TTDevUser user = new BLL.Core.Domain.TTDevUser(new DAL.SharedContext(), 0);

                //////////////////
                // WSREs table
                DAL.WSRE WSREsRecord = new DAL.WSRE();
                WSREsRecord.Date = DateTime.Now;
                WSREsRecord.SystemId = equip.SystemId;
                WSREsRecord.JobsiteId = equip.JobsiteId;
                WSREsRecord.InspectorId = user.GetUserAutoById(equip.InspectorId);
                WSREsRecord.JobNumber = equip.JobNumber;
                WSREsRecord.OldTagNumber = equip.OldTagNumber;
                WSREsRecord.OverallComment = equip.OverallComment;
                WSREsRecord.OverallRecommendation = equip.OverallRecommendation;
                WSREsRecord.StatusId = 1;
                WSREsRecord.CustomerReference = equip.CustomerReference;
                WSREsRecord.SystemLife = new BLL.Core.Domain.UCSystem(new DAL.UndercarriageContext(), (int)WSREsRecord.SystemId).GetSystemLife(DateTime.Now);
                _WSREs.Add(WSREsRecord);

                // COMMIT
                _context.SaveChanges();
                _WSREId = WSREsRecord.Id;
            }
            catch (Exception e)
            {
                // Sync failed, roll back
                RollbackWSREInfo();
                return 0;
            }

            return _WSREId;
        }

        public Boolean SaveImage(SyncImage Image)
        {
            WSRE_TEMP_UPLOAD_IMAGE tempUploadImg = new WSRE_TEMP_UPLOAD_IMAGE();
            int ExistingImg = tempUploadImg.CheckRecordExist(Image.UploadInspectionId, Image.FileName);
            if (ExistingImg > 0)
                // No need to upload
                return false;
            try
            {
                ///////////////////////////
                // WSRE_TEMP_UPLOAD_IMAGE table
                DAL.WSRE_TEMP_UPLOAD_IMAGE record = new DAL.WSRE_TEMP_UPLOAD_IMAGE();
                record.UploadInspectionId = Image.UploadInspectionId;
                record.Title = Image.Title;
                record.Comment = Image.Comment;
                record.FileName = Image.FileName;
                record.Data = Convert.FromBase64String(Image.Data);
                record.UploadDate = DateTime.Now;

                // COMMIT
                _WSRE_TEMP_UPLOAD_IMAGE.Add(record);
                _context.SaveChanges();

            }
            catch (Exception e)
            {
                // Sync failed, roll back
                return false;
            }

            return true;
        }

        public Boolean SaveWSREInfo(WRESSyncModel equip)
        {
            try
            {
                BLL.Core.Domain.TTDevUser user = new BLL.Core.Domain.TTDevUser(new DAL.SharedContext(), 0);

                ////////////////////
                //// WSREs table
                //DAL.WSRE WSREsRecord = new DAL.WSRE();
                //WSREsRecord.Date = DateTime.Now;
                //WSREsRecord.SystemId = equip.SystemId;
                //WSREsRecord.JobsiteId = equip.JobsiteId;
                //WSREsRecord.InspectorId = user.GetUserAutoById(equip.InspectorId);
                //WSREsRecord.JobNumber = equip.JobNumber;
                //WSREsRecord.OldTagNumber = equip.OldTagNumber;
                //WSREsRecord.OverallComment = equip.OverallComment;
                //WSREsRecord.OverallRecommendation = equip.OverallRecommendation;
                //WSREsRecord.StatusId = 1;
                //WSREsRecord.CustomerReference = equip.CustomerReference;
                //WSREsRecord.SystemLife = new BLL.Core.Domain.UCSystem(new DAL.UndercarriageContext(), (int)WSREsRecord.SystemId).GetSystemLife(DateTime.Now);
                //_WSREs.Add(WSREsRecord);

                //// COMMIT
                //_context.SaveChanges();
                //_WSREId = WSREsRecord.Id;

                _WSREId = equip.serverInspectionId;

                /////////////////////////////
                // WSREInitialImages table
                foreach (var item in equip.InitialImages)
                {
                    DAL.WSREInitialImage record = new DAL.WSREInitialImage();
                    record.WSREId = _WSREId;
                    SaveWSREInitialImages(record, item);
                }

                //////////////////////////////////
                // WSREComponentRecords table
                foreach (var item in equip.ComponentRecords)
                {
                    DAL.WSREComponentRecord record = new DAL.WSREComponentRecord();
                    record.WSREId = _WSREId;
                    SaveWSREComponentRecords(record, item);
                }

                //////////////////////////////////
                // WSRECrackTest table
                DAL.WSRECrackTest recordCrack = new DAL.WSRECrackTest();
                recordCrack.WSREId = _WSREId;
                SaveWSRECrackTestsRecords(recordCrack, equip);

                //////////////////////////////
                // DipTests tables
                foreach (var item in equip.DipTestRecords)
                {
                    DAL.WSREDipTest recordDipTest = new DAL.WSREDipTest();
                    recordDipTest.WSREId = _WSREId;
                    SaveWSREDipTestsRecords(recordDipTest, item);
                }

                // COMMIT
                _context.SaveChanges();

                /////////////////////////
                // Update "OverallEval"
                UpdateOverallEval();

            }
            catch (Exception e)
            {
                // Sync failed, roll back
                RollbackWSREInfo();
                return false;
            }
            
            return true;
        }

        public Boolean SaveWSREInitialImages(
            DAL.WSREInitialImage record, 
            WSREImage item)
        {
            // WSREId
            //record.WSREId = WSREsRecord.Id;

            // ImageTypeId
            WSREInitialImageType initialImg = new WSREInitialImageType();
            int Id = initialImg.GetIdByDescr(item.ImageTypeDescription);
            record.ImageTypeId = Id;

            // Data
            byte[] image = GetUploadImageData(item.Data);
            record.Data = image;

            // Title
            record.Title = item.ImageTitle;

            // Comment
            record.Comment = item.ImageComment;

            // INSERT
            _WSREInitialImages.Add(record);

            return true;
        }

        public Boolean SaveWSREComponentRecords(
            DAL.WSREComponentRecord record, 
            WSREComponentRecordModel item)
        {
            // Comment
            record.Comment = item.Comment;

            // ComponentId
            record.ComponentId = item.ComponentId;

            // Measurement
            record.Measurement = item.Measurement;

            // MeasurementToolId
            TrackTool tool = new TrackTool();
            int Id = tool.GetIdByToolCode(item.MeasurementToolId);
            record.MeasurementToolId = Id;

            // WornPercentage
            var component = new BLL.Core.Domain.Component(new UndercarriageContext(), Convert.ToInt32(record.ComponentId));
            record.WornPercentage = component.CalcWornPercentage(record.Measurement.ConvertMMToInch(), record.MeasurementToolId, InspectionImpact.High);
            char charEval = record.WornPercentage.toEvalChar();
            _overallEval = GetOverallEval(charEval, _overallEval);

            // INSERT
            _WSREComponentRecords.Add(record);
            _context.SaveChanges();
            _arrComponentRecordId.Add(record.Id);

            //////////////////////////////////
            // WSREComponentImages table
            foreach (var image in item.Images)
            {
                // ComponentrecordId
                DAL.WSREComponentImage imgRecord = new DAL.WSREComponentImage();
                imgRecord.ComponentRecordId = record.Id;

                // Data
                byte[] imageData = GetUploadImageData(image.Data);
                imgRecord.Data = imageData;

                // Title
                imgRecord.Title = image.ImageTitle;

                // Comment
                imgRecord.Comment = image.ImageComment;

                // IncludeInReport
                imgRecord.IncludeInReport = true;

                // Deleted
                imgRecord.Deleted = false;

                // INSERT
                _WSREComponentImages.Add(imgRecord);
            }

            ///////////////////////////////////////////////
            // WSREComponentRecordRecommendations table
            foreach (var recommend in item.RecommendationId)
            {
                DAL.WSREComponentRecordRecommendation recommendRecord = new DAL.WSREComponentRecordRecommendation();
                recommendRecord.ComponentRecordId = record.Id;
                recommendRecord.RecommendationId = recommend;

                // INSERT
                _WSREComponentRecordRecommendations.Add(recommendRecord);
            }

            return true;
        }

        public Boolean SaveWSRECrackTestsRecords(
            DAL.WSRECrackTest record,
            WRESSyncModel equip)
        {
            //////////////////////////////////
            // WSRECrackTest table

            if (equip.CrackTests_TestPassed == 0)
                record.TestPassed = false;
            else
                record.TestPassed = true;
            record.Comment = equip.CrackTests_Comment;

            // INSERT
            _WSRECrackTests.Add(record);
            _context.SaveChanges();
            _arrCrackTestId.Add(record.Id);

            //////////////////////////////////
            // WSRECrackTestImages table
            foreach (var crackImg in equip.CrackTestImages)
            {
                DAL.WSRECrackTestImage recordCrackImg = new DAL.WSRECrackTestImage();
                recordCrackImg.CrackTestId = record.Id;
                byte[] imageData = GetUploadImageData(crackImg.Data);
                recordCrackImg.Data = imageData;
                recordCrackImg.Title = crackImg.ImageTitle;
                recordCrackImg.Comment = crackImg.ImageComment;
                recordCrackImg.IncludeInReport = true;
                recordCrackImg.Deleted = false;

                // INSERT
                _WSRECrackTestImages.Add(recordCrackImg);
            }

            return true;
        }

        public Boolean SaveWSREDipTestsRecords(
            DAL.WSREDipTest record,
            WSREDiptestModel item)
        {
            //////////////////////////////////
            // WSREDipTests table
            record.Measurement = item.Measurement;

            // ConditionId
            record.ConditionId = item.ConditionId;

            // Comment
            record.Comment = item.Comment;

            // Recommendation
            record.Recommendation = item.Recommendation;

            // Number
            record.Number = item.Number;

            // INSERT
            _WSREDipTests.Add(record);
            _context.SaveChanges();
            _arrDipTestId.Add(record.Id);

            //////////////////////////////////
            // WSREDipTestImages table
            foreach (var img in item.Images)
            {
                DAL.WSREDipTestImage recordImg = new DAL.WSREDipTestImage();
                recordImg.DipTestId = record.Id;
                byte[] imageData = GetUploadImageData(img.Data);
                recordImg.Data = imageData;
                recordImg.Title = img.ImageTitle;
                recordImg.Comment = img.ImageComment;
                recordImg.IncludeInReport = true;
                recordImg.Deleted = false;

                // INSERT
                _WSREDipTestImages.Add(recordImg);
            }

            return true;
        }

        public void UpdateOverallEval()
        {
            try
            {
                var result = _WSREs.SingleOrDefault(wsre => wsre.Id == _WSREId);
                if (result != null)
                {
                    result.OverallEval = _overallEval.ToString();
                    _context.SaveChanges();
                }
            } catch (Exception ex)
            {

            }
        }

        public Boolean RollbackWSREInfo()
        {
            ///////////////
            // Dip Tests
            int noOfRowDeleted = 0;
            for (int i = 0; i < _arrDipTestId.Count; i++)
            {
                // Rollback WSREDipTestImages
                noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                    "delete from WSREDipTestImages where DipTestId = @DipTestId", new SqlParameter("@DipTestId", _arrDipTestId[i]));
            }

            // Rollback WSREDipTests
            noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                "delete from WSREDipTests where WSREId = @WSREId", new SqlParameter("@WSREId", _WSREId));

            /////////////////
            // Crack Tests
            for (int i = 0; i < _arrCrackTestId.Count; i++)
            {
                // Rollback WSRECrackTestImages
                noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                    "delete from WSRECrackTestImages where CrackTestId = @CrackTestId", new SqlParameter("@CrackTestId", _arrCrackTestId[i]));
            }

            // Rollback WSRECrackTests
            noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                "delete from WSRECrackTests where WSREId = @WSREId", new SqlParameter("@WSREId", _WSREId));

            ////////////////////
            // Component Tests
            for (int i = 0; i < _arrComponentRecordId.Count; i++)
            {
                // Rollback WSREComponentRecordRecommendations
                noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                    "delete from WSREComponentRecordRecommendations where ComponentRecordId = @ComponentRecordId", new SqlParameter("@ComponentRecordId", _arrComponentRecordId[i]));

                // Rollback WSREComponentImages
                noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                    "delete from WSREComponentImages where ComponentRecordId = @ComponentRecordId", new SqlParameter("@ComponentRecordId", _arrComponentRecordId[i]));

            }

            // Rollback WSREComponentRecords
            noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                "delete from WSREComponentRecords where WSREId = @WSREId", new SqlParameter("@WSREId", _WSREId));

            ////////////
            // WSREs
            // Rollback WSREInitialImages
            noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                "delete from WSREInitialImages where WSREId = @WSREId", new SqlParameter("@WSREId", _WSREId));

            // Rollback WSREs
            noOfRowDeleted = _context.Database.ExecuteSqlCommand(
                "delete from WSREs where Id = @Id", new SqlParameter("@Id", _WSREId));

            return true;
        }

        /// <summary>
        /// SETTING
        /// </summary>
        /// <returns></returns>
        public String GetWSREEnableSetting()
        {
            String returnVal = "0";

            BLL.Core.Domain.AppConfigAccess ACA = new BLL.Core.Domain.AppConfigAccess();
            returnVal = ACA.GetApplicationValue("EnableWorkshopRepairEstimate");

            return returnVal;
        }

        private byte[] GetUploadImageData(string fileName)
        {
            if (fileName == null || fileName.Equals(""))
                return null;

            var record = _context.WSRE_TEMP_UPLOAD_IMAGE
                .Where(b => b.FileName == fileName && b.UploadInspectionId == _WSREId).FirstOrDefault();
            if (record == null)
                return null;

            // Get raw data
            byte[] data = record.Data;

            // Delete image
            try
            {
                _context.WSRE_TEMP_UPLOAD_IMAGE.Remove(record);
                _context.SaveChanges();
            }
            catch
            {
                // Errored and couldn't delete the image.
            }

            return data;
        }
    }
}
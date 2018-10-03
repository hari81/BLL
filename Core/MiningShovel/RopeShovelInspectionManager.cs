using BLL.Core.Actions;
using BLL.Core.Domain;
using BLL.Core.ViewModel;
using BLL.Extensions;
using BLL.Interfaces;
using BLL.Services;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.MiningShovel
{
    public class RopeShovelInspectionManager
    {
        UndercarriageContext _context;

        public RopeShovelInspectionManager(UndercarriageContext context)
        {
            _context = context;
        }

        public List<RopeShovelInspectionSearchResultModel> GetInspections(int userId, RopeShovelInspectionSearchRequestModel searchModel)
        {
            var equipment = new Core.Domain.UserAccess(new SharedContext(), userId).getAccessibleEquipments().Select(e => e.equipmentid_auto).ToList();

            return _context.TRACK_INSPECTION
                .Where(i => equipment.Contains(i.equipmentid_auto))
                .Where(i => i.EQUIPMENT.LU_MMTA.TYPE.typeid == "RSH")
                .Where(i => i.inspection_date >= searchModel.StartDate && i.inspection_date <= searchModel.EndDate)
                .Where(i => i.EQUIPMENT.Jobsite.Customer.cust_name.Contains(searchModel.CustomerName))
                .Where(i => i.EQUIPMENT.Jobsite.site_name.Contains(searchModel.JobsiteName))
                .Where(i => i.EQUIPMENT.serialno.Contains(searchModel.SerialNumber))
                .Where(i => i.EQUIPMENT.unitno.Contains(searchModel.UnitNumber))
                .Where(i => i.created_user.Contains(searchModel.InspectorName))
                .Where(i => i.ActionTakenHistory.recordStatus == (int)RecordStatus.Available)
                .Select(i => new RopeShovelInspectionSearchResultModel()
                {
                    CustomerName = i.EQUIPMENT.Jobsite.Customer.cust_name,
                    Evaluation = i.evalcode,
                    Id = i.inspection_auto,
                    InspectionDate = i.inspection_date,
                    InspectorName = i.created_user,
                    JobsiteName = i.EQUIPMENT.Jobsite.site_name,
                    SerialNumber = i.EQUIPMENT.serialno,
                    UnitNumber = i.EQUIPMENT.unitno,
                    EquipmentId = i.equipmentid_auto
                }).OrderByDescending(i => i.InspectionDate).ToList();
        }

        public MeasurementPointPhoto GetMeasurementPointPhotoThumbnail(int photoId)
        {
            var photo = _context.MEASUREPOINT_RECORD_IMAGES.Find(photoId);
            if (photo == null || photo.Data == null)
            {
                return new MeasurementPointPhoto()
                {
                    Id = -1
                };
            }

            return new MeasurementPointPhoto()
            {
                Id = photoId,
                Title = photo.Title,
                Comment = photo.Comment,
                Photo = Convert.ToBase64String(ResizeImage.GetThumbnail(photo.Data))
            };
        }

        public MeasurementPointPhoto GetMeasurementPointPhoto(int photoId)
        {
            var photo = _context.MEASUREPOINT_RECORD_IMAGES.Find(photoId);
            if (photo == null || photo.Data == null)
            {
                return new MeasurementPointPhoto()
                {
                    Id = -1
                };
            }

            return new MeasurementPointPhoto()
            {
                Id = photoId,
                Title = photo.Title,
                Comment = photo.Comment,
                Photo = Convert.ToBase64String(photo.Data)
            };
        }

        public EquipmentPhotosViewModel GetCompartMandatoryImagePhotoThumbnail(int photoId)
        {
            var photo = _context.INSPECTION_COMPARTTYPE_IMAGES.Find(photoId);
            if (photo == null || photo.Data == null)
            {
                return new EquipmentPhotosViewModel()
                {
                    Id = -1
                };
            }

            return new EquipmentPhotosViewModel()
            {
                Id = photoId,
                Title = photo.Title,
                Description = photo.Comment,
                Photo = Convert.ToBase64String(ResizeImage.Get160by120(photo.Data))
            };
        }

        public EquipmentPhotosViewModel GetCompartMandatoryImagePhoto(int photoId)
        {
            var photo = _context.INSPECTION_COMPARTTYPE_IMAGES.Find(photoId);
            if (photo == null || photo.Data == null)
            {
                return new EquipmentPhotosViewModel()
                {
                    Id = -1
                };
            }

            return new EquipmentPhotosViewModel()
            {
                Id = photoId,
                Title = photo.Title,
                Description = photo.Comment,
                Photo = Convert.ToBase64String(photo.Data)
            };
        }

        /// <summary>
        /// Updates the tramming hours for the given inspection Id. Doesn't allow a negative value for tramming hours. 
        /// </summary>
        /// <param name="inspectionId">The inspection to update</param>
        /// <param name="trammingHours">The new tramming hour value</param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> UpdateTrammingHoursAsync(int inspectionId, int trammingHours)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return Tuple.Create(false, "Cannot find an inspection with the given Id. ");
            if (trammingHours < 0)
                return Tuple.Create(false, "The tramming hours must be greater than 0. ");
            inspection.TrammingHours = trammingHours;
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Tramming hours updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the customer contact for the given inspection Id.
        /// </summary>
        /// <param name="inspectionId">The inspection to update</param>
        /// <param name="customerContact">The new customer contact</param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> UpdateCustomerContactAsync(int inspectionId, string customerContact)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return Tuple.Create(false, "Cannot find an inspection with the given Id. ");
            inspection.CustomerContact = customerContact;
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Customer contact updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the general inspection notes field for the given inspection Id.
        /// </summary>
        /// <param name="inspectionId">The inspection to update</param>
        /// <param name="generalInspectionNotes">The new general inspection notes</param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> UpdateGeneralInspectionNotesAsync(int inspectionId, string generalInspectionNotes)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return Tuple.Create(false, "Cannot find an inspection with the given Id. ");
            inspection.notes = generalInspectionNotes;
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "General inspection notes updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the impact of the given inspection Id.
        /// </summary>
        /// <param name="inspectionId">The inspection to update</param>
        /// <param name="impact">The new impact value. Can be 0, 1, 2 for Low, Medium or High. </param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> UpdateImpactAsync(int inspectionId, short impact)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return Tuple.Create(false, "Cannot find an inspection with the given Id. ");
            inspection.impact = impact;
            if (impact < 0 || impact > 2)
                return Tuple.Create(false, "Invalid impact. Value must be between 0 and 2. ");
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Impact updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the abrasive of the given inspection Id.
        /// </summary>
        /// <param name="inspectionId">The inspection to update</param>
        /// <param name="abrasive">The new abrasive value. Can be 0, 1, 2 for Low, Medium or High. </param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> UpdateAbrasiveAsync(int inspectionId, short abrasive)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return Tuple.Create(false, "Cannot find an inspection with the given Id. ");
            inspection.abrasive = abrasive;
            if (abrasive < 0 || abrasive > 2)
                return Tuple.Create(false, "Invalid abrasive. Value must be between 0 and 2. ");
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Abrasive updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the moisture of the given inspection Id.
        /// </summary>
        /// <param name="inspectionId">The inspection to update</param>
        /// <param name="moisture">The new moisture value. Can be 0, 1, 2 for Low, Medium or High. </param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> UpdateMoistureAsync(int inspectionId, short moisture)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return Tuple.Create(false, "Cannot find an inspection with the given Id. ");
            inspection.moisture = moisture;
            if (moisture < 0 || moisture > 2)
                return Tuple.Create(false, "Invalid moisture. Value must be between 0 and 2. ");
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Moisture updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the packing of the given inspection Id.
        /// </summary>
        /// <param name="inspectionId">The inspection to update</param>
        /// <param name="packing">The new packing value. Can be 0, 1, 2 for Low, Medium or High. </param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> UpdatePackingAsync(int inspectionId, short packing)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return Tuple.Create(false, "Cannot find an inspection with the given Id. ");
            inspection.packing = packing;
            if (packing < 0 || packing > 2)
                return Tuple.Create(false, "Invalid packing. Value must be between 0 and 2. ");
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Packing updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the jobsite comment for the given inspection Id.
        /// </summary>
        /// <param name="inspectionId">The inspection to update</param>
        /// <param name="jobsiteComment">The new jobsite comment. </param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> UpdateOverallJobsiteCommentAsync(int inspectionId, string jobsiteComment)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return Tuple.Create(false, "Cannot find an inspection with the given Id. ");
            inspection.Jobsite_Comms = jobsiteComment;
            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Jobsite comment updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        public async Task<Tuple<bool, string>> UploadNewMeasurementPointPhoto(int inspectionDetailId, int compartMeasurePointId, string photo)
        {
            var measurementPoint = _context.MEASUREMENT_POINT_RECORD
                .Where(i => i.InspectionDetailId == inspectionDetailId)
                .Where(i => i.CompartMeasurePointId == compartMeasurePointId)
                .Where(i => i.MeasureNumber == 1)
                .FirstOrDefault();
            if (measurementPoint == null)
            {
                var compartMeasurePoint = await _context.COMPART_MEASUREMENT_POINT.FindAsync(compartMeasurePointId);
                measurementPoint = new MEASUREMENT_POINT_RECORD()
                {
                    CompartMeasurePointId = compartMeasurePointId,
                    EvalCode = "",
                    InboardOutborad = 0,
                    InspectionDetailId = inspectionDetailId,
                    MeasureNumber = 1,
                    Notes = "",
                    Reading = 0,
                    ToolId = compartMeasurePoint.DefaultToolId != null ? (int)compartMeasurePoint.DefaultToolId : -1,
                    Worn = 0
                };

                _context.MEASUREMENT_POINT_RECORD.Add(measurementPoint);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    return Tuple.Create(false, e.ToDetailedString());
                }
            }

            var photoRecord = new MEASUREPOINT_RECORD_IMAGES()
            {
                Comment = "",
                Data = Convert.FromBase64String(photo),
                MeasurePointRecordId = measurementPoint.Id,
                Title = ""
            };
            _context.MEASUREPOINT_RECORD_IMAGES.Add(photoRecord);

            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Measurement point comment updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        public async Task<Tuple<bool, string>> UpdateMeasurementPointComment(int inspectionDetailId, int compartMeasurePointId, string comment)
        {
            var measurementPoint = _context.MEASUREMENT_POINT_RECORD
                .Where(i => i.InspectionDetailId == inspectionDetailId)
                .Where(i => i.CompartMeasurePointId == compartMeasurePointId)
                .Where(i => i.MeasureNumber == 1)
                .FirstOrDefault();
            if (measurementPoint == null)
            {
                var compartMeasurePoint = await _context.COMPART_MEASUREMENT_POINT.FindAsync(compartMeasurePointId);
                measurementPoint = new MEASUREMENT_POINT_RECORD()
                {
                    CompartMeasurePointId = compartMeasurePointId,
                    EvalCode = "",
                    InboardOutborad = 0,
                    InspectionDetailId = inspectionDetailId,
                    MeasureNumber = 1,
                    Notes = comment,
                    Reading = 0,
                    ToolId = -1,//compartMeasurePoint.DefaultToolId != null ? (int)compartMeasurePoint.DefaultToolId : -1,
                    Worn = 0
                };
                _context.MEASUREMENT_POINT_RECORD.Add(measurementPoint);
            }
            else
            {
                measurementPoint.Notes = comment;
            }

            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Measurement point comment updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        public async Task<Interfaces.IGeneralInspectionModel> GetInspectionGeneralDetails(int inspectionId)
        {
            var inspection = await _context.TRACK_INSPECTION.FindAsync(inspectionId);
            if (inspection == null)
                return null;
            return new GeneralInspectionViewModel()
            {
                Abrasive = (int)inspection.abrasive,
                CustomerContact = inspection.CustomerContact,
                Date = inspection.inspection_date,
                DryJointsLeft = (int)inspection.dry_joints_left,
                DryJointsRight = (int)inspection.dry_joints_right,
                ExtCannonLeft = (int)inspection.ext_cannon_left,
                ExtCannonRight = (int)inspection.ext_cannon_right,
                Impact = (int)inspection.impact,
                InspectionNotes = inspection.notes,
                JobSiteNotes = inspection.Jobsite_Comms,
                Life = (int)inspection.ltd,
                SMU = (int)inspection.smu,
                Moisture = (int)inspection.moisture,
                Packing = (int)inspection.packing,
                TrackSagLeft = (int)inspection.track_sag_left,
                TrackSagRight = (int)inspection.track_sag_right,
                TrammingHours = (int)inspection.TrammingHours,
                Id = inspection.inspection_auto
            };
        }

        public async Task<Tuple<bool, string>> UploadEquipmentMandatoryPhoto(MandatoryEquipmentPhotoModel p)
        {
            var photoRecord = await _context.INSPECTION_MANDATORY_IMAGES.FindAsync(p.PhotoRecordId);
            if (photoRecord != null)
            {
                try
                {
                    string[] arr = p.Photo.Split(',');
                    string newPhoto = "";
                    if (arr.Length > 1)
                        newPhoto = arr[1];

                    photoRecord.Data = Convert.FromBase64String(newPhoto);
                }
                catch (Exception e)
                {
                    return Tuple.Create(false, "Photo data was not in the correct format. ");
                }
                
            }
            else
            {
                string[] arr = p.Photo.Split(',');
                string newPhoto = "";
                if (arr.Length > 1)
                    newPhoto = arr[1];

                if (newPhoto == "")
                    return Tuple.Create(false, "Photo data didn't upload correctly. ");

                var definition = await _context.CUSTOMER_MODEL_MANDATORY_IMAGE.FindAsync(p.MandatoryImageId);
                if (definition == null)
                    return Tuple.Create(false, "Cannot find a mandatory image type with this Id. ");
                var newRecord = new INSPECTION_MANDATORY_IMAGES()
                {
                    Comment = definition.Description,
                    CustomerModelMandatoryImageId = p.MandatoryImageId,
                    Data = Convert.FromBase64String(newPhoto),
                    InspectionId = p.InspectionId,
                    Side = 0,
                    Title = definition.Title
                };
                _context.INSPECTION_MANDATORY_IMAGES.Add(newRecord);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Photo uploaded successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to upload photo");
            }
        }

        public async Task<Tuple<bool, string>> UploadCompartMandatoryPhoto(MandatoryCompartTypePhotoModel p)
        {
            var photoRecord = await _context.INSPECTION_COMPARTTYPE_IMAGES.FindAsync(p.PhotoRecordId);
            if (photoRecord != null)
            {
                photoRecord.Data = Convert.FromBase64String(p.Photo);
            }
            else
            {
                string[] arr = p.Photo.Split(',');
                string newPhoto = "";
                if (arr.Length > 1)
                    newPhoto = arr[1];

                if (newPhoto == "")
                    return Tuple.Create(false, "Photo data didn't upload correctly. ");

                var definition = await _context.CUSTOMER_MODEL_COMPARTTYPE_MANDATORY_IMAGE.FindAsync(p.MandatoryImageId);
                if (definition == null)
                    return Tuple.Create(false, "Cannot find a mandatory image type with this Id. ");
                var newRecord = new INSPECTION_COMPARTTYPE_IMAGES()
                {
                    Comment = definition.Description,
                    CompartTypeMandatoryImageId = p.MandatoryImageId,
                    Data = Convert.FromBase64String(newPhoto),
                    InspectionId = p.InspectionId,
                    Side = p.Side,
                    Title = definition.Title
                };
                _context.INSPECTION_COMPARTTYPE_IMAGES.Add(newRecord);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Photo uploaded successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to upload photo");
            }
        }

        public async Task<Tuple<bool, string>> UploadMeasurementPointPhoto(MeasurementPointPhotoModel p)
        {
            var photoRecord = await _context.MEASUREPOINT_RECORD_IMAGES.FindAsync(p.RecordId);
            if (photoRecord != null)
            {
                photoRecord.Data = Convert.FromBase64String(p.PhotoData);
            }

            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(true, "Photo updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to upload photo");
            }
        }

        public async Task<Tuple<decimal, string>> UpdateMeasurementPointReading(int inspectionDetailId, int compartMeasurePointId, int readingNumber, decimal reading)
        {
            bool _deleted = false;
            var measurementPoint = _context.MEASUREMENT_POINT_RECORD
                .Where(i => i.InspectionDetailId == inspectionDetailId)
                .Where(i => i.CompartMeasurePointId == compartMeasurePointId)
                .Where(i => i.MeasureNumber == readingNumber)
                .FirstOrDefault();
            if(measurementPoint == null && reading < 0) return Tuple.Create((decimal)-1, "Reading updated successfully.");
            if (measurementPoint == null)
            {
                var compartMeasurePoint = await _context.COMPART_MEASUREMENT_POINT.FindAsync(compartMeasurePointId);
                measurementPoint = new MEASUREMENT_POINT_RECORD()
                {
                    CompartMeasurePointId = compartMeasurePointId,
                    EvalCode = "",
                    InboardOutborad = 0,
                    InspectionDetailId = inspectionDetailId,
                    MeasureNumber = readingNumber,
                    Notes = "",
                    Reading = reading,
                    ToolId = compartMeasurePoint.DefaultToolId != null ? (int)compartMeasurePoint.DefaultToolId : -1,
                    Worn = 0
                };
                _context.MEASUREMENT_POINT_RECORD.Add(measurementPoint);
            }
            else if (reading < 0)
            {
                _context.MEASUREMENT_POINT_RECORD.Remove(measurementPoint);
                _deleted = true;
            }
            else
            {
                measurementPoint.Reading = reading;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create((decimal)-999, e.ToDetailedString());
            }
            decimal worn = 0;
            if (!_deleted)
            {
                // Worn
                BLL.Core.Domain.MiningShovelDomain.MeasurementPoint pointRecord = new Domain.MiningShovelDomain.MeasurementPoint(_context, measurementPoint.Id);
                worn = pointRecord.CalcWornPercentage(
                    reading.ConvertMMToInch(),
                    measurementPoint.ToolId,
                    InspectionImpact.High
                );

                measurementPoint.Worn = worn;
                //if (inspectionDetail != null && inspectionDetail.worn_percentage < worn)
                //inspectionDetail.worn_percentage = worn;
                //var details = _context.TRACK_INSPECTION_DETAIL.Where(d => d.inspection_auto == inspectionDetail.inspection_auto).ToList();

                try
                {
                    await _context.SaveChangesAsync();
                    //return Tuple.Create(worn, "Reading updated successfully. ");
                }
                catch (Exception e)
                {
                    return Tuple.Create((decimal)-999, e.ToDetailedString());
                }
            }
            var inspectionDetail = await _context.TRACK_INSPECTION_DETAIL.FindAsync(measurementPoint.InspectionDetailId);
            var manager = new MiningShovelMobileSyncManager(_context);
            var newWorn = manager.GetComponentWorn(inspectionDetail.inspection_auto, inspectionDetail.track_unit_auto);
            inspectionDetail.worn_percentage = newWorn.wornPercentage;
            inspectionDetail.eval_code = newWorn.evalCode.ToString();
            try
            {
                await _context.SaveChangesAsync();
                //return Tuple.Create(worn, "Reading updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create((decimal)-999, e.ToDetailedString());
            }

            var newEquipmentWorn = manager.GetEquipmentWorn(inspectionDetail.inspection_auto);
            inspectionDetail.TRACK_INSPECTION.evalcode = newEquipmentWorn.evalCode.ToString();

            try
            {
                await _context.SaveChangesAsync();
                return Tuple.Create(worn, "Reading updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create((decimal)-999, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Returns the image category ID for recommendation photos matching the 
        /// specified measurement point photo ID.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<List<int>> GetRecommendationPhotoIdsForMPPhoto(int photoId)
        {
            var result = await _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES
                .Where(w => w.ImageCategories.Measurement_Point_Image == photoId)
                .Select(s => s.ImageCategoryId).ToListAsync();

            return result;
        }

        /// <summary>
        /// Returns the image category ID for recommendation photos matching the 
        /// specified (Comparttype) mandatory photo ID.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<List<int>> GetRecommendationPhotoIdsForMandatoryPhoto(int photoId)
        {
            var result = await _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES
                .Where(w => w.ImageCategories.Comparttype_Mandatory_Image == photoId)
                .Select(s => s.ImageCategoryId).ToListAsync();

            return result;
        }

        /// <summary>
        /// Returns the image category ID for recommendation photos matching the 
        /// specified (Equipment) mandatory photo ID.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<List<int>> GetRecommendationPhotoIdsForEquipmentMandatoryPhoto(int photoId)
        {
            var result = await _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES
                .Where(w => w.ImageCategories.Inspection_Mandatory_Image == photoId)
                .Select(s => s.ImageCategoryId).ToListAsync();

            return result;
        }

        /// <summary>
        /// Returns the image category ID for report photos matching the 
        /// specified measurement point photo ID.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<List<int>> GetReportPhotoIdsForMPPhoto(int photoId)
        {
            var result = await _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                .Where(w => w.ImageCategories.Measurement_Point_Image == photoId)
                .Select(s => s.ReportImageId != null ? s.ReportImageId.Value : 0).ToListAsync();

            return result;
        }

        /// <summary>
        /// Returns the image category ID for report photos matching the 
        /// specified (Comparttype) mandatory photo ID.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<List<int>> GetReportPhotoIdsForMandatoryPhoto(int photoId)
        {
            var result = await _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                .Where(w => w.ImageCategories.Comparttype_Mandatory_Image == photoId)
                .Select(s => s.ReportImageId != null ? s.ReportImageId.Value : 0).ToListAsync();

            return result;
        }

        /// <summary>
        /// Returns the image category ID for report photos matching the 
        /// specified (Equipment) mandatory photo ID.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<List<int>> GetReportPhotoIdsForEquipmentMandatoryPhoto(int photoId)
        {
            var result = await _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                .Where(w => w.ImageCategories.Inspection_Mandatory_Image == photoId)
                .Select(s => s.ReportImageId != null ? s.ReportImageId.Value : 0).ToListAsync();

            return result;
        }

        /// <summary>
        /// Returns the IDs for any report introduction records that use the 
        /// specified (Equipment) mandatory photo ID as the cover photo.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<List<int>> GetReportIntroductionIdForCoverPhoto(int photoId)
        {
            var result = await _context.MININGSHOVEL_REPORT_INTRODUCTION
                .Where(w => w.CoverImage == photoId)
                .Select(s => s.Id).ToListAsync();

            return result;
        }

        /// <summary>
        /// Delete Measurement point photos.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> DeleteMeasurementPointPhoto(int photoId)
        {
            bool result = false;
            string message = "Unable to delete Measurement Point Photo.";

            var photoToDelete = await _context.MEASUREPOINT_RECORD_IMAGES.FindAsync(photoId);
            if(photoToDelete != null)
            {
                _context.MEASUREPOINT_RECORD_IMAGES.Remove(photoToDelete);

                try
                {
                    int _changesSaved = await _context.SaveChangesAsync();
                    if(_changesSaved > 0)
                    {
                        result = true;
                        message = "Measurement Point Photo deleted successfully.";
                    }
                }
                catch (Exception e)
                {
                    message = e.ToDetailedString();
                }
            }

            return Tuple.Create(result, message);
        }

        /// <summary>
        /// Delete comparttype mandatory photos.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> DeleteMandatoryPhoto(int photoId)
        {
            bool result = false;
            string message = "Unable to delete Mandatory Photo.";

            var photoToDelete = await _context.INSPECTION_COMPARTTYPE_IMAGES.FindAsync(photoId);
            if (photoToDelete != null)
            {
                _context.INSPECTION_COMPARTTYPE_IMAGES.Remove(photoToDelete);

                try
                {
                    int _changesSaved = await _context.SaveChangesAsync();
                    if (_changesSaved > 0)
                    {
                        result = true;
                        message = "Mandatory Photo deleted successfully.";
                    }
                }
                catch (Exception e)
                {
                    message = e.ToDetailedString();
                }
            }

            return Tuple.Create(result, message);
        }

        /// <summary>
        /// Delete equipment mandatory photos.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, string>> DeleteEquipmentMandatoryPhoto(int photoId)
        {
            bool result = false;
            string message = "Unable to delete Equipment Mandatory Photo.";

            var photoToDelete = await _context.INSPECTION_MANDATORY_IMAGES.FindAsync(photoId);
            if (photoToDelete != null)
            {
                photoToDelete.Data = null;

                try
                {
                    int _changesSaved = await _context.SaveChangesAsync();
                    if (_changesSaved > 0)
                    {
                        result = true;
                        message = "Equipment Mandatory Photo deleted successfully.";
                    }
                }
                catch (Exception e)
                {
                    message = e.ToDetailedString();
                }
            }

            return Tuple.Create(result, message);
        }
    }
}
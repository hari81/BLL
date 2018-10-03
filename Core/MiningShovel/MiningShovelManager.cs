using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using BLL.Core.MiningShovel.Models;
using BLL.Core.Domain;

namespace BLL.Core.MiningShovel
{
    public class MiningShovelManager
    {
        private UndercarriageContext _context;

        private const bool CONSOLIDATE_TRACK_SHOE_PITCH_DATA = true;
        private const int TRACK_SHOE_PITCH_ID = 20180705;
        private int TrackShoePitch_1Shoe_ID;
        private int TrackShoePitch_10Shoes_ID;

        public MiningShovelManager(UndercarriageContext context)
        {
            _context = context;

            TrackShoePitch_1Shoe_ID = _context.COMPART_MEASUREMENT_POINT
                .Where(w => w.Name == "Track Shoe Pitch – 1 Shoe")
                .Select(s => s.Id).FirstOrDefault();

            TrackShoePitch_10Shoes_ID = _context.COMPART_MEASUREMENT_POINT
                .Where(w => w.Name == "Track Shoe  Pitch – 10 Shoe")
                .Select(s => s.Id).FirstOrDefault();
        }

        /// <summary>
        /// Returns the following details for a given inspection ID.
        /// - Date of inspection
        /// - Customer name
        /// - Equipment make and model
        /// - Unit number
        /// - SMU
        /// - Inspector
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<MiningShovelInspectionOverview> GetMSInspectionDetails(int MSInspectionID)
        {
            MiningShovelInspectionOverview msio = new MiningShovelInspectionOverview();

            var inspection = await _context.TRACK_INSPECTION.FindAsync(MSInspectionID);

           


            if (inspection == null) return null;

            msio.dateOfInspection = inspection.inspection_date;
            msio.customerName = _context.CRSF
                    .Where(c => c.crsf_auto == (inspection.EQUIPMENT.crsf_auto))
                    .Select(s => s.Customer.cust_name).FirstOrDefault();
            msio.equipmentId = inspection.equipmentid_auto;
            msio.equipmentMake = inspection.EQUIPMENT.LU_MMTA.MAKE.makedesc;
            msio.equipmentModel = inspection.EQUIPMENT.LU_MMTA.MODEL.modeldesc;
            msio.unitNumber = inspection.EQUIPMENT.unitno;
            msio.undercarriageSMU = inspection.smu.Value.ToString();
            msio.UCInspector = inspection.examiner;
            msio.equipmentCurrentSMU = inspection.EQUIPMENT.currentsmu.ToString();
            msio.ReportDateStringFormat = inspection.inspection_date.Date.ToString("dd/MMM/yyyy");


            var jobsiteObj = _context.CRSF.FirstOrDefault(c => c.crsf_auto == inspection.EQUIPMENT.crsf_auto);
            msio.equipmentJobsite =  jobsiteObj.site_name;


            var systems = await _context.LU_Module_Sub.Where(m => m.equipmentid_auto == inspection.equipmentid_auto  && m.systemTypeEnumIndex == (int)UCSystemType.Chain).ToListAsync();

            var systemViewModels = new List<SystemPrefillContentViewModel>();
            systems.ForEach(s => systemViewModels.Add(new SystemPrefillContentViewModel
            {
                CMU = s.CMU,
                SystemId = s.Module_sub_auto,
                SystemSerialNumber = s.Serialno,
                SystemTypeEnum = s.systemTypeEnumIndex,
                Side =
                _context.GENERAL_EQ_UNIT.Count(e => e.equipmentid_auto == inspection.equipmentid_auto && e.module_ucsub_auto == s.Module_sub_auto && e.side == (int)Side.Left) >
                _context.GENERAL_EQ_UNIT.Count(e => e.equipmentid_auto == inspection.equipmentid_auto && e.module_ucsub_auto == s.Module_sub_auto && e.side == (int)Side.Right) ? (int)Side.Left : (int)Side.Right,
            }));


            msio.systemPrefillContentViewModels = systemViewModels;

            return msio;
        }

        /// <summary>
        /// Return the list of measurement points based on the compartID provided.
        /// </summary>
        /// <param name="compartTypeID"></param>
        /// <returns></returns>
        public async Task<List<CompartMeasurementPoint>> GetCompartMeasurementPoints(int compartTypeID, int EqId)
        {
            List<CompartMeasurementPoint> MPs = await _context.EQUIPMENTs.Where(m => m.equipmentid_auto == EqId)
                .SelectMany(m => m.Components)
                .Where(m=> m.LU_COMPART.comparttype_auto == compartTypeID)
                .SelectMany(m => m.LU_COMPART.MeasurementPoints)
                .OrderBy(o => o.Order)
                .Select(m => new CompartMeasurementPoint
                {
                    measurePointId = m.Id,
                    measurePointName = m.Name,
                    numberOfMeasurements = m.DefaultNumberOfMeasurements
                })
                .GroupBy(g => g.measurePointId).Select(s => s.FirstOrDefault())
                .ToListAsync();

            // NOTE: This is to override the way Track Shoes are handled, since there is effectively 
            // only 1 measurement point, but it contains measurements for both 1-Shoe and 10-Shoes.
            if((CONSOLIDATE_TRACK_SHOE_PITCH_DATA) && (compartTypeID == (int)BLL.Core.Domain.CompartTypeEnum.Link))
            {
                // Get the indices for Track Shoe Pitch data.
                int TrackShoePitch1Shoe = -1;
                int TrackShoePitch10Shoes = -1;
                for(int i=0; i<MPs.Count; i++)
                {
                    if(MPs[i].measurePointId == TrackShoePitch_1Shoe_ID)
                    {
                        TrackShoePitch1Shoe = i;
                        continue;
                    }
                    else if(MPs[i].measurePointId == TrackShoePitch_10Shoes_ID)
                    {
                        TrackShoePitch10Shoes = i;
                        continue;
                    }
                }

                if(TrackShoePitch1Shoe > -1 && TrackShoePitch10Shoes > -1)
                {
                    MPs[TrackShoePitch1Shoe].measurePointId = TRACK_SHOE_PITCH_ID;
                    MPs[TrackShoePitch1Shoe].measurePointName = "Track Shoe Pitch";
                    MPs[TrackShoePitch1Shoe].numberOfMeasurements += 10;

                    MPs.RemoveAt(TrackShoePitch10Shoes);
                }
            }

            return MPs;
        }


        /// <summary>
        /// If creating report retrieve all the necessary content for prefilling the report builder 
        /// </summary>
        /// <param name="equipmentId"></param>
        /// <returns></returns>
        public async Task<ReportPrefillContentViewModel> GetPrefillReportContent(int equipmentId)
        {
            var systems = await _context.LU_Module_Sub.Where(m => m.equipmentid_auto == equipmentId).ToListAsync();

           

            var systemViewModels = new List<SystemPrefillContentViewModel>();
            systems.ForEach(s => systemViewModels.Add(new SystemPrefillContentViewModel {
                CMU = s.CMU,
                SystemId = s.Module_sub_auto,
                SystemSerialNumber = s.Serialno,
                SystemTypeEnum = s.systemTypeEnumIndex,
                Side = 
                _context.GENERAL_EQ_UNIT.Count(e => e.equipmentid_auto == equipmentId && e.module_ucsub_auto == s.Module_sub_auto && e.side == (int)Side.Left) >
                _context.GENERAL_EQ_UNIT.Count(e => e.equipmentid_auto == equipmentId && e.module_ucsub_auto == s.Module_sub_auto && e.side == (int)Side.Right) ? (int)Side.Left : (int)Side.Right,
                
        }));

            return new ReportPrefillContentViewModel {  Systems = systemViewModels };
           
        }

        /// <summary>
        /// Return the measurement point photos for a specified inspection and measurement point.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <param name="measurePointID"></param>
        /// <returns></returns>
        public async Task<List<InspectionPhoto>> GetMeasurementPointImages(int MSInspectionID, int measurePointID)
        {
            // Override for Track Shoe Pitch. First handle the images for 1 Shoe.
            if(CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TRACK_SHOE_PITCH_ID)
            {
                measurePointID = TrackShoePitch_1Shoe_ID;
            }

            var MPIs = await _context.MEASUREPOINT_RECORD_IMAGES
                        .Where(s => s.MeasurePointRecord.InspectionDetail.inspection_auto == MSInspectionID
                            && s.MeasurePointRecord.CompartMeasurePointId == measurePointID)
                        .Select(a => new
                        {
                            id = a.Id,
                            a.Data,
                            imageTitle = a.Title,
                            imageComment = a.Comment
                        }).ToListAsync();

            // Now handle the images for 10 Shoes.
            if(CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TrackShoePitch_1Shoe_ID)
            {
                var MPIs2 = await _context.MEASUREPOINT_RECORD_IMAGES
                        .Where(s => s.MeasurePointRecord.InspectionDetail.inspection_auto == MSInspectionID
                            && s.MeasurePointRecord.CompartMeasurePointId == TrackShoePitch_10Shoes_ID)
                        .Select(a => new
                        {
                            id = a.Id,
                            a.Data,
                            imageTitle = a.Title,
                            imageComment = a.Comment
                        }).ToListAsync();
                MPIs.AddRange(MPIs2);
            }

            List<InspectionPhoto> result = new List<InspectionPhoto>();
            for (int i = 0; i < MPIs.Count; i++)
            {
                int imageId = MPIs[i].id;
                bool isHiddenFromReport = await _context.REPORT_HIDDEN_MEASUREPOINT_RECORD_IMAGES
                    .Where(w => w.MeasurePointRecordImageId == imageId
                        && w.Report.InspectionId == MSInspectionID).AnyAsync();

                bool imageIsEnlarged = await _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.ImageCategories.Measurement_Point_Image == imageId
                        && w.MiningShovelReport.InspectionId == MSInspectionID).AnyAsync();

                result.Add(new InspectionPhoto
                {
                    photoType = (int) InspectionPhotoType.Measurement_Point_Photo,
                    id = MPIs[i].id,
                    data = Convert.ToBase64String(MPIs[i].Data),
                    title = MPIs[i].imageTitle,
                    comment = MPIs[i].imageComment,
                    isHidden = isHiddenFromReport,
                    isLarge = imageIsEnlarged
                });
            }

            return result;
        }



        /// <summary>
        /// Return the measurement point readings for a given inspection.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <param name="measurePointID"></param>
        /// <returns></returns>
        public async Task<MeasurementPointReadings> GetMeasurementPointReadings(int MSInspectionID, int measurePointID)
        {
            MeasurementPointReadings result = new MeasurementPointReadings();
            result.measurePointId = measurePointID;

            // Override for Track Shoe Pitch. First handle the data for 1 Shoe.
            if (CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TRACK_SHOE_PITCH_ID)
            {
                measurePointID = TrackShoePitch_1Shoe_ID;
            }

            // Get the inspection readings for the provided measure point ID.
            var inspectionReadings = await _context.MEASUREMENT_POINT_RECORD
                .Where(c => c.CompartMeasurePointId == measurePointID
                    && c.InspectionDetail.TRACK_INSPECTION.inspection_auto == MSInspectionID)
                .Join(_context.TRACK_INSPECTION_DETAIL,
                    mpr => mpr.InspectionDetailId,
                    ids => ids.inspection_detail_auto,
                    (mpr, ids) => new { Measurements = mpr, Details = ids })
                .OrderBy(m => m.Measurements.MeasureNumber)
                .ThenBy(s => s.Details.GENERAL_EQ_UNIT.side)
                .ToListAsync();

            // Override for Track Shoe Pitch. Query the data for 10 Shoes.
            if (CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TrackShoePitch_1Shoe_ID)
            {
                var inspectionReadings2 = await _context.MEASUREMENT_POINT_RECORD
                .Where(c => c.CompartMeasurePointId == TrackShoePitch_10Shoes_ID
                    && c.InspectionDetail.TRACK_INSPECTION.inspection_auto == MSInspectionID)
                .Join(_context.TRACK_INSPECTION_DETAIL,
                    mpr => mpr.InspectionDetailId,
                    ids => ids.inspection_detail_auto,
                    (mpr, ids) => new { Measurements = mpr, Details = ids })
                .OrderBy(m => m.Measurements.MeasureNumber)
                .ThenBy(s => s.Details.GENERAL_EQ_UNIT.side)
                .ToListAsync();
                inspectionReadings.AddRange(inspectionReadings2);
            }

            List<ReadingValue> rv = new List<ReadingValue>();
            for (int i = 0; i < inspectionReadings.Count; i++)
            {
                ReadingValue reading;

                // Check for existing reading.
                int existingReading = -1;
                for (int j = 0; j < rv.Count; j++)
                {
                    // Handle the location for Track Shoe Pitch - 10 Shoes.
                    if (inspectionReadings[i].Measurements.CompartMeasurePoint.Id == TrackShoePitch_10Shoes_ID)
                    {
                        if (rv[j].location == inspectionReadings[i].Measurements.CompartMeasurePoint.Name)
                        {
                            existingReading = j;
                            break;
                        }
                    }

                    // Special case for Track Rollers
                    else if (inspectionReadings[i].Measurements.CompartMeasurePoint.Compart.LU_COMPART_TYPE.comparttype == "Track Roller")
                    {
                        var component = new BLL.Core.Domain.Component(_context, (int)inspectionReadings[i].Measurements.InspectionDetail.track_unit_auto);
                        var desc = component.GetComponentDescription() + " " + component.GetPositionLabel();

                        if (rv[j].location == desc)
                        {
                            existingReading = j;
                            break;
                        }
                    }

                    // Show the measure number as the location for all other readings.
                    else
                    {
                        if (rv[j].location == inspectionReadings[i].Measurements.MeasureNumber.ToString())
                        {
                            existingReading = j;
                            break;
                        }
                    }

                }

                // Doesn't exist.
                if (existingReading == -1)
                {
                    reading = new ReadingValue();

                    // Update the reading location to reflect the Track Shoe Pitch for 10 Shoes. 
                    if (inspectionReadings[i].Measurements.CompartMeasurePoint.Id == TrackShoePitch_10Shoes_ID)
                    {
                        reading.location = inspectionReadings[i].Measurements.CompartMeasurePoint.Name;
                    }

                    // Special case for Track Rollers
                    else if(inspectionReadings[i].Measurements.CompartMeasurePoint.Compart.LU_COMPART_TYPE.comparttype == "Track Roller")
                    {
                        var component = new BLL.Core.Domain.Component(_context, (int)inspectionReadings[i].Measurements.InspectionDetail.track_unit_auto);
                        reading.location = component.GetComponentDescription() + " " +component.GetPositionLabel();
                    }

                    // Show other reading locations normally.
                    else
                    {
                        reading.location = inspectionReadings[i].Measurements.MeasureNumber.ToString();
                    }
                }
                else
                {
                    reading = rv[existingReading];
                }

                // Left or Right side.
                if (inspectionReadings[i].Details.GENERAL_EQ_UNIT.side == 1)
                {
                    reading.left = inspectionReadings[i].Measurements.Reading;
                }
                else
                {
                    reading.right = inspectionReadings[i].Measurements.Reading;
                }

                // Add new reading to the list.
                if (existingReading == -1)
                {
                    rv.Add(reading);
                }
            }

            if (inspectionReadings.Count > 0)
            {
                result.tool = inspectionReadings[0].Measurements.Tool.tool_name;
            }
            else
            {
                result.tool = "N/A";
            }

            result.listOfReadings = rv;
            result.totalCount = rv.Count;

            // Override for Track Shoe Pitch
            if (CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TrackShoePitch_1Shoe_ID)
            {
                measurePointID = TRACK_SHOE_PITCH_ID;
                result.totalCount += 9;

                var hiddenFromReport = await _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD
                .Where(w => (w.CompartMeasurementPointId == TrackShoePitch_1Shoe_ID)
                    && (w.Report.InspectionId == MSInspectionID))
                .FirstOrDefaultAsync();
                if (hiddenFromReport != null)
                {
                    result.isHidden = hiddenFromReport.hideReadings;
                    result.isHiddenAll = hiddenFromReport.hideAll;
                }
            }

            else
            {
                var hiddenFromReport = await _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD
                                .Where(w => (w.CompartMeasurementPointId == measurePointID)
                                    && (w.Report.InspectionId == MSInspectionID))
                                .FirstOrDefaultAsync();
                if (hiddenFromReport != null)
                {
                    result.isHidden = hiddenFromReport.hideReadings;
                    result.isHiddenAll = hiddenFromReport.hideAll;
                }
            }

            

            return result;
        }

        

        /// <summary>
        /// Return the observations for a given measurement point inspection.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <param name="measurePointID"></param>
        /// <returns></returns>
        public async Task<List<MeasurementPointObservation>> GetMeasurementPointObservations(int MSInspectionID, int measurePointID)
        {
            List<MeasurementPointObservation> result = new List<MeasurementPointObservation>();

            // Override for Track Shoe Pitch. First handle the data for 1 Shoe.
            if (CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TRACK_SHOE_PITCH_ID)
            {
                measurePointID = TrackShoePitch_1Shoe_ID;
            }

            // Get the observations for this measurement point ID.
            var inspectionReadings = await _context.MEASUREMENT_POINT_RECORD
                .Where(c => c.CompartMeasurePointId == measurePointID
                    && c.InspectionDetail.TRACK_INSPECTION.inspection_auto == MSInspectionID
                    && c.MeasureNumber == 1)
                .Join(_context.TRACK_INSPECTION_DETAIL,
                    mpr => mpr.InspectionDetailId,
                    ids => ids.inspection_detail_auto,
                    (mpr, ids) => new { Measurements = mpr, Details = ids })
                .OrderBy(m => m.Measurements.MeasureNumber)
                .ThenBy(s => s.Details.GENERAL_EQ_UNIT.side)
                .ToListAsync();

            // Override for Track Shoe Pitch. Query the data for 10 Shoes.
            if (CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TrackShoePitch_1Shoe_ID)
            {
                var inspectionReadings2 = await _context.MEASUREMENT_POINT_RECORD
                .Where(c => c.CompartMeasurePointId == TrackShoePitch_10Shoes_ID
                    && c.InspectionDetail.TRACK_INSPECTION.inspection_auto == MSInspectionID
                    && c.MeasureNumber == 1)
                .Join(_context.TRACK_INSPECTION_DETAIL,
                    mpr => mpr.InspectionDetailId,
                    ids => ids.inspection_detail_auto,
                    (mpr, ids) => new { Measurements = mpr, Details = ids })
                .OrderBy(m => m.Measurements.MeasureNumber)
                .ThenBy(s => s.Details.GENERAL_EQ_UNIT.side)
                .ToListAsync();
                inspectionReadings.AddRange(inspectionReadings2);

                // Reset the measurePointID.
                measurePointID = TRACK_SHOE_PITCH_ID;
            }

            for (int i = 0; i < inspectionReadings.Count; i++)
            {
                string mpDesc = inspectionReadings[i].Measurements.CompartMeasurePoint.Name;
                if (inspectionReadings[i].Measurements.CompartMeasurePoint.Compart.LU_COMPART_TYPE.comparttype == "Track Roller")
                {
                    var component = new BLL.Core.Domain.Component(_context, (int)inspectionReadings[i].Measurements.InspectionDetail.track_unit_auto);
                    mpDesc = component.GetComponentDescription() + " " + component.GetPositionLabel();
                }

                result.Add(new MeasurementPointObservation
                {
                    measurePointId = measurePointID,
                    observation = inspectionReadings[i].Measurements.Notes,
                    side = (
                        inspectionReadings[i].Details.GENERAL_EQ_UNIT.side == 1 ?
                            "Left"
                        : (inspectionReadings[i].Details.GENERAL_EQ_UNIT.side == 2 ?
                                "Right"
                            : "General"
                        )),
                    desc = mpDesc
                });
            }

            return result;
        }

        /// <summary>
        /// Gets the Rope Shovel Report for a given inspection Id.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<int> GetReport(int MSInspectionID)
        {
            int result = -1;

            var reportId = await _context.MININGSHOVEL_REPORT
                .Where(r => r.InspectionId == MSInspectionID)
                .Select(r2 => r2.Id).FirstOrDefaultAsync();

            result = reportId;

            return result;
        }

        /// <summary>
        /// Save an entry for new reports in the MININGSHOVEL_REPORT table.
        /// </summary>
        /// <param name="reportParams"></param>
        /// <returns></returns>
        public int SaveReport(SaveReportParams reportParams)
        {
            int result = -1;

            var reportId = _context.MININGSHOVEL_REPORT.Where(r => r.InspectionId == reportParams.MSInspectionID)
                .Select(r2 => r2.Id).FirstOrDefault();
            if (reportId == 0)
            {
                DateTime date = DateTime.TryParse(reportParams.createdDate, out date) ? date : DateTime.Now;

                if (reportParams.createdUser <= 0)
                {
                    return result;
                }

                MININGSHOVEL_REPORT report = new MININGSHOVEL_REPORT
                {
                    InspectionId = reportParams.MSInspectionID,
                    CreatedByUserId = reportParams.createdUser,
                    CreatedDate = date
                };
                _context.MININGSHOVEL_REPORT.Add(report);
                _context.SaveChanges();

                result = report.Id;
            }

            else
            {
                // Report already exists in dbo.MININGSHOVEL_REPORT table.
                result = reportId;
            }

            return   result   ;
        }

        /// <summary>
        /// Save an entry for new recommendation photos in the MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES table.
        /// </summary>
        /// <param name="photoParams"></param>
        /// <returns></returns>
        public int SaveRecommendationPhoto(SaveRecommendationPhotoParams photoParams)
        {
            int _changesSaved = -1;
            int result = -1;

            MININGSHOVEL_REPORT_IMAGE_CATEGORIES imageCat = new MININGSHOVEL_REPORT_IMAGE_CATEGORIES();
            if (photoParams.photoType == (int)InspectionPhotoType.Inspection_Mandatory_Photo)
            {
                imageCat.Inspection_Mandatory_Image = photoParams.photoId;
            }
            else if (photoParams.photoType == (int)InspectionPhotoType.Comparttype_Mandatory_Photo)
            {
                imageCat.Comparttype_Mandatory_Image = photoParams.photoId;
            }
            else if (photoParams.photoType == (int)InspectionPhotoType.Comparttype_Additional_Photo)
            {
                imageCat.Comparttype_Additional_Image = photoParams.photoId;
            }
            else if (photoParams.photoType == (int)InspectionPhotoType.Measurement_Point_Photo)
            {
                imageCat.Measurement_Point_Image = photoParams.photoId;
            }
            _context.MININGSHOVEL_REPORT_IMAGE_CATEGORIES.Add(imageCat);
            _changesSaved = _context.SaveChanges();

            if (_changesSaved > 0)
            {
                MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES recommendationImg = new MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES();
                recommendationImg.RecommendationId = photoParams.recommendationId;
                recommendationImg.ImageCategoryId = imageCat.Id;
                _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES.Add(recommendationImg);
                _changesSaved = _context.SaveChanges();

                if (_changesSaved > 0)
                {
                    result = recommendationImg.Id;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all photos stored against a recommendation.
        /// </summary>
        /// <param name="recommendationID"></param>
        /// <returns></returns>
        public async Task<List<InspectionPhoto>> GetRecommendationPhotos(int recommendationID)
        {
            List<InspectionPhoto> result = new List<InspectionPhoto>();

            // Guard
            if (recommendationID <= 0)
            {
                return result;
            }

            // NOTE: The Id returned here is the ImageCategoryId, required specifically for recommendations.
            var inspection_mandatory_images = await _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES
                .Where(w => w.RecommendationId == recommendationID && w.ImageCategories.Inspection_Mandatory_Image != null)
                .Select(s => new
                {
                    s.Id,
                    s.ImageCategoryId,
                    s.ImageCategories.InspectionMandatoryImages.Title,
                    s.ImageCategories.InspectionMandatoryImages.Comment,
                    s.ImageCategories.InspectionMandatoryImages.Data
                }).ToListAsync();

            var comparttype_mandatory_images = await _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES
                .Where(w => w.RecommendationId == recommendationID && w.ImageCategories.Comparttype_Mandatory_Image != null)
                .Select(s => new
                {
                    s.Id,
                    s.ImageCategoryId,
                    s.ImageCategories.InspectionComparttypeImages.Title,
                    s.ImageCategories.InspectionComparttypeImages.Comment,
                    s.ImageCategories.InspectionComparttypeImages.Data
                }).ToListAsync();

            var comparttype_additional_images = await _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES
                .Where(w => w.RecommendationId == recommendationID && w.ImageCategories.Comparttype_Additional_Image != null)
                .Select(s => new
                {
                    s.Id,
                    s.ImageCategoryId,
                    s.ImageCategories.InspectionComparttypeRecordImages.Title,
                    s.ImageCategories.InspectionComparttypeRecordImages.Comment,
                    s.ImageCategories.InspectionComparttypeRecordImages.Data
                }).ToListAsync();

            var measurement_point_images = await _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES
                .Where(w => w.RecommendationId == recommendationID && w.ImageCategories.Measurement_Point_Image != null)
                .Select(s => new
                {
                    s.Id,
                    s.ImageCategoryId,
                    s.ImageCategories.MeasurePointRecordImages.Title,
                    s.ImageCategories.MeasurePointRecordImages.Comment,
                    s.ImageCategories.MeasurePointRecordImages.Data
                }).ToListAsync();

            for(int i=0;i<inspection_mandatory_images.Count; i++)
            {
                var imgId = inspection_mandatory_images[i].Id;
                bool imageIsEnlarged = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.RecommendationImageId == imgId).Any();

                result.Add(new InspectionPhoto
                {
                    photoType = (int)InspectionPhotoType.Inspection_Mandatory_Photo,
                    id = inspection_mandatory_images[i].ImageCategoryId,
                    title = inspection_mandatory_images[i].Title,
                    comment = inspection_mandatory_images[i].Comment,
                    data = Convert.ToBase64String(inspection_mandatory_images[i].Data),
                    isLarge = imageIsEnlarged
                });
            }

            for (int i = 0; i < comparttype_mandatory_images.Count; i++)
            {
                var imgId = comparttype_mandatory_images[i].Id;
                bool imageIsEnlarged = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.RecommendationImageId == imgId).Any();

                result.Add(new InspectionPhoto
                {
                    photoType = (int)InspectionPhotoType.Comparttype_Mandatory_Photo,
                    id = comparttype_mandatory_images[i].ImageCategoryId,
                    title = comparttype_mandatory_images[i].Title,
                    comment = comparttype_mandatory_images[i].Comment,
                    data = Convert.ToBase64String(comparttype_mandatory_images[i].Data),
                    isLarge = imageIsEnlarged
                });
            }

            for (int i = 0; i < comparttype_additional_images.Count; i++)
            {
                var imgId = comparttype_additional_images[i].Id;
                bool imageIsEnlarged = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.RecommendationImageId == imgId).Any();

                result.Add(new InspectionPhoto
                {
                    photoType = (int)InspectionPhotoType.Comparttype_Additional_Photo,
                    id = comparttype_additional_images[i].ImageCategoryId,
                    title = comparttype_additional_images[i].Title,
                    comment = comparttype_additional_images[i].Comment,
                    data = Convert.ToBase64String(comparttype_additional_images[i].Data),
                    isLarge = imageIsEnlarged
                });
            }

            for (int i = 0; i < measurement_point_images.Count; i++)
            {
                var imgId = measurement_point_images[i].Id;
                bool imageIsEnlarged = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.RecommendationImageId == imgId).Any();

                result.Add(new InspectionPhoto
                {
                    photoType = (int)InspectionPhotoType.Measurement_Point_Photo,
                    id = measurement_point_images[i].ImageCategoryId,
                    title = measurement_point_images[i].Title,
                    comment = measurement_point_images[i].Comment,
                    data = Convert.ToBase64String(measurement_point_images[i].Data),
                    isLarge = imageIsEnlarged
                });
            }

            return result;
        }

        /// <summary>
        /// Save any recommendations entered on the Mining Shovel Inspection Report.
        /// </summary>
        /// <param name="recommendationParams"></param>
        /// <returns></returns>
        public int SaveRecommendation(RecommendationParams recommendationParams)
        {
            int _changesSaved = 0;
            int result = -1;

            // Check to make sure that the report exists.
            if (recommendationParams.ReportId <= 0)
            {
                return result;
            }

            // This is a new recommendation.
            if (recommendationParams.RecommendationId == 0)
            {
                MININGSHOVEL_REPORT_RECOMMENDATIONS newRecommendation = new MININGSHOVEL_REPORT_RECOMMENDATIONS
                {
                    MiningShovelReportId = recommendationParams.ReportId,
                    RecommendationTitle = recommendationParams.RecommendationTitle,
                    RecommendationText = recommendationParams.RecommendationText
                };
                _context.MININGSHOVEL_REPORT_RECOMMENDATIONS.Add(newRecommendation);
                _changesSaved = _context.SaveChanges();

                if (_changesSaved > 0)
                {
                    result = newRecommendation.Id;
                }
            }
            // Update existing recommendation.
            else
            {
                var recommendation = _context.MININGSHOVEL_REPORT_RECOMMENDATIONS
                .Where(r => r.MiningShovelReportId == recommendationParams.ReportId
                    && r.Id == recommendationParams.RecommendationId)
                .FirstOrDefault();

                if (recommendation != null)
                {
                    recommendation.RecommendationTitle = recommendationParams.RecommendationTitle;
                    recommendation.RecommendationText = recommendationParams.RecommendationText;
                }
                _changesSaved = _context.SaveChanges();

                if (_changesSaved > 0)
                {
                    result = recommendation.Id;
                }
            }

            return result;
        }

        /// <summary>
        /// Takes in the reportId for the MININGSHOVEL_REPORT table and returns all 
        /// recommendations added on this report.
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<List<RecommendationParams>> GetRecommendations(int reportId)
        {
            List<RecommendationParams> result = new List<RecommendationParams>();

            result = await _context.MININGSHOVEL_REPORT_RECOMMENDATIONS
                .Where(r => r.MiningShovelReportId == reportId)
                .Select(s => new RecommendationParams
                {
                    ReportId = s.MiningShovelReportId,
                    RecommendationId = s.Id,
                    RecommendationTitle = s.RecommendationTitle,
                    RecommendationText = s.RecommendationText
                }).ToListAsync();

            return result;
        }

        /// <summary>
        /// Get the mandatory equipment photos for a given inspection.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<List<InspectionPhoto>> GetEquipmentPhotos(int MSInspectionID)
        {
            List<InspectionPhoto> result = new List<InspectionPhoto>();

            var images = await _context.INSPECTION_MANDATORY_IMAGES
                .Where(w => w.InspectionId == MSInspectionID)
                .Select(s => new
                {
                    id = s.Id,
                    data = s.Data,
                    title = s.Title,
                    comment = s.Comment
                }).ToListAsync();

            for (int i = 0; i < images.Count; i++)
            {
                int imageId = images[i].id;
                bool isHiddenFromReport = await _context.REPORT_HIDDEN_INSPECTION_MANDATORY_IMAGES
                    .Where(w => w.InspectionMandatoryImageId == imageId
                        && w.Report.InspectionId == MSInspectionID).AnyAsync();

                bool imageIsEnlarged = await _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.ImageCategories.Inspection_Mandatory_Image == imageId
                        && w.MiningShovelReport.InspectionId == MSInspectionID).AnyAsync();

                result.Add(new InspectionPhoto
                {
                    id = images[i].id,
                    data = images[i].data != null ? Convert.ToBase64String(images[i].data) : "",
                    title = images[i].title,
                    comment = images[i].comment,
                    isHidden = isHiddenFromReport,
                    isLarge = imageIsEnlarged
                });
            }

       

            return result;
        }

        /// <summary>
        /// Get the mandatory equipment photos for a given inspection.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<List<InspectionPhoto>> GetAvailableEquipmentPhotos(int MSInspectionID)
        {
            List<InspectionPhoto> result = new List<InspectionPhoto>();

            var photos = await GetEquipmentPhotos(MSInspectionID);
            for(int i=0; i< photos.Count;i++)
            {
                if(!photos[i].isHidden)
                {
                    result.Add(photos[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Get all the photos for a given inspection.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<List<InspectionPhoto>> GetAllInspectionPhotos(int MSInspectionID)
        {
            List<InspectionPhoto> result = new List<InspectionPhoto>();

            // Inspection Mandatory images
            var images = await _context.INSPECTION_MANDATORY_IMAGES
                .Where(w => w.InspectionId == MSInspectionID)
                .Select(s => new
                {
                    id = s.Id,
                    data = s.Data,
                    title = s.Title,
                    comment = s.Comment,
                }).ToListAsync();

            for (int i = 0; i < images.Count; i++)
            {
                result.Add(new InspectionPhoto
                {
                    id = images[i].id,
                    data = images[i].data != null ? Convert.ToBase64String(images[i].data) : "",
                    title = images[i].title,
                    comment = images[i].comment,
                    photoType = (int)InspectionPhotoType.Inspection_Mandatory_Photo
                });
            }

            // Comparttype Mandatory images
            var images2 = await _context.INSPECTION_COMPARTTYPE_IMAGES
                .Where(w => w.InspectionId == MSInspectionID)
                .Select(s => new
                {
                    id = s.Id,
                    data = s.Data,
                    title = s.Title,
                    comment = s.Comment
                }).ToListAsync();

            for (int i = 0; i < images2.Count; i++)
            {
                result.Add(new InspectionPhoto
                {
                    id = images2[i].id,
                    data = Convert.ToBase64String(images2[i].data),
                    title = images2[i].title,
                    comment = images2[i].comment,
                    photoType = (int)InspectionPhotoType.Comparttype_Mandatory_Photo
                });
            }

            // Comparttype Additional images
            var images3 = await _context.INSPECTION_COMPARTTYPE_RECORD_IMAGES
                .Join(_context.INSPECTION_COMPARTTYPE_RECORD,
                    icri => icri.RecordId,
                    icr => icr.Id,
                    (icri, icr) => new { RecordImages = icri, Record = icr })
                .Where(w => w.Record.InspectionId == MSInspectionID)
                .Select(s => new
                {
                    id = s.RecordImages.Id,
                    data = s.RecordImages.Data,
                    title = s.RecordImages.Title,
                    comment = s.RecordImages.Comment
                }).ToListAsync();

            for (int i = 0; i < images3.Count; i++)
            {
                result.Add(new InspectionPhoto
                {
                    id = images3[i].id,
                    data = Convert.ToBase64String(images3[i].data),
                    title = images3[i].title,
                    comment = images3[i].comment,
                    photoType = (int)InspectionPhotoType.Comparttype_Additional_Photo
                });
            }

            // Measurement point images
            var MPIs = await _context.MEASUREPOINT_RECORD_IMAGES
                        .Where(s => s.MeasurePointRecord.InspectionDetail.inspection_auto == MSInspectionID)
                        .Select(a => new
                        {
                            a.Id,
                            a.Data,
                            imageTitle = a.Title,
                            imageComment = a.Comment
                        }).ToListAsync();

            for (int i = 0; i < MPIs.Count; i++)
            {
                result.Add(new InspectionPhoto
                {
                    id = MPIs[i].Id,
                    data = Convert.ToBase64String(MPIs[i].Data),
                    title = MPIs[i].imageTitle,
                    comment = MPIs[i].imageComment,
                    photoType = (int)InspectionPhotoType.Measurement_Point_Photo
                });
            }

            return result;
        }

        /// <summary>
        /// Get the mandatory inspection photos for the measurement points.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<List<InspectionPhoto>> GetMandatoryPhotos(int MSInspectionID, int compartTypeID)
        {
            List<InspectionPhoto> result = new List<InspectionPhoto>();

            var images = await _context.INSPECTION_COMPARTTYPE_IMAGES
                .Where(w => w.InspectionId == MSInspectionID
                    && w.CompartTypeMandatoryImage.CompartTypeId == compartTypeID)
                .Select(s => new
                {
                    id = s.Id,
                    data = s.Data,
                    title = s.Title,
                    comment = s.Comment
                }).ToListAsync();

            for (int i = 0; i < images.Count; i++)
            {
                int imageId = images[i].id;
                bool isHiddenFromReport = await _context.REPORT_HIDDEN_COMPARTTYPE_MANDATORY_IMAGE
                    .Where(w => w.InspectionCompartTypeImageId == imageId
                        && w.Report.InspectionId == MSInspectionID).AnyAsync();

                bool imageIsEnlarged = await _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.ImageCategories.Comparttype_Mandatory_Image == imageId
                        && w.MiningShovelReport.InspectionId == MSInspectionID).AnyAsync();

                result.Add(new InspectionPhoto
                {
                    id = images[i].id,
                    data = Convert.ToBase64String(images[i].data),
                    title = images[i].title,
                    comment = images[i].comment,
                    isHidden = isHiddenFromReport,
                    isLarge = imageIsEnlarged
                });
            }

            return result;
        }

        /// <summary>
        /// Get the additional photos for the measurement points.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<List<InspectionPhoto>> GetAdditionalPhotos(int MSInspectionID, int compartTypeID)
        {
            List<InspectionPhoto> result = new List<InspectionPhoto>();

            var images = await _context.INSPECTION_COMPARTTYPE_RECORD_IMAGES
                .Join(_context.INSPECTION_COMPARTTYPE_RECORD,
                    icri => icri.RecordId,
                    icr => icr.Id,
                    (icri, icr) => new { RecordImages = icri, Record = icr })
                .Where(w => w.Record.InspectionId == MSInspectionID
                    && w.Record.CompartTypeAdditional.CompartTypeId == compartTypeID)
                .Select(s => new
                {
                    id = s.RecordImages.Id,
                    data = s.RecordImages.Data,
                    title = s.RecordImages.Title,
                    comment = s.RecordImages.Comment
                }).ToListAsync();

            for (int i = 0; i < images.Count; i++)
            {
                int imageId = images[i].id;
                bool isHiddenFromReport = await _context.REPORT_HIDDEN_ADDITIONAL_RECORD_IMAGE
                    .Where(w => w.CompartTypeRecordImageId == imageId
                        && w.Report.InspectionId == MSInspectionID).AnyAsync();

                bool imageIsEnlarged = await _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.ImageCategories.Comparttype_Additional_Image == imageId
                        && w.MiningShovelReport.InspectionId == MSInspectionID).AnyAsync();

                result.Add(new InspectionPhoto
                {
                    id = images[i].id,
                    data = Convert.ToBase64String(images[i].data),
                    title = images[i].title,
                    comment = images[i].comment,
                    isHidden = isHiddenFromReport,
                    isLarge = imageIsEnlarged,
                    CompartTypeId = compartTypeID
                });
            }

            return result;
        }

        /// <summary>
        /// Get the additional records for a given comparttype and inspection.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <param name="compartTypeID"></param>
        /// <returns></returns>
        public async Task<List<AdditionalRecordDetails>> GetAdditionalRecords(int MSInspectionID, int compartTypeID)
        {
            List<AdditionalRecordDetails> result = new List<AdditionalRecordDetails>();

            var details = await _context.INSPECTION_COMPARTTYPE_RECORD
                .Where(w => w.InspectionId == MSInspectionID
                    && w.CompartTypeAdditional.CompartTypeId == compartTypeID)
                .Select(s => new
                {
                    Title = s.CompartTypeAdditional.Title,
                    Reading = (s.Tool.tool_name == "Observation") ? s.ObservationNote : s.Reading.ToString(),
                    Tool = s.Tool.tool_name,
                    Side = s.Side == 1 ? "Left" : "Right"
                }).ToListAsync();

            for (int i = 0; i < details.Count; i++)
            {
                int existingRecord = -1;
                AdditionalRecordDetails newRecord;

                // Find existing record for this additional reading.
                for (int j = 0; j < result.Count; j++)
                {
                    if (details[i].Title == result[j].Title)
                    {
                        existingRecord = j;
                        break;
                    }
                }

                // Update existing record.
                if (existingRecord != -1)
                {
                    if (details[i].Side == "Left")
                    {
                        result[existingRecord].ReadingL = details[i].Reading;
                    }
                    else if (details[i].Side == "Right")
                    {
                        result[existingRecord].ReadingR = details[i].Reading;
                    }
                }
                // Create new record.
                else
                {
                    newRecord = new AdditionalRecordDetails();
                    newRecord.Title = details[i].Title;
                    newRecord.Tool = details[i].Tool;

                    if (details[i].Side == "Left")
                    {
                        newRecord.ReadingL = details[i].Reading;
                    }
                    else if (details[i].Side == "Right")
                    {
                        newRecord.ReadingR = details[i].Reading;
                    }

                    result.Add(newRecord);
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the cover photo for the Mining Shovel Report.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public async Task<InspectionPhoto> GetCoverPhoto(int photoId)
        {
            var photo = await _context.INSPECTION_MANDATORY_IMAGES
                .Where(i => i.Id == photoId)?
                .Select(s => new
                {
                    id = s.Id,
                    data = s.Data,
                    title = s.Title,
                    comment = s.Comment
                })?.FirstOrDefaultAsync();

            if (photo != null)
            {
                InspectionPhoto result = new InspectionPhoto();
                result.id = photo.id;
                result.data = Convert.ToBase64String(photo.data);
                result.title = photo.title;
                result.comment = photo.comment;
            }

            return null;
        }




        public async Task<bool> ChangeMandatoryImageComment(int photoId, string changedComment)
        {
            var photo = await _context.INSPECTION_COMPARTTYPE_IMAGES.FirstOrDefaultAsync(i => i.Id == photoId);
            if (photo == null) throw new Exception("ChangeMandatoryImageTitle : There is no photo with photo Id " + photoId);
            photo.Comment = changedComment;
            return await SaveChanges();
        }


        public async Task<bool> ChangeAdditionalImageComment(int photoId, string changedComment)
        {
            var image = await _context.INSPECTION_COMPARTTYPE_RECORD_IMAGES.FirstOrDefaultAsync(i => i.Id == photoId);
            image.Comment = changedComment;
            return await SaveChanges();
        }


        public async Task<bool> ChangeMeasurementPointImageComment(int photoId, string changedComment)
        {
            var image = await _context.MEASUREPOINT_RECORD_IMAGES.FirstOrDefaultAsync(i => i.Id == photoId);
            image.Comment = changedComment;
            return await SaveChanges();
        }

        public async Task<bool> ChangeInspectionMandatoryPhotoComment(int photoId, string changedComment)
        {
            var image = await _context.INSPECTION_MANDATORY_IMAGES.FirstOrDefaultAsync(i => i.Id == photoId);
            image.Comment = changedComment;
            return await SaveChanges();
        }


      


        private async Task<bool> SaveChanges()    
        {            
            try
            {
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Return the cover image and introduction text for the specified Report ID.
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<IntroductionParams> GetReportIntroduction(int reportId)
        { 
           var result = await _context.MININGSHOVEL_REPORT_INTRODUCTION
                .Where(r => r.MiningShovelReportId == reportId)
                .Select(s => new IntroductionParams
                {
                    ReportId = s.MiningShovelReportId,
                    IntroId = s.Id,
                    CoverImage = (s.CoverImage == null) ? 0 : s.CoverImage.Value,
                    IntroText1 = s.IntroText1,
                    IntroText2 = s.IntroText2
                }).FirstOrDefaultAsync();

            return result;
        }

        /// <summary>
        /// Save the cover image and introduction text for the Mining Shovel Report.
        /// </summary>
        /// <param name="introParams"></param>
        /// <returns></returns>
        public int SaveReportIntroduction(IntroductionParams introParams)
        {
            int result = -1;
            int _changesSaved = 0;

            // Check that the report has been created otherwise return.
            if (introParams.ReportId == 0)
            {
                return result;
            }

            // Introduction record doesn't exist, so create it.
            if (introParams.IntroId == 0)
            {
                MININGSHOVEL_REPORT_INTRODUCTION intro = new MININGSHOVEL_REPORT_INTRODUCTION
                {
                    MiningShovelReportId = introParams.ReportId,
                    CoverImage = (introParams.CoverImage == 0) ? (int?)null : introParams.CoverImage,
                    IntroText1 = introParams.IntroText1,
                    IntroText2 = introParams.IntroText2
                };
                _context.MININGSHOVEL_REPORT_INTRODUCTION.Add(intro);
                _changesSaved = _context.SaveChanges();

                if (_changesSaved > 0)
                {
                    result = intro.Id;
                }
            }
            // Update an existing record.
            else
            {
                var intro = _context.MININGSHOVEL_REPORT_INTRODUCTION.Find(introParams.IntroId);
                if (intro != null)
                {
                    intro.CoverImage = (introParams.CoverImage == 0) ? (int?)null : introParams.CoverImage;
                    intro.IntroText1 = introParams.IntroText1;
                    intro.IntroText2 = introParams.IntroText2;
                    _changesSaved = _context.SaveChanges();

                    if (_changesSaved > 0)
                    {
                        result = intro.Id;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the Summary section for a given report.
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<SummaryParams> GetReportSummary(int reportId)
        {
            SummaryParams result = new SummaryParams();

            result = await _context.MININGSHOVEL_REPORT_SUMMARY
                .Where(w => w.MiningShovelReportId == reportId)
                .Select(s => new SummaryParams
                {
                    ReportId = s.MiningShovelReportId,
                    SummaryId = s.Id,
                    SummaryText = s.SummaryText,
                    RecommendationOverview = s.RecommendationOverview
                }).FirstOrDefaultAsync();

            return result;
        }

        /// <summary>
        /// Save the Summary section for a given report.
        /// </summary>
        /// <param name="summaryParams"></param>
        /// <returns></returns>
        public int SaveReportSummary(SummaryParams summaryParams)
        {
            int _changesSaved = -1;
            int result = -1;

            MININGSHOVEL_REPORT_SUMMARY summary = _context.MININGSHOVEL_REPORT_SUMMARY.Find(summaryParams.SummaryId);

            // Update an existing record. 
            if (summary != null)
            {
                summary.SummaryText = summaryParams.SummaryText;
                summary.RecommendationOverview = summaryParams.RecommendationOverview;

                _changesSaved = _context.SaveChanges();
                if (_changesSaved > 0)
                {
                    result = summary.Id;
                }
            }
            // Create a new record.
            else
            {
                summary = new MININGSHOVEL_REPORT_SUMMARY();
                summary.MiningShovelReportId = summaryParams.ReportId;
                summary.SummaryText = summaryParams.SummaryText;
                summary.RecommendationOverview = summaryParams.RecommendationOverview;

                _context.MININGSHOVEL_REPORT_SUMMARY.Add(summary);
                _changesSaved = _context.SaveChanges();
                if (_changesSaved > 0)
                {
                    result = summary.Id;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the General Notes for an inspection.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public string GetGeneralNotes(int MSInspectionID)
        {
            string result = _context.TRACK_INSPECTION.Find(MSInspectionID)?.GeneralNotes;
            
            return result;
        }

        /// <summary>
        /// Delete any hidden Equipment Mandatory Image for the specified photoId.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public bool DeleteHiddenEquipmentMandatoryImages(int photoId)
        {
            bool result = false;
            int _changesSaved = 0;

            var records = _context.REPORT_HIDDEN_INSPECTION_MANDATORY_IMAGES
                .Where(w => w.InspectionMandatoryImageId == photoId)
                .ToList();

            // Guard again empty resultset.
            if(records.Count == 0)
            {
                return true;
            }
            
            for(int i=0; i<records.Count; i++)
            {
                _context.REPORT_HIDDEN_INSPECTION_MANDATORY_IMAGES.Remove(records[i]);
            }

            try
            {
                _changesSaved = _context.SaveChanges();
                if (_changesSaved > 0)
                {
                    result = true;
                }
            }
            catch { }
            

            return result;
        }

        /// <summary>
        /// Delete any hidden Comparttype Mandatory Image for the specified photoId.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public bool DeleteHiddenComparttypeMandatoryImages(int photoId)
        {
            bool result = false;
            int _changesSaved = 0;

            var records = _context.REPORT_HIDDEN_COMPARTTYPE_MANDATORY_IMAGE
                .Where(w => w.InspectionCompartTypeImageId == photoId)
                .ToList();

            // Guard again empty resultset.
            if (records.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < records.Count; i++)
            {
                _context.REPORT_HIDDEN_COMPARTTYPE_MANDATORY_IMAGE.Remove(records[i]);
            }

            try
            {
                _changesSaved = _context.SaveChanges();
                if (_changesSaved > 0)
                {
                    result = true;
                }
            }
            catch { }


            return result;
        }

        /// <summary>
        /// Delete any hidden Measurement Point Image for the specified photoId.
        /// </summary>
        /// <param name="photoId"></param>
        /// <returns></returns>
        public bool DeleteHiddenMeasurementPointImages(int photoId)
        {
            bool result = false;
            int _changesSaved = 0;

            var records = _context.REPORT_HIDDEN_MEASUREPOINT_RECORD_IMAGES
                .Where(w => w.MeasurePointRecordImageId == photoId)
                .ToList();

            // Guard again empty resultset.
            if (records.Count == 0)
            {
                return true;
            }

            for (int i = 0; i < records.Count; i++)
            {
                _context.REPORT_HIDDEN_MEASUREPOINT_RECORD_IMAGES.Remove(records[i]);
            }

            try
            {
                _changesSaved = _context.SaveChanges();
                if (_changesSaved > 0)
                {
                    result = true;
                }
            }
            catch { }


            return result;
        }

        /// <summary>
        /// Removes the cover photo for the report.
        /// </summary>
        /// <param name="reportIntroductionID"></param>
        /// <returns></returns>
        public bool DeleteCoverPhoto(int reportIntroductionID)
        {
            bool result = false;
            int _changesSaved = 0;

            var record = _context.MININGSHOVEL_REPORT_INTRODUCTION.Find(reportIntroductionID);
            if(record != null)
            {
                // Guard again empty resultset.
                if (record.CoverImage == null)
                {
                    return true;
                }

                record.CoverImage = null;
                try
                {
                    _changesSaved = _context.SaveChanges();
                    if (_changesSaved > 0)
                    {
                        result = true;
                    }
                }
                catch { }
            }
            else
            {
                // Guard again empty resultset.
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Delete the specified photo from the report.
        /// </summary>
        /// <param name="reportPhotoID"></param>
        /// <returns></returns>
        public bool DeleteReportPhoto(int reportPhotoID)
        {
            bool result = false;
            int _changesSaved = 0;

            var recordTodelete = _context.MININGSHOVEL_REPORT_IMAGE_CATEGORIES.Find(reportPhotoID);
            if (recordTodelete != null)
            {
                // Delete any record for resized recommendation photos.
                var resizedPhoto = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.ImageCategories.Id == reportPhotoID).FirstOrDefault();
                if (resizedPhoto != null)
                {
                    _context.MININGSHOVEL_REPORT_IMAGE_RESIZED.Remove(resizedPhoto);
                    _changesSaved = _context.SaveChanges();
                }

                // Now proceed with deleting the report photo.
                _context.MININGSHOVEL_REPORT_IMAGE_CATEGORIES.Remove(recordTodelete);
                _changesSaved = _context.SaveChanges();

                if (_changesSaved > 0)
                {
                    result = true;
                }
            }
            else
            {
                // Guard again empty resultset.
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Delete the specified photo from the recommendation.
        /// </summary>
        /// <param name="recommendationPhotoID"></param>
        /// <returns></returns>
        public bool DeleteRecommendationPhoto(int recommendationPhotoID)
        {
            bool result = false;
            int _changesSaved = 0;

            var recordTodelete = _context.MININGSHOVEL_REPORT_IMAGE_CATEGORIES.Find(recommendationPhotoID);
            if(recordTodelete != null)
            {
                // Delete any record for resized recommendation photos.
                var resizedPhoto = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                    .Where(w => w.RecommendationImages.ImageCategoryId == recommendationPhotoID).FirstOrDefault();
                if (resizedPhoto != null)
                {
                    _context.MININGSHOVEL_REPORT_IMAGE_RESIZED.Remove(resizedPhoto);
                    _changesSaved = _context.SaveChanges();
                }

                // Now proceed with deleting the recommendation photo.
                _context.MININGSHOVEL_REPORT_IMAGE_CATEGORIES.Remove(recordTodelete);
                _changesSaved = _context.SaveChanges();

                if(_changesSaved > 0)
                {
                    result = true;
                }
            }
            else
            {
                // Guard against empty resultset.
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Delete recommendation and any linked records.
        /// </summary>
        /// <param name="recommendationID"></param>
        /// <returns></returns>
        public async Task<bool> DeleteRecommendation(int recommendationID)
        {
            bool result = false;
            int _changesSaved = 0;

            // Delete all recommendation photos.
            var recommendationPhotos = await GetRecommendationPhotos(recommendationID);
            for (int i = 0; i < recommendationPhotos.Count; i++)
            {
                var isDeleted = DeleteRecommendationPhoto(recommendationPhotos[i].id);
            }

            // Now delete the recommendation.
            var recordToDelete = _context.MININGSHOVEL_REPORT_RECOMMENDATIONS.Find(recommendationID);
            if(recordToDelete != null)
            {
                _context.MININGSHOVEL_REPORT_RECOMMENDATIONS.Remove(recordToDelete);
                _changesSaved = _context.SaveChanges();

                if(_changesSaved > 0)
                {
                    result = true;
                }
            }
            else
            {
                // Guard against empty resultset.
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Toggle show or hide for the inspection mandatory photos.
        /// </summary>
        /// <param name="photoId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<ToggleHideAndShowResult> ToggleInspectionMandatoryPhoto(int photoId, int reportId)
        {
            bool result = false;
            int _changesSaved = 0;

            // Check for an existing record.
            var imgHidden = await _context.REPORT_HIDDEN_INSPECTION_MANDATORY_IMAGES
                .Where(w => w.InspectionMandatoryImageId == photoId && w.ReportId == reportId)
                .FirstOrDefaultAsync();

            // Record exists, so delete it to show the photo on the report.
            if (imgHidden != null)
            {
                _context.REPORT_HIDDEN_INSPECTION_MANDATORY_IMAGES.Remove(imgHidden);
            }
            // No record exists, so create a new entry to hide the photo on the report.
            else
            {
                REPORT_HIDDEN_INSPECTION_MANDATORY_IMAGES photoToHide = new REPORT_HIDDEN_INSPECTION_MANDATORY_IMAGES();
                photoToHide.InspectionMandatoryImageId = photoId;
                photoToHide.ReportId = reportId;
                _context.REPORT_HIDDEN_INSPECTION_MANDATORY_IMAGES.Add(photoToHide);
            }

            _changesSaved = _context.SaveChanges();
            if(_changesSaved > 0)
            {
                result = true;
            }

            return new ToggleHideAndShowResult {  Id = photoId , IsHiding = imgHidden==null , SavedResult = result};
        }

        /// <summary>
        /// Toggle show or hide for the comparttype mandatory photos.
        /// </summary>
        /// <param name="photoId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<ToggleHideAndShowResult> ToggleComparttypeMandatoryPhoto(int photoId, int reportId)
        {
            bool result = false;
            int _changesSaved = 0;

            // Check for an existing record.
            var imgHidden = await _context.REPORT_HIDDEN_COMPARTTYPE_MANDATORY_IMAGE
                .Where(w => w.InspectionCompartTypeImageId == photoId && w.ReportId == reportId)
                .FirstOrDefaultAsync();

            // Record exists, so delete it to show the photo on the report.
            if (imgHidden != null)
            {
                _context.REPORT_HIDDEN_COMPARTTYPE_MANDATORY_IMAGE.Remove(imgHidden);
            }
            // No record exists, so create a new entry to hide the photo on the report.
            else
            {
                REPORT_HIDDEN_COMPARTTYPE_MANDATORY_IMAGE photoToHide = new REPORT_HIDDEN_COMPARTTYPE_MANDATORY_IMAGE();
                photoToHide.InspectionCompartTypeImageId = photoId;
                photoToHide.ReportId = reportId;
                _context.REPORT_HIDDEN_COMPARTTYPE_MANDATORY_IMAGE.Add(photoToHide);
            }

            _changesSaved = _context.SaveChanges();
            if (_changesSaved > 0)
            {
                result = true;
            }

            return new ToggleHideAndShowResult { Id = photoId, IsHiding = imgHidden== null, SavedResult = result };
        }

        /// <summary>
        /// Toggle show or hide for the comparttype additional photos.
        /// </summary>
        /// <param name="photoId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<ToggleHideAndShowResult> ToggleComparttypeAdditionalPhoto(int photoId, int reportId)
        {
            bool result = false;
            int _changesSaved = 0;

            // Check for an existing record.
            var imgHidden = await _context.REPORT_HIDDEN_ADDITIONAL_RECORD_IMAGE
                .Where(w => w.CompartTypeRecordImageId == photoId && w.ReportId == reportId)
                .FirstOrDefaultAsync();

            // Record exists, so delete it to show the photo on the report.
            if (imgHidden != null)
            {
                _context.REPORT_HIDDEN_ADDITIONAL_RECORD_IMAGE.Remove(imgHidden);
            }
            // No record exists, so create a new entry to hide the photo on the report.
            else
            {
                REPORT_HIDDEN_ADDITIONAL_RECORD_IMAGE photoToHide = new REPORT_HIDDEN_ADDITIONAL_RECORD_IMAGE();
                photoToHide.CompartTypeRecordImageId = photoId;
                photoToHide.ReportId = reportId;
                _context.REPORT_HIDDEN_ADDITIONAL_RECORD_IMAGE.Add(photoToHide);
            }

            _changesSaved = _context.SaveChanges();
            if (_changesSaved > 0)
            {
                result = true;
            }

            return new ToggleHideAndShowResult { Id = photoId, IsHiding = imgHidden == null, SavedResult = result };
        }

        /// <summary>
        /// Toggle show or hide for measurement point photos.
        /// </summary>
        /// <param name="photoId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<ToggleHideAndShowResult> ToggleMeasurementPointPhoto(int photoId, int reportId, bool status)
        {
            bool result = false;
            int _changesSaved = 0;

            // Check for an existing record.
            var imgHidden = await _context.REPORT_HIDDEN_MEASUREPOINT_RECORD_IMAGES
                .Where(w => w.MeasurePointRecordImageId == photoId && w.ReportId == reportId)
                .FirstOrDefaultAsync();

            // Record exists, so delete it to show the photo on the report.
            if (imgHidden != null)//is not hiding
            {
                _context.REPORT_HIDDEN_MEASUREPOINT_RECORD_IMAGES.Remove(imgHidden);
            }
            // No record exists, so create a new entry to hide the photo on the report.
            else
            {
                REPORT_HIDDEN_MEASUREPOINT_RECORD_IMAGES photoToHide = new REPORT_HIDDEN_MEASUREPOINT_RECORD_IMAGES();
                photoToHide.MeasurePointRecordImageId = photoId;
                photoToHide.ReportId = reportId;
                _context.REPORT_HIDDEN_MEASUREPOINT_RECORD_IMAGES.Add(photoToHide);
            }

            _changesSaved = _context.SaveChanges();
            if (_changesSaved > 0)
            {
                result = true;
            }

            return new ToggleHideAndShowResult { Id = photoId, IsHiding = imgHidden == null, SavedResult = result };
        }

        /// <summary>
        /// Toggle show or hide for the measurement point records.
        /// </summary>
        /// <param name="measurePointID"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<ToggleHideAndShowResult> ToggleMeasurementPointRecord(int measurePointID, int reportId)
        {
            bool result = false;
            int _changesSaved = 0;
            var isHiding = false;
            // Override for Track Shoe Pitch
            if(CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TRACK_SHOE_PITCH_ID)
            {
                // Check for existing record.
                var dataHidden1 = await _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD
                    .Where(w => w.CompartMeasurementPointId == TrackShoePitch_1Shoe_ID
                        && w.ReportId == reportId)
                    .FirstOrDefaultAsync();
                var dataHidden2 = await _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD
                    .Where(w => w.CompartMeasurementPointId == TrackShoePitch_10Shoes_ID
                        && w.ReportId == reportId)
                    .FirstOrDefaultAsync();

                // 1-Shoe
                // Record exists, so delete it to show the readings on the report.
                if (dataHidden1 != null)
                {
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Remove(dataHidden1);
                }
                // No record exists, so create a new entry to hide the data on the report.
                else
                {
                    REPORT_HIDDEN_MEASUREMENT_POINT_RECORD dataToHide1 = new REPORT_HIDDEN_MEASUREMENT_POINT_RECORD();
                    dataToHide1.CompartMeasurementPointId = TrackShoePitch_1Shoe_ID;
                    dataToHide1.ReportId = reportId;
                    isHiding = true;
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Add(dataToHide1);
                }

                // 10-Shoes
                // Record exists, so delete it to show the readings on the report.
                if (dataHidden2 != null)
                {
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Remove(dataHidden2);
                }
                // No record exists, so create a new entry to hide the data on the report.
                else
                {
                    REPORT_HIDDEN_MEASUREMENT_POINT_RECORD dataToHide2 = new REPORT_HIDDEN_MEASUREMENT_POINT_RECORD();
                    dataToHide2.CompartMeasurementPointId = TrackShoePitch_10Shoes_ID;
                    dataToHide2.ReportId = reportId;
                    isHiding = true;
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Add(dataToHide2);
                }
            }

            // Handle all other records normally.
            else
            {
                // Check for an existing record.
                var dataHidden = await _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD
                    .Where(w => w.CompartMeasurementPointId == measurePointID
                        && w.ReportId == reportId)
                    .FirstOrDefaultAsync();

                // Record exists, so delete it to show the readings on the report.
                if (dataHidden != null)
                {
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Remove(dataHidden);
                }
                // No record exists, so create a new entry to hide the data on the report.
                else
                {
                    REPORT_HIDDEN_MEASUREMENT_POINT_RECORD dataToHide = new REPORT_HIDDEN_MEASUREMENT_POINT_RECORD();
                    dataToHide.CompartMeasurementPointId = measurePointID;
                    dataToHide.ReportId = reportId;
                    isHiding = true;
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Add(dataToHide);
                }
            }

            _changesSaved = _context.SaveChanges();
            if (_changesSaved > 0)
            {
                result = true;
            }
            return new ToggleHideAndShowResult {  Id  = measurePointID, IsHiding = isHiding ,SavedResult  = result};
        }

        /// <summary>
        /// Toggle show or hide for the measurement point.
        /// </summary>
        /// <param name="measurePointID"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<ToggleHideAndShowResult> ToggleMeasurementPoint(int measurePointID, int reportId)
        {
            bool result = false;
            int _changesSaved = 0;
            var isHiding = false;
            // Override for Track Shoe Pitch
            if (CONSOLIDATE_TRACK_SHOE_PITCH_DATA && measurePointID == TRACK_SHOE_PITCH_ID)
            {
                // Check for existing record.
                var dataHidden1 = await _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD
                    .Where(w => w.CompartMeasurementPointId == TrackShoePitch_1Shoe_ID
                        && w.ReportId == reportId)
                    .FirstOrDefaultAsync();
                var dataHidden2 = await _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD
                    .Where(w => w.CompartMeasurementPointId == TrackShoePitch_10Shoes_ID
                        && w.ReportId == reportId)
                    .FirstOrDefaultAsync();

                // 1-Shoe
                // Record exists, so delete it to show the MP on the report.
                if (dataHidden1 != null)
                {
                    // Only toggle MP section.
                    if (dataHidden1.hideAll && !dataHidden1.hideReadings)
                    {
                        _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Remove(dataHidden1);
                    }
                    // Show the MP section, but leave the readings visibility as is.
                    else
                    {
                        //dataHidden1.hideReadings = false;
                        dataHidden1.hideAll = false;
                    }
                    
                }
                // No record exists, so create a new entry to hide the data on the report.
                else
                {
                    REPORT_HIDDEN_MEASUREMENT_POINT_RECORD dataToHide1 = new REPORT_HIDDEN_MEASUREMENT_POINT_RECORD();
                    dataToHide1.CompartMeasurementPointId = TrackShoePitch_1Shoe_ID;
                    dataToHide1.ReportId = reportId;
                    dataToHide1.hideReadings = false;
                    dataToHide1.hideAll = true;
                    isHiding = true;
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Add(dataToHide1);
                }

                // 10-Shoes
                // Record exists, so delete it to show the MP on the report.
                if (dataHidden2 != null)
                {
                    // Only toggle MP section.
                    if (dataHidden2.hideAll && !dataHidden2.hideReadings)
                    {
                        _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Remove(dataHidden2);
                    }
                    // Show the MP section, but leave the readings visibility as is.
                    else
                    {
                        dataHidden2.hideAll = false;
                    }
                }
                // No record exists, so create a new entry to hide the data on the report.
                else
                {
                    REPORT_HIDDEN_MEASUREMENT_POINT_RECORD dataToHide2 = new REPORT_HIDDEN_MEASUREMENT_POINT_RECORD();
                    dataToHide2.CompartMeasurementPointId = TrackShoePitch_10Shoes_ID;
                    dataToHide2.ReportId = reportId;
                    dataToHide2.hideReadings = true;
                    dataToHide2.hideAll = true;
                    isHiding = true;
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Add(dataToHide2);
                }
            }

            // Handle all other records normally.
            else
            {
                // Check for an existing record.
                var dataHidden = await _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD
                    .Where(w => w.CompartMeasurementPointId == measurePointID
                        && w.ReportId == reportId)
                    .FirstOrDefaultAsync();

                // Record exists, so delete it to show the MP on the report.
                if (dataHidden != null)
                {
                    // Only toggle MP section.
                    if (dataHidden.hideAll && !dataHidden.hideReadings)
                    {
                        _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Remove(dataHidden);
                    }
                    // Show the MP section, but leave the readings visibility as is.
                    else
                    {
                        dataHidden.hideAll = false;
                    }
                }
                // No record exists, so create a new entry to hide the data on the report.
                else
                {
                    REPORT_HIDDEN_MEASUREMENT_POINT_RECORD dataToHide = new REPORT_HIDDEN_MEASUREMENT_POINT_RECORD();
                    dataToHide.CompartMeasurementPointId = measurePointID;
                    dataToHide.ReportId = reportId;
                    dataToHide.hideReadings = false;
                    dataToHide.hideAll = true;
                    isHiding = true;
                    _context.REPORT_HIDDEN_MEASUREMENT_POINT_RECORD.Add(dataToHide);
                }
            }

            _changesSaved = _context.SaveChanges();
            if (_changesSaved > 0)
            {
                result = true;
            }

            return new ToggleHideAndShowResult { Id= measurePointID , IsHiding = isHiding, SavedResult = result };
        }

        /// <summary>
        /// Toggle show or hide for the additional records.
        /// </summary>
        /// <param name="compartTypeID"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<ToggleHideAndShowResult> ToggleComparttypeAdditionalRecord(int compartTypeID, int reportId)
        {
            bool result = false;
            int _changesSaved = 0;

            // Check for an existing record.
            var recordHidden = await _context.REPORT_HIDDEN_ADDITIONAL_RECORD
                .Where(w => w.CompartTypeId == compartTypeID && w.ReportId == reportId)
                .FirstOrDefaultAsync();

            // Record exists, so delete it to show the data on the report.
            if (recordHidden != null)
            {
                _context.REPORT_HIDDEN_ADDITIONAL_RECORD.Remove(recordHidden);
            }
            // No record exists, so create a new entry to hide the data on the report.
            else
            {
                REPORT_HIDDEN_ADDITIONAL_RECORD dataToHide = new REPORT_HIDDEN_ADDITIONAL_RECORD();
                dataToHide.CompartTypeId = compartTypeID;
                dataToHide.ReportId = reportId;
                _context.REPORT_HIDDEN_ADDITIONAL_RECORD.Add(dataToHide);
            }

            _changesSaved = _context.SaveChanges();
            if (_changesSaved > 0)
            {
                result = true;
            }

            return new ToggleHideAndShowResult {  Id = compartTypeID  , SavedResult = result, IsHiding = recordHidden ==null};
        }

        /// <summary>
        /// Checks the status of the additional record, whether it is hidden or not on the report.
        /// </summary>
        /// <param name="compartTypeID"></param>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<bool> GetStatusOfAdditionalRecord(int compartTypeID, int MSInspectionID)
        {
            // Default status is to show.
            bool isHidden = false;

            // Check for an existing record.
            var recordHidden = await _context.REPORT_HIDDEN_ADDITIONAL_RECORD
                .Where(w => w.CompartTypeId == compartTypeID && w.Report.InspectionId == MSInspectionID)
                .FirstOrDefaultAsync();
            if (recordHidden != null)
            {
                isHidden = true;
            }

            return isHidden;
        }

        /// <summary>
        /// Create a new record in the MININGSHOVEL_REPORT_IMAGE_RESIZED table if images are 
        /// enlarged on the report.
        /// </summary>
        /// <param name="isLarge"></param>
        /// <param name="imageCategoryId"></param>
        /// <returns></returns>
        public bool ToggleRecommendationImageResize(bool isLarge, int imageCategoryId)
        {
            bool result = false;
            int _changesSaved = 0;

            // Check for an existing record.
            var existingRecord = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                .Where(w => w.RecommendationImages.ImageCategoryId == imageCategoryId)
                .FirstOrDefault();

            // The recommendation image already has a record in the 'resized' table.
            if (existingRecord != null)
            {
                if (isLarge)
                {
                    // Do nothing.
                }
                else
                {
                    _context.MININGSHOVEL_REPORT_IMAGE_RESIZED.Remove(existingRecord);
                }
            }

            // A record does not already exist in the IMAGE_RESIZED table.
            else
            {
                // Create a new record for the enlarged image.
                if (isLarge)
                {
                    var recommendationImage = _context.MININGSHOVEL_REPORT_RECOMMENDATION_IMAGES
                        .Where(w => w.ImageCategoryId == imageCategoryId).FirstOrDefault();
                    if(recommendationImage != null)
                    {
                        int reportId = recommendationImage.MiningShovelReportRecommendation.MiningShovelReportId;
                        int recommendationImageId = recommendationImage.Id;

                        MININGSHOVEL_REPORT_IMAGE_RESIZED newRecord = new MININGSHOVEL_REPORT_IMAGE_RESIZED();
                        newRecord.RecommendationImageId = recommendationImageId;
                        newRecord.ReportId = reportId;
                        _context.MININGSHOVEL_REPORT_IMAGE_RESIZED.Add(newRecord);
                    }
                }
            }

            _changesSaved = _context.SaveChanges();
            if (_changesSaved > 0)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Create a new record in the MININGSHOVEL_REPORT_IMAGE_RESIZED table if images are 
        /// enlarged on the report.
        /// </summary>
        /// <param name="isLarge"></param>
        /// <param name="photoId"></param>
        /// <param name="photoType"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public bool ToggleReportImageResize(bool isLarge, int photoId, int photoType, int reportId)
        {
            bool result = false;
            int _changesSaved = 0;

            // Check for an existing record.
            MININGSHOVEL_REPORT_IMAGE_RESIZED existingRecord = new MININGSHOVEL_REPORT_IMAGE_RESIZED();
            if (photoType == (int)InspectionPhotoType.Inspection_Mandatory_Photo)
            {
                existingRecord = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                .Where(w => w.ImageCategories.Inspection_Mandatory_Image == photoId && w.ReportId == reportId)
                .FirstOrDefault();
            }
            else if (photoType == (int)InspectionPhotoType.Comparttype_Mandatory_Photo)
            {
                existingRecord = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                .Where(w => w.ImageCategories.Comparttype_Mandatory_Image == photoId && w.ReportId == reportId)
                .FirstOrDefault();
            }
            else if (photoType == (int)InspectionPhotoType.Comparttype_Additional_Photo)
            {
                existingRecord = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                .Where(w => w.ImageCategories.Comparttype_Additional_Image == photoId && w.ReportId == reportId)
                .FirstOrDefault();
            }
            else if (photoType == (int)InspectionPhotoType.Measurement_Point_Photo)
            {
                existingRecord = _context.MININGSHOVEL_REPORT_IMAGE_RESIZED
                .Where(w => w.ImageCategories.Measurement_Point_Image == photoId && w.ReportId == reportId)
                .FirstOrDefault();
            }

            // The recommendation image already has a record in the 'resized' table.
            if (existingRecord != null)
            {
                if (isLarge)
                {
                    // Do nothing.
                }
                else
                {
                    // Remove the corresponding entry in the IMAGE_CATEGORIES table.
                    var reportImg = _context.MININGSHOVEL_REPORT_IMAGE_CATEGORIES.Find(existingRecord.ReportImageId);
                    if(reportImg != null)
                    {
                        _context.MININGSHOVEL_REPORT_IMAGE_CATEGORIES.Remove(reportImg);
                        _context.SaveChanges();
                    }

                    // Remove the entry in the IMAGE_RESIZED table.
                    _context.MININGSHOVEL_REPORT_IMAGE_RESIZED.Remove(existingRecord);
                }
            }

            // A record does not already exist in the IMAGE_RESIZED table.
            else
            {
                // Create a new record for the enlarged image.
                if (isLarge)
                {
                    MININGSHOVEL_REPORT_IMAGE_CATEGORIES reportImage = new MININGSHOVEL_REPORT_IMAGE_CATEGORIES();
                    if(photoType == (int)InspectionPhotoType.Inspection_Mandatory_Photo)
                    {
                        reportImage.Inspection_Mandatory_Image = photoId;
                    }
                    else if (photoType == (int)InspectionPhotoType.Comparttype_Mandatory_Photo)
                    {
                        reportImage.Comparttype_Mandatory_Image = photoId;
                    }
                    else if (photoType == (int)InspectionPhotoType.Comparttype_Additional_Photo)
                    {
                        reportImage.Comparttype_Additional_Image = photoId;
                    }
                    else if (photoType == (int)InspectionPhotoType.Measurement_Point_Photo)
                    {
                        reportImage.Measurement_Point_Image = photoId;
                    }
                    _context.MININGSHOVEL_REPORT_IMAGE_CATEGORIES.Add(reportImage);
                    _context.SaveChanges();

                    if (reportImage != null)
                    {
                        int reportImageId = reportImage.Id;

                        MININGSHOVEL_REPORT_IMAGE_RESIZED newRecord = new MININGSHOVEL_REPORT_IMAGE_RESIZED();
                        newRecord.ReportImageId = reportImageId;
                        newRecord.ReportId = reportId;
                        _context.MININGSHOVEL_REPORT_IMAGE_RESIZED.Add(newRecord);
                    }
                }
            }

            _changesSaved = _context.SaveChanges();
            if (_changesSaved > 0)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Gets the Axial Gap calculation for the Idler measurements.
        /// </summary>
        /// <param name="MSInspectionID"></param>
        /// <returns></returns>
        public async Task<List<AdditionalRecordDetails>> GetAxialGapCalculations(int MSInspectionID)
        {
            List<AdditionalRecordDetails> result = new List<AdditionalRecordDetails>();

            string sInboardAxialGap_AB = "Inboard Axial Gap = A+B";
            string sOutboardAxialGap_CD = "Outboard Axial Gap = C+D";
            string sTotalAxialGap = "Total Axial Gap = A+B+C+D";

            string inboardKeeperA = "Inboard Keeper Face to Outside Thrust Washer Face (A)";
            string inboardAdjustmentB = "Inboard Adjustment Block Face to Outside Crawler Frame Face (B)";
            string outboardKeeperC = "Outboard Keeper Face to Outside Thrust Washer Face (C)";
            string outboardAdjustmentD = "Outboard Adjustment Block Face to Outside Crawler Frame Face (D)";


            var resultsA = await _context.MEASUREMENT_POINT_RECORD
                .Where(w => w.CompartMeasurePoint.Name == inboardKeeperA
                    && w.InspectionDetail.inspection_auto == MSInspectionID)
                .Join(_context.TRACK_INSPECTION_DETAIL,
                    mpr => mpr.InspectionDetailId,
                    ids => ids.inspection_detail_auto,
                    (mpr, ids) => new { Measurements = mpr, Details = ids })
                .OrderBy(m => m.Measurements.MeasureNumber)
                .ThenBy(s => s.Details.GENERAL_EQ_UNIT.side)
                .ToListAsync();

            var resultsB = await _context.MEASUREMENT_POINT_RECORD
                .Where(w => w.CompartMeasurePoint.Name == inboardAdjustmentB
                    && w.InspectionDetail.inspection_auto == MSInspectionID)
                .Join(_context.TRACK_INSPECTION_DETAIL,
                    mpr => mpr.InspectionDetailId,
                    ids => ids.inspection_detail_auto,
                    (mpr, ids) => new { Measurements = mpr, Details = ids })
                .OrderBy(m => m.Measurements.MeasureNumber)
                .ThenBy(s => s.Details.GENERAL_EQ_UNIT.side)
                .ToListAsync();

            var resultsC = await _context.MEASUREMENT_POINT_RECORD
                .Where(w => w.CompartMeasurePoint.Name == outboardKeeperC
                    && w.InspectionDetail.inspection_auto == MSInspectionID)
                .Join(_context.TRACK_INSPECTION_DETAIL,
                    mpr => mpr.InspectionDetailId,
                    ids => ids.inspection_detail_auto,
                    (mpr, ids) => new { Measurements = mpr, Details = ids })
                .OrderBy(m => m.Measurements.MeasureNumber)
                .ThenBy(s => s.Details.GENERAL_EQ_UNIT.side)
                .ToListAsync();

            var resultsD = await _context.MEASUREMENT_POINT_RECORD
                .Where(w => w.CompartMeasurePoint.Name == outboardAdjustmentD
                    && w.InspectionDetail.inspection_auto == MSInspectionID)
                .Join(_context.TRACK_INSPECTION_DETAIL,
                    mpr => mpr.InspectionDetailId,
                    ids => ids.inspection_detail_auto,
                    (mpr, ids) => new { Measurements = mpr, Details = ids })
                .OrderBy(m => m.Measurements.MeasureNumber)
                .ThenBy(s => s.Details.GENERAL_EQ_UNIT.side)
                .ToListAsync();

            // A + B
            AdditionalRecordDetails iAB = new AdditionalRecordDetails();
            iAB.Title = sInboardAxialGap_AB;

            // A
            for(int i=0; i<resultsA.Count; i++)
            {
                var reading = resultsA[i].Measurements.Reading;

                // Left or Right side.
                if (resultsA[i].Details.GENERAL_EQ_UNIT.side == 1)
                {
                    iAB.ReadingL = reading.ToString();
                }
                else
                {
                    iAB.ReadingR = reading.ToString();
                }

                iAB.Tool = resultsA[i].Measurements.Tool.tool_name;
            }

            // B
            for (int i = 0; i < resultsB.Count; i++)
            {
                var reading = resultsB[i].Measurements.Reading;

                // Left or Right side.
                if (resultsB[i].Details.GENERAL_EQ_UNIT.side == 1)
                {
                    decimal iOrig = decimal.TryParse(iAB.ReadingL, out iOrig) ? iOrig : 0;
                    iAB.ReadingL = (iOrig + reading).ToString();
                }
                else
                {
                    decimal iOrig = decimal.TryParse(iAB.ReadingR, out iOrig) ? iOrig : 0;
                    iAB.ReadingR = (iOrig + reading).ToString();
                }

                iAB.Tool = resultsB[i].Measurements.Tool.tool_name;
            }

            // C + D
            AdditionalRecordDetails oCD = new AdditionalRecordDetails();
            oCD.Title = sOutboardAxialGap_CD;

            // C
            for (int i = 0; i < resultsC.Count; i++)
            {
                var reading = resultsC[i].Measurements.Reading;

                // Left or Right side.
                if (resultsC[i].Details.GENERAL_EQ_UNIT.side == 1)
                {
                    oCD.ReadingL = reading.ToString();
                }
                else
                {
                    oCD.ReadingR = reading.ToString();
                }

                oCD.Tool = resultsC[i].Measurements.Tool.tool_name;
            }

            // D
            for (int i = 0; i < resultsD.Count; i++)
            {
                var reading = resultsD[i].Measurements.Reading;

                // Left or Right side.
                if (resultsD[i].Details.GENERAL_EQ_UNIT.side == 1)
                {
                    decimal iOrig = decimal.TryParse(oCD.ReadingL, out iOrig) ? iOrig : 0;
                    oCD.ReadingL = (iOrig + reading).ToString();
                }
                else
                {
                    decimal iOrig = decimal.TryParse(oCD.ReadingR, out iOrig) ? iOrig : 0;
                    oCD.ReadingR = (iOrig + reading).ToString();
                }

                oCD.Tool = resultsD[i].Measurements.Tool.tool_name;
            }

            // A + B + C + D
            AdditionalRecordDetails tABCD = new AdditionalRecordDetails();
            tABCD.Title = sTotalAxialGap;
            tABCD.Tool = iAB.Tool;

            decimal iOrig1L = decimal.TryParse(iAB.ReadingL, out iOrig1L) ? iOrig1L : 0;
            decimal iOrig1R = decimal.TryParse(iAB.ReadingR, out iOrig1R) ? iOrig1R : 0;
            decimal iOrig2L = decimal.TryParse(oCD.ReadingL, out iOrig2L) ? iOrig2L : 0;
            decimal iOrig2R = decimal.TryParse(oCD.ReadingR, out iOrig2R) ? iOrig2R : 0;

            tABCD.ReadingL = (iOrig1L + iOrig2L).ToString();
            tABCD.ReadingR = (iOrig1R + iOrig2R).ToString();

            result.Add(iAB);
            result.Add(oCD);
            result.Add(tABCD);

            return result;
        }

        /// <summary>
        /// Returns the overall comments based on the report Id and comparttype Id.
        /// </summary>
        /// <param name="reportId"></param>
        /// <returns></returns>
        public async Task<OverallComments> GetOverallComments(int MSInspectionID, int compartTypeID)
        {
            OverallComments result = new OverallComments();

            result = await _context.MININGSHOVEL_REPORT_OVERALL_COMMENTS
                .Where(w => w.MiningShovelReport.InspectionId == MSInspectionID && w.CompartTypeId == compartTypeID)
                .Select(s => new OverallComments
                {
                    ReportId = s.MiningShovelReportId,
                    Id = s.Id,
                    Comments = s.Comments,
                    CompartTypeId = s.CompartTypeId
                }).FirstOrDefaultAsync();

            return result;
        }

        /// <summary>
        /// Method to save the overall comments for a given comparttype Id and report.
        /// </summary>
        /// <param name="commentsToSave"></param>
        /// <returns></returns>
        public int SaveOverallComments(OverallComments commentsToSave)
        {
            int result = -1;
            int _changesSaved = 0;

            // Guard
            if(commentsToSave.CompartTypeId <= 0 || commentsToSave.ReportId <= 0)
            {
                return result;
            }

            // Record doesn't exist, so create it.
            if (commentsToSave.Id == 0)
            {
                MININGSHOVEL_REPORT_OVERALL_COMMENTS newRecord = new MININGSHOVEL_REPORT_OVERALL_COMMENTS
                {
                    MiningShovelReportId = commentsToSave.ReportId,
                    CompartTypeId = commentsToSave.CompartTypeId,
                    Comments = commentsToSave.Comments
                };
                _context.MININGSHOVEL_REPORT_OVERALL_COMMENTS.Add(newRecord);
                _changesSaved = _context.SaveChanges();

                if (_changesSaved > 0)
                {
                    result = newRecord.Id;
                }
            }
            // Update an existing record.
            else
            {
                var existingRecord = _context.MININGSHOVEL_REPORT_OVERALL_COMMENTS.Find(commentsToSave.Id);
                if (existingRecord != null)
                {
                    existingRecord.Comments = commentsToSave.Comments;
                    _changesSaved = _context.SaveChanges();

                    if (_changesSaved > 0)
                    {
                        result = existingRecord.Id;
                    }
                }
            }

            return result;
        }
    }
}
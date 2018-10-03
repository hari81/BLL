using BLL.Core.Audit;
using BLL.Core.ViewModel;
using BLL.Extensions;
using BLL.Services;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.Domain
{
    /// <summary>
    /// All interactions related to an undercarriage interpretation are defined within
    /// this class. 
    /// </summary>
    public class Interpretation
    {
        private UndercarriageContext _context;
        private TRACK_INSPECTION _inspection;
        private InterpretationAuditor _logger;
        private USER_TABLE _user;

        /// <summary>
        /// Initialize the interpretation. 
        /// </summary>
        /// <param name="undercarriageContext">The undercarriage database context. </param>
        /// <param name="inspectionId">The inspection Id related to this interpretation. </param>
        /// <param name="userId">The user Id to use for the audit logger. </param>
        public Interpretation(UndercarriageContext undercarriageContext, int inspectionId, long userId)
        {
            _context = undercarriageContext;
            _inspection = _context.TRACK_INSPECTION.Find(inspectionId);
            _logger = new InterpretationAuditor(_context, inspectionId, userId);
            _user = _context.USER_TABLE.Find(userId);
        }

        /// <summary>
        /// Gets the overall inspection evaluation. If the eval is null it will return "U" for unknown. 
        /// </summary>
        /// <returns>Overall inspection eval. </returns>
        public string GetOverallEval()
        {
            return _inspection.evalcode ?? "U";
        }

        /// <summary>
        /// Updates the overall eval of the inspection. 
        /// </summary>
        /// <param name="newEval">The new eval to set the inspection to. </param>
        /// <returns>A tuple with 2 values. The first will be true if successful, the second is a message of the result. </returns>
        public Tuple<bool, string> SetOverallEval(string newEval)
        {
            _inspection.evalcode = newEval;
            try
            {
                _logger.LogOverallEvalChange(newEval);
                _context.SaveChanges();
                return Tuple.Create(true, "Overall eval code updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Gets the interpretation overview information. This is displayed at the top of the interpretation page,
        /// and contains general information about the equipment at the time of the inspection. 
        /// </summary>
        /// <returns></returns>
        public InterpretationOverviewModel GetInterpretationOverview()
        {
            var equipment = new Core.Domain.Equipment(_context, (int)_inspection.EQUIPMENT.equipmentid_auto);
            return new InterpretationOverviewModel()
            {
                InspectionId = _inspection.inspection_auto,
                InspectionDate = _inspection.inspection_date,
                InspectorName = _inspection.examiner,
                Released = _inspection.released_date == null ? false : true,
                Equipment = new EquipmentModel()
                {
                    EquipmentId = _inspection.EQUIPMENT.equipmentid_auto,
                    SerialNumber = _inspection.EQUIPMENT.serialno,
                    UnitNumber = _inspection.EQUIPMENT.unitno,
                    SMU = _inspection.smu ?? 0,
                    LTD = _inspection.ltd ?? 0,
                    CustomerName = _inspection.EQUIPMENT.Jobsite.Customer.cust_name,
                    JobsiteName = _inspection.EQUIPMENT.Jobsite.site_name,
                    Family = _inspection.EQUIPMENT.LU_MMTA.TYPE.typedesc,
                    Make = _inspection.EQUIPMENT.LU_MMTA.MAKE.makedesc,
                    Model = _inspection.EQUIPMENT.LU_MMTA.MODEL.modeldesc,
                    EquipmentPhoto = "data:image/png;base64," + Convert.ToBase64String(equipment.GetEquipmentImage()),
                    //TravelledKms = _inspection.TravelledKms,
                    //ForwardTravel = _inspection.ForwardTravelHours,
                    //ReverseTravel = _inspection.ReverseTravelHours,

                    ForwardTravelHrs = _inspection.ForwardTravelHours,
                    ForwardTravelKM = _inspection.ForwardTravelKm,
                    ReverseTravelHrs = _inspection.ReverseTravelHours,
                    ReverseTravelKM = _inspection.ReverseTravelKm,

                    TrackSagL = _inspection.track_sag_left ?? 0,
                    TrackSagR = _inspection.track_sag_right ?? 0,
                    TrackSagCommentL = _inspection.LeftTrackSagComment ?? "",
                    TrackSagCommentR = _inspection.RightTrackSagComment ?? "",
                    TrackSagPhotoL = getPhoto(_inspection.LeftTrackSagImage),
                    TrackSagPhotoR = getPhoto(_inspection.RightTrackSagImage),
                    DryJointsL = _inspection.dry_joints_left ?? 0,
                    DryJointsCommentsOnLeft = _inspection.LeftDryJointComments ?? "",
                    DryJointsPhotoOnLeft = getPhoto(_inspection.DryJointsLeftImage),
                    DryJointsR = _inspection.dry_joints_right ?? 0,
                    DryJointsCommentsOnRight = _inspection.RightDryJointComments ?? "",
                    DryJointsPhotoOnRight = getPhoto(_inspection.DryJointsRightImage),
                    CannonExtL = _inspection.ext_cannon_left ?? 0,
                    CannonExtR = _inspection.ext_cannon_right ?? 0,
                    CannonExtCommentL = _inspection.LeftCannonExtensionComment ?? "",
                    CannonExtCommentR = _inspection.RightCannonExtensionComment ?? "",
                    CannonExtPhotoL = getPhoto(_inspection.LeftCannonExtensionImage),
                    CannonExtPhotoR = getPhoto(_inspection.RightCannonExtensionImage),
                    ScallopL = _inspection.LeftScallopMeasurement,
                    ScallopR = _inspection.RightScallopMeasurement,
                    ScallopCommentOnLeft = _inspection.LeftScallopComments ?? "",
                    ScallopCommentOnRight = _inspection.RightScallopComments?? "",
                    ScallopPhotoOnLeft = getPhoto(_inspection.LeftScallopImage),
                    ScallopPhotoOnRight = getPhoto(_inspection.RightScallopImage),
                    
                }
            };
        }

        /// <summary>
        /// Converts the byte array photos into base 64 strings if they exist for the interp overview object. 
        /// Also resizes the photo to thumbnail size. 
        /// </summary>
        /// <param name="photo">The photo as a byte array. Eg TrackSag photo. </param>
        /// <returns>String of an image. </returns>
        private string getPhoto(byte[] photo)
        {
            return (photo != null && photo.Length > 0) ? "data:image/png;base64," + Convert.ToBase64String(ResizeImage.GetThumbnail(photo)) : "";
        }

        /// <summary>
        /// Returns the overall interpretation comment. This can be seen at the 
        /// top of the page and will also flow through to the PDF report. 
        /// </summary>
        /// <returns>Overall interpretation comment. </returns>
        public string GetOverallInterpretationComment()
        {
            return _inspection.eval_comment;
        }

        /// <summary>
        /// Updates the overall interpretation comment. 
        /// </summary>
        /// <param name="newComment">The new overall interpretation comment. </param>
        /// <returns>A tuple with 2 values. The first will be true if successful, the second is a message of the result. </returns>
        public Tuple<bool, string> SetOverallInterpretationComment(string newComment)
        {
            string oldComment = _inspection.eval_comment;
            _inspection.eval_comment = newComment;
            try
            {
                _logger.LogOverallInterpCommentChange(oldComment, newComment);
                _context.SaveChanges();
                return Tuple.Create(true, "Overall interpretation comment updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Gets the ids of the systems (chains/frames) that were on this equipment at the time of inspection. The
        /// front end page uses these id's to display a pre-loading indicator (so it doesn't have to wait for all 4 systems to
        /// load before showing something to the user). 
        /// </summary>
        /// <returns>A list of system Ids</returns>
        public List<long> GetSystemIds()
        {
            return _inspection.TRACK_INSPECTION_DETAIL
                .Where(d => d.UCSystemId != null)
                .OrderBy(m=> m.GENERAL_EQ_UNIT.UCSystem.systemTypeEnumIndex)
                .OrderBy(s => s.Side)
                .Select(d => d.UCSystemId ?? 0)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Gets a model of the given system at the time of inspection. Also includes all
        /// the components on that system. 
        /// </summary>
        /// <returns>A SystemModel object</returns>
        public SystemModel GetSystem(int systemId, long userId)
        {
            var equipment = new Core.Domain.Equipment(_context, (int)_inspection.EQUIPMENT.equipmentid_auto);
            var system = new Core.Domain.UCSystem(_context, systemId);
            var user = _context.USER_TABLE.Find(userId);
            MeasurementType uom = user.track_uom == "inch" ? MeasurementType.Inch : MeasurementType.Milimeter;

            var systemModel = new SystemModel()
            {
                Id = systemId,
                Type = system.GetSystemType(),
                SmuAtInstall = (int)(system.DALSystem.equipment_LTD_at_attachment ?? 0),
                DateInstalled = system.GetSystemDateOfInstallOnCurrentEquipment(),
                SerialNumber = system.GetSystemSerial(),
                Side = system.side
            };

            var componentEntities = _inspection.TRACK_INSPECTION_DETAIL
                .Where(d => d.UCSystemId == systemId).ToList();
            var componentModels = new List<ComponentModel>();
            componentEntities.ForEach(d =>
            {
                var component = new BLL.Core.Domain.Component(_context, (int)d.track_unit_auto);
                var photo = d.Images.FirstOrDefault();
                bool isChild = component.isAChildBasedOnCompart();
                var componentImage = component.GetComponentPhoto();
                string img = "";
                if(componentImage != null && componentImage.Length > 1)
                {
                    img = Convert.ToBase64String(componentImage);
                }
                componentModels.Add(new ComponentModel()
                {
                    ComponentId = d.track_unit_auto,
                    InspectionDetailId = d.inspection_detail_auto,
                    Cmu = d.hours_on_surface ?? 0,
                    Comment = d.comments,
                    Measurement = uom == MeasurementType.Milimeter ? d.reading : d.reading.MilimeterToInch(),
                    MeasurementType = uom,
                    Name = !isChild ? component.GetComponentDescription() : d.GENERAL_EQ_UNIT.LU_COMPART.compart,
                    ComponentTypeImage = "data:image/png;base64," + img,
                    RisidualLife100 = d.remaining_hours ?? 0,
                    RisidualLife120 = d.ext_remaining_hours ?? 0,
                    Tool = d.TRACK_TOOL.tool_name,
                    Position = component.GetPositionLabel(),
                    WornPercentage = d.worn_percentage,
                    PhotoId = photo != null ? photo.ID : -1,
                    PhotoThumbnail = photo != null ? "data:image/png;base64," + Convert.ToBase64String(ResizeImage.GetThumbnail(photo.image_data)) : "",
                    IsChild = isChild,
                    ComponentTypeId = d.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto,
                    SortOrder = d.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.sorder ?? 0
                });
            });
            
            systemModel.Components = componentModels.OrderBy(c => c.SortOrder).ThenBy(c => c.IsChild).ThenBy(c => c.ComponentId).ToList();
            return systemModel;
        }

        /// <summary>
        /// Gets a model of the given system at the time of inspection. Also includes all
        /// the components on that system. Used for the bar chart view. 
        /// </summary>
        /// <returns>A SystemModel object</returns>
        public SystemGraphModel GetSystemGraph(int systemId)
        {
            var equipment = new Core.Domain.Equipment(_context, (int)_inspection.EQUIPMENT.equipmentid_auto);
            var system = new Core.Domain.UCSystem(_context, systemId);
            var systemModel = new SystemGraphModel()
            {
                Id = systemId,
                Type = system.GetSystemType(),
                SmuAtInstall = (int)(system.DALSystem.equipment_LTD_at_attachment ?? 0),
                DateInstalled = system.GetSystemDateOfInstallOnCurrentEquipment(),
                SerialNumber = system.GetSystemSerial(),
                InspectionId = _inspection.inspection_auto,
                InspectionSmu = _inspection.smu ?? 0,
                Side = system.side
            };

            var componentEntities = _inspection.TRACK_INSPECTION_DETAIL
                .Where(d => d.UCSystemId == systemId).ToList();
            var componentModels = new List<ComponentGraphModel>();
            var quotes = _inspection.Quotes.ToList();
            componentEntities.ForEach(d =>
            {
                var component = new BLL.Core.Domain.Component(_context, (int)d.track_unit_auto);
                var photo = d.Images.FirstOrDefault();
                List<RecommendationModel> recs = new List<RecommendationModel>();
                quotes.ForEach(q =>
                {
                    recs.AddRange(q.Recommendations
                        .Where(r => r.track_unit_auto.RemoveWhitespace() == d.track_unit_auto.ToString())
                        .Select(r => new RecommendationModel()
                        {
                            RecommendationId = r.quote_detail_auto,
                            Comment = r.Comment ?? "",
                            ComponentName = component.GetComponentDescription(),
                            PartsCost = r.PartsCost ?? 0,
                            LabourCost = r.LabourCost ?? 0,
                            MiscCost = r.MiscCost ?? 0,
                            TotalCost = r.price ?? 0,
                            StartActionAtSmu = r.start_smu,
                            CompleteActionBySmu = r.end_smu,
                            ComponentId = int.Parse(r.track_unit_auto),
                            Position = component.GetPositionLabel(),
                            Side = component.GetComponentSideLabel(),
                            RecommendationName = GetRecommendationName(r.op_type_auto),
                            ActionId = r.op_type_auto,
                            QuoteId = r.quote_auto
                        }).ToList());
                });
                componentModels.Add(new ComponentGraphModel()
                {
                    ComponentId = d.track_unit_auto,
                    InspectionDetailId = d.inspection_detail_auto,
                    Cmu = d.hours_on_surface ?? 0,
                    Name = component.GetComponentDescription(),
                    ComponentTypeImage = "data:image/png;base64," + Convert.ToBase64String(component.GetComponentPhoto()),
                    RisidualLife100 = d.remaining_hours ?? 0,
                    RisidualLife120 = d.ext_remaining_hours ?? 0,
                    Position = component.GetPositionLabel(),
                    WornPercentage = d.worn_percentage,
                    SmuAtInstall = component.GetEquipmentSmuWhenComponentInstalled(),
                    CmuAtInstall = (int)(component.DALComponent.cmu ?? 0),
                    Recommendations = recs
                });
            });
            systemModel.Components = componentModels;
            return systemModel;
        }

        public Tuple<bool, string> UpdateInspectionReadingBasedOnMeasurementClass(UpdateInspectionReadingBasedOnMeasurementClassModel model)
        {
            var updatedNoticationMessage = "";
            switch ((Condition)model.Condition)
            {
                case Condition.CannonExtL:
                    _inspection.ext_cannon_left = model.Reading;
                    updatedNoticationMessage = " Cannon Ext left  reading  saved successfully. ";
                    break;
                case Condition.CannonExtR:
                    _inspection.ext_cannon_right = model.Reading;
                    updatedNoticationMessage = " Cannon Ext right  reading  saved successfully. ";
                    break;
                case Condition.DryJointsL:
                    _inspection.dry_joints_left = model.Reading;
                    updatedNoticationMessage = " Dry Joint left  reading  saved successfully. ";
                    break;
                case Condition.DryJointsR:
                    _inspection.dry_joints_right = model.Reading;
                    updatedNoticationMessage = " Dry Joint right  reading  saved successfully. ";
                    break;
                case Condition.ScallopL:
                    _inspection.LeftScallopMeasurement = model.Reading;
                    updatedNoticationMessage = " Scallop left  reading  saved successfully. ";
                    break;
                case Condition.ScallopR:
                    _inspection.RightScallopMeasurement = model.Reading;
                    updatedNoticationMessage = " Scallop right  reading  saved successfully. ";
                    break;
                case Condition.TrackSagL:
                    _inspection.track_sag_left = model.Reading;
                    updatedNoticationMessage = " Track Sag left  reading  saved successfully. ";
                    break;
                case Condition.TrackSagR:
                    _inspection.track_sag_right = model.Reading;
                    updatedNoticationMessage = " Track Sag right  reading  saved successfully. ";
                    break;
                default: break;
            }
            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, updatedNoticationMessage);
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        private string GetRecommendationName(int actionId)
        {
            return _context.TRACK_ACTION_TYPE
                .Where(a => a.action_type_auto == actionId)
                .Select(a => a.action_description)
                .FirstOrDefault() ?? "Unknown";
        }

        /// <summary>
        /// Gets a list of all existing quotes for this interpretation. Will return the
        /// quote id's and all the id's of each recommendation attached to that quote. 
        /// This method is used to display a preloading effect of possible quotes while the application loads 
        /// in more details about the recommendations attached to each quote. 
        /// </summary>
        /// <returns>A list of quotes and the ids of the recommendations attached to each quote. </returns>
        public List<QuoteOverviewModel> GetQuoteOverviews()
        {
            return _inspection.Quotes.Select(q => new QuoteOverviewModel()
            {
                QuoteId = q.quote_auto,
                RecommendationIds = q.Recommendations.Select(r => r.quote_detail_auto).ToList(),
                Recommendations = q.Recommendations.Select(r => new RecommendationModel()
                {
                    ComponentId = Int32.Parse(r.track_unit_auto.Trim())
                }).ToList()
            }).ToList();
        }

        /// <summary>
        /// Returns details for the given recommendation id. 
        /// Will return null if recommendation is not found.
        /// Will return "Unknown" in place of recommendation name if the action doesn't exist. 
        /// </summary>
        /// <param name="recommendationId">The recommendation we want to get details for. </param>
        public RecommendationModel GetRecommendation(int recommendationId)
        {
            var r = _context.TRACK_QUOTE_DETAIL.Find(recommendationId);
            if (r == null) return null;
            var recommendedAction = _context.TRACK_ACTION_TYPE.Find(r.op_type_auto);
            var component = new Component(_context, int.Parse(r.track_unit_auto));
            return new RecommendationModel()
            {
                RecommendationId = r.quote_detail_auto,
                Comment = r.Comment ?? "",
                ComponentName = component.GetComponentDescription(),
                PartsCost = r.PartsCost ?? 0,
                LabourCost = r.LabourCost ?? 0,
                MiscCost = r.MiscCost ?? 0,
                TotalCost = r.price ?? 0,
                StartActionAtSmu = r.start_smu,
                CompleteActionBySmu = r.end_smu,
                ComponentId = int.Parse(r.track_unit_auto),
                Position = component.GetPositionLabel(),
                Side = component.GetComponentSideLabel(),
                RecommendationName = recommendedAction == null ? "Unknown" : recommendedAction.action_description,
                ActionId = r.op_type_auto
            };
        }

        /// <summary>
        /// Returns the inspection detail photo for the given photo id. Will return null if
        /// that photo id can't be found. 
        /// </summary>
        /// <param name="photoId">The id of the photo you want to get. </param>
        /// <returns>A photo already formated with png as a base64 string. </returns>
        public string GetComponentPhoto(int photoId)
        {
            var p = _context.TRACK_INSPECTION_IMAGES.Find(photoId);
            if (p == null) return null;
            return "data:image/png;base64," + Convert.ToBase64String(p.image_data);
        }

        /// <summary>
        /// Returns the photo taken for the Track Sag or Cannon extension left
        /// or right sides. 
        /// </summary>
        /// <param name="condition">The type of photo you want to get. </param>
        /// <returns>A photo already formated with png as a base64 string. </returns>
        public string GetConditionPhoto(Condition condition)
        {
            byte[] p;
            switch(condition)
            {
                case Condition.TrackSagL:
                    p = _inspection.LeftTrackSagImage;
                    break;
                case Condition.TrackSagR:
                    p = _inspection.RightTrackSagImage;
                    break;
                case Condition.CannonExtL:
                    p = _inspection.LeftCannonExtensionImage;
                    break;
                case Condition.CannonExtR:
                    p = _inspection.RightCannonExtensionImage;
                    break;
                case Condition.DryJointsL:
                    p = _inspection.DryJointsLeftImage;
                    break;
                case Condition.DryJointsR:
                    p = _inspection.DryJointsRightImage;
                    break;
                case Condition.ScallopL:
                    p = _inspection.LeftScallopImage;
                    break;
                case Condition.ScallopR:
                    p = _inspection.RightScallopImage;
                    break;
                default:
                    return null;
            }
            if (p == null) return null;
            return "data:image/png;base64," + Convert.ToBase64String(p);
        }

        /// <summary>
        /// Updates the given condition type with the new photo. For example the track sag on the left
        /// side. 
        /// </summary>
        /// <param name="condition">The condition type. IE. Track Sag on the left side. </param>
        /// <param name="photo">The new photo data in base64 string format. </param>
        /// <returns>Returns a tuple, first value true if succeeded. Second value a message with the result. </returns>
        public Tuple<bool, string> UpdateConditionPhoto(Condition condition, string photo)
        {
            switch (condition)
            {
                case Condition.TrackSagL:
                    _inspection.LeftTrackSagImage = Convert.FromBase64String(photo);
                    break;
                case Condition.TrackSagR:
                    _inspection.RightTrackSagImage = Convert.FromBase64String(photo);
                    break;
                case Condition.CannonExtL:
                    _inspection.LeftCannonExtensionImage = Convert.FromBase64String(photo);
                    break;
                case Condition.CannonExtR:
                    _inspection.RightCannonExtensionImage = Convert.FromBase64String(photo);
                    break;
                case Condition.DryJointsL:
                    _inspection.DryJointsLeftImage = Convert.FromBase64String(photo);
                    break;
                case Condition.DryJointsR:
                    _inspection.DryJointsRightImage = Convert.FromBase64String(photo);
                    break;
                case Condition.ScallopL:
                    _inspection.LeftScallopImage = Convert.FromBase64String(photo);
                    break;
                case Condition.ScallopR:
                    _inspection.RightScallopImage = Convert.FromBase64String(photo);
                    break;
                default:
                    return Tuple.Create(false, "Invalid condition type. ");
            }

            try
            {
                _logger.LogConditionPhotoUpdate(condition);
                _context.SaveChanges();
                return Tuple.Create(true, "Photo saved successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the given condition type with the new comment. For example the track sag on the left
        /// side. 
        /// </summary>
        /// <param name="condition">The condition type. IE. Track Sag on the left side. </param>
        /// <param name="newComment">The new comment. </param>
        /// <returns>Returns a tuple, first value true if succeeded. Second value a message with the result. </returns>
        public Tuple<bool, string> UpdateConditionComment(Condition condition, string newComment)
        {
            string oldComment;
            switch (condition)
            {
                case Condition.TrackSagL:
                    oldComment = _inspection.LeftTrackSagComment;
                    _inspection.LeftTrackSagComment = newComment;
                    break;
                case Condition.TrackSagR:
                    oldComment = _inspection.RightTrackSagComment;
                    _inspection.RightTrackSagComment = newComment;
                    break;
                case Condition.CannonExtL:
                    oldComment = _inspection.LeftCannonExtensionComment;
                    _inspection.LeftCannonExtensionComment = newComment;
                    break;
                case Condition.CannonExtR:
                    oldComment = _inspection.RightCannonExtensionComment;
                    _inspection.RightCannonExtensionComment = newComment;
                    break;
                case Condition.DryJointsL:
                    oldComment = _inspection.LeftDryJointComments;
                    _inspection.LeftDryJointComments = newComment;
                    break;
                case Condition.DryJointsR:
                    oldComment = _inspection.RightDryJointComments;
                    _inspection.RightDryJointComments = newComment;
                    break;
                case Condition.ScallopL:
                    oldComment = _inspection.LeftScallopComments;
                    _inspection.LeftScallopComments = newComment;
                    break;
                case Condition.ScallopR:
                    oldComment = _inspection.RightScallopComments;
                    _inspection.RightScallopComments = newComment;
                    break;
                default:
                    return Tuple.Create(false, "Invalid condition type. ");
            }

            try
            {
                _logger.LogConditionCommentUpdate(condition, oldComment, newComment);
                _context.SaveChanges();
                return Tuple.Create(true, "Comment saved successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Deletes the given component inspection photo id from the database. 
        /// Also logs that the photo was deleted. 
        /// </summary>
        /// <param name="condition">The condition photo to delete. </param>
        /// <returns>Tuple, first value true if deleted successfully. Second value is a message
        /// of the result. </returns>
        public Tuple<bool, string> DeleteConditionPhoto(Condition condition)
        {
            switch (condition)
            {
                case Condition.TrackSagL:
                    _inspection.LeftTrackSagImage = null;
                    break;
                case Condition.TrackSagR:
                    _inspection.RightTrackSagImage = null;
                    break;
                case Condition.CannonExtL:
                    _inspection.LeftCannonExtensionImage = null;
                    break;
                case Condition.CannonExtR:
                    _inspection.RightCannonExtensionImage = null;
                    break;
                case Condition.DryJointsL:
                    _inspection.DryJointsLeftImage = null;
                    break;
                case Condition.DryJointsR:
                    _inspection.DryJointsRightImage = null;
                    break;
                case Condition.ScallopL:
                    _inspection.LeftScallopImage = null;
                    break;
                case Condition.ScallopR:
                    _inspection.RightScallopImage = null;
                    break;
                default:
                    return Tuple.Create(false, "Invalid condition type. ");
            }

            try
            {
                _logger.LogConditionPhotoDeleted(condition);
                _context.SaveChanges();
                return Tuple.Create(true, "Photo deleted successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the given photo id record with the new photo data passed in. Also
        /// logs that the photo has been updated. 
        /// </summary>
        /// <param name="photoId">The id of the photo to update. </param>
        /// <param name="photoData">The new photo data in base64 string format. </param>
        /// <returns>Returns a tuple, first value true if succeeded. Second value a message with the result. </returns>
        public Tuple<bool, string> UpdateComponentPhoto(int photoId, string photoData)
        {
            var p = _context.TRACK_INSPECTION_IMAGES.Find(photoId);
            if (p == null)
                return Tuple.Create(false, "Couldn't find a photo with this id. ");
            
            try
            {
                p.image_data = Convert.FromBase64String(photoData);
                _logger.LogComponentPhotoUpdate(photoId);
                _context.SaveChanges();
                return Tuple.Create(true, "Photo updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Creates a new photo record tied to the given inspection detail id. Also logs
        /// that the photo was uploaded. 
        /// </summary>
        /// <param name="inspectionDetailId">The component / inspeciton detail id this photo is of. </param>
        /// <param name="photoData">The photo data in base64 string format. </param>
        /// <returns>Returns a tuple first value is the new photos id, second value is a message of the result. 
        /// First value will be -1 if added the photo failed. </returns>
        public Tuple<long, string> AddComponentPhoto(int inspectionDetailId, string photoData)
        {
            var p = _context.TRACK_INSPECTION_DETAIL.Find(inspectionDetailId);
            var c = new Component(_context, (int)p.track_unit_auto);
            if (p == null)
                return Tuple.Create((long)-1, "Couldn't find a component inspection record with this id. ");
            var newRecord = new TRACK_INSPECTION_IMAGES()
            {
                image_data = Convert.FromBase64String(photoData),
                inspection_detail_auto = inspectionDetailId.ToString(),
                InspectionDetailId = inspectionDetailId,
                image_title = c.GetComponentDescription(),
                GUID = Guid.NewGuid()
            };

            try
            {
                _context.TRACK_INSPECTION_IMAGES.Add(newRecord);
                _logger.LogComponentPhotoUploaded((int)p.track_unit_auto);
                _context.SaveChanges();
                return Tuple.Create(newRecord.ID, "Photo uploaded successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create((long)-1, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Deletes the given component inspection photo id from the database. 
        /// Also logs that the photo was deleted. 
        /// </summary>
        /// <param name="photoId">The id of the photo to delete. </param>
        /// <returns>Tuple, first value true if deleted successfully. Second value is a message
        /// of the result. </returns>
        public Tuple<bool, string> DeleteComponentPhoto(int photoId)
        {
            var p = _context.TRACK_INSPECTION_IMAGES.Find(photoId);
            if (p == null)
                return Tuple.Create(false, "Couldn't find a photo with this id. ");

            try
            {
                _logger.LogComponentPhotoDeleted((int)p.TRACK_INSPECTION_DETAIL.track_unit_auto);
                _context.TRACK_INSPECTION_IMAGES.Remove(p);
                _context.SaveChanges();
                return Tuple.Create(true, "Photo deleted successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates the comment for a given inspection detail id. 
        /// </summary>
        /// <param name="newComment">The new overall comment. </param>
        /// <returns>A tuple with 2 values. The first will be true if successful, the second is a message of the result. </returns>
        public Tuple<bool, string> SetComponentComment(int inspectionDetailId, string newComment)
        {
            var i = _context.TRACK_INSPECTION_DETAIL.Find(inspectionDetailId);
            string oldComment = i.comments;
            i.comments = newComment;
            try
            {
                _logger.LogComponentCommentUpdate(inspectionDetailId, oldComment, newComment);
                _context.SaveChanges();
                return Tuple.Create(true, "Comment updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Gets the audit log for the current interpretation. 
        /// </summary>
        public List<AuditModel> GetAuditList()
        {
            return _context.InterpretationAudit
                .Where(a => a.InspectionId == _inspection.inspection_auto)
                .Select(a => new AuditModel()
                {
                    Date = a.EventTime,
                    EventDescription = a.Message,
                    UserEmail = a.User.email,
                    UserName = a.User.username
                })
                .OrderByDescending(a => a.Date)
                .ToList();
        }

        /// <summary>
        /// Releases the inspection so it is visible to the customer. They will
        /// now be able to access it from the dashboard. The report can now also be
        /// emailed. Also logs that the interpretation has been released and the user released it. 
        /// </summary>
        /// <returns>Tuple with first value true if successful and second value a message. </returns>
        public Tuple<bool, string> ReleaseReport(int quoteId)
        {
            _inspection.released_by = _user.username;
            _inspection.released_date = DateTime.Now;
            _inspection.last_interp_date = DateTime.Now;
            _inspection.last_interp_user = _user.username;
            _inspection.quote_auto = quoteId;

            try
            {
                _logger.LogInterpReleased();
                _context.SaveChanges();
                return Tuple.Create(true, "Report was released successfully. ");
            } catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Creates a new quote with a uniquely generated quote number. Will return the 
        /// id of the new quote if successful, or -1 if it failed. Also logs that the
        /// quote was created. 
        /// </summary>
        public Tuple<int, string> AddQuote()
        {
            var quote = new TRACK_QUOTE()
            {
                created_date = DateTime.Now,
                created_user = _user.username,
                inspection_auto = _inspection.inspection_auto,
                quote_name = "UC" + GetUniqueQuoteNumber(),
                status_auto = 1
            };
            _context.TRACK_QUOTE.Add(quote);
            try
            {
                _logger.LogQuoteCreated(quote.quote_name);
                _context.SaveChanges();
                return Tuple.Create(quote.quote_auto, "New quote created successfully. ");
            } catch (Exception e)
            {
                return Tuple.Create(-1, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Adds a new recommendation based on the given AddRecommendationModel. Will also
        /// create a new quote if there is not yet any quote to attach the recommendation to. 
        /// Logs the recommendation being added. 
        /// </summary>
        /// <param name="recommendation">Contains details about the new recommendation we're adding. 
        /// Set QuoteId to -1 if you don't have a quote created yet. </param>
        /// <returns>A tuple with the first value as the quote id, second as the new recommendation id and the third value
        /// a message. </returns>
        public Tuple<int, int, string> AddRecommendation(AddRecommendationModel recommendation)
        {
            var component = new Component(_context, recommendation.ComponentId); 
            var quote = _context.TRACK_QUOTE.Find(recommendation.QuoteId);
            var action = _context.TRACK_ACTION_TYPE.Find(recommendation.ActionId);
            if(quote == null)
            {
                var createQuoteResult = AddQuote();
                if (createQuoteResult.Item1 < 0)
                    return Tuple.Create(-1, -1, "Failed to create a new quote to add this recommendation to. ");
                quote = _context.TRACK_QUOTE.Find(createQuoteResult.Item1);
            }
            var newRecommendation = new TRACK_QUOTE_DETAIL()
            {
                Comment = recommendation.Comment,
                created_date = DateTime.Now,
                created_user = _user.username,
                end_smu = recommendation.SmuToCompleteActionBy,
                start_smu = recommendation.SmuToTakeAction,
                op_type_auto = recommendation.ActionId,
                price = recommendation.LabourCost + recommendation.MiscCost + recommendation.PartsCost,
                quote_auto = quote.quote_auto,
                track_unit_auto = recommendation.ComponentId.ToString().RemoveWhitespace(),
                PartsCost = recommendation.PartsCost,
                MiscCost = recommendation.MiscCost,
                LabourCost = recommendation.LabourCost,
                ComponentId = recommendation.ComponentId
            };
            _context.TRACK_QUOTE_DETAIL.Add(newRecommendation);
            try
            {
                _context.SaveChanges();
                _logger.LogRecommendationCreated(quote.quote_name, component.GetComponentDescription(), action.action_description, component.GetComponentSideLabel());
                return Tuple.Create(newRecommendation.quote_auto, newRecommendation.quote_detail_auto, "Recommendation added successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(-1, -1, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Updates an existing recommendation based on the given AddRecommendationModel. Logs the
        /// recommendation being updated. 
        /// </summary>
        /// <param name="recommendation">Contains details about the we're updating. </param>
        /// <returns>A tuple with the first value true if successful and the second value
        /// a message. </returns>
        public Tuple<bool, string> UpdateRecommendation(UpdateRecommendationModel recommendation)
        {
            var component = new Component(_context, recommendation.ComponentId);
            var quote = _context.TRACK_QUOTE.Find(recommendation.QuoteId);
            var action = _context.TRACK_ACTION_TYPE.Find(recommendation.ActionId);
            var rec = _context.TRACK_QUOTE_DETAIL.Find(recommendation.RecommendationId);
            rec.Comment = recommendation.Comment;
            rec.end_smu = recommendation.SmuToCompleteActionBy;
            rec.start_smu = recommendation.SmuToTakeAction;
            rec.modified_date = DateTime.Now;
            rec.modified_user = _user.username;
            //rec.op_type_auto = recommendation.ActionId;
            rec.price = recommendation.LabourCost + recommendation.MiscCost + recommendation.PartsCost;
            rec.PartsCost = recommendation.PartsCost;
            rec.MiscCost = recommendation.MiscCost;
            rec.LabourCost = recommendation.LabourCost;

            try
            {
                _context.SaveChanges();
                _logger.LogRecommendationUpdated(quote.quote_name, component.GetComponentDescription(), action.action_description, component.GetComponentSideLabel());
                return Tuple.Create(true, "Recommendation updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        private InspectionImpact ShortToImpact(short number)
        {
            switch (number)
            {
                case 1: return InspectionImpact.Normal;
                case 2:
                default:
                    return InspectionImpact.High;
            }
        }

        /// <summary>
        /// Updates the measurement for a given track inspection detail record. Will recalculate
        /// the percentage worn, eval code, remaining and projected hours, and will also update
        /// the parent component if it is a child. 
        /// </summary>
        /// <param name="m">A model of the inspection detail</param>
        /// <returns>A tuple with 2 values. The first value is true if successful, the second
        /// is a message. </returns>
        public Tuple<bool, string> UpdateComponentMeasurement(UpdateComponentMeasurementModel m)
        {
            var inspectionDetail = _context.TRACK_INSPECTION_DETAIL.Find(m.InspectionDetailId);
            var component = new Component(_context, Convert.ToInt32(inspectionDetail.track_unit_auto));

            // Calculate new worn percentage for component 
            var newWornPercentage = component.CalcWornPercentage(m.MeasurementType == MeasurementType.Milimeter ? m.Measurement.ConvertMMToInch() : m.Measurement, m.ToolId > 0 ? m.ToolId : inspectionDetail.tool_auto ?? -1, ShortToImpact(inspectionDetail.TRACK_INSPECTION.impact ?? 0));
            inspectionDetail.reading = m.MeasurementType == MeasurementType.Inch ? m.Measurement.InchToMilimeter() : m.Measurement;
            inspectionDetail.worn_percentage = newWornPercentage;
            if (m.ToolId > 0)
                inspectionDetail.tool_auto = m.ToolId;
            // Calculate new eval code
            char componentEval;
            component.GetEvalCodeByWorn(newWornPercentage, out componentEval);
            inspectionDetail.eval_code = componentEval.ToString();

            // Update projected hours and remaining hours
            // This logic was taken from the existing update inspection function in the old
            // undercarriage inspection page. 
            if (inspectionDetail.hours_on_surface != null && inspectionDetail.hours_on_surface > 0)
            {
                if (inspectionDetail.worn_percentage > 0M && inspectionDetail.worn_percentage < 120M)
                {
                    inspectionDetail.projected_hours = inspectionDetail.worn_percentage <= 100M
                        ? Convert.ToInt32(Convert.ToDecimal(inspectionDetail.hours_on_surface) * 100 /
                                          inspectionDetail.worn_percentage)
                        : Convert.ToInt32(inspectionDetail.hours_on_surface);

                    inspectionDetail.ext_projected_hours =
                        Convert.ToInt32(Convert.ToInt32(inspectionDetail.projected_hours) * 1.2);

                    inspectionDetail.remaining_hours = Convert.ToInt32(inspectionDetail.projected_hours) >=
                                               Convert.ToInt32(inspectionDetail.hours_on_surface)
                        ? Convert.ToInt32(inspectionDetail.projected_hours) -
                          Convert.ToInt32(inspectionDetail.hours_on_surface)
                        : 0;

                    inspectionDetail.ext_remaining_hours = Convert.ToInt32(inspectionDetail.ext_projected_hours) >=
                                                   Convert.ToInt32(inspectionDetail.hours_on_surface)
                        ? Convert.ToInt32(inspectionDetail.ext_projected_hours) -
                          Convert.ToInt32(inspectionDetail.hours_on_surface)
                        : 0;

                    if (inspectionDetail.worn_percentage < 30M)
                    {
                        inspectionDetail.remaining_hours = Convert.ToInt32(inspectionDetail.GENERAL_EQ_UNIT.track_budget_life) - Convert.ToInt32(inspectionDetail.hours_on_surface);
                        inspectionDetail.ext_remaining_hours = Convert.ToInt32(Convert.ToInt32(inspectionDetail.remaining_hours) * 1.2);
                        inspectionDetail.projected_hours = inspectionDetail.GENERAL_EQ_UNIT.track_budget_life;
                        inspectionDetail.ext_projected_hours =
                        Convert.ToInt32(Convert.ToInt32(inspectionDetail.projected_hours) * 1.2);
                    }
                }
                else if (inspectionDetail.worn_percentage >= 120M)
                {
                    inspectionDetail.projected_hours = inspectionDetail.GENERAL_EQ_UNIT.track_budget_life;
                    inspectionDetail.remaining_hours = 0;
                    inspectionDetail.ext_projected_hours = Convert.ToInt32(Convert.ToInt32(inspectionDetail.projected_hours) * 1.2);
                    inspectionDetail.ext_remaining_hours = 0;
                }
                else
                {
                    inspectionDetail.projected_hours = inspectionDetail.GENERAL_EQ_UNIT.track_budget_life;
                    inspectionDetail.remaining_hours = inspectionDetail.GENERAL_EQ_UNIT.track_budget_life;
                    inspectionDetail.ext_projected_hours = Convert.ToInt32(Convert.ToInt32(inspectionDetail.projected_hours) * 1.2);
                    inspectionDetail.ext_remaining_hours = Convert.ToInt32(Convert.ToInt32(inspectionDetail.remaining_hours) * 1.2);
                }
            }
            else if (inspectionDetail.hours_on_surface != null && inspectionDetail.hours_on_surface == 0)
            {
                inspectionDetail.projected_hours = inspectionDetail.GENERAL_EQ_UNIT.track_budget_life;
                inspectionDetail.remaining_hours = inspectionDetail.GENERAL_EQ_UNIT.track_budget_life;
                inspectionDetail.ext_projected_hours = Convert.ToInt32(Convert.ToInt32(inspectionDetail.projected_hours) * 1.2);
                inspectionDetail.ext_remaining_hours = Convert.ToInt32(Convert.ToInt32(inspectionDetail.remaining_hours) * 1.2);
            }

            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }

            // Update child components
            Equipment eq = new Equipment(_context, (int)inspectionDetail.TRACK_INSPECTION.equipmentid_auto);
            eq.UpdateMiningShovelInspectionParentsFromChildren(inspectionDetail.inspection_auto);
            return Tuple.Create(true, "Measurement updated successfully. ");
        }

        /// <summary>
        /// Deletes the given recommendation id.  
        /// </summary>
        /// <param name="recommendationId">The ID of the recommendation record to delete.  </param>
        /// <returns>A tuple with the first value true if successful and the second value
        /// a message. </returns>
        public Tuple<bool, string> DeleteRecommendation(int recommendationId)
        {
            var rec = _context.TRACK_QUOTE_DETAIL.Find(recommendationId);
            var component = new Component(_context, Int32.Parse(rec.track_unit_auto));
            var quote = _context.TRACK_QUOTE.Find(rec.quote_auto);
            var action = _context.TRACK_ACTION_TYPE.Find(rec.op_type_auto);

            try
            {
                _context.TRACK_QUOTE_DETAIL.Remove(rec);
                _context.SaveChanges();
                _logger.LogRecommendationDeleted(quote.quote_name, component.GetComponentDescription(), action.action_description, component.GetComponentSideLabel());
                return Tuple.Create(true, "Recommendation deleted successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Gets a list of possible recommendations for the given component (from the general_eq_unit table). 
        /// </summary>
        public List<RecommendationTypeModel> GetPossibleRecommendations(int componentId)
        {
            List<RecommendationTypeModel> response = new List<RecommendationTypeModel>();
            var component = _context.GENERAL_EQ_UNIT.Find(componentId);
            if (component == null)
                return response;
            return _context.TRACK_ACTION_TYPE
                .Where(t => t.compartment_type == component.LU_COMPART.LU_COMPART_TYPE.comparttype)
                .Select(t => new RecommendationTypeModel()
                {
                    Id = t.action_type_auto,
                    Description = t.action_description
                }).ToList();
        }



        public async Task<bool> ChangeInspectionsInspector(int inspectionId, int userId)
        {
            var user = await _context.USER_TABLE.FirstOrDefaultAsync(u => u.user_auto == userId);
            if (user == null) throw new Exception("can not find user id with " + userId);
            _inspection.examiner = user.username;
            try
            {
               await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets a list of email addresses for user accounts currently setup in the system as
        /// contacts for the equipment this inspection was performed on. 
        /// </summary>
        /// <returns>A list of recipients for the equipment inspected. </returns>
        public List<RecipientModel> GetRecipientList()
        {
            return _context.CONTACTS
                .Where(c => (c.equipmentid_auto == _inspection.equipmentid_auto && c.Type == "EQ")
                || (c.crsf_auto == _inspection.EQUIPMENT.crsf_auto && c.Type == "SITE")
                || (c.customer_auto == _inspection.EQUIPMENT.Jobsite.customer_auto && c.Type == "CUST"))
                .Select(c => new RecipientModel()
                {
                    UserId = (int)c.user_auto,
                    Email = c.User.email,
                    Name = c.User.username
                }).ToList();
        }

        public Tuple<bool, string> SendReport(List<string> emails)
        {
            throw new NotImplementedException();
            return Tuple.Create(false, "Failed to send the report. ");
        }

        /// <summary>
        /// Generates a unique quote number to be used when creating a new quote. 
        /// Will keep generating random numbers until a unique one is generated which has not
        /// been used before. 
        /// </summary>
        /// <returns>A random quote number 8 digits long. </returns>
        private int GetUniqueQuoteNumber()
        {
            Random rnd = new Random();
            int number = rnd.Next(10000000, 100000000);
            var exists = _context.TRACK_QUOTE.Where(q => q.quote_name.Contains(number.ToString())).FirstOrDefault();
            if (exists != null)
                number = GetUniqueQuoteNumber();
            return number;
        }




        public async  Task<Tuple<bool, string>>  UpdateReverseForwardTravelReadingsOnInspection(UpdateInspectionForwardAndReverseTravelModel model)
        {
            switch (model.ForwardAndReverseOptions)
            {
                case ForwardAndReverseOptions.ForwardTravelKm:
                    _inspection.ForwardTravelKm = model.NewReading;
                    break;
                case ForwardAndReverseOptions.ReverseTravelKm:
                    _inspection.ReverseTravelKm = model.NewReading;
                    break;
                case ForwardAndReverseOptions.ForwardTravelHrs:
                    _inspection.ForwardTravelHours = model.NewReading;
                    break;
                case ForwardAndReverseOptions.ReverseTravelHrs:
                    _inspection.ReverseTravelHours = model.NewReading;
                    break;
            }
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
            return Tuple.Create(true, "Successfully updated " + model.ForwardAndReverseOptions.ToString() + " readings." );
            
        }
    }
}
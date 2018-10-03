using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.Extensions;
using BLL.Core.ViewModel;
using System.Threading.Tasks;

namespace BLL.Core.Domain.MiningShovelDomain
{
    public class UCInspection : UCDomain
    {
        private DAL.TRACK_INSPECTION DALInspection;
        private int Id = 0;
        public UCInspection(IUndercarriageContext context) : base(context)
        {
            
        }
        public UCInspection(IUndercarriageContext context, int InspectionId) : base(context)
        {
            Id = InspectionId;
        }
        public DAL.TRACK_INSPECTION getDALInspection(int InspectionId)
        {
            if (DALInspection != null && DALInspection.inspection_auto == InspectionId)
                return DALInspection;
            DALInspection = _domainContext.TRACK_INSPECTION.Find(InspectionId);
            return DALInspection;
        }
        public DAL.TRACK_INSPECTION getDALInspection()
        {
            if (DALInspection != null && DALInspection.inspection_auto == Id)
                return DALInspection;
            DALInspection = _domainContext.TRACK_INSPECTION.Find(Id);
            return DALInspection;
        }
        public DAL.TRACK_INSPECTION getDALInspectionByQuoteId(int QuoteId)
        {
            if (DALInspection != null && DALInspection.quote_auto == QuoteId)
                return DALInspection;
            DALInspection = _domainContext.TRACK_INSPECTION.Where(m=>m.quote_auto == QuoteId).FirstOrDefault();
            return DALInspection;
        }
        /// <summary>
        /// Returns all existing compart types on the Equipment
        /// </summary>
        /// <param name="EquipmentId"></param>
        /// <returns></returns>
        public IQueryable<CompartTypeV > getAvailableCompartTypes(int EquipmentId)
        {
            var compartTypeIds = _domainContext.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == EquipmentId).Select(m => m.LU_COMPART.comparttype_auto).GroupBy(m => m).Select(m => m.FirstOrDefault());
            return _domainContext.LU_COMPART_TYPE.Where(m=> compartTypeIds.Any(k=> m.comparttype_auto == k)).Select(m => new CompartTypeV { Id = m.comparttype_auto, Title = m.comparttype, Order = m.sorder ?? 99 });
        }
        /// <summary>
        /// Returns all existing compart types on the Equipment for LNH Rope Shovel Page
        /// </summary>
        /// <param name="EquipmentId"></param>
        /// <returns></returns>
        public IQueryable<CompartTypeV> getAvailableCompartTypesLNH(int EquipmentId)
        {
            var compartTypeIds = _domainContext.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == EquipmentId).Select(m => m.LU_COMPART.comparttype_auto).GroupBy(m => m).Select(m => m.FirstOrDefault());
            return _domainContext.LU_COMPART_TYPE.Where(m => compartTypeIds.Any(k => m.comparttype_auto == k)).Select(m => new CompartTypeV { Id = m.comparttype_auto, Title = ConvertToLNHCompartTypeName(m.comparttype), Order = m.sorder ?? 99 });
        }

        /// <summary>
        /// LNH want their link to be called a shoe, and they want their sprocket to be called a tumbler. 
        /// </summary>
        /// <param name="compartType"></param>
        /// <returns></returns>
        private string ConvertToLNHCompartTypeName(string compartType)
        {
            switch(compartType)
            {
                case "Link":
                    return "Shoe";
                case "Sprocket":
                    return "Tumbler";
                default:
                    return compartType;
            }
        }

        public Core.ViewModel.UserViewModel getInspector(int InspectionId)
        {
            if(getDALInspection() == null) return new Core.ViewModel.UserViewModel();
            var inspector = _domainContext.USER_TABLE.Where(m => m.userid == DALInspection.examiner).FirstOrDefault();
            if(inspector == null) return new Core.ViewModel.UserViewModel();
            return new Core.ViewModel.UserViewModel
            {
                UserId = inspector.user_auto.LongNullableToInt(),
                AspNetUserId = inspector.AspNetUserId,
                Name = inspector.username
            };
        }

        public Core.ViewModel.UserViewModel getInspector()
        {
            if (Id != 0)
                return getInspector(Id);
            return new Core.ViewModel.UserViewModel();
        }

        public Core.ViewModel.EvalDetailsV getOverAllEval()
        {
            if (getDALInspection() == null) return new Core.ViewModel.EvalDetailsV();
            return new Core.ViewModel.EvalDetailsV { Reading = 0, EvalCode = getDALInspection().evalcode, ObservationNote = "" };
        }

        public Core.ViewModel.EvalDetailsV getOverAllEval(int InspectionId)
        {
            if (getDALInspection(InspectionId) == null) return new Core.ViewModel.EvalDetailsV();
            return new Core.ViewModel.EvalDetailsV { Reading = 0, EvalCode = getDALInspection(InspectionId).evalcode, ObservationNote = "" };
        }

        public Core.ViewModel.EquipmentInspectionV getEquipment()
        {
            if (getDALInspection() == null) return new Core.ViewModel.EquipmentInspectionV();
            return getEquipmentByInspection(getDALInspection().inspection_auto);
        }
        public Core.ViewModel.EquipmentInspectionV getEquipmentByInspection(int InspectionId)
        {
            getDALInspection(InspectionId);
            if (DALInspection == null)
                return new Core.ViewModel.EquipmentInspectionV
                {
                    InspectionId = 0,
                    CompartTypes = new List<CompartTypeV>().AsQueryable(),
                    Equipment = new Core.ViewModel.EquipmentViewModel(),
                    Eval = new Core.ViewModel.EvalDetailsV(),
                    Inspector = new Core.ViewModel.UserViewModel()
                };
            var LogicalEquipment = new Equipment(_domainContext, DALInspection.equipmentid_auto.LongNullableToInt());
            return new Core.ViewModel.EquipmentInspectionV
            {
                InspectionId = InspectionId,
                CompartTypes = getAvailableCompartTypes(DALInspection.equipmentid_auto.LongNullableToInt()),
                Equipment = LogicalEquipment.getEquipmentForInspection(),
                Inspector = getInspector(InspectionId),
                Eval = getOverAllEval(InspectionId)
            };
        }

        public Core.ViewModel.EquipmentInspectionV getEquipmentById(int EquipmentId)
        {
            var LogicalEquipment = new Equipment(_domainContext, EquipmentId);
            if (LogicalEquipment.Id == 0)
                return new Core.ViewModel.EquipmentInspectionV
                {
                    InspectionId = 0,
                    CompartTypes = new List<CompartTypeV>().AsQueryable(),
                    Equipment = new Core.ViewModel.EquipmentViewModel(),
                    Eval = new Core.ViewModel.EvalDetailsV(),
                    Inspector = new Core.ViewModel.UserViewModel()
                };
            var _lastInspction = _domainContext.TRACK_INSPECTION.Where(m => m.equipmentid_auto == EquipmentId && m.ActionTakenHistory.recordStatus == (int)RecordStatus.Available).OrderByDescending(m => m.inspection_date).FirstOrDefault();
            return new Core.ViewModel.EquipmentInspectionV
            {
                InspectionId = 0,
                CompartTypes = getAvailableCompartTypes(EquipmentId),
                Equipment = LogicalEquipment.getEquipmentForInspection(),
                Inspector = new Core.ViewModel.UserViewModel { UserId = 0, Name = "-", AspNetUserId = "" },
                Eval = getOverAllEval(_lastInspction != null ? _lastInspction.inspection_auto : 0)
            };
        }

        public Core.ViewModel.EquipmentInspectionV getEquipmentByIdAndInspection(int EquipmentId, int inspectionId)
        {
            var LogicalEquipment = new Equipment(_domainContext, EquipmentId);
            if (LogicalEquipment.Id == 0)
                return new Core.ViewModel.EquipmentInspectionV
                {
                    InspectionId = 0,
                    CompartTypes = new List<CompartTypeV>().AsQueryable(),
                    Equipment = new Core.ViewModel.EquipmentViewModel(),
                    Eval = new Core.ViewModel.EvalDetailsV(),
                    Inspector = new Core.ViewModel.UserViewModel()
                };
            var _inspection = _domainContext.TRACK_INSPECTION.Find(inspectionId);
            return new Core.ViewModel.EquipmentInspectionV
            {
                InspectionId = inspectionId,
                CompartTypes = getAvailableCompartTypes(EquipmentId),
                Equipment = LogicalEquipment.getEquipmentForInspection(),
                Inspector = _inspection != null ? new Core.ViewModel.UserViewModel { UserId = 0, Name = _inspection.examiner, AspNetUserId = "" } : new Core.ViewModel.UserViewModel { UserId = 0, Name = "", AspNetUserId = "" },
                Eval = getOverAllEval(_inspection != null ? _inspection.inspection_auto : 0)
            };
        }

        public Interfaces.IGeneralInspectionModel getInspectionGeneral(int InspectionId) {
            var _inspection = getDALInspection(InspectionId);
            if (_inspection == null) return new Core.ViewModel.GeneralInspectionViewModel();
            var _equipment = new Equipment(_domainContext);
            //var _life = _equipment.GetEquipmentLife(_inspection.equipmentid_auto.LongNullableToInt(), _inspection.inspection_date);
            var _jobsite = _equipment.getEquipmentJobSite(_inspection.equipmentid_auto.LongNullableToInt());
            return new Core.ViewModel.GeneralInspectionViewModel
            {
                Id = InspectionId,
                Date = _inspection.inspection_date,
                EquipmentId = (int)_inspection.equipmentid_auto,
                Abrasive = _inspection.abrasive ?? 0,
                Impact = _inspection.impact ?? 0,
                Moisture = _inspection.moisture ?? 0,
                Packing = _inspection.packing ?? 0,
                Life = _inspection.ltd ?? 0, //_life
                SMU = _inspection.smu ?? 0,
                CustomerContact = _inspection.CustomerContact,
                TrammingHours = _inspection.TrammingHours ?? 0,
                TrackSagLeft = _inspection.track_sag_left ?? 0,
                TrackSagRight = _inspection.track_sag_right ?? 0,
                DryJointsLeft = _inspection.dry_joints_left ?? 0,
                DryJointsRight = _inspection.dry_joints_right ?? 0,
                ExtCannonLeft = _inspection.ext_cannon_left ?? 0,
                ExtCannonRight = _inspection.ext_cannon_right ?? 0,
                InspectionNotes = _inspection.notes,
                JobSiteNotes = _inspection.Jobsite_Comms,
                JobSite = _jobsite == null ? new JobSiteForSelectionVwMdl
                {
                    Id = 0,
                    CustomerId = 0,
                    Title = "-"
                } : 
                new JobSiteForSelectionVwMdl
                { Id = _jobsite.crsf_auto.LongNullableToInt(),
                    CustomerId = _jobsite.customer_auto.LongNullableToInt(),
                    Title = _jobsite.site_name
                },
            };

        }

        public List<EquipmentPhotosViewModel> GetMandatoryPhotos(int inspectionId)
        {
            var _inspection = getDALInspection(inspectionId);
            if (_inspection == null)
                return new List<EquipmentPhotosViewModel>();
            var _mandatoryImages = _domainContext.CUSTOMER_MODEL_MANDATORY_IMAGE
                .Where(i => i.ModelId == _inspection.EQUIPMENT.LU_MMTA.model_auto || i.ModelId == null)
                .Where(i => i.CustomerId == _inspection.EQUIPMENT.Jobsite.customer_auto || i.CustomerId == null)
                .OrderBy(i => i.ModelId).ThenBy(i => i.Order)
                .Select(i => new EquipmentPhotosViewModel()
                {
                    CustomerModelMandatoryImageId = i.Id,
                    Title = i.Title
                }).ToList();
            _mandatoryImages.ForEach(i =>
            {
                var image = _inspection.InspectionMandatoryImages.Where(a => a.CustomerModelMandatoryImageId == i.CustomerModelMandatoryImageId).FirstOrDefault();
                if(image != null)
                {
                    i.Description = image.Comment;
                    i.Id = image.Id;
                    i.Photo = image.Data != null ? Convert.ToBase64String(image.Data) : "";
                    i.Title = image.Title;
                }
            });
            return _mandatoryImages;

            /*return _inspection.InspectionMandatoryImages.Select(i => new EquipmentPhotosViewModel()
            {
                Description = i.Comment,
                Id = i.Id,
                Photo = Convert.ToBase64String(i.Data),
                Title = i.Title
            }).ToList();*/
        }

        public List<AdditionalRecordOverviewModel> GetAdditionalRecords(int inspectionId, int compartTypeId, string side)
        {
            var _inspection = getDALInspection(inspectionId);
            if (_inspection == null)
                return new List<AdditionalRecordOverviewModel>();

            var allAdditionalRecords = _domainContext.CUSTOMER_MODEL_COMPARTTYPE_ADDITIONAL
                .Where(c => c.CompartTypeId == compartTypeId)
                .Where(c => c.ModelId == null || c.ModelId == _inspection.EQUIPMENT.LU_MMTA.model_auto)
                .Where(c => c.CustomerId == null || c.CustomerId == _inspection.EQUIPMENT.Jobsite.customer_auto)
                .Select(c => new AdditionalRecordOverviewModel()
                {
                    Id = c.Id,
                    RecordId = -1,
                    Type = c.DefaultTool.tool_code
                }).ToList();

            allAdditionalRecords.ForEach(r =>
            {
                var existing = _inspection.CompartTypeAdditionals
                .Where(i => i.CompartTypeAdditional.CompartTypeId == compartTypeId)
                .Where(i => i.CompartTypeAdditionalId == r.Id)
                .Where(i => i.Side == (side == "Left" ? 1 : 2)).FirstOrDefault();
                if(existing != null)
                    r.RecordId = existing.Id;
            });

            return allAdditionalRecords;
        }

        public List<EquipmentPhotosViewModel> GetMandatoryPhotosForCompartType(int inspectionId, int compartTypeId, string side)
        {
            var _inspection = getDALInspection(inspectionId);
            if (_inspection == null)
                return new List<EquipmentPhotosViewModel>();

            var allImages = _domainContext.CUSTOMER_MODEL_COMPARTTYPE_MANDATORY_IMAGE
                .Where(i => i.CompartTypeId == compartTypeId)
                .Where(i => i.ModelId == null || i.ModelId == _inspection.EQUIPMENT.LU_MMTA.model_auto)
                .Where(i => i.CustomerId == null || i.CustomerId == _inspection.EQUIPMENT.Jobsite.customer_auto)
                .Where(i => i.RecordStatus == 0)
                .Select(i => new EquipmentPhotosViewModel()
                {
                    CustomerModelMandatoryImageId = i.Id,
                    Description = i.Description,
                    Id = -1,
                    Photo = "",
                    Title = i.Title
                })
                .ToList();

            allImages.ForEach(k =>
            {
                var image = _inspection.InspectionCompartTypeImages
                    .Where(i => i.CompartTypeMandatoryImageId == k.CustomerModelMandatoryImageId)
                    .Where(i => i.Side == (side == "Left" ? 1 : 2)).FirstOrDefault();
                if(image != null)
                {
                    k.Description = image.Comment;
                    //k.Photo = Convert.ToBase64String(image.Data);
                    k.Id = image.Id;
                }
            });

            return allImages;
        }

        public Tuple<bool, string> UpdateAdditionalRecord(int recordId, int id, string side, int inspectionId, string reading)
        {
            var parsedReading = Decimal.Parse(reading);
            var _record = _domainContext.INSPECTION_COMPARTTYPE_RECORD.Find(recordId);
            if(_record != null)
            {
                if(_record.Tool.tool_code == "OB")
                {
                    _record.ObservationNote = reading;
                } else
                {
                    _record.Reading = Decimal.Parse(reading);
                }
            } else
            {
                var definition = _domainContext.CUSTOMER_MODEL_COMPARTTYPE_ADDITIONAL.Find(id);
                if(definition == null)
                {
                    return Tuple.Create(false, "There is no additional record type with this Id. ");
                }
                var toolId = definition.DefaultToolId != null ? (int)definition.DefaultToolId : -1;
                var newRecord = new INSPECTION_COMPARTTYPE_RECORD()
                {
                    CompartTypeAdditionalId = id,
                    InspectionId = inspectionId,
                    MeasureNumber = 1,
                    ToolId = toolId,
                    Side = side == "Left" ? 1 : 2,
                    Reading = toolId == 8 ? 0 : Decimal.Parse(reading),
                    ObservationNote = toolId == 8 ? reading : ""
                };
                _domainContext.INSPECTION_COMPARTTYPE_RECORD.Add(newRecord);
            }

            try
            {
                _domainContext.SaveChanges();
                return Tuple.Create(true, "Saved additional record successfully. ");
            } catch (Exception e)
            {
                return Tuple.Create(false, "Failed to save changes. ");
            }
        }

        public AdditionalRecordModel GetAdditionalRecordDetails(int recordId, int id, string side, int inspectionId)
        {
            var _record = _domainContext.INSPECTION_COMPARTTYPE_RECORD.Find(recordId);
            if (_record == null)
            {
                var _newRecord = _domainContext.CUSTOMER_MODEL_COMPARTTYPE_ADDITIONAL.Find(id);
                if(_newRecord == null)
                    return new AdditionalRecordModel();
                return new AdditionalRecordModel()
                {
                    Name = _newRecord.Description,
                    Data = ""
                };
            }
            string reading = ""; 
            switch(_record.Tool.tool_code)
            {
                case "OB":
                    reading = _record.ObservationNote;
                    break;
                case "YES/NO":
                    reading = Convert.ToInt32(_record.Reading).ToString();
                    break;
                default:
                    reading = _record.Reading.ToString();
                    break;
            }
            return new AdditionalRecordModel()
            {
                Name = _record.CompartTypeAdditional.Description,
                Data = reading
            };
        }
        /// <summary>
        /// This method is used by the report tool only and will possible be removed when report tool be retired
        /// Return null if cannot find any inspection related to the QuoteId provided
        /// </summary>
        /// <param name="QuoteId"></param>
        /// <returns></returns>
        public System.Data.DataTable getInspectionOverviewDataTable(int QuoteId, int width, int height)
        {
            var _inspection = getDALInspectionByQuoteId(QuoteId);
            if ( _inspection == null) return null;
            return (new[] { (new {
                TravelledHoursKmsLabel = _inspection.TravelledKms ? "Travelled Kms:" : "Travelled Hours:",
                TravelForward = _inspection.ForwardTravelHours,
                TravelReverse = _inspection.ReverseTravelHours,
                TravelForwardKm = _inspection.ForwardTravelKm,
                TravelReverseKm = _inspection.ReverseTravelKm,
                TrackSagLeft = _inspection.track_sag_left ?? 0,
                TrackSagRight = _inspection.track_sag_right ?? 0,
                TrackSagLeftImage = _inspection.LeftTrackSagImage.IsValidImage() ? _inspection.LeftTrackSagImage.ResizeImageWithBackColor(width,height) : null,
                TrackSagRightImage = _inspection.RightTrackSagImage.IsValidImage() ? _inspection.RightTrackSagImage.ResizeImageWithBackColor(width,height) : null,
                TrackSagLeftComment = _inspection.LeftTrackSagComment,
                TrackSagRightComment = _inspection.RightTrackSagComment,
                TrackSagLeftImageIcon = _inspection.LeftTrackSagImage.ToIcon(20, 20, ImageIcon.Camera),
                TrackSagRightImageIcon = _inspection.RightTrackSagImage.ToIcon(20, 20, ImageIcon.Camera),
                TrackSagLeftCommentIcon = _inspection.LeftTrackSagComment.ToIcon(20, 20, ImageIcon.Comment),
                TrackSagRightCommentIcon = _inspection.RightTrackSagComment.ToIcon(20, 20, ImageIcon.Comment),
                ExtCannonLeft = _inspection.ext_cannon_left,
                ExtCannonRight = _inspection.ext_cannon_right,
                ExtCannonLeftImage = _inspection.LeftCannonExtensionImage.IsValidImage() ? _inspection.LeftCannonExtensionImage.ResizeImageWithBackColor(width,height) : null,
                ExtCannonRightImage = _inspection.RightCannonExtensionImage.IsValidImage() ? _inspection.RightCannonExtensionImage.ResizeImageWithBackColor(width,height) : null,
                ExtCannonLeftComment = _inspection.LeftCannonExtensionComment,
                ExtCannonRightComment = _inspection.RightCannonExtensionComment,
                ExtCannonLeftImageIcon = _inspection.LeftCannonExtensionImage.ToIcon(20, 20, ImageIcon.Camera),
                ExtCannonRightImageIcon = _inspection.RightCannonExtensionImage.ToIcon(20, 20, ImageIcon.Camera),
                ExtCannonLeftCommentIcon = _inspection.LeftCannonExtensionComment.ToIcon(20, 20, ImageIcon.Comment),
                ExtCannonRightCommentIcon = _inspection.RightCannonExtensionComment.ToIcon(20, 20, ImageIcon.Comment),
                DryJointsNoLeft = _inspection.dry_joints_left,
                DryJointsNoRight = _inspection.dry_joints_right,
                DryJointsNoLeftComment = _inspection.LeftDryJointComments,
                DryJointsNoRightComment = _inspection.RightDryJointComments,
                DryJointsNoLeftImage = _inspection.DryJointsLeftImage,
                DryJointsNoRightImage = _inspection.DryJointsRightImage,
                ScallopMeasureLeftComment = _inspection.LeftScallopComments,
                ScallopMeasureRightComment = _inspection.RightScallopComments,
                ScallopMeasureLeftImage = _inspection.LeftScallopImage,
                ScallopMeasureRightImage = _inspection.RightScallopImage,
                ScallopMeasureLeft = _inspection.LeftScallopMeasurement,
                ScallopMeasureRight = _inspection.RightScallopMeasurement,

            }) }).ToList().ToDataTable();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.GETCore.Classes.ViewModel;
using System.IO;
using System.Drawing;

namespace BLL.GETCore.Classes
{
    public class GETInspection
    {
        public bool isInspectionFlagged(string inspect_auto)
        {
            bool result = false;
            int iInspectAuto = int.Parse(inspect_auto);

            using (var dataEntities = new GETContext())
            {
                var inspectionFlagged = dataEntities.GET_IMPLEMENT_INSPECTION.Find(iInspectAuto);

                if (inspectionFlagged != null)
                    result = inspectionFlagged.flag;
            }

            return result;
        }

        public bool isInspectionViewed(string inspect_auto, int userAuto)
        {
            bool result = false;
            int iInspectAuto = int.Parse(inspect_auto);

            using (var dataEntities = new GETContext())
            {
                var inspectionViewed = (from giv in dataEntities.GET_INSPECTIONS_VIEWED
                                        where giv.inspection_auto == iInspectAuto && giv.user_auto == userAuto
                                        select new
                                        {
                                            giv.inspections_viewed_auto
                                        });

                // Inspection has been viewed before.
                if (inspectionViewed.FirstOrDefault() != null)
                {
                    result = true;
                }
                // This is the first time viewing the inspection for this user.
                else
                {
                    // Mark the inspection as viewed.
                    dataEntities.GET_INSPECTIONS_VIEWED.Add(
                        new GET_INSPECTIONS_VIEWED
                        {
                            inspection_auto = iInspectAuto,
                            user_auto = userAuto,
                            viewed_date = DateTime.Now
                        });
                    dataEntities.SaveChanges();

                    result = false;
                }
            }

            return result;
        }

        public List<GETInspectionDateEvalVM> GetInspectionDates(string impSerial, string eqmtSerialNo)
        {
            List<GETInspectionDateEvalVM> result = new List<GETInspectionDateEvalVM>();
            long eqmtAuto = 0;

            using (var dataEntitiesShared = new DAL.SharedContext())
            {
                eqmtAuto = dataEntitiesShared.EQUIPMENT
                                .Where(e => e.serialno == eqmtSerialNo)
                                .Select(e => e.equipmentid_auto).FirstOrDefault();
            }

            if (eqmtAuto == 0)
            {
                return result;
            }

            using (var dataEntities = new DAL.GETContext())
            {
                result = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                          join g in dataEntities.GET
                             on gii.get_auto equals g.get_auto
                          where g.impserial == impSerial && g.equipmentid_auto == eqmtAuto
                          orderby gii.inspection_date descending, gii.inspection_auto descending
                          select new GETInspectionDateEvalVM
                          {
                              inspection_auto = gii.inspection_auto,
                              inspection_date = gii.inspection_date,
                              eval = gii.eval
                          }).ToList();
            }

            return result;
        }

        public GETEquipmentDetailsVM GetEquipmentDetails(string inspect_auto)
        {
            int iInspectAuto = int.TryParse(inspect_auto, out iInspectAuto) ? iInspectAuto : 0;
            GETEquipmentDetailsVM result = new GETEquipmentDetailsVM();

            using (var dataEntities = new DAL.GETContext())
            {
                var result2 = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                               join g in dataEntities.GET
                                  on gii.get_auto equals g.get_auto
                               where gii.inspection_auto == iInspectAuto
                               select new
                               {
                                   gii.inspection_auto,
                                   gii.meter_reading,
                                   g.get_auto,
                                   g.equipmentid_auto
                               }).FirstOrDefault();

                if (result2 == null)
                {
                    return result;
                }

                using (var dataEntitiesShared = new DAL.SharedContext())
                {
                    var currentEquipmentAuto = result2.equipmentid_auto;
                    var intermediate_result = (from e in dataEntitiesShared.EQUIPMENT
                                               join c in dataEntitiesShared.CRSF
                                                  on e.crsf_auto equals c.crsf_auto
                                               join lm in dataEntitiesShared.LU_MMTA
                                                  on e.mmtaid_auto equals lm.mmtaid_auto
                                               join mk in dataEntitiesShared.MAKE
                                                  on lm.make_auto equals mk.make_auto
                                               join md in dataEntitiesShared.MODEL
                                                  on lm.model_auto equals md.model_auto
                                               join t in dataEntitiesShared.TYPE
                                                  on lm.type_auto equals t.type_auto
                                               join ee in dataEntitiesShared.GET_EVENTS_EQUIPMENT
                                                  on e.equipmentid_auto equals ee.equipment_auto
                                               join ge in dataEntitiesShared.GET_EVENTS
                                                  on ee.events_auto equals ge.events_auto
                                               where e.equipmentid_auto == currentEquipmentAuto
                                                  && ge.action_auto == (int)BLL.Core.Domain.GETActionType.Equipment_Setup
                                               select new
                                               {
                                                   id = e.equipmentid_auto,
                                                   serialno = e.serialno,
                                                   unitno = e.unitno,
                                                   site_name = c.site_name,
                                                   meter_reading = result2.meter_reading,
                                                   makedesc = mk.makedesc,
                                                   modeldesc = md.modeldesc,
                                                   ltd = (result2.meter_reading - e.smu_at_start + e.LTD_at_start).Value,
                                                   typedesc = t.typedesc,
                                                   setup_date = ge.event_date
                                               }
                                           ).FirstOrDefault();

                    if (intermediate_result != null)
                    {
                        result.id = intermediate_result.id;
                        result.serialno = intermediate_result.serialno;
                        result.unitno = intermediate_result.unitno;
                        result.site_name = intermediate_result.site_name;
                        result.meter_reading = intermediate_result.meter_reading;
                        result.makedesc = intermediate_result.makedesc;
                        result.modeldesc = intermediate_result.modeldesc;
                        result.ltd = intermediate_result.ltd;
                        result.typedesc = intermediate_result.typedesc;
                        result.setup_date = intermediate_result.setup_date.ToShortDateString();
                    }

                }
            }

            return result;
        }

        public GETInspectionSummaryDetailsVM GetInspectionDetails(string inspect_auto)
        {
            GETInspectionSummaryDetailsVM result = new GETInspectionSummaryDetailsVM();
            int tempId = 0;
            if (!Int32.TryParse(inspect_auto, out tempId))
                return result;

            GET_IMPLEMENT_INSPECTION currentInspection;
            using (var _GetContext = new DAL.GETContext())
            {
                currentInspection = _GetContext.GET_IMPLEMENT_INSPECTION.Find(tempId);
                if (currentInspection == null)
                    return result;
                result.eval = currentInspection.eval;
                result.inspection_date = currentInspection.inspection_date;
                result.ltd = currentInspection.ltd;
                result.impserial = currentInspection.GET.impserial;
            }

            using (var _SharedContext = new DAL.SharedContext())
            {
                var currentEquipment = _SharedContext.EQUIPMENT.Find(currentInspection.GET.equipmentid_auto);
                if (currentEquipment == null)
                    return result;
                result.serialno = currentEquipment.serialno;
                result.unitno = currentEquipment.unitno;

                var inspectionUser = _SharedContext.USER_TABLE.Find(currentInspection.user_auto);
                if (inspectionUser == null)
                    return result;

                result.username = inspectionUser.username;
            }

            return result;
        }

        public GETGeneralQuestionsVM GetGeneralQuestions(string inspect_auto)
        {
            GETGeneralQuestionsVM result = new GETGeneralQuestionsVM();
            int iInspectAuto = int.Parse(inspect_auto);

            using (var dataEntities = new DAL.GETContext())
            {
                var inspOverallComments = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                           where gii.inspection_auto == iInspectAuto
                                           select new
                                           {
                                               gii.overall_notes
                                           }).FirstOrDefault();

                var inspDirtyEnv = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                    where gii.inspection_auto == iInspectAuto
                                    join gic in dataEntities.GET_INSPECTION_CONSTANTS
                                       on gii.dirty_environment equals gic.constants_auto
                                    select new
                                    {
                                        dirty_env = gic.inspect_desc
                                    }).FirstOrDefault();

                var inspCondition = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                     where gii.inspection_auto == iInspectAuto
                                     join gic in dataEntities.GET_INSPECTION_CONSTANTS
                                        on gii.condition equals gic.constants_auto
                                     select new
                                     {
                                         condition = gic.inspect_desc
                                     }).FirstOrDefault();

                var inspWorkArea = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                    where gii.inspection_auto == iInspectAuto
                                    join gic in dataEntities.GET_INSPECTION_CONSTANTS
                                       on gii.work_area equals gic.constants_auto
                                    select new
                                    {
                                        work_area = gic.inspect_desc
                                    }).FirstOrDefault();

                var inspMachine = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                   where gii.inspection_auto == iInspectAuto
                                   join gic in dataEntities.GET_INSPECTION_CONSTANTS
                                      on gii.machine equals gic.constants_auto
                                   select new
                                   {
                                       machine = gic.inspect_desc
                                   }).FirstOrDefault();

                var inspArea = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                where gii.inspection_auto == iInspectAuto
                                join gic in dataEntities.GET_INSPECTION_CONSTANTS
                                   on gii.area equals gic.constants_auto
                                select new
                                {
                                    area = gic.inspect_desc
                                }).FirstOrDefault();

                result = new GETGeneralQuestionsVM
                {
                    inspOverallComments = inspOverallComments.overall_notes,
                    inspDirtyEnv = inspDirtyEnv.dirty_env,
                    inspCondition = inspCondition.condition,
                    inspWorkArea = inspWorkArea.work_area,
                    inspMachine = inspMachine.machine,
                    inspArea = inspArea.area
                };
            }

            return result;
        }

        public List<GETComponentInfoVM> GetComponents(string inspect_auto)
        {
            int iInspectAuto = int.Parse(inspect_auto);
            List<GETComponentInfoVM> result = new List<GETComponentInfoVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var replaceEvents = (from gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                     join gci in dataEntities.GET_COMPONENT_INSPECTION
                                         on gii.inspection_auto equals gci.implement_inspection_auto
                                     join gec in dataEntities.GET_EVENTS_COMPONENT
                                         on gci.get_component_auto equals gec.get_component_auto
                                     join ge in dataEntities.GET_EVENTS
                                         on gec.events_auto equals ge.events_auto
                                     join ga in dataEntities.GET_ACTIONS
                                       on ge.action_auto equals ga.actions_auto
                                     where ga.action_name == GET_ACTIONS_constants.GET_EVENT_ComponentReplacement
                                       && gii.inspection_auto == iInspectAuto
                                       && ge.recordStatus == 0
                                     select new
                                     {
                                         ge.events_auto,
                                         gci.inspection_auto
                                     }).ToArray();


                var componentInfo = (from gci in dataEntities.GET_COMPONENT_INSPECTION
                                     join gc in dataEntities.GET_COMPONENT
                                        on gci.get_component_auto equals gc.get_component_auto
                                     join gsc in dataEntities.GET_SCHEMATIC_COMPONENT
                                        on gc.schematic_component_auto equals gsc.schematic_component_auto
                                     join lc in dataEntities.LU_COMPART_TYPE
                                        on gsc.comparttype_auto equals lc.comparttype_auto
                                     where gci.implement_inspection_auto == iInspectAuto
                                     select new GETComponentInfoVM
                                     {
                                         inspection_auto = gci.inspection_auto,
                                         measurement = gci.measurement,
                                         flag = gci.flag,
                                         replace = gci.replace,
                                         comment = gci.comment,
                                         flag_ignored = gci.flag_ignored,
                                         ltd = gci.ltd,
                                         condition = ((gc.req_measure.Value == true) && (gc.initial_length.Value - gc.worn_length.Value) > 0) ?
                                                ((gc.initial_length.Value - gci.measurement) / (gc.initial_length.Value - gc.worn_length.Value)) * 100 : 0,
                                         comparttype = lc.comparttype,
                                         req_measure = gc.req_measure.Value
                                     }).ToList();

                result = componentInfo;
            }

            return result;
        }

        public List<GETInterpretationCommentsVM> AddInterpretationComment(string inspect_auto, string userAuto, string interp_comment)
        {
            List<GETInterpretationCommentsVM> result = new List<GETInterpretationCommentsVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                dataEntities.GET_INTERPRETATION_COMMENTS.Add(
                    new GET_INTERPRETATION_COMMENTS
                    {
                        comment = interp_comment,
                        comment_date = DateTime.Now,
                        user_auto = int.Parse(userAuto),
                        inspection_auto = int.Parse(inspect_auto)
                    }
                );
                dataEntities.SaveChanges();
            }

            result = GetInterpretationComments(inspect_auto);

            return result;
        }

        public List<GETInterpretationCommentsVM> GetInterpretationComments(string inspect_auto)
        {
            int iInspectAuto = int.TryParse(inspect_auto, out iInspectAuto) ? iInspectAuto : 0;
            List<GETInterpretationCommentsVM> result = new List<GETInterpretationCommentsVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var interpretationComments = dataEntities.GET_INTERPRETATION_COMMENTS
                                            .Where(c => c.inspection_auto == iInspectAuto)
                                            .OrderBy(c => c.comment_auto)
                                            .Select(c => new
                                            {
                                                comment = c.comment,
                                                user_auto = c.user_auto,
                                                comment_date = c.comment_date
                                            }).ToList();

                using (var dataEntitiesShared = new DAL.SharedContext())
                {
                    foreach (var entry in interpretationComments)
                    {
                        result.Add(new GETInterpretationCommentsVM
                        {
                            comment = entry.comment,
                            username = dataEntitiesShared.USER_TABLE.Find(entry.user_auto).username,
                            comment_date = entry.comment_date
                        });
                    }
                }
            }

            return result;
        }

        public List<GETComponentInspectionPhotosVM> GetComponentPhotos(string inspection_auto)
        {
            int iInspectAuto = int.Parse(inspection_auto);
            List<GETComponentInspectionPhotosVM> result = new List<GETComponentInspectionPhotosVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var componentPhotos = (from gcii in dataEntities.GET_COMPONENT_INSPECTION_IMAGES
                                       join gci in dataEntities.GET_COMPONENT_INSPECTION
                                        on gcii.component_inspection_auto equals gci.inspection_auto
                                       where gci.implement_inspection_auto == iInspectAuto
                                       select new GETComponentInspectionPhotosVM
                                       {
                                           component_auto = gci.get_component_auto,
                                           component_inspection_auto = gcii.component_inspection_auto,
                                           image_auto = gcii.image_auto
                                       }).ToList();

                result = componentPhotos;
            }

            return result;
        }

        public List<GETObservationPointInspectionPhotosVM> GetObservationPointPhotos(string inspect_auto)
        {
            int iInspectAuto = int.Parse(inspect_auto);
            List<GETObservationPointInspectionPhotosVM> result = new List<GETObservationPointInspectionPhotosVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var OPPhotos = (from gopii in dataEntities.GET_OBSERVATION_POINT_INSPECTION_IMAGES
                                join gopi in dataEntities.GET_OBSERVATION_POINT_INSPECTION
                                    on gopii.observation_point_inspection_auto equals gopi.observation_point_inspection_auto
                                where gopi.inspection_auto == iInspectAuto
                                select new GETObservationPointInspectionPhotosVM
                                {
                                    observation_point_auto = gopi.observation_point_auto,
                                    op_inspection_auto = gopi.observation_point_inspection_auto,
                                    image_auto = gopii.image_auto
                                }).ToList();

                result = OPPhotos;
            }

            return result;
        }

        public List<GETImplementInspectionPhotosVM> GetImplementInspectionPhotos(string inspect_auto)
        {
            int iInspectAuto = int.Parse(inspect_auto);
            List<GETImplementInspectionPhotosVM> result = new List<GETImplementInspectionPhotosVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var inspectionPhotos = (from giii in dataEntities.GET_IMPLEMENT_INSPECTION_IMAGE
                                        where giii.inspection_auto == iInspectAuto
                                        select new
                                        {
                                            giii.image_auto,
                                            giii.parameter_type
                                        });

                result = inspectionPhotos.Select(
                    ip => new GETImplementInspectionPhotosVM
                    {
                        image_auto = ip.image_auto,
                        parameter_type = ip.parameter_type
                    }).ToList();
            }

            return result;
        }

        public byte[] GetImplementInspectionPhotoById(string image_auto)
        {
            int iImageAuto = int.Parse(image_auto);
            byte[] result;

            using (var dataEntities = new DAL.GETContext())
            {
                var inspectionPhotos = (from giii in dataEntities.GET_IMPLEMENT_INSPECTION_IMAGE
                                        where giii.image_auto == iImageAuto
                                        select new
                                        {
                                            giii.inspection_photo
                                        }).FirstOrDefault();

                result = inspectionPhotos.inspection_photo;
            }

            return result;
        }

        public List<GETObservationsVM> GetObservations(string inspect_auto)
        {
            int iInspectAuto = int.Parse(inspect_auto);
            List<GETObservationsVM> result = new List<GETObservationsVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var observations = (from gob in dataEntities.GET_OBSERVATIONS
                                    join gor in dataEntities.GET_OBSERVATION_RESULTS
                                        on gob.observations_auto equals gor.observations_auto
                                    join gci in dataEntities.GET_COMPONENT_INSPECTION
                                        on gor.inspection_auto equals gci.inspection_auto
                                    join gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                        on gci.implement_inspection_auto equals gii.inspection_auto
                                    where gor._checked == true && gii.inspection_auto == iInspectAuto
                                    select new
                                    {
                                        gci.inspection_auto,
                                        gob.observations_auto,
                                        gob.observation
                                    });

                result = observations.Select(
                    o => new GETObservationsVM
                    {
                        inspection_auto = o.inspection_auto,
                        observations_auto = o.observations_auto,
                        observation = o.observation
                    }).ToList();
            }

            return result;
        }

        public List<GETOPObservationsVM> GetOPObservations(string inspect_auto)
        {
            int iInspectAuto = int.Parse(inspect_auto);
            List<GETOPObservationsVM> result = new List<GETOPObservationsVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var observations = (from gob in dataEntities.GET_OBSERVATIONS
                                    join gopr in dataEntities.GET_OBSERVATION_POINT_RESULTS
                                        on gob.observations_auto equals gopr.observations_auto
                                    join gopi in dataEntities.GET_OBSERVATION_POINT_INSPECTION
                                        on gopr.observation_point_inspection_auto equals gopi.observation_point_inspection_auto
                                    join gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                        on gopi.inspection_auto equals gii.inspection_auto
                                    where gopr._checked == true && gii.inspection_auto == iInspectAuto
                                    select new
                                    {
                                        gopi.observation_point_inspection_auto,
                                        gob.observations_auto,
                                        gob.observation
                                    });

                result = observations.Select(
                    o => new GETOPObservationsVM
                    {
                        op_inspection_auto = o.observation_point_inspection_auto,
                        observations_auto = o.observations_auto,
                        observation = o.observation
                    }).ToList();
            }

            return result;
        }

        public List<GETObservationPhotosVM> GetObservationPhotos(string inspect_auto)
        {
            int iInspectAuto = int.Parse(inspect_auto);
            List<GETObservationPhotosVM> result = new List<GETObservationPhotosVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var observationImages = (from gob in dataEntities.GET_OBSERVATIONS
                                         join gor in dataEntities.GET_OBSERVATION_RESULTS
                                             on gob.observations_auto equals gor.observations_auto
                                         join goi in dataEntities.GET_OBSERVATION_IMAGE
                                             on gor.results_auto equals goi.results_auto
                                         join gci in dataEntities.GET_COMPONENT_INSPECTION
                                             on gor.inspection_auto equals gci.inspection_auto
                                         join gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                             on gci.implement_inspection_auto equals gii.inspection_auto
                                         where gii.inspection_auto == iInspectAuto
                                         select new
                                         {
                                             gci.inspection_auto,
                                             gob.observations_auto,
                                             goi.image_auto
                                         });

                result = observationImages.Select(
                    oi => new GETObservationPhotosVM
                    {
                        inspection_auto = oi.inspection_auto,
                        observations_auto = oi.observations_auto,
                        image_auto = oi.image_auto
                    }).ToList();
            }

            return result;
        }

        /// <summary>
        /// Returns inspection photos by image_auto and photo_type.
        /// Photo type is defined in the Enum - MobileAppInspectionPhotoType.
        /// </summary>
        /// <param name="image_auto"></param>
        /// <param name="photo_type"></param>
        /// <returns></returns>
        public byte[] GetPhotoByIdAndType(string image_auto, int photo_type)
        {
            int iImageAuto = int.Parse(image_auto);
            byte[] result;

            using (var dataEntities = new DAL.GETContext())
            {
                switch (photo_type)
                {
                    // Component photos
                    case (int)BLL.GETInterfaces.Enum.MobileAppInspectionPhotoType.Component:
                        var componentImage = (from gcii in dataEntities.GET_COMPONENT_INSPECTION_IMAGES
                                              where gcii.image_auto == iImageAuto
                                              select new
                                              {
                                                  gcii.component_photo
                                              }).FirstOrDefault();
                        result = componentImage.component_photo;
                        break;

                    // Observation Point photos
                    case (int)BLL.GETInterfaces.Enum.MobileAppInspectionPhotoType.Observation_Point:
                        var OPImage = (from gopii in dataEntities.GET_OBSERVATION_POINT_INSPECTION_IMAGES
                                       where gopii.image_auto == iImageAuto
                                       select new
                                       {
                                           gopii.observation_point_photo
                                       }).FirstOrDefault();

                        result = OPImage.observation_point_photo;
                        break;
                    
                    // Component Observation photos
                    case (int)BLL.GETInterfaces.Enum.MobileAppInspectionPhotoType.Component_Observation:
                        var observationImages = (from goi in dataEntities.GET_OBSERVATION_IMAGE
                                                 where goi.image_auto == iImageAuto
                                                 select new
                                                 {
                                                     goi.observation_photo
                                                 }).FirstOrDefault();

                        result = observationImages.observation_photo;
                        break;

                    // Observation Point - Observation photos
                    case (int)BLL.GETInterfaces.Enum.MobileAppInspectionPhotoType.OP_Observation:
                        var observationImages2 = (from goi in dataEntities.GET_OBSERVATION_POINT_IMAGES
                                                 where goi.image_auto == iImageAuto
                                                 select new
                                                 {
                                                     goi.observation_photo
                                                 }).FirstOrDefault();

                        result = observationImages2.observation_photo;
                        break;
                    
                    default:
                        result = null;
                        break;
                }
            }

            return result;   
        }

        public List<GETOPObservationPhotosVM> GetObservationPhotosOP(string inspect_auto)
        {
            int iInspectAuto = int.Parse(inspect_auto);
            List<GETOPObservationPhotosVM> result = new List<GETOPObservationPhotosVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var observationImages = (from gopr in dataEntities.GET_OBSERVATION_POINT_RESULTS
                                         join goi in dataEntities.GET_OBSERVATION_POINT_IMAGES
                                             on gopr.observation_point_results_auto equals goi.results_auto
                                         join gopi in dataEntities.GET_OBSERVATION_POINT_INSPECTION
                                             on gopr.observation_point_inspection_auto equals gopi.observation_point_inspection_auto
                                         join gii in dataEntities.GET_IMPLEMENT_INSPECTION
                                             on gopi.inspection_auto equals gii.inspection_auto
                                         where gii.inspection_auto == iInspectAuto
                                         select new
                                         {
                                             gopi.observation_point_inspection_auto,
                                             gopr.observations_auto,
                                             goi.image_auto
                                         });

                result = observationImages.Select(
                    oi => new GETOPObservationPhotosVM
                    {
                        op_inspection_auto = oi.observation_point_inspection_auto,
                        observations_auto = oi.observations_auto,
                        image_auto = oi.image_auto
                    }).ToList();
            }

            return result;
        }

        public bool IgnoreFlag(string comp_inspect_auto, string meter_reading, string user_auto)
        {
            //bool result = false;
            int iCompInspectAuto = int.Parse(comp_inspect_auto);
            int iMeterReading = int.Parse(meter_reading);
            int iUserAuto = int.Parse(user_auto);

            var ActionParam = new BLL.Core.Domain.EquipmentActionRecord
            {
                TypeOfAction = BLL.Core.Domain.ActionType.GETAction
            };

            var FlagIgnoredParams = new BLL.Core.Domain.GETFlagIgnoredParams
            {
                UserAuto = iUserAuto,
                ActionType = BLL.Core.Domain.GETActionType.Flag_Ignored,
                RecordedDate = DateTime.Now,
                EventDate = DateTime.Now,
                Comment = "",
                Cost = 0.0M,
                ComponentInspectionAuto = iCompInspectAuto,
                MeterReading = iMeterReading
            };

            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = " ",
                LastMessage = " ",
                Id = 0
            };

            using (var FlagIgnoredAction = new BLL.Core.Domain.Action(new DAL.GETContext(), ActionParam, FlagIgnoredParams))
            {
                FlagIgnoredAction.Operation.Start();

                if (FlagIgnoredAction.Operation.Status == BLL.Core.Domain.ActionStatus.Close)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = FlagIgnoredAction.Operation.ActionLog;
                    rm.LastMessage = FlagIgnoredAction.Operation.Message;
                }

                if (FlagIgnoredAction.Operation.Status == BLL.Core.Domain.ActionStatus.Started)
                {
                    FlagIgnoredAction.Operation.Validate();
                }

                if (FlagIgnoredAction.Operation.Status == BLL.Core.Domain.ActionStatus.Invalid)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = FlagIgnoredAction.Operation.ActionLog;
                    rm.LastMessage = FlagIgnoredAction.Operation.Message;
                }

                if (FlagIgnoredAction.Operation.Status == BLL.Core.Domain.ActionStatus.Valid)
                {
                    FlagIgnoredAction.Operation.Commit();
                }

                if (FlagIgnoredAction.Operation.Status == BLL.Core.Domain.ActionStatus.Failed)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = FlagIgnoredAction.Operation.ActionLog;
                    rm.LastMessage = FlagIgnoredAction.Operation.Message;
                }

                if (FlagIgnoredAction.Operation.Status == BLL.Core.Domain.ActionStatus.Succeed)
                {
                    rm.OperationSucceed = true;
                    rm.ActionLog = FlagIgnoredAction.Operation.ActionLog;
                    rm.LastMessage = FlagIgnoredAction.Operation.Message;
                }

                rm.Id = FlagIgnoredAction.Operation.UniqueId;
            }

            return rm.OperationSucceed;
        }

        /* This method is called when replacing a component from the Inspection Details screen.
         * 
         * Input Parameters:
         * comp_inspect_auto - Component Inspection Auto from the GET_COMPONENT_INSPECTION table.
         * meter_reading - The SMU or meter reading of the equipment at the time the replacement occurred.
         * user_auto - The user who initiated the change.
         * replaced_year - The year that the component replacement occurred.
         * replaced_month - The month that the component replacement occurred.
         * replaced_day - The day that the component replacement occurred.
         * 
         * Output: Returns a string indicating whether the replacement was successful or not.
         */
        public string ReplaceComponent(string comp_inspect_auto, string meter_reading, string user_auto, string replaced_year, string replaced_month, string replaced_day)
        {
            string result = "";
            int iCompInspectAuto = int.Parse(comp_inspect_auto);
            int iMeterReading = int.Parse(meter_reading);
            int iUserAuto = int.Parse(user_auto);

            int iReplacedYear = int.Parse(replaced_year);
            int iReplacedMonth = int.Parse(replaced_month);
            int iReplacedDay = int.Parse(replaced_day);

            var ActionParam = new BLL.Core.Domain.EquipmentActionRecord
            {
                TypeOfAction = BLL.Core.Domain.ActionType.GETAction
            };

            var ComponentReplacementParams = new BLL.Core.Domain.GETComponentReplacementParams
            {
                UserAuto = iUserAuto,
                ActionType = BLL.Core.Domain.GETActionType.Component_Replacement,
                RecordedDate = DateTime.Now,
                EventDate = new DateTime(iReplacedYear, iReplacedMonth, iReplacedDay),
                Comment = "",
                Cost = 0.0M,
                ComponentInspectionAuto = iCompInspectAuto,
                MeterReading = iMeterReading
            };

            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = " ",
                LastMessage = " ",
                Id = 0
            };

            using (var ComponentReplacementAction = new BLL.Core.Domain.Action(new DAL.GETContext(), ActionParam, ComponentReplacementParams))
            {
                ComponentReplacementAction.Operation.Start();

                if (ComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Close)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = ComponentReplacementAction.Operation.ActionLog;
                    rm.LastMessage = ComponentReplacementAction.Operation.Message;
                }

                if (ComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Started)
                {
                    result = ComponentReplacementAction.Operation.Message;
                    ComponentReplacementAction.Operation.Validate();
                }

                if (ComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Invalid)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = ComponentReplacementAction.Operation.ActionLog;
                    rm.LastMessage = ComponentReplacementAction.Operation.Message;
                }

                if (ComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Valid)
                {
                    ComponentReplacementAction.Operation.Commit();
                }

                if (ComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Failed)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = ComponentReplacementAction.Operation.ActionLog;
                    rm.LastMessage = ComponentReplacementAction.Operation.Message;
                }

                if (ComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Succeed)
                {
                    rm.OperationSucceed = true;
                    rm.ActionLog = ComponentReplacementAction.Operation.ActionLog;
                    rm.LastMessage = ComponentReplacementAction.Operation.Message;
                }

                rm.Id = ComponentReplacementAction.Operation.UniqueId;
            }

            return result;
        }

        /* This method is called when undoing a component replacement from the Inspection Details screen.
         * 
         * Input Parameters:
         * comp_inspect_auto - Component Inspection Auto from the GET_COMPONENT_INSPECTION table.
         * user_auto - The user who initiated the change.
         * 
         * Output: Returns a string indicating whether the replacement was successful or not.
         */
        public string UndoReplaceComponent(string comp_inspect_auto, string user_auto)
        {
            string result = "0";
            int iCompInspectAuto = int.Parse(comp_inspect_auto);
            int iUserAuto = int.Parse(user_auto);

            var ActionParam = new BLL.Core.Domain.EquipmentActionRecord
            {
                TypeOfAction = BLL.Core.Domain.ActionType.GETAction
            };

            var UndoComponentReplacementParams = new BLL.Core.Domain.GETUndoComponentReplacementParams
            {
                UserAuto = iUserAuto,
                ActionType = BLL.Core.Domain.GETActionType.Undo_Component_Replacement,
                RecordedDate = DateTime.Now,
                EventDate = DateTime.Now,
                Comment = "",
                Cost = 0.0M,
                ComponentInspectionAuto = iCompInspectAuto
            };

            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = " ",
                LastMessage = " ",
                Id = 0
            };

            using (var UndoComponentReplacementAction = new BLL.Core.Domain.Action(new DAL.GETContext(), ActionParam, UndoComponentReplacementParams))
            {
                UndoComponentReplacementAction.Operation.Start();

                if (UndoComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Close)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = UndoComponentReplacementAction.Operation.ActionLog;
                    rm.LastMessage = UndoComponentReplacementAction.Operation.Message;
                }

                if (UndoComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Started)
                {
                    result = UndoComponentReplacementAction.Operation.Message;
                    UndoComponentReplacementAction.Operation.Validate();
                }

                if (UndoComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Invalid)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = UndoComponentReplacementAction.Operation.ActionLog;
                    rm.LastMessage = UndoComponentReplacementAction.Operation.Message;
                }

                if (UndoComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Valid)
                {
                    UndoComponentReplacementAction.Operation.Commit();
                }

                if (UndoComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Failed)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = UndoComponentReplacementAction.Operation.ActionLog;
                    rm.LastMessage = UndoComponentReplacementAction.Operation.Message;
                }

                if (UndoComponentReplacementAction.Operation.Status == BLL.Core.Domain.ActionStatus.Succeed)
                {
                    rm.OperationSucceed = true;
                    rm.ActionLog = UndoComponentReplacementAction.Operation.ActionLog;
                    rm.LastMessage = UndoComponentReplacementAction.Operation.Message;
                }

                rm.Id = UndoComponentReplacementAction.Operation.UniqueId;
            }

            return result;
        }

        public List<GETObservationPointResultsSummaryVM> GetObservationPoints(int inspection_auto, int user_auto)
        {
            List<GETObservationPointResultsSummaryVM> result = new List<GETObservationPointResultsSummaryVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var opResultsInfo = (from s in dataEntities.GET_OBSERVATION_POINT_INSPECTION
                                     join t in dataEntities.GET_OBSERVATION_POINTS
                                     on s.observation_point_auto equals t.observation_point_auto
                                     where s.inspection_auto == inspection_auto
                                     select new
                                     {
                                         s.observation_point_inspection_auto,
                                         t.observation_name,
                                         s.measurement,
                                         s.comment,
                                         worn_pct = ((t.req_measure == true) && (t.worn_length.Value - t.initial_length.Value) > 0) ?
                                                100 - ((t.worn_length.Value - s.measurement) / (t.worn_length.Value - t.initial_length.Value)) * 100.0m : 0,
                                         t.req_measure
                                     });

                result = opResultsInfo.Select(
                    op => new GETObservationPointResultsSummaryVM
                    {
                        op_inspection_auto = op.observation_point_inspection_auto,
                        observation_name = op.observation_name,
                        measurement = op.measurement,
                        comment = op.comment,
                        condition = op.worn_pct,
                        req_measure = op.req_measure
                    }).ToList();
            }

            return result;
        }

        public List<GETInspectionDetailsVM> ReturnInspectionDetailsForGET(int get_auto)
        {
            List<GETInspectionDetailsVM> result = new List<GETInspectionDetailsVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                result = (from ii in dataEntities.GET_IMPLEMENT_INSPECTION
                          join ut in dataEntities.USER_TABLE
                            on ii.user_auto equals ut.user_auto
                          where ii.get_auto == get_auto
                          orderby ii.inspection_auto descending
                          select new GETInspectionDetailsVM
                          {
                              id = ii.inspection_auto,
                              condition = ii.eval,
                              inspectionDate = ii.inspection_date.ToString(),
                              inspector = ut.username,
                              life = ii.ltd
                          }).ToList();
            }

            return result;
        }

        public List<GETHistoryVM> ReturnHistoryForGET(int get_auto)
        {
            List<GETHistoryVMInternal> result = new List<GETHistoryVMInternal>();

            // Define actions to filter by.
            int ACTION_Inspection = (int)Core.Domain.GETActionType.Inspection;
            int ACTION_Implement_Setup = (int) Core.Domain.GETActionType.Implement_Setup;
            int ACTION_Component_Replacement = (int)Core.Domain.GETActionType.Component_Replacement;
            int ACTION_Undo_Component_Replacement = (int)Core.Domain.GETActionType.Undo_Component_Replacement;
            int ACTION_Change_Implement_Jobsite = (int)Core.Domain.GETActionType.Change_Implement_Jobsite;
            int ACTION_Attach_Implement_To_Equipment = (int)Core.Domain.GETActionType.Attach_Implement_to_Equipment;
            int ACTION_Move_Implement_To_Inventory = (int)Core.Domain.GETActionType.Move_Implement_To_Inventory;
            int ACTION_Change_Implement_Status = (int)Core.Domain.GETActionType.Change_Implement_Status;
            int ACTION_Component_Repair = (int)Core.Domain.GETActionType.Component_Repair;
            int ACTION_Implement_Updated = (int)Core.Domain.GETActionType.Implement_Updated;

            using (var dataEntities = new DAL.GETContext())
            {
                List<GETHistoryVMInternal> ImplementSetupActions =
                    ReturnImplementActionsForGET(get_auto, ACTION_Implement_Setup);

                List<GETHistoryVMInternal> ImplementUpdatedActions =
                    ReturnImplementActionsForGET(get_auto, ACTION_Implement_Updated);

                List<GETHistoryVMInternal> InspectionActions =
                    ReturnImplementActionsForGET(get_auto, ACTION_Inspection);

                List<GETHistoryVMInternal> ComponentReplacementActions =
                    ReturnComponentReplaceActionsForGET(get_auto, ACTION_Component_Replacement);

                List<GETHistoryVMInternal> UndoComponentReplacementActions =
                    ReturnComponentReplaceActionsForGET(get_auto, ACTION_Undo_Component_Replacement);

                List<GETHistoryVMInternal> ChangeImplementJobsiteActions =
                    ReturnChangeImplementJobsiteActionsForGET(get_auto, ACTION_Change_Implement_Jobsite);

                List<GETHistoryVMInternal> AttachImplementToEquipmentActions =
                    ReturnAttachImplementActionsForGET(get_auto, ACTION_Attach_Implement_To_Equipment);

                List<GETHistoryVMInternal> MoveImplementToInventoryActions =
                    ReturnMoveImplementActionsForGET(get_auto, ACTION_Move_Implement_To_Inventory);

                List<GETHistoryVMInternal> ChangeImplementStatusActions =
                    ReturnChangeImplementStatusActionsForGET(get_auto, ACTION_Change_Implement_Status);

                List<GETHistoryVMInternal> ComponentRepairActions =
                    ReturnComponentRepairActionsForGET(get_auto, ACTION_Component_Repair);

                result.AddRange(ImplementSetupActions);
                result.AddRange(ImplementUpdatedActions);
                result.AddRange(InspectionActions);
                result.AddRange(ComponentReplacementActions);
                result.AddRange(UndoComponentReplacementActions);
                result.AddRange(ChangeImplementJobsiteActions);
                result.AddRange(AttachImplementToEquipmentActions);
                result.AddRange(MoveImplementToInventoryActions);
                result.AddRange(ChangeImplementStatusActions);
                result.AddRange(ComponentRepairActions);

                // Sort by event_date and events_auto.
                result = result.OrderByDescending(s => DateTime.Parse(s.date))
                    .ThenByDescending(t => t.events_auto)
                    .ToList();
            }

            // Remove the events_auto in the final result, as we do not want to expose this value.
            var finalResult = result.Select(r => new GETHistoryVM
            {
                date = r.date,
                implement_life = r.implement_life,
                component = r.component,
                component_life = r.component_life,
                action_taken = r.action_taken,
                cost = r.cost,
                comment = r.comment
            }).ToList();

            return finalResult;
        }

        public List<GETHistoryVMInternal> ReturnImplementActionsForGET(int get_auto, int action)
        {
            List<GETHistoryVMInternal> result = new List<GETHistoryVMInternal>();           

            using (var dataEntities = new DAL.GETContext())
            {
                result = (from gei in dataEntities.GET_EVENTS_IMPLEMENT
                          join ge in dataEntities.GET_EVENTS
                             on gei.events_auto equals ge.events_auto
                          join ga in dataEntities.GET_ACTIONS
                             on ge.action_auto equals ga.actions_auto
                          where ga.actions_auto == action
                             && gei.get_auto == get_auto
                             && ge.recordStatus == 0
                          select new GETHistoryVMInternal
                          {
                              date = ge.event_date.ToString(),
                              implement_life = gei.ltd.ToString(),
                              component = "",
                              component_life = "",
                              action_taken = ga.action_name,
                              cost = (int)ge.cost,
                              comment = ge.comment
                          }).ToList();
            }

            return result;
        }


        public List<GETHistoryVMInternal> ReturnComponentReplaceActionsForGET(int get_auto, int action)
        {
            List<GETHistoryVMInternal> result = new List<GETHistoryVMInternal>();

            using (var dataEntities = new DAL.GETContext())
            {
                var result2 = (from gec in dataEntities.GET_EVENTS_COMPONENT
                               join ge in dataEntities.GET_EVENTS
                                 on gec.events_auto equals ge.events_auto
                               join ga in dataEntities.GET_ACTIONS
                                 on ge.action_auto equals ga.actions_auto
                               join gc in dataEntities.GET_COMPONENT
                                 on gec.get_component_auto equals gc.get_component_auto
                               join gsc in dataEntities.GET_SCHEMATIC_COMPONENT
                                 on gc.schematic_component_auto equals gsc.schematic_component_auto
                               join lct in dataEntities.LU_COMPART_TYPE
                                 on gsc.comparttype_auto equals lct.comparttype_auto
                               join geii in dataEntities.GET_EVENTS_IMPLEMENT
                                 on ge.events_auto equals geii.events_auto into gei2
                               from gei in gei2.DefaultIfEmpty()
                               where ga.actions_auto == action
                                 && gc.get_auto == get_auto
                                 && ge.recordStatus == 0
                               select new GETHistoryVMInternal
                               {
                                   date = ge.event_date.ToString(),
                                   implement_life = gei.ltd.ToString(),
                                   component = lct.comparttype,
                                   component_life = gec.ltd.ToString(),
                                   action_taken = ga.action_name,
                                   cost = (int)ge.cost,
                                   comment = ge.comment,
                                   events_auto = ge.events_auto,
                                   component_part_no = gc.part_no,
                                   recordStatus = gec.recordStatus
                               }).ToList();

                var components = result2.Select(r => r.events_auto).Distinct().ToList();
                for (int i = 0; i < components.Count; i++)
                {
                    var eventAuto = components[i];
                    var originalComponent = result2
                        .Where(r => r.events_auto == eventAuto && r.recordStatus == 1)
                        .FirstOrDefault();

                    var replacementComponent = result2
                        .Where(r => r.events_auto == eventAuto && r.recordStatus == 0)
                        .FirstOrDefault();

                    if(originalComponent != null && replacementComponent != null)
                    {
                        GETHistoryVMInternal newRecord = new GETHistoryVMInternal
                        {
                            events_auto = eventAuto,
                            date = originalComponent.date,
                            implement_life = originalComponent.implement_life,
                            component = originalComponent.component + " : " + originalComponent.component_part_no,
                            component_life = originalComponent.component_life,
                            action_taken = originalComponent.action_taken + " : "
                                + " Replaced " + originalComponent.component + " ( " + originalComponent.component_part_no + " ) "
                                + " With " + replacementComponent.component + " ( " + replacementComponent.component_part_no + " ) ",
                            cost = originalComponent.cost,
                            comment = originalComponent.comment,
                            component_part_no = originalComponent.component_part_no,
                            recordStatus = originalComponent.recordStatus
                        };
                        result.Add(newRecord);
                    }
                }
            }

            return result;
        }

        public List<GETHistoryVMInternal> ReturnChangeImplementJobsiteActionsForGET(int get_auto, int action)
        {
            List<GETHistoryVMInternal> result = new List<GETHistoryVMInternal>();

            using (var dataEntities = new DAL.GETContext())
            {
                var changeJobsiteRecords = (from e in dataEntities.GET_EVENTS
                                            join i in dataEntities.GET_EVENTS_IMPLEMENT
                                                on e.events_auto equals i.events_auto
                                            join iv in dataEntities.GET_EVENTS_INVENTORY
                                                on i.implement_events_auto equals iv.implement_events_auto
                                            join ga in dataEntities.GET_ACTIONS
                                                on e.action_auto equals ga.actions_auto
                                            where i.get_auto == get_auto
                                             && e.action_auto == action
                                             && e.recordStatus == 0
                                            orderby e.event_date descending, e.events_auto descending
                                            select new
                                            {
                                                events_auto = e.events_auto,
                                                sDate = e.event_date.ToString(),
                                                date = e.event_date,
                                                action_taken = ga.action_name,
                                                implement_life = i.ltd,
                                                cost = e.cost,
                                                comment = e.comment,
                                                jobsite_auto = iv.jobsite_auto
                                            }).ToList();

                foreach (var item in changeJobsiteRecords)
                {
                    DateTime item_date = new DateTime(item.date.Year, item.date.Month, item.date.Day, 23, 59, 59);

                    // Find the previous Inventory event for this implement.
                    var movedToInventoryRecord = (from e in dataEntities.GET_EVENTS
                                                  join ie in dataEntities.GET_EVENTS_IMPLEMENT
                                                    on e.events_auto equals ie.events_auto
                                                  join iv in dataEntities.GET_EVENTS_INVENTORY
                                                    on ie.implement_events_auto equals iv.implement_events_auto
                                                  where e.events_auto < item.events_auto
                                                    && e.event_date <= item_date
                                                    && e.recordStatus == 0
                                                  orderby e.event_date descending, e.events_auto descending
                                                  select new
                                                  {
                                                      iv.inventory_events_auto,
                                                      iv.jobsite_auto
                                                  }).FirstOrDefault();

                    string previousJobsite = "UNKNOWN";
                    if(movedToInventoryRecord != null)
                    {
                        previousJobsite = dataEntities.CRSF.Find(movedToInventoryRecord.jobsite_auto).site_name;
                    }

                    string currentJobsite = dataEntities.CRSF.Find(item.jobsite_auto).site_name;

                    GETHistoryVMInternal newRecord = new GETHistoryVMInternal
                    {
                        events_auto = item.events_auto,
                        date = item.sDate,
                        implement_life = item.implement_life.ToString(),
                        component = "",
                        component_life = "",
                        action_taken = item.action_taken + " : "
                                + " Implement moved from jobsite " + previousJobsite 
                                + " to " + currentJobsite + ".",
                        cost = item.cost,
                        comment = item.comment,
                        component_part_no = "",
                        recordStatus = 0
                    };
                    result.Add(newRecord);
                }
            }


            return result;
        }

        public List<GETHistoryVMInternal> ReturnAttachImplementActionsForGET(int get_auto, int action)
        {
            List<GETHistoryVMInternal> result = new List<GETHistoryVMInternal>();

            using (var dataEntities = new DAL.GETContext())
            {
                result = (from e in dataEntities.GET_EVENTS
                          join i in dataEntities.GET_EVENTS_IMPLEMENT
                              on e.events_auto equals i.events_auto
                          join ee in dataEntities.GET_EVENTS_EQUIPMENT
                              on e.events_auto equals ee.events_auto
                          join eq in dataEntities.EQUIPMENTs
                              on ee.equipment_auto equals eq.equipmentid_auto
                          join ga in dataEntities.GET_ACTIONS
                              on e.action_auto equals ga.actions_auto
                          where i.get_auto == get_auto
                           && e.action_auto == action
                           && e.recordStatus == 0
                          orderby e.event_date descending, e.events_auto descending
                          select new GETHistoryVMInternal
                          {
                              events_auto = e.events_auto,
                              date = e.event_date.ToString(),
                              action_taken = ga.action_name + " : "
                                + " Implement attached to "
                                + eq.serialno,
                              component = "",
                              component_life = "",
                              implement_life = i.ltd.ToString(),
                              cost = e.cost,
                              comment = e.comment,
                              component_part_no = "",
                              recordStatus = 0
                          }).ToList();

            }

            return result;
        }

        public List<GETHistoryVMInternal> ReturnMoveImplementActionsForGET(int get_auto, int action)
        {
            List<GETHistoryVMInternal> result = new List<GETHistoryVMInternal>();

            using (var dataEntities = new DAL.GETContext())
            {
                result = (from e in dataEntities.GET_EVENTS
                          join i in dataEntities.GET_EVENTS_IMPLEMENT
                              on e.events_auto equals i.events_auto
                          join ee in dataEntities.GET_EVENTS_EQUIPMENT
                              on e.events_auto equals ee.events_auto
                          join eq in dataEntities.EQUIPMENTs
                              on ee.equipment_auto equals eq.equipmentid_auto
                          join ga in dataEntities.GET_ACTIONS
                              on e.action_auto equals ga.actions_auto
                          where i.get_auto == get_auto
                           && e.action_auto == action
                           && e.recordStatus == 0
                          orderby e.event_date descending, e.events_auto descending
                          select new GETHistoryVMInternal
                          {
                              events_auto = e.events_auto,
                              date = e.event_date.ToString(),
                              action_taken = ga.action_name + " : "
                                + " Implement removed from "
                                + eq.serialno,
                              component = "",
                              component_life = "",
                              implement_life = i.ltd.ToString(),
                              cost = e.cost,
                              comment = e.comment,
                              component_part_no = "",
                              recordStatus = 0
                          }).ToList();
            }

            return result;
        }

        public List<GETHistoryVMInternal> ReturnChangeImplementStatusActionsForGET(int get_auto, int action)
        {
            List<GETHistoryVMInternal> result = new List<GETHistoryVMInternal>();

            using (var dataEntities = new DAL.GETContext())
            {
                var changeStatusRecords = (from e in dataEntities.GET_EVENTS
                                            join i in dataEntities.GET_EVENTS_IMPLEMENT
                                                on e.events_auto equals i.events_auto
                                            join iv in dataEntities.GET_EVENTS_INVENTORY
                                                on i.implement_events_auto equals iv.implement_events_auto
                                            join ga in dataEntities.GET_ACTIONS
                                                on e.action_auto equals ga.actions_auto
                                            where i.get_auto == get_auto
                                             && e.action_auto == action
                                             && e.recordStatus == 0
                                           orderby e.event_date descending, e.events_auto descending
                                           select new
                                            {
                                                events_auto = e.events_auto,
                                                sDate = e.event_date.ToString(),
                                                date = e.event_date,
                                                action_taken = ga.action_name,
                                                implement_life = i.ltd,
                                                cost = e.cost,
                                                comment = e.comment,
                                                jobsite_auto = iv.jobsite_auto,
                                                iv.inventory_status.status_desc,
                                                workshop = iv.workshop != null ? iv.workshop.name : "",
                                                repairer = iv.workshop != null ? iv.workshop.repairer.name : ""
                                           }).ToList();

                foreach (var item in changeStatusRecords)
                {
                    DateTime item_date = new DateTime(item.date.Year, item.date.Month, item.date.Day, 23, 59, 59);

                    // Find the previous Inventory event for this implement.
                    var inventoryStatusRecord = (from e in dataEntities.GET_EVENTS
                                                  join ie in dataEntities.GET_EVENTS_IMPLEMENT
                                                    on e.events_auto equals ie.events_auto
                                                  join iv in dataEntities.GET_EVENTS_INVENTORY
                                                    on ie.implement_events_auto equals iv.implement_events_auto
                                                  where e.events_auto < item.events_auto
                                                    && e.event_date <= item_date
                                                    && e.recordStatus == 0
                                                 orderby e.event_date descending, e.events_auto descending
                                                 select new
                                                  {
                                                      iv.inventory_events_auto,
                                                      iv.inventory_status.status_desc,
                                                      workshop = iv.workshop != null ? iv.workshop.name : "",
                                                      repairer = iv.workshop != null ? iv.workshop.repairer.name : ""
                                                  }).FirstOrDefault();

                    string previousStatus = "UNKNOWN";
                    if (inventoryStatusRecord != null)
                    {
                        previousStatus = inventoryStatusRecord.status_desc;
                    }

                    string currentStatus = item.status_desc;

                    GETHistoryVMInternal newRecord = new GETHistoryVMInternal
                    {
                        events_auto = item.events_auto,
                        date = item.sDate,
                        implement_life = item.implement_life.ToString(),
                        component = "",
                        component_life = "",
                        action_taken = item.action_taken + " : "
                                + " Implement status changed from " + previousStatus
                                + " to " + currentStatus,
                        cost = item.cost,
                        comment = item.comment,
                        component_part_no = "",
                        recordStatus = 0
                    };

                    // Display the workshop and repairer information (if available)
                    // Both previous and current status have a workshop allocation.
                    if((item.workshop != "") && (inventoryStatusRecord.workshop != ""))
                    {
                        newRecord.action_taken = item.action_taken + " : "
                                + " Implement status changed from " + previousStatus
                                + " at " + inventoryStatusRecord.workshop
                                    + " ( " + inventoryStatusRecord.repairer + " ) "
                                + " to " + currentStatus
                                + " at " + item.workshop
                                    + " ( " + item.repairer + " ) ";
                    }
                    // Only the current status has a workshop allocation.
                    else if((item.workshop != "") && (inventoryStatusRecord.workshop == ""))
                    {
                        newRecord.action_taken = item.action_taken + " : "
                                + " Implement status changed from " + previousStatus
                                + " to " + currentStatus
                                + " at " + item.workshop
                                    + " ( " + item.repairer + " ) ";
                    }
                    // The previous status had a workshop allocation.
                    else if ((item.workshop == "") && (inventoryStatusRecord.workshop != ""))
                    {
                        newRecord.action_taken = item.action_taken + " : "
                                + " Implement status changed from " + previousStatus
                                + " at " + inventoryStatusRecord.workshop
                                    + " ( " + inventoryStatusRecord.repairer + " ) "
                                + " to " + currentStatus;
                    }

                    result.Add(newRecord);
                }
            }

            return result;
        }

        public List<GETHistoryVMInternal> ReturnComponentRepairActionsForGET(int get_auto, int action)
        {
            List<GETHistoryVMInternal> result = new List<GETHistoryVMInternal>();

            using (var dataEntities = new DAL.GETContext())
            {
                var componentRepairRecords = (from gec in dataEntities.GET_EVENTS_COMPONENT
                                              join ge in dataEntities.GET_EVENTS
                                                on gec.events_auto equals ge.events_auto
                                              join ga in dataEntities.GET_ACTIONS
                                                on ge.action_auto equals ga.actions_auto
                                              join gc in dataEntities.GET_COMPONENT
                                                on gec.get_component_auto equals gc.get_component_auto
                                              join gsc in dataEntities.GET_SCHEMATIC_COMPONENT
                                                on gc.schematic_component_auto equals gsc.schematic_component_auto
                                              join lct in dataEntities.LU_COMPART_TYPE
                                                on gsc.comparttype_auto equals lct.comparttype_auto
                                              join gei in dataEntities.GET_EVENTS_IMPLEMENT
                                                on ge.events_auto equals gei.events_auto
                                              where ga.actions_auto == action
                                                && gc.get_auto == get_auto
                                                && ge.recordStatus == 0
                                              select new 
                                              {
                                                  sDate = ge.event_date.ToString(),
                                                  date = ge.event_date,
                                                  implement_life = gei.ltd.ToString(),
                                                  component = lct.comparttype,
                                                  component_life = gec.ltd.ToString(),
                                                  action_taken = ga.action_name,
                                                  cost = (int)ge.cost,
                                                  comment = ge.comment,
                                                  events_auto = ge.events_auto,
                                                  component_part_no = gc.part_no,
                                                  recordStatus = gec.recordStatus
                                              }).ToList();

                foreach (var item in componentRepairRecords)
                {
                    DateTime item_date = new DateTime(item.date.Year, item.date.Month, item.date.Day, 23, 59, 59);

                    // Find the previous Inventory event for this implement.
                    var inventoryStatusRecord = (from e in dataEntities.GET_EVENTS
                                                 join ie in dataEntities.GET_EVENTS_IMPLEMENT
                                                   on e.events_auto equals ie.events_auto
                                                 join iv in dataEntities.GET_EVENTS_INVENTORY
                                                   on ie.implement_events_auto equals iv.implement_events_auto
                                                 where e.events_auto < item.events_auto
                                                    && ie.get_auto == get_auto
                                                    && e.event_date <= item_date
                                                    && e.recordStatus == 0
                                                 orderby e.event_date descending, e.events_auto descending
                                                 select new
                                                 {
                                                     iv.inventory_events_auto
                                                 }).FirstOrDefault();

                    string repairerName = "UNKNOWN";
                    string workshopName = "UNKNOWN";
                    if (inventoryStatusRecord != null)
                    {
                        var inventoryRecord = dataEntities.GET_EVENTS_INVENTORY
                            .Find(inventoryStatusRecord.inventory_events_auto);
                        if(inventoryRecord != null)
                        {
                            try
                            {
                                repairerName = inventoryRecord.workshop.repairer.name;
                                workshopName = inventoryRecord.workshop.name;
                            }
                            catch (Exception ex1)
                            {
                                repairerName = "UNKNOWN";
                                workshopName = "UNKNOWN";
                            }
                        }   
                    }

                    GETHistoryVMInternal newRecord = new GETHistoryVMInternal
                    {
                        events_auto = item.events_auto,
                        date = item.sDate,
                        implement_life = item.implement_life.ToString(),
                        component = item.component,
                        component_life = item.component_life,
                        action_taken = "Repair by " + repairerName + " - " + workshopName,
                        cost = item.cost,
                        comment = item.comment,
                        component_part_no = "",
                        recordStatus = 0
                    };
                    result.Add(newRecord);
                }
            }

            return result;
        }

        public bool rotatePhoto(string type, int photoId, bool rotateCW)
        {
            bool result = false;

            using (var dataEntities = new DAL.GETContext())
            {
                if(type == "general_questions")
                {
                    var imageEntity = dataEntities.GET_IMPLEMENT_INSPECTION_IMAGE.Find(photoId);
                    if (imageEntity == null)
                    {
                        return false;
                    }
                    imageEntity.inspection_photo = performImageRotation(imageEntity.inspection_photo, rotateCW);
                }
                else if (type == "observations")
                {
                    var imageEntity = dataEntities.GET_OBSERVATION_IMAGE.Find(photoId);
                    if (imageEntity == null)
                    {
                        return false;
                    }
                    imageEntity.observation_photo = performImageRotation(imageEntity.observation_photo, rotateCW);
                }
                else if (type == "observationsOP")
                {
                    var imageEntity = dataEntities.GET_OBSERVATION_POINT_IMAGES.Find(photoId);
                    if (imageEntity == null)
                    {
                        return false;
                    }
                    imageEntity.observation_photo = performImageRotation(imageEntity.observation_photo, rotateCW);
                }
                else if (type == "componentPhoto")
                {
                    var imageEntity = dataEntities.GET_COMPONENT_INSPECTION_IMAGES.Find(photoId);
                    if (imageEntity == null)
                    {
                        return false;
                    }
                    imageEntity.component_photo = performImageRotation(imageEntity.component_photo, rotateCW);
                }
                else if (type == "observationPointPhoto")
                {
                    var imageEntity = dataEntities.GET_OBSERVATION_POINT_INSPECTION_IMAGES.Find(photoId);
                    if (imageEntity == null)
                    {
                        return false;
                    }
                    imageEntity.observation_point_photo = performImageRotation(imageEntity.observation_point_photo, rotateCW);
                }
                else
                {
                    return false;
                }

                try
                {
                    dataEntities.SaveChanges();
                    result = true;
                }
                catch
                {
                    result = false;
                }
            }

            return result;
        }

        private byte[] performImageRotation(byte[] imageData, bool rotateCW)
        {
            var memStream = new MemoryStream(imageData);
            Image img = Image.FromStream(memStream, true);
            var imgSize = memStream.Length;
            memStream.Dispose();

            // Clockwise rotation
            if (rotateCW)
            {
                img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            }
            // Counter-clockwise rotation
            else
            {
                img.RotateFlip(RotateFlipType.Rotate270FlipNone);
            }

            var newImg = new byte[imgSize];
            newImg = ImageToByteArray(img);

            return newImg;
        }

        private byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            ms.Position = 0;
            return ms.ToArray();
        }

        public int ReturnGETForInspection(string inspectionAuto)
        {
            int result = 0;
            int iInspectionAuto = int.TryParse(inspectionAuto, out iInspectionAuto) ? iInspectionAuto : 0;

            using (var dataEntities = new DAL.GETContext())
            {
                result = dataEntities.GET_IMPLEMENT_INSPECTION.Find(iInspectionAuto).get_auto;
            }

            return result;
        }
    }
}
 
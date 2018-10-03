using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using BLL.Extensions;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;

namespace BLL.Core.Domain
{
    public class Inspection
    {
        private DbContext pvContext;
        private TRACK_INSPECTION DALInspection;
        private int Id = 0;
        private UndercarriageContext _context
        {
            get { return pvContext as UndercarriageContext; }
        }
        public Inspection(DbContext UCcontext)
        {
            pvContext = UCcontext;
        }
        public Inspection(DbContext UCcontext, int id)
        {
            pvContext = UCcontext;
            Id = id;
        }
        public TRACK_INSPECTION getDALInspection()
        {
            if (Id == 0)
                return null;
            if (DALInspection != null && DALInspection.inspection_auto == Id)
                return DALInspection;
            DALInspection = _context.TRACK_INSPECTION.Find(Id);
            return DALInspection;
        }

        public string getDetailComment(int detailId)
        {
            var detail = _context.TRACK_INSPECTION_DETAIL.Find(detailId);
            if (detail == null)
                return "";
            return detail.comments;
        }

        /// <summary>
        /// Rotates the photo attached to the given inspection detail record 90 degrees clockwise.
        /// </summary>
        /// <param name="inspectionDetailAuto">The inspection_detail_auto the photo you want to rotate references. </param>
        /// <returns>True if saved successfully. False if it fails to save. </returns>
        public bool RotateDetailPhotoClockwise(int inspectionDetailAuto)
        {
            var imageEntity = _context.TRACK_INSPECTION_IMAGES.Where(i => i.inspection_detail_auto == inspectionDetailAuto.ToString()).FirstOrDefault();
            if (imageEntity == null)
                return true;
            var memStream = new MemoryStream(imageEntity.image_data);
            Image img = Image.FromStream(memStream, true);
            var imgSize = memStream.Length;
            memStream.Dispose();
            img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            var newImg = new byte[imgSize];
            newImg = ImageToByteArray(img);
            imageEntity.image_data = newImg;
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Rotates the photo attached to the given inspection detail record 90 degrees counter clockwise.
        /// </summary>
        /// <param name="inspectionDetailAuto">The inspection_detail_auto the photo you want to rotate references. </param>
        /// <returns>True if saved successfully. False if it fails to save. </returns>
        public bool RotateDetailPhotoCounterClockwise(int inspectionDetailAuto)
        {
            var imageEntity = _context.TRACK_INSPECTION_IMAGES.Where(i => i.inspection_detail_auto == inspectionDetailAuto.ToString()).FirstOrDefault();
            if (imageEntity == null)
                return true;
            var memStream = new MemoryStream(imageEntity.image_data);
            Image img = Image.FromStream(memStream, true);
            var imgSize = memStream.Length;
            memStream.Dispose();
            img.RotateFlip(RotateFlipType.Rotate270FlipNone);
            var newImg = new byte[imgSize];
            newImg = ImageToByteArray(img);
            imageEntity.image_data = newImg;
            try
            {
                _context.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            ms.Position = 0;
            return ms.ToArray();
        }

        /// <summary>
        /// Deletes the photo attached to an inspection detail id from the TRACK_INSPECTION_IMAGES table. 
        /// </summary>
        /// <param name="inspectionDetailId">The inspection_detail_auto which the image references. </param>
        /// <returns>Returns true if the image was deleted successfully, or if there was no image to delete. False if it fails. </returns>
        public bool DeleteDetailPhoto(int inspectionDetailId)
        {
            var imageEntity = _context.TRACK_INSPECTION_IMAGES.Where(i => i.inspection_detail_auto == inspectionDetailId.ToString()).FirstOrDefault();

            // Couldn't find an image.
            if (imageEntity == null)
                return true;
            try
            {
                _context.TRACK_INSPECTION_IMAGES.Remove(imageEntity);
                _context.SaveChanges();
            }
            catch
            {
                // Errored and couldn't delete the image.
                return false;
            }

            // Image was deleted successfully
            return true;
        }

        public TRACK_INSPECTION getDALInspection(int InspectionId)
        {
            if (InspectionId == 0)
                return null;
            if (DALInspection != null && DALInspection.inspection_auto == InspectionId)
                return DALInspection;
            DALInspection = _context.TRACK_INSPECTION.Find(InspectionId);
            return DALInspection;
        }
        /// <summary>
        /// Returns true if this inspection has been inspected 
        /// </summary>
        /// <returns></returns>
        public bool IsInterpreted()
        {
            if (getDALInspection() != null && getDALInspection().quote_auto != null && getDALInspection().quote_auto > 0)
                return true;
            return false;
        }

        public InspectionUnsyncedVwMdl GetUnsyncedInspection(int Id)
        {
            var invalidResult = new InspectionUnsyncedVwMdl { Id = 0 };
            if (Id == 0)
                return invalidResult;

            var inspections = _context.Mbl_Track_Inspection.Where(m => m.inspection_auto == Id);
            DAL.Mbl_Track_Inspection inspection = null;
            if (inspections.Count() > 0)
                inspection = inspections.First();
            if (inspection == null)
                return invalidResult;
            var equipments = _context.Mbl_NewEquipment.Where(m => m.equipmentid_auto == inspection.equipmentid_auto);
            DAL.Mbl_NewEquipment mbl_equipment = null;
            if (equipments.Count() > 0)
            {
                mbl_equipment = equipments.First();
            }
            if (mbl_equipment == null)
                return invalidResult;
            var inspectionDetails = _context.Mbl_Track_Inspection_Detail.Where(m => m.inspection_auto == inspection.inspection_auto).ToList();
            var result = new InspectionUnsyncedVwMdl
            {
                Id = inspection.inspection_auto,
                MatchedEquipmentId = (mbl_equipment.pc_equipmentid_auto ?? 0).LongNullableToInt(),
                MobileEqId = mbl_equipment.equipmentid_auto.LongNullableToInt(),
                Customer = mbl_equipment.customer_name,
                InspectionDate = inspection.inspection_date.ToString("dd MMM yyyy"),
                InspectionNotes = inspection.inspection_comments,
                Model = mbl_equipment.model ?? "N/A",
                Serial = mbl_equipment.serialno,
                UnitNumber = mbl_equipment.unitno,
                SMU = inspection.smu ?? 0,
                JobSiteDetails = new JobSiteDetailsUnsyncedVwMdl
                {
                    Id = mbl_equipment.crsf_auto.LongNullableToInt(),
                    Abrasive = inspection.abrasive.getLabelForImpact(),
                    Impact = inspection.impact.getLabelForImpact(),
                    Moisture = inspection.moisture.getLabelForImpact(),
                    Packing = inspection.packing.getLabelForImpact(),
                    DryJointsLeft = inspection.dry_joints_left ?? 0,
                    DryJointsRight = inspection.dry_joints_right ?? 0,
                    ExtCannonLeft = inspection.ext_cannon_left ?? 0,
                    ExtCannonRight = inspection.ext_cannon_right ?? 0,
                    JobSiteName = mbl_equipment.jobsite_name,
                    JobSiteNotes = inspection.Jobsite_Comms,
                    TrakSagLeft = inspection.track_sag_left ?? 0,
                    TrakSagRight = inspection.track_sag_right ?? 0
                }
            };

            var matchedModel = getMatchingForInspectionSync(result.MatchedEquipmentId, mbl_equipment.equipmentid_auto.LongNullableToInt());

            var inspectionDetailsResult = new List<ComponentUnsyncedVwMdl>();
            foreach (var detail in inspectionDetails)
            {
                var components = _context.Mbl_NewGENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == mbl_equipment.equipmentid_auto && m.compartid_auto == detail.track_unit_auto);
                DAL.Mbl_NewGENERAL_EQ_UNIT component = null;
                if (components.Count() > 0)
                    component = components.First();
                if (component == null)
                    continue;
                var images = _context.Mbl_CompartAttach_filestream.Where(m => m.compartid_auto == component.compartid_auto && m.compart_attach_type_auto - 2 == component.side && inspection.inspection_auto == m.inspection_auto);
                Mbl_CompartAttach_filestream image = null;
                if (images.Count() > 0)
                    image = images.First();
                string imageBase64 = "";
                try { imageBase64 = Convert.ToBase64String(image.attachment); } catch { }
                var item = new ComponentUnsyncedVwMdl
                {
                    Id = detail.inspection_detail_auto,
                    ComponentId = matchedModel.Where(m => m.Id == component.equnit_auto).FirstOrDefault() == null ? 0 : matchedModel.Where(m => m.Id == component.equnit_auto).FirstOrDefault().MatchedComponentId,
                    Component = component.compartsn,
                    Comments = detail.comments ?? "",
                    Image = imageBase64,
                    Position = ((int)component.pos).PositionLabel(component.compartsn.getCompartTypeId()) ?? "",
                    Reading = detail.reading,
                    Side = component.side == 1 ? Side.Left : Side.Right
                };
                inspectionDetailsResult.Add(item);
            }
            result.InspectionDetails = inspectionDetailsResult;
            return result;
        }

        /// <summary>
        /// Returns a list of inspections based on the provided inspection Id
        /// </summary>
        /// <param name="InspectionId"></param>
        /// <returns></returns>
        public List<int> getInspectionIds(int InspectionId)
        {
            var inspection = _context.TRACK_INSPECTION.Find(InspectionId);
            if (inspection == null)
                return new List<int>();
            int EqId = inspection.equipmentid_auto.LongNullableToInt();
            return _context.TRACK_INSPECTION.Where(m => m.equipmentid_auto == EqId && m.ActionTakenHistory.recordStatus == (int)(RecordStatus.Available)).OrderBy(m => m.inspection_date).Select(m => m.inspection_auto).ToList();
        }


        /// <summary>
        /// Returns a datatable to replace return_track_inspection SP in the Old UC
        /// </summary>
        /// <param name="InspectionId"></param>
        /// <returns></returns>
        public System.Data.DataTable GetInspectionDataTableForOldUI(int InspectionId)
        {
            var inspection = getDALInspection(InspectionId);
            var ids = new List<int>();
            try
            {
                ids = inspection.TRACK_INSPECTION_DETAIL.Select(i => new
                {
                    inspection_detail_auto = i.inspection_detail_auto,
                    sortOrder = i.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.sorder,
                    isChild = i.GENERAL_EQ_UNIT.LU_COMPART.compartid_auto.isAChildCompart(),
                    side = (i.SIDE != null ? i.SIDE.Side : (i.GENERAL_EQ_UNIT.side ?? 0)),
                    position = (int)(i.GENERAL_EQ_UNIT.pos ?? 0)
                }).ToList().OrderBy(l => l.side).ThenBy(l => l.sortOrder).ThenBy(l => l.isChild).ThenBy(l => l.position).Select(x => x.inspection_detail_auto).ToList();

            }
            catch {
                ids = inspection.TRACK_INSPECTION_DETAIL.Select(i => i.inspection_detail_auto).ToList();
            }
            
            return inspection.TRACK_INSPECTION_DETAIL.Select(m => new
            {
                equnit_auto = m.track_unit_auto,
                comparttype = m.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.comparttype,
                compart = m.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.comparttype + ":" + m.GENERAL_EQ_UNIT.LU_COMPART.compart,
                compartid_auto = m.GENERAL_EQ_UNIT.compartid_auto,
                track_comp_cts_maintype = m.GENERAL_EQ_UNIT.LU_COMPART.track_comp_cts_maintype,
                track_comp_cts_subtype = m.GENERAL_EQ_UNIT.LU_COMPART.track_comp_cts_subtype,
                pos = ((int)(m.GENERAL_EQ_UNIT.pos ?? 0)).PositionLabel(m.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto), // (m.GENERAL_EQ_UNIT.pos == null || m.GENERAL_EQ_UNIT.pos == 0 || m.GENERAL_EQ_UNIT.LU_COMPART.compartid_auto.isAChildCompart()) ? "" : m.GENERAL_EQ_UNIT.pos.ToString(),
                side = m.SIDE.Side,
                eval_code = m.eval_code,
                tool_name = m.TRACK_TOOL.tool_name,
                reading = m.reading,
                worn_percentage = m.worn_percentage,
                component_hours = m.hours_on_surface,
                remaining_hours = m.remaining_hours,
                ext_remaining_hours = m.ext_remaining_hours,
                expected_life = m.projected_hours,
                ext_expected_life = m.ext_projected_hours,
                eval_code1 = m.eval_code,
                track_unit_auto = m.track_unit_auto,
                smcs_code = m.GENERAL_EQ_UNIT.LU_COMPART.smcs_code,
                eq_ltd_at_install = m.GENERAL_EQ_UNIT.eq_ltd_at_install,
                smu_at_install = m.GENERAL_EQ_UNIT.smu_at_install,
                imgCountLeft = m.Images.Count(),
                imgCountRight = m.Images.Count(),
                compartid = m.GENERAL_EQ_UNIT.LU_COMPART.compartid,
                comments = m.comments,
                inspection_detail_auto = m.inspection_detail_auto,
                side1 = m.SIDE.Side,
                sorder = m.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.sorder,
                pos1 = ((int)(m.GENERAL_EQ_UNIT.pos ?? 0)).PositionLabel(m.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto),//(m.GENERAL_EQ_UNIT.pos == null || m.GENERAL_EQ_UNIT.pos == 0 || m.GENERAL_EQ_UNIT.LU_COMPART.compartid_auto.isAChildCompart()) ? "" : m.GENERAL_EQ_UNIT.pos.ToString(),
                comparttypeid = m.GENERAL_EQ_UNIT.LU_COMPART.LU_COMPART_TYPE.comparttypeid,
                cost = m.GENERAL_EQ_UNIT.equnit_auto.CalcTotalComponentCost(),
                costperhour = m.GENERAL_EQ_UNIT.equnit_auto.CalcTotalComponentCost(),
                Serialno = (m.UCSystemId != null && m.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto.ShouldDisplaySystemSerialNextToComponentType((int)m.GENERAL_EQ_UNIT.pos)) ? m.LU_Module_Sub.Serialno : "",
                equipmentid_auto = m.GENERAL_EQ_UNIT.equipmentid_auto,
                historytable_auto = m.TRACK_INSPECTION.ActionHistoryId,
                inspection_date = m.TRACK_INSPECTION.inspection_date
            }).OrderBy(i => ids.IndexOf(i.inspection_detail_auto)).ToList().ToDataTable();
        }

        public class matchingModel
        {
            public int Id { get; set; }
            /// <summary>
            /// There is no relation between mobile inspection details and mobile general eq unit
            /// BUT compart Id in 'mbl geu' is the same as 'track unit auto' in 'mbl_inspection_details'
            /// I think compartId is miss understood and has been used as componentId
            /// ComponentIndex is that one 
            /// </summary>
            public int ComponentIndex { get; set; }
            public int Side { get; set; }
            public int TypeId { get; set; } = 0;
            public int Pos { get; set; } = 0;
            public int MatchedComponentId { get; set; } = 0;
        }

        /// <summary>
        /// returns matching model of mobile and database for the given equipments
        /// result->MatchedComponentId = G_E_U.Id or (real component Id)
        /// if any error occurs result contains no rows
        /// It checks side, position and type of the components.
        /// Type is string in mobile table which is converted to corresponding Type Id
        /// by using getCompartTypeId() in type extension name space
        /// </summary>
        /// <param name="equipmentId"></param>
        /// <param name="mobileEquipmentId"></param>
        /// <returns></returns>
        public List<matchingModel> getMatchingForInspectionSync(int equipmentId, int mobileEquipmentId)
        {
            var result = new List<matchingModel>();
            var equipment = _context.EQUIPMENTs.Find(equipmentId);

            if (equipment == null) return result;
            var mobileEquipments = _context.Mbl_NewEquipment.Where(m => m.equipmentid_auto == mobileEquipmentId);
            if (mobileEquipments.Count() == 0) return result;

            var mobileEquipment = mobileEquipments.First();
            var components = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == equipmentId).Select(m => new matchingModel { Id = (int)m.equnit_auto, MatchedComponentId = 0, Pos = m.pos ?? 0, Side = m.side ?? 0, TypeId = m.LU_COMPART.comparttype_auto }).ToList();
            var mobileComponentsOriginial = _context.Mbl_NewGENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == mobileEquipmentId);

            List<matchingModel> mobileComponents = new List<matchingModel>();
            foreach (var m in mobileComponentsOriginial)
            {
                mobileComponents.Add(
                    new matchingModel
                    {
                        Id = m.equnit_auto.LongNullableToInt(),
                        ComponentIndex = m.compartid_auto,
                        MatchedComponentId = 0,
                        Pos = m.pos ?? 0,
                        Side = m.side ?? 0,
                        TypeId = m.compartsn.getCompartTypeId()
                    }
                    );
            }

            ///Data from mobile -> position for link, bushing, shoe, sprocket and guard is always 0
            ///but in general_eq_unit position for these is all 1
            ///in this loop it will be fixed
            foreach (var c in mobileComponents) {
                if (c.TypeId == (int)CompartTypeEnum.Link || c.TypeId == (int)CompartTypeEnum.Bushing || c.TypeId == (int)CompartTypeEnum.Shoe || c.TypeId == (int)CompartTypeEnum.Sprocket || c.TypeId == (int)CompartTypeEnum.Guard)
                    c.Pos++;
            }

            foreach (var match in components)
            {
                foreach (var m in mobileComponents)
                {
                    if (m.MatchedComponentId == 0 && match.Side == m.Side && (match.Pos == m.Pos || (match.Pos == 0 && m.Pos == 1) || (match.Pos == 1 && m.Pos == 0)) && match.TypeId == m.TypeId)
                    {
                        m.MatchedComponentId = match.Id;
                        match.MatchedComponentId = m.Id;
                    }
                }
            }
            return mobileComponents;
        }

        /// <summary>
        /// Checks to see if components on mobile and database for the given equipments are match
        /// if yes returns operationsucceeded as true otherwise false
        /// It checks side, position and type of the components.
        /// Type is string in mobile table which is converted to corresponding Type Id
        /// by using getCompartTypeId() in type extension name space
        /// </summary>
        /// <param name="equipmentId"></param>
        /// <param name="mobileEquipmentId"></param>
        /// <returns></returns>
        public ResultMessage checkEquipmentForInspectionSync(int equipmentId, int mobileEquipmentId)
        {
            var equipment = _context.EQUIPMENTs.Find(equipmentId);
            if (equipment == null) return new ResultMessage { Id = 0, LastMessage = "Equipment not found!", ActionLog = "Equipment not found in checkEquipmentForInspectionSync!", OperationSucceed = false };
            var mobileEquipments = _context.Mbl_NewEquipment.Where(m => m.equipmentid_auto == mobileEquipmentId);
            if (mobileEquipments.Count() == 0) return
                    new ResultMessage { Id = 0, LastMessage = "Mobile Equipment not found!", ActionLog = "Mobile Equipment not found in checkEquipmentForInspectionSync!", OperationSucceed = false };
            var mobileEquipment = mobileEquipments.First();
            var components = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == equipmentId).Select(m => new matchingModel { Id = (int)m.equnit_auto, MatchedComponentId = 0, Pos = m.pos ?? 0, Side = m.side ?? 0, TypeId = m.LU_COMPART.comparttype_auto }).ToList();
            var mobileComponentsOriginial = _context.Mbl_NewGENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == mobileEquipmentId);

            List<matchingModel> mobileComponents = new List<matchingModel>();
            foreach (var m in mobileComponentsOriginial)
            {
                mobileComponents.Add(
                    new matchingModel
                    {
                        Id = m.equnit_auto.LongNullableToInt(),
                        MatchedComponentId = 0,
                        Pos = m.pos ?? 0,
                        Side = m.side ?? 0,
                        TypeId = m.compartsn.getCompartTypeId()
                    }
                    );
            }

            foreach (var match in components)
            {
                foreach (var m in mobileComponents)
                {
                    if (m.MatchedComponentId == 0 && match.Side == m.Side && match.Pos == m.Pos && match.TypeId == m.TypeId)
                    {
                        m.MatchedComponentId = match.Id;
                        match.MatchedComponentId = m.Id;
                    }
                }
            }

            string errorMessage = "";
            int unmatchedMain = components.Where(m => m.MatchedComponentId == 0).Count();
            int unmatchedMobile = mobileComponents.Where(m => m.MatchedComponentId == 0).Count();

            if (unmatchedMain == 0 && unmatchedMobile == 0)
                return new ResultMessage { Id = 0, ActionLog = "All are matched!", LastMessage = "All components are matched!", OperationSucceed = true };

            if (unmatchedMain == 0 && unmatchedMobile > 0)
                errorMessage = "There " + (unmatchedMobile == 1 ? "is one component inspected which does " : "are " + unmatchedMobile + " components inspected which do ") + "not exist on this machine. ";
            else if (unmatchedMain > 0 && unmatchedMobile == 0)
                errorMessage = "There " + (unmatchedMain == 1 ? "is one component on the machine which does " : "are " + unmatchedMain + " components on the machine which do ") + "not exist in this inspection. ";
            else
                errorMessage = "There " + (unmatchedMain == 1 ? " is one component" : "are " + unmatchedMain + " components on this machine") + " and there " + (unmatchedMobile == 1 ? "is one component in the inspection " : "are " + unmatchedMobile + " components in the inspection ") + " which are not matched! ";

            return new ResultMessage
            {
                Id = 0,
                OperationSucceed = false,
                ActionLog = errorMessage,
                LastMessage = "The components on the inspection do not match this equipment. " + errorMessage + " Syncing result in permanent loss of inspection data for missing components. Do you wish to proceed?"
            };
        }

        /// <summary>
        /// This method match Mobile inspection without having mobile equipmentId
        /// It calls the next method generally
        /// </summary>
        /// <param name="EquipmentId"></param>
        /// <param name="MobileInspectionId"></param>
        /// <returns></returns>
        public ResultMessage MatchMobileInspection(int EquipmentId, int MobileInspectionId)
        {
            var inspection = _context.Mbl_Track_Inspection.Where(m => m.inspection_auto == MobileInspectionId).FirstOrDefault();
            if (inspection == null)
                return MatchMobileInspection(EquipmentId, MobileInspectionId,0);
            return MatchMobileInspection(EquipmentId, MobileInspectionId, inspection.equipmentid_auto.LongNullableToInt());
        }

        /// <summary>
        /// This method set pc_equipmentid_auto with EquipmentId
        /// BUT pc_inspection_auto will be set with MobileInspectionId and not Track_Inspection table
        /// </summary>
        /// <param name="EquipmentId"></param>
        /// <param name="MobileInspectionId"></param>
        /// <param name="MobileEqId"></param>
        /// <returns></returns>
        public ResultMessage MatchMobileInspection(int EquipmentId, int MobileInspectionId, int MobileEqId)
        {
            var result = new ResultMessage { Id = 0, LastMessage = "", OperationSucceed = false };
            var mobileEquipment = _context.Mbl_NewEquipment.Where(m => m.equipmentid_auto == MobileEqId).FirstOrDefault();
            if (mobileEquipment == null)
            {
                result.LastMessage = "Equipment inspected on mobile cannot be found!";
                return result;
            }
            var equipment = _context.EQUIPMENTs.Find(EquipmentId);
            if (equipment == null)
            {
                result.LastMessage = "Equipment cannot be found!";
                return result;
            }
            var alreadyMatched = _context.Mbl_NewEquipment.Where(m => m.pc_equipmentid_auto == EquipmentId);

            foreach (var eq in alreadyMatched) {
                eq.pc_equipmentid_auto = null;
                eq.pc_inspection_auto = null;
            }

            mobileEquipment.pc_equipmentid_auto = EquipmentId;
            mobileEquipment.pc_inspection_auto = MobileInspectionId;

            try
            {
                _context.SaveChanges();
                result.LastMessage = "Operation succeeded!";
                result.OperationSucceed = true;
                return result;
            }
            catch (Exception ex)
            {
                result.LastMessage = ex.Message;
                result.ActionLog = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return result;
            }
        }

        private decimal ConvertFrom(BLL.Core.Domain.MeasurementType from, decimal reading)
        {
            if (from == BLL.Core.Domain.MeasurementType.Milimeter)
                return reading * (decimal)(0.0393701);
            return reading * (decimal)25.4;
        }

        public ResultMessage TransitFromMobileInspection(long EquipmentId, long MblEquipmentId, int InspectionId, Interfaces.IUser user)
        {
            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = "Operation failed in TransitFromMobileInspection",
                LastMessage = "Operation failed!",
                Id = 0
            };

            if (user == null) {
                rm.LastMessage = "Operation failed! User not found!";
                rm.ActionLog = "user is null";
                return rm;
            }
            var equipment = _context.EQUIPMENTs.Find(EquipmentId);
            var equipmentInspection = _context.Mbl_Track_Inspection.Where(m=>m.inspection_auto == InspectionId && m.equipmentid_auto == MblEquipmentId).FirstOrDefault();
            Mbl_NewEquipment mobileEquipment = _context.Mbl_NewEquipment.Where(m => m.equipmentid_auto == MblEquipmentId).FirstOrDefault();

            if (equipment == null || equipmentInspection == null || mobileEquipment == null)
            {
                rm.LastMessage = "Operation failed! equipment or inspection not found!";
                rm.ActionLog = "(equipment == null || equipmentInspection == null || mobileEquipment == null) detected!";
                return rm;
            }
            var mobileEqInspection = _context.Mbl_Track_Inspection.Where(m => m.equipmentid_auto == MblEquipmentId).FirstOrDefault();
            if (mobileEqInspection == null)
            {
                rm.LastMessage = "Operation failed! Inspection from mobile not found!";
                rm.ActionLog = "Mbl_Track_Inspection(this inspection).count() is 0";
                return rm;
            }

            var mobileInspectionDetails = _context.Mbl_Track_Inspection_Detail.Where(m => m.inspection_auto == mobileEqInspection.inspection_auto).ToList();

            var mobileComponents = _context.Mbl_NewGENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == MblEquipmentId).ToList();
            var geuComponents = _context.GENERAL_EQ_UNIT.Where(m => m.equipmentid_auto == EquipmentId).ToList();

            BLL.Core.Domain.InspectionImpact impact = equipmentInspection.impact == 2 ? BLL.Core.Domain.InspectionImpact.High : BLL.Core.Domain.InspectionImpact.Low;

            DAL.TRACK_INSPECTION EquipmentInspectionParam = new TRACK_INSPECTION
            {
                abrasive = equipmentInspection.abrasive,
                allowableWear = equipmentInspection.allowableWear,
                inspection_comments = equipmentInspection.inspection_comments,
                impact = equipmentInspection.impact,
                inspection_date = equipmentInspection.inspection_date,
                Jobsite_Comms = equipmentInspection.Jobsite_Comms,
                quote_auto = equipmentInspection.quote_auto,
                last_interp_date = equipmentInspection.last_interp_date,
                last_interp_user = equipmentInspection.last_interp_user,
                location = equipmentInspection.location,
                ltd = equipmentInspection.ltd,
                confirmed_date = equipmentInspection.confirmed_date,
                confirmed_user = equipmentInspection.confirmed_user,
                created_date = equipmentInspection.created_date,
                created_user = equipmentInspection.created_user,
                docket_no = equipmentInspection.docket_no + DateTime.Now.Millisecond,
                dry_joints_left = equipmentInspection.dry_joints_left,
                dry_joints_right = equipmentInspection.dry_joints_right,
                evalcode = equipmentInspection.evalcode,
                eval_comment = equipmentInspection.eval_comment,
                equipmentid_auto = EquipmentId,
                examiner = equipmentInspection.examiner,
                ext_cannon_left = equipmentInspection.ext_cannon_left,
                ext_cannon_right = equipmentInspection.ext_cannon_right,
                frame_ext_left = equipmentInspection.frame_ext_left,
                frame_ext_right = equipmentInspection.frame_ext_right,
                moisture = equipmentInspection.moisture,
                notes = equipmentInspection.notes,
                packing = equipmentInspection.packing,
                released_by = equipmentInspection.released_by,
                released_date = equipmentInspection.released_date,
                smu = equipmentInspection.smu,
                sprocket_left_status = equipmentInspection.sprocket_left_status,
                sprocket_right_status = equipmentInspection.sprocket_right_status,
                track_sag_left = equipmentInspection.track_sag_left,
                track_sag_left_status = equipmentInspection.track_sag_left_status,
                track_sag_right = equipmentInspection.track_sag_right,
                track_sag_right_status = equipmentInspection.track_sag_right_status,
                ucbrand = equipmentInspection.ucbrand,
                uccode = equipmentInspection.uccode,
                uccodedesc = equipmentInspection.uccodedesc,
                wear = equipmentInspection.wear,
                LeftTrackSagComment = equipmentInspection.LeftTrackSagComment,
                RightTrackSagComment = equipmentInspection.RightTrackSagComment,
                LeftCannonExtensionComment = equipmentInspection.LeftCannonExtensionComment,
                RightCannonExtensionComment = equipmentInspection.RightCannonExtensionComment,
                LeftTrackSagImage = equipmentInspection.LeftTrackSagImage,
                RightTrackSagImage = equipmentInspection.RightTrackSagImage,
                LeftCannonExtensionImage = equipmentInspection.LeftCannonExtensionImage,
                RightCannonExtensionImage = equipmentInspection.RightCannonExtensionImage,
                TravelledKms = equipmentInspection.TravelledKms,
                ForwardTravelHours = equipmentInspection.ForwardTravelHours,
                ReverseTravelHours = equipmentInspection.ReverseTravelHours,
                LeftScallopMeasurement = equipmentInspection.LeftScallopMeasurement,
                RightScallopMeasurement = equipmentInspection.RightScallopMeasurement
            };

            var matchedComponents = getMatchingForInspectionSync(EquipmentId.LongNullableToInt(), MblEquipmentId.LongNullableToInt());
            List<BLL.Core.Domain.InspectionDetailWithSide> tidWithSIdeList = new List<BLL.Core.Domain.InspectionDetailWithSide>();
            foreach (var mtid in mobileInspectionDetails)
            {
                var matched = matchedComponents.Where(m => m.ComponentIndex == mtid.track_unit_auto).FirstOrDefault();

                if (matched == null || matched.MatchedComponentId == 0)
                    continue;

                BLL.Core.Domain.Component LogicalComponent = new Component(_context, matched.MatchedComponentId);
                
                if (LogicalComponent != null && LogicalComponent.Id != 0)
                {
                    decimal worn = LogicalComponent.CalcWornPercentage(ConvertFrom(BLL.Core.Domain.MeasurementType.Milimeter, mtid.reading), mtid.tool_auto ?? 0, impact);
                    char eval = ' ';
                    LogicalComponent.GetEvalCodeByWorn(worn, out eval);
                    List<TRACK_INSPECTION_IMAGES> imageList = new List<TRACK_INSPECTION_IMAGES>();
                    List<COMPART_ATTACH_FILESTREAM> imageStreamList = new List<COMPART_ATTACH_FILESTREAM>();
                    var mobileFileStream = _context.Mbl_CompartAttach_filestream.Where(m => m.inspection_auto == mobileEqInspection.inspection_auto && m.compartid_auto == matched.ComponentIndex).ToList();
                    foreach (var streamImage in mobileFileStream)
                    {
                        TRACK_INSPECTION_IMAGES inspectionImage = new TRACK_INSPECTION_IMAGES
                        {
                            GUID = streamImage.guid,
                            image_comment = streamImage.comment,
                            image_data = streamImage.attachment
                        };
                        imageList.Add(inspectionImage);
                        COMPART_ATTACH_FILESTREAM inspectionStreamImage = new COMPART_ATTACH_FILESTREAM
                        {
                            attachment = streamImage.attachment,
                            attachment_name = streamImage.attachment_name,
                            comment = streamImage.comment,
                            compartid_auto = LogicalComponent.DALComponent.compartid_auto,
                            comparttype_auto = LogicalComponent.DALComponent.LU_COMPART.comparttype_auto,
                            compart_attach_type_auto = streamImage.compart_attach_type_auto,
                            entry_date = streamImage.entry_date,
                            guid = streamImage.guid,
                            inspection_auto = InspectionId,
                            position = LogicalComponent.DALComponent.pos,
                            tool_auto = mtid.tool_auto,
                            user_auto = user.Id
                        };
                        imageStreamList.Add(inspectionStreamImage);
                    }
                    TRACK_INSPECTION_DETAIL tid = new TRACK_INSPECTION_DETAIL
                    {
                        comments = mtid.comments,
                        worn_percentage = worn,
                        eval_code = eval.ToString(),
                        ext_projected_hours = mtid.ext_projected_hours,
                        ext_remaining_hours = mtid.ext_remaining_hours,
                        hours_on_surface = mtid.hours_on_surface,
                        projected_hours = mtid.projected_hours,
                        reading = mtid.reading,
                        remaining_hours = mtid.remaining_hours,
                        tool_auto = mtid.tool_auto,
                        track_unit_auto = LogicalComponent.Id,
                        Images = imageList
                    };
                    BLL.Core.Domain.InspectionDetailWithSide tidWithSide = new BLL.Core.Domain.InspectionDetailWithSide
                    {
                        CompartAttachFileStreamImage = imageStreamList,
                        ComponentInspectionDetail = tid,
                        side = (LogicalComponent.DALComponent.side == null || LogicalComponent.DALComponent.side > 2 || LogicalComponent.DALComponent.side < 0) ? 0 : (int)LogicalComponent.DALComponent.side
                    };
                    tidWithSIdeList.Add(tidWithSide);
                }
            }

            BLL.Core.Domain.InsertInspectionParams Params = new BLL.Core.Domain.InsertInspectionParams
            {
                EquipmentInspection = EquipmentInspectionParam,
                ComponentsInspection = tidWithSIdeList,
                EvaluationOverall = ' '
            };

            BLL.Interfaces.IEquipmentActionRecord EquipmentAction = new BLL.Core.Domain.EquipmentActionRecord
            {
                ActionDate = Params.EquipmentInspection.inspection_date,
                ActionUser = user,
                EquipmentId = Params.EquipmentInspection.equipmentid_auto > int.MaxValue ? int.MaxValue : (int)Params.EquipmentInspection.equipmentid_auto,
                Comment = Params.EquipmentInspection.inspection_comments,
                ReadSmuNumber = Params.EquipmentInspection.smu == null ? 0 : (int)Params.EquipmentInspection.smu,
                TypeOfAction = BLL.Core.Domain.ActionType.InsertInspection,
                Cost = 0
            };

            using (BLL.Core.Domain.Action UCAction = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), EquipmentAction, Params))
            {
                System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();

                UCAction.Operation.Start();
                if (UCAction.Operation.Status == BLL.Core.Domain.ActionStatus.Close)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = UCAction.Operation.ActionLog;
                    rm.LastMessage = UCAction.Operation.Message;
                }
                if (UCAction.Operation.Status == BLL.Core.Domain.ActionStatus.Started)
                    UCAction.Operation.Validate();

                if (UCAction.Operation.Status == BLL.Core.Domain.ActionStatus.Invalid)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = UCAction.Operation.ActionLog;
                    rm.LastMessage = UCAction.Operation.Message;
                }
                if (UCAction.Operation.Status == BLL.Core.Domain.ActionStatus.Valid)
                    UCAction.Operation.Commit();
                if (UCAction.Operation.Status == BLL.Core.Domain.ActionStatus.Failed)
                {
                    rm.OperationSucceed = false;
                    rm.ActionLog = UCAction.Operation.ActionLog;
                    rm.LastMessage = UCAction.Operation.Message;
                }
                if (UCAction.Operation.Status == BLL.Core.Domain.ActionStatus.Succeed)
                {
                    rm.OperationSucceed = true;
                    rm.ActionLog = UCAction.Operation.ActionLog;
                    rm.LastMessage = UCAction.Operation.Message;
                }
                rm.Id = UCAction.Operation.UniqueId;
                if (rm.OperationSucceed)
                {
                    // TT-49
                    if(mobileEquipment.EquipmentPhoto != null)
                    {
                        _context.EQUIPMENTs.Find(equipment.equipmentid_auto).EquipmentPhoto = mobileEquipment.EquipmentPhoto;
                    }

                    _context.Mbl_Track_Inspection_Detail.RemoveRange(mobileInspectionDetails);
                    _context.Mbl_Track_Inspection.Remove(mobileEqInspection);
                    _context.Mbl_NewGENERAL_EQ_UNIT.RemoveRange(mobileComponents);
                    _context.Mbl_NewEquipment.Remove(mobileEquipment);
                    var imagesTobeDelete = _context.Mbl_CompartAttach_filestream.Where(m => m.inspection_auto == mobileEqInspection.inspection_auto);
                    _context.Mbl_CompartAttach_filestream.RemoveRange(imagesTobeDelete);
                    
                    try
                    {
                        _context.SaveChanges();
                        rm.OperationSucceed = true;
                        rm.LastMessage = "Operation was successfull!";
                    }
                    catch (Exception m)
                    {
                        string Message = m.Message;
                        rm.OperationSucceed = false;
                        rm.LastMessage = "Operation failed! please check log for more details!";
                        rm.ActionLog = Message + ((m.InnerException != null) ? m.InnerException.Message : "");
                    }
                }
            }
            try
            {
                BLL.Core.Domain.Equipment LogicalEquipment = new BLL.Core.Domain.Equipment(new UndercarriageContext(), (int)EquipmentId);
                if (LogicalEquipment.Id == 0 || LogicalEquipment.GetEquipmentFamily() != BLL.Core.Domain.EquipmentFamily.MEX_Mining_Shovel)
                    return rm;
                LogicalEquipment.UpdateMiningShovelInspectionParentsFromChildren(rm.Id);
                return rm;
            } catch(Exception ex)
            {
                string message = ex.Message;
                return rm;
            }
            
        }// End of transit from mobile
        public int getEquipmentUnsyncedId(int EquipmentId) {
            var mblequipment = _context.Mbl_NewEquipment.Where(m => m.pc_equipmentid_auto == EquipmentId).FirstOrDefault();
            if (mblequipment == null || mblequipment.equipmentid_auto == 0)
                return 0;
            var inspections = _context.Mbl_Track_Inspection.Where(m => m.equipmentid_auto == mblequipment.equipmentid_auto);
            if (inspections.Count() > 1)
                return 0;
            return inspections.FirstOrDefault().inspection_auto;
        }

        public ResultMessage saveTrackSagCannonExt(Side side, int TracksagORCannon, int imgORComment, string Value)
        {
            ResultMessage result = new ResultMessage
            {
                Id = 0,
                OperationSucceed = false,
            };
            if (getDALInspection() == null)
            {
                result.LastMessage = "Inspection cannot be found!";
                result.ActionLog = "Inspection cannot be found!";
                return result;
            }

            if (side == Side.Left)
            {
                if (TracksagORCannon == 1) //TrackSag
                {
                    if (imgORComment == 1)
                        DALInspection.LeftTrackSagImage = Convert.FromBase64String(Value);
                    else if (imgORComment == 2)
                        DALInspection.LeftTrackSagComment = Value;
                }
                else if (TracksagORCannon == 2) //CannonExt
                {
                    if (imgORComment == 1)
                        DALInspection.LeftCannonExtensionImage = Convert.FromBase64String(Value);
                    else if (imgORComment == 2)
                        DALInspection.LeftCannonExtensionComment = Value;
                }
            }
            else if (side == Side.Right)
            {
                if (TracksagORCannon == 1) //TrackSag
                {
                    if (imgORComment == 1)
                        DALInspection.RightTrackSagImage = Convert.FromBase64String(Value);
                    else if (imgORComment == 2)
                        DALInspection.RightTrackSagComment = Value;
                }
                else if (TracksagORCannon == 2) //CannonExt
                {
                    if (imgORComment == 1)
                        DALInspection.RightCannonExtensionImage = Convert.FromBase64String(Value);
                    else if (imgORComment == 2)
                        DALInspection.RightCannonExtensionComment = Value;
                }
            }
            _context.Entry(DALInspection).State = EntityState.Modified;
            try
            {
                _context.SaveChanges();
                result.OperationSucceed = true;
                result.Id = DALInspection.inspection_auto;
                result.LastMessage = "Operation Succeeded!";
                return result;
            }
            catch (Exception ex) {
                result.LastMessage = "Operation failed! please check log!";
                result.ActionLog = ex.Message + (ex.InnerException == null ? "" : ex.InnerException.Message);
                return result;
            }
        }

        // TT-516 Edit Photo Popup (to be updated)
        public string GetPhoto()
        {
            return Convert.ToBase64String(_context.TRACK_INSPECTION_IMAGES
                        .Select(t => t.image_data)
                        .FirstOrDefault());
        }
    }
}
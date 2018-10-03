using BLL.Administration;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using BLL.Extensions;

namespace BLL.Core.Domain
{
    public class SetupEquipment
    {
        UndercarriageContext _context;
        public SetupEquipment(UndercarriageContext context)
        {
            this._context = context;
        }

        public async Task<object> GetCustomerList(long userId)
        {            
           return await new UserAccess(new SharedContext(), (int)userId).getAccessibleCustomersExtended().Select(c => new { Id = c.customer_auto, Name = c.cust_name }).OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<object> GetJobsiteList(long customerId, System.Security.Principal.IPrincipal User)
        {
            return await new UserAccess(new SharedContext(), User).getAccessibleJobsites().Where(j => j.customer_auto == customerId).Select(j => new { Id = j.crsf_auto, Name = j.site_name }).OrderBy(j => j.Name).ToListAsync();
        }

        public async Task<object> GetEquipmentList(long jobsiteId, System.Security.Principal.IPrincipal User)
        {
            return await new UserAccess(new SharedContext(), User).getAccessibleEquipments().Where(e => e.crsf_auto == jobsiteId).Select(e => new { Id = e.equipmentid_auto, SerialNumber = e.serialno, UnitNumber = e.unitno}).OrderBy(e => e.SerialNumber).ToListAsync();
        }

        public async Task<object> GetMakeList()
        {
            return await _context.MAKE.Where(m => m.Undercarriage == true).Select(m => new { Id = m.make_auto, Name = m.makedesc }).OrderBy(m => m.Name).ToListAsync();
        }

        public async Task<object> GetOEMMakeList()
        {
            return await _context.MAKE.Where(m => (m.Undercarriage ?? false) && m.OEM).Select(m => new { Id = m.make_auto, Name = m.makedesc }).OrderBy(m => m.Name).ToListAsync();
        }

        public async Task<object> GetModelList(long makeId)
        {
            return await _context.LU_MMTA.Where(m => m.make_auto == makeId).GroupBy(m => m.MODEL.modeldesc).Select(m => m.FirstOrDefault()).Select(m => new { Id = m.model_auto, Name = m.MODEL.modeldesc }).OrderBy(m => m.Name).ToListAsync();
        }

        public async Task<string> GetFamilyNameForModel(long modelId)
        {
            return await _context.LU_MMTA.Where(m => m.model_auto == modelId).Select(m => m.TYPE.typedesc).FirstOrDefaultAsync();
        }

        public async Task<object> GetEquipmentRankingListAsync()
        {
            return await _context.LU_EQUIPMENT_RANKING.OrderBy(r => r.rorder).Select(r => new { Id = r.ranking_auto, Name = r.ranking }).ToListAsync();
        }

        public async Task<object> GetEquipmentApplicationListAsync()
        {
            return await _context.APPLICATIONs.Select(a => new { Id = a.app_auto, Name = a.appdesc }).OrderBy(a => a.Name).ToListAsync();
        }

        /// <summary>
        /// Returns true if the given equipment serial and unit number are valid and unique. 
        /// </summary>
        /// <param name="serialNumber">Serial number of the new equipment </param>
        /// <param name="unitNumber">Unit number of the new equipment</param>
        /// <returns>True if given serial and unit numbers are valid and unique. </returns>
        public async Task<bool> ValidateSerialAndUnitNumberAsync(string serialNumber, string unitNumber, long? equipmentId)
        {
            int count = 0;
            if(equipmentId != null)
                count = await _context.EQUIPMENTs.Where(e => e.serialno.ToLower() == serialNumber.ToLower()).Where(e => e.unitno.ToLower() == unitNumber.ToLower()).Where(e => e.equipmentid_auto != equipmentId).CountAsync();
            else
                count = await _context.EQUIPMENTs.Where(e => e.serialno.ToLower() == serialNumber.ToLower()).Where(e => e.unitno.ToLower() == unitNumber.ToLower()).CountAsync();
            if (count > 0)
                return false;
            return true;
        }

        public async Task<SetupEquipmentViewModel> GetExistingEquipmentDetails(long equipmentId)
        {
            var e = await _context.EQUIPMENTs.FindAsync(equipmentId);
            if (e == null)
                return null;
            int inspectionCount = await _context.TRACK_INSPECTION.Where(i => i.equipmentid_auto == equipmentId).CountAsync();
            int getActionCount = await _context.GET_EVENTS_EQUIPMENT.Where(g => g.equipment_auto == equipmentId).CountAsync();//await _context.GET_EVENTS_EQUIPMENT.Where(g => g.equipment_auto == equipmentId).Where(g => g.Event.action_auto != 2).CountAsync();
            SetupEquipmentViewModel equipment = new SetupEquipmentViewModel()
            {
                ApplicationId = e.LU_MMTA.app_auto,
                CreatedByUserId = -1,
                CustomerId = e.Jobsite.customer_auto,
                Date = e.purchase_date == null ? DateTime.Now : (DateTime)e.purchase_date,
                HoursOfUsePerDay = e.op_hrs_per_day == null ? 0 : (int)e.op_hrs_per_day,
                Id = e.equipmentid_auto,
                InspectEvery = e.InspectEvery,
                InspectEveryUnitTypeId = e.InspectEveryUnitTypeId,
                JobsiteId = e.crsf_auto,
                Ltd = e.LTD_at_start == null ? 0 : (int)e.LTD_at_start,
                MakeId = e.LU_MMTA.make_auto,
                ModelId = e.LU_MMTA.model_auto,
                Photo = e.EquipmentPhoto != null ? Convert.ToBase64String(e.EquipmentPhoto) : "",
                RankingId = e.ranking_auto,
                SerialNumber = e.serialno,
                Smu = e.smu_at_start == null ? 0 : (int)e.smu_at_start,
                StatusId = e.status_auto == null ? -1 : (int)e.status_auto,
                UnitNumber = e.unitno,
                UsedMonday = e.UsedMonday,
                UsedTuesday = e.UsedTuesday,
                UsedWednesday = e.UsedWednesday,
                UsedThursday = e.UsedThursday,
                UsedFriday = e.UsedFriday,
                UsedSaturday = e.UsedSaturday,
                UsedSunday = e.UsedSunday,
                CanChangeSmuLtd = true, //(inspectionCount + getActionCount) == 0 ? true : false
                EnableAutoInspectionPlanner = e.EnableAutoInspectionPlanner,
                NextInspectionDate = e.NextInspectionDate.Date,
            };

            return equipment;
        }

        public bool VerifyUserAccessToEquipment(long userId, long equipmentId)
        {
            return new UserAccess(new SharedContext(), (int)userId).hasAccessToEquipment(equipmentId);
        }

        public async Task<Tuple<long, string>> UpdateEquipmentInspectionPlanner(SetupEquipmentViewModel equipment)
        {
            var _equipment = _context.EQUIPMENTs.Find(equipment.Id);
            if(_equipment == null)
                return Tuple.Create((long)-1, "Failed to complete the action: Equipment cannot be found!");
            _equipment.op_hrs_per_day = equipment.HoursOfUsePerDay;
            _equipment.UsedMonday = equipment.UsedMonday;
            _equipment.UsedTuesday = equipment.UsedTuesday;
            _equipment.UsedWednesday = equipment.UsedWednesday;
            _equipment.UsedThursday = equipment.UsedThursday;
            _equipment.UsedFriday = equipment.UsedFriday;
            _equipment.UsedSaturday = equipment.UsedSaturday;
            _equipment.UsedSunday = equipment.UsedSunday;
            _equipment.InspectEvery = equipment.InspectEvery;
            _equipment.InspectEveryUnitTypeId = equipment.InspectEveryUnitTypeId;
            _equipment.EnableAutoInspectionPlanner = equipment.EnableAutoInspectionPlanner;
            _context.Entry(_equipment).State = EntityState.Modified;
            try
            {
                if (await _context.SaveChangesAsync() > 0)
                {
                    var _logicalEquipment = new Equipment(new UndercarriageContext());
                    var _nextInspectionDate = _logicalEquipment.ForcastNextInspectionDate(DateTime.Now, (int)equipment.Id);
                    var _nextInspectionSMU = _logicalEquipment.ForcastNextInspectionSMU(DateTime.Now, (int)equipment.Id);
                    if (_nextInspectionDate > DateTime.MinValue) _equipment.NextInspectionDate = _nextInspectionDate.Date;
                    if (_nextInspectionSMU > 0 ) _equipment.NextInspectionSMU = _nextInspectionSMU;
                    if (_nextInspectionDate > DateTime.MinValue && _nextInspectionSMU > 0)
                    {
                        _equipment.NextInspectionDate = _nextInspectionDate.Date;
                        _context.Entry(_equipment).State = EntityState.Modified;
                        try
                        {
                            await _context.SaveChangesAsync();
                        }
                        catch
                        {
                            //This can be seen as a warning and not an error
                        }
                    }
                }
            }catch(Exception ex)
            {
                return Tuple.Create((long)-1, "Failed to complete the action: "+ex.ToDetailedString());
            }
            return Tuple.Create(equipment.Id, "Operation completed successfully. ");
        }

        public async Task<Tuple<long, string>> UpdateEquipment(SetupEquipmentViewModel e)
        {
            var ActionParam = new BLL.Core.Domain.EquipmentActionRecord
            {
                ActionUser = new BLL.Core.Domain.User { Id =(int)(e.CreatedByUserId??0)},
                EquipmentId = (int)e.Id,
                ReadSmuNumber = e.Smu,
                EquipmentActualLife = e.Ltd,
                TypeOfAction = BLL.Core.Domain.ActionType.UpdateSetupEquipment,
                Cost = 0.0M,
                Comment = "Update Equipment Setup",
                ActionDate = e.Date.ToLocalTime().Date
            };

            var EquipmentSetupParams = new BLL.Core.Domain.GETEquipmentSetupParams
            {
                UserAuto = (int)(e.CreatedByUserId ?? 0),
                ActionType = BLL.Core.Domain.GETActionType.UpdateSetupEquipment,
                RecordedDate = DateTime.Now,
                EventDate = e.Date.ToLocalTime().Date,
                Comment = "Update Equipment Setup",
                Cost = 0.0M,
                MeterReading = e.Smu,
                EquipmentLTD = e.Ltd,
                EquipmentId = e.Id,
                IsUpdating = true,
            };

            var rm = new BLL.Core.Domain.ResultMessage
            {
                OperationSucceed = false,
                ActionLog = " ",
                LastMessage = " ",
                Id = 0
            };

            using(var action = new BLL.Core.Domain.Action(new DAL.UndercarriageContext(), new DAL.GETContext(), ActionParam, EquipmentSetupParams))
            {
                if (action.Operation.Start() == ActionStatus.Started && action.Operation.Validate() == ActionStatus.Valid && action.Operation.Commit() == ActionStatus.Succeed)
                    rm.OperationSucceed = true;
                else
                    rm.OperationSucceed = false;

                rm.LastMessage = action.Operation.Message;
                rm.ActionLog = action.Operation.ActionLog;
                rm.Id = action.Operation.UniqueId;
            }
            if(!rm.OperationSucceed && rm.Id != -2)
                return Tuple.Create((long)-1, "Failed to complete the action: "+ rm.LastMessage);
            var mmta = await _context.LU_MMTA.Where(m => m.model_auto == e.ModelId && m.make_auto == e.MakeId).FirstOrDefaultAsync();
            var updater = await _context.USER_TABLE.FindAsync(e.CreatedByUserId);
            string[] LogoArr = e.Photo.Split(',');
            string equipmentPhoto = "";
            if (LogoArr.Length > 1)
                equipmentPhoto = LogoArr[1];
            var eq = await _context.EQUIPMENTs.FindAsync(e.Id);

            // Need to check if inspections exist
            eq.smu_at_start = e.Smu;
            eq.currentsmu = e.Smu;
            eq.last_reading_date = e.Date.ToLocalTime().Date;
            eq.LTD_at_start = e.Ltd;
            eq.purchase_op_hrs = e.Smu;
            eq.purchase_date = e.Date.ToLocalTime().Date;
            eq.modified_date = DateTime.Now;
            eq.modified_user = updater.username;
            eq.equip_status = e.StatusId;

            eq.crsf_auto = e.JobsiteId;
            eq.serialno = e.SerialNumber;
            eq.unitno = e.UnitNumber;
            eq.mmtaid_auto = mmta.mmtaid_auto;
            eq.op_hrs_per_day = e.HoursOfUsePerDay;
            eq.ranking_auto = Convert.ToByte((e.RankingId == null || e.RankingId < 0) ? 0 : e.RankingId);
            eq.UsedMonday = e.UsedMonday;
            eq.UsedTuesday = e.UsedTuesday;
            eq.UsedWednesday = e.UsedWednesday;
            eq.UsedThursday = e.UsedThursday;
            eq.UsedFriday = e.UsedFriday;
            eq.UsedSaturday = e.UsedSaturday;
            eq.UsedSunday = e.UsedSunday;
            if(e.Photo.Length > 0)
                eq.EquipmentPhoto = Convert.FromBase64String(equipmentPhoto);
            eq.InspectEvery = e.InspectEvery;
            eq.InspectEveryUnitTypeId = e.InspectEveryUnitTypeId;

            var mmtaEntity = await _context.LU_MMTA.FindAsync(eq.mmtaid_auto);
            mmtaEntity.make_auto = e.MakeId;
            mmtaEntity.model_auto = e.ModelId;
            mmtaEntity.type_auto = mmta.type_auto;
            mmtaEntity.app_auto = e.ApplicationId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Tuple.Create((long)-1, "Failed to update the equipment. " + ex.Message + ". " + ex.InnerException != null ? ex.InnerException.Message : "");
            }

            //var getSetupEvent = new GETCore.Classes.GETEquipment();
            //getSetupEvent.equipmentSetupEvent(eq.equipmentid_auto, e.Smu, e.Ltd, e.CreatedByUserId != null ? (long)e.CreatedByUserId : -1);
            if (e.UnsyncedId > 0)
                new BLL.Core.Domain.Inspection(new DAL.UndercarriageContext()).MatchMobileInspection((int)e.Id, e.UnsyncedId);
            if(!rm.OperationSucceed)
            return Tuple.Create(eq.equipmentid_auto, "Your equipment has been updated with warnings. This equipment was setup in the old application and some manual midifications may be required!");
            
                return Tuple.Create(eq.equipmentid_auto, "Your equipment has been updated successfully. ");

        }

        public async Task<Tuple<long, string>> CreateNewEquipment(SetupEquipmentViewModel e)
        {
            var mmta = await _context.LU_MMTA.Where(m => m.model_auto == e.ModelId && m.make_auto == e.MakeId).FirstOrDefaultAsync();
            var creator = await _context.USER_TABLE.FindAsync(e.CreatedByUserId);
            string[] LogoArr = e.Photo.Split(',');
            string equipmentPhoto = "";
            if (LogoArr.Length > 1)
                equipmentPhoto = LogoArr[1].Trim();

            var mmtaEntity = new DAL.LU_MMTA()
            {
                make_auto = e.MakeId,
                model_auto = e.ModelId,
                type_auto = mmta.type_auto,
                app_auto = e.ApplicationId,
                service_cycle_type_auto = 1,
                expiry_date = DateTime.MaxValue,
                created_date = DateTime.Now,
                created_user = creator.username
            };
            _context.LU_MMTA.Add(mmtaEntity);
            try {
                await _context.SaveChangesAsync();
            } catch(Exception ex)
            {
                return Tuple.Create((long)-1, "Failed to create the MMTA record. " + ex.Message + ". " + ex.InnerException != null ? ex.InnerException.Message : "");
            }

            var equipmentEntity = new DAL.EQUIPMENT()
            {
                serialno = e.SerialNumber,
                unitno = e.UnitNumber,
                mmtaid_auto = mmtaEntity.mmtaid_auto,
                measure_unit = 1,
                op_hrs_per_day = e.HoursOfUsePerDay,
                op_dist_uom = 0,
                op_range = "DEFAULT",
                smu_at_start = e.Smu,
                distance_at_start = 0,
                smu_at_end = 100000,
                distance_at_end = 100000,
                currentsmu = e.Smu,
                currentdistance = 0,
                last_reading_date = e.Date.ToLocalTime().Date,
                notes = "",
                LTD_at_start = e.Ltd,
                crsf_auto = e.JobsiteId,
                purchase_op_hrs = e.Smu,
                purchase_op_dist = 0,
                purchase_date = e.Date.ToLocalTime().Date,
                deprate = 0,
                created_date = DateTime.Now,
                created_user = creator.username,
                modified_date = DateTime.Now,
                modified_user = creator.username,
                equip_status = e.StatusId,
                update_accept = true,
                da_inclusion = true, 
                dtd_at_start = 0,
                ranking_auto = Convert.ToByte((e.RankingId == null || e.RankingId < 0) ? 0 : e.RankingId),
                secondary_uom_isHours = false,
                secondary_uom_isDistance = false,
                secondary_uom_isKWHours = false,
                secondary_uom_isCalendar = false,
                secondary_uom_isFuelBurn = false,
                health_review_auto = 0,
                vision_link_exist = false,
                UsedMonday = e.UsedMonday,
                UsedTuesday = e.UsedTuesday,
                UsedWednesday = e.UsedWednesday,
                UsedThursday = e.UsedThursday,
                UsedFriday = e.UsedFriday,
                UsedSaturday = e.UsedSaturday,
                UsedSunday = e.UsedSunday,
                EquipmentPhoto = equipmentPhoto.Length > 0 ? Convert.FromBase64String(equipmentPhoto) : null,
                InspectEvery = e.InspectEvery,
                InspectEveryUnitTypeId = e.InspectEveryUnitTypeId
            };

            _context.EQUIPMENTs.Add(equipmentEntity);
            try
            {
                await _context.SaveChangesAsync();
            } catch (Exception ex)
            {
                return Tuple.Create((long)-1, "Failed to create the new equipment. " + ex.Message + ". " + ex.InnerException != null ? ex.InnerException.Message : "");
            }

            var moduleRegEntity = new DAL.MODULE_REGISTRATION_EQUIPMENT()
            {
                equipmentid_auto = equipmentEntity.equipmentid_auto,
                pm_servicing = true,
                pm_servicing_last_reg_date = DateTime.Now,
                backlog = true,
                backlog_last_reg_date = DateTime.Now,
                scheduler = true,
                scheduler_last_reg_date = DateTime.Now,
                trakalerts = true,
                trakalerts_last_reg_date = DateTime.Now,
                component_manager = true,
                component_manager_last_reg_date = DateTime.Now,
                tyre = true,
                tyre_last_reg_date = DateTime.Now,
                general_inspection = true,
                general_inspection_last_reg_date = DateTime.Now,
                get = true,
                get_last_reg_date = DateTime.Now,
                undercarriage = true,
                undercarriage_last_reg_date = DateTime.Now,
                body_bowl = true,
                body_bowl_last_reg_date = DateTime.Now,
                modified_user = creator.username,
                rail = true,
                rail_last_reg_date = DateTime.Now,
                dashboard = true,
                dashboard_last_reg_date = DateTime.Now,
            };

            _context.MODULE_REGISTRATION_EQUIPMENT.Add(moduleRegEntity);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return Tuple.Create((long)-1, "Failed to create the equipment secondary records. " + ex.Message + ". " + ex.InnerException != null ? ex.InnerException.Message : "");
            }
            var getSetupEvent = new GETCore.Classes.GETEquipment();
            getSetupEvent.equipmentSetupEvent(equipmentEntity.equipmentid_auto, e.Smu, e.Ltd, e.CreatedByUserId != null ? (long)e.CreatedByUserId : -1);

            if (e.UnsyncedId > 0)
            new BLL.Core.Domain.Inspection(new DAL.UndercarriageContext()).MatchMobileInspection((int)equipmentEntity.equipmentid_auto, e.UnsyncedId);

            return Tuple.Create(equipmentEntity.equipmentid_auto, "Your equipment has been added successfully. ");
        }
    }
}

public class SetupEquipmentViewModel
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public long JobsiteId { get; set; }
    public string SerialNumber { get; set; }
    public string UnitNumber { get; set; }
    public int MakeId { get; set; }
    public int ModelId { get; set; }
    public int Smu { get; set; }
    public int Ltd { get; set; }
    public DateTime Date { get; set; }
    public string Photo { get; set; }
    public int HoursOfUsePerDay { get; set; }
    public bool UsedMonday { get; set; }
    public bool UsedTuesday { get; set; }
    public bool UsedWednesday { get; set; }
    public bool UsedThursday { get; set; }
    public bool UsedFriday { get; set; }
    public bool UsedSaturday { get; set; }
    public bool UsedSunday { get; set; }
    public int InspectEvery { get; set; }
    public int InspectEveryUnitTypeId { get; set; }
    public short? ApplicationId { get; set; }
    public int? RankingId { get; set; }
    public int StatusId { get; set; }
    public long? CreatedByUserId { get; set; }
    public bool? CanChangeSmuLtd { get; set; }
    public int UnsyncedId { get; set; } = 0;
    public bool EnableAutoInspectionPlanner { get; set; } = false;
    public DateTime NextInspectionDate { get; set; }
}
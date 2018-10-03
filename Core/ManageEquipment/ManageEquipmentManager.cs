using BLL.Core.Domain;
using BLL.Core.ManageEquipment.ViewModels;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.ManageEquipment
{
    /// <summary>
    /// This is used to retrieve and manipulate data on the manage equipment page in the undercarriage ui. 
    /// </summary>
    public class ManageEquipmentManager
    {
        private UndercarriageContext _context;

        /// <summary>
        /// This is used to retrieve and manipulate data on the manage equipment page in the undercarriage ui. 
        /// </summary>
        /// <param name="context">New instance of DAL.UndercarriageContext</param>
        public ManageEquipmentManager(UndercarriageContext context)
        {
            this._context = context;
        }

        public async Task<Tuple<bool, string>> ChangeEquipmentJobsite(long equipmentId, long jobsiteId)
        {
            var equipment = await this._context.EQUIPMENTs.FindAsync(equipmentId);
            if (equipment == null)
                return Tuple.Create(false, "Failed to find equipment. ");

            equipment.crsf_auto = jobsiteId;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update jobsite. " + e.Message + e.InnerException != null ? e.InnerException.Message : "");
            }
            return Tuple.Create(true, equipment.Jobsite.site_name);
        }



        public async Task<Tuple<bool, string>> UpdateEquipmentSMU(long equipmentId, long currentReading, long userId, DateTime dateReadSMU)
        {
            var equipment = await this._context.EQUIPMENTs.FindAsync(equipmentId);
            if (equipment == null)
                return Tuple.Create(false, "Failed to find equipment. ");
            var userIntId = Convert.ToInt32(userId);
            var equipmentIntId = Convert.ToInt32(equipmentId);
            var currentReadingInt = Convert.ToInt32(currentReading);



            var operationResult = false;
            var message = "";
            var ucUser = new Domain.TTDevUser(new SharedContext(), userIntId).getUCUser();
            using (var action = new Domain.Action
                        (new UndercarriageContext(),
                        new Domain.EquipmentActionRecord
                        {
                            ActionDate = dateReadSMU,
                            ActionUser = ucUser,
                            EquipmentId = equipmentIntId,
                            ReadSmuNumber = currentReadingInt,
                            TypeOfAction = Domain.ActionType.SMUReadingAction,
                        }))
            {
                if (action.Operation.Start() == Domain.ActionStatus.Started)
                    if (action.Operation.Validate() == Domain.ActionStatus.Valid)
                        if (action.Operation.Commit() == Domain.ActionStatus.Succeed)
                            operationResult = true;
                message = action.Operation.Message;
            }
            return Tuple.Create(operationResult, message);
        }




        public async Task<Tuple<bool, string>> ChangeMeterUnit(long equipmentId,long oldSmuReading ,long smuOnNewMeter, long userId, DateTime dateReplaced)
        {
            var equipment = await this._context.EQUIPMENTs.FindAsync(equipmentId);
            if (equipment == null)
                return Tuple.Create(false, "Failed to find equipment. ");


            var operationResult = false;
            var message = "";
            var ucUser = new Domain.TTDevUser(new SharedContext(), (int)userId).getUCUser();
            using (var action = new Domain.Action
                        (new UndercarriageContext(),
                        new Domain.EquipmentActionRecord
                        {
                            ActionDate = dateReplaced,
                            ActionUser = ucUser,
                            EquipmentId = (int) equipmentId,
                            ReadSmuNumber = (int) oldSmuReading,
                            TypeOfAction = Domain.ActionType.ChangeMeterUnit,
                        }, new Domain.ChangeMeterUnitParams {
                            Id= (int) equipmentId,
                            SMUnew = (int)smuOnNewMeter
                        }))
            {
                if (action.Operation.Start() == Domain.ActionStatus.Started)
                    if (action.Operation.Validate() == Domain.ActionStatus.Valid)
                        if (action.Operation.Commit() == Domain.ActionStatus.Succeed)
                            operationResult = true;
                message = action.Operation.Message;
            }
            return Tuple.Create(operationResult, message);
        }



            public async Task<List<ManageEquipmentInspectionViewModel>> GetInspections(long equipmentId)
        {
            var equipment = await this._context.EQUIPMENTs.FindAsync(equipmentId);
            if (equipment == null)
                return null;


            return equipment.TRACK_INSPECTION.Where(m => m.ActionTakenHistory?.recordStatus == 0).OrderByDescending(i => i.inspection_date).Select(i => new ManageEquipmentInspectionViewModel()
            {
                Date = i.inspection_date.ToString("dd/MMM/yyyy"),
                Eval = i.evalcode,
                Id = i.inspection_auto,
                Interpreted = i.quote_auto == null ? false : true,
                QuoteId = i.quote_auto == null && !i.quote_auto.HasValue ? 0 : i.quote_auto.Value,
                IsReportSaved = IsMiningShovleReportSaved(i.inspection_auto)
            }).ToList();


       

        }



    

        /// <summary>
        /// Returns a list of the systems on the given equipment Id, each of these systems also contains a
        /// list of the components on that system. This data is displayed on the manage equipment page. 
        /// </summary>
        /// <param name="equipmentId">The equipment Id to get the systems for</param>
        public async Task<List<ManageEquipmentSystemViewModel>> GetEquipmentSystems(long equipmentId)
        {
            var equipment = await this._context.EQUIPMENTs.FindAsync(equipmentId);
            if (equipment == null)
                return null;

            List<ManageEquipmentSystemViewModel> equipmentSystemsReturnData = new List<ManageEquipmentSystemViewModel>();

            // Get list of the systems currently on this equipment, loop through them all and create the return objects. 
            var systems = equipment.UCSystems.ToList();
            systems.ForEach(s =>
            {
                var system = new BLL.Core.Domain.UCSystem(_context, Convert.ToInt32(s.Module_sub_auto), true);
               
                // Get list of all components on the system, create a list to add to this systems return object. 
                //var components = system.Components.OrderBy(c => c.LU_COMPART.LU_COMPART_TYPE.sorder).ToList();

                // Currently just filter the compart without a child 
                var components = system.Components.Where(c => c.LU_COMPART.CHILD_RELATION_LIST.Count() == 0).OrderBy(c => c.LU_COMPART.LU_COMPART_TYPE.sorder).ToList();

                List<ManageEquipmentComponentViewModel> componentsOnSystem = new List<ManageEquipmentComponentViewModel>();
                components.ForEach(c =>
                {
                    var component = new BLL.Core.Domain.Component(_context, Convert.ToInt32(c.equnit_auto));
                    componentsOnSystem.Add(new ManageEquipmentComponentViewModel()
                    {
                        Brand = c.Make != null ? c.Make.makedesc : "Unknown",
                        Cmu = component.GetComponentLife(DateTime.Now),
                        EquipmentSmuAtInstall = component.getEquipmentSmuAtSetup(),
                        Id = c.equnit_auto,
                        InstallDate = ((DateTime)c.date_installed).ToString("dd/MMM/yyyy"),
                        Name = component.GetComponentDescription(),
                        PercentWorn = Math.Round(component.GetComponentWorn(DateTime.Now)),
                        PurchaseCost = c.cost == null ? 0 : (decimal)c.cost,
                        RepairsCost = component.GetComponentActionsCost(),
                        RemainingLife100 = Math.Round(component.GetComponentRemainingLife100()),
                        RemainingLife120 = Math.Round(component.GetComponentRemainingLife120()),
                        Photo = component.GetComponentPhoto() == null ? "" : Convert.ToBase64String(component.GetComponentPhoto()),
                        CompartTypeId = c.LU_COMPART.comparttype_auto,
                        Position = c.pos ?? 0
                    });
                });

                var installDate = s.modifiedDate != null ? (DateTime)s.modifiedDate : (DateTime)system.Components.FirstOrDefault().date_installed;
                equipmentSystemsReturnData.Add(new ManageEquipmentSystemViewModel()
                {
                    DateInstalled = installDate.ToString("dd/MMM/yyyy"),
                    EquipmentSmuAtInstall = s.equipment_LTD_at_attachment != null ? (int)s.equipment_LTD_at_attachment : 0,
                    Id = s.Module_sub_auto,
                    LifeLived = system.GetSystemLife(DateTime.Now),
                    SerialNumber = s.Serialno,
                    Side = system.side == Domain.Side.Left ? "Left" : "Right",
                    SystemType = system.SystemType == Domain.UCSystemType.Chain ? "Chain" : "Frame",
                    TotalComponentPurchaseCost = system.GetTotalSetupCostOfComponentsOnSystem(),
                    TotalComponentRepairsCost = system.GetTotalCostOfAllComponentActions(),
                    Components = componentsOnSystem
                });
            });

            return equipmentSystemsReturnData.OrderBy(d => d.Side).ThenBy(d => d.SystemType).ToList();
        }

        /// <summary>
        /// Gets the equipment details information for the given equipment ID. This is displayed at the top of the page. 
        /// </summary>
        /// <returns>General details about the given equipment Id. </returns>
        public async Task<ManageEquipmentDetailsViewModel> GetEquipmentDetails(long equipmentId)
        {
            var equipment = await this._context.EQUIPMENTs.FindAsync(equipmentId);
            var equipment2 = new BLL.Core.Domain.Equipment(_context, (int)equipmentId);
            var lastInspection = equipment.TRACK_INSPECTION.LastOrDefault();
            string lastInspectionDate = lastInspection == null ? "Never" : lastInspection.inspection_date.ToString("dd/MMM/yyyy");
            var nextInpectionForecast = CalculateNextInspectionDate(lastInspection, equipment);
            var equipmentImage = Convert.ToBase64String(equipment2.GetEquipmentImage());
            return new ManageEquipmentDetailsViewModel()
            {
                CustomerName = equipment.Jobsite.Customer.cust_name,
                JobsiteName = equipment.Jobsite.site_name,
                Family = equipment.LU_MMTA.TYPE.typedesc,
                LastInspectionDate = lastInspectionDate,
                Ltd = equipment2.GetEquipmentLife(DateTime.Now),
                Make = equipment.LU_MMTA.MAKE.makedesc,
                Model = equipment.LU_MMTA.MODEL.modeldesc,
                NextInspectionDate = equipment.NextInspectionDate.ToString("dd/MMM/yyyy"),
                PercentWorn = Math.Round(equipment2.getEquipmentComponentsWorn(DateTime.Now).Select(c => c.wornPercentage).OrderByDescending(c => c).FirstOrDefault()),
                SerialNumber = equipment.serialno,
                Smu = equipment.currentsmu != null ? (int)equipment.currentsmu : 0,
                UnitNumber = equipment.unitno,
                EquipmentPhoto = equipmentImage,
                CustomerId = equipment.Jobsite.customer_auto,
                JobsiteId = equipment.crsf_auto,
                InspectEvery = equipment.InspectEvery,
                InspectEveryUnitTypeId = equipment.InspectEveryUnitTypeId,
            };
        }

        private string CalculateNextInspectionDate(TRACK_INSPECTION lastInspection, EQUIPMENT equipment)
        {
            if (lastInspection == null)
            {
                return equipment.NextInspectionDate.ToShortDateString();
            }
            else
            {                
                var inspectEvery = equipment.InspectEvery;
                var typeEvery = equipment.InspectEveryUnitTypeId;
                if (typeEvery == (int)InspectEveryUnitType.Hours)
                {
                    var added = inspectEvery / equipment.op_hrs_per_day;
                    if (added.HasValue)
                       return lastInspection.inspection_date.AddDays(added.Value).ToShortDateString();
                    else
                        return lastInspection.inspection_date.AddHours(inspectEvery).ToShortDateString();
                }
                else if(typeEvery == (int)InspectEveryUnitType.Days)
                {
                    return lastInspection.inspection_date.AddDays(inspectEvery).ToShortDateString();
                }
                else{
                    return equipment.NextInspectionDate.ToShortDateString();
                }
            }
        }



        public bool IsMiningShovleReportSaved(int inspectionId)
        {
            var report = _context.MININGSHOVEL_REPORT.FirstOrDefault(m => m.InspectionId == inspectionId);
            return report != null ? true : false;
        }


        public async Task<Tuple<int, string>> UpdateEquipmentInspectionForecastingInfo(UpdateInspectionForecastingInfoViewModel model)
        {
            var equipment = await _context.EQUIPMENTs.FindAsync(model.EquipmentId);
            if (equipment == null) return Tuple.Create(-1, "There is no equipment " + model.EquipmentId + "in the database");
            equipment.InspectEvery = model.InspectEvery;
            equipment.InspectEveryUnitTypeId = model.InspectEveryUnitType;
            try
            {
               await  _context.SaveChangesAsync();
            }
            catch (Exception ex) {
                return Tuple.Create(-1, "Failed to update Equipment inspection forcasting information.");
            }
            return Tuple.Create(1, "Equipment inspection forecasting information has been sucessfully updated");
        }

        /// <summary>
        /// Checks if the user has access to the given equipment Id
        /// </summary>
        /// /// <param name="equipmentId">The user requesting access to the equipment. </param>
        /// <param name="equipmentId">The equipment Id to check the users access for. </param>
        /// <returns>True if the user has access to the given equipment</returns>
        public async Task<bool> VerifyUserAccessToEquipment(long userId, long equipmentId)
        {
            return new Domain.UserAccess(new SharedContext(), (int)userId).hasAccessToEquipment(equipmentId);
            var equipment = await _context.EQUIPMENTs.FindAsync(equipmentId);
            if (equipment == null)
                return false;
            var customerId = equipment.Jobsite.customer_auto;
            return GETCore.Classes.AuthorizeUserAccess.verifyAccessToCustomer(userId, customerId, false);
        }
    }


    public enum InspectEveryUnitType
    {
        Hours = 0,
        Days = 1
    }
}
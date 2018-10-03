using System.Collections.Generic;
using System.Linq;
using BLL.GETCore.Classes.ViewModel;
using DAL;
using System;

namespace BLL.GETCore.Classes
{
    public class InventoryManagement
    {
        public List<GETImplementInventoryVM> returnImplementInventory(long customer, long jobsite, int make, long type,
            string equipmentSerial, string equipmentUnit, string implementSerial, int status, string userId)
        {
            List<GETImplementInventoryVM> result = new List<GETImplementInventoryVM>();

            // Parse the user id.
            long lUserId = long.TryParse(userId, out lUserId) ? lUserId : 0;

            // Set the initial condition and LTD for an implement.
            int INITIAL_CONDITION = 0; // 0% worn
            int INITIAL_LTD = 0;

            // Filter by customers that the user has permissions to view.
            List<CustomerIdAndNameDataSet> customers = new List<CustomerIdAndNameDataSet>();
            CustomerManagement customerManagement = new CustomerManagement();
            customers = customerManagement.getListOfCustomersForLoggedInUser(lUserId);

            // Filter by customer dropdown list.
            long[] customerIds = new long[customers.Count];
            for (int i = 0; i < customers.Count; i++)
            {
                if((customer == customers[i].customerId) || (customer == 0))
                {
                    customerIds[i] = customers[i].customerId;
                }
            }

            // Parse the status bit positions.
            int status_OnEquipment = (status & 0x01);
            int status_AwaitingRepairs = (status & 0x02) >> 1;
            int status_UndergoingRepairs = (status & 0x04) >> 2;
            int status_ReadyForUse = (status & 0x08) >> 3;
            int status_Scrapped = (status & 0x10) >> 4;

            // These statuses map to the values in GET_INVENTORY_STATUS table.
            // Needs to be made dynamic eventually.
            int STATUS_ON_EQUIPMENT = (int)GETInterfaces.Enum.InventoryStatus.On_Equipment;
            int STATUS_AWAITING_REPAIR = (int)GETInterfaces.Enum.InventoryStatus.Awaiting_Repair;
            int STATUS_UNDERGOING_REPAIR = (int)GETInterfaces.Enum.InventoryStatus.Undergoing_Repair;
            int STATUS_READY_FOR_USE = (int)GETInterfaces.Enum.InventoryStatus.Ready_for_Use;
            int STATUS_SCRAPPED = (int)GETInterfaces.Enum.InventoryStatus.Scrapped;

            using (var dataEntities = new DAL.GETContext())
            {
                // Find the implements that are already installed on equipment.
                var statusDescription_OnEquipment = dataEntities.GET_INVENTORY_STATUS.Find(STATUS_ON_EQUIPMENT).status_desc;
                var inventoryOnEquipment = (from g in dataEntities.GET
                                            join l in dataEntities.LU_IMPLEMENT
                                                on g.implement_auto equals l.implement_auto
                                            join e in dataEntities.EQUIPMENTs
                                                on g.equipmentid_auto equals e.equipmentid_auto
                                            join j in dataEntities.CRSF
                                                on e.crsf_auto equals j.crsf_auto
                                            join c in dataEntities.CUSTOMERs
                                                on j.customer_auto equals c.customer_auto
                                            join m in dataEntities.MAKE
                                                on g.make_auto equals m.make_auto
                                            where customerIds.Contains(c.customer_auto)
                                                && ((j.crsf_auto == jobsite) || (jobsite == 0))
                                                && ((m.make_auto == make) || (make == 0))
                                                && ((l.implement_auto == type) || (type == 0))
                                                && ((g.impserial.ToString().Contains(implementSerial)) || (implementSerial == null) || (implementSerial == ""))
                                                && ((e.serialno.ToString().Contains(equipmentSerial)) || (equipmentSerial == null) || (equipmentSerial == ""))
                                                && ((e.unitno.ToString().Contains(equipmentUnit)) || (equipmentUnit == null) || (equipmentUnit == ""))
                                                && ((g.on_equipment == true) && (status_OnEquipment == 1))
                                            select new
                                            {
                                                g.get_auto,
                                                condition = INITIAL_CONDITION,
                                                c.cust_name,
                                                j.site_name,
                                                m.makedesc,
                                                l.implementdescription,
                                                g.impserial,
                                                ltd = INITIAL_LTD,
                                                status_desc = statusDescription_OnEquipment,
                                                e.serialno,
                                                e.unitno
                                            });

                // Find implements that are in inventory (status != onEquipment).
                var inventoryList = (from gi in dataEntities.GET_INVENTORY
                                     join gis in dataEntities.GET_INVENTORY_STATUS
                                        on gi.status_auto equals gis.status_auto
                                     join gw in dataEntities.GET_WORKSHOP
                                        on gi.workshop_auto equals gw.workshop_auto into gw_gi
                                     from gw2 in gw_gi.DefaultIfEmpty()
                                     join gr in dataEntities.GET_REPAIRER
                                        on gw2.repairer_auto equals gr.repairer_auto into gr_gw2
                                     from gr2 in gr_gw2.DefaultIfEmpty()
                                     join g in dataEntities.GET
                                        on gi.get_auto equals g.get_auto
                                     join l in dataEntities.LU_IMPLEMENT
                                        on g.implement_auto equals l.implement_auto
                                     join j in dataEntities.CRSF
                                        on gi.jobsite_auto equals j.crsf_auto
                                     join c in dataEntities.CUSTOMERs
                                        on j.customer_auto equals c.customer_auto
                                     join m in dataEntities.MAKE
                                        on g.make_auto equals m.make_auto
                                     where customerIds.Contains(c.customer_auto)
                                        && ((j.crsf_auto == jobsite) || (jobsite == 0))
                                        && ((m.make_auto == make) || (make == 0))
                                        && ((l.implement_auto == type) || (type == 0))
                                        && ((equipmentSerial == null) || (equipmentSerial.Trim() == ""))
                                        && ((equipmentUnit == null) || (equipmentUnit.Trim() == ""))
                                        && ((g.impserial.ToString().Contains(implementSerial)) || (implementSerial == null) || (implementSerial == ""))
                                        && ( 
                                            ((gi.status_auto == STATUS_AWAITING_REPAIR) && (g.on_equipment == false) && (status_AwaitingRepairs == 1))
                                            || ((gi.status_auto == STATUS_UNDERGOING_REPAIR) && (g.on_equipment == false) && (status_UndergoingRepairs == 1))
                                            || ((gi.status_auto == STATUS_READY_FOR_USE) && (g.on_equipment == false) && (status_ReadyForUse == 1))
                                            || ((gi.status_auto == STATUS_SCRAPPED) && (g.on_equipment == false) && (status_Scrapped == 1))
                                        )
                                     select new
                                     {
                                         g.get_auto,
                                         condition = INITIAL_CONDITION,
                                         c.cust_name,
                                         j.site_name,
                                         m.makedesc,
                                         l.implementdescription,
                                         g.impserial,
                                         ltd = INITIAL_LTD,
                                         gis.status_desc,
                                         serialno = "",
                                         unitno = "",
                                         workshopName = (gw2 != null ? gw2.name : ""),
                                         repairerName = (gr2 != null ? gr2.name : "")
                                     });
                
                result = inventoryOnEquipment.Select(
                    i => new GETImplementInventoryVM
                    {
                        get_auto = i.get_auto,
                        condition = i.condition,
                        customer = i.cust_name,
                        jobsite = i.site_name,
                        make = i.makedesc,
                        type = i.implementdescription,
                        serial_no = i.impserial,
                        ltd = i.ltd,
                        status = i.status_desc,
                        equipment_serialno = (i.serialno != null ? i.serialno : ""),
                        equipment_unitno = (i.unitno != null ? i.unitno : ""),
                        repairer = "",
                        workshop = ""
                    }).ToList();

                result.AddRange(
                    inventoryList.Select(
                        i => new GETImplementInventoryVM
                        {
                            get_auto = i.get_auto,
                            condition = i.condition,
                            customer = i.cust_name,
                            jobsite = i.site_name,
                            make = i.makedesc,
                            type = i.implementdescription,
                            serial_no = i.impserial,
                            ltd = i.ltd,
                            status = i.status_desc,
                            equipment_serialno = (i.serialno != null ? i.serialno : ""),
                            equipment_unitno = (i.unitno != null ? i.unitno : ""),
                            repairer = i.repairerName,
                            workshop = i.workshopName
                        }).ToList()
                );

                
                foreach (var item in result)
                {
                    // Find the LTD for each implement.
                    var eventRecord = dataEntities.GET_EVENTS_IMPLEMENT
                        .Where(s => s.get_auto == item.get_auto)
                        .OrderByDescending(m => m.implement_events_auto)
                        .FirstOrDefault();
                    if(eventRecord != null)
                    {
                        item.ltd = eventRecord.ltd;
                    }

                    // Find the remaining life for each implement.
                    var inspectionRecord = dataEntities.GET_IMPLEMENT_INSPECTION
                        .Where(g => g.get_auto == item.get_auto)
                        .OrderByDescending(h => h.inspection_auto)
                        .FirstOrDefault();
                    if(inspectionRecord != null)
                    {
                        item.condition = inspectionRecord.eval;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns status for implements in inventory. Excludes status 'On Equipment'.
        /// </summary>
        /// <returns></returns>
        public List<GenericIdNameVM> ReturnInventoryStatusList()
        {
            List<GenericIdNameVM> result = new List<GenericIdNameVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                result = dataEntities.GET_INVENTORY_STATUS
                            .Where(s => s.status_auto != (int)GETInterfaces.Enum.InventoryStatus.On_Equipment)
                            .Select(i => new GenericIdNameVM
                            {
                                Id = i.status_auto,
                                Name = i.status_desc
                            }).ToList();
            }

            return result;
        }


        /// <summary>
        /// Returns the list of available repairers for a customer. Uses the jobsiteId 
        /// of the implement in inventory to determine which customer to filter by.
        /// </summary>
        /// <param name="jobsiteId"></param>
        /// <returns></returns>
        public List<GenericIdNameVM> ReturnRepairerListForCustomer(long jobsiteId)
        {
            List<GenericIdNameVM> result = new List<GenericIdNameVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var crsf = dataEntities.CRSF.Find(jobsiteId);
                if(crsf != null)
                {
                    result = dataEntities.GET_REPAIRER
                        .Where(r => r.customer_auto == crsf.customer_auto)
                        .Select(s => new GenericIdNameVM
                        {
                            Id = s.repairer_auto,
                            Name = s.name
                        }).ToList();
                }
                
            }

            return result;
        }

        /// <summary>
        /// Returns the list of available workshops for a given repairer.
        /// </summary>
        /// <param name="repairerId"></param>
        /// <returns></returns>
        public List<GenericIdNameVM> ReturnWorkshopListForRepairer(int repairerId)
        {
            List<GenericIdNameVM> result = new List<GenericIdNameVM>();

            using (var dataEntities = new DAL.GETContext())
            {
                var repairer = dataEntities.GET_REPAIRER.Find(repairerId);
                if(repairer != null)
                {
                    result = dataEntities.GET_WORKSHOP
                        .Where(w => w.repairer_auto == repairer.repairer_auto)
                        .Select(s => new GenericIdNameVM
                        {
                            Id = s.workshop_auto,
                            Name = s.name
                        }).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Takes in the status id and returns a boolean which indicates whether 
        /// the repairer needs to be assigned for the status.
        /// </summary>
        /// <param name="status"></param>
        /// <returns></returns>
        public bool CheckIfRepairerRequired(int status)
        {
            bool result = false;

            if((status == (int)GETInterfaces.Enum.InventoryStatus.Awaiting_Repair)
                || (status == (int)GETInterfaces.Enum.InventoryStatus.Undergoing_Repair))
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Creates a new repairer for the customer at the specified jobsite.
        /// </summary>
        /// <param name="jobsiteId"></param>
        /// <param name="repairerName"></param>
        /// <returns></returns>
        public int CreateRepairer(long jobsiteId, string repairerName)
        {
            int result = 0;

            using (var dataEntities = new DAL.GETContext())
            {
                // Check that the customer exists.
                var jobsite = dataEntities.CRSF.Find(jobsiteId);
                if(jobsite != null)
                {
                    var customer = dataEntities.CUSTOMERs.Find(jobsite.customer_auto);
                    if (customer != null)
                    {
                        // Check that the repairer doesn't already exist.
                        var repairer = dataEntities.GET_REPAIRER
                            .Where(r => r.name == repairerName)
                            .FirstOrDefault();

                        if (repairer == null)
                        {
                            // Add new repairer.
                            DAL.GET_REPAIRER newRepairer = new DAL.GET_REPAIRER
                            {
                                name = repairerName,
                                customer_auto = customer.customer_auto
                            };
                            dataEntities.GET_REPAIRER.Add(newRepairer);

                            int _changesSaved = dataEntities.SaveChanges();
                            if (_changesSaved > 0)
                            {
                                result = newRepairer.repairer_auto;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a new workshop for the specified repairer.
        /// </summary>
        /// <param name="repairerId"></param>
        /// <param name="workshopName"></param>
        /// <returns></returns>
        public int CreateWorkshop(int repairerId, string workshopName)
        {
            int result = 0;

            using (var dataEntities = new DAL.GETContext())
            {
                // Check that the repairer exists.
                var repairer = dataEntities.GET_REPAIRER.Find(repairerId);
                if (repairer != null)
                {
                    // Check that the workshop doesn't already exist.
                    var workshop = dataEntities.GET_WORKSHOP
                        .Where(w => w.name == workshopName)
                        .FirstOrDefault();

                    if (workshop == null)
                    {
                        // Add new workshop.
                        DAL.GET_WORKSHOP newWorkshop = new DAL.GET_WORKSHOP
                        {
                            name = workshopName,
                            repairer_auto = repairerId
                        };
                        dataEntities.GET_WORKSHOP.Add(newWorkshop);

                        int _changesSaved = dataEntities.SaveChanges();
                        if (_changesSaved > 0)
                        {
                            result = newWorkshop.workshop_auto;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Create a new inventory record for an implement.
        /// </summary>
        /// <param name="details"></param>
        /// <param name="jobsiteId"></param>
        /// <param name="authUserId"></param>
        /// <param name="_context"></param>
        /// <returns>The ID of the created inventory record.</returns>
        public int InsertIntoInventory(ImplementDetails details, int jobsiteId, long authUserId, GETContext _context)
        {
            int result = -1;

            try
            {
                GET_INVENTORY newGETinInventory = new GET_INVENTORY();
                newGETinInventory.get_auto = details.Id;
                newGETinInventory.jobsite_auto = jobsiteId;
                newGETinInventory.status_auto = (int)GETInterfaces.Enum.InventoryStatus.Ready_for_Use;
                newGETinInventory.modified_date = DateTime.Now;
                newGETinInventory.modified_user = (int)authUserId;
                newGETinInventory.ltd = (int)details.ImplementHoursAtSetup;
                newGETinInventory.workshop_auto = null;

                _context.GET_INVENTORY.Add(newGETinInventory);
                int _changesSaved = _context.SaveChanges();

                if (_changesSaved > 0)
                {
                    result = newGETinInventory.inventory_auto;
                }
            }
            catch (Exception ex1)
            {

            }

            return result;
        }

        /// <summary>
        /// Update an existing Inventory record for an implement.
        /// </summary>
        /// <param name="details"></param>
        /// <param name="jobsiteId"></param>
        /// <param name="authUserId"></param>
        /// <param name="_context"></param>
        /// <returns>The ID of the updated inventory record.</returns>
        public int UpdateInventoryRecord(ImplementDetails details, int jobsiteId, long authUserId, GETContext _context)
        {
            int result = -1;

            try
            {
                GET_INVENTORY inventoryRecord = _context.GET_INVENTORY.Where(w => w.get_auto == details.Id).FirstOrDefault();
                if(inventoryRecord != null)
                {
                    inventoryRecord.modified_date = DateTime.Now;
                    inventoryRecord.modified_user = (int)authUserId;
                    inventoryRecord.ltd = (int)details.ImplementHoursAtSetup;

                    int _changesSaved = _context.SaveChanges();
                    if (_changesSaved > 0)
                    {
                        result = inventoryRecord.inventory_auto;
                    }
                }
            }
            catch (Exception ex1)
            {

            }

            return result;
        }

        /// <summary>
        /// Update the GET_INVENTORY record for a specified implement.
        /// </summary>
        /// <param name="details"></param>
        /// <param name="jobsiteId"></param>
        /// <param name="authUserId"></param>
        /// <param name="_context"></param>
        /// <returns>The ID of the GET_INVENTORY record that was inserted or updated.</returns>
        public int UpdateImplementInInventory(ImplementDetails details, int jobsiteId, long authUserId, GETContext _context)
        {
            int result = 0;

            // Check if the record already exists in inventory.
            var inventoryRecordExists = _context.GET_INVENTORY.Where(w => w.get_auto == details.Id).Any();
            if (!inventoryRecordExists)
            {
                // Add a new inventory record for this implement.
                result = InsertIntoInventory(details, jobsiteId, authUserId, _context);
            }
            else
            {
                // Update the existing inventory record for this implement.
                result = UpdateInventoryRecord(details, jobsiteId, authUserId, _context);
            }

            return result;
        }
    }
}
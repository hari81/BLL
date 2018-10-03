using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.GETInterfaces
{
    public class Enum
    {
        /// <summary>
        /// Implement category, it's value corresponds to its category ID in the database. 
        /// </summary>
        public enum ImplementCategory
        {
            GET = 1,
            DumpBody = 2
        }

        /// <summary>
        /// Component category, it's value corresponds to its category ID in the database. 
        /// </summary>
        public enum ComponentTypeCategory
        {
            GET = 8,
            DumpBody = 11
        }

        /// <summary>
        /// Defines whether a template is available to be used globally for all customers in the system,
        /// or if it is just for a specific customer
        /// </summary>
        public enum TemplateAccess
        {
            Global,
            Customer
        }

        /// <summary>
        /// Defines the status of GET implements in inventory. Special case for 'On_Equipment' where 
        /// an inventory record should not exist. Should be mapped to the GET_INVENTORY_STATUS table.
        /// </summary>
        public enum InventoryStatus
        {
            On_Equipment = 1,
            Awaiting_Repair = 2,
            Undergoing_Repair = 3,
            Ready_for_Use = 4,
            Scrapped = 5
        }

        public enum MobileAppInspectionPhotoType
        {
            Component = 1,
            Component_Observation = 2,
            Observation_Point = 3,
            OP_Observation = 4
        }
    }
}
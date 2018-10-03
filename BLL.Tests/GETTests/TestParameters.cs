using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Tests.GETTests
{
    public class TestParameters
    {
        // Testing parameters.
        public long testUserID { get; set; }
        public int equipmentAuto { get; set; }
        public int GETAuto1 { get; set; }
        public int GETAuto2 { get; set; }
        public int jobsiteId { get; set; }
        public int SMU { get; set; }
        public decimal cost { get; set; }
        public string comment { get; set; }
        public DateTime eventDate { get; set; }
        public int repairerId { get; set; }
        public int workshopId { get; set; }
        public int statusId { get; set; }

        public DateTime pastDate { get; set; }
        public DateTime futureDate { get; set; }

        public TestParameters()
        {
            testUserID = 1;
            equipmentAuto = 1406;
            GETAuto1 = 38;
            GETAuto2 = 39;
            jobsiteId = 282;
            SMU = 3;
            cost = 100;
            comment = "Unit testing";
            eventDate = DateTime.Now;
            repairerId = 2;
            workshopId = 3;
            statusId = (int)GETInterfaces.Enum.InventoryStatus.Ready_for_Use;

            pastDate = new DateTime(2008, 01, 01);
            futureDate = new DateTime(2028, 01, 01);
        }
    }
}

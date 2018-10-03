using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BLL.GETCore.Classes;

namespace BLL.Tests.GETTests
{
    [TestClass]
    public class MoveImplementToInventoryTest
    {
        GETImplement getImplement = new GETImplement();
        TestParameters tP = new TestParameters();

        /// <summary>
        /// SMU is negative.
        /// </summary>
        [TestMethod]
        public void Test_MoveImplement_InvalidSMU_1()
        {
            int iSMU = -1;
            bool expected = false;
            bool actual = getImplement.MoveImplementToInventory(tP.GETAuto1, tP.equipmentAuto, tP.jobsiteId,
                iSMU, tP.cost, tP.eventDate, tP.comment, tP.statusId, tP.repairerId, tP.workshopId, tP.testUserID);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// SMU is lower than current.
        /// </summary>
        [TestMethod]
        public void Test_MoveImplement_InvalidSMU_2()
        {
            int iSMU = 100;
            bool expected = false;
            bool actual = getImplement.MoveImplementToInventory(tP.GETAuto1, tP.equipmentAuto, tP.jobsiteId,
                iSMU, tP.cost, tP.eventDate, tP.comment, tP.statusId, tP.repairerId, tP.workshopId, tP.testUserID);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Same or higher SMU than current is fine.
        /// </summary>
        [TestMethod]
        public void Test_MoveImplement_ValidSMU_1()
        {
            int iSMU = 200001;
            bool expected = true;
            bool actual = getImplement.MoveImplementToInventory(tP.GETAuto1, tP.equipmentAuto, tP.jobsiteId,
                iSMU, tP.cost, tP.eventDate, tP.comment, tP.statusId, tP.repairerId, tP.workshopId, tP.testUserID);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Event Date is earlier than implement creation date.
        /// </summary>
        [TestMethod]
        public void Test_MoveImplement_InvalidDate_1()
        {
            DateTime dt = tP.pastDate;
            bool expected = false;
            bool actual = getImplement.MoveImplementToInventory(tP.GETAuto1, tP.equipmentAuto, tP.jobsiteId,
                tP.SMU, tP.cost, dt, tP.comment, tP.statusId, tP.repairerId, tP.workshopId, tP.testUserID);

            Assert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Event Date is too far in the future.
        /// </summary>
        [TestMethod]
        public void Test_MoveImplement_InvalidDate_2()
        {
            DateTime dt = tP.futureDate;
            bool expected = false;
            bool actual = getImplement.MoveImplementToInventory(tP.GETAuto1, tP.equipmentAuto, tP.jobsiteId,
                tP.SMU, tP.cost, dt, tP.comment, tP.statusId, tP.repairerId, tP.workshopId, tP.testUserID);

            Assert.AreEqual(expected, actual);
        }
    }
}

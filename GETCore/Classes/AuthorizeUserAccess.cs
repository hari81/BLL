using BLL.Extensions;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Classes
{
    public class UserTeam
    {
        public UserAccountType teamType { get; set; }
        public int teamId { get; set; }
    }

    public class AuthorizeUserAccess
    {
        public static bool isUserGlobalAdministrator(long userId)
        {
            return new Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).checkUserAccess(Core.Domain.AccessCategory.DealerGroup, "Administrator");
        }

        // Check whether the user is an administrator, whether they be a global admin, 
        // a dealership admin or a customer admin.
        public static bool isUserAdministrator(long userId)
        {
            return new Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).hasRole("Administrator");
        }

        public static bool verifyAccessToChangeUserAccount(long userIdRequesting, long userAccountToChangeId)
        {
            var _userAccess = new Core.Domain.UserAccess(new SharedContext(), userIdRequesting.LongNullableToInt());
            var users = _userAccess.getAccessibleUsers().Where(m=> m.user_auto == userAccountToChangeId).Count();
            return _userAccess.hasRole("Administrator") && users > 0;
        }

        public static UserTeam getUserTeam(long userId)
        {
            var _userAccess = new Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt());
            var supportTeams = _userAccess.getAccessibleSupportTeams();
            if (supportTeams.Count() > 0)
                return new UserTeam
                {
                    teamId = supportTeams.FirstOrDefault().Id,
                    teamType = UserAccountType.SupportTeam_Id_4
                };
            var dealerGroups = _userAccess.getAccessibleDealerGroups();
            if (dealerGroups.Count() > 0)
                return new UserTeam
                {
                    teamId = dealerGroups.FirstOrDefault().Id,
                    teamType = UserAccountType.DealerGroup_Id_3
                };

            var dealers = _userAccess.getAccessibleDealers();
            if(dealers.Count() > 0)
            return new UserTeam {
                teamId = dealers.FirstOrDefault().DealershipId,
                teamType = UserAccountType.Dealership
            };
            var customers = _userAccess.getAccessibleCustomers();
            if (customers.Count() > 0)
                return new UserTeam
                {
                    teamId = customers.FirstOrDefault().DealershipId,
                    teamType = UserAccountType.Customer
                };
            var jobsites = _userAccess.getAccessibleJobsites();
            if (jobsites.Count() > 0)
                return new UserTeam
                {
                    teamId = jobsites.FirstOrDefault().crsf_auto.LongNullableToInt(),
                    teamType = UserAccountType.Jobsite_Id_5
                };

            var equipments = _userAccess.getAccessibleEquipments();
            if (equipments.Count() > 0)
                return new UserTeam
                {
                    teamId = equipments.FirstOrDefault().equipmentid_auto.LongNullableToInt(),
                    teamType = UserAccountType.Equipment_Id_6
                };

            return new UserTeam { teamId = 0, teamType = UserAccountType.Unknown };
        }
        
        public static bool verifyAccessToJobsite(long userId, long jobsiteId, bool adminAccessRequired)
        {
            if(adminAccessRequired)
            return new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).checkUserAccess(Core.Domain.AccessCategory.Jobsite, jobsiteId.LongNullableToInt(), "Administrator");
            return new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleJobsites().Where(m => m.crsf_auto == jobsiteId).Count() > 0;
        }

        /// <summary>
        /// Verifies the given user ID's access to the given customer ID. 
        /// If adminAccess = true, return true if the user has admin access to that customer.
        /// If admin access = false, return true if user account has admin OR normal access to customer.
        /// </summary>
        public static bool verifyAccessToCustomer(long userId, long customerId, bool adminAccessRequired)
        {
            if (adminAccessRequired)
                return new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).checkUserAccess(Core.Domain.AccessCategory.Customer, customerId.LongNullableToInt(), "Administrator");
            return new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleCustomers().Where(m => m.customer_auto == customerId).Count() > 0;
        }

        /// <summary>
        /// Verifies the given user ID's access to the given Dealership ID. 
        /// If adminAccess = true, return true if the user has admin access to that Dealership.
        /// If admin access = false, return true if user account has admin OR normal access to Dealership.
        /// </summary>
        public static bool verifyAccessToDealership(long userId, int dealershipId, bool adminAccessRequired)
        {
            if (adminAccessRequired)
                return new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).checkUserAccess(Core.Domain.AccessCategory.Dealer, dealershipId, "Administrator");
            return new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleDealers().Where(m => m.DealershipId == dealershipId).Count() > 0;
        }

        /// <summary>
        /// Verifies the given user ID's access to review GET Inspection Data
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static bool verifyAccessToGETInspectionData(long userId, int inspectionID)
        {
            bool userHasAccess = false;

            using (var context = new DAL.GETContext())
            {
                try
                {
                    var jobsiteId = context.GET_IMPLEMENT_INSPECTION.Find(inspectionID).GET.EQUIPMENT.crsf_auto;
                    userHasAccess = verifyAccessToJobsite(userId, jobsiteId, false);
                }
                catch (Exception ex1)
                {
                    userHasAccess = false;
                }
            }

            return userHasAccess;
        }
    }
}
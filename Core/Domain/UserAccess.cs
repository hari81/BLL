using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using System.Security.Claims;
using BLL.Extensions;
using BLL.Interfaces;
using BLL.Core.ViewModel;
using System.Data.Entity.Validation;

namespace BLL.Core.Domain
{
    public class UserAccess : SharedDomain
    {
        protected string AspNetId;
        protected int UserId;
        protected USER_TABLE UserTable;
        protected bool Initialized = false;
        public UserAccess(SharedContext context, System.Security.Principal.IPrincipal LoggedInUser) : base(context)
        {
            Initialized = Init(LoggedInUser);
        }

        /// <summary>
        /// This constructor does not initialize user! 
        /// </summary>
        /// <param name="context"></param>
        public UserAccess(SharedContext context) : base(context)
        {

        }

        public UserAccess(SharedContext context, int _UserId) : base(context)
        {
            Initialized = Init(_UserId);
        }

        protected bool Init(int CurrentUser)
        {
            if (CurrentUser == 0)
                return false;
            var user = _domainContext.USER_TABLE.Find(CurrentUser);
            if (user == null)
                return false;
            UserTable = user;
            UserId = CurrentUser;
            return true;
        }

        protected bool Init(System.Security.Principal.IPrincipal CurrentUser)
        {
            if (!CurrentUser.Identity.IsAuthenticated)
                return false;
            var identity = (ClaimsIdentity)CurrentUser.Identity;

            IEnumerable<Claim> claims = identity.Claims.Where(m => m.Type == "sub");
            IEnumerable<Claim> nameIdentifiers = identity.Claims.Where(m => m.Type == ClaimTypes.NameIdentifier);
            if (claims.Count() == 0 && nameIdentifiers.Count() == 0)
                return false;

            if (claims.Count() > 0)
                AspNetId = claims.First().Value;
            else AspNetId = nameIdentifiers.First().Value;

            var users = _domainContext.USER_TABLE.Where(m => m.AspNetUserId == AspNetId);
            if (users.Count() == 0)
                return false;
            UserTable = users.First();
            UserId = users.First().user_auto.LongNullableToInt();
            return true;
        }

        /// <summary>
        /// needs to be initialized! Example: new UserAcces(new DAL.SharedContext(), →→ User ←←)
        /// </summary>
        /// <param name="SupportTeamId"></param>
        /// <returns></returns>
        public bool hasAccessToSupportTeam(int SupportTeamId)
        {
            if (!Initialized) return false;
            return _domainContext.USER_SUPPORTTEAM_RELATION.Where(m => m.UserId == UserId && m.SupportTeamId == SupportTeamId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public bool hasAccessToAnySupportTeam()
        {
            if (!Initialized) return false;
            return _domainContext.USER_SUPPORTTEAM_RELATION.Where(m => m.UserId == UserId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public bool hasAccessToAnySupportTeam(long userId)
        {
            if (!Initialized) return false;
            return _domainContext.USER_SUPPORTTEAM_RELATION.Where(m => m.UserId == userId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;

        }


        public IQueryable<SUPPORT_TEAM> getAccessibleSupportTeams()
        {
            if (!Initialized) return new List<SUPPORT_TEAM>().AsQueryable();
            return _domainContext.USER_SUPPORTTEAM_RELATION.Where(m => m.UserId == UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.SupportTeam);
        }

        public bool hasAccessToDealerGroup(int DealerGroupId)
        {
            if (!Initialized) return false;
            if (hasAccessToAnySupportTeam()) return true;
            return _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == UserId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public IQueryable<DEALER_GROUP> getAccessibleDealerGroups()
        {
            if (!Initialized) return new List<DEALER_GROUP>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.DEALER_GROUP;

            return _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.DealerGroup);
        }

        public IQueryable<DEALER_GROUP> getAccessibleDealerGroups(int _UserId)
        {
            if (!Initialized) return new List<DEALER_GROUP>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.DEALER_GROUP;

            return _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == _UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.DealerGroup);
        }
        /// <summary>
        /// Returns true if the user is a user of this dealrship or, a dealergroup which this dealer is part of that
        /// </summary>
        /// <param name="DealerId"></param>
        /// <returns></returns>
        public bool hasAccessToDealer(int DealerId)
        {
            if (!Initialized) return false;
            if (hasAccessToAnySupportTeam()) return true;
            if (_domainContext.USER_DEALER_RELATION.Where(m => m.UserId == UserId && m.RecordStatus == (int)RecordStatus.Available && m.DealerId == DealerId).Count() > 0)
                return true;
            return _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == UserId && m.RecordStatus == (int)RecordStatus.Available && m.DealerGroup.Dealers.Any(k => k.DealerId == DealerId)).Count() > 0;
        }

        public IQueryable<DAL.Dealership> getAccessibleDealers()
        {
            if (!Initialized) return new List<DAL.Dealership>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.Dealerships;

            var groups = getAccessibleDealerGroups().Select(m => m.Id);
            var groupDealers = _domainContext.DEALERGROUP_DEALER_RELATION.Where(m => groups.Any(k => m.DealerGroupId == k) && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.DealerId);
            var dealers = _domainContext.USER_DEALER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.Dealer.DealershipId);
            var all = groupDealers.Union(dealers);
            var result = _domainContext.Dealerships.Where(m => all.Any(k => k == m.DealershipId));
            return result;
        }

        public bool hasAccessToCustomer(int CustomerId)
        {
            if (!Initialized) return false;
            if (hasAccessToAnySupportTeam()) return true;
            return getAccessibleCustomers().Where(m => m.customer_auto == CustomerId).Count() > 0;
        }

        public IQueryable<CUSTOMER> getAccessibleCustomers()
        {
            if (!Initialized) return new List<CUSTOMER>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.CUSTOMER;
            var groups = getAccessibleDealerGroups().Select(m => m.Id);
            var groupCustomers = _domainContext.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && groups.Any(k => k == m.DealerGroupId)).Select(m => m.CustomerId);

            var dealers = getAccessibleDealers().Select(m => m.DealershipId);
            var dealerCustomers = _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && dealers.Any(k => k == m.DealerId)).Select(m => m.CustomerId);

            var userCustomers = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.CustomerId);

            var all = userCustomers.Union(dealerCustomers.Union(groupCustomers));

            return _domainContext.CUSTOMER.Where(m =>
            all.Any(k => k == m.customer_auto)
            );
        }
        /// <summary>
        /// This method returns customers that user has access to one of their equipments even if user doesn't have access to the customer 
        /// </summary>
        /// <returns></returns>
        public IQueryable<CUSTOMER> getAccessibleCustomersExtended()
        {
            if (!Initialized) return new List<CUSTOMER>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.CUSTOMER;
            var groups = getAccessibleDealerGroups().Select(m => m.Id);
            var groupCustomers = _domainContext.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && groups.Any(k => k == m.DealerGroupId)).Select(m => m.CustomerId);

            var dealers = getAccessibleDealers().Select(m => m.DealershipId);
            var dealerCustomers = _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && dealers.Any(k => k == m.DealerId)).Select(m => m.CustomerId);

            var jobsiteCustomers = getAccessibleJobsites().Select(m => m.customer_auto);
            

            var userCustomers = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.CustomerId);

            var all = jobsiteCustomers.Union(userCustomers.Union(dealerCustomers.Union(groupCustomers)));

            return _domainContext.CUSTOMER.Where(m =>
            all.Any(k => k == m.customer_auto)
            );
        }

        public bool hasAccessToJobsite(int JobsiteId)
        {
            if (!Initialized) return false;
            if (hasAccessToAnySupportTeam()) return true;
            return getAccessibleJobsites().Where(m => m.crsf_auto == JobsiteId).Count() > 0;
        }

        public IQueryable<CRSF> getAccessibleJobsites()
        {
            if (!Initialized) return new List<CRSF>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.CRSF;

            var dealers = getAccessibleDealers().Select(m => m.DealershipId);
            var dealerJobsites = _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && dealers.Any(k => m.DealerId == k)).Select(m => m.JobsiteId);

            var customers = getAccessibleCustomers().Select(m => m.customer_auto);
            var customerJobsites = _domainContext.CRSF.Where(m => customers.Any(k => k == m.customer_auto)).Select(m => m.crsf_auto);

            var userJobsites = _domainContext.USER_JOBSITE_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.JobsiteId);

            var all = userJobsites.Union(customerJobsites.Union(dealerJobsites));

            return _domainContext.CRSF.Where(m => all.Any(k => k == m.crsf_auto));
        }

        public IQueryable<LU_Module_Sub> GetAccessibleSystems()
        {
            if (!Initialized) return new List<LU_Module_Sub>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.LU_Module_Sub;

            var jobsites = getAccessibleJobsites().Select(j => j.crsf_auto).ToList();
            //var systems = _domainContext
            //    .EQUIPMENT
            //    .Where(e => jobsites.Contains(e.crsf_auto))
            //    .Select(e => e.UCSystems).ToList();
            return _domainContext.LU_Module_Sub.Where(s => jobsites.Contains(s.crsf_auto ?? 0) || (s.equipmentid_auto != null && jobsites.Contains(s.EQUIPMENT.crsf_auto)));

        }

        public bool hasAccessToEquipment(long EquipmentId)
        {
            if (!Initialized) return false;
            if (hasAccessToAnySupportTeam()) return true;
            return getAccessibleEquipments().Where(m => m.equipmentid_auto == EquipmentId).Count() > 0;
        }


     
        public bool HasAccessToInspectionId(int inspectionId)
        {
            if (!Initialized) return false;
            var inspection = _domainContext.TRACK_INSPECTION.Find(inspectionId);
            if (inspection == null) return false;
            if (hasAccessToAnySupportTeam()) return true;
            long equipmentId = inspection.equipmentid_auto;
            return getAccessibleEquipments().Where(m => m.equipmentid_auto == equipmentId).Count() > 0;
        }

        public IQueryable<EQUIPMENT> getAccessibleEquipments()
        {
            if (!Initialized) return new List<EQUIPMENT>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.EQUIPMENT;            
            var jobsites = getAccessibleJobsites().Select(m => m.crsf_auto);

       
            var equipments = _domainContext.USER_EQUIPMENT_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.Equipment.equipmentid_auto);
            return _domainContext.EQUIPMENT.Where(m => jobsites.Any(k => k == m.crsf_auto) || equipments.Any(k => k == m.equipmentid_auto));
        }


        public IQueryable<EQUIPMENT> GetAccessibleEquipmentsByUserId(long userId)
        {
            if (!Initialized) return new List<EQUIPMENT>().AsQueryable();
            if (hasAccessToAnySupportTeam()) return _domainContext.EQUIPMENT;
            var jobsites = getAccessibleJobsites().Select(m => m.crsf_auto);

           
            var equipments = _domainContext.USER_EQUIPMENT_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == userId).Select(m => m.Equipment.equipmentid_auto);
            return _domainContext.EQUIPMENT.Where(m => jobsites.Any(k => k == m.crsf_auto) || equipments.Any(k => k == m.equipmentid_auto));
        }



        public UserRelationV getUserRealtionStatus()
        {
            return new UserRelationV
            {
                SupportMember = getAccessibleSupportTeams().Count() > 0,
                DealerGroupMember = getAccessibleDealerGroups().Count() > 0,
                DealerMember = getAccessibleDealers().Count() > 0,
                CustomerMember = getAccessibleCustomers().Count() > 0,
                JobsiteMember = getAccessibleJobsites().Count() > 0,
                EquipmentMember = getAccessibleEquipments().Count() > 0
            };
        }

        public IQueryable<USER_TABLE> getAccessibleUsers()
        {

            if (hasAccessToAnySupportTeam()) return _domainContext.USER_TABLE;

            var dealerGroupIds = _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.DealerGroupId);
            var dealerGroupUserIds = _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && dealerGroupIds.Any(k => m.DealerGroupId == k)).Select(m => m.UserId);

            var inclusiveDealerIds = getAccessibleDealers().Select(m => m.DealershipId);
            var dealerIds = _domainContext.USER_DEALER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.DealerId);
            var inclusiveDealerUserIds = _domainContext.USER_DEALER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && inclusiveDealerIds.Any(k => m.DealerId == k)).Select(m => m.UserId);
            var dealerUserIds = _domainContext.USER_DEALER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && dealerIds.Any(k => m.DealerId == k)).Select(m => m.UserId);

            var inclusiveCustomerIds = getAccessibleCustomers().Select(m => m.customer_auto);
            var customerIds = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.CustomerId);
            var inclusiveCustomerUserIds = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && inclusiveCustomerIds.Any(k => m.CustomerId == k)).Select(m => m.UserId);
            var customerUserIds = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && customerIds.Any(k => m.CustomerId == k)).Select(m => m.UserId);

            var inclusiveJobsiteIds = getAccessibleJobsites().Select(m => m.crsf_auto);
            var jobsiteIds = _domainContext.USER_JOBSITE_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.JobsiteId);
            var inclusiveJobsiteUserIds = _domainContext.USER_JOBSITE_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && inclusiveJobsiteIds.Any(k => m.JobsiteId == k)).Select(m => m.UserId);
            var jobsiteUserIds = _domainContext.USER_JOBSITE_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && jobsiteIds.Any(k => m.JobsiteId == k)).Select(m => m.UserId);

            var inclusiveEquipmentIds = getAccessibleEquipments().Select(m => m.equipmentid_auto);
            var equipmentIds = _domainContext.USER_EQUIPMENT_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == UserId).Select(m => m.EquipmentId);
            var inclusiveEquipmentUserIds = _domainContext.USER_EQUIPMENT_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && inclusiveEquipmentIds.Any(k => m.EquipmentId == k)).Select(m => m.UserId);
            var equipmentUserIds = _domainContext.USER_EQUIPMENT_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && equipmentIds.Any(k => m.EquipmentId == k)).Select(m => m.UserId);

            if (dealerIds.Count() > 0)
            { //User is part of a dealer -> dealer can see only their users!
                return _domainContext.USER_TABLE.Where(m => dealerUserIds.Any(k => k == m.user_auto));
            }

            var all = dealerGroupUserIds.Union(inclusiveDealerUserIds.Union(inclusiveCustomerUserIds.Union(inclusiveJobsiteUserIds.Union(inclusiveEquipmentUserIds))));
            return _domainContext.USER_TABLE.Where(m => all.Any(k => k == m.user_auto));
        }

        public bool checkUserAccess(AccessCategory Category,int Id, string Role) {
            var roles = _domainContext.USER_GROUP.Where(m => m.USER_GROUP_ASSIGN.Any(k => k.user_auto == UserId && k.active)).Select(m => m.groupname);
            if (!roles.Contains(Role)) return false;
            switch (Category) {
                case AccessCategory.SupportTeam:
                    return hasAccessToSupportTeam(Id);
                case AccessCategory.DealerGroup:
                    return hasAccessToDealerGroup(Id);
                case AccessCategory.Dealer:
                    return hasAccessToDealer(Id);
                case AccessCategory.Customer:
                    return hasAccessToCustomer(Id);
                case AccessCategory.Jobsite:
                    return hasAccessToJobsite(Id);
                case AccessCategory.Equipment:
                    return hasAccessToEquipment(Id);
            }
            return false;
        }

        public bool checkUserAccess(AccessCategory Category, string Role)
        {
            var roles = _domainContext.USER_GROUP.Where(m => m.USER_GROUP_ASSIGN.Any(k => k.user_auto == UserId && k.active)).Select(m => m.groupname);
            if (!roles.Contains(Role)) return false;
            switch (Category)
            {
                case AccessCategory.SupportTeam:
                    return hasAccessToAnySupportTeam();
                case AccessCategory.DealerGroup:
                    return getAccessibleDealerGroups().Count() > 0;
                case AccessCategory.Dealer:
                    return getAccessibleDealers().Count() > 0;
                case AccessCategory.Customer:
                    return getAccessibleCustomers().Count() > 0;
                case AccessCategory.Jobsite:
                    return getAccessibleJobsites().Count() > 0;
                case AccessCategory.Equipment:
                    return getAccessibleEquipments().Count() > 0;
            }
            return false;
        }

        public bool hasRole(string Role) {
            return _domainContext.USER_GROUP.Where(m => m.USER_GROUP_ASSIGN.Any(k => k.user_auto == UserId && k.active)).Select(m => m.groupname).Contains(Role);
        }

        public AccessCategory getHighestCategoryAccess() {
            if (!Initialized)
                return AccessCategory.Unknown;
            var relations = getUserRealtionStatus();
            if (relations.SupportMember) return AccessCategory.SupportTeam;
            if (relations.DealerGroupMember) return AccessCategory.DealerGroup;
            if (relations.DealerMember) return AccessCategory.Dealer;
            if (relations.CustomerMember) return AccessCategory.Customer;
            if (relations.JobsiteMember) return AccessCategory.Jobsite;
            if (relations.EquipmentMember) return AccessCategory.Equipment;

            return AccessCategory.Unknown;
        }

        public IQueryable<string> getRoles() {
            return _domainContext.USER_GROUP.Where(m => m.USER_GROUP_ASSIGN.Any(k => k.user_auto == UserId && k.active)).Select(m => m.groupname);
        }

        /// <summary>
        /// This method returns the dealership for an specific equipment based on the jobsite of that equipment! 
        /// </summary>
        /// <param name="EquipmentId"></param>
        /// <param name="DoNotAuthorize"> If true then checks permission for the user</param>
        /// <returns></returns>
        public IQueryable<DAL.Dealership> getEquipmentDealers(int EquipmentId, bool DoNotAuthorize)
        {
            if (DoNotAuthorize)
            {
                var equipment = _domainContext.EQUIPMENT.Find(EquipmentId);
                if (equipment == null) return new List<DAL.Dealership>().AsQueryable();
                return _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.JobsiteId == equipment.crsf_auto).Select(m => m.Dealer);
            }
            else
            {
                if (!Initialized) return new List<DAL.Dealership>().AsQueryable();
                var equipment = getAccessibleEquipments().Where(m => m.equipmentid_auto == EquipmentId).FirstOrDefault();
                if (equipment == null) return new List<DAL.Dealership>().AsQueryable();
                return _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.JobsiteId == equipment.crsf_auto).Select(m => m.Dealer);
            }
        }

        public IQueryable<SUPPORT_TEAM> getDirectSupportTeamsForUser(int _UserId)
        {
            return _domainContext.USER_SUPPORTTEAM_RELATION.Where(m => m.UserId == _UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.SupportTeam);
        }

        public IQueryable<DEALER_GROUP> getDirectDealerGroupsForUser(int _UserId)
        {
            return _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == _UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.DealerGroup);
        }

        public IQueryable<DAL.Dealership> getDirectDealersForUser(int _UserId)
        {
            return _domainContext.USER_DEALER_RELATION.Where(m => m.UserId == _UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.Dealer);
        }

        public IQueryable<DAL.CUSTOMER> getDirectCustomersForUser(int _UserId)
        {
            return _domainContext.USER_CUSTOMER_RELATION.Where(m => m.UserId == _UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.Customer);
        }

        public IQueryable<DAL.CRSF> getDirectJobsitesForUser(int _UserId)
        {
            return _domainContext.USER_JOBSITE_RELATION.Where(m => m.UserId == _UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.Jobsite);
        }

        public IQueryable<DAL.EQUIPMENT> getDirectEquipmentsForUser(int _UserId)
        {
            return _domainContext.USER_EQUIPMENT_RELATION.Where(m => m.UserId == _UserId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.Equipment);
        }

        public IQueryable<DEALER_GROUP> getDirectDealerGroupsForDealer(int _DealerId)
        {
            return _domainContext.DEALERGROUP_DEALER_RELATION.Where(m => m.DealerId == _DealerId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.DealerGroup);
        }

        public IQueryable<DEALER_GROUP> getDirectDealerGroupsForCustomer(int _CustomerId)
        {
            return _domainContext.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.CustomerId == _CustomerId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.DealerGroup);
        }


        public IQueryable<DAL.Dealership> getDirectDealersForCustomer(int _CustomerId)
        {
            return _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.CustomerId == _CustomerId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.Dealer);
        }

        public IQueryable<DAL.Dealership> getDirectDealersForJobsite(int _JobsiteId)
        {
            return _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.JobsiteId == _JobsiteId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.Dealer);
        }

        public IQueryable<DAL.CUSTOMER> getDirectCustomersForDealer(int _DealerId)
        {
            return _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.DealerId == _DealerId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.Customer);
        }

        public IQueryable<DAL.CRSF> getDirectJobsitesForDealer(int _DealerId)
        {
            return _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.DealerId == _DealerId && m.RecordStatus == (int)RecordStatus.Available).Select(m => m.Jobsite);
        }



        public List<AvailableInspectorsViewMode> GetAllAvailableInspectorBasedOnEquipment(int equipmentId)
        {            
            var users = GetAllInpectorUsersBasedOnEquipment(equipmentId);
            if (users == null) return null;
            var result = new List<AvailableInspectorsViewMode>();
            foreach (var item in users)
            {
                result.Add(new AvailableInspectorsViewMode
                {
                    UserId = (int)item.user_auto,
                    UserName = item.username,
                });
            }
            return result;

        }


        public List<USER_TABLE> GetAllInpectorUsersBasedOnEquipment(int equipmentId)
        {
            if (!Initialized) return null;
            var inspectorGroup = _domainContext.USER_GROUP.FirstOrDefault(g => g.groupname == "Inspectors");
            if (inspectorGroup == null) throw new Exception("Can not find user group Inspectors");

            var allUsers = _domainContext.USER_TABLE.Join(_domainContext.USER_GROUP_ASSIGN,
             us => us.user_auto,
             ug => ug.user_auto, (us, ug) => new { Users = us, UserGroup = ug }
             ).Where(u => u.UserGroup.group_auto == inspectorGroup.group_auto).Select(u=>u.Users).ToList();

            var result = new List<USER_TABLE>();
            foreach (var user in allUsers)
            {
                if (hasAccessToAnySupportTeam(user.user_auto))
                    result.Add(user);
                else
                {
                    var equipments = _domainContext.USER_EQUIPMENT_RELATION.Where(m => m.RecordStatus == (int)RecordStatus.Available && m.UserId == user.user_auto && m.EquipmentId == equipmentId).Select(m => m.Equipment.equipmentid_auto);
                    if(equipments.Count() >0)
                        result.Add(user);
                }
            }
            return result;
        }


    }
}
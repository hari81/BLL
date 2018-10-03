using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Administration.Models
{
    public class UserModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public int AccessTypeId { get; set; }
        public int? DealershipId { get; set; }
        public long? CustomerId { get; set; }
        public long? JobsiteId { get; set; }
        public int? DealerGroupId { get; set; }
        public List<long> AccessCustomerIds { get; set; }
        public List<int> JobRoles { get; set; }
        public bool UndercarriageEnabled { get; set; }
        public bool GetEnabled { get; set; }
        public string Password { get; set; }
        public int StyleId { get; set; }
        public long CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; }
    }

    public class EditUserModel
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int AccessTypeId { get; set; }
        public int? DealershipId { get; set; }
        public long? CustomerId { get; set; }
        public List<AccessCustomerIdsForUserEditModel> AccessCustomerIds { get; set; }
        public List<JobRolesForUserEditModel> JobRoles { get; set; }
        public bool UndercarriageEnabled { get; set; }
        public bool GetEnabled { get; set; }
        public string Password { get; set; }
    }

    public class JobRolesForUserEditModel
    {
        public int id { get; set; }
        public string itemName { get; set; }
    }

    public class AccessCustomerIdsForUserEditModel
    {
        public int id { get; set; }
        public string itemName { get; set; }
    }

    public class UserOverviewModel
    {
        public long UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string TeamName { get; set; }
        public string Email { get; set; }
        public string Access { get; set; }
        public bool Disabled { get; set; }
    }

    public class PendingUserOverviewModel
    {
        public long InviteId { get; set; }
        public string UserName { get; set; }
        public string TeamName { get; set; }
        public string Email { get; set; }
        public string Access { get; set; }
    }

    public class AuthorizedUserAccessType
    {
        public int UserAccessTypeId { get; set; }
        public string Name { get; set; }
    }

    public class JobRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ViewModel
{
    public class UserAccessViewModel
    {

    }

    public class DealerGroupV
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DealerV
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class SupportTeamV
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class UserRelationV
    {
        public bool SupportMember { get; set; }
        public bool DealerGroupMember { get; set; }
        public bool DealerMember { get; set; }
        public bool CustomerMember { get; set; }
        public bool JobsiteMember { get; set; }
        public bool EquipmentMember { get; set; }
    }

    public class CustomerAccessV
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class JobsiteAccessV
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class EquipmentAccessV
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ModifyRelationParam
    {
        public int LeftId { get; set; }
        public int RightId { get; set; }
        public bool Remove { get; set; }
    }
}
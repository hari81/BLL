using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.Interfaces;

namespace BLL.Core.Domain
{
    public class ActionLifeUpdate:IActionLifeUpdate
    {
        public int EqCurrentSMU { get; set; }
        public EQUIPMENT_LIFE EquipmentLife { get; set; }
        public IList<SystemLife> SystemsLife { get; set; }
        public IList<ComponentLife> ComponentsLife { get; set; }
        public ACTION_TAKEN_HISTORY ActionTakenHistory { get; set; }
    }
}
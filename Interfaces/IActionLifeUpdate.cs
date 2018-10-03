using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL;

namespace BLL.Interfaces
{
    public interface IActionLifeUpdate
    {
        int EqCurrentSMU { get; set; }
        EQUIPMENT_LIFE EquipmentLife { get; set; }
        IList<SystemLife> SystemsLife { get; set; }
        IList<ComponentLife> ComponentsLife { get; set; }
        ACTION_TAKEN_HISTORY ActionTakenHistory { get; set; }
    }
}

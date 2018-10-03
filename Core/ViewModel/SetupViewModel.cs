using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;

namespace BLL.Core.ViewModel
{
    public class SetupViewModel
    {
        public int Id { get; set; } = 0;
        public string Serial { get; set; }
        public int JobsiteId { get; set; }
        public UCSystemType Type { get; set; }
        public int EquipmentId { get; set; }
        public int SmuAtInstall { get; set; }
        public int HoursAtInstall { get; set; }
        public int UserId { get; set; }
        public decimal Cost { get; set; }
        public DateTime SetupDate { get; set; } = DateTime.Now;
        public DateTime InstallationDate { get; set; }
        public string Comment { get; set; }
        public bool InstallOnEquipment { get; set; } = false;
        public List<ComponentSetup> Components { get; set; }
        public MakeForSelectionVwMdl Make { get; set; }
        public ModelForSelectionVwMdl Model { get; set; }
        public FamilyForSelectionVwMdl Family { get; set; }
        public ResultMessage Result { get; set; }
        public Side Side { get; set; } = Side.Unknown;
    }

    public class ComponentSetup
    {
        public int Id { get; set; }
        public int SystemId { get; set; } = 0;
        public int listPosition { get; set; }
        public CompartV Compart { get; set; }
        public IdTitleV Grouser { get; set; }
        public MakeForSelectionVwMdl Brand { get; set; }
        public int BudgetLife { get; set; }
        public int HoursAtInstall { get; set; }
        public DateTime InstallDate { get; set; }
        public int EquipmentSMU { get; set; }
        public decimal InstallCost { get; set; }
        public string Note { get; set; }
        public int Pos { get; set; }
        public int Points { get; set; }
        public ShoeSizeV ShoeSize { get; set; }
        public ResultMessage Result { get; set; }
        public bool Validity { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
namespace BLL.Core.ViewModel
{
    public class ComponentActionViewModel
    {
        public int Id { get; set; }
        public int EquipmentId { get; set; }
        public int ComponentId { get; set; }
        public int ActionType { get; set; }
        public string Title { get; set; }
        public DateTime Date { get; set; }
    }
}
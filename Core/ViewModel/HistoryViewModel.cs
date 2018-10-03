using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ViewModel
{
    public class ComponentHistoryTemplate
    {
        public int Id { get; set; }
        public int _order { get; set; }
        public int CompartTypeId { get; set; }
        public Domain.Side Side { get; set; }
        public int Pos { get; set; }
    }
    public class SystemHistoryTemplate
    {
        public int Id { get; set; }
        public int _order { get; set; }
        public Domain.UCSystemType SystemTypeId { get; set; }
        public Domain.Side Side { get; set; }
        public List<ComponentHistoryTemplate> ComponentsHistory { get; set; }
    }
    public class EquipmentHistoryTemplate
    {
        public int Id { get; set; }
        public List<SystemHistoryTemplate> SystemsHistory { get; set; }
    }
    public class ComponentHistoryQueryViewModel
    {
        public IQueryable<Domain.ComponentHistoryOldViewModel> Query { get; set; }
        //item1 -> historyId, item2 -> componentId
        public List<Tuple<int,int, DateTime>> ComponentIds { get; set; }
    }
}
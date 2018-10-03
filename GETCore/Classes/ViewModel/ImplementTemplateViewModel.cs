using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static BLL.GETInterfaces.Enum;

namespace BLL.GETCore.Classes.ViewModel
{
    public class NewImplementTemplateViewModel
    {
        public long TemplateId { get; set; }
        public string TemplateName { get; set; }
        public TemplateAccess TemplateAccess { get; set; }
        public Int64? CustomerId { get; set; }
        public ImplementCategory ImplementCategory { get; set; }
        public int[] EquipmentModels { get; set; }
        public List<ImplementComponentTypeViewModel> ComponentTypes { get; set; }
    }

    public class TemplateViewModel {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
    }

    public class ImplementComponentTypeViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ImplementTemplateExtendedViewModel : TemplateViewModel
    {
        public string CustomerName { get; set; }
        public int ImplementsUsing { get; set; }
        public ImplementCategory ImplementCategory { get; set; }
        //public int[] SchematicImageIds { get; set; }
    }
}
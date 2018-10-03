using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Middleware
{
    public class MenuResultVwMdl
    {
        public int Id { get; set; }
        public int Levels { get; set; }
        public int SupMenuAuto { get; set; }
        public int MenuId { get; set; }
        public int ModuleId { get; set; }
        public string Label { get; set; }
        public string RelativePath { get; set;}
        public int ObjectId { get; set; }
        public int SortOrder { get; set; }
        public string Tooltip { get; set; }
        public bool NewWindow { get; set; }
    }

}
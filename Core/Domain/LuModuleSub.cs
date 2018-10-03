using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.Extensions;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace BLL.Core.Domain
{
    public class LuModuleSub
    {
        private UndercarriageContext _context;

        public LuModuleSub(UndercarriageContext context)
        {
            this._context = context;
        }

        public List<COMPART_TOOL_IMAGE> GetEquipmentCompartToolImageListByModuleSubAuto(long Module_sub_auto)
        {
            List<COMPART_TOOL_IMAGE> resultList = new List<COMPART_TOOL_IMAGE>();
            if (Module_sub_auto == 0)
                return resultList;
            var components = GetEquipmentComponentsBySubModuleId(Module_sub_auto);
            foreach (var cmp in components)
            {
                var k = new UndercarriageContext().COMPART_TOOL_IMAGE.Where(m => m.CompartId == cmp.compartid_auto);
                resultList.AddRange(k);
            }
            return resultList.GroupBy(m => m.Id).Select(m => m.First()).ToList();
        }

        public List<CompartWornExtViewModel> getWornLimitListBySubModuleId(long Module_sub_auto)
        {
            List<CompartWornExtViewModel> result = new List<CompartWornExtViewModel>();
            var components = GetEquipmentComponentsBySubModuleId(Module_sub_auto);
            foreach (var DALcomp in components.GroupBy(m => m.compartid_auto).Select(m => m.First()))
            {
                var LogicalCompart = new Compart(new UndercarriageContext(), DALcomp.compartid_auto);
                if (LogicalCompart.Id != 0)
                    result.AddRange(LogicalCompart.getCompartWornDataAllMethods());
            }
            return result;
        }

        public List<GENERAL_EQ_UNIT> GetEquipmentComponentsBySubModuleId(long Module_sub_auto)
        {
            IList<GENERAL_EQ_UNIT> DALComponents = new UndercarriageContext().GENERAL_EQ_UNIT.Where(m => m.module_ucsub_auto == Module_sub_auto).OrderBy(m => m.LU_COMPART.LU_COMPART_TYPE.sorder).ThenBy(m => m.pos).ToList();
            return DALComponents.ToList();
        }

        public List<CompartWornExtViewModel> getWornLimitListByCompartIdAuto(int compartid_auto)
        {
            List<CompartWornExtViewModel> result = new List<CompartWornExtViewModel>();
            var LogicalCompart = new Compart(new UndercarriageContext(), compartid_auto);
            if (LogicalCompart.Id != 0)
                result.AddRange(LogicalCompart.getCompartWornDataAllMethods());
            return result;
        }
    }
}
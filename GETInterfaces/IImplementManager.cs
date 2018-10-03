using BLL.GETCore.Classes.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.GETInterfaces
{
    public interface IImplementManager
    {
        Task<Tuple<bool, List<TemplateViewModel>>> GetTemplateListForEquipment(long equipmentId);
    }
}
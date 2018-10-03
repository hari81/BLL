using BLL.GETCore.Classes.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.GETInterfaces
{
    public interface IImplementTemplateManager
    {
        Task<Tuple<int, string>> CreateNewImplementTemplate(NewImplementTemplateViewModel newImplementTemplate);
        Task<Tuple<bool, List<ImplementTemplateExtendedViewModel>>> GetImplementTemplatesForUser(long userId);
        Task<Byte[]> getSchematicImageSmall(int schematicId);
        Task<Tuple<bool, NewImplementTemplateViewModel>> ReturnTemplateById(long templateId);
        Task<Tuple<int, string>> UpdateExistingImplementTemplate(NewImplementTemplateViewModel implementTemplate);
    }
}
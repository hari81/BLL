using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.Domain
{
    public class SharedDomain
    {
        protected DAL.SharedContext _domainContext;
        
        public SharedDomain(DAL.SharedContext context)
        {
            _domainContext = context;
        }

        public async Task<IEnumerable<MakeForSelectionVwMdl>> getComponentMakeList()
        {
            return await Task.Run<IEnumerable<MakeForSelectionVwMdl>>(() => _domainContext.MAKE.Where(m => m.Components ?? false).Select(m => new MakeForSelectionVwMdl { Id = m.make_auto, Symbol = m.makeid, Title = m.makedesc }).OrderBy(m=> m.Title));
        }

        public IEnumerable<MakeForSelectionVwMdl> getComponentMakeListNonAsync()
        {
            return _domainContext.MAKE.Where(m => m.Components ?? false).Select(m => new MakeForSelectionVwMdl { Id = m.make_auto, Symbol = m.makeid, Title = m.makedesc }).OrderBy(m => m.Title);
        }
        public IEnumerable<MakeForSelectionVwMdl> getComponentMakeListForEquipmentNonAsync(int equipmentId)
        {
            var equipment = _domainContext.EQUIPMENT.Find(equipmentId);
            var result = new List<MakeForSelectionVwMdl>();
            if (equipment == null)
            return _domainContext.MAKE.Where(m => m.Components ?? false).Select(m => new MakeForSelectionVwMdl { Id = m.make_auto, Symbol = m.makeid, Title = m.makedesc }).OrderBy(m => m.Title);
            result.Add(new MakeForSelectionVwMdl { Id = equipment.LU_MMTA.MAKE.make_auto, Title = equipment.LU_MMTA.MAKE.makedesc, Symbol = equipment.LU_MMTA.MAKE.makeid });
            result.AddRange(_domainContext.MAKE.Where(m => m.Components ?? false).Select(m => new MakeForSelectionVwMdl { Id = m.make_auto, Symbol = m.makeid, Title = m.makedesc }));
            return result.OrderBy(m => m.Title);
        }

        public MakeForSelectionVwMdl getEquipmentMake(int EquipmentId) {
            var equipment = _domainContext.EQUIPMENT.Find(EquipmentId);
            if (equipment == null) return new MakeForSelectionVwMdl { Id = 0, Title = "Unknown", Symbol = "UN", ExistingCount = 0 };
            return new MakeForSelectionVwMdl { Id = equipment.LU_MMTA.MAKE.make_auto, Title = equipment.LU_MMTA.MAKE.makedesc, Symbol = equipment.LU_MMTA.MAKE.makeid };
        }
        /// <summary>
        /// Return stored value in MENU_L2 target_path column for row number 5 !
        /// Ids for menu are assumed to be the same in all applications
        /// </summary>
        /// <returns></returns>
        public string getApkFileAddressFromMenu() {
            var apkMenu = _domainContext.MENU_L2.Find(5);
            if (apkMenu == null)
                return "";
            return apkMenu.targetpath;
        }
    }
}
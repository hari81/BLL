using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Domain
{
    public class AppConfigAccess
    {
        private DAL.SharedContext _contex;
        public AppConfigAccess()
        {
            _contex = new DAL.SharedContext();
        }
        
        public string GetApplicationValue(string Key)
        {
            var values = _contex.APPLICATION_LU_CONFIG.Where(m => m.variable_key.ToUpper().Trim() == Key.ToUpper().Trim());
            if (values.Count() > 0)
                return values.First().value_key;
            return "";
        }
    }
}
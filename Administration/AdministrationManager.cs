using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Administration
{
    public class AdministrationManager
    {
        private SharedContext _context;

        public AdministrationManager()
        {
            this._context = new SharedContext();
        }


    }
}
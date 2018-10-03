using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace BLL.AuthCore
{
    public class TTAuthorization
    {
        protected DAL.SharedContext _context;
        public TTAuthorization(DbContext context)
        {
            _context = (DAL.SharedContext)context;
        }
        public int longNullableToint(long? number)
        {
            if (number == null)
                return 0;
            if (number > Int32.MaxValue) //:) So Stupid
                return Int32.MaxValue;
            if (number < Int32.MinValue) // :))
                return Int32.MinValue;
            try { return (int)number; } catch { return 0; }
        }
    }
}
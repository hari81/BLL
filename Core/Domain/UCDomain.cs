using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using DAL;
using BLL.Core.ViewModel;

namespace BLL.Core.Domain
{
    public class UCDomain
    {
        protected IUndercarriageContext _domainContext;
        public UCDomain(IUndercarriageContext context)
        {
            _domainContext = context;
        }

        public IEnumerable<CompartTypeV> getAvailableCompartTypeList() {
            return _domainContext.LU_COMPART_TYPE.Select(m => new CompartTypeV { Id = m.comparttype_auto, Order = m.sorder ?? 1, Title = m.comparttype });
        }

    }
}
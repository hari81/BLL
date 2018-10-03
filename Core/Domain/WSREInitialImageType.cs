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
    public class WSREInitialImageType
    {

        public int GetIdByDescr(string description)
        {
            using (var dataEntities = new UndercarriageContext())
            {
                var items = dataEntities.Database.SqlQuery<DAL.WSREInitialImageType>(
                    "select top 1 * from WSREInitialImageTypes "
                    + " where Description = @Description"
                    ,new SqlParameter("@Description", description)
                ).ToList();

                foreach (var item in items)
                {
                    return item.Id;
                }
            }
            return 0;
        }

    }
}
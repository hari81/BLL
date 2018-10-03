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
    public class CUSTOMER_MODEL_MANDATORY_IMAGE
    {

        public int GetIdByTitle(string title)
        {
            using (var dataEntities = new UndercarriageContext())
            {
                var items = dataEntities.Database.SqlQuery<DAL.CUSTOMER_MODEL_MANDATORY_IMAGE>(
                    "select top 1 * from CUSTOMER_MODEL_MANDATORY_IMAGE "
                    + " where Title = @Title"
                    , new SqlParameter("@Title", title)
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
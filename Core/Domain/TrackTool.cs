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
    public class TrackTool
    {

        public int GetIdByToolCode(string toolCode)
        {
            using (var dataEntities = new UndercarriageContext())
            {
                var items = dataEntities.Database.SqlQuery<DAL.TRACK_TOOL>(
                    "select top 1 * from TRACK_TOOL "
                    + " where tool_code = @tool_code"
                    , new SqlParameter("@tool_code", toolCode)
                ).ToList();

                foreach (var item in items)
                {
                    return item.tool_auto;
                }
            }
            return 0;
        }

    }
}
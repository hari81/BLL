using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.Extensions;
using System.Threading.Tasks;

namespace BLL.Core.Domain
{
    public class LinksCondition
    {
        public List<DAL.LuLinksCondition> GetAllLinksConditions()
        {
            List<DAL.LuLinksCondition> result = new List<DAL.LuLinksCondition>();
            using (var dataEntities = new UndercarriageContext())
            {
                var items = dataEntities.Database.SqlQuery<DAL.LuLinksCondition>(
                    "select * from LuLinksConditions"
                ).ToList();

                foreach (var item in items)
                {
                    result.Add(item);
                }
            }
            return result;
        }

    }
}
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
    public class WSRE_TEMP_UPLOAD_IMAGE
    {

        public int CheckRecordExist(int? UploadInspectionId, string FileName)
        {
            using (var dataEntities = new UndercarriageContext())
            {
                var items = dataEntities.Database.SqlQuery<DAL.TEMP_UPLOAD_IMAGE>(
                    "select top 1 * from WSRE_TEMP_UPLOAD_IMAGE "
                    + " where UploadInspectionId = @UploadInspectionId and FileName = @FileName"
                    , new SqlParameter("@UploadInspectionId", UploadInspectionId)
                    , new SqlParameter("@FileName", FileName)
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
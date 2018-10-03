using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Domain
{
    public static class ApplicationDefaultStyles
    {
        public const string IdentityDefaultStyle = "/Content/scss/main.css";
        public const string UCDefaultStyle = "/Content/TrackTreads/css/master.css";
        public const string UCUIDefaultStyle = "/Content/scss/themes/default.css";
        public const string GETDefaultStyle = "/Content/scss/main.css";
        public const string GETUIDefaultStyle = "/Content/scss/main.css";
        public const string DefaultStyle = "/Content/scss/main.css";
    }

    public static class ApplicationDefaultURIs
    {
        public const string IdentityServer = "login.tracktreads.com";
        public const string UC = "undercarriage.tracktreads.com";
        public const string UCUI = "undercarriageui.tracktreads.com";
        public const string GET = "get.tracktreads.com";
        public const string GETUI = "getui.tracktreads.com";
    }

    public static class DefaultTemplate
    {
        public static List<SystemTemplateVwMdl> getUndercarriageSystemTemplate(int ModelId)
        {
            var result = new List<SystemTemplateVwMdl>();
            result.Add(new SystemTemplateVwMdl
            {
                Id = 1,
                CompartTypeId = (int)CompartTypeEnum.Link ,
                ModelId = ModelId,
                Name = "Default Link",
                Min = 1,
                Max = 1
            });
            result.Add(new SystemTemplateVwMdl
            {
                Id = 2,
                CompartTypeId = (int)CompartTypeEnum.Bushing,
                ModelId = ModelId,
                Name = "Default Bushing",
                Min = 1,
                Max = 1
            });
            result.Add(new SystemTemplateVwMdl
            {
                Id = 3,
                CompartTypeId = (int)CompartTypeEnum.Shoe,
                ModelId = ModelId,
                Name = "Default Shoe",
                Min = 1,
                Max = 1
            });
            result.Add(new SystemTemplateVwMdl
            {
                Id = 4,
                CompartTypeId = (int)CompartTypeEnum.Idler,
                ModelId = ModelId,
                Name = "Default Idler",
                Min = 1,
                Max = 2
            });
            result.Add(new SystemTemplateVwMdl
            {
                Id = 5,
                CompartTypeId = (int)CompartTypeEnum.CarrierRoller,
                ModelId = ModelId,
                Name = "Default Carrier Roller",
                Min = 0,
                Max = 3
            });
            result.Add(new SystemTemplateVwMdl
            {
                Id = 6,
                CompartTypeId = (int)CompartTypeEnum.TrackRoller,
                ModelId = ModelId,
                Name = "Default Track Roller",
                Min = 4,
                Max = 15
            });
            result.Add(new SystemTemplateVwMdl
            {
                Id = 7,
                CompartTypeId = (int)CompartTypeEnum.Sprocket,
                ModelId = ModelId,
                Name = "Default Sprocket",
                Min = 1,
                Max = 1
            });
            result.Add(new SystemTemplateVwMdl
            {
                Id = 8,
                CompartTypeId = (int)CompartTypeEnum.Guard,
                ModelId = ModelId,
                Name = "Default Guard",
                Min = 0,
                Max = 1
            });
            result.Add(new SystemTemplateVwMdl
            {
                Id = 9,
                CompartTypeId = (int)CompartTypeEnum.TrackElongation,
                ModelId = ModelId,
                Name = "Default Track Elongation",
                Min = 0,
                Max = 1
            });
            return result;
        }
    }
}
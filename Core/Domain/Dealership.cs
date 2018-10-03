using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using System.Data.Entity;
using DAL;

namespace BLL.Core.Domain
{
    public class Dealership : UCDomain
    {
        public Dealership(IUndercarriageContext context) : base(context)
        {

        }
        public IEnumerable<ReportVwMdl> getDealershipAvailableReports(int DealershipId)
        {
            return _domainContext.DealershipReports.Where(m => m.DealershipId == DealershipId)
                .Select(m => new ReportVwMdl { ReportId = m.ReportId, ReportName = m.Report.report_display_name });
        }


        public IEnumerable<ReportVwMdl> getDealershipAvailableQuoteReports(int DealershipId)
        {
            return _domainContext.DealershipQuoteReports.Where(m => m.DealershipId == DealershipId)
               .Select(m => new ReportVwMdl { ReportId = m.QuoteReportId, ReportName = m.QuoteReport.QuoteReportDesc });
        }


        //public IEnumerable<QuoteReportStyleViewModel> getDealershipAvailableReports(int DealershipId)
        //{

        //}
        public string getDealershipStyleByHost(string host, InfotrakApplications InfotrakApp)
        {
            IEnumerable<DealershipBranding> brandedrecords;
            switch (InfotrakApp)
            {
                case InfotrakApplications.IdentityServer:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.IdentityHost == host);
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultStyles.IdentityDefaultStyle;
                    return brandedrecords.First().ApplicationStyle.IdentityCSSFile;

                case InfotrakApplications.UC:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.UCHost == host);
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultStyles.UCDefaultStyle;
                    return brandedrecords.First().ApplicationStyle.UCCSSFile;

                case InfotrakApplications.UCUI:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.UCUIHost == host);
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultStyles.UCUIDefaultStyle;
                    return brandedrecords.First().ApplicationStyle.UCUICSSFile;

                case InfotrakApplications.GET:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.GETHost == host);
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultStyles.GETDefaultStyle;
                    return brandedrecords.First().ApplicationStyle.GETCSSFile;

                case InfotrakApplications.GETUI:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.GETUIHost == host);
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultStyles.GETUIDefaultStyle;
                    return brandedrecords.First().ApplicationStyle.GETUICSSFile;

                default:
                    return ApplicationDefaultStyles.DefaultStyle;
            }
        }
        /// <summary>
        /// Returns Identity host address based on the provided host and application
        /// </summary>
        /// <param name="host"></param>
        /// <param name="InfotrakApp"></param>
        /// <returns></returns>
        public string getDealershipIdentityAddressByHost(string host, InfotrakApplications InfotrakApp)
        {
            IEnumerable<DealershipBranding> brandedrecords;
            switch (InfotrakApp)
            {
                case InfotrakApplications.IdentityServer:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.IdentityHost == host);
                    break;
                case InfotrakApplications.UC:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.UCHost == host);
                    break;
                case InfotrakApplications.UCUI:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.UCUIHost == host);
                    break;
                case InfotrakApplications.GET:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.GETHost == host);
                    break;
                case InfotrakApplications.GETUI:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.GETUIHost == host);
                    break;
                default:
                    return ApplicationDefaultURIs.IdentityServer;
            }
            if(brandedrecords.Count() == 0)
                return ApplicationDefaultURIs.IdentityServer;
            return brandedrecords.First().IdentityHost;
        }

        /// <summary>
        /// Returns Dealership Branding record based on the provided host and application
        /// </summary>
        /// <param name="host"></param>
        /// <param name="InfotrakApp"></param>
        /// <returns></returns>
        public DealershipBranding getDealershipBrandingByHost(string host, InfotrakApplications InfotrakApp)
        {
            IEnumerable<DealershipBranding> brandedrecords;
            switch (InfotrakApp)
            {
                case InfotrakApplications.IdentityServer:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.IdentityHost == host);
                    break;
                case InfotrakApplications.UC:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.UCHost == host);
                    break;
                case InfotrakApplications.UCUI:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.UCUIHost == host);
                    break;
                case InfotrakApplications.GET:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.GETHost == host);
                    break;
                case InfotrakApplications.GETUI:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.GETUIHost == host);
                    break;
                default:
                    return null;
            }
            if (brandedrecords.Count() == 0)
                return null;
            return brandedrecords.First();
        }

        /// <summary>
        /// Returns the requested application based on the IdentityServer Host.
        /// Only must be called from Identity server to knows to which application should redirect
        /// </summary>
        /// <param name="identityHost"></param>
        /// <param name="InfotrakApp"></param>
        /// <returns></returns>
        public string getDealershipApplicationAddressByIdentityHost(InfotrakApplications InfotrakApp, string identityHost)
        {
            IEnumerable<DealershipBranding> brandedrecords = _domainContext.DealershipBranding.Where(m => m.IdentityHost == identityHost);
            switch (InfotrakApp)
            {
                case InfotrakApplications.UC:
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultURIs.UC;
                    return brandedrecords.First().UCHost;

                case InfotrakApplications.UCUI:
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultURIs.UCUI;
                    return brandedrecords.First().UCUIHost;

                case InfotrakApplications.GET:
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultURIs.GET;
                    return brandedrecords.First().GETHost;

                case InfotrakApplications.GETUI:
                    if (brandedrecords.Count() == 0)
                        return ApplicationDefaultURIs.GETUI;
                    return brandedrecords.First().GETUIHost;
                default:
                    return "/";
            }
        }

        private byte[] getDefaultTrackTreadLogo()
        {
            string image = "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";
            return Convert.FromBase64String(image);
        }

        public byte[] getDealershipLogoByHost(string host, InfotrakApplications InfotrakApp)
        {
            IEnumerable<DealershipBranding> brandedrecords;
            switch (InfotrakApp)
            {
                case InfotrakApplications.IdentityServer:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.IdentityHost == host);
                    if (brandedrecords.Count() == 0)
                        return getDefaultTrackTreadLogo();
                    return brandedrecords.First().DealershipLogo;

                case InfotrakApplications.UC:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.UCHost == host);
                    if (brandedrecords.Count() == 0)
                        return getDefaultTrackTreadLogo();
                    return brandedrecords.First().DealershipLogo;

                case InfotrakApplications.UCUI:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.UCUIHost == host);
                    if (brandedrecords.Count() == 0)
                        return getDefaultTrackTreadLogo();
                    return brandedrecords.First().DealershipLogo;

                case InfotrakApplications.GET:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.GETHost == host);
                    if (brandedrecords.Count() == 0)
                        return getDefaultTrackTreadLogo();
                    return brandedrecords.First().DealershipLogo;

                case InfotrakApplications.GETUI:
                    brandedrecords = _domainContext.DealershipBranding.Where(m => m.GETUIHost == host);
                    if (brandedrecords.Count() == 0)
                        return getDefaultTrackTreadLogo();
                    return brandedrecords.First().DealershipLogo;

                default:
                    return getDefaultTrackTreadLogo();
            }
        }

        /// <summary>
        /// Returns requested branding details
        /// </summary>
        /// <param name="brandingId"></param>
        /// <returns></returns>
        public DealershipBrandingVwMdl getBrandingDetails(int brandingId)
        {
            var branding = _domainContext.DealershipBranding.Find(brandingId);
            string logo = "";
            try { logo = Convert.ToBase64String(branding.DealershipLogo); } catch { logo = Convert.ToBase64String(getDefaultTrackTreadLogo()); }
            if (branding == null)
                return new DealershipBrandingVwMdl();
            return new DealershipBrandingVwMdl
            {
                Id = branding.Id,
                ApplicationStyleId = branding.ApplicationStyleId,
                DealershipId = branding.DealershipId,
                GETUIHost = branding.GETUIHost,
                DealershipLogo = logo,
                GETHost = branding.GETHost,
                IdentityHost = branding.IdentityHost,
                Name = branding.Name,
                UCHost = branding.UCHost,
                UCUIHost = branding.UCUIHost,
                ReturnUrl = branding.LogoutRedirectUrl
            };
        }
        /// <summary>
        /// Returns all stored brandings for a dealership, only name and Id of available brandings are sent
        /// </summary>
        /// <param name="dealershipId"></param>
        /// <returns></returns>
        public IEnumerable<DealershipBarndingIdName> getDealershipStoredBrandings(int dealershipId)
        {
            return _domainContext.DealershipBranding.Where(m => m.DealershipId == dealershipId).Select(m => new DealershipBarndingIdName { Id = m.Id, Name = m.Name });
        }
        /// <summary>
        /// This method removes the select branding for the dealership
        /// 
        /// </summary>
        /// <param name="brandingId"></param>
        /// <returns>ResultMessage which has operation succeed and last message :) very useful</returns>
        public ResultMessage RemoveStoredBranding(int brandingId)
        {
            var branding = _domainContext.DealershipBranding.Find(brandingId);
            if (branding == null)
                return new ResultMessage { Id = 0, OperationSucceed = false, LastMessage = "Selected brand cannot be found in database!", ActionLog = "Selected brand cannot be found in database!" };
            _domainContext.DealershipBranding.Remove(branding);
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage { Id = brandingId, OperationSucceed = true, LastMessage = "Selected brand removed successfully.", ActionLog = "Selected brand removed successfully." };
            }
            catch (Exception ex)
            {
                return new ResultMessage { Id = 0, OperationSucceed = false, LastMessage = "Failed to remove from database! Please check log by pressing F12 or contact infotrak support!", ActionLog = ex.InnerException == null ? ex.Message : ex.InnerException.Message };
            }
        }
        /// <summary>
        /// Adds the new brand for the dealership
        /// </summary>
        /// <param name="brand"></param>
        /// <returns></returns>
        public ResultMessage AddOrUpdateBrand(DealershipBrandingVwMdl brand)
        {
            string[] UCArr = brand.UCHost.Split('.');
            string[] UCUIArr = brand.UCUIHost.Split('.');
            string[] GETArr = brand.GETHost.Split('.');
            string[] GETUIArr = brand.GETUIHost.Split('.');
            bool sameLength = false;
            if (UCArr.Length == UCUIArr.Length && UCUIArr.Length == GETArr.Length && GETArr.Length == GETUIArr.Length)
                sameLength = true;
            if (!sameLength)
            {
                return new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    LastMessage = "All host addresses must be on the same domain!",
                    ActionLog = "All host addresses must be on the same domain!"
                };
            }
            bool sameDomain = false;
            if (UCArr.Length > 2 && UCArr[2] == UCUIArr[2] && UCUIArr[2] == GETArr[2] && GETArr[2] == GETUIArr[2] &&
                UCArr[1] == UCUIArr[1] && UCUIArr[1] == GETArr[1] && GETArr[1] == GETUIArr[1])
                sameDomain = true;
            if (!sameDomain)
            {
                return new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    LastMessage = "All host addresses must be on the same domain!",
                    ActionLog = "All host addresses must be on the same domain!"
                };
            }
            byte[] logo = null;
            try {
                string[] logoArr = brand.DealershipLogo.Split(',');
                if(logoArr.Length > 1)
                logo = Convert.FromBase64String(logoArr[1]);
            } catch(Exception ex) {
                string message = ex.Message;
            };
            var currentBrand = _domainContext.DealershipBranding.Find(brand.Id);
            var nextBrand = new DealershipBranding
            {
                ApplicationStyleId = brand.ApplicationStyleId,
                DealershipId = brand.DealershipId,
                DealershipLogo = logo,
                GETHost = brand.GETHost,
                GETUIHost = brand.GETUIHost,
                IdentityHost = brand.IdentityHost,
                Name = brand.Name,
                UCHost = brand.UCHost,
                UCUIHost = brand.UCUIHost,
                LogoutRedirectUrl = brand.ReturnUrl
            };
            if (currentBrand == null) //Add
            {
                _domainContext.ClientRedirectUris.Add(new ClientRedirectUri {
                    Client_Id = (int)InfotrakClientApps.UCUI,
                    Uri = "http://" + brand.UCUIHost + "/account/signInCallback"
                });
                _domainContext.ClientRedirectUris.Add(new ClientRedirectUri
                {
                    Client_Id = (int)InfotrakClientApps.GETUI,
                    Uri = "http://" + brand.GETUIHost + "/account/signInCallback"
                });
                _domainContext.DealershipBranding.Add(nextBrand);
            }
            else //Update
            {

                var UCUrisClient = _domainContext.ClientRedirectUris.Where(m => m.Uri == "http://" + currentBrand.UCUIHost + "/account/signInCallback" && m.Client_Id == (int)InfotrakClientApps.UCUI);
                foreach (var m in UCUrisClient) {
                    m.Uri = "http://" + brand.UCUIHost + "/account/signInCallback";
                    _domainContext.MarkAsModified(m);
                }
                var GETUrisClient = _domainContext.ClientRedirectUris.Where(m => m.Uri == "http://" + currentBrand.GETUIHost + "/account/signInCallback" && m.Client_Id == (int)InfotrakClientApps.GETUI);
                foreach (var m in GETUrisClient) {
                    m.Uri = "http://" + brand.GETUIHost + "/account/signInCallback";
                    _domainContext.MarkAsModified(m);
                }
                currentBrand.ApplicationStyleId = brand.ApplicationStyleId;
                currentBrand.DealershipId = brand.DealershipId;
                currentBrand.DealershipLogo = logo;
                currentBrand.GETHost = brand.GETHost;
                currentBrand.GETUIHost = brand.GETUIHost;
                currentBrand.IdentityHost = brand.IdentityHost;
                currentBrand.Name = brand.Name;
                currentBrand.UCHost = brand.UCHost;
                currentBrand.UCUIHost = brand.UCUIHost;
                currentBrand.LogoutRedirectUrl = brand.ReturnUrl;
                _domainContext.MarkAsModified(currentBrand);
            }
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage
                {
                    Id = nextBrand.Id,
                    LastMessage = "Operation succeeded.",
                    ActionLog = "Operation succeeded. ",
                    OperationSucceed = true
                };
            }
            catch (Exception ex)
            {
                return new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = false,
                    LastMessage = "Failed to add/update new brand to database! Please check log by pressing F12 or contact infotrak support!",
                    ActionLog = ex.InnerException == null ? ex.Message : ex.InnerException.Message
                };
            }
        }

        public IEnumerable<ApplicationStyleIdName> getApplicationStyleForDealerships()
        {
            return _domainContext.ApplicationStyle.Select(m => new ApplicationStyleIdName { Id = m.Id, Name = m.Name }).AsEnumerable();
        }

        public DealershipBranding getDealershipFirstBranding() {
            return _domainContext.DealershipBranding.FirstOrDefault();
        }
    }
}
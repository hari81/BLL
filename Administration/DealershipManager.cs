using BLL.Administration.Models;
using BLL.Extensions;
using BLL.GETCore.Classes;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Administration
{
    public class DealershipManager
    {
        private SharedContext _context;
        private UndercarriageContext _undercarriageContext;
        private const int GLOBAL_ADMIN = 1;
        private const int DEALERSHIP_ADMIN = 2;
        private const int DEALERSHIP_USER = 3;
        private const int CUSTOMER_ADMIN = 4;
        private const int CUSTOMER_USER = 5;

        public DealershipManager()
        {
            this._context = new SharedContext();
            this._undercarriageContext = new UndercarriageContext();
        }

        /// <summary>
        /// Returns a list of possible branded styles which are selectable for a new user. The person creating the new user will select one of these,
        /// and this URL will be sent in the email to the new user providing them with the URL based on this selection to login. 
        /// </summary>
        /// <param name="newUserAccessTypeId">The AccessLevel of the new user. (Global Admin, Dealership Admin, etc. )</param>
        /// <param name="newUserDealershipId">The dealership this new user will belong to. Only relevent if the user will be a dealership admin or user. </param>
        /// <param name="currentUrl">The current URL this account is being created from. If the user is being created by a customer admin, they will not be able to select
        /// their own style, and will only be able to create users using the same site as theirs. </param>
        /// <returns>A list of possible branded styles a new user account can use. </returns>
        public async Task<List<BrandedStyleModel>> GetPossibleBrandedStylesForNewUser(int requestingUserAccessTypeId, int newUserAccessTypeId, int newUserTeamId, string currentUrl)
        {
            switch(newUserAccessTypeId)
            {
                case GLOBAL_ADMIN:
                    return await _undercarriageContext.DealershipBranding.Select(d => new BrandedStyleModel()
                    {
                        Id = d.Id,
                        Name = d.Name + " (" + d.IdentityHost + ")"
                    }).ToListAsync();
                case DEALERSHIP_ADMIN:
                case DEALERSHIP_USER:
                    return await _undercarriageContext.DealershipBranding.Where(d => d.DealershipId == newUserTeamId).Select(d => new BrandedStyleModel()
                    {
                        Id = d.Id,
                        Name = d.Name + " (" + d.IdentityHost + ")"
                    }).ToListAsync();
                case CUSTOMER_ADMIN:
                case CUSTOMER_USER:
                    /* 
                     If the user will belong to a customer. We need to check if the user creating the account also belongs to a customer,
                     or if they are part of a dealership. 

                     If the user creating the new user belongs to a customer, then they will not have any choice of branded site and we use
                     the URL the user is currently logged in to. 

                     If the user is a global or dealership admin, then we let them select from the list of available URLs within that dealership. 
                     */
                    if (requestingUserAccessTypeId == CUSTOMER_ADMIN)
                    {
                        return await _undercarriageContext.DealershipBranding.Where(d => d.IdentityHost.ToLower() == currentUrl.ToLower()).Select(d => new BrandedStyleModel()
                        {
                            Id = d.Id,
                            Name = d.Name + " (" + d.IdentityHost + ")"
                        }).ToListAsync();
                    }
                    else if (requestingUserAccessTypeId == DEALERSHIP_ADMIN || requestingUserAccessTypeId == GLOBAL_ADMIN)
                    {
                        int dealershipId = await _context.CUSTOMER.Where(c => c.customer_auto == newUserTeamId).Select(c => c.DealershipId).FirstOrDefaultAsync();
                        return await _undercarriageContext.DealershipBranding.Where(d => d.DealershipId == dealershipId).Select(d => new BrandedStyleModel()
                        {
                            Id = d.Id,
                            Name = d.Name + " (" + d.IdentityHost + ")"
                        }).ToListAsync();
                    }
                    else
                        return null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns a details object with all the data related to the given branded style Id. 
        /// </summary>
        /// <param name="styleId">The Branded Style to return. </param>
        /// <returns></returns>
        public async Task<BrandedStyleDetailsModel> GetBrandedStyleDetails(int styleId)
        {
            var b = await _undercarriageContext.DealershipBranding.Where(brand => brand.Id == styleId).FirstOrDefaultAsync();
            string logo = "";
            try
            {
                logo = Convert.ToBase64String(b.DealershipLogo);
            }
            catch
            {
                logo = "";
            }

            return new BrandedStyleDetailsModel()
            {
                ColourSchemeId = b.ApplicationStyleId,
                DealershipId = b.DealershipId,
                GetUiUrl = b.GETUIHost,
                GetUrl = b.GETHost,
                Id = styleId,
                IdentityServerUrl = b.IdentityHost,
                Logo = logo,
                LogoutRedirectUrl = b.LogoutRedirectUrl,
                Name = b.Name,
                UndercarriageUiUrl = b.UCUIHost,
                UndercarriageUrl = b.UCHost
            };
        }


        /// <summary>
        /// Gets a list of all possible report types. Used when a global admin is creating a new dealership, they can select
        /// the report types which they want this dealership and it's customers to be able to use. 
        /// </summary>
        /// <returns>A list of Report Types. </returns>
        public async Task<List<ReportModel>> GetAllReportTypes()
        {
            return await _undercarriageContext.FLUID_REPORT_LU_REPORTS.Where(r => r.active == true).Select(r => new ReportModel()
            {
                ReportId = r.report_auto,
                ReportName = r.report_display_name
            }).ToListAsync();
        }

        /// <summary>
        /// returns all quote report styles for dealers to choose from
        /// logic is basically same as above
        /// </summary>
        /// <returns></returns>
        public async Task<List<ReportModel>> GetAllQuoteReportStyles()
        {
            return await _undercarriageContext.LU_QuoteReports.Select(s => new ReportModel { ReportId = s.Id, ReportName = s.QuoteReportDesc }).ToListAsync();
        }

        /// <summary>
        /// Get a list of all possible colour schemes. Used when creating a new branded style for a dealership. The user can
        /// select the colour scheme they want the branded style to use. 
        /// </summary>
        /// <returns></returns>
        public async Task<List<ColourSchemeModel>> GetAllColourSchemes()
        {
            return await _undercarriageContext.ApplicationStyle.Select(s => new ColourSchemeModel()
            {
                Id = s.Id,
                Name = s.Name
            }).ToListAsync();
        }

        public async Task<NewDealershipModel> GetDealershipDetails(int dealershipId)
        {
            var reports = await _undercarriageContext.DealershipReports.Where(r => r.DealershipId == dealershipId).Select(r => r.ReportId).ToListAsync();
            List<ReportModel> reportStyles = new List<ReportModel>();
            reports.ForEach(r =>
            {
                var report = _undercarriageContext.FLUID_REPORT_LU_REPORTS.Where(x => x.report_auto == r).FirstOrDefault();
                reportStyles.Add(new ReportModel()
                {
                    ReportId = report.report_auto,
                    ReportName = report.report_display_name
                });
            });


          

            var quoteReports = await _undercarriageContext.DealershipQuoteReports.Where(d => d.DealershipId == dealershipId).Select(r => r.QuoteReportId).ToListAsync();
            var quoteReportStyles = new List<ReportModel>();

            foreach (var item in quoteReports)
            {
                var repo = _undercarriageContext.LU_QuoteReports.FirstOrDefault(r => r.Id == item);
                quoteReportStyles.Add(new ReportModel {
                    ReportId = repo.Id,
                    ReportName = repo.QuoteReportDesc

                });
            }




            var dealershipName = await _context.Dealerships.Where(d => d.DealershipId == dealershipId).Select(d => d.Name).FirstOrDefaultAsync();
            var result = new NewDealershipModel()
            {
                DealershipName = dealershipName,
                ReportStyles = reportStyles,
                QuoteReportStyles = quoteReportStyles
            };

            return result;
        }

        public async Task<Tuple<int, string>> UpdateDealership(UpdateDealershipModel dealership)
        {
            var existingDealerships = this._context.Dealerships.Where(d => d.Name == dealership.DealershipName && d.DealershipId != dealership.DealershipId).FirstOrDefault();
            if (existingDealerships != null)
                return Tuple.Create(-1, "A dealership with this name already exists. ");

            var dealerEntity = await _context.Dealerships.FindAsync(dealership.DealershipId);
            if (dealerEntity == null)
                return Tuple.Create(-1, "No dealership exists with this Id to update. ");

            dealerEntity.Name = dealership.DealershipName;
            var reports = await _undercarriageContext.DealershipReports.Where(r => r.DealershipId == dealership.DealershipId).ToListAsync();
            _undercarriageContext.DealershipReports.RemoveRange(reports);

            foreach (var report in dealership.ReportStyles)
            {
                _undercarriageContext.DealershipReports.Add(new DealershipReports()
                {
                    DealershipId = dealership.DealershipId,
                    ReportId = report.ReportId
                });
            }
            foreach (var report in dealership.QuoteReportStyles)
            {
                _undercarriageContext.DealershipQuoteReports.Add(new DealershipQuoteReports()
                {
                    DealershipId = dealership.DealershipId,
                    QuoteReportId = report.ReportId
                });
            }

            try
            {
                await _context.SaveChangesAsync();
                await _undercarriageContext.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create(-1, e.Message);
            }

            return Tuple.Create(dealership.DealershipId, "Successfully updated dealership. ");
        }

        public async Task<Tuple<bool, string>> UpdateBrandedStyle(BrandedStyleDetailsModel style)
        {
            var styleEntity = await _undercarriageContext.DealershipBranding.Where(b => b.Id == style.Id).FirstOrDefaultAsync();
            string currentUcUriHost = "http://" + styleEntity.UCUIHost + "/account/signInCallback";
            string currentGetUriHost = "http://"+ styleEntity.GETUIHost + "/account/signInCallback";
            if (styleEntity == null)
                return Tuple.Create(false, "No style exists with this Id to update. ");

            var clientRedirectUriUc = await _undercarriageContext.ClientRedirectUris.Where(c => c.Uri == currentUcUriHost && c.Client_Id == 1).FirstOrDefaultAsync();
            var clientRedirectUriGet = await _undercarriageContext.ClientRedirectUris.Where(c => c.Uri == currentGetUriHost && c.Client_Id == 2).FirstOrDefaultAsync();

            if (clientRedirectUriGet == null || clientRedirectUriUc == null)
                return Tuple.Create(false, "Could not find client redirect uri's to update. Contact TrackTreads support. ");

            string[] LogoArr = style.Logo.Split(',');
            string dealershipLogo = "";
            if (LogoArr.Length > 1)
                dealershipLogo = LogoArr[1];

            styleEntity.ApplicationStyleId = style.ColourSchemeId;
            styleEntity.DealershipId = style.DealershipId;
            if (dealershipLogo != "")
                styleEntity.DealershipLogo = Convert.FromBase64String(dealershipLogo);
            styleEntity.GETHost = style.GetUrl;
            styleEntity.HelpCentreUrl = "";
            styleEntity.GETUIHost = style.GetUiUrl;
            styleEntity.IdentityHost = style.IdentityServerUrl;
            styleEntity.LogoutRedirectUrl = style.LogoutRedirectUrl;
            styleEntity.UCHost = style.UndercarriageUrl;
            styleEntity.UCUIHost = style.UndercarriageUiUrl;
            styleEntity.Name = style.Name;

            // Need to update ClientRedirectUris table records so that the identity application knows that this application url is authorized
            clientRedirectUriUc.Uri = "http://" + style.UndercarriageUiUrl + "/account/signInCallback";
            clientRedirectUriGet.Uri = "http://" + style.GetUiUrl + "/account/signInCallback";

            try
            {
                await _undercarriageContext.SaveChangesAsync();
            }
            catch
            {
                return Tuple.Create(false, "Failed to update branded style. ");
            }
            return Tuple.Create(true, "Branded style updated successfully. ");
        }

        public async Task<Tuple<bool, string>> AddNewBrandedStyle(BrandedStyleDetailsModel style)
        {
            string[] LogoArr = style.Logo.Split(',');
            string dealershipLogo = "";
            if (LogoArr.Length > 1)
                dealershipLogo = LogoArr[1];
            //
            _undercarriageContext.DealershipBranding.Add(new DealershipBranding()
            {
                ApplicationStyleId = style.ColourSchemeId,
                DealershipId = style.DealershipId,
                DealershipLogo = Convert.FromBase64String(dealershipLogo),
                GETHost = style.GetUrl,
                HelpCentreUrl = "",
                GETUIHost = style.GetUiUrl,
                IdentityHost = style.IdentityServerUrl,
                LogoutRedirectUrl = style.LogoutRedirectUrl,
                UCHost = style.UndercarriageUrl,
                UCUIHost = style.UndercarriageUiUrl,
                Name = style.Name
            });

            _undercarriageContext.ClientRedirectUris.Add(new ClientRedirectUri()
            {
                Client_Id = 1, //Undercarriage UI Client
                Uri = "http://" + style.UndercarriageUiUrl + "/account/signInCallback"
            });

            _undercarriageContext.ClientRedirectUris.Add(new ClientRedirectUri()
            {
                Client_Id = 2, //GET UI Client
                Uri = "http://" + style.GetUiUrl + "/account/signInCallback"
            });

            try
            {
                await _undercarriageContext.SaveChangesAsync();
            } catch
            {
                return Tuple.Create(false, "Failed to create new branded style. ");
            }
            return Tuple.Create(true, "Branded style created successfully. ");
        }

        public int AddNewDealership(NewDealershipModel newDealership, long createdUserId)
        {
            var existingDealerships =  this._context.Dealerships.Where(d => d.Name == newDealership.DealershipName).FirstOrDefault();
            if (existingDealerships != null)
                return -1;

            var newDealershipEntity = new Dealership()
            {
                Name = newDealership.DealershipName,
                Owner = createdUserId
            };
            _context.Dealerships.Add(newDealershipEntity);
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return -1;
            }

            
            
            foreach(var report in newDealership.ReportStyles)
            {
                _undercarriageContext.DealershipReports.Add(new DealershipReports()
                {
                    DealershipId = newDealershipEntity.DealershipId,
                    ReportId = report.ReportId
                });
            }

          

            foreach (var qReport in newDealership.QuoteReportStyles)
            {
                _undercarriageContext.DealershipQuoteReports.Add(new DealershipQuoteReports()
                {
                    DealershipId = newDealershipEntity.DealershipId,
                    QuoteReportId = qReport.ReportId,
                });
            }


            try {
                _undercarriageContext.SaveChanges();
            } catch(Exception e)
            {
                return -1;
            }
            new Core.Domain.UserAccessDomain.DealerGroupAccess(new DAL.SharedContext(), (int)createdUserId).AddDealerToDealerGroup(newDealership.DealerGroupId, newDealershipEntity.DealershipId);
            return newDealershipEntity.DealershipId;
        }

        /// <summary>
        /// Returns some general details of dealerships the given user id has access to 
        /// </summary>
        /// <param name="userId">User ID used to determine which dealerships to return. </param>
        /// <returns></returns>
        public async Task<List<DealershipOverviewModel>> GetAllDealershipsForUser(long userId)
        {
            List<DealershipOverviewModel> returnList = new List<DealershipOverviewModel>();
            List<long> dealershipIds = new List<long>();
            var userAccess = await _context.UserAccessMaps.Where(m => m.user_auto == userId).ToListAsync();

            var dealers = new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleDealers().Select(m=> new DealershipOverviewModel { DealershipId = m.DealershipId, DealershipName = m.Name }).ToList();
            foreach (var dealer in dealers) {
                dealer.BrandedStyles = _undercarriageContext.DealershipBranding.Where(m=> m.DealershipId == dealer.DealershipId).Select(b => new BrandedStyleModel()
                {
                    Id = b.Id,
                    Name = b.Name
                }).ToList();
            }

            return dealers.OrderBy(m=> m.DealershipName).ToList();
        }
    }
}
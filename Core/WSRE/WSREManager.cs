using BLL.Administration;
using BLL.Core.WSRE.Models;
using BLL.Extensions;
using BLL.GETCore.Classes;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.Domain
{
    public class WSREManager
    {
        private UndercarriageContext _context;

        public WSREManager(UndercarriageContext context)
        {
            this._context = context;
        }

        /// <summary>
        /// Returns the overview data displayed at the top of the WSRE inspection page. 
        /// </summary>
        /// <param name="wsreId">The WSRE id to return data for</param>
        /// <returns>Returns data or null if the id doesn't exist. </returns>
        public async Task<WsreOverviewModel> GetRepairEstimateOverview(int wsreId)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            return new WsreOverviewModel()
            {
                CustomerName = w.Jobsite.Customer.cust_name,
                CustomerReference = w.CustomerReference,
                InspectionDate = w.Date,
                JobNumber = w.JobNumber,
                JobsiteName = w.Jobsite.site_name,
                LifeLived = w.SystemLife,
                Make = w.System.Make.makedesc,
                Model = w.System.Model.modeldesc,
                OldJobNumber = w.OldTagNumber,
                ReportPrepared = false,
                SerialNumber = w.System.Serialno
            };
        }

        /// <summary>
        /// Returns the username of the inspector who performed the WSRE Inspection.
        /// </summary>
        /// <param name="wsreId">The WSRE id to return data for</param>
        /// <returns>Returns data or null if the id doesn't exist. </returns>
        public async Task<string> GetRepairEstimateInspector(int wsreId)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            return w.Inspector.username;
        }

        public async Task<List<WsreComponentTab>> GetComponentTabData(int wsreId, MeasurementType uom)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            return w.ComponentRecords.Select(c => new WsreComponentTab()
            {
                Id = c.Id,
                Brand = c.Component.Make == null ? "Unknown" : c.Component.Make.makedesc,
                Cmu = c.Cmu,
                Comment = c.Comment,
                Component = c.Component.LU_COMPART.LU_COMPART_TYPE.comparttype + " - " + c.Component.LU_COMPART.compartid,
                Measurement = uom == MeasurementType.Milimeter ? c.Measurement : c.Measurement.MilimeterToInch(),
                Photos = c.Photos.Where(p => p.Deleted == false).Select(p => new WsreComponentPhoto()
                {
                    Id = p.Id,
                    ImageData = Convert.ToBase64String(p.Data),
                    Title = p.Title
                }).ToList(),
                Recommendations = c.Recommendations.Select(r => r.Recommendation.Description).ToList(),
                RemainingLife = c.RemainingLife,
                WornPercentage = c.WornPercentage
            }).ToList();
        }

        public async Task<WsrePhoto> GetPhoto(int photoId, string photoType)
        {
            switch(photoType)
            {
                case "initial":
                    var initialPhoto = await _context.WSREInitialImage.FindAsync(photoId);
                    if (initialPhoto == null)
                        return null;
                    return new WsrePhoto()
                    {
                        Comment = initialPhoto.Comment,
                        ImageData = Convert.ToBase64String(initialPhoto.Data),
                        Title = initialPhoto.Title
                    };
                case "component":
                    var componentPhoto = await _context.WSREComponentImage.FindAsync(photoId);
                    if (componentPhoto == null)
                        return null;
                    return new WsrePhoto()
                    {
                        Comment = componentPhoto.Comment,
                        ImageData = Convert.ToBase64String(componentPhoto.Data),
                        Title = componentPhoto.Title
                    };
                case "cracktest":
                    var crackTestPhoto = await _context.WSRECrackTestImage.FindAsync(photoId);
                    if (crackTestPhoto == null)
                        return null;
                    return new WsrePhoto()
                    {
                        Comment = crackTestPhoto.Comment,
                        ImageData = Convert.ToBase64String(crackTestPhoto.Data),
                        Title = crackTestPhoto.Title
                    };
                case "diptest":
                    var dipTestPhoto = await _context.WSREDipTestImage.FindAsync(photoId);
                    if (dipTestPhoto == null)
                        return null;
                    return new WsrePhoto()
                    {
                        Comment = dipTestPhoto.Comment,
                        ImageData = Convert.ToBase64String(dipTestPhoto.Data),
                        Title = dipTestPhoto.Title
                    };
            }

            return null;
        }

        public async Task<bool> DeletePhoto(int photoId, string photoType)
        {
            switch (photoType)
            {
                case "initial":
                    var initialPhoto = await _context.WSREInitialImage.FindAsync(photoId);
                    if (initialPhoto == null)
                        return false;
                    initialPhoto.Deleted = true;
                    try
                    {
                        await _context.SaveChangesAsync();
                        return true;
                    } catch
                    {
                        return false;
                    }
                case "component":
                    var componentPhoto = await _context.WSREComponentImage.FindAsync(photoId);
                    if (componentPhoto == null)
                        return false;
                    componentPhoto.Deleted = true;
                    try
                    {
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case "cracktest":
                    var crackTestPhoto = await _context.WSRECrackTestImage.FindAsync(photoId);
                    if (crackTestPhoto == null)
                        return false;
                    crackTestPhoto.Deleted = true;
                    try
                    {
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                case "diptest":
                    var dipTestPhoto = await _context.WSREDipTestImage.FindAsync(photoId);
                    if (dipTestPhoto == null)
                        return false;
                    dipTestPhoto.Deleted = true;
                    try
                    {
                        await _context.SaveChangesAsync();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
            }

            return false;
        }

        public async Task<List<WsreArrivalPhotoOverview>> GetArrivalPhotosOverview(int wsreId)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            return w.InitialPhotos.Where(p => p.Deleted == false).Select(p => new WsreArrivalPhotoOverview()
            {
                Id = p.Id,
                ImageData = Convert.ToBase64String(p.Data),
                Type = (WsreInitialImageType)p.ImageTypeId,
                Title = p.Title,
                Comment = p.Comment
            }).ToList();
        }

        public async Task<List<WsreDipTest>> GetDipTests(int wsreId)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            return w.DipTests.Select(d => new WsreDipTest()
            {
                Id = d.Id,
                Comment = d.Comment,
                Condition = d.Condition.Description,
                Level = d.Measurement,
                Colour = d.Condition.Colour,
                Number = d.Number,
                Recommendation = d.Recommendation,
                Photos = d.Photos.Where(p => p.Deleted == false).Select(p => new WsreComponentPhoto()
                {
                    Id = p.Id,
                    ImageData = Convert.ToBase64String(p.Data),
                    Title = p.Title
                }).ToList()
            }).OrderBy(d => d.Number).ToList();
        }

        public async Task<WsreCrackTestTab> GetCrackTestTab(int wsreId)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            var ct = w.CrackTest.FirstOrDefault();
            if (ct == null)
                return null;

            var photos = new List<WsreComponentPhoto>();
            photos.AddRange(ct.Photos.Where(p => p.Deleted == false).Select(p => new WsreComponentPhoto()
            {
                Id = p.Id,
                ImageData = Convert.ToBase64String(p.Data),
                Title = p.Title
            }).ToList());

            return new WsreCrackTestTab()
            {
                Comment = ct.Comment,
                TestPassed = ct.TestPassed,
                Photos = photos
            };
        }

        public async Task<WsreSummaryModel> GetRepairEstimateSummary(int wsreId)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            return new WsreSummaryModel()
            {
                OverallComment = w.OverallComment,
                OverallRecommendation = w.OverallRecommendation,
                OverallEval = w.OverallEval,
                Components = w.ComponentRecords.Select(c => new ComponentSummaryModel()
                {
                    Comment = c.Comment,
                    Recommendations = c.Recommendations.Select(r => r.Recommendation.Description).ToList(),
                    Type = c.Component.LU_COMPART.LU_COMPART_TYPE.comparttype,
                    WornPercentage = c.WornPercentage
                }).ToList(),
                CrackTest = w.CrackTest.Select(c => new CrackTestSummaryModel()
                {
                    Comment = c.Comment,
                    Passed = c.TestPassed
                }).FirstOrDefault(),
                DipTests = w.DipTests.Where(c => c.Condition.Description != "Good").Select(c => new DipTestSummaryModel()
                {
                    Colour = c.Condition.Colour,
                    Comment = c.Comment,
                    Number = c.Number,
                    Problem = c.Condition.Description
                }).ToList()
            };
        }

        public async Task<MeasurementType> GetUserUOM(long userId)
        {
            var user = await _context.USER_TABLE.FindAsync(userId);
            if (user == null)
                return MeasurementType.Milimeter;
            if (user.track_uom == "mm") return MeasurementType.Milimeter;
            else if (user.track_uom == "inch") return MeasurementType.Inch;
            return MeasurementType.Milimeter;
        }

        public async Task<Tuple<bool, string>> UpdateOverallSummaryComment(int wsreId, string comment)
        {
            var record = await _context.WSRE.FindAsync(wsreId);
            if (record == null)
                return Tuple.Create(false, "Couldn't find an inspection with this Id. ");

            record.OverallComment = comment;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                return Tuple.Create(false, "Failed to update overall summary comment. ");
            }
            return Tuple.Create(true, "Overall summary comment updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateOverallEval(int wsreId, string eval)
        {
            var record = await _context.WSRE.FindAsync(wsreId);
            if (record == null)
                return Tuple.Create(false, "Couldn't find an inspection with this Id. ");

            record.OverallEval = eval;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                return Tuple.Create(false, "Failed to update overall eval. ");
            }
            return Tuple.Create(true, "Overall eval updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateOverallSummaryRecommendation(int wsreId, string recommendation)
        {
            var record = await _context.WSRE.FindAsync(wsreId);
            if (record == null)
                return Tuple.Create(false, "Couldn't find an inspection with this Id. ");

            record.OverallRecommendation = recommendation;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                return Tuple.Create(false, "Failed to update overall summary recommendation. ");
            }
            return Tuple.Create(true, "Overall summary recommendation updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateComponentRecordComment(int id, string newComment)
        {
            var record = await _context.WSREComponentRecord.FindAsync(id);
            if(record == null)
                return Tuple.Create(false, "Couldn't find a component record with this id. ");

            record.Comment = newComment;
            try
            {
                await _context.SaveChangesAsync();
            } catch
            {
                return Tuple.Create(false, "Failed to save component comment. ");
            }
            return Tuple.Create(true, "Comment updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateCrackTestEval(int wsreId, bool testPassed)
        {
            var wsre = await _context.WSRE.FindAsync(wsreId);
            if (wsre == null)
                return Tuple.Create(false, "Couldn't find an inspection with this Id. ");

            var crackTest = wsre.CrackTest.FirstOrDefault();
            if(crackTest == null)
                return Tuple.Create(false, "This inspection has no crack test record. Contact TrackTreads support for help. ");

            crackTest.TestPassed = testPassed;
            try
            {
                await _context.SaveChangesAsync();
            } catch (Exception e) {
                return Tuple.Create(false, "Failed to update crack test. ");
            }
            return Tuple.Create(true, "The crack test eval was updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateDipTestLevel(int id, int level)
        {
            var dipTest = await _context.WSREDipTest.FindAsync(id);
            if (dipTest == null)
                return Tuple.Create(false, "Couldn't find a dip test with this Id. ");

            dipTest.Measurement = level;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update dip test level. ");
            }
            return Tuple.Create(true, "The dip test level was updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateDipTestComment(int id, string newComment)
        {
            var dipTest = await _context.WSREDipTest.FindAsync(id);
            if (dipTest == null)
                return Tuple.Create(false, "Couldn't find a dip test with this Id. ");

            dipTest.Comment = newComment;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update dip test comment. ");
            }
            return Tuple.Create(true, "The dip test comment was updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateDipTestRecommendation(int id, string newRecommendation)
        {
            var dipTest = await _context.WSREDipTest.FindAsync(id);
            if (dipTest == null)
                return Tuple.Create(false, "Couldn't find a dip test with this Id. ");

            dipTest.Recommendation = newRecommendation;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update dip test recommendation. ");
            }
            return Tuple.Create(true, "The dip test recommendation was updated successfully. ");
        }

        public async Task<List<DipTestConditionType>> GetPossibleDipTestConditionTypes()
        {
            return await _context.WSREDipTestCondition.Select(d => new DipTestConditionType()
            {
               Id = d.Id,
               Description = d.Description
            }).ToListAsync();
        }

        public async Task<Tuple<bool, string>> UpdateDipTestCondition(int id, int newConditionId)
        {
            var dipTest = await _context.WSREDipTest.FindAsync(id);
            if (dipTest == null)
                return Tuple.Create(false, "Couldn't find a dip test with this Id. ");

            dipTest.ConditionId = newConditionId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update dip test condition. Does this condition Id exist? ");
            }
            return Tuple.Create(true, "The dip test condition was updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateCrackTestComment(int wsreId, string newComment)
        {
            var wsre = await _context.WSRE.FindAsync(wsreId);
            if (wsre == null)
                return Tuple.Create(false, "Couldn't find an inspection with this Id. ");

            var crackTest = wsre.CrackTest.FirstOrDefault();
            if (crackTest == null)
                return Tuple.Create(false, "This inspection has no crack test record. Contact TrackTreads support for help. ");

            crackTest.Comment = newComment;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update crack test. ");
            }
            return Tuple.Create(true, "The crack test comment was updated successfully. ");
        }

        public async Task<Tuple<bool, string>> UpdateComponentRecordMeasurement(int id, decimal newMeasurement, MeasurementType uom)
        {
            var record = await _context.WSREComponentRecord.FindAsync(id);
            if (record == null)
                return Tuple.Create(false, "Couldn't find a component record with this id. ");

            var component = new BLL.Core.Domain.Component(new UndercarriageContext(), Convert.ToInt32(record.ComponentId));
            var newWornPercentage = component.CalcWornPercentage(uom == MeasurementType.Milimeter ? newMeasurement.ConvertMMToInch() : newMeasurement, record.MeasurementToolId, InspectionImpact.High);
            record.Measurement = uom == MeasurementType.Inch ? newMeasurement.InchToMilimeter() : newMeasurement;
            record.WornPercentage = newWornPercentage;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                return Tuple.Create(false, "Failed to save component measurement. ");
            }
            return Tuple.Create(true, "Measurement and % updated successfully. ");
        }

        /// <summary>
        /// Checks if the given user id is allowed to view the given inspection. 
        /// </summary>
        /// <param name="userId">The user Id to check the access for</param>
        /// <param name="inspectionId">The inspection Id to check the access for</param>
        /// <returns>True if user is allowed to access the inspection</returns>
        public async Task<bool> VerifyUserAccessToInspection(long userId, int inspectionId)
        {
            var wsre = await _context.WSRE.FindAsync(inspectionId);
            if (wsre == null)
                return false;
            var jobsiteId = wsre.JobsiteId;
            return AuthorizeUserAccess.verifyAccessToJobsite(userId, jobsiteId, false);
        }

        public async Task<bool> CheckUserCanEdit(long userId, int inspectionId)
        {
            var wsre = await _context.WSRE.FindAsync(inspectionId);
            var user = await _context.USER_TABLE.FindAsync(userId);
            if (wsre == null || user == null)
                return false;
            var jobsiteId = wsre.JobsiteId;
            bool hasAccess = AuthorizeUserAccess.verifyAccessToJobsite(userId, jobsiteId, false);
            if (!hasAccess)
                return false;
            if(user.JobRoles.Select(j => j.USER_GROUP.groupname.ToLower()).Contains("inspector")
                || user.JobRoles.Select(j => j.USER_GROUP.groupname.ToLower()).Contains("interpreter")
                || user.JobRoles.Select(j => j.USER_GROUP.groupname.ToLower()).Contains("administrator")
                || user.JobRoles.Select(j => j.USER_GROUP.groupname.ToLower()).Contains("super user"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a list of workshop repair estimates, used to populate the table on the inspection
        /// search page. 
        /// </summary>
        /// <param name="userId">User ID requesting the list to filter by access. </param>
        /// <param name="searchRequest">Data we want to filter the search results by. </param>
        /// <returns>List of workshop repair estimates to be displayed in a table. </returns>
        public List<WorkshopRepairEstimateSearchResultModel> GetWorkshopEstimates(long userId, WorkshopRepairEstimateSearchRequestModel searchRequest)
        {
            var access = new CustomerManagement(); 
            var customerAccessIds = access.getListOfActiveCustomersForLoggedInUser(userId).Select(c => c.customerId).ToList();

            return _context.WSRE
                .Where(w => w.Jobsite.Customer.cust_name.Contains(searchRequest.CustomerName))
                .Where(w => w.Jobsite.site_name.Contains(searchRequest.JobsiteName))
                .Where(w => w.Inspector.username.Contains(searchRequest.InspectorName))
                .Where(w => w.System.Serialno.Contains(searchRequest.SerialNumber))
                .Where(w => w.JobNumber.Contains(searchRequest.JobNumber))
                .Where(w => w.CustomerReference.Contains(searchRequest.CustomerReference))
                .Where(w => w.Status.Description.Contains(searchRequest.Status))
                .Where(w => w.Date > searchRequest.StartDate && w.Date < searchRequest.EndDate)
                .Where(w => customerAccessIds.Contains(w.Jobsite.customer_auto))
                .Select(w => new WorkshopRepairEstimateSearchResultModel()
                {
                    CustomerName = w.Jobsite.Customer.cust_name,
                    CustomerReference = w.CustomerReference,
                    InspectionDate = w.Date,
                    InspectorName = w.Inspector.username,
                    JobNumber = w.JobNumber,
                    JobsiteName = w.Jobsite.site_name,
                    SerialNumber = w.System.Serialno,
                    Id = w.Id,
                    Status = w.Status.Description
                }).OrderByDescending(l => l.InspectionDate).ToList();
        }

        public async Task<List<WsreComponentTabForReport>> GetComponentTabDataForReport(int wsreId, MeasurementType uom)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            return w.ComponentRecords.Select(c => new WsreComponentTabForReport()
            {
                Id = c.Id,
                Brand = c.Component.Make == null ? "Unknown" : c.Component.Make.makedesc,
                Cmu = c.Cmu,
                Comment = c.Comment,
                Component = c.Component.LU_COMPART.LU_COMPART_TYPE.comparttype + " - " + c.Component.LU_COMPART.compartid,
                Measurement = uom == MeasurementType.Milimeter ? c.Measurement : c.Measurement.MilimeterToInch(),
                Photos = c.Photos.Where(p => p.Deleted == false).Select(p => new WsrePhoto()
                {
                    ImageData = Convert.ToBase64String(p.Data),
                    Title = p.Title,
                    Comment = p.Comment
                }).ToList(),
                Recommendations = c.Recommendations.Select(r => r.Recommendation.Description).ToList(),
                RemainingLife = c.RemainingLife,
                WornPercentage = c.WornPercentage,
                Tool = c.MeasurementTool.tool_name,
                ComponentImage = Convert.ToBase64String(_context.COMPART_ATTACH_FILESTREAM
                        .Where(c2 => c2.comparttype_auto == c.Component.LU_COMPART.comparttype_auto)
                        .Where(c2 => c2.compart_attach_type_auto == 5)
                        .Select(c2 => c2.attachment).FirstOrDefault())
        }).ToList();
        }

        public async Task<WsreCrackTestTabForReport> GetCrackTestTabForReport(int wsreId)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            var ct = w.CrackTest.FirstOrDefault();
            if (ct == null)
                return null;

            var photos = new List<WsrePhoto>();
            photos.AddRange(ct.Photos.Where(p => p.Deleted == false).Select(p => new WsrePhoto()
            {
                ImageData = Convert.ToBase64String(p.Data),
                Title = p.Title,
                Comment = p.Comment
            }).ToList());

            return new WsreCrackTestTabForReport()
            {
                Comment = ct.Comment,
                TestPassed = ct.TestPassed,
                Photos = photos
            };
        }

        public async Task<List<WsreDipTestForReport>> GetDipTestsForReport(int wsreId)
        {
            var w = await _context.WSRE.FindAsync(wsreId);
            if (w == null)
                return null;

            return w.DipTests.Select(d => new WsreDipTestForReport()
            {
                Id = d.Id,
                Comment = d.Comment,
                Condition = d.Condition.Description,
                Level = d.Measurement,
                Colour = d.Condition.Colour,
                Number = d.Number,
                Recommendation = d.Recommendation,
                Photos = d.Photos.Where(p => p.Deleted == false).Select(p => new WsrePhoto()
                {
                    ImageData = Convert.ToBase64String(p.Data),
                    Title = p.Title,
                    Comment = p.Comment
                }).ToList()
            }).OrderBy(d => d.Number).ToList();
        }
    }
}
//using BLL.Controllers;
using BLL.GETInterfaces;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core;
using System.Linq;
using System.Web;
using BLL.GETCore.Classes.ViewModel;
using System.Threading.Tasks;
using BLL.Extensions;

namespace BLL.GETCore.Classes
{
    public class UserManagement : IUserManager
    {
        private GETContext _context;

        public UserManagement()
        {
            this._context = new GETContext();
        }

        public async Task<Tuple<bool, UserViewModel>> GetUser(int userId, System.Security.Principal.IPrincipal User)
        {
            var _access = new BLL.Core.Domain.UserAccess(new SharedContext(), User);
            if (_access.getAccessibleUsers().Where(m=> m.user_auto == userId).Count() == 0)
                return Tuple.Create<bool, UserViewModel>(false, null);

            USER_TABLE user = await _context.USER_TABLE.FindAsync(userId);

            if (user == null)
                return Tuple.Create<bool, UserViewModel>(false, null);

            UserViewModel returnUser = new UserViewModel()
            {
                Email = user.email,
                Id = (int)user.user_auto,
                Name = user.username,
                AccessLevel = (UserAccessTypes)(int)_access.getHighestCategoryAccess()
            };

            return Tuple.Create(true,returnUser);
        }

        public GETResponseMessage updateUserEnabledStatus(long userId, bool enabled)
        {
            using (var context = new SharedContext())
            {
                var userAccount = context.USER_TABLE.Find(userId);
                if (userAccount == null)
                    return new GETResponseMessage(ResponseTypes.Failed, "User ID does not exist. ");
                userAccount.suspended = !enabled;
                try
                {
                    context.SaveChanges();
                    return new GETResponseMessage(ResponseTypes.Success, "User's enabled status has been updated. ");
                }
                catch
                {
                    return new GETResponseMessage(ResponseTypes.Failed, "Unable to save to database. ");
                }
            }
        }



        public GETResponseMessage updateExistingUserAccount(long userId, string username, string email, int accessLevel)
        {
            UserTeam usersTeam = AuthorizeUserAccess.getUserTeam(userId);

            using (var context = new SharedContext())
            {
                var userAccount = context.USER_TABLE.Find(userId);
                
                if (userAccount == null || username == "" || email == "")
                    return new GETResponseMessage(ResponseTypes.InvalidInputs, "Invalid user details. ");

                var aspUserAccount = context.AspNetUsers.Find(userAccount.AspNetUserId);
                if (aspUserAccount == null)
                    return new GETResponseMessage(ResponseTypes.InvalidInputs, "Internal error occurred!. AspUser not found!");

                if (email != userAccount.email)
                {
                    if (!checkEmailIsUnique(email) || !checkAspEmailIsUnique(email))
                        return new GETResponseMessage(ResponseTypes.InvalidInputs, "Email address must be unique. ");
                }

                if (username != userAccount.username)
                {
                    if (!checkUsernameIsUnique(username) || !checkAspUsernameIsUnique(username))
                        return new GETResponseMessage(ResponseTypes.InvalidInputs, "Username must be unique. ");
                }



                // Ensure that user is updating the access level correctly. 
                // (A user who is part of a dealership must have a dealership access level). 
                bool accessLevelAllowed = false;
                if (usersTeam.teamType == UserAccountType.Dealership && (accessLevel == (int)UserAccessTypes.DealershipAdministrator
                        || accessLevel == (int)UserAccessTypes.DealershipUser))
                {
                    accessLevelAllowed = true;
                }
                else if (usersTeam.teamType == UserAccountType.Customer && (accessLevel == (int)UserAccessTypes.CustomerAdministrator
                        || accessLevel == (int)UserAccessTypes.CustomerUser))
                {
                    accessLevelAllowed = true;
                }
                else if (accessLevel == 0) // Level 0 means don't change the access level. 
                    accessLevelAllowed = true;

                if (!accessLevelAllowed)
                {
                    return new GETResponseMessage(ResponseTypes.InvalidInputs, "You are not allowed to give this user account this access level. ");
                }
                userAccount.username = username;
                userAccount.userid = username;
                userAccount.email = email;

                aspUserAccount.UserName = username;
                aspUserAccount.Email = email;

                UserAccessMaps userMap;

                if (usersTeam.teamType == UserAccountType.Dealership)
                {
                    userMap = context.UserAccessMaps.FirstOrDefault(m => m.user_auto == userId && m.DealershipId == usersTeam.teamId);
                }
                else
                {
                    userMap = context.UserAccessMaps.FirstOrDefault(m => m.user_auto == userId && m.customer_auto == usersTeam.teamId);
                }

                if (userMap == null)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, "Failed to update the users access level record. Couldn't find it in the database. ");
                }

                // If access level passed in is 0, we wont change their access. 
                if (accessLevel != 0)
                {
                    // If the user is getting changed to a dealership user, and wasn't already
                    // we need to remove their access to all customers
                    if(accessLevel == (int)UserAccessTypes.DealershipUser && userMap.AccessLevelTypeId != (int)UserAccessTypes.DealershipUser)
                    {
                        var list = context.USER_CRSF_CUST_EQUIP.Where(u => u.user_auto == userId).ToList();
                        context.USER_CRSF_CUST_EQUIP.RemoveRange(list);

                        var list2 = context.UserAccessMaps.Where(m => m.user_auto == userId && m.customer_auto != null).ToList();
                        context.UserAccessMaps.RemoveRange(list2);
                    } else if (accessLevel == (int)UserAccessTypes.DealershipAdministrator && userMap.AccessLevelTypeId != (int)UserAccessTypes.DealershipAdministrator)
                    {
                        long[] customerIds = context.CUSTOMER.Where(c => c.DealershipId == usersTeam.teamId).Select(c => c.customer_auto).ToArray();
                        foreach (long customerId in customerIds)
                        {
                            USER_CRSF_CUST_EQUIP accessRecord = new USER_CRSF_CUST_EQUIP()
                            {
                                user_auto = userId,
                                customer_auto = customerId,
                                level_type = 1,
                                modified_user = "AUTO INSERT USER CHANGED TO DEALERSHIP ADMIN"
                            };
                            context.USER_CRSF_CUST_EQUIP.Add(accessRecord);
                        }
                    }
                    userMap.AccessLevelTypeId = accessLevel;
                }
                try
                {
                    context.SaveChanges();
                    return new GETResponseMessage(ResponseTypes.Success, "User account updated successfully. ");
                }
                catch (Exception e)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, "Failed to save. " + e.Message);
                }
            }
        }

        public bool checkUsernameIsUnique(string username)
        {
            int existingUsername;

            using (var context = new SharedContext())
            {
                existingUsername = context.USER_TABLE.Where(u => (u.username == username || u.userid == username)).Count();
            }
            if (existingUsername == 0)
                return true;
            else
                return false;
        }

        public bool checkAspUsernameIsUnique(string username)
        {
            int existingUsername;

            using (var context = new SharedContext())
            {
                existingUsername = context.AspNetUsers.Where(u => (u.UserName == username)).Count();
            }
            if (existingUsername == 0)
                return true;
            else
                return false;
        }

        public bool checkEmailIsUnique(string email)
        {
            int existingEmail;

            using (var context = new SharedContext())
            {
                existingEmail = context.USER_TABLE.Where(u => u.email == email).Count();
            }
            if (existingEmail == 0)
                return true;
            else
                return false;
        }

        public bool checkAspEmailIsUnique(string email)
        {
            int existingEmail;

            using (var context = new SharedContext())
            {
                existingEmail = context.AspNetUsers.Where(u => u.Email == email).Count();
            }
            if (existingEmail == 0)
                return true;
            else
                return false;
        }

        public GETResponseMessage createNewUserAccount(string username, string password, string email, int accessLevel, UserAccountType type, int teamId)
        {
            var newUser = new USER_TABLE();

            if (type == UserAccountType.Dealership && (accessLevel != (int)UserAccessTypes.DealershipAdministrator && accessLevel != (int)UserAccessTypes.DealershipUser))
            {
                return new GETResponseMessage(ResponseTypes.InvalidInputs, "Failed. The access level you attempted to give is not valid for a dealership. ");
            }
            else if (type == UserAccountType.Customer && (accessLevel != (int)UserAccessTypes.CustomerAdministrator && accessLevel != (int)UserAccessTypes.CustomerUser))
            {
                return new GETResponseMessage(ResponseTypes.InvalidInputs, "Failed. The access level you attempted to give is not valid for a customer. ");
            }
            else if (!checkUsernameIsUnique(username))
            {
                return new GETResponseMessage(ResponseTypes.InvalidInputs, "Failed: Username already exists. ");
            }
            else if (!checkEmailIsUnique(email))
            {
                return new GETResponseMessage(ResponseTypes.InvalidInputs, "Failed: Email already exists. ");
            }
            else if (username.Length < 1 || password.Length < 1 || email.Length < 1)
            {
                return new GETResponseMessage(ResponseTypes.InvalidInputs, "Failed: Invalid username, password, or email length. ");
            }
            else
            {
                // Creating user account 
                // GET related fields
                newUser.username = username;
                newUser.userid = username;
                newUser.passwd = password;
                newUser.email = email;
                newUser.language_auto = 1; // English
                newUser.currency_auto = 1; // AUD
                newUser.active = true;
                newUser.suspended = false;

                // Fields not related to GET, but that are currently required.
                newUser.internalemp = false;
                newUser.internalother = false;
                newUser.viewe = false;
                newUser.viewr = false;
                newUser.interpreter = false;
                newUser._protected = false;
                newUser.attach = false;
                newUser.print_copies = 0;
                newUser.sos = false;
                newUser.IsEquipmentEdit = false;

                if (type == UserAccountType.Customer)
                    newUser.customer_auto = teamId;

                using (var context = new SharedContext())
                {
                    context.USER_TABLE.Add(newUser);

                    try
                    {
                        context.SaveChanges();
                    }
                    catch
                    {
                        return new GETResponseMessage(ResponseTypes.Failed, "Failed: Unable to store user in database. ");
                    }

                    // Creating user access mapping entry
                    var newUserAccessMap = new UserAccessMaps();
                    newUserAccessMap.user_auto = newUser.user_auto;
                    if (type == UserAccountType.Dealership)
                    {
                        newUserAccessMap.DealershipId = teamId;
                    }
                    else
                    {
                        newUserAccessMap.customer_auto = teamId;
                    }
                    newUserAccessMap.AccessLevelTypeId = accessLevel;
                    context.UserAccessMaps.Add(newUserAccessMap);

                    try
                    {
                        context.SaveChanges();
                    }
                    catch
                    {
                        // IF this fails, user account is still created but with no access record. What should we do?
                        // Need to ask Mason. 
                        return new GETResponseMessage(ResponseTypes.Failed, "Failed: Unable to create access map record for the new user. ");
                    }
                }
            }
            // Insert module access records (required for old undercarriage application)
            var moduleAccess1 = new USER_MODULE_ACCESS()
            {
                moduleid = 0,
                user_auto = newUser.user_auto,
            };
            var moduleAccess2 = new USER_MODULE_ACCESS()
            {
                moduleid = 1,
                user_auto = newUser.user_auto,
            };
            var moduleAccess3 = new USER_MODULE_ACCESS()
            {
                moduleid = 3,
                user_auto = newUser.user_auto,
            };

            using (var context = new SharedContext())
            {
                context.USER_MODULE_ACCESS.Add(moduleAccess1);
                context.USER_MODULE_ACCESS.Add(moduleAccess2);
                context.USER_MODULE_ACCESS.Add(moduleAccess3);

                try
                {
                    context.SaveChanges();
                }
                catch
                {
                    return new GETResponseMessage(ResponseTypes.Failed, "Failed: User was created, but there was an error giving them module access. ");
                }
            }
            return new GETResponseMessage(ResponseTypes.Success, newUser.user_auto.ToString());
        }

        /// <summary>
        /// Returns a basic list of all user accounts for a given customer, if the userId passed in has
        /// the correct access level requirements to see that customer. 
        /// </summary>
        /// <param name="userId">The userId requesting to access the customer list</param>
        /// <param name="customerId">The customerId we want to get a list of user accounts for</param>
        /// <returns>A BasicUserDataSet object which contains basic user account information</returns>
        public IEnumerable<BasicUserDataSet> getUserAccountsForCustomer(long userId, long customerId)
        {
            List<BasicUserDataSet> userAccountReturn = new List<BasicUserDataSet>();
            bool hasAccessToCustomer = AuthorizeUserAccess.verifyAccessToCustomer(userId, customerId, true);
            bool isGlobalAdmin = AuthorizeUserAccess.isUserGlobalAdministrator(userId);
            if (hasAccessToCustomer)
            {
                using (var context = new DAL.SharedContext())
                {
                    userAccountReturn = context.UserAccessMaps
                                        .Where(um => um.customer_auto == customerId)
                                        .Where(um => um.AccessLevelTypeId == (int)UserAccessTypes.CustomerAdministrator || um.AccessLevelTypeId == (int)UserAccessTypes.CustomerUser)
                                        .OrderBy(um => um.USER_TABLE.suspended)
                                        .ThenBy(um => um.AccessLevelTypeId)
                                        .ThenBy(um => um.USER_TABLE.username)
                                        .Select(um => new BasicUserDataSet
                                        {
                                            UserId = um.USER_TABLE.user_auto,
                                            Username = um.USER_TABLE.username,
                                            Email = um.USER_TABLE.email,
                                            AccessLevel = um.AccessLevelTypeId,
                                            Disabled = um.USER_TABLE.suspended,
                                            Password = isGlobalAdmin ? um.USER_TABLE.passwd : "******"
                                        }).ToList();
                }
            }
            return userAccountReturn;
        }

        /// <summary>
        /// Returns a basic list of all user accounts for a given dealership id, if the userId passed in has
        /// the correct access level requirements to see that dealerships users. 
        /// </summary>
        /// <param name="userId">The userId requesting to access the customer list</param>
        /// <param name="customerId">The customerId we want to get a list of user accounts for</param>
        /// <returns>A BasicUserDataSet object which contains basic user account information</returns>
        public IEnumerable<BasicUserDataSet> getUserAccountsForDealership(long userId, int dealershipId)
        {
            // List of user accounts to be returned
            List<BasicUserDataSet> userAccountReturn = new List<BasicUserDataSet>();
            bool hasAccessToDealership = AuthorizeUserAccess.verifyAccessToDealership(userId, dealershipId, true);
            bool isGlobalAdmin = AuthorizeUserAccess.isUserGlobalAdministrator(userId);
            if (hasAccessToDealership)
            {
                using (var context = new DAL.SharedContext())
                {
                    userAccountReturn = context.UserAccessMaps
                                        .Where(um => um.DealershipId == dealershipId)
                                        .OrderBy(um => um.USER_TABLE.suspended)
                                        .ThenBy(um => um.AccessLevelTypeId)
                                        .ThenBy(um => um.USER_TABLE.username)
                                        .Select(um => new BasicUserDataSet
                                        {
                                            UserId = um.USER_TABLE.user_auto,
                                            Username = um.USER_TABLE.username,
                                            Email = um.USER_TABLE.email,
                                            AccessLevel = um.AccessLevelTypeId,
                                            Disabled = um.USER_TABLE.suspended,
                                            Password = isGlobalAdmin ? um.USER_TABLE.passwd : "******"
                                        }).ToList();
                }
            }

            return userAccountReturn;
        }

        /// <summary>
        /// Returns a list of customers and dealerships that the given user Id has access to view. 
        /// </summary>
        /// <param name="userId">ID of the user requesting the list of customers and dealerships</param>
        /// <returns>A dataset of customers and dealers that the given user id is allowed to view/edit</returns>
        public IEnumerable<CustomerDealershipDataSet> getCustomerAndDealershipList(long userId)
        {
            List<CustomerDealershipDataSet> returnData = new List<CustomerDealershipDataSet>();

            using (var context = new SharedContext())
            {
                List<UserAccessMaps> userAccessRecords = context.UserAccessMaps
                                                        .Where(m => m.user_auto == userId)
                                                        .OrderBy(m => m.AccessLevelTypeId)
                                                        .ToList();
                foreach (var accessRecord in userAccessRecords)
                {
                    if (accessRecord.AccessLevelTypeId == (int)UserAccessTypes.GlobalAdministrator)
                    {
                        returnData.AddRange(
                            context.Dealerships.OrderBy(d => d.Name).Select(d => new CustomerDealershipDataSet
                            {
                                id = d.DealershipId,
                                type = "dealership",
                                name = d.Name
                            })
                        );
                        returnData.AddRange(
                            context.CUSTOMER.OrderBy(c => c.cust_name).Select(c => new CustomerDealershipDataSet
                            {
                                id = c.customer_auto,
                                type = "customer",
                                name = c.cust_name
                            })
                        );
                        return returnData;
                    }
                    else if (accessRecord.AccessLevelTypeId == (int)UserAccessTypes.DealershipAdministrator)
                    {
                        returnData.AddRange(
                            context.Dealerships.OrderBy(d => d.Name).Where(d => d.DealershipId == accessRecord.DealershipId).Select(d => new CustomerDealershipDataSet
                            {
                                id = d.DealershipId,
                                type = "dealership",
                                name = d.Name
                            })
                        );
                        returnData.AddRange(
                            context.CUSTOMER.OrderBy(c => c.cust_name).Where(c => c.DealershipId == accessRecord.DealershipId)
                            .Select(c => new CustomerDealershipDataSet
                            {
                                id = c.customer_auto,
                                type = "customer",
                                name = c.cust_name
                            })
                        );
                    }
                    else if (accessRecord.AccessLevelTypeId == (int)UserAccessTypes.CustomerAdministrator)
                    {
                        returnData.Add(
                            context.CUSTOMER.OrderBy(c => c.cust_name).Where(c => c.customer_auto == accessRecord.customer_auto)
                            .Select(c => new CustomerDealershipDataSet
                            {
                                id = c.customer_auto,
                                type = "customer",
                                name = c.cust_name
                            }).First()
                        );
                    }
                }
            }

            return returnData;
        }

        public IEnumerable<UserCustomerAccessDataSet> getCustomerAccessForUser(long userId, System.Security.Principal.IPrincipal User)
        {
            var accessibleCustomersForTheUser = new Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleCustomers().Select(m=> m.customer_auto);
            return new Core.Domain.UserAccess(new SharedContext(), User).getAccessibleCustomers().Select(m => new UserCustomerAccessDataSet
            {
                customerId = m.customer_auto,
                customerName = m.cust_name,
                hasAccess = accessibleCustomersForTheUser.Any(k => m.customer_auto == k)
            }).AsEnumerable();
        }

        public IEnumerable<UserJobRolesDataSet> getJobRolesForUser(long userId)
        {
            List<UserJobRolesDataSet> returnData = new List<UserJobRolesDataSet>();

            using (var context = new SharedContext())
            {
                returnData.AddRange(
                    context.USER_GROUP.Where(ug => ug.active == true && ug.groupname != "Super User").
                                        Select(ug => new UserJobRolesDataSet
                                        {
                                            jobRoleId = ug.group_auto,
                                            roleName = ug.groupname,
                                            hasRole = false
                                        }));
                var groupsUserBelongsTo = context.USER_GROUP_ASSIGN.Where(u => u.user_auto == userId).ToList();

                foreach (var data in returnData)
                {
                    foreach (var userGroupAssigned in groupsUserBelongsTo)
                    {
                        if (userGroupAssigned.group_auto == data.jobRoleId)
                        {
                            data.hasRole = true;
                        }
                    }
                }
            }

            return returnData;
        }

        public GETResponseMessage updateUserCustomerAccess(long userId, UserCustomerAccessDataSet[] customers)
        {
            using (var context = new SharedContext())
            {
                var customersUserHasAccessTo = context.UserAccessMaps.Where(m => m.customer_auto != null && m.user_auto == userId).ToList();

                foreach (UserCustomerAccessDataSet customer in customers)
                {
                    if (customer.hasAccess)
                    {
                        if (!context.UserAccessMaps.Where(m => m.user_auto == userId && m.customer_auto == customer.customerId).Any())
                        {
                            var customerAccessRecord = new UserAccessMaps()
                            {
                                AccessLevelTypeId = 3,
                                customer_auto = customer.customerId,
                                user_auto = userId
                            };

                            context.UserAccessMaps.Add(customerAccessRecord);
                        }

                        if (!context.USER_CRSF_CUST_EQUIP.Where(m => m.user_auto == userId && m.customer_auto == customer.customerId && m.level_type == 1).Any())
                        {
                            var customerAccessRecord = new USER_CRSF_CUST_EQUIP()
                            {
                                user_auto = userId,
                                customer_auto = customer.customerId,
                                level_type = 1,
                                modified_user = "AUTO INSERT FROM CUSTOMER ACCESS CHANGE"
                            };

                            context.USER_CRSF_CUST_EQUIP.Add(customerAccessRecord);
                        }
                    }
                    else
                    {
                        if (context.UserAccessMaps.Where(m => m.user_auto == userId && m.customer_auto == customer.customerId).Any())
                        {
                            var record = context.UserAccessMaps.Where(m => m.user_auto == userId && m.customer_auto == customer.customerId).First();
                            context.UserAccessMaps.Remove(record);
                        }

                        if (context.USER_CRSF_CUST_EQUIP.Where(m => m.user_auto == userId && m.customer_auto == customer.customerId && m.level_type == 1).Any())
                        {
                            var record = context.USER_CRSF_CUST_EQUIP.Where(m => m.user_auto == userId && m.customer_auto == customer.customerId && m.level_type == 1).First();
                            context.USER_CRSF_CUST_EQUIP.Remove(record);
                        }
                    }
                }

                try
                {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, "Failed to update users customer access. " + e.Message + e.InnerException);
                }
            }

            return new GETResponseMessage(ResponseTypes.Success, "Users customer access updated successfully. ");
        }

        public GETResponseMessage updateJobRolesForUser(long userId, UserJobRolesDataSet[] jobRoles)
        {
            using (var context = new SharedContext())
            {
                var groupsUserBelongsTo = context.USER_GROUP_ASSIGN.Where(u => u.user_auto == userId).ToList();

                foreach (UserJobRolesDataSet role in jobRoles)
                {
                    if(role.hasRole)
                    {
                        if(!context.USER_GROUP_ASSIGN.Where(u => u.user_auto == userId && u.group_auto == role.jobRoleId).Any())
                        {
                            var newRecord = new USER_GROUP_ASSIGN()
                            {
                                active = true,
                                admin = null,
                                user_auto = userId,
                                created_date = DateTime.Now,
                                end_date = null,
                                created_user_auto = 1,
                                start_date = DateTime.Now,
                                group_auto = role.jobRoleId,
                            };
                            context.USER_GROUP_ASSIGN.Add(newRecord);
                        }
                    } else
                    {
                        if(context.USER_GROUP_ASSIGN.Where(u => u.user_auto == userId && u.group_auto == role.jobRoleId).Any())
                        {
                            var record = context.USER_GROUP_ASSIGN.Where(u => u.user_auto == userId && u.group_auto == role.jobRoleId).First();
                            context.USER_GROUP_ASSIGN.Remove(record);
                        }
                    }
                }

                try {
                    context.SaveChanges();
                }
                catch (Exception e)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, "Failed to update users job roles. " + e.Message + e.InnerException);
                }
            }

            return new GETResponseMessage(ResponseTypes.Success, "Uses job roles have been updated successfully. ");
        }

        public string RecordNewUserInvitation(int type, long team_id, string email, int access_level, int userId, bool enableUndercarriage, bool enableGET)
        {
            string result = "";

            using (var context = new SharedContext())
            {
                var currentuser = context.USER_TABLE.Find(userId);
                string aspUserId = currentuser != null ? currentuser.AspNetUserId : "";
                var aspUser = context.AspNetUsers.Find(aspUserId);
                UserInvitations invite = new UserInvitations();
                invite.team_type = type;
                invite.team_id = team_id;
                invite.email = email;
                invite.access_level = access_level;
                invite.invitation_sent = DateTime.Now;
                invite.disable = false;
                invite.SenderEmail = (aspUser == null && currentuser == null) ? "" :
                    (aspUser == null ? currentuser.email : aspUser.Email);
                invite.SenderAspUserId = aspUser == null ? "" : aspUserId;
                invite.InvitationAccepted = false;
                invite.EnableUndercarriage = enableUndercarriage;
                invite.EnableGET = enableGET;
                context.UserInvitations.Add(invite);
                context.SaveChanges();

                result = invite.unique_id.ToString();
            }

            return result;
        }
    }

    // List of available job roles, has role will be true if the user currently has that role
    public class UserJobRolesDataSet
    {
        public int jobRoleId { get; set; }
        public string roleName { get; set; }
        public bool hasRole { get; set; }
    }

    public class UserCustomerAccessDataSet
    {
        public long customerId { get; set; }
        public string customerName { get; set; }
        public bool hasAccess { get; set; }
    }

    public class CustomerDealershipDataSet
    {
        public long id { get; set; }
        public string type { get; set; }
        public string name { get; set; }
    }

    public class BasicUserDataSet
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; } = "******";
        public int? AccessLevel { get; set; }
        public bool Disabled { get; set; }
    }
}
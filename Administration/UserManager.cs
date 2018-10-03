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
using BLLDomain = BLL.Core.Domain;
using BLLGetCore = BLL.GETCore;

namespace BLL.Administration
{
    public class UserManager
    {
        private SharedContext _context;

        public UserManager()
        {
            this._context = new SharedContext();
        }

        /// <summary>
        /// Returns a list of the job roles the given user id has. 
        /// </summary>
        /// <param name="userId">The user Id we want to get a list of job roles for</param>
        public async Task<List<JobRole>> GetUserJobRoles(long userId)
        {
            return await _context.USER_GROUP_ASSIGN.Where(j => j.user_auto == userId).Select(j => new JobRole()
            {
                Id = j.group_auto,
                Name = j.USER_GROUP.groupname
            }).ToListAsync();
        }

        public async Task<UserAccessTypes> GetAccessLevelForUser(long userId)
        {
            var accessLevel = await Task.Run(() => { return new Core.Domain.UserAccess(new SharedContext(), (int)userId).getUserRealtionStatus(); }); //_context.UserAccessMaps.Where(m => m.user_auto == userId).OrderBy(m => m.AccessLevelTypeId).Select(m => m.AccessLevelTypeId).FirstOrDefaultAsync();
            if (accessLevel.SupportMember) return UserAccessTypes.GlobalAdministrator;
            if (accessLevel.DealerGroupMember) return UserAccessTypes.DealershipAdministrator;
            if (accessLevel.DealerMember) return UserAccessTypes.DealershipUser;
            if (accessLevel.CustomerMember) return UserAccessTypes.CustomerAdministrator;
            if (accessLevel.JobsiteMember) return UserAccessTypes.CustomerUser;
            return UserAccessTypes.Unknown;
        }

        public async Task<Tuple<bool, string>> DisableUser(long userId)
        {
            var user = await _context.USER_TABLE.Where(u => u.user_auto == userId).FirstOrDefaultAsync();
            if(user == null)
            {
                return Tuple.Create(false, "Cannot find a user with this ID. ");
            }
            user.suspended = true;
            try
            {
                await _context.SaveChangesAsync();
            } catch
            {
                return Tuple.Create(false, "Failed to disable user. ");
            }
            SendAccountDisabledEmail(user.email);
            return Tuple.Create(true, "User was disabled successfully. ");
        }

        public async Task<Tuple<bool, string>> EnableUser(long userId)
        {
            var user = await _context.USER_TABLE.Where(u => u.user_auto == userId).FirstOrDefaultAsync();
            if (user == null)
            {
                return Tuple.Create(false, "Cannot find a user with this ID. ");
            }
            user.suspended = false;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                return Tuple.Create(false, "Failed to enable user. ");
            }
            SendAccountEnabledEmail(user.email, user.userid);
            return Tuple.Create(true, "User was enabled successfully. ");
        }

        public async Task<Tuple<bool, string>> CancelInvitation(int invitationId)
        {
            var invitation = await _context.UserInvitations.Where(i => i.invitation_auto == invitationId).FirstOrDefaultAsync();
            if(invitation == null)
            {
                return Tuple.Create(false, "Cannot find an invitation with this ID. ");
            }
            _context.UserInvitations.Remove(invitation);
            try
            {
                await _context.SaveChangesAsync();
            } catch
            {
                return Tuple.Create(false, "Failed to cancel invitation. ");
            }
            return Tuple.Create(true, "The invitation has been canceled successfully. ");
        }

        public async Task<List<JobRole>> GetAssignableJobRolesForUser(long userId)
        {
            List<JobRole> returnData = new List<JobRole>();

            returnData.AddRange(await _context.USER_GROUP.Where(ug => ug.active == true).
                                    Select(ug => new JobRole
                                    {
                                        Id = ug.group_auto,
                                        Name = ug.groupname
                                    }).ToListAsync());

            var userAccess = await GetAccessLevelForUser(userId);
            if(userAccess != UserAccessTypes.GlobalAdministrator)
            {
                returnData.RemoveAll(a => a.Name == "Super User");
            }

            return returnData;
        }

        public async Task<EditUserModel> GetUserDetails(long userId, bool showPassword)
        {
            EditUserModel returnUser;
            var userEntity = await _context.USER_TABLE.Where(u => u.user_auto == userId).FirstOrDefaultAsync();
            List<AccessCustomerIdsForUserEditModel> customerIds = new List<AccessCustomerIdsForUserEditModel>();
            List<JobRolesForUserEditModel> jobRoles = await _context.USER_GROUP_ASSIGN.Where(j => j.user_auto == userId).Select(j => new JobRolesForUserEditModel()
            {
                id = j.group_auto,
                itemName = j.USER_GROUP.groupname
            }).ToListAsync();

            if (userEntity == null)
                return null;
            //var accessMap = await _context.UserAccessMaps.Where(m => m.user_auto == userId)
            //    .OrderByDescending(m => m.DealershipId).ThenByDescending(m => m.customer_auto)
            //    .ThenBy(m => m.AccessLevelTypeId).FirstOrDefaultAsync();

            var highestAccessCategory = new BLL.Core.Domain.UserAccess(new SharedContext(), (int)userId).getHighestCategoryAccess();

            //if(highestAccessCategory == BLLDomain.AccessCategory.SupportTeam)
            //{
            //    customerIds = await _context.UserAccessMaps.Where(c => c.user_auto == userId).Where(c => c.customer_auto != null).Select(c => new AccessCustomerIdsForUserEditModel()
            //    {
            //        id = c.customer_auto == null ? 0 : (int) c.customer_auto,
            //        itemName = c.CUSTOMER == null ? "" : c.CUSTOMER.cust_name
            //    }).ToListAsync();
            //}

            returnUser = new EditUserModel()
            {
                AccessTypeId = (int)highestAccessCategory,
                CustomerId = 0, //accessMap.customer_auto,
                DealershipId = 0, //accessMap.DealershipId,
                Email = userEntity.email,
                UserName = userEntity.userid,
                GetEnabled = DoesAccessTypeAllowGET(userEntity.ApplicationAccess == null ? 0 : (int) userEntity.ApplicationAccess),
                UndercarriageEnabled = DoesAccessTypeAllowUndercarriage(userEntity.ApplicationAccess == null ? 0 : (int)userEntity.ApplicationAccess),
                Id = userEntity.user_auto,
                Name = userEntity.username,
                AccessCustomerIds = customerIds,
                JobRoles = jobRoles,
                Password = showPassword ? userEntity.passwd : ""
            };

            return returnUser;
        }

        /*
         * Checks if the application access type number allows a user to use the undercarriage application. 
         */
        private bool DoesAccessTypeAllowUndercarriage(int accessType)
        {
            if(accessType == (int) ApplicationAccessTypes.Undercarriage || accessType == (int)ApplicationAccessTypes.UndercarriageAndGET)
            {
                return true;
            }
            return false;
        }

        /*
         * Checks if the applicaton access type allows a user to use GET
         */
        private bool DoesAccessTypeAllowGET(int accessType)
        {
            if (accessType == (int)ApplicationAccessTypes.GET || accessType == (int)ApplicationAccessTypes.UndercarriageAndGET)
            {
                return true;
            }
            return false;
        }

        public async Task<Tuple<bool, string>> InviteNewUser(UserModel user, long invitedByUserId)
        {
            var existingAccountEmail = await _context.USER_TABLE.Where(t => t.email == user.Email).FirstOrDefaultAsync();
            var existingInvitationEmail = await _context.UserInvitations.Where(t => t.email == user.Email).FirstOrDefaultAsync();

            if (existingAccountEmail != null || existingInvitationEmail != null)
                return Tuple.Create(false, "A user with this email already exists. ");

            var existingUserName = await _context.USER_TABLE.Where(t => t.userid == user.UserName).Where(t => t.user_auto != user.Id).FirstOrDefaultAsync();
            if (existingUserName != null)
                return Tuple.Create(false, "A user with this username already exists. ");

            var aspNetUserId = await _context.USER_TABLE.Where(t => t.user_auto == user.Id).Select(t => t.AspNetUserId).FirstOrDefaultAsync();
            var existingUserName2 = await _context.AspNetUsers.Where(t => t.UserName == user.UserName).Where(t => t.Id != aspNetUserId).FirstOrDefaultAsync();
            if (existingUserName2 != null)
                return Tuple.Create(false, "A user with this username already exists. ");
            if (user.Password.Length < 3)
                return Tuple.Create(false, "Password must be at least 3 characters. ");

            int teamType = 0;
            int teamId = 0;
            if (user.DealershipId != null)
            {
                teamType = 1;
                teamId = (int)user.DealershipId;
            }
            else if (user.CustomerId != null)
            {
                teamType = 2;
                teamId = (int)user.CustomerId;
            }

            var userInvitation = new UserInvitations()
            {
                team_type = teamType,
                team_id = teamId,
                email = user.Email,
                name = user.Name,
                username = user.UserName,
                access_level = user.AccessTypeId,
                invitation_sent = DateTime.Now,
                disable = false,
                SenderEmail = _context.USER_TABLE.Find(invitedByUserId).email,
                SenderAspUserId = _context.USER_TABLE.Find(invitedByUserId).AspNetUserId,
                InvitationAccepted = false,
                EnableUndercarriage = user.UndercarriageEnabled,
                EnableGET = user.GetEnabled
            };
            _context.UserInvitations.Add(userInvitation);

            // Add customer access records if required
            if(user.AccessTypeId == (int) UserAccessTypes.DealershipUser)
            {
                foreach(var customer in user.AccessCustomerIds)
                {
                    _context.UserInvitationAccessToCustomers.Add(new UserInvitationAccessToCustomers()
                    {
                        customerId = customer,
                        invitationId = userInvitation.invitation_auto
                    });
                }
            }

            // Add job role records
            foreach(var role in user.JobRoles)
            {
                _context.UserInvitationJobRoles.Add(new UserInvitationJobRoles()
                {
                    invitationId = userInvitation.invitation_auto,
                    jobRoleId = role
                });
            }

            _context.SaveChanges();

            // Send email
            if (userInvitation.unique_id.ToString() == "")
                return Tuple.Create(false, "Failed to generate unique registration ID. Please contact support. ");

            var emailManager = new EmailManager();
            BLL.Core.Domain.AppConfigAccess ACA = new BLL.Core.Domain.AppConfigAccess();
            string identityServerAccess = ACA.GetApplicationValue("IdentityServerUri");
            string emailSubject = "Invited to register for TrackTreads";
            string emailBody = @"<br/>
                                <p>You have been invited to register for TrackTreads.</p> 
                                <br/>
                                <p>Please use the following link to complete your registration.</p>
                                <p><a href='" + identityServerAccess + @"Account/Register?token=" + userInvitation.unique_id + "'>"
                                 + identityServerAccess + @"Account/Register?token=" + userInvitation.unique_id + @"
                                </a></p>
                                <br/><br/>";
            var result = emailManager.SendEmail(user.Email, emailSubject, emailBody, true);
            if (result)
                return Tuple.Create(true, userInvitation.unique_id.ToString());
            return Tuple.Create(false, "Failed to send the email to the user. ");
        }

        public async Task<Tuple<bool, string>> UpdateUser(UserModel user)
        {
            var existingAccountEmail = await _context.USER_TABLE.Where(t => t.email == user.Email).Where(t => t.user_auto != user.Id).FirstOrDefaultAsync();
            if (existingAccountEmail != null)
                return Tuple.Create(false, "A user with this email already exists. ");

            var existingUserName = await _context.USER_TABLE.Where(t => t.userid == user.UserName).Where(t => t.user_auto != user.Id).FirstOrDefaultAsync();
            if (existingUserName != null)
                return Tuple.Create(false, "A user with this username already exists. ");

            var aspNetUserId = await _context.USER_TABLE.Where(t => t.user_auto == user.Id).Select(t => t.AspNetUserId).FirstOrDefaultAsync();

            var existingUserName2 = await _context.AspNetUsers.Where(t => t.UserName == user.UserName).Where(t => t.Id != aspNetUserId).FirstOrDefaultAsync();
            if (existingUserName2 != null)
                return Tuple.Create(false, "A user with this username already exists. ");

            var userEntity = await _context.USER_TABLE.Where(u => u.user_auto == user.Id).FirstOrDefaultAsync();
            if(userEntity == null)
                return Tuple.Create(false, "This user id does not exist. ");

            var aspNetUserEntity = await _context.AspNetUsers.Where(u => u.Id == userEntity.AspNetUserId).FirstOrDefaultAsync();

            if (aspNetUserEntity == null)
                return Tuple.Create(false, "Failed to find AspNetUser record. ");

            //if (!updateAccessRecordsOldUCUIForNewUser(user))
                //return Tuple.Create(false, "Failed to update user access records. ");

            //if (!updateUserAccessMaps(user))
             //   return Tuple.Create(false, "Failed to update user access maps. ");

            if (!updateUserJobRoles(user))
                return Tuple.Create(false, "Failed to update user job roles. ");

            userEntity.ApplicationAccess = (int) getApplicationAccessType(user.UndercarriageEnabled, user.GetEnabled);
            userEntity.username = user.Name;// Flows through as the inspectors name    
            userEntity.userid = user.UserName; // Used to login to mobile app
            userEntity.email = user.Email;
            aspNetUserEntity.UserName = user.UserName;  // Can login to web app
            aspNetUserEntity.Email = user.Email;    // Can login to web app

            try
            {
                _context.SaveChanges();
            } catch
            {
                return Tuple.Create(false, "Failed to update user correctly. ");
            }

            bool result = SendUpdateUserEmail(user.Name, user.UserName, user.Email);
            if (!result)
                return Tuple.Create(false, "User was updated but failed to send notification email to the user. ");


            return Tuple.Create(true, "User was updated successfully. ");
        }

        private bool SendUpdateUserEmail(string name, string userName, string email)
        {
            var emailManager = new EmailManager();
            string emailSubject = "Account Updated";
            string emailBody = @"<p>Your user account details have been updated. You will need to use your <b>Username</b> when you wish to login to both the web application and mobile inspection application. </p> 
                                <p>Full Name: <b>"+name+@"</b></p>
                                <p>Email: <b>"+email+@"</b></p>
                                <p>Username: <b>"+userName+@"</b> (Login with this)</p>
                                <br/>";
            return emailManager.SendEmail(email, emailSubject,emailBody,true);
        }

        private bool SendAccountDisabledEmail(string email)
        {
            var emailManager = new EmailManager();
            string emailSubject = "Account Disabled";
            string emailBody = @"<p>Your account has been disabled and you will no longer be able to sign in. If you believe this was an error, please contact TrackTreads support. </p>";
            return emailManager.SendEmail(email, emailSubject, emailBody, true);
        }

        private bool SendAccountEnabledEmail(string email, string username)
        {
            var emailManager = new EmailManager();
            string emailSubject = "Account Enabled";
            string emailBody = @"<p>Your account has been enabled and you can now sign in to TrackTreads. </p>
                                 <p>Username: <b>"+username+ @"</b>";
            return emailManager.SendEmail(email, emailSubject, emailBody, true);
        }

        private ApplicationAccessTypes getApplicationAccessType(bool enableUndercarriage, bool enableGET)
        {
            if (!enableUndercarriage && !enableGET)
            {
                return ApplicationAccessTypes.None;
            }
            else if (enableUndercarriage && !enableGET)
            {
                return ApplicationAccessTypes.Undercarriage;
            }
            else if (!enableUndercarriage && enableGET)
            {
                return ApplicationAccessTypes.GET;
            }
            else
            {
                return ApplicationAccessTypes.UndercarriageAndGET;
            }
        }

        private bool updateUserJobRoles(UserModel user)
        {
            var oldRoles = _context.USER_GROUP_ASSIGN.Where(r => r.user_auto == user.Id).ToList();
            _context.USER_GROUP_ASSIGN.RemoveRange(oldRoles);

            foreach(var role in user.JobRoles)
            {
                _context.USER_GROUP_ASSIGN.Add(new USER_GROUP_ASSIGN()
                {
                    user_auto = user.Id,
                    active = true,
                    created_date = DateTime.Now,
                    group_auto = role,
                    start_date = DateTime.Now
                });
            }

            try
            {
                _context.SaveChanges();
            } catch
            {
                return false;
            }
            return true;
        }

        private bool updateUserAccessMaps(UserModel user)
        {
            var existingMaps = _context.UserAccessMaps.Where(m => m.user_auto == user.Id).ToList();
            _context.UserAccessMaps.RemoveRange(existingMaps);

            UserAccessMaps registerUserAccess = new UserAccessMaps()
            {
                user_auto = user.Id,
                AccessLevelTypeId = user.AccessTypeId,
                DealershipId = user.DealershipId,
                customer_auto = user.CustomerId,
                crsf_auto = null,
                equipmentid_auto = null
            };
            _context.UserAccessMaps.Add(registerUserAccess);

            // Add access maps for multiple customers if user is a dealership user, and has been given access to specific customers
            if (user.AccessTypeId == (int)UserAccessTypes.DealershipUser)
            {
                foreach (var custId in user.AccessCustomerIds)
                {
                    UserAccessMaps customerAccess = new UserAccessMaps()
                    {
                        user_auto = user.Id,
                        AccessLevelTypeId = user.AccessTypeId,
                        DealershipId = null,
                        customer_auto = custId,
                        crsf_auto = null,
                        equipmentid_auto = null
                    };
                    _context.UserAccessMaps.Add(customerAccess);
                }
            }

            try
            {
                _context.SaveChanges();
            } catch
            {
                return false;
            }

            foreach (var support in _context.SUPPORT_TEAM)
                new BLLDomain.UserAccessDomain.SupportTeamAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromSupportTeam(support.Id, user.Id.LongNullableToInt());
            foreach (var group in _context.DEALER_GROUP)
                new BLLDomain.UserAccessDomain.DealerGroupAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromDealerGroup(group.Id, user.Id.LongNullableToInt());
            foreach (var dealer in _context.Dealerships)
                new BLLDomain.UserAccessDomain.DealerAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromDealer(dealer.DealershipId, user.Id.LongNullableToInt());
            foreach (var customer in _context.CUSTOMER)
                new BLLDomain.UserAccessDomain.CustomerAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromCustomer(customer.customer_auto.LongNullableToInt(), user.Id.LongNullableToInt());
            foreach (var jobsite in _context.CRSF)
                new BLLDomain.UserAccessDomain.JobsiteAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromJobsite(jobsite.crsf_auto.LongNullableToInt(), user.Id.LongNullableToInt());

            switch ((BLLGetCore.Classes.UserAccessTypes)user.AccessTypeId)
            {
                case BLLGetCore.Classes.UserAccessTypes.GlobalAdministrator:
                    new BLLDomain.UserAccessDomain.SupportTeamAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToSupportTeam(1, user.Id.LongNullableToInt());
                    break;
                case BLLGetCore.Classes.UserAccessTypes.DealershipAdministrator:
                    new BLLDomain.UserAccessDomain.DealerGroupAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToDealerGroup(1, user.Id.LongNullableToInt());
                    break;
                case BLLGetCore.Classes.UserAccessTypes.DealershipUser:
                    new BLLDomain.UserAccessDomain.DealerAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToDealer(user.DealershipId.Value, user.Id.LongNullableToInt());
                    break;
                case BLLGetCore.Classes.UserAccessTypes.CustomerAdministrator:
                    new BLLDomain.UserAccessDomain.CustomerAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToCustomer(user.CustomerId.LongNullableToInt(), user.Id.LongNullableToInt());
                    break;
                case BLLGetCore.Classes.UserAccessTypes.CustomerUser:
                    new BLLDomain.UserAccessDomain.JobsiteAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToJobsite(user.JobsiteId.LongNullableToInt(), user.Id.LongNullableToInt());
                    break;
            }

            return true;
        }

        private bool updateAccessRecordsOldUCUIForNewUser(UserModel user)
        {
            var existingRecords = _context.USER_CRSF_CUST_EQUIP.Where(c => c.user_auto == user.Id).ToList();
            _context.USER_CRSF_CUST_EQUIP.RemoveRange(existingRecords);

            if (user.AccessTypeId == (int)UserAccessTypes.GlobalAdministrator)
            {
                List<long> customerIds = _context.CUSTOMER.Select(c => c.customer_auto).ToList();
                foreach (long customerId in customerIds)
                {
                    USER_CRSF_CUST_EQUIP accessRecord = new USER_CRSF_CUST_EQUIP()
                    {
                        user_auto = user.Id,
                        customer_auto = customerId,
                        level_type = 1,
                        modified_user = "AUTO INSERT FROM IDENTITY SERVER ACCOUNT UPDATE"
                    };
                    _context.USER_CRSF_CUST_EQUIP.Add(accessRecord);
                }
            }
            // Dealership admin
            else if (user.AccessTypeId == (int)UserAccessTypes.DealershipAdministrator)
            {
                long[] customerIds = _context.CUSTOMER.Where(c => c.DealershipId == user.DealershipId).Select(c => c.customer_auto).ToArray();
                foreach (long customerId in customerIds)
                {
                    USER_CRSF_CUST_EQUIP accessRecord = new USER_CRSF_CUST_EQUIP()
                    {
                        user_auto = user.Id,
                        customer_auto = customerId,
                        level_type = 1,
                        modified_user = "AUTO INSERT FROM IDENTITY SERVER ACCOUNT UPDATE"
                    };
                    _context.USER_CRSF_CUST_EQUIP.Add(accessRecord);
                }
            }
            else if (user.AccessTypeId == (int)UserAccessTypes.DealershipUser) // Dealership user
            {
                foreach (var customer in user.AccessCustomerIds)
                {
                    USER_CRSF_CUST_EQUIP accessRecord = new USER_CRSF_CUST_EQUIP()
                    {
                        user_auto = user.Id,
                        customer_auto = customer,
                        level_type = 1,
                        modified_user = "AUTO INSERT FROM IDENTITY SERVER ACCOUNT UPDATE"
                    };
                    _context.USER_CRSF_CUST_EQUIP.Add(accessRecord);
                }

            } // Customer admin or customer user
            else if (user.AccessTypeId == (int)UserAccessTypes.CustomerAdministrator || user.AccessTypeId == (int)UserAccessTypes.CustomerUser)
            {
                USER_CRSF_CUST_EQUIP accessRecord = new USER_CRSF_CUST_EQUIP()
                {
                    user_auto = user.Id,
                    customer_auto = (long)user.CustomerId,
                    level_type = 1,
                    modified_user = "AUTO INSERT FROM IDENTITY SERVER ACCOUNT UPDATE"
                };
                _context.USER_CRSF_CUST_EQUIP.Add(accessRecord);
            }

            try
            {
                _context.SaveChanges();
            } catch
            {
                return false;
            }

            foreach(var support in _context.SUPPORT_TEAM)
                new BLLDomain.UserAccessDomain.SupportTeamAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromSupportTeam(support.Id, user.Id.LongNullableToInt());
            foreach(var group in _context.DEALER_GROUP)
                new BLLDomain.UserAccessDomain.DealerGroupAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromDealerGroup(group.Id, user.Id.LongNullableToInt());
            foreach(var dealer in _context.Dealerships)
                new BLLDomain.UserAccessDomain.DealerAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromDealer(dealer.DealershipId, user.Id.LongNullableToInt());
            foreach(var customer in _context.CUSTOMER)
                new BLLDomain.UserAccessDomain.CustomerAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromCustomer(customer.customer_auto.LongNullableToInt(), user.Id.LongNullableToInt());
            foreach(var jobsite in _context.CRSF)
                new BLLDomain.UserAccessDomain.JobsiteAccess(new SharedContext(), user.Id.LongNullableToInt()).RemoveUserFromJobsite(jobsite.crsf_auto.LongNullableToInt(), user.Id.LongNullableToInt());

            switch ((BLLGetCore.Classes.UserAccessTypes)user.AccessTypeId)
            {
                case BLLGetCore.Classes.UserAccessTypes.GlobalAdministrator:
                    new BLLDomain.UserAccessDomain.SupportTeamAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToSupportTeam(1, user.Id.LongNullableToInt());
                    break;
                case BLLGetCore.Classes.UserAccessTypes.DealershipAdministrator:
                    new BLLDomain.UserAccessDomain.DealerGroupAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToDealerGroup(1, user.Id.LongNullableToInt());
                    break;
                case BLLGetCore.Classes.UserAccessTypes.DealershipUser:
                    new BLLDomain.UserAccessDomain.DealerAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToDealer(user.DealershipId.Value, user.Id.LongNullableToInt());
                    break;
                case BLLGetCore.Classes.UserAccessTypes.CustomerAdministrator:
                    new BLLDomain.UserAccessDomain.CustomerAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToCustomer(user.CustomerId.LongNullableToInt(), user.Id.LongNullableToInt());
                    break;
                case BLLGetCore.Classes.UserAccessTypes.CustomerUser:
                    new BLLDomain.UserAccessDomain.JobsiteAccess(new SharedContext(), user.Id.LongNullableToInt()).AddUserToJobsite(user.JobsiteId.LongNullableToInt(), user.Id.LongNullableToInt());
                    break;
            }

            return true;
        }

        // Returns a list of access types the given user ID is allowed to assign to new users they are creating. 
        public async Task<List<AuthorizedUserAccessType>> GetAuthorizedUserAccessTypes(long userId)
        {
            var accessLevel = await GetAccessLevelForUser(userId);
            var result = new List<AuthorizedUserAccessType>();
            var globalAdmin = new AuthorizedUserAccessType()
            {
                UserAccessTypeId = (int)UserAccessTypes.GlobalAdministrator,
                Name = "Support"
            };
            var dealerAdmin = new AuthorizedUserAccessType()
            {
                UserAccessTypeId = (int)UserAccessTypes.DealershipAdministrator,
                Name = "Dealer Group"
            };
            var dealerUser = new AuthorizedUserAccessType()
            {
                UserAccessTypeId = (int)UserAccessTypes.DealershipUser,
                Name = "Dealer"
            };
            var customerAdmin = new AuthorizedUserAccessType()
            {
                UserAccessTypeId = (int)UserAccessTypes.CustomerAdministrator,
                Name = "Customer"
            };
            var customerUser = new AuthorizedUserAccessType()
            {
                UserAccessTypeId = (int)UserAccessTypes.CustomerUser,
                Name = "Jobsite"
            };

            if ((int)accessLevel <= 5) result.Add(customerUser);
            if ((int)accessLevel <= 4) result.Add(customerAdmin);
            if ((int)accessLevel <= 3) result.Add(dealerUser);
            if ((int)accessLevel <= 2) result.Add(dealerAdmin);
            if ((int)accessLevel <= 1) result.Add(globalAdmin);

            return result;
        }

        public async Task<List<PendingUserOverviewModel>> GetAllPendingUsersForUser(long userId)
        {
            List<PendingUserOverviewModel> returnList = new List<PendingUserOverviewModel>();
            List<long> inviteIds = new List<long>();
            var userAccess = await _context.UserAccessMaps.Where(m => m.user_auto == userId).ToListAsync();

            foreach (var access in userAccess)
            {
                if (access.AccessLevelTypeId == (int)UserAccessTypes.GlobalAdministrator)
                {
                    inviteIds.AddRange(_context.UserInvitations.Where(i => i.InvitationAccepted == false).Select(i => i.invitation_auto));
                }
                else if (access.AccessLevelTypeId == (int)UserAccessTypes.DealershipAdministrator)
                {
                    inviteIds.AddRange(_context.UserInvitations.Where(i => i.InvitationAccepted == false)
                        .Where(m => m.team_id == access.DealershipId && m.team_type == 1).Select(u => u.invitation_auto));
                    var customerIds = _context.CUSTOMER.Where(c => c.DealershipId == access.DealershipId).Select(c => c.customer_auto);
                    foreach (var customerId in customerIds)
                    {
                        try
                        {
                            inviteIds.AddRange(_context.UserInvitations.Where(i => i.InvitationAccepted == false)
    .Where(m => m.team_id == customerId && m.team_type == 2).Select(u => u.invitation_auto));
                        }
                        catch (Exception e)
                        {
                            //
                        }
                    }
                }
                else if (access.AccessLevelTypeId == (int)UserAccessTypes.CustomerAdministrator)
                {
                    inviteIds.AddRange(_context.UserInvitations.Where(i => i.InvitationAccepted == false)
                        .Where(m => m.team_id == access.customer_auto && m.team_type == 2).Select(u => u.invitation_auto));
                }
            }

            foreach (var user in inviteIds.Distinct())
            {
                var userAccessLevel = await _context.UserInvitations.Where(m => m.invitation_auto == user).FirstOrDefaultAsync();
                string name;
                if (userAccessLevel != null)
                {
                    if (userAccessLevel.access_level == (int)UserAccessTypes.DealershipAdministrator || userAccessLevel.access_level == (int)UserAccessTypes.DealershipUser)
                        name = await _context.Dealerships.Where(d => d.DealershipId == userAccessLevel.team_id).Select(d => d.Name).FirstOrDefaultAsync();
                    else if (userAccessLevel.access_level == (int)UserAccessTypes.CustomerAdministrator || userAccessLevel.access_level == (int)UserAccessTypes.CustomerUser)
                        name = await _context.CUSTOMER.Where(d => d.customer_auto == userAccessLevel.team_id).Select(d => d.cust_name).FirstOrDefaultAsync();
                    else
                        name = "";
                    returnList.AddRange(_context.UserInvitations.Where(u => u.invitation_auto == user).Select(u => new PendingUserOverviewModel()
                    {
                        Email = u.email,
                        InviteId = u.invitation_auto,
                        UserName = "",
                        TeamName = name,
                        Access = _context.AccessLevelTypes.Where(a => a.AccessLevelTypesId == userAccessLevel.access_level).Select(a => a.Name).FirstOrDefault()
                    }));
                }
            }

            return returnList.OrderBy(l => l.Email).ToList();
        }

        /// <summary>
        /// Returns some general details of all user accounts in the database which the user id given has access to see. 
        /// Used to populate the user table in the team administration page. 
        /// </summary>
        /// <param name="userId">User ID used to determine which accounts to return. </param>
        /// <returns></returns>
        /*public async Task<List<UserOverviewModel>> GetAllUsersForUser(long userId)
        {
            List<UserOverviewModel> returnList = new List<UserOverviewModel>();
            List<long> userIds = new List<long>();
            var userAccess = await _context.UserAccessMaps.Where(m => m.user_auto == userId).ToListAsync();
            
            foreach(var access in userAccess)
            {
                if(access.AccessLevelTypeId == (int) UserAccessTypes.GlobalAdministrator)
                {
                    userIds.AddRange(_context.USER_TABLE.Select(u => u.user_auto));
                } else if(access.AccessLevelTypeId == (int) UserAccessTypes.DealershipAdministrator)
                {
                    userIds.AddRange(_context.UserAccessMaps.Where(m => m.DealershipId == access.DealershipId).Where(m=> m.user_auto != null).Select(u => (long)u.user_auto));
                    var customerIds = _context.CUSTOMER.Where(c => c.DealershipId == access.DealershipId).Select(c => c.customer_auto);
                    foreach(var customerId in customerIds)
                    {
                        try
                        {
                            userIds.AddRange(_context.UserAccessMaps.Where(m => m.customer_auto == customerId).Where(m => m.user_auto != null).Select(u => (long)u.user_auto).ToList());
                        } catch (Exception e)
                        {
                            //
                        }

                    }
                } else if(access.AccessLevelTypeId == (int) UserAccessTypes.CustomerAdministrator)
                {
                    userIds.AddRange(_context.UserAccessMaps.Where(m => m.customer_auto == access.customer_auto)
                        .Where(m => m.AccessLevelTypeId == (int)UserAccessTypes.CustomerUser || m.AccessLevelTypeId == (int)UserAccessTypes.CustomerAdministrator)
                        .Where(m => m.user_auto != null).Select(u => (long)u.user_auto));
                }
            }

            foreach(var user in userIds.Distinct())
            {
                var userAccessLevel = await _context.UserAccessMaps.Where(m => m.user_auto == user).OrderByDescending(m => m.DealershipId).ThenByDescending(m => m.customer_auto).ThenBy(m => m.AccessLevelTypeId).FirstOrDefaultAsync();
                string name;
                if (userAccessLevel != null)
                {
                    if (userAccessLevel.AccessLevelTypeId == (int)UserAccessTypes.DealershipAdministrator || userAccessLevel.AccessLevelTypeId == (int)UserAccessTypes.DealershipUser)
                        name = userAccessLevel.Dealership.Name;
                    else if (userAccessLevel.AccessLevelTypeId == (int)UserAccessTypes.CustomerAdministrator || userAccessLevel.AccessLevelTypeId == (int)UserAccessTypes.CustomerUser)
                        name = userAccessLevel.CUSTOMER.cust_name;
                    else
                        name = "";
                    returnList.AddRange(_context.USER_TABLE.Where(u => u.user_auto == user).Select(u => new UserOverviewModel()
                    {
                        Email = u.email,
                        UserId = u.user_auto,
                        FullName = u.username,
                        UserName = u.userid,
                        TeamName = name,
                        Access = userAccessLevel.AccessLevelType.Name,
                        Disabled = u.suspended
                    }));
                }
            }

            return returnList.OrderBy(l => l.Disabled).ThenBy(l => l.UserName).ToList();
        }
        */
        /// <summary>
        /// Returns some general details of all user accounts in the database which the user id given has access to see. 
        /// Used to populate the user table in the team administration page. 
        /// </summary>
        /// <param name="userId">User ID used to determine which accounts to return. </param>
        /// <returns></returns>
        public async Task<List<UserOverviewModel>> GetAllUsersForUser(long userId)
        {
            List<UserOverviewModel> returnList = new List<UserOverviewModel>();
            
            var users = await Task.Run(() => { return new BLLDomain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleUsers(); });

            foreach (var user in users.ToList())
            {
                var supportTeams = new BLLDomain.UserAccess(new SharedContext(), user.user_auto.LongNullableToInt()).getAccessibleSupportTeams();
                if (supportTeams.Count() > 0)
                {
                    returnList.Add(new UserOverviewModel
                    {
                        Access = "Support Team",
                        Disabled = user.suspended,
                        UserId = user.user_auto,
                        Email = user.email,
                        UserName = user.userid,
                        TeamName = string.Join(",", supportTeams.Select(m => m.Name).ToArray()),
                        FullName = user.username
                    });
                    continue;
                }
                var dealerGroups = new BLLDomain.UserAccess(new SharedContext(), user.user_auto.LongNullableToInt()).getAccessibleDealerGroups();
                if (dealerGroups.Count() > 0)
                {
                    returnList.Add(new UserOverviewModel
                    {
                        Access = "Dealer Group",
                        Disabled = user.suspended,
                        UserId = user.user_auto,
                        Email = user.email,
                        UserName = user.userid,
                        TeamName = string.Join(",", dealerGroups.Select(m => m.Name).ToArray()),
                        FullName = user.username
                    });
                    continue;
                }

                var dealers = new BLLDomain.UserAccess(new SharedContext(), user.user_auto.LongNullableToInt()).getAccessibleDealers();
                if (dealers.Count() > 0)
                {
                    returnList.Add(new UserOverviewModel
                    {
                        Access = "Dealer",
                        Disabled = user.suspended,
                        UserId = user.user_auto,
                        Email = user.email,
                        UserName = user.userid,
                        TeamName = string.Join(",", dealers.Select(m => m.Name).ToArray()),
                        FullName = user.username
                    });
                    continue;
                }

                var customers = new BLLDomain.UserAccess(new SharedContext(), user.user_auto.LongNullableToInt()).getAccessibleCustomers();
                if (customers.Count() > 0)
                {
                    returnList.Add(new UserOverviewModel
                    {
                        Access = "Customer",
                        Disabled = user.suspended,
                        UserId = user.user_auto,
                        Email = user.email,
                        UserName = user.userid,
                        TeamName = string.Join(",", customers.Select(m => m.cust_name).ToArray()),
                        FullName = user.username
                    });
                    continue;
                }

                var jobsites = new BLLDomain.UserAccess(new SharedContext(), user.user_auto.LongNullableToInt()).getAccessibleJobsites();
                if (jobsites.Count() > 0)
                {
                    returnList.Add(new UserOverviewModel
                    {
                        Access = "Jobsite",
                        Disabled = user.suspended,
                        UserId = user.user_auto,
                        Email = user.email,
                        UserName = user.userid,
                        TeamName = string.Join(",", jobsites.Select(m => m.site_name).ToArray()),
                        FullName = user.username
                    });
                    continue;
                }

                var equipemnts = new BLLDomain.UserAccess(new SharedContext(), user.user_auto.LongNullableToInt()).getAccessibleEquipments();
                if (equipemnts.Count() > 0)
                {
                    returnList.Add(new UserOverviewModel
                    {
                        Access = "Equipment",
                        Disabled = user.suspended,
                        UserId = user.user_auto,
                        Email = user.email,
                        UserName = user.userid,
                        TeamName = string.Join(",", equipemnts.Select(m => m.serialno).ToArray()),
                        FullName = user.username
                    });
                    continue;
                }
                returnList.Add(new UserOverviewModel
                {
                    Access = "No Access",
                    Disabled = user.suspended,
                    UserId = user.user_auto,
                    Email = user.email,
                    UserName = user.userid,
                    TeamName = "",
                    FullName = user.username
                });
            }

            return returnList.OrderBy(l => l.Disabled).ThenBy(l => l.UserName).ToList();
        }
    }
}
 
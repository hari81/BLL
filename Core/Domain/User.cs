using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Interfaces;
using System.Data.Entity;
using BLL.Extensions;
using BLL.GETCore.Classes;
using DAL;
using System.Data.SqlClient;

namespace BLL.Core.Domain
{
    public class User:IUser
    {
        public int Id { get; set; }
        public string userStrId { get; set; }
        public string userName { get; set; }
    }

    public class TTDevUser
    {
        private DAL.SharedContext _context;
        private bool Initialized = false;
        private DAL.USER_TABLE DALUser;
        public TTDevUser(DbContext sharedContext, int oldUserId)
        {
            Init(sharedContext, oldUserId);
        }
        private bool Init(DbContext sharedContext, int oldUserId)
        {
            try
            {
                _context = (DAL.SharedContext)sharedContext;
                if (oldUserId <= 0)
                    return false;
                DALUser = _context.USER_TABLE.Find(oldUserId);
                if (DALUser == null)
                    return false;
                Initialized = true;
            }
            catch(Exception ex)
            {
                string message = ex.Message;
            }
            return Initialized;
        }

        public string GetUserEmail()
        {
            return DALUser.email;
        }



        public string GetUserName()
        {
            return DALUser.username;
        }

        public string GetUserPhoneNo()
        {
            return DALUser.phone_number;
        }

        public string GetUserPhoneAreaCode()
        {
            return DALUser.phone_area_code;
        }

        public User getUCUser()
        {
            if (!Initialized)
                return null;
            return new User
            {
                Id = DALUser.user_auto.LongNullableToInt(),
                userName = DALUser.username,
                userStrId = DALUser.userid
            };
        }
        public IQueryable<DAL.USER_TABLE> getUsersForThisUser()
        {
            var result = new List<DAL.USER_TABLE>();
            if (!Initialized)
                return result.AsQueryable();
            var dealershipIds = DALUser.UserAccessMaps.Select(m => m.DealershipId);
            var userCustomerIds = DALUser.UserAccessMaps.Select(m => m.customer_auto);
            var dealershipCustomerIds = _context.CUSTOMER.Where(m => dealershipIds.Any(p => p.Value == m.DealershipId)).Select(m => m.customer_auto);
            var users = _context.UserAccessMaps.Where(m => dealershipCustomerIds.Any(p => p == m.customer_auto) || userCustomerIds.Any(p => p == m.customer_auto)).Select(m => m.USER_TABLE).GroupBy(m => m.user_auto).Select(m => m.FirstOrDefault());
            return users;
        }

        /// <summary>
        /// Returns a list of users which this user has access to view. 
        /// </summary>
        /// <returns></returns>
        public IQueryable<DAL.USER_TABLE> GetUsersThisUserHasAccessTo()
        {
            var result = new List<DAL.USER_TABLE>();
            if (!Initialized)
                return result.AsQueryable();
            var userAccess = _context.UserAccessMaps.Where(m => m.user_auto == DALUser.user_auto).ToList();
            userAccess.ForEach(i =>
            {
                switch(i.AccessLevelTypeId)
                {
                    case (int)UserAccessTypes.GlobalAdministrator:
                        result.AddRange(_context.USER_TABLE.ToList()); // Return All users
                        break;
                    case (int)UserAccessTypes.DealershipAdministrator:
                        result.AddRange(_context.UserAccessMaps.Where(m => m.DealershipId == i.DealershipId).Select(u => u.USER_TABLE)); //All users in dealership
                        var customersInDealership = _context.CUSTOMER.Where(c => c.DealershipId == i.DealershipId).Select(c => c.customer_auto).ToList();
                        customersInDealership.ForEach(c =>
                        {
                            // All users in customers in this dealership, excluding dealership users who have access to the customers because we already added them above
                            result.AddRange(_context.UserAccessMaps.Where(m => m.customer_auto == c).Where(m => m.AccessLevelTypeId != (int)UserAccessTypes.DealershipUser).Select(m => m.USER_TABLE));
                        });
                        break;
                    case (int)UserAccessTypes.DealershipUser:
                        // This record only specifys the user is a dealership user and belongs to that dealership. Contains no customers.
                        if (i.DealershipId != null)
                        {
                            result.Add(DALUser);
                            break;
                        }
                        // All users within customers which this dealership user has been given access to see.
                        result.AddRange(_context.UserAccessMaps.Where(m => m.customer_auto == i.customer_auto).Where(m => m.AccessLevelTypeId != (int)UserAccessTypes.DealershipUser).Select(m => m.USER_TABLE));
                        break;
                    case (int)UserAccessTypes.CustomerAdministrator:
                        // All users inside this users customer
                        result.AddRange(_context.UserAccessMaps.Where(m => m.customer_auto == i.customer_auto).Where(m => m.AccessLevelTypeId != (int)UserAccessTypes.DealershipUser).Select(m => m.USER_TABLE));
                        break;
                    case (int)UserAccessTypes.CustomerUser:
                        // Just this user. 
                        result.Add(DALUser);
                        break;
                }
            });


            return result.AsQueryable();
        }

        public string getUserLanguage()
        {
            if (!Initialized)
                return "English";
              return DALUser.Language.Fulllanguage;
        }

        public long GetUserAutoById(string loginId)
        {
            long usrAuto = 0;

            var items = _context.USER_TABLE.Where(m => m.userid == loginId).ToList();
            foreach (var item in items)
            {
                return item.user_auto;
            }

            return usrAuto;
        }
    }
}
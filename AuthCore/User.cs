using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Security.Claims;

namespace BLL.AuthCore
{
    public class User : TTAuthorization
    {
        private bool Initialized = false;
        private System.Security.Principal.IPrincipal _User;
        public User(DbContext context) : base(context)
        {
        }
        public User(DbContext context, System.Security.Principal.IPrincipal User) : base(context)
        {
            Init(User);
        }
        private void Init(System.Security.Principal.IPrincipal User)
        {
            if (User != null)
            {
                _User = User;
                Initialized = true;
            }    
        }
        public int GetOldUserId(string AspNetUserId)
        {
            var oldUsers = _context.USER_TABLE.Where(m => m.AspNetUserId == AspNetUserId);
            if (oldUsers.Count() == 0)
                return 0;
            return longNullableToint(oldUsers.First().user_auto);
        }

        public DAL.USER_TABLE GetOldUserRecord(string AspNetUserId)
        {
            var oldUsers = _context.USER_TABLE.Where(m => m.AspNetUserId == AspNetUserId);
            if (oldUsers.Count() == 0)
                return null;
            return oldUsers.First();
        }
        public DAL.USER_TABLE GetOldUserRecord(int userTableId)
        {
            return _context.USER_TABLE.Find(userTableId);
        }
    }
}
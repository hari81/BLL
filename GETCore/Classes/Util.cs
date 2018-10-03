using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Security.Principal;
using System.Security.Claims;
using DAL;

namespace BLL.GETCore.Classes
{
    public class Util
    {
        /// <summary>
        /// Returns the given user id for a Principal user object.
        /// Useful in our API Controller classes to quickly get the user ID making the API request. 
        /// </summary>
        /// <param name="user"></param>
        /// <returns>Long: Corresponding users ID from the USER_TABLE</returns>
        public static long getUserId(IPrincipal user)
        {
            SharedContext context = new SharedContext();

            if (!user.Identity.IsAuthenticated)
                return -1;

            var identity = (ClaimsIdentity) user.Identity;
            IEnumerable<Claim> claims = identity.Claims.Where(m => m.Type == "sub");
            if (claims.Count() == 0)
                return -1;

            string AspNetId = claims.First().Value;
            var users = context.USER_TABLE.Where(m => m.AspNetUserId == AspNetId);

            if (users.Count() == 0)
                return -1;

            return users.First().user_auto;
        }

        public static USER_TABLE getUser(IPrincipal user)
        {
            SharedContext context = new SharedContext();

            if (!user.Identity.IsAuthenticated)
                return null;

            var identity = (ClaimsIdentity)user.Identity;
            IEnumerable<Claim> claims = identity.Claims.Where(m => m.Type == "sub");
            if (claims.Count() == 0)
                return null;

            string AspNetId = claims.First().Value;
            var users = context.USER_TABLE.Where(m => m.AspNetUserId == AspNetId);

            if (users.Count() == 0)
                return null;

            return users.First();
        }

        public static long getUserAutoFromId(string userId)
        {
            using(var context = new SharedContext())
            {
                return context.USER_TABLE.Where(u => u.AspNetUserId == userId).Select(u => u.user_auto).FirstOrDefault();
            }
            return 0;
        }

        public static int getUserIdFromCookie(HttpRequestBase request)
        {
            var cookies = request.Cookies.AllKeys;
            int index = -1;
            for (int k = 0; k < cookies.Length; k++)
            {
                if (cookies[k] == "TTDevUserID") index = k;
            }
            if (index == -1)
            {
                for (int k = 0; k < cookies.Length; k++)
                {
                    if (cookies[k] == "GETCommanderUserID") index = k;
                }
            }
            if (index == -1)
                return 0;
            int userId = 0;
            try
            {
                Int32.TryParse(Security.StringCipher.Decrypt(request.Cookies[index].Value, "VeryComplexPassKey:xD"), out userId);
            }
            catch
            {
                return 0;
            }
            return userId;
        }
    }
}
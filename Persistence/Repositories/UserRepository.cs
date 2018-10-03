using BLL.Core.Repositories;
using BLL.Interfaces;
using DAL;
using BLL.Core.Domain;
using BLL.Extensions;

namespace BLL.Persistence.Repositories
{
    public class UserRepository : Repository<IUser>, IUserRepository
    {
        public UserRepository(UndercarriageContext context) : base(context)
        {

        }
        public IUser GetUserById(int id)
        {
            var DalUser = UndercarriageContext.USER_TABLE.Find(id);
            if (DalUser != null)
                return new User { Id = DalUser.user_auto.LongNullableToInt(), userName = DalUser.username, userStrId = DalUser.userid };
            return null;
        }
        public UndercarriageContext UndercarriageContext
        {
            get { return Context as UndercarriageContext; }
        }
    }

}
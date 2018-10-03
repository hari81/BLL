using BLL.Core.ViewModel;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;



namespace BLL.Core.Domain.UserAccessDomain
{
    public class CustomerAccess:UserAccess
    {
        public CustomerAccess(SharedContext context, System.Security.Principal.IPrincipal LoggedInUser) : base(context)
        {
            Initialized = Init(LoggedInUser);
        }
        public CustomerAccess(SharedContext context) : base(context)
        {

        }

        public CustomerAccess(SharedContext context, int _UserId) : base(context)
        {
            Initialized = Init(_UserId);
        }

        public ResultMessage AddUserToCustomer(int CustomerId, int _UserId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.UserId == _UserId && m.CustomerId == CustomerId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() > 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is already exist in this dealer!", OperationSucceed = false };
            var entity = new USER_CUSTOMER_RELATION { CustomerId = CustomerId, UserId = _UserId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() };
            _domainContext.USER_CUSTOMER_RELATION.Add(entity);
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage { Id = entity.Id, LastMessage = "Operation Succeeded!", OperationSucceed = true, ActionLog = "Operation Succeeded!" };
            }
            catch (Exception ex)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! please check log", OperationSucceed = false, ActionLog = ex.InnerException != null ? ex.Message + "Inner Exception: " + ex.InnerException.Message : ex.Message };
            }
        }



        public List<ResultMessage> AddUserToCustomer(int CustomerId, List<int> _UserIds)
        {
            var result = new List<ResultMessage>();
            if (!Initialized)
            {
                foreach (var _UserId in _UserIds)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" });
                }
                return result;
            }

            var entityRange = new List<USER_CUSTOMER_RELATION>();
            foreach (var _UserId in _UserIds.Distinct())
            {
                var entities = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.UserId == _UserId && m.CustomerId == CustomerId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() > 0)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is already exist for this customer!", OperationSucceed = false });
                    continue;
                }
                entityRange.Add(new USER_CUSTOMER_RELATION { CustomerId = CustomerId, UserId = _UserId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() });
            }
            var addedEntities = _domainContext.USER_CUSTOMER_RELATION.AddRange(entityRange);
            try
            {
                _domainContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string _message = ex.Message;
            }

            foreach (var entity in addedEntities)
            {
                if (entity.Id != 0)
                    result.Add(new ResultMessage { Id = entity.Id, LastMessage = "Operation Succeeded!", OperationSucceed = true, ActionLog = "Operation Succeeded!" });
                else
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! please check log", OperationSucceed = false, ActionLog = "This entity could not be added! This is all we know! :(" });
            }

            return result;
        }

        public ResultMessage RemoveUserFromCustomer(int CustomerId, int _UserId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.UserId == _UserId && m.CustomerId == CustomerId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() == 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is not found or has already been removed!", OperationSucceed = false };
            foreach (var entity in entities)
            {
                entity.RecordStatus = (int)RecordStatus.Deleted;
                entity.ModifiedByUserId = UserId;
                entity.ModifiedDate = DateTime.Now.ToLocalTime();
                _domainContext.Entry(entity).State = System.Data.Entity.EntityState.Modified;
            }
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage { Id = 0, LastMessage = "Operation Succeeded!", OperationSucceed = true, ActionLog = "Operation Succeeded!" };
            }
            catch (Exception ex)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! please check log", OperationSucceed = false, ActionLog = ex.InnerException != null ? ex.Message + "Inner Exception: " + ex.InnerException.Message : ex.Message };
            }
        }
        public ResultMessage RemoveUserFromCustomer(int CustomerId, List<int> _UserIds)
        {
            if (!Initialized)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };
            }
            foreach (var _UserId in _UserIds)
            {
                var entities = _domainContext.USER_CUSTOMER_RELATION.Where(m => m.UserId == _UserId && m.CustomerId == CustomerId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() == 0)
                {
                    //ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer is not found or has already been removed!", OperationSucceed = false });
                    continue;
                }
                foreach (var entity in entities)
                {
                    entity.RecordStatus = (int)RecordStatus.Deleted;
                    entity.ModifiedByUserId = UserId;
                    entity.ModifiedDate = DateTime.Now.ToLocalTime();
                    _domainContext.Entry(entity).State = System.Data.Entity.EntityState.Modified;
                }
            }
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage { Id = 0, LastMessage = "Operation Succeeded!", OperationSucceed = true, ActionLog = "Operation Succeeded!" };
            }
            catch (Exception ex)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! please check log", OperationSucceed = false, ActionLog = ex.InnerException != null ? ex.Message + "Inner Exception: " + ex.InnerException.Message : ex.Message };
            }
        }

        public bool checkUserRelation(int _userId, int _customerId)
        {
            return _domainContext.USER_CUSTOMER_RELATION.Where(m => m.CustomerId == _customerId && m.UserId == _userId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public bool checkDealerRelation(int _dealerId, int _customerId)
        {
            return _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.CustomerId == _customerId && m.DealerId == _dealerId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }
    }
}
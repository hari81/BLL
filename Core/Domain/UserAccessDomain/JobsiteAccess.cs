using System;
using System.Collections.Generic;
using BLL.Core.ViewModel;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Domain.UserAccessDomain
{
    public class JobsiteAccess:UserAccess
    {
        public JobsiteAccess(SharedContext context, System.Security.Principal.IPrincipal LoggedInUser) : base(context)
        {
            Initialized = Init(LoggedInUser);
        }
        public JobsiteAccess(SharedContext context) : base(context)
        {

        }

        public JobsiteAccess(SharedContext context, int _UserId) : base(context)
        {
            Initialized = Init(_UserId);
        }

        public ResultMessage AddUserToJobsite(int JobsiteId, int _UserId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.USER_JOBSITE_RELATION.Where(m => m.UserId == _UserId && m.JobsiteId == JobsiteId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() > 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is already exist in this jobsite!", OperationSucceed = false };
            var entity = new USER_JOBSITE_RELATION { JobsiteId = JobsiteId, UserId = _UserId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() };
            _domainContext.USER_JOBSITE_RELATION.Add(entity);
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



        public List<ResultMessage> AddUserToJobsite(int JobsiteId, List<int> _UserIds)
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

            var entityRange = new List<USER_JOBSITE_RELATION>();
            foreach (var _UserId in _UserIds.Distinct())
            {
                var entities = _domainContext.USER_JOBSITE_RELATION.Where(m => m.UserId == _UserId && m.JobsiteId == JobsiteId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() > 0)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is already exist in this jobsite!", OperationSucceed = false });
                    continue;
                }
                entityRange.Add(new USER_JOBSITE_RELATION { JobsiteId = JobsiteId, UserId = _UserId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() });
            }
            var addedEntities = _domainContext.USER_JOBSITE_RELATION.AddRange(entityRange);
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

        public ResultMessage RemoveUserFromJobsite(int JobsiteId, int _UserId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.USER_JOBSITE_RELATION.Where(m => m.UserId == _UserId && m.JobsiteId == JobsiteId && m.RecordStatus == (int)RecordStatus.Available);
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
        public ResultMessage RemoveUserFromJobsite(int JobsiteId, List<int> _UserIds)
        {
            if (!Initialized)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };
            }
            foreach (var _UserId in _UserIds)
            {
                var entities = _domainContext.USER_JOBSITE_RELATION.Where(m => m.UserId == _UserId && m.JobsiteId == JobsiteId && m.RecordStatus == (int)RecordStatus.Available);
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
        public bool checkUserRelation(int _userId, int _jobsiteId)
        {
            return _domainContext.USER_JOBSITE_RELATION.Where(m => m.JobsiteId == _jobsiteId && m.UserId == _userId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }
    }
}
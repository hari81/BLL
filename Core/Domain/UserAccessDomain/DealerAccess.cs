using BLL.Core.ViewModel;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
namespace BLL.Core.Domain.UserAccessDomain
{
    public class DealerAccess:UserAccess
    {
        public DealerAccess(SharedContext context, System.Security.Principal.IPrincipal LoggedInUser) : base(context)
        {
            Initialized = Init(LoggedInUser);
        }
        public DealerAccess(SharedContext context) : base(context)
        {

        }

        public DealerAccess(SharedContext context, int _UserId) : base(context)
        {
            Initialized = Init(_UserId);
        }

        public ResultMessage AddDealer(DealerV Dealer)
        {
            var entity = new DAL.Dealership { Name = Dealer.Name, Owner = 1 };
            _domainContext.Dealerships.Add(entity);
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage { Id = entity.DealershipId, LastMessage = "Operation Succeeded!", OperationSucceed = true, ActionLog = "Operation Succeeded!: " + Dealer.Name };
            }
            catch (Exception ex)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! please check log", OperationSucceed = false, ActionLog = ex.InnerException != null ? ex.Message + "Inner Exception: " + ex.InnerException.Message : ex.Message };
            }
        }

        public ResultMessage RemoveDealer(int Id)
        {
            var entity = _domainContext.Dealerships.Find(Id);
            if (entity == null)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer not found!", OperationSucceed = false };
            _domainContext.Dealerships.Remove(entity);
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage { Id = entity.DealershipId, LastMessage = "Operation Succeeded!", OperationSucceed = true, ActionLog = "Operation Succeeded!" };
            }
            catch (Exception ex)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! please check log", OperationSucceed = false, ActionLog = ex.InnerException != null ? ex.Message + "Inner Exception: " + ex.InnerException.Message : ex.Message };
            }
        }

        public ResultMessage UpdateDealer(DealerV Dealer)
        {
            var entity = _domainContext.Dealerships.Find(Dealer.Id);
            if (entity == null)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer not found!", OperationSucceed = false };
            entity.Name = Dealer.Name;
            _domainContext.Entry(entity).State = System.Data.Entity.EntityState.Modified;
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage { Id = entity.DealershipId, LastMessage = "Operation Succeeded!", OperationSucceed = true, ActionLog = "Operation Succeeded!" };
            }
            catch (Exception ex)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! please check log", OperationSucceed = false, ActionLog = ex.InnerException != null ? ex.Message + "Inner Exception: " + ex.InnerException.Message : ex.Message };
            }
        }

        public ResultMessage AddJobsiteToDealer(int DealerId, int JobsiteId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.DealerId == JobsiteId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() > 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Jobsite is already exist for this dealer!", OperationSucceed = false };
            var entity = new DEALER_JOBSITE_RELATION { DealerId = DealerId, JobsiteId = JobsiteId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() };
            _domainContext.DEALER_JOBSITE_RELATION.Add(entity);
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



        public List<ResultMessage> AddJobsiteToDealer(int DealerId, List<int> JobsiteIds)
        {
            var result = new List<ResultMessage>();
            if (!Initialized)
            {
                foreach (var jobsiteId in JobsiteIds)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" });
                }
                return result;
            }

            var entityRange = new List<DEALER_JOBSITE_RELATION>();
            foreach (var jobsiteId in JobsiteIds.Distinct())
            {
                var entities = _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.DealerId == DealerId && m.JobsiteId == jobsiteId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() > 0)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer is already exist in this jobsite!", OperationSucceed = false });
                    continue;
                }
                entityRange.Add(new DEALER_JOBSITE_RELATION { DealerId = DealerId, JobsiteId = jobsiteId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() });
            }
            var addedEntities = _domainContext.DEALER_JOBSITE_RELATION.AddRange(entityRange);
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

        public ResultMessage RemoveDealerFromJobsite(int DealerId, int JobsiteId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.JobsiteId == JobsiteId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() == 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer is not found or has already been removed!", OperationSucceed = false };
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
        public ResultMessage RemoveDealerFromJobsite(int DealerId, List<int> JobsiteIds)
        {
            if (!Initialized)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };
            }
            foreach (var JobsiteId in JobsiteIds)
            {
                var entities = _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.JobsiteId == JobsiteId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
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

        public ResultMessage AddUserToDealer(int DealerId, int _UserId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.USER_DEALER_RELATION.Where(m => m.UserId == _UserId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() > 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is already exist in this dealer!", OperationSucceed = false };
            var entity = new USER_DEALER_RELATION { DealerId = DealerId, UserId = _UserId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() };
            _domainContext.USER_DEALER_RELATION.Add(entity);
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



        public List<ResultMessage> AddUserToDealer(int DealerId, List<int> _UserIds)
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

            var entityRange = new List<USER_DEALER_RELATION>();
            foreach (var _UserId in _UserIds.Distinct())
            {
                var entities = _domainContext.USER_DEALER_RELATION.Where(m => m.UserId == _UserId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() > 0)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is already exist in this dealer!", OperationSucceed = false });
                    continue;
                }
                entityRange.Add(new USER_DEALER_RELATION { DealerId = DealerId, UserId = _UserId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() });
            }
            var addedEntities = _domainContext.USER_DEALER_RELATION.AddRange(entityRange);
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

        public ResultMessage RemoveUserFromDealer(int DealerId, int _UserId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.USER_DEALER_RELATION.Where(m => m.UserId == _UserId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
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
        public ResultMessage RemoveUserFromDealer(int DealerId, List<int> _UserIds)
        {
            if (!Initialized)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };
            }
            foreach (var _UserId in _UserIds)
            {
                var entities = _domainContext.USER_DEALER_RELATION.Where(m => m.UserId == _UserId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
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


        public ResultMessage AddCustomerToDealer(int DealerId, int CustomerId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.CustomerId == CustomerId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() > 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Customer is already exist in this dealer group!", OperationSucceed = false };
            var entity = new DEALER_CUSTOMER_RELATION { DealerId = DealerId, CustomerId = CustomerId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() };
            _domainContext.DEALER_CUSTOMER_RELATION.Add(entity);
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



        public List<ResultMessage> AddCustomerToDealer(int DealerId, List<int> CustomerIds)
        {
            var result = new List<ResultMessage>();
            if (!Initialized)
            {
                foreach (var CustomerId in CustomerIds)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" });
                }
                return result;
            }

            var entityRange = new List<DEALER_CUSTOMER_RELATION>();
            foreach (var CustomerId in CustomerIds.Distinct())
            {
                var entities = _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.CustomerId == CustomerId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() > 0)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! Customer is already exist in this dealer group!", OperationSucceed = false });
                    continue;
                }
                entityRange.Add(new DEALER_CUSTOMER_RELATION { DealerId = DealerId, CustomerId = CustomerId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() });
            }
            var addedEntities = _domainContext.DEALER_CUSTOMER_RELATION.AddRange(entityRange);
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

        public ResultMessage RemoveCustomerFromDealer(int DealerId, int CustomerId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.CustomerId == CustomerId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() == 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer is not found or has already been removed!", OperationSucceed = false };
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
        public ResultMessage RemoveCustomerFromDealer(int DealerId, List<int> CustomerIds)
        {
            if (!Initialized)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };
            }
            foreach (var CustomerId in CustomerIds)
            {
                var entities = _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.CustomerId == CustomerId && m.DealerId == DealerId && m.RecordStatus == (int)RecordStatus.Available);
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

        public bool checkUserRelation(int _userId, int _dealerId)
        {
            return _domainContext.USER_DEALER_RELATION.Where(m => m.DealerId == _dealerId && m.UserId == _userId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public bool checkDealerGroupRelation(int _dealerGroupId, int _dealerId)
        {
            return _domainContext.DEALERGROUP_DEALER_RELATION.Where(m => m.DealerGroupId == _dealerGroupId && m.DealerId == _dealerId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public bool checkCustomerRelation(int _customerId, int _dealerId)
        {
            return _domainContext.DEALER_CUSTOMER_RELATION.Where(m => m.DealerId == _dealerId && m.CustomerId == _customerId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public bool checkJobsiteRelation(int _jobsiteId, int _dealerId)
        {
            return _domainContext.DEALER_JOBSITE_RELATION.Where(m => m.DealerId == _dealerId && m.JobsiteId == _jobsiteId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

    }
}
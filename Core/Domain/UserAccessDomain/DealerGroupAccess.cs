using BLL.Core.ViewModel;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Domain.UserAccessDomain
{
    public class DealerGroupAccess : UserAccess
    {
        public DealerGroupAccess(SharedContext context, System.Security.Principal.IPrincipal LoggedInUser) : base(context)
        {
            Initialized = Init(LoggedInUser);
        }
        public DealerGroupAccess(SharedContext context) : base(context)
        {

        }

        public DealerGroupAccess(SharedContext context, int _UserId) : base(context)
        {
            Initialized = Init(_UserId);
        }

        public ResultMessage AddDealerGroup(DealerGroupV DealerGroup)
        {
            var entity = new DEALER_GROUP { Name = DealerGroup.Name };
            _domainContext.DEALER_GROUP.Add(entity);
            try
            {
                _domainContext.SaveChanges();
                return new ResultMessage { Id = entity.Id, LastMessage = "Operation Succeeded!", OperationSucceed = true, ActionLog = "Operation Succeeded!: " + DealerGroup.Name };
            }
            catch (Exception ex)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! please check log", OperationSucceed = false, ActionLog = ex.InnerException != null ? ex.Message + "Inner Exception: " + ex.InnerException.Message : ex.Message };
            }
        }

        public ResultMessage RemoveDealerGroup(int Id)
        {
            var entity = _domainContext.DEALER_GROUP.Find(Id);
            if (entity == null)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer Group not found!", OperationSucceed = false };
            _domainContext.DEALER_GROUP.Remove(entity);
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

        public ResultMessage UpdateDealerGroup(DealerGroupV DealerGroup)
        {
            var entity = _domainContext.DEALER_GROUP.Find(DealerGroup.Id);
            if (entity == null)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer Group not found!", OperationSucceed = false };
            entity.Name = DealerGroup.Name;
            _domainContext.Entry(entity).State = System.Data.Entity.EntityState.Modified;
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

        public ResultMessage AddDealerToDealerGroup(int DealerGroupId, int DealerId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.DEALERGROUP_DEALER_RELATION.Where(m => m.DealerId == DealerId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() > 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer is already exist in this dealer group!", OperationSucceed = false };
            var entity = new DEALERGROUP_DEALER_RELATION { DealerGroupId = DealerGroupId, DealerId = DealerId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() };
            _domainContext.DEALERGROUP_DEALER_RELATION.Add(entity);
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



        public List<ResultMessage> AddDealerToDealerGroup(int DealerGroupId, List<int> DealerIds)
        {
            var result = new List<ResultMessage>();
            if (!Initialized)
            {
                foreach (var DealerId in DealerIds)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" });
                }
                return result;
            }

            var entityRange = new List<DEALERGROUP_DEALER_RELATION>();
            foreach (var DealerId in DealerIds.Distinct())
            {
                var entities = _domainContext.DEALERGROUP_DEALER_RELATION.Where(m => m.DealerId == DealerId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() > 0)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! Dealer is already exist in this dealer group!", OperationSucceed = false });
                    continue;
                }
                entityRange.Add(new DEALERGROUP_DEALER_RELATION { DealerGroupId = DealerGroupId, DealerId = DealerId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() });
            }
            var addedEntities = _domainContext.DEALERGROUP_DEALER_RELATION.AddRange(entityRange);
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

        public ResultMessage RemoveDealerFromDealerGroup(int DealerGroupId, int DealerId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.DEALERGROUP_DEALER_RELATION.Where(m => m.DealerId == DealerId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
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
        public ResultMessage RemoveDealerFromDealerGroup(int DealerGroupId, List<int> DealerIds)
        {
            if (!Initialized)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };
            }
            foreach (var DealerId in DealerIds)
            {
                var entities = _domainContext.DEALERGROUP_DEALER_RELATION.Where(m => m.DealerId == DealerId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
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

        public ResultMessage AddUserToDealerGroup(int DealerGroupId, int _UserId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == _UserId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() > 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is already exist in this dealer group!", OperationSucceed = false };
            var entity = new USER_DEALERGROUP_RELATION { DealerGroupId = DealerGroupId, UserId = _UserId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() };
            _domainContext.USER_DEALERGROUP_RELATION.Add(entity);
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



        public List<ResultMessage> AddUserToDealerGroup(int DealerGroupId, List<int> _UserIds)
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

            var entityRange = new List<USER_DEALERGROUP_RELATION>();
            foreach (var _UserId in _UserIds.Distinct())
            {
                var entities = _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == _UserId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() > 0)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! User is already exist in this dealer group!", OperationSucceed = false });
                    continue;
                }
                entityRange.Add(new USER_DEALERGROUP_RELATION { DealerGroupId = DealerGroupId, UserId = _UserId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() });
            }
            var addedEntities = _domainContext.USER_DEALERGROUP_RELATION.AddRange(entityRange);
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

        public ResultMessage RemoveUserFromDealerGroup(int DealerGroupId, int _UserId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == _UserId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
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
        public ResultMessage RemoveUserFromDealerGroup(int DealerGroupId, List<int> _UserIds)
        {
            if (!Initialized)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };
            }
            foreach (var _UserId in _UserIds)
            {
                var entities = _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.UserId == _UserId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
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


        public ResultMessage AddCustomerToDealerGroup(int DealerGroupId, int CustomerId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.CustomerId == CustomerId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
            if (entities.Count() > 0)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Customer is already exist in this dealer group!", OperationSucceed = false };
            var entity = new DEALERGROUP_CUSTOMER_RELATION { DealerGroupId = DealerGroupId, CustomerId = CustomerId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() };
            _domainContext.DEALERGROUP_CUSTOMER_RELATION.Add(entity);
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



        public List<ResultMessage> AddCustomerToDealerGroup(int DealerGroupId, List<int> CustomerIds)
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

            var entityRange = new List<DEALERGROUP_CUSTOMER_RELATION>();
            foreach (var CustomerId in CustomerIds.Distinct())
            {
                var entities = _domainContext.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.CustomerId == CustomerId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
                if (entities.Count() > 0)
                {
                    result.Add(new ResultMessage { Id = 0, LastMessage = "Operation Failed! Customer is already exist in this dealer group!", OperationSucceed = false });
                    continue;
                }
                entityRange.Add(new DEALERGROUP_CUSTOMER_RELATION { DealerGroupId = DealerGroupId, CustomerId = CustomerId, AddedByUserId = UserId, AddedDate = DateTime.Now.ToLocalTime() });
            }
            var addedEntities = _domainContext.DEALERGROUP_CUSTOMER_RELATION.AddRange(entityRange);
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

        public ResultMessage RemoveCustomerFromDealerGroup(int DealerGroupId, int CustomerId)
        {
            if (!Initialized)
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };

            var entities = _domainContext.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.CustomerId == CustomerId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
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
        public ResultMessage RemoveCustomerFromDealerGroup(int DealerGroupId, List<int> CustomerIds)
        {
            if (!Initialized)
            {
                return new ResultMessage { Id = 0, LastMessage = "Operation Failed! Your user account seems not exist!", OperationSucceed = false, ActionLog = "Operation Failed because this user may have not setup correctly. There is no user in the USER_TABLE associated with this user AspNetId, so initialization failed!" };
            }
            foreach (var CustomerId in CustomerIds)
            {
                var entities = _domainContext.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.CustomerId == CustomerId && m.DealerGroupId == DealerGroupId && m.RecordStatus == (int)RecordStatus.Available);
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

        public bool checkUserRelation(int _userId,  int _dealerGroupId)
        {
            return _domainContext.USER_DEALERGROUP_RELATION.Where(m => m.DealerGroupId == _dealerGroupId && m.UserId == _userId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public bool checkDealerRelation(int _dealerId, int _dealerGroupId)
        {
            return _domainContext.DEALERGROUP_DEALER_RELATION.Where(m => m.DealerGroupId == _dealerGroupId && m.DealerId == _dealerId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }

        public bool checkCustomerRelation(int _customerId, int _dealerGroupId)
        {
            return _domainContext.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.DealerGroupId == _dealerGroupId && m.CustomerId == _customerId && m.RecordStatus == (int)RecordStatus.Available).Count() > 0;
        }
    }
}
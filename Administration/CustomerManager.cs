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

namespace BLL.Administration
{
    public class CustomerManager
    {
        private SharedContext _context;

        public CustomerManager()
        {
            this._context = new SharedContext();
        }

        public List<CustomerOverviewModel> GetAllCustomersForUser(long userId)
        {
            List<CustomerOverviewModel> returnList = new List<CustomerOverviewModel>();
            List<long> customerIds = new List<long>();
            var customers = new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleCustomers();

            foreach (var customer in customers.ToList())
            {
                returnList.Add(new CustomerOverviewModel {
                    CustomerId = customer.customer_auto,
                    CustomerName = customer.cust_name,
                    Email = customer.cust_email,
                    JobsiteCount = _context.CRSF.Where(j => j.customer_auto == customer.customer_auto).Count(),
                    DealershipName = string.Join(", ", customer.DealerGroups.Where(m => m.RecordStatus == (int)Core.Domain.RecordStatus.Available).Select(m => "[" + m.DealerGroup.Name + "]").ToArray()) + " " + string.Join(", ", customer.Dealers.Where(m => m.RecordStatus == (int)Core.Domain.RecordStatus.Available).Select(m => m.Dealer.Name).ToArray())
                });
            }

            return returnList.OrderBy(l => l.CustomerName).ToList();
        }
        /*
         * Returns a list of customers the given user Id has access to view. 
         */
        /*public List<CustomerOverviewModel> GetCustomerListForUser(long userId)
        {
            List<CustomerOverviewModel> returnList = new List<CustomerOverviewModel>();
            List<long> customerIds = new List<long>();
            var userAccess = _context.UserAccessMaps.Where(m => m.user_auto == userId).ToList();

            foreach (var access in userAccess)
            {
                if (access.AccessLevelTypeId == (int)UserAccessTypes.GlobalAdministrator)
                {
                    customerIds.AddRange(_context.CUSTOMER.Select(c => c.customer_auto));
                }
                else if (access.AccessLevelTypeId == (int)UserAccessTypes.DealershipAdministrator)
                {
                    customerIds.AddRange(_context.CUSTOMER.Where(c => c.DealershipId == access.DealershipId).Select(c => c.customer_auto));
                }
                else if(access.AccessLevelTypeId == (int)UserAccessTypes.DealershipUser)
                {
                    if (access.customer_auto != null)
                        customerIds.Add((long)access.customer_auto);
                }
                else if (access.AccessLevelTypeId == (int)UserAccessTypes.CustomerAdministrator || access.AccessLevelTypeId == (int)UserAccessTypes.CustomerUser)
                {
                    customerIds.Add((long)access.customer_auto);
                }
            }

            foreach (var customer in customerIds.Distinct())
            {
                returnList.AddRange(_context.CUSTOMER.Where(c => c.customer_auto == customer).Select(c => new CustomerOverviewModel()
                {
                    CustomerId = c.customer_auto,
                    CustomerName = c.cust_name,
                    Email = c.cust_email,
                    JobsiteCount = _context.CRSF.Where(j => j.customer_auto == c.customer_auto).Count(),
                    DealershipName = c.Dealership.Name
                }));
            }

            return returnList.OrderBy(l => l.CustomerName).ToList();
        }*/

        public async Task<List<CustomerOverviewModel>> GetAllCustomersForDealership(int dealershipId, System.Security.Principal.IPrincipal User)
        {
            return await new Core.Domain.UserAccess(new SharedContext(), User).getAccessibleCustomers().OrderBy(c => c.cust_name).Where(c => c.DealershipId == dealershipId)
                .Select(c => new CustomerOverviewModel
                {
                    CustomerId = c.customer_auto,
                    CustomerName = c.cust_name,
                    DealershipName = c.Dealership.Name,
                    Email = c.cust_email,
                    JobsiteCount = _context.CRSF.Where(j => j.customer_auto == c.customer_auto).Count()
                }).ToListAsync();
        }

        public async Task<NewCustomerModel> GetCustomerDetails(long customerId, System.Security.Principal.IPrincipal User)
        {
            var customer = await new Core.Domain.UserAccess(new SharedContext(), User).getAccessibleCustomers().Where(c => c.customer_auto == customerId).FirstOrDefaultAsync();
            if (customer == null)
                return null;
            string logo = "";
            try {
                logo = Convert.ToBase64String(customer.logo);
            } catch
            {
                logo = null;
            }
            var dealergroupRecord = _context.DEALERGROUP_CUSTOMER_RELATION.Where(m => m.CustomerId == customerId && m.RecordStatus == (int)Core.Domain.RecordStatus.Available);

            var response = new NewCustomerModel()
            {
                Address = customer.fullAddress,
                CustomerName = customer.cust_name,
                DealershipId = customer.DealershipId,
                DealerGroupId = dealergroupRecord.Count() == 1 ? dealergroupRecord.FirstOrDefault().DealerGroupId : 0,
                Email = customer.cust_email,
                Logo = logo,
                PhoneNumber = customer.cust_phone,
                ReportStyleId = customer.SelectedReportId == null ? 0 : (int)customer.SelectedReportId,
                QuoteReportStyleId = customer.QuoteReportStyle == null ? 0 : (int)customer.QuoteReportStyle,
                HourlyLabourCost = customer.DefaultHourlyRate,
            };
            return response;
        }

        public Tuple<long, string> UpdateCustomer(UpdateCustomerModel customer)
        {
            string[] LogoArr = customer.Logo.Split(',');
            string customerLogo = "";
            if (LogoArr.Length > 1)
                customerLogo = LogoArr[1];
            int? reportId = null;
            if (customer.ReportStyleId != 0)
                reportId = customer.ReportStyleId;
            var customerEntity = _context.CUSTOMER.Where(c => c.customer_auto == customer.CustomerId).FirstOrDefault();
            if (customerEntity == null)
                return Tuple.Create(Convert.ToInt64(-1), "Couldn't find a customer with this ID.");

            customerEntity.cust_name = customer.CustomerName;
            customerEntity.custid = customer.CustomerName;
            customerEntity.DealershipId = customer.DealershipId;
            customerEntity.cust_phone = customer.PhoneNumber;
            customerEntity.cust_email = customer.Email;
            customerEntity.fullAddress = customer.Address;
            if(customerLogo.Trim() != "")
                customerEntity.logo = Convert.FromBase64String(customerLogo);
            customerEntity.SelectedReportId = customer.ReportStyleId;
            customerEntity.QuoteReportStyle = customer.QuoteReportStyleId;
            customerEntity.DefaultHourlyRate = customer.HourlyLabourCost;

            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return Tuple.Create(Convert.ToInt64(-1), e.Message);
            }

            return Tuple.Create(customerEntity.customer_auto, "Successfully updated customer. ");
        }

        public Tuple<long, string> AddNewCustomer(NewCustomerModel customer, int _userId)
        {
            if (!checkCustomerNameIsUnique(customer.CustomerName))
                return Tuple.Create(Convert.ToInt64(-1), "A customer with this name already exists. ");

            string[] LogoArr = customer.Logo.Split(',');
            string customerLogo = "";
            if (LogoArr.Length > 1)
                customerLogo = LogoArr[1];
            int? reportId = null;
            if (customer.ReportStyleId != 0)
                reportId = customer.ReportStyleId;
            CUSTOMER newCustomer = new CUSTOMER()
            {
                cust_name = customer.CustomerName,
                custid = customer.CustomerName,
                active = true,
                billing_address = false,
                Showlimits = false,
                DealershipId = customer.DealershipId,
                cust_phone = customer.PhoneNumber,
                cust_email = customer.Email,
                created_date = DateTime.UtcNow,
                CreatedByUserId = customer.CreatedByUserId,
                fullAddress = customer.Address,
                labonly = false,
                logo = customerLogo != "" ? Convert.FromBase64String(customerLogo) : null,
                SelectedReportId = reportId,
                QuoteReportStyle = customer.QuoteReportStyleId,
                DefaultHourlyRate = customer.HourlyLabourCost,
            };

            _context.CUSTOMER.Add(newCustomer);

            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return Tuple.Create(Convert.ToInt64(-1), e.Message);
            }
            /*
            long[] userIds = _context.UserAccessMaps.Where(a => (a.DealershipId == customer.DealershipId && a.AccessLevelTypeId == (int)UserAccessTypes.DealershipAdministrator)
                                                        || a.AccessLevelTypeId == (int)UserAccessTypes.GlobalAdministrator)
                                                        .Where(m => m.user_auto != null).Select(u => (long)u.user_auto).ToArray();

            foreach (long userId in userIds)
            {
                USER_CRSF_CUST_EQUIP accessRecord = new USER_CRSF_CUST_EQUIP()
                {
                    user_auto = userId,
                    customer_auto = newCustomer.customer_auto,
                    level_type = 1,
                    modified_user = "AUTO INSERT FROM CUSTOMER SETUP"
                };
                _context.USER_CRSF_CUST_EQUIP.Add(accessRecord);
            }
            
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return Tuple.Create(Convert.ToInt64(-1), e.Message);
            }*/
            if (customer.DealerGroupId != 0)
            {
            var result = new BLL.Core.Domain.UserAccessDomain.DealerGroupAccess(new SharedContext(), _userId).AddCustomerToDealerGroup(customer.DealerGroupId, newCustomer.customer_auto.LongNullableToInt());
            if (result.OperationSucceed)
            return Tuple.Create(newCustomer.customer_auto, "Successfully created new customer. ");
            else
                return Tuple.Create(newCustomer.customer_auto, "Operation completed with warning! This customer could not be registered for the selected dealer group!");
            }else if (customer.DealershipId != 0)
            {
                var result = new BLL.Core.Domain.UserAccessDomain.DealerAccess(new SharedContext(), _userId).AddCustomerToDealer(customer.DealershipId, newCustomer.customer_auto.LongNullableToInt());
                if (result.OperationSucceed)
                    return Tuple.Create(newCustomer.customer_auto, "Successfully created new customer. ");
                else
                    return Tuple.Create(newCustomer.customer_auto, "Operation completed with warning! This customer could not be registered for the selected dealer !");
            }
            return Tuple.Create(Convert.ToInt64(-1), "Please select dealer or dealer group! ");
        }

        /// <summary>
        /// Checks if the given customer name is already in use. Returns true if the name is not yet used by another customer. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if the given name does not yet exist in the system. </returns>
        public bool checkCustomerNameIsUnique(string name)
        {
            int existingCustomersWithName = -1;

            existingCustomersWithName = _context.CUSTOMER.Where(c => c.cust_name == name || c.custid == name).Count();

            if (existingCustomersWithName == 0)
                return true;
            return false;
        }
    }
}
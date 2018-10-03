using BLL.Extensions;
using DAL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BLL.GETCore.Classes
{
    public class CustomerManagement
    {
        /// <summary>
        /// Returns a list of all customers that belong to a given dealership. 
        /// </summary>
        /// <param name="dealershipId"></param>
        /// <returns>A list of customer objects. Each object contains the customer's id, name, contactEmail, isActive and jobsiteCount. </returns>
        
        public List<BasicCustomerDataSet> getListOfCustomersForDealership(int dealershipId, System.Security.Principal.IPrincipal User)
        {
            var customers = new BLL.Core.Domain.UserAccess(new SharedContext(), User).getAccessibleCustomers();
            using(var context = new SharedContext())
            {
                return customers.OrderBy(c => c.cust_name).Where(c => c.DealershipId == dealershipId)
                    .Select(c => new BasicCustomerDataSet
                    {
                        id = c.customer_auto,
                        name = c.cust_name,
                        contactEmail = c.cust_email,
                        isActive = c.active,
                        jobsiteCount = context.CRSF.Where(j => j.customer_auto == c.customer_auto).Count()
                    }).ToList();
            }
        }
        
        /// <summary>
        /// Checks if the given customer name is already in use. Returns true if the name is not yet used by another customer. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>True if the given name does not yet exist in the system. </returns>
        public bool checkCustomerNameIsUnique(string name)
        {
            int existingCustomersWithName = -1;
            using (var context = new SharedContext())
            {
                existingCustomersWithName = context.CUSTOMER.Where(c => c.cust_name == name || c.custid == name).Count();
            }

            if (existingCustomersWithName == 0)
                return true;
            return false;
        }

        /// <summary>
        /// Checks if the given dealership id references a real dealership. Returns true if the dealership exists. 
        /// </summary>
        /// <param name="dealershipId"></param>
        /// <returns>Returns true if the dealership exists. </returns>
        public bool doesDealershipExist(int dealershipId)
        {
            using (var context = new SharedContext())
            {
                var dealershipExists = context.Dealerships.Find(dealershipId);
                if (dealershipExists == null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Gets a list of all customers the logged in user has access to. Use this method to populate dropdowns on various pages 
        /// when the user needs to select an equipment. Returns a list of objects with 2 values (customerId, customerName). 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>Returns a list of objects with 2 values (customerId, customerName). </returns>
        public List<CustomerIdAndNameDataSet> getListOfCustomersForLoggedInUser(long userId)
        {
            return new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleCustomersExtended().Select(m => new CustomerIdAndNameDataSet { customerId = m.customer_auto, customerName = m.cust_name }).ToList();
        }

        /// <summary>
        /// Gets a list of active customers the logged in user has access to. Use this method to populate dropdowns on various pages 
        /// when the user needs to select an equipment. Returns a list of objects with 2 values (customerId, customerName). 
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>Returns a list of objects with 2 values (customerId, customerName). </returns>
        public List<CustomerIdAndNameDataSet> getListOfActiveCustomersForLoggedInUser(long userId)
        {
            return new BLL.Core.Domain.UserAccess(new SharedContext(), userId.LongNullableToInt()).getAccessibleCustomersExtended().Select(m => new CustomerIdAndNameDataSet { customerId = m.customer_auto, customerName = m.cust_name }).ToList();
        }

        /// <summary>
        /// Given a customerId this will return the id of the dealership that customer belongs to. 
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns>The dealershipId the given customer belongs to. </returns>
        public int getCustomersDealershipId(long customerId, System.Security.Principal.IPrincipal User)
        {
            int id = -1;
            if (!new BLL.Core.Domain.UserAccess(new SharedContext(), User).hasAccessToCustomer(customerId.LongNullableToInt()))
                return -1;
            using (var context = new SharedContext())
            {
                var customer = context.CUSTOMER.Find(customerId);
                if(customer != null)
                    id = customer.Dealership.DealershipId;
            }
            return id;
        }
        
        /// <summary>
        /// Returns an object containing all relevent details about a given customer id. 
        /// Used on the customer details view of the user management page. 
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns>An object containing a given customers details. </returns>
        public CustomerDetailsDataSet getCustomerDetails(long customerId, System.Security.Principal.IPrincipal User)
        {
            var currentCustomer = new Core.Domain.UserAccess(new SharedContext(), User).getAccessibleCustomers().Where(m=> m.customer_auto == customerId).FirstOrDefault();
            if (currentCustomer == null)
                return null;
            using (var context = new SharedContext())
            {
                string customerLogo = "";
                try { customerLogo = Convert.ToBase64String(currentCustomer.logo); } catch { }
                var k = new CustomerDetailsDataSet
                    {
                        name = currentCustomer.cust_name,
                        email = currentCustomer.cust_email,
                        fullAddress = currentCustomer.fullAddress,
                        phoneNumber = currentCustomer.cust_phone,
                        mobileNumber = currentCustomer.cust_mobile,
                        logoBase64 = customerLogo,
                        reportId = currentCustomer.SelectedReportId == null ? 0 : (int)currentCustomer.SelectedReportId,
                        
                };
                return k;
            }
        }

        public CustomerDetailsDataSet getCustomerDetails(long customerId, int User)
        {
            var currentCustomer = new Core.Domain.UserAccess(new SharedContext(), User).getAccessibleCustomers().Where(m => m.customer_auto == customerId).FirstOrDefault();
            if (currentCustomer == null)
                return null;
            using (var context = new SharedContext())
            {
                string customerLogo = "";
                try { customerLogo = Convert.ToBase64String(currentCustomer.logo); } catch { }
                var k = new CustomerDetailsDataSet()
                {
                    name = currentCustomer.cust_name,
                    email = currentCustomer.cust_email,
                    fullAddress = currentCustomer.fullAddress,
                    phoneNumber = currentCustomer.cust_phone,
                    mobileNumber = currentCustomer.cust_mobile,
                    logoBase64 = customerLogo,
                    reportId = currentCustomer.SelectedReportId == null ? 0 : (int)currentCustomer.SelectedReportId
                };
                return k;
            }
        }
        /// <summary>
        /// Given an object which contains details about a customer, this will update that customers details accordingly. 
        /// Used on the customer details area of the user management page.
        /// </summary>
        /// <param name="customerData"></param>
        /// <returns>A response message containing whether or not the update was successful, and a message. </returns>
        public GETResponseMessage updateCustomerDetails(UpdateCustomerDataSet customerData, System.Security.Principal.IPrincipal User)
        {
            var customer = new Core.Domain.UserAccess(new SharedContext(), User).getAccessibleCustomers().Where(m => m.customer_auto == customerData.customerId).FirstOrDefault();
            if (customer == null)
                return new GETResponseMessage(ResponseTypes.Failed, "Access is denied!");

            using (var context = new SharedContext())
            {

                if(customer == null)
                    return new GETResponseMessage(ResponseTypes.Failed, "The given customer ID doesn't exist. ");
                if(!doesDealershipExist(customerData.dealershipId))
                    return new GETResponseMessage(ResponseTypes.Failed, "The given dealership ID doesn't exist. ");
                if (customerData.customerName == "")
                    return new GETResponseMessage(ResponseTypes.InvalidInputs, "Missing required data. ");
                if(customerData.customerName != customer.cust_name && !checkCustomerNameIsUnique(customerData.customerName))
                    return new GETResponseMessage(ResponseTypes.Failed, "Customer name is not unique. ");

                string[] LogoArr = customerData.logoBase64.Split(',');
                string customerLogo = "";
                if (LogoArr.Length > 1)
                    customerLogo = LogoArr[1];

                int? reportId = null;
                if (customerData.reportId != 0)
                    reportId = customerData.reportId;

                try
                {
                    customer.cust_name = customerData.customerName;
                    customer.custid = customerData.customerName;
                    customer.DealershipId = customerData.dealershipId;
                    customer.cust_street = customerData.streetNumber + " " + customerData.streetAddress;
                    customer.cust_suburb = customerData.city;
                    customer.cust_postcode = customerData.postCode;
                    customer.cust_state = customerData.state;
                    customer.cust_phone = customerData.phoneNumber;
                    customer.cust_country = customerData.country;
                    customer.cust_mobile = customerData.mobileNumber;
                    customer.cust_email = customerData.emailAddress;
                    customer.modified_date = DateTime.UtcNow;
                    customer.modified_user = customerData.authUserId.ToString(); // Once we merge in with uc, need to fix this. 
                    customer.fullAddress = customerData.fullAddress;
                    customer.logo = Convert.FromBase64String(customerLogo);
                    customer.SelectedReportId = reportId;
                    context.SaveChanges();
                    return new GETResponseMessage(ResponseTypes.Success, "Customer updated successfully. ");
                } catch (Exception e)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, "Database error. Failed to save changes. " + e.Message);
                }
            }
        }

        /// <summary>
        /// Creates a new customer given a customerData object which contains all relevent details about that customer. 
        /// Returns a ResponseMessage object which contains the success status, and the id of the new customer if successful.  
        /// </summary>
        /// <param name="customerData"></param>
        /// <returns></returns>
        public GETResponseMessage createNewCustomer(CreateNewCustomerDataSet customerData)
        {
            if(!checkCustomerNameIsUnique(customerData.customerName))
                return new GETResponseMessage(ResponseTypes.Failed, "Customer name is not unique. ");

            if(!doesDealershipExist(customerData.dealershipId))
                return new GETResponseMessage(ResponseTypes.Failed, "Dealership ID not found. ");

            if(customerData.customerName == "")
                return new GETResponseMessage(ResponseTypes.InvalidInputs, "Missing required data. ");

            string[] LogoArr = customerData.logoBase64.Split(',');
            string customerLogo = "";
            if (LogoArr.Length > 1)
                customerLogo = LogoArr[1];
            int? reportId = null;
            if (customerData.reportId != 0)
                reportId = customerData.reportId;
            CUSTOMER newCustomer = new CUSTOMER()
            {
                cust_name = customerData.customerName,
                custid = customerData.customerName,
                active = true,
                billing_address = false,
                Showlimits = false,
                DealershipId = customerData.dealershipId,
                cust_street = customerData.streetNumber + " " + customerData.streetAddress,
                cust_suburb = customerData.city,
                cust_postcode = customerData.postCode,
                cust_state = customerData.state,
                cust_phone = customerData.phoneNumber,
                cust_country = customerData.country,
                cust_mobile = customerData.mobileNumber,
                cust_email = customerData.emailAddress,
                created_date = DateTime.UtcNow,
                CreatedByUserId = customerData.authUserId,
                fullAddress = customerData.fullAddress,
                labonly = false,
                logo = Convert.FromBase64String(customerLogo),
                SelectedReportId = reportId
            };

            using (var context = new SharedContext())
            {
                context.CUSTOMER.Add(newCustomer);
                
                try
                {
                    context.SaveChanges();
                }
                catch(Exception e)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, e.Message);
                }
            }

            using(var context = new SharedContext())
            {
                long[] userIds = context.UserAccessMaps.Where(a => (a.DealershipId == customerData.dealershipId && a.AccessLevelTypeId == (int) UserAccessTypes.DealershipAdministrator)
                                                            || a.AccessLevelTypeId == (int) UserAccessTypes.GlobalAdministrator)
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
                    context.USER_CRSF_CUST_EQUIP.Add(accessRecord);
                }

                try
                {
                    context.SaveChanges();
                } catch(Exception e)
                {
                    return new GETResponseMessage(ResponseTypes.Failed, e.Message);
                }
            }

            return new GETResponseMessage(ResponseTypes.Success, newCustomer.customer_auto.ToString());
        }
    }

    public class CustomerIdAndNameDataSet
    {
        public long customerId { get; set; }
        public string customerName { get; set; }
    }

    public class BasicCustomerDataSet
    {
        public long id { get; set; }
        public string name { get; set; }
        public string contactEmail { get; set; }
        public bool isActive { get; set; }
        public int jobsiteCount { get; set; }
    }

    public class CustomerDetailsDataSet
    {
        public string name { get; set; }
        public string email { get; set; }
        public string fullAddress { get; set; }
        //public int dealershipId { get; set; }
        //public string dealershipName { get; set; }
        public string phoneNumber { get; set; }
        public string mobileNumber { get; set; }
        public string logoBase64 { get; set; }
        public int reportId { get; set; }
        
    }

    public class UpdateCustomerDataSet
    {
        public long customerId { get; set; }
        public string customerName { get; set; }
        public string fullAddress { get; set; }
        public int? subPremise { get; set; }
        public int streetNumber { get; set; }
        public string streetAddress { get; set; }
        public string city { get; set; }
        public string postCode { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string phoneNumber { get; set; }
        public string mobileNumber { get; set; }
        public string emailAddress { get; set; }
        public int dealershipId { get; set; }
        public long authUserId { get; set; }
        public string logoBase64 { get; set; }
        public int reportId { get; set; }
    }

    public class CreateNewCustomerDataSet
    {
        public string customerName { get; set; }
        public string fullAddress { get; set; }
        public int? subPremise { get; set; }
        public int streetNumber { get; set; }
        public string streetAddress { get; set; }
        public string city { get; set; }
        public string postCode { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string phoneNumber { get; set; }
        public string mobileNumber { get; set; }
        public string emailAddress { get; set; }
        public int dealershipId { get; set; }
        public long authUserId { get; set; }
        public string logoBase64 { get; set; }
        public int reportId { get; set; }
    }

}
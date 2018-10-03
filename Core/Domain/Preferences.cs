using BLL.Core.ViewModel;
using BLL.Extensions;
using BLL.Services;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.Domain
{
    public class Preferences
    {
        private SharedContext _context;
        private USER_TABLE _user;
        private AspNetUsers _aspUser;

        public Preferences(SharedContext context, long userId)
        {
            _context = context;
            _user = _context.USER_TABLE.Find(userId);
            _aspUser = _context.AspNetUsers.Find(_user.AspNetUserId);
        }

        public PreferencesViewModel GetPreferences()
        {
            PreferencesViewModel p = new PreferencesViewModel();
            p.Country = _user.country;
            p.CurrencyId = _user.currency_auto;
            p.CurrencyName = "Australian Dollar"; // Need to import currency table into DAL
            p.HomePageId = 0;
            p.HomePageName = "Dashboard";
            p.LanguageId = _user.language_auto;
            p.LanguageName = _user.Language.Fulllanguage;
            p.FullName = _user.username;
            p.PhoneNumber = _user.phone_number;
            p.PostCode = _user.postcode;
            p.State = _user.state;
            p.StreetAddress = _user.street1;
            p.Suburb = _user.suburb;
            p.UnitOfMeasurementName = _user.track_uom;
            return p;
        }

        /// <summary>
        /// Returns the user preferences for GET.
        /// </summary>
        /// <returns></returns>
        public UserPreferencesViewModel GetUserPreferences()
        {
            UserPreferencesViewModel p = new UserPreferencesViewModel();
            p.Language = _user.language_auto;
            p.Currency = _user.currency_auto;
            p.UnitOfMeasurementName = _user.track_uom;
            p.Email = _user.email;
            p.Mobile = _user.mobile;
            p.PhoneNumber = _user.phone_number;
            p.PhoneAreaCode = _user.phone_area_code;
            p.Fax = _user.fax_number;
            p.FaxAreaCode = _user.fax_area_code;
            p.Address = _user.street1;
            p.Address2 = _user.street2;
            p.Suburb = _user.suburb;
            p.PostCode = _user.postcode;
            p.State = _user.state;
            p.Country = _user.country;
            return p;
        }

        public Tuple<bool, string> UpdateLanguage(byte languageId)
        {
            _user.language_auto = languageId;
            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Language updated successfully. ");
            } catch(Exception e)
            {
                return Tuple.Create(false, "Failed to update language. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdateHomePage(int homePageId)
        {
            throw new NotImplementedException();
        }

        public Tuple<bool, string> UpdateUnitOfMeasurement(string uom)
        {
            if (uom != "mm" && uom != "inch")
                return Tuple.Create(false, "Invalid unit of measurement. Must be 'mm' or 'inch'. ");
            _user.track_uom = uom;
            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Unit of measurement updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update unit of measurement. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdateEmail(string emailAddress)
        {
            ValidateEmail v = new ValidateEmail();
            if (!v.IsValidEmail(emailAddress))
                return Tuple.Create(false, "Invalid email address. ");
            if (_context.USER_TABLE.Where(u => u.email == emailAddress).Count() > 0)
                return Tuple.Create(false, "This email is taken. Your email must be unique. ");
            if (_context.AspNetUsers.Where(u => u.Email == emailAddress).Count() > 0)
                return Tuple.Create(false, "This email is taken. Your email must be unique. ");

            _user.email = emailAddress;
            _aspUser.Email = emailAddress;
            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Email updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update email address. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdateFullName(string name)
        {
            if (name.Length < 2)
                return Tuple.Create(false, "Name must be at least 2 characters long. ");
            _user.username = name;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Full name updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update full name. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdateUsername(string username)
        {
            if (username.Length < 3)
                return Tuple.Create(false, "Username must be at least 3 characters long. ");
            if(_context.USER_TABLE.Where(u => u.userid == username).Count() > 0)
                return Tuple.Create(false, "This username is taken. Your username must be unique. ");
            if (_context.AspNetUsers.Where(u => u.UserName == username).Count() > 0)
                return Tuple.Create(false, "This username is taken. Your username must be unique. ");

            _user.userid = username;
            _aspUser.UserName = username;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Username updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update username. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdatePhoneNumber(string number)
        {
            _user.phone_number = number;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Phone number updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update phone number. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdateStreetAddress(string address)
        {
            _user.street1 = address;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Street address updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update street address. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdateSuburb(string suburb)
        {
            _user.suburb = suburb;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Suburb updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update suburb. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdateState(string state)
        {
            _user.state = state;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "State updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update state. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdatePostCode(string code)
        {
            _user.postcode = code;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Post code updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update post code. " + e.ToDetailedString());
            }
        }

        public Tuple<bool, string> UpdateCountry(string country)
        {
            _user.country = country;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Country updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update country. " + e.ToDetailedString());
            }
        }

        public List<CurrencyViewModel> GetAllCurrencies()
        {
            return _context.Currencies.Select(c => new CurrencyViewModel()
            {
                Id = c.currency_auto,
                Code = c.currency_code,
                Name = c.currency_name
            }).ToList();
        }

        public Tuple<bool, string> UpdateCurrency(int currencyId)
        {
            _user.currency_auto = currencyId;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Currency updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update currency. " + e.ToDetailedString());
            }
        }

        public List<LanguageViewModel> GetAllLanguages()
        {
            return _context.LANGUAGE.Select(c => new LanguageViewModel()
            {
                Id = c.language_auto,
                Name = c.Fulllanguage
            }).ToList();
        }

        public string GetUsername()
        {
            return _user.userid;
        }

        public string GetEmail()
        {
            return _user.email;
        }

        private string ConvertUomToFullName(string uom)
        {
            switch(uom)
            {
                case "mm": return "Millimetres (mm)";
                case "inch": return "Inches (in)";
                default: return "Not Set";
            }
        }

        public Tuple<bool, string> SaveUserPreferences(UserPreferencesViewModel p)
        {
            Tuple<bool, string> result;

            result = SaveRegionalSettings(p);
            if(result.Item1)
            {
                result = SaveContactDetails(p);
            }
            
            return (result);
        }

        private Tuple<bool, string> SaveRegionalSettings(UserPreferencesViewModel p)
        {
            Tuple<bool, string> result;

            result = UpdateLanguage((byte)p.Language);
            if (result.Item1)
            {
                result = UpdateCurrency(p.Currency);
                if (result.Item1)
                {
                    result = UpdateUnitOfMeasurement(p.UnitOfMeasurementName);
                }
            }

            return (result);
        }

        private Tuple<bool, string> SaveContactDetails(UserPreferencesViewModel p )
        {
            Tuple<bool, string> result = ValidateInputs(p);
            if(!result.Item1)
            {
                return result;
            }

            _user.mobile = p.Mobile;
            _user.phone_area_code = p.PhoneAreaCode;
            _user.phone_number = p.PhoneNumber;
            _user.fax_area_code = p.FaxAreaCode;
            _user.fax_number = p.Fax;
            _user.street1 = p.Address;
            _user.street2 = p.Address2;
            _user.suburb = p.Suburb;
            _user.postcode = p.PostCode;
            _user.state = p.State;
            _user.country = p.Country;

            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Details updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update contact details. " + e.ToDetailedString());
            }
        }

        private Tuple<bool, string> ValidateInputs(UserPreferencesViewModel p)
        {
            bool result = true;
            string error = "";

            Regex rgx1 = new Regex("[A-Za-z0-9 ]+$");
            Regex rgx2 = new Regex("[A-Za-z ]+$");
            Regex rgx3 = new Regex("[0-9 ]+$");

            try
            {
                if (!rgx3.IsMatch(p.Mobile) && p.Mobile.Length > 0)
                {
                    result = false;
                    error = "Mobile number must only contain numeric characters. ";
                }

                if (result && ((!rgx3.IsMatch(p.PhoneAreaCode) && p.PhoneAreaCode.Length > 0)
                    || (!rgx3.IsMatch(p.PhoneNumber) && p.PhoneNumber.Length > 0)))
                {
                    result = false;
                    error = "Phone number must only contain numeric characters. ";
                }

                if (result && ((!rgx3.IsMatch(p.FaxAreaCode) && p.FaxAreaCode.Length > 0)
                    || (!rgx3.IsMatch(p.Fax) && p.Fax.Length > 0)))
                {
                    result = false;
                    error = "Fax number must only contain numeric characters. ";
                }

                if (result && !rgx3.IsMatch(p.PostCode) && p.PostCode.Length > 0)
                {
                    result = false;
                    error = "Post code must only contain numeric characters. ";
                }

                if (result && !rgx2.IsMatch(p.State) && p.State.Length > 0)
                {
                    result = false;
                    error = "Invalid characters specified for State. ";
                }

                if (result && !rgx2.IsMatch(p.Country) && p.Country.Length > 0)
                {
                    result = false;
                    error = "Invalid characters specified for Country. ";
                }
            }
            catch (Exception e)
            {
                result = false;
                error = "Validation failed for one or more inputs.";
            } 

            return new Tuple<bool, string>(result, error);
        }
    }
}
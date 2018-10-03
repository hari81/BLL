using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ViewModel
{
    public class PreferencesViewModel
    {
        public string LanguageName { get; set; }
        public int LanguageId { get; set; }
        public string HomePageName { get; set; }
        public int HomePageId { get; set; }
        public string CurrencyName { get; set; }
        public int CurrencyId { get; set; }
        public string UnitOfMeasurementName { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string StreetAddress { get; set; }
        public string Suburb { get; set; }
        public string PostCode { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }

    public class CurrencyViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class LanguageViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class UserPreferencesViewModel
    {
        public int Language { get; set; }
        public int Currency { get; set; }
        public string UnitOfMeasurementName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string PhoneNumber { get; set; }
        public string PhoneAreaCode { get; set; }
        public string Fax { get; set; }
        public string FaxAreaCode { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public string Suburb { get; set; }
        public string PostCode { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
    }
}
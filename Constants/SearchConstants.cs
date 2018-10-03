using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.ViewModel;
namespace BLL.Constants
{
    public static class SearchModel
    {
        public static List<SearchItem> SearchItems = new List<SearchItem> {
            new SearchItem { Id = 1, Title = "Customer" },
            new SearchItem { Id = 2, Title = "Jobsite" },
            new SearchItem { Id = 3, Title = "Equipment" },
            new SearchItem { Id = 4, Title = "Family" },
            new SearchItem { Id = 5, Title = "Make" },
            new SearchItem { Id = 6, Title = "Model" },
            new SearchItem { Id = 7, Title = "System" },
            new SearchItem { Id = 8, Title = "Evaluation A" },
            new SearchItem { Id = 9, Title = "Evaluation B" },
            new SearchItem { Id = 10, Title = "Evaluation C" },
            new SearchItem { Id = 11, Title = "Evaluation X" }
        };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
namespace BLL.Core.ViewModel
{
    /// <summary>
    /// SearchItem Id is defined in Types.cs as SearchItemType
    ///{ Id: 1, Title: "Customer" },
    ///{ Id: 2, Title: "Jobsite" },
    ///{ Id: 3, Title: "Equipment" },
    ///{ Id: 4, Title: "Family" },
    ///{ Id: 5, Title: "Make" },
    ///{ Id: 6, Title: "Model" },
    ///{ Id: 7, Title: "System" },
    ///{ Id: 8, Title: "Evaluation A" },
    ///{ Id: 9, Title: "Evaluation B" },
    ///{ Id: 10, Title: "Evaluation C" },
    ///{ Id: 11, Title: "Evaluation X" },
    ///{ Id: 12, Title: "Start Date" },
    ///{ Id: 13, Title: "End Date" }
    /// 
    /// </summary>
    public class SearchItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int SearchId { get; set; } = 0;
        public string SearchStr { get; set; } = "";
    }
    public class SearchResult {
        public List<IdAndDate> Result { get; set; }
        public int Total { get; set; }
    }
    public class SearchFavorite {
        public int Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
        public string BackgroundColor { get; set; }
        public string TextColor { get; set; }
        public List<SearchItem> SearchItems { get; set; }
    }

    public class SearchFavoriteOperation {
        public List<SearchFavorite> SearchFavorites { get; set; }
        public ResultMessage ResultMessage { get; set; }
    }
}
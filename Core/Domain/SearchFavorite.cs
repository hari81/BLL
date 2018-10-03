using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using DAL;
using BLL.Extensions;
using System.Threading.Tasks;

namespace BLL.Core.Domain
{
    public class SearchFavorite:UCDomain
    {
        public SearchFavorite(DAL.UndercarriageContext context) : base(context) {

        }

        public List<Core.ViewModel.SearchFavorite> getUserFavorites(int UserId) {
            return _domainContext.SearchFavorite.Where(m => m.UserId == UserId).Select(m => new Core.ViewModel.SearchFavorite {
                Id = m.Id,
                Name = m.Name,
                UserId = m.UserId,
                BackgroundColor = m.BackgroundColor,
                TextColor = m.TextColor,
                SearchItems = m.SearchFavoriteItems.Select(k=> new Core.ViewModel.SearchItem {
                    Id = k.ItemId,
                    SearchId = k.SearchId,
                    SearchStr = k.SearchStr,
                    Title = k.SearchStr
                }).ToList()
            }).ToList();
        }

        public Core.ViewModel.SearchFavoriteOperation addSearchFavorite(Core.ViewModel.SearchFavorite favorite, int userId) {
            ResultMessage result = new ResultMessage
            {
                Id = 0,
                OperationSucceed = true,
                LastMessage = "Operation was successfull.",
                ActionLog = "Operation was successfull."
            };
            var favEntity = new DAL.SearchFavorite {
                Name = favorite.Name,
                UserId = userId,
                BackgroundColor = favorite.BackgroundColor,
                TextColor = favorite.TextColor,
            };
            _domainContext.SearchFavorite.Add(favEntity);
            _domainContext.SearchFavoriteItems.AddRange(favorite.SearchItems.Select(m => new SearchFavoriteItems {
                ItemId = m.Id,
                SearchFavoriteId = favEntity.Id,
                SearchId = m.SearchId,
                SearchStr = m.SearchStr
            }));
            try
            {
                _domainContext.SaveChanges();
            }
            catch (Exception ex) {
                string message = ex.Message;
                result.LastMessage = "Operation Failed, Please check log!";
                result.OperationSucceed = false;
                result.ActionLog = "Operation Failed, log: " + ex.Message + " " +(ex.InnerException.Message ?? ""); 
            }
            return new Core.ViewModel.SearchFavoriteOperation
            {
                SearchFavorites = getUserFavorites(userId),
                ResultMessage = result
            };
        }

        public Core.ViewModel.SearchFavoriteOperation updateSearchFavorite(Core.ViewModel.SearchFavorite favorite, int userId)
        {
            ResultMessage result = new ResultMessage
            {
                Id = 0,
                OperationSucceed = true,
                LastMessage = "Operation was successfull.",
                ActionLog = "Operation was successfull."
            };
            var removingEntity = _domainContext.SearchFavorite.Find(favorite.Id);
            if (removingEntity == null)
            {
                result.OperationSucceed = false;
                result.LastMessage = "Favorite cannot be found!";
                return new Core.ViewModel.SearchFavoriteOperation
                {
                    SearchFavorites = getUserFavorites(userId),
                    ResultMessage = result
                };
            }

            _domainContext.SearchFavorite.Remove(removingEntity);

            var favEntity = new DAL.SearchFavorite
            {
                Name = favorite.Name,
                UserId = userId,
                BackgroundColor = favorite.BackgroundColor,
                TextColor = favorite.TextColor,
            };
            _domainContext.SearchFavorite.Add(favEntity);
            _domainContext.SearchFavoriteItems.AddRange(favorite.SearchItems.Select(m => new SearchFavoriteItems
            {
                ItemId = m.Id,
                SearchFavoriteId = favEntity.Id,
                SearchId = m.SearchId,
                SearchStr = m.SearchStr
            }));
            try
            {
                _domainContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                result.LastMessage = "Operation Failed, Please check log!";
                result.OperationSucceed = false;
                result.ActionLog = "Operation Failed, log: " + ex.Message + " " + (ex.InnerException.Message ?? "");
            }
            return new Core.ViewModel.SearchFavoriteOperation
            {
                SearchFavorites = getUserFavorites(userId),
                ResultMessage = result
            };
        }

        public Core.ViewModel.SearchFavoriteOperation removeSearchFavorite(int favoriteId, int userId) {
            ResultMessage result = new ResultMessage
            {
                Id = 0,
                OperationSucceed = true,
                LastMessage = "Operation was successfull.",
                ActionLog = "Operation was successfull."
            };
            var favorite = _domainContext.SearchFavorite.Find(favoriteId);
            if (favorite == null) {
                result.OperationSucceed = false;
                result.LastMessage = "Favorite cannot be found!";
                return new Core.ViewModel.SearchFavoriteOperation
                {
                    SearchFavorites = getUserFavorites(userId),
                    ResultMessage = result
                };
            }    
            _domainContext.SearchFavorite.Remove(favorite);
            try
            {
                _domainContext.SaveChanges();
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                result.LastMessage = "Operation Failed, Please check log!";
                result.OperationSucceed = false;
                result.ActionLog = "Operation Failed, log: " + ex.Message + " " + (ex.InnerException.Message ?? "");
            }
            return new Core.ViewModel.SearchFavoriteOperation
            {
                SearchFavorites = getUserFavorites(userId),
                ResultMessage = result
            };
        }

        public List<Core.ViewModel.SearchItem> getFavoriteItems(int favoriteId) {
            var favorite = _domainContext.SearchFavorite.Find(favoriteId);
            if (favorite == null)
                return new List<Core.ViewModel.SearchItem>();
            return favorite.SearchFavoriteItems.Select(m => new Core.ViewModel.SearchItem {
                Id = m.ItemId,
                SearchId = m.SearchId,
                SearchStr = m.SearchStr,
                Title = m.SearchStr
            }).ToList();
        }
    }
}
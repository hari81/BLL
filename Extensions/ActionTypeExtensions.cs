using BLL.Core.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Extensions
{
    public static class ActionTypeExtensions
    {
        public static ActionType ToActionType(this int TypeAsInt)
        {
            ActionType result = ActionType.NoActionTakenYet;
            try
            {
                result = (ActionType)TypeAsInt;
                return result;
            }
            catch
            {
                return result;
            }
        }
    }
}
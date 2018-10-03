using BLL.Extensions;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.Domain
{
    /// <summary>
    /// Used to manipulate a history record. History records exist for all
    /// events a user performs on a component. 
    /// </summary>
    public class History
    {
        private UndercarriageContext _context;
        private ACTION_TAKEN_HISTORY _history;

        public History(UndercarriageContext undercarriageContext, int historyRecordId)
        {
            _context = undercarriageContext;
            _history = _context.ACTION_TAKEN_HISTORY.Find(historyRecordId);
        }

        /// <summary>
        /// Updates the cost of an event. 
        /// </summary>
        /// <returns>Tuple with 2 values. First is true if the cost was updated successfully. Second is a message. </returns>
        public Tuple<bool, string> UpdateCost(decimal partsCost, decimal labourCost, decimal miscCost)
        {
            if (partsCost < 0 || labourCost < 0 || miscCost < 0)
                return Tuple.Create(false, "Failed to update cost. The value can't be less than 0. ");

            // If the history record has a valid component id and the action type was
            // replace with new. We will also update the cost of the new component with the parts cost.
            if (_history.equnit_auto != null && _history.equnit_auto != 0 && 
                _history.TRACK_ACTION_TYPE.action_description == "Replace component with new")
            {
                var component = new BLL.Core.Domain.Component(_context, (int)_history.equnit_auto);
                var result = component.UpdateComponentCost(partsCost);
                if (!result.Item1)
                    return result;
            }

            try
            {
                _history.cost = Convert.ToInt64(partsCost + labourCost + miscCost);
                _history.LabourCost = labourCost;
                _history.PartsCost = partsCost;
                _history.MiscCost = miscCost;
                _context.SaveChanges();
                return Tuple.Create(true, "Cost updated successfully. ");
            }
            catch (Exception e)
            {
                return Tuple.Create(false, "Failed to update cost. " + e.ToDetailedString());
            }
        }

        public EventCosts GetEventCosts()
        {
            return new EventCosts()
            {
                EventId = _history.history_id,
                LabourCost = _history.LabourCost,
                MiscCost = _history.MiscCost,
                PartsCost = _history.PartsCost
            };
        }
    }

    public class EventCosts {
        public long EventId { get; set; }
        public decimal PartsCost { get; set; }
        public decimal LabourCost { get; set; }
        public decimal MiscCost { get; set; }
    }
}
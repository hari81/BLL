using BLL.Core.Domain;
using BLL.Core.ViewModel;
using BLL.Extensions;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.Audit
{
    /// <summary>
    /// This auditor is used to log all the changes a user can make to an interpretation. 
    /// </summary>
    public class InterpretationAuditor
    {
        private UndercarriageContext _context;
        private USER_TABLE _user;
        private TRACK_INSPECTION _inspection;

        /// <summary>
        /// Audit logger for the undercarriage interpretation page. 
        /// </summary>
        /// <param name="context">The undercarriage database context. </param>
        /// <param name="inspectionId">The inspection Id we are logged changes for. </param>
        /// <param name="userId">The user who is making a change to the data. </param>
        public InterpretationAuditor(UndercarriageContext context, int inspectionId, long userId) {
            _context = context;
            _user = _context.USER_TABLE.Find(userId);
            _inspection = _context.TRACK_INSPECTION.Find(inspectionId);
        }

        /// <summary>
        /// Creates and saves a new interpretation audit log record in the database. This method should be called by
        /// all publicly exposed log methods to store a new audit log. 
        /// </summary>
        /// <param name="message">The message to store in the log. Generally in the format 
        /// "VARIABLE_NAME changed from OLD_VALUE to NEW_VALUE. "</param>
        /// <returns>Returns a tuple with the first value being true if successful, will include the exception in the second 
        /// value if there was an error saving the new log to the database. </returns>
        private Tuple<bool, string> Log(string message)
        {
            var audit = new InterpretationAudit()
            {
                EventTime = DateTime.Now,
                InspectionId = _inspection.inspection_auto,
                UserId = _user.user_auto,
                Message = message
            };
            _context.InterpretationAudit.Add(audit);
            try
            {
                _context.SaveChanges();
                return Tuple.Create(true, "Event was logged successfully. ");
            }
            catch(Exception e)
            {
                return Tuple.Create(false, e.ToDetailedString());
            }
        }

        /// <summary>
        /// Logs the old and new value of the overall interp eval being changed. 
        /// You must call this method before saving the new value. 
        /// </summary>
        /// <param name="newEval">The new eval</param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogOverallEvalChange(string newEval)
        {
            var old = _inspection.evalcode;
            return Log("Overall evaluation changed from '" + old + "' to '" + newEval + "'. ");
        }

        /// <summary>
        /// Logs that the interpretation has been released to the customer.  
        /// </summary>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogInterpReleased()
        {
            return Log("Interpretation was released. ");
        }

        /// <summary>
        /// Logs that there was a new quote created and logs the quote number.  
        /// </summary>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogQuoteCreated(string quoteNumber)
        {
            return Log("Created a new quote with quote number "+quoteNumber+". ");
        }

        /// <summary>
        /// Logs that there was a new recommendation created and which quote number it was added to.  
        /// </summary>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogRecommendationCreated(string quoteNumber, string componentName, string recommendationName, string side)
        {
            return Log("Added the recommendation " + recommendationName + " to the "+componentName+" on the " +side+" side for quote number "+quoteNumber+". ");
        }

        /// <summary>
        /// Logs that a recommendation was updated. 
        /// </summary>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogRecommendationUpdated(string quoteNumber, string componentName, string recommendationName, string side)
        {
            return Log("Updated the recommendation " + recommendationName + " for the " + componentName + " on the " + side + " side for quote number " + quoteNumber + ". ");
        }

        /// <summary>
        /// Logs that a recommendation was deleted. 
        /// </summary>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogRecommendationDeleted(string quoteNumber, string componentName, string recommendationName, string side)
        {
            return Log("Deleted the recommendation " + recommendationName + " for the " + componentName + " on the " + side + " side for quote number " + quoteNumber + ". ");
        }

        /// <summary>
        /// Logs the old and new value of the overall interp comment being changed. 
        /// </summary>
        /// <param name="oldComment">The old comment</param>
        /// <param name="newComment">The new comment</param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogOverallInterpCommentChange(string oldComment, string newComment)
        {
            return Log("Overall interpretation comment changed from '" + oldComment + "' to '" + newComment + "'. ");
        }

        /// <summary>
        /// Logs that a photo of a component was updated. 
        /// </summary>
        /// <param name="photoId">The id of the photo that was updated. </param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogComponentPhotoUpdate(int photoId)
        {
            var photo = _context.TRACK_INSPECTION_IMAGES.Find(photoId);
            var component = new Component(_context, (int)photo.TRACK_INSPECTION_DETAIL.track_unit_auto);
            string positionMessage = component.GetPositionLabel() == "" ? " " : " at position " + component.GetPositionLabel() + " ";
            return Log("Photo for " + component.GetComponentDescription() + positionMessage + "was updated. ");
        }

        /// <summary>
        /// Logs that a condition photo was changed. For example the track sag photo on the left. 
        /// </summary>
        /// <param name="condition">The condition type that the photo was altered for.  </param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogConditionPhotoUpdate(Condition condition)
        {
            switch(condition)
            {
                case Condition.TrackSagL:
                    return Log("Photo for track sag on the left side was updated. ");
                case Condition.TrackSagR:
                    return Log("Photo for track sag on the right side was updated. ");
                case Condition.CannonExtL:
                    return Log("Photo for cannon extension on the left side was updated. ");
                case Condition.CannonExtR:
                    return Log("Photo for cannon extension on the right side was updated. ");
                case Condition.ScallopL:
                    return Log("Photo for scallop on the left side was updated. ");
                case Condition.ScallopR:
                    return Log("Photo for scallop sag on the right side was updated. ");
                case Condition.DryJointsL:
                    return Log("Photo for dry joints extension on the left side was updated. ");
                case Condition.DryJointsR:
                    return Log("Photo for dry joints extension on the right side was updated. ");
                default:
                    return Tuple.Create(false, "Failed to log the change. You can't perform this task. ");
            }
        }

        /// <summary>
        /// Logs that a condition photo was deleted. For example the track sag photo on the left. 
        /// </summary>
        /// <param name="condition">The condition type that the photo was deleted for.  </param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogConditionPhotoDeleted(Condition condition)
        {
            switch (condition)
            {
                case Condition.TrackSagL:
                    return Log("Photo for track sag on the left side was deleted. ");
                case Condition.TrackSagR:
                    return Log("Photo for track sag on the right side was deleted. ");
                case Condition.CannonExtL:
                    return Log("Photo for cannon extension on the left side was deleted. ");
                case Condition.CannonExtR:
                    return Log("Photo for cannon extension on the right side was deleted. ");
                case Condition.DryJointsL:
                    return Log("Photo for Dry Joints on the left side was deleted. ");
                case Condition.DryJointsR:
                    return Log("Photo for Dry Joints on the right side was deleted. ");
                case Condition.ScallopL:
                    return Log("Photo for scallop on the left side was deleted. ");
                case Condition.ScallopR:
                    return Log("Photo for scallop on the right side was deleted. ");
                default:
                    return Tuple.Create(false, "Failed to log the change. You can't perform this task. ");
            }
        }

        /// <summary>
        /// Logs that a condition comment was changed. For example the track sag comment on the left. 
        /// </summary>
        /// <param name="condition">The condition type that the comment was altered for.  </param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogConditionCommentUpdate(Condition condition, string oldComment, string newComment)
        {
            switch (condition)
            {
                case Condition.TrackSagL:
                    return Log("Comment for track sag on the left side was changed from '" + oldComment + "' to '" + newComment + "'. ");
                case Condition.TrackSagR:
                    return Log("Comment for track sag on the right side was changed from '" + oldComment + "' to '" + newComment + "'. ");
                case Condition.CannonExtL:
                    return Log("Comment for cannon extension on the left side was changed from '" + oldComment + "' to '" + newComment + "'. ");
                case Condition.CannonExtR:
                    return Log("Comment for cannon extension on the right side was changed from '" + oldComment + "' to '" + newComment + "'. ");
                case Condition.ScallopL:
                    return Log("Comment for Scallop on the left side was changed from '" + oldComment + "' to '" + newComment + "'. ");
                case Condition.ScallopR:
                    return Log("Comment for Scallop on the right side was changed from '" + oldComment + "' to '" + newComment + "'. ");
                case Condition.DryJointsL:
                    return Log("Comment for Dry Joints on the left side was changed from '" + oldComment + "' to '" + newComment + "'. ");
                case Condition.DryJointsR:
                    return Log("Comment for Dry Joints on the right side was changed from '" + oldComment + "' to '" + newComment + "'. ");

                default:
                    return Tuple.Create(false, "Failed to log the change. You can't perform this task. ");
            }
        }

        /// <summary>
        /// Logs that a new photo for a component was uploaded. 
        /// </summary>
        /// <param name="componentId">The component id the photo was uploaded for. </param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogComponentPhotoUploaded(int componentId)
        {
            var component = new Component(_context, componentId);
            string positionMessage = component.GetPositionLabel() == "" ? "" : " at position " + component.GetPositionLabel();
            return Log("Uploaded a new photo for the " + component.GetComponentDescription() + positionMessage + ". ");
        }

        /// <summary>
        /// Logs that a photo was deleted. 
        /// </summary>
        /// <param name="componentId">The component id the photo being deleted was attached to. </param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogComponentPhotoDeleted(int componentId)
        {
            var component = new Component(_context, componentId);
            string positionMessage = component.GetPositionLabel() == "" ? "" : " at position " + component.GetPositionLabel();
            return Log("Deleted photo attached to the " + component.GetComponentDescription() + positionMessage + ". ");
        }

        /// <summary>
        /// Logs that a comment of a component was updated. 
        /// </summary>
        /// <param name="inspectionDetailId">The id of the component inspection detail that was updated. </param>
        /// <returns>Tuple with first value true if saved successfully. False if failed. </returns>
        public Tuple<bool, string> LogComponentCommentUpdate(int inspectionDetailId, string oldComment, string newComment)
        {
            var inspectionDetail = _context.TRACK_INSPECTION_DETAIL.Find(inspectionDetailId);
            var component = new Component(_context, (int)inspectionDetail.track_unit_auto);
            string positionMessage = component.GetPositionLabel() == "" ? " " : " at position " + component.GetPositionLabel() + " ";
            return Log("Comment for " + component.GetComponentDescription() + positionMessage + "was changed from '"+oldComment+"' to '"+newComment+"'. ");
        }

        /// <summary>
        /// Logs that the PDF report was sent and the email addresses it was sent to. 
        /// </summary>
        /// <param name="quoteId">Which quote of the inspection was sent. </param>
        /// <param name="emailAddresses">An array of email addresses the report was sent to. </param>
        /// <returns></returns>
        public Tuple<bool, string> LogReportMailed(int quoteId, string[] emailAddresses)
        {
            var quote = _context.TRACK_QUOTE.Find(quoteId);
            string formattedEmailList = "";
            for(int i = 0; i < emailAddresses.Length; i++)
            {
                formattedEmailList = formattedEmailList + emailAddresses[i];
                if (i != emailAddresses.Length - 1)
                    formattedEmailList = formattedEmailList + ", ";
                else
                    formattedEmailList = formattedEmailList + ". ";
            }
            return Log("Report with recommendations from quote " + quote.quote_name + " was emailed to " + formattedEmailList);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.GETCore.Classes
{
    public class GET_ACTIONS_constants
    {
        // These constants should be mapped to the 'action_name' column of the
        // GET_ACTIONS table in the database.
        public const string GET_EVENT_Inspection = "Inspection";
        public const string GET_EVENT_EquipmentSetup = "Equipment Setup";
        public const string GET_EVENT_ImplementSetup = "Implement Setup";
        public const string GET_EVENT_ComponentReplacement = "Component Replacement";
        public const string GET_EVENT_UndoComponentReplacement = "Undo Component Replacement";
        public const string GET_EVENT_FlagIgnored = "Flag Ignored";
    }
}
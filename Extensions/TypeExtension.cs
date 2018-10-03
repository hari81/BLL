using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Domain;
using BLL.Core.ViewModel;

namespace BLL.Extensions
{
    public static class TypeExtension
    {
        public static string RemoveWhitespace(this string str)
        {
            string res = string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
            return res;
        }

        public static decimal ConvertFrom(this decimal reading, MeasurementType from)
        {
            if (from == MeasurementType.Milimeter)
                return reading * (decimal)(0.0393701);
            return reading * (decimal)25.4;
        }

        public static decimal MilimeterToInch(this decimal measurement)
        {
            return measurement * (decimal)0.0393701;
        }

        public static decimal InchToMilimeter(this decimal measurement)
        {
            return measurement * (decimal)25.4;
        }

        public static int LongNullableToInt(this long? value)
        {
            if (value == null)
                return 0;
            if (value > Int32.MaxValue) //:) So Stupid if the number is bigger 
                return Int32.MaxValue;
            if (value < Int32.MinValue) // :))
                return Int32.MinValue;
            try { return (int)value; } catch { return 0; }
        }
        public static int LongNullableToInt(this long value)
        {
            if (value > Int32.MaxValue) //:) So Stupid if the number is bigger 
                return Int32.MaxValue;
            if (value < Int32.MinValue) // :))
                return Int32.MinValue;
            try { return (int)value; } catch { return 0; }
        }

        public static List<int> LongNullableToInt(this List<long> value)
        {
            var result = new List<int>();
            return value.Select(m => (m.LongNullableToInt())).ToList();
        }
        public static string Label(this ActionType value)
        {
            switch (value)
            {
                case ActionType.AdjustTensionLink:
                    return "Adjust Tension"; ;
                case ActionType.ChangeMeterUnit:
                    return "Change Meter Unit"; ;
                case ActionType.EquipmentSetup:
                    return "Equipment Setup"; ;
                case ActionType.GETAction:
                    return "'GET' Action"; ;
                case ActionType.InsertInspection:
                    return "Insert Inspection"; ;
                case ActionType.InstallComponentOnSystemOnEquipment:
                    return "Install Component On System"; ;
                case ActionType.InstallSystemOnEquipment:
                    return "Install System On Equipment"; ;
                case ActionType.OldActionType:
                    return "Old Action Type"; ;
                case ActionType.Regrouser:
                    return "Regrouser"; ;
                case ActionType.RepairDryJointsLink:
                case ActionType.RepairDryJointsBush:
                    return "Repair Dry Joints"; ;
                case ActionType.Replace:
                    return "Replace"; ;
                case ActionType.ReplaceComponentWithNew:
                    return "Replace Component With New"; ;
                case ActionType.ReplaceSystemFromInventory:
                    return "Replace System From Inventory"; ;
                case ActionType.Reshell:
                    return "Reshell"; ;
                case ActionType.SMUReadingAction:
                    return "SMU Reading";
                case ActionType.SwapFrontRear:
                    return "Swap Front Rear";
                case ActionType.TurnPinsAndBushingsLink:
                case ActionType.TurnPinsAndBushingsBush:
                    return "Turn Pins And Bushings";
                case ActionType.UpdateInspection:
                    return "Update Inspection";
                case ActionType.Weld:
                    return "Weld";
                default:
                    return "Unknown";
            }
        }
        public static char toEvalChar(this EvalCode code)
        {
            if (code == EvalCode.A) return 'A';
            if (code == EvalCode.B) return 'B';
            if (code == EvalCode.C) return 'C';
            if (code == EvalCode.X) return 'X';
            return 'U';
        }
        public static char toEvalChar(this decimal worn)
        {
            if (worn <= 30)
                return 'A';
            if (worn <= 50)
                return 'B';
            if (worn <= 70)
                return 'C';
            return 'X';
        }
        public static char toEvalChar(this int worn)
        {
            if (worn <= 30)
                return 'A';
            if (worn <= 50)
                return 'B';
            if (worn <= 70)
                return 'C';
            return 'X';
        }
        public static decimal ConvertMMToInch(this decimal reading)
        {
            return reading * (decimal)(0.0393701);
        }

        public static string getLabelForImpact(this int value)
        {
            if (value == 0)
                return "Low";
            if (value == 1)
                return "Normal";
            if (value == 2)
                return "High";
            return "N/A";
        }
        public static string getLabelForImpact(this short? value)
        {
            if (!value.HasValue)
                return "N/A";
            if (value == 0)
                return "Low";
            if (value == 1)
                return "Normal";
            if (value == 2)
                return "High";
            return "N/A";
        }

        public static string CalcMethodMapper(this string value)
        {
            string k = value.ToLower().Trim();
            if (k == "cat")
                return "Inflection";
            else if (k == "caterpillar")
                return "Inflection";
            else if (k == "komatsu")
                return "Polynomial";
            else if (k == "liebherr")
                return "Linear";
            else if (k == "hitachi")
                return "Linear";
            else if (k == "itm")
                return "Stepped";
            return "None";
        }

        public static string CalcMethodIdMapper(this int value)
        {
            switch (value)
            {
                case 1:
                    return "Stepped";
                case 2:
                    return "Inflection";
                case 3:
                    return "Polynomial";
                case 4:
                    return "Linear";
                case 5:
                    return "Linear";
                default:
                    return "None";
            }
        }

        public static string GetSystemSerialFromId(this long? systemId)
        {
            if (systemId == null)
                return "";
            if (systemId < 1)
                return "";
            using (var _context = new DAL.UndercarriageContext())
            {
                return _context.LU_Module_Sub.Where(s => s.Module_sub_auto == systemId).Select(s => s.Serialno).FirstOrDefault();
            }
        }

        /// <summary>
        /// Checks if the system serial number (chain or frame) should be displayed next to the component. 
        /// This is used on the old undercarriage interpretation and inspection details pages. 
        /// </summary>
        /// <param name="componentType">CompartType_Auto - ID of component type Link, Bushing, Shoe, etc.</param>
        /// <param name="position">Position the component is in. (we only want to show the serial if it is in the first position)</param>
        /// <returns>True if serial number should be displayed. </returns>
        public static bool ShouldDisplaySystemSerialNextToComponentType(this int componentType, int position)
        {
            if (position > 1)
                return false;

            CompartTypeEnum compartType = CompartTypeEnum.Unknown;
            try { compartType = (CompartTypeEnum)componentType; } catch { return false; }
            if (compartType == CompartTypeEnum.Unknown)
                return false;
            switch (compartType)
            {
                case CompartTypeEnum.Link:
                case CompartTypeEnum.Idler:
                    return true;
                default: return false;
            }
        }

        public static decimal CalcTotalComponentCost(this long componentId)
        {

            decimal cost = 0;
            if (componentId == 0)
                return cost;

            using (var _context = new DAL.UndercarriageContext())
            {
                var component = _context.GENERAL_EQ_UNIT.Where(g => g.equnit_auto == componentId).FirstOrDefault();
                if (component == null)
                    return 0;
                var actions = _context.ACTION_TAKEN_HISTORY.Where(m => m.equnit_auto == componentId && m.recordStatus == 0);
                if (actions.Count() == 0)
                    return 0;
                cost = actions.Select(m => m.cost).Sum();
                cost += component.cost == null ? 0 : (decimal)component.cost;
            }
            return cost;
        }

        public static bool IsMainComponentType(this int value)
        {
            CompartTypeEnum compartType = CompartTypeEnum.Unknown;
            try { compartType = (CompartTypeEnum)value; } catch { return false; }
            if (compartType == CompartTypeEnum.Unknown)
                return false;
            switch (compartType)
            {
                case CompartTypeEnum.Link:
                case CompartTypeEnum.Idler:
                    return true;
                default: return false;
            }
        }

        public static bool IsMainComponentType(this CompartTypeEnum value)
        {
            return ((int)value).IsMainComponentType();
        }

        public static int getCompartTypeId(this string TypeStr)
        {
            if (TypeStr.ToLower().Contains("link"))
                return (int)CompartTypeEnum.Link;

            if (TypeStr.ToLower().Contains("bushing"))
                return (int)CompartTypeEnum.Bushing;

            if (TypeStr.ToLower().Contains("shoe"))
                return (int)CompartTypeEnum.Shoe;

            if (TypeStr.ToLower().Contains("idler"))
                return (int)CompartTypeEnum.Idler;

            if (TypeStr.ToLower().Contains("carrier"))
                return (int)CompartTypeEnum.CarrierRoller;

            if (TypeStr.ToLower().Contains("roller"))
                return (int)CompartTypeEnum.TrackRoller;

            if (TypeStr.ToLower().Contains("sprocket"))
                return (int)CompartTypeEnum.Sprocket;

            if (TypeStr.ToLower().Contains("elongation"))
                return (int)CompartTypeEnum.TrackElongation;

            if (TypeStr.ToLower().Contains("guard"))
                return (int)CompartTypeEnum.Guard;

            return 0;
        }

        /// <summary>
        /// If this compartment is a child of any other compartments
        /// returns true otherwise false
        /// </summary>
        /// <returns></returns>
        public static bool isAChildCompart(this int CompartId)
        {
            if (CompartId == 0)
                return false;
            using (var _context = new DAL.UndercarriageContext())
            {
                var k = _context.COMPART_PARENT_RELATION.Where(m => m.ChildCompartId == CompartId);
                if (k.Count() == 0)
                    return false;
                return true;
            }
        }

        public static int NumberOfChildPoints(this int CompartId)
        {
            int k = 1;
            if (CompartId == 0)
                return k;
            using (var _context = new DAL.UndercarriageContext())
            {
                k = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == CompartId).Count();
            }
            if (k == 0)
                return 1;
            return k;
        }

        public static List<DAL.LU_COMPART> getChildCompartIdPoints(this int CompartId)
        {
            var result = new List<DAL.LU_COMPART>();
            if (CompartId == 0)
                return result;
            
            using (var _context = new DAL.UndercarriageContext())
            {
                result = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == CompartId).Select(m=> m.ParentCompartment).ToList();
            }
            return result;
        }

        public static MakeForSelectionVwMdl ToMake(this int makeId)
        {
            using (var _context = new DAL.UndercarriageContext())
            {
                var make = _context.MAKE.Find(makeId);
                if (make == null)
                    return new MakeForSelectionVwMdl
                    {
                        Id = 0,
                        Symbol = "UN",
                        Title = "UNKNOWN"
                    };
                return new MakeForSelectionVwMdl
                {
                    Id = makeId,
                    Symbol = make.makeid,
                    Title = make.makedesc
                };
            }
        }

        public static ModelForSelectionVwMdl ToModel(this int componentId)
        {
            using (var _context = new DAL.UndercarriageContext())
            {
                var component = _context.GENERAL_EQ_UNIT.Find(componentId);
                if (component == null || component.module_ucsub_auto == null || component.UCSystem == null)
                    return new ModelForSelectionVwMdl {
                        Id = 0, MakeId = 0, FamilyId = 0, Title =""
                    };

                var model = _context.MODELs.Find(component.UCSystem.model_auto);
                if(model == null)
                    return new ModelForSelectionVwMdl {
                        Id = 0, MakeId = 0, FamilyId = 0, Title =""
                    };

                    return new ModelForSelectionVwMdl
                    {
                        Id = (int)component.UCSystem.model_auto,
                        FamilyId = component.UCSystem.type_auto.LongNullableToInt(),
                        MakeId = component.make_auto ?? 0,
                        Title = model.modeldesc
                    };
            }
        }

        public static ComponentSetup ToComponentSetup(this DAL.GENERAL_EQ_UNIT geu)
        {
            return new ComponentSetup
            {
                Brand = geu.make_auto.HasValue ? ((int)geu.make_auto).ToMake() : 0.ToMake(),
                BudgetLife = geu.track_budget_life ?? 0,
                EquipmentSMU = geu.eq_smu_at_install.LongNullableToInt(),
                Grouser = new IdTitleV { Id = geu.ShoeGrouserNo ?? 0, Title = geu.ShoeGrouserNo.HasValue ? ((int)geu.ShoeGrouserNo).ToGrouser() : "" },
                HoursAtInstall = geu.cmu.LongNullableToInt(),
                Id = geu.equnit_auto.LongNullableToInt(),
                InstallCost = geu.cost ?? 0,
                InstallDate = (geu.date_installed ?? DateTime.MinValue).ToLocalTime().Date,
                listPosition = 0,
                Note = geu.compart_note,
                Points = 1,
                Pos = geu.pos ?? 0,
                Result = new ResultMessage { Id = 0, OperationSucceed = true, LastMessage = "", ActionLog = "" },
                ShoeSize = new ShoeSizeV { Id = geu.ShoeSizeId ?? 0, Size = geu.SHOE_SIZE != null ? geu.SHOE_SIZE.Size : 0, Title = geu.SHOE_SIZE != null ? geu.SHOE_SIZE.Title : "" },
                SystemId = geu.module_ucsub_auto.LongNullableToInt(),
                Validity = false,
                Compart = new CompartV
                {
                    Id = geu.compartid_auto,
                    CompartStr = geu.compartsn,
                    CompartTitle = geu.LU_COMPART.compart,
                    MeasurementPointsNo = 1,
                    CompartType = new CompartTypeV { Id = geu.LU_COMPART.LU_COMPART_TYPE.comparttype_auto, Title = geu.LU_COMPART.LU_COMPART_TYPE.comparttype, Order = geu.LU_COMPART.LU_COMPART_TYPE.sorder ?? 1 },
                    Model = geu.equnit_auto.LongNullableToInt().ToModel()
                }
            };
        }

        public static string ToGrouser(this int grouserId) {
            if (grouserId > 3 || grouserId < 1)
                return "";
            switch(grouserId){
                case 1: return "Single Grouser";
                case 2: return "Double Grouser";
                default: return "Triple Grouser";
            }
        }

        public static string PositionLabel(this int Pos, int CompartTypeId) {
            if ((CompartTypeId >= 230 && CompartTypeId <= 232) || CompartTypeId == 236 || CompartTypeId == 237 || CompartTypeId == 240) //Link, Bushing, Shoe, Sprocket, Guard
                return "";
            if (CompartTypeId == 233) //Idler
                if (Pos == 1) return "Front"; else if (Pos == 2) return "Rear";
            if (CompartTypeId == 234 && Pos == 1) //Carrier roller
                return "Front";
            return Pos.ToString();
        }

        public static int[] SplitToNumberArray(this string input)
        {
            var result = new List<int>();
            foreach(var number in input.Split(','))
            {
                try
                {
                    result.Add(int.Parse(number));
                }
                catch
                {
                    //
                }
            }
            return result.ToArray();
        }
    }
}
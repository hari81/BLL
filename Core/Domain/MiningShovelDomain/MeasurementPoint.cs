using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.Extensions;
using System.Threading.Tasks;

namespace BLL.Core.Domain.MiningShovelDomain
{
    /// <summary>
    /// This class is working with an instance of MEASUREMENT_POINT_RECORD 
    /// </summary>
    public class MeasurementPoint : UCDomain
    {
        private DAL.MEASUREMENT_POINT_RECORD DALMeasurePoint;
        private int Id = 0;

        public MeasurementPoint(IUndercarriageContext context) : base(context)
        {
        }
        public MeasurementPoint(IUndercarriageContext context, int MeasurepointId) : base(context)
        {
            Id = MeasurepointId;
        }
        public DAL.MEASUREMENT_POINT_RECORD getDALMeasurePoint(int MeasurepointId)
        {
            if (DALMeasurePoint != null && DALMeasurePoint.Id == MeasurepointId) return DALMeasurePoint;
            DALMeasurePoint = _domainContext.MEASUREMENT_POINT_RECORD.Find(MeasurepointId);
            return DALMeasurePoint;
        }
        public DAL.MEASUREMENT_POINT_RECORD getDALMeasurePoint()
        {
            if (DALMeasurePoint != null && DALMeasurePoint.Id == Id) return DALMeasurePoint;
                DALMeasurePoint = _domainContext.MEASUREMENT_POINT_RECORD.Find(Id);
            return DALMeasurePoint;
        }

        public decimal CalcWornPercentage(int MeasurePointId, decimal reading, int toolId, InspectionImpact? impact)
        {
            if(getDALMeasurePoint(MeasurePointId) == null) return (decimal)-0.0009;
            return CalcWornPercentageByCompartMeasure(getDALMeasurePoint(MeasurePointId).CompartMeasurePointId, reading, toolId, impact);
        }

        public decimal CalcWornPercentage(decimal reading, int toolId, InspectionImpact? impact)
        {
            if (getDALMeasurePoint() == null) return (decimal)-0.0009;
            return CalcWornPercentageByCompartMeasure(getDALMeasurePoint().CompartMeasurePointId, reading, toolId, impact);
        }

        public decimal CalcWornPercentage(decimal reading, int toolId, InspectionImpact? impact, int compartMeasureId)
        {
            if (getDALMeasurePoint() == null) return (decimal)-0.0009;
            return CalcWornPercentageByCompartMeasure(compartMeasureId, reading, toolId, impact);
        }

        public decimal CalcWornPercentageByCompartMeasure(int CompartMeasureId, decimal reading, int toolId, InspectionImpact? impact)
        {
            if (reading == 0) //TT-520 in comments 
                return (decimal)-0.0001;
            var _compartMeasurePoint = _domainContext.COMPART_MEASUREMENT_POINT.Find(CompartMeasureId);

            if (_compartMeasurePoint == null)
                return (decimal)-0.0002;
            
            var tcx = _domainContext.TRACK_COMPART_EXT.Where(m => m.compartid_auto == _compartMeasurePoint.CompartId && m.tools_auto == toolId && m.CompartMeasurePointId == CompartMeasureId);
            if (tcx == null || tcx.Count() == 0)
                return (decimal)-0.0003;
            WornCalculationMethod method;
            try { method = (WornCalculationMethod)tcx.First().track_compart_worn_calc_method_auto; } catch { method = WornCalculationMethod.None; };
            switch (method)
            {
                case WornCalculationMethod.ITM: //ITM
                    var kITM = _domainContext.TRACK_COMPART_WORN_LIMIT_ITM.Where(m => m.compartid_auto == _compartMeasurePoint.CompartId && m.track_tools_auto == toolId && m.MeasurePointId == CompartMeasureId);
                    if (kITM.Count() > 0)
                    {
                        var k = WornCalculationExtension.ITMReadingMapper(kITM.First(), reading.InchToMilimeter());
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
                case WornCalculationMethod.CAT: //CAT
                    var kCAT = _domainContext.TRACK_COMPART_WORN_LIMIT_CAT.Where(m => m.compartid_auto == _compartMeasurePoint.CompartId && m.track_tools_auto == toolId && m.MeasurePointId == CompartMeasureId);
                    if (kCAT.Count() > 0)
                    {
                        var k = WornCalculationExtension.CATReadingMapper(kCAT.First(), reading, impact);
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
                case WornCalculationMethod.Komatsu: //Komatsu
                    var kKomatsu = _domainContext.TRACK_COMPART_WORN_LIMIT_KOMATSU.Where(m => m.compartid_auto == _compartMeasurePoint.CompartId && m.track_tools_auto == toolId && m.MeasurePointId == CompartMeasureId);
                    if (kKomatsu.Count() > 0)
                    {
                        var k = WornCalculationExtension.KomatsuReadingMapper(kKomatsu.First(), reading, impact);
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
                case WornCalculationMethod.Hitachi: //Hitachi
                    var kHitach = _domainContext.TRACK_COMPART_WORN_LIMIT_HITACHI.Where(m => m.compartid_auto == _compartMeasurePoint.CompartId && m.track_tools_auto == toolId && m.MeasurePointId == CompartMeasureId);
                    if (kHitach.Count() > 0)
                    {
                        var k = WornCalculationExtension.HitachiReadingMapper(kHitach.First(), reading, impact);
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
                case WornCalculationMethod.Liebherr: //Liebherr
                    var kLiebherr = _domainContext.TRACK_COMPART_WORN_LIMIT_LIEBHERR.Where(m => m.compartid_auto == _compartMeasurePoint.CompartId && m.track_tools_auto == toolId && m.MeasurePointId == CompartMeasureId);
                    if (kLiebherr.Count() > 0)
                    {
                        var k = WornCalculationExtension.LiebherrReadingMapper(kLiebherr.First(), reading, impact);
                        return k < (decimal)-0.0009 && k >= -10 ? 0 : k;
                    }
                    break;
            }
            return (decimal)-0.0004;//Method not found
        }


        public bool EquipmentHasAnyMeasurePoint(int EquipmentId)
        {
            return _domainContext.EQUIPMENTs.Where(m => m.equipmentid_auto == EquipmentId).SelectMany(m => m.Components.Select(k => k.LU_COMPART.MeasurementPoints.Count() > 0)).Count() > 0;
        }


        public List<GENERAL_EQ_UNIT> EquipmentsHasMeasurementPointsConfigured()
        {
            return  _domainContext.EQUIPMENTs.Select(e => e.Components.FirstOrDefault(c => c.LU_COMPART.MeasurementPoints.Count() > 0)).ToList();
        }


      

    }
}
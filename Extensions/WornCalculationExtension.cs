using BLL.Core.Domain;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Extensions
{
    public static class WornCalculationExtension
    {
        public static decimal LiebherrReadingMapper(TRACK_COMPART_WORN_LIMIT_LIEBHERR r, decimal reading, InspectionImpact? i)
        {
            if (i == null)
                return (decimal)-0.0009;
            InspectionImpact impact = (InspectionImpact)i;
            decimal? A;
            decimal? B;
            if (i == InspectionImpact.High)
            {
                A = r.impact_slope;
                B = r.impact_intercept;
            }
            else
            {
                A = r.normal_slope;
                B = r.normal_intercept;
            }
            return firstOrder(A, B, reading);
        }

        public static decimal HitachiReadingMapper(TRACK_COMPART_WORN_LIMIT_HITACHI r, decimal reading, InspectionImpact? i)
        {
            if (i == null)
                return (decimal)-0.0008;
            InspectionImpact impact = (InspectionImpact)i;
            decimal? A;
            decimal? B;
            if (i == InspectionImpact.High)
            {
                A = r.impact_slope;
                B = r.impact_intercept;
            }
            else
            {
                A = r.normal_slope;
                B = r.normal_intercept;
            }
            return firstOrder(A, B, reading);
        }

        public static decimal KomatsuReadingMapper(TRACK_COMPART_WORN_LIMIT_KOMATSU r, decimal reading, InspectionImpact? i)
        {
            if (i == null)
                return (decimal)-0.0007;
            InspectionImpact impact = (InspectionImpact)i;
            decimal? A;
            decimal? B;
            decimal? C;
            if (i == InspectionImpact.High)
            {
                A = r.impact_secondorder;
                B = r.impact_slope;
                C = r.impact_intercept;
            }
            else
            {
                A = r.normal_secondorder;
                B = r.normal_slope;
                C = r.normal_intercept;
            }
            return secondOrder(A, B, C, reading);
        }

        public static decimal linearFormula(decimal? Ax, decimal? Ay, decimal? Bx, decimal? By, decimal reading)
        {
            decimal ax = Ax == null ? 0 : (decimal)Ax;
            decimal bx = Bx == null ? 0 : (decimal)Bx;
            decimal ay = Ay == null ? 0 : (decimal)Ay;
            decimal by = By == null ? 0 : (decimal)By;

            var m = ((by - ay) / (bx - ax));
            var c = (ay - (ax * m));
            return Math.Round((m * reading) + c, 3);
        }
        public static decimal secondOrder(decimal? A, decimal? B, decimal? C, decimal reading)
        {
            decimal a = A == null ? 0 : (decimal)A;
            decimal b = B == null ? 0 : (decimal)B;
            decimal c = C == null ? 0 : (decimal)C;

            decimal k = (decimal)Math.Pow((double)reading, 2);

            return Math.Round((a * k) + (b * reading) + c, 3);
        }
        public static decimal firstOrder(decimal? A, decimal? B, decimal reading)
        {
            decimal a = A == null ? 0 : (decimal)A;
            decimal b = B == null ? 0 : (decimal)B;

            return Math.Round((a * reading) + b, 3);
        }

        public static decimal ITMReadingMapper(TRACK_COMPART_WORN_LIMIT_ITM r, decimal reading)
        {
            if (r.start_depth_new > r.wear_depth_10_percent)
            {
                if (reading > r.start_depth_new)//todo test
                    return linearFormula(r.start_depth_new * 2 - r.wear_depth_10_percent, -10, r.start_depth_new, 0, reading);
                if (reading <= r.start_depth_new && reading > r.wear_depth_10_percent)
                    return linearFormula(r.start_depth_new, 0, r.wear_depth_10_percent, 10, reading);
                if (reading <= r.wear_depth_10_percent && reading > r.wear_depth_20_percent)
                    return linearFormula(r.wear_depth_10_percent, 10, r.wear_depth_20_percent, 20, reading);
                if (reading <= r.wear_depth_20_percent && reading > r.wear_depth_30_percent)
                    return linearFormula(r.wear_depth_20_percent, 20, r.wear_depth_30_percent, 30, reading);
                if (reading <= r.wear_depth_30_percent && reading > r.wear_depth_40_percent)
                    return linearFormula(r.wear_depth_30_percent, 30, r.wear_depth_40_percent, 40, reading);
                if (reading <= r.wear_depth_40_percent && reading > r.wear_depth_50_percent)
                    return linearFormula(r.wear_depth_40_percent, 40, r.wear_depth_50_percent, 50, reading);
                if (reading <= r.wear_depth_50_percent && reading > r.wear_depth_60_percent)
                    return linearFormula(r.wear_depth_50_percent, 50, r.wear_depth_60_percent, 60, reading);
                if (reading <= r.wear_depth_60_percent && reading > r.wear_depth_70_percent)
                    return linearFormula(r.wear_depth_60_percent, 60, r.wear_depth_70_percent, 70, reading);
                if (reading <= r.wear_depth_70_percent && reading > r.wear_depth_80_percent)
                    return linearFormula(r.wear_depth_70_percent, 70, r.wear_depth_80_percent, 80, reading);
                if (reading <= r.wear_depth_80_percent && reading > r.wear_depth_90_percent)
                    return linearFormula(r.wear_depth_80_percent, 80, r.wear_depth_90_percent, 90, reading);
                if (reading <= r.wear_depth_90_percent && reading > r.wear_depth_100_percent)
                    return linearFormula(r.wear_depth_90_percent, 90, r.wear_depth_100_percent, 100, reading);
                if (reading <= r.wear_depth_100_percent && reading > r.wear_depth_110_percent)
                    return linearFormula(r.wear_depth_100_percent, 100, r.wear_depth_110_percent, 110, reading);
                if (reading <= r.wear_depth_110_percent && reading > r.wear_depth_120_percent)
                    return linearFormula(r.wear_depth_110_percent, 110, r.wear_depth_120_percent, 120, reading);
                if (reading < r.wear_depth_120_percent)
                    return linearFormula(r.wear_depth_120_percent, 120, r.wear_depth_120_percent*2 - r.wear_depth_110_percent, 130, reading);
            }
            else
            {
                if (reading < r.start_depth_new)
                    return linearFormula(r.start_depth_new- r.wear_depth_10_percent, -10, r.wear_depth_10_percent, 0, reading);
                if (reading >= r.start_depth_new && reading < r.wear_depth_10_percent)
                    return linearFormula(r.start_depth_new, 0, r.wear_depth_10_percent, 10, reading);
                if (reading >= r.wear_depth_10_percent && reading < r.wear_depth_20_percent)
                    return linearFormula(r.wear_depth_10_percent, 10, r.wear_depth_20_percent, 20, reading);
                if (reading >= r.wear_depth_20_percent && reading < r.wear_depth_30_percent)
                    return linearFormula(r.wear_depth_20_percent, 20, r.wear_depth_30_percent, 30, reading);
                if (reading >= r.wear_depth_30_percent && reading < r.wear_depth_40_percent)
                    return linearFormula(r.wear_depth_30_percent, 30, r.wear_depth_40_percent, 40, reading);
                if (reading >= r.wear_depth_40_percent && reading < r.wear_depth_50_percent)
                    return linearFormula(r.wear_depth_40_percent, 40, r.wear_depth_50_percent, 50, reading);
                if (reading >= r.wear_depth_50_percent && reading < r.wear_depth_60_percent)
                    return linearFormula(r.wear_depth_50_percent, 50, r.wear_depth_60_percent, 60, reading);
                if (reading >= r.wear_depth_60_percent && reading < r.wear_depth_70_percent)
                    return linearFormula(r.wear_depth_60_percent, 60, r.wear_depth_70_percent, 70, reading);
                if (reading >= r.wear_depth_70_percent && reading < r.wear_depth_80_percent)
                    return linearFormula(r.wear_depth_70_percent, 70, r.wear_depth_80_percent, 80, reading);
                if (reading >= r.wear_depth_80_percent && reading < r.wear_depth_90_percent)
                    return linearFormula(r.wear_depth_80_percent, 80, r.wear_depth_90_percent, 90, reading);
                if (reading >= r.wear_depth_90_percent && reading < r.wear_depth_100_percent)
                    return linearFormula(r.wear_depth_90_percent, 90, r.wear_depth_100_percent, 100, reading);
                if (reading >= r.wear_depth_100_percent && reading < r.wear_depth_110_percent)
                    return linearFormula(r.wear_depth_100_percent, 100, r.wear_depth_110_percent, 110, reading);
                if (reading >= r.wear_depth_110_percent && reading < r.wear_depth_120_percent)
                    return linearFormula(r.wear_depth_110_percent, 110, r.wear_depth_120_percent, 120, reading);
                if(reading > r.wear_depth_120_percent)
                    return linearFormula(r.wear_depth_120_percent, 120, r.wear_depth_120_percent*2- r.wear_depth_110_percent, 130, reading);
            }



            return (decimal)-0.0005;
        }
        public static decimal CATReadingMapper(TRACK_COMPART_WORN_LIMIT_CAT r, decimal reading, InspectionImpact? i)
        {
            if (i == null)
                return (decimal)-0.0006;
            InspectionImpact impact = (InspectionImpact)i;
            decimal? slope;
            decimal? intercept;
            if (impact == InspectionImpact.High)
            {
                if (r.slope == 0)
                {
                    if (reading >= r.hi_inflectionPoint)
                    {
                        slope = r.hi_slope1;
                        intercept = r.hi_intercept1;
                    }
                    else
                    {
                        slope = r.hi_slope2;
                        intercept = r.hi_intercept2;
                    }
                }
                else
                {
                    if (reading >= r.hi_inflectionPoint)
                    {
                        slope = r.hi_slope2;
                        intercept = r.hi_intercept2;
                    }
                    else
                    {
                        slope = r.hi_slope1;
                        intercept = r.hi_intercept1;
                    }
                }
            }
            else
            {
                if (r.slope == 0)
                {
                    if (reading >= r.mi_inflectionPoint)
                    {
                        slope = r.mi_slope1;
                        intercept = r.mi_intercept1;
                    }
                    else
                    {
                        slope = r.mi_slope2;
                        intercept = r.mi_intercept2;
                    }
                }
                else
                {
                    if (reading >= r.mi_inflectionPoint)
                    {
                        slope = r.mi_slope2;
                        intercept = r.mi_intercept2;
                    }
                    else
                    {
                        slope = r.mi_slope1;
                        intercept = r.mi_intercept1;
                    }
                }
            }

            if (slope == null || intercept == null)
                return (decimal)-0.00069;
            return Math.Round(((decimal)slope * reading) + ((decimal)intercept), 3);
        }
    }
}
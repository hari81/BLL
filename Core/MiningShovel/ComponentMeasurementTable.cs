using BLL.Core.ViewModel;
using BLL.Extensions;
using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.MiningShovel
{
    public class ComponentMeasurementTable
    {
        UndercarriageContext _context;

        public ComponentMeasurementTable(UndercarriageContext context)
        {
            _context = context;
        }

        public List<ComponentRecord> GetTable(int inspectionId, int compartTypeId, string side, string uom)
        {
            var _inspection = _context.TRACK_INSPECTION.Find(inspectionId);
            if (_inspection == null)
                return null;

            var _inspectionDetails = _inspection.TRACK_INSPECTION_DETAIL
                .Where(d => d.GENERAL_EQ_UNIT.LU_COMPART.comparttype_auto == compartTypeId)
                .Where(d => d.GENERAL_EQ_UNIT.side == ConvertSideTextToByte(side))
                .ToList();
            var componentRecords = new List<ComponentRecord>();
            _inspectionDetails.ForEach(i =>
            {
                componentRecords.Add(GetComponentRecord((int)i.track_unit_auto, i, uom));
            });
            return componentRecords;
        }

        private ComponentRecord GetComponentRecord(int componentId, TRACK_INSPECTION_DETAIL inspectionDetail, string uom)
        {
            var dalComponent = new BLL.Core.Domain.Component(_context, componentId);
            var photo = dalComponent.GetComponentPhoto();
            var x = new ComponentRecord();
            x.Id = inspectionDetail.inspection_detail_auto;
            x.WornPercentage = Decimal.Round(inspectionDetail.worn_percentage, 2);
            x.Name = dalComponent.GetComponentDescription();
            x.Photo = photo != null ? Convert.ToBase64String(photo) : "";
            x.Position = dalComponent.GetPositionLabel();
            x.MeasurementPoints = GetMeasurementPointsForComponent(dalComponent, inspectionDetail, uom);
            return x;
        }

        private List<MeasurementPointRecord> GetMeasurementPointsForComponent(Domain.Component component, TRACK_INSPECTION_DETAIL inspectionDetail, string uom)
        {
            var possibleMeasurementPoints = _context.COMPART_MEASUREMENT_POINT.Where(p => p.CompartId == component.DALComponent.compartid_auto).ToList();
            List<MeasurementPointRecord> measurementPoints = new List<MeasurementPointRecord>();
            possibleMeasurementPoints.ForEach(p =>
            {
                measurementPoints.Add(GetMeasurementPointRecord(p, inspectionDetail, uom));
            });
            return measurementPoints;
        }

        private MeasurementPointRecord GetMeasurementPointRecord(COMPART_MEASUREMENT_POINT measurementPoint, TRACK_INSPECTION_DETAIL inspectionDetail, string uom)
        {

            var readingList = new List<MeasurementPointReading>();
            var tool = measurementPoint.PossibleTools.FirstOrDefault();
            return new MeasurementPointRecord()
            {
                CompartMeasurementPointId = measurementPoint.Id,
                Name = measurementPoint.Name,
                Photo = tool != null ? Convert.ToBase64String(tool.HowToUseImage) : null,
                AverageReading = GetAverageReadingForMeasurementPoint(measurementPoint, inspectionDetail, uom),
                Comment = GetCommentForMeasurementPoint(measurementPoint, inspectionDetail),
                Photos = GetPhotosForMeasurementPoint(measurementPoint, inspectionDetail),
                WornPercentage = GetAverageWornPercentageForMeasurementPoint(measurementPoint, inspectionDetail),
                Readings = GetReadingsForMeasurementPoint(measurementPoint, inspectionDetail, uom)
            };
        }

        private string GetAverageReadingForMeasurementPoint(COMPART_MEASUREMENT_POINT measurementPoint, TRACK_INSPECTION_DETAIL inspectionDetail, string uom)
        {
            var readings = inspectionDetail.MeaseurementPointRecors
                .Where(r => r.CompartMeasurePointId == measurementPoint.Id)
                .Where(r => r.ToolId != -1)
                .ToList();
            if (readings.Count == 0)
                return "?";
            if(readings.First().Tool.tool_code == "YES/NO")
            {
                switch((int)readings.First().Reading)
                {
                    case 1:
                        return "Yes";
                    case 0:
                        return "No";
                    default:
                        return "?";
                }
            }
            if(readings.First().Tool.tool_code == "KPO")
            {
                switch ((int)readings.First().Reading)
                {
                    case 1:
                        return "A";
                    case 2:
                        return "B";
                    case 3:
                        return "C";
                    case 4:
                        return "D";
                    default:
                        return "?";
                }
            }
            decimal total = 0;
            decimal count = 0;
            readings.ForEach(r =>
            {
                total += uom == "mm" ? r.Reading : r.Reading.MilimeterToInch();
                count++;
            });
            if(total > 0 && count > 0)
            {
                return Decimal.Round(total / count, 2).ToString();
            }
            return "0";
        }

        private decimal GetAverageWornPercentageForMeasurementPoint(COMPART_MEASUREMENT_POINT measurementPoint, TRACK_INSPECTION_DETAIL inspectionDetail)
        {
            var readings = inspectionDetail.MeaseurementPointRecors.Where(r => r.CompartMeasurePointId == measurementPoint.Id).ToList();
            if (readings.Count == 0)
                return 0;
            decimal total = 0;
            int count = 0;
            readings.ForEach(r =>
            {
                total += r.Worn;
                count++;
            });
            if (total > 0)
            {
                return Decimal.Round(total / count, 2);
            }
            return Decimal.Round(0, 2);
        }

        private string GetCommentForMeasurementPoint(COMPART_MEASUREMENT_POINT measurementPoint, TRACK_INSPECTION_DETAIL inspectionDetail)
        {
            var reading = inspectionDetail.MeaseurementPointRecors.Where(r => r.CompartMeasurePointId == measurementPoint.Id).FirstOrDefault();
            if (reading == null)
                return "";
            return reading.Notes;
        }

        private List<MeasurementPointReading> GetReadingsForMeasurementPoint(COMPART_MEASUREMENT_POINT measurementPoint, TRACK_INSPECTION_DETAIL inspectionDetail, string uom)
        {
            var readings = inspectionDetail.MeaseurementPointRecors.Where(r => r.CompartMeasurePointId == measurementPoint.Id).ToList();
            var possibleReadings = measurementPoint.DefaultNumberOfMeasurements;
            List<MeasurementPointReading> readingList = new List<MeasurementPointReading>();
            for(int i = 0; i < possibleReadings; i++)
            {
                var record = new MeasurementPointReading()
                {
                    Id = i + 1,
                    Measurement = Decimal.Round(-1, 2),
                    ToolId = measurementPoint.DefaultToolId != null ? (int)measurementPoint.DefaultToolId : 0,
                    WornPercentage = Decimal.Round(0, 2)
                };

                var recordDal = readings.Where(r => r.MeasureNumber == i + 1).FirstOrDefault();
                if(recordDal != null)
                {
                    //If tool is KPO, YES/NO, or Drive Lugs dont convert to inch because the front end wont be able to convert it to the answer
                    record.Measurement = Decimal.Round((uom == "mm" || recordDal.ToolId == 5 || recordDal.ToolId == 6 || recordDal.ToolId == 7) ? recordDal.Reading : recordDal.Reading.MilimeterToInch(), 2); 
                    record.ToolId = recordDal.ToolId;
                    record.WornPercentage = Decimal.Round(recordDal.Worn, 2);
                }
                readingList.Add(record);
            }

            return readingList;
        }

        private List<MeasurementPointPhoto> GetPhotosForMeasurementPoint(COMPART_MEASUREMENT_POINT measurementPoint, TRACK_INSPECTION_DETAIL inspectionDetail)
        {
            var reading = inspectionDetail.MeaseurementPointRecors.Where(r => r.CompartMeasurePointId == measurementPoint.Id).FirstOrDefault();
            if(reading == null)
                return new List<MeasurementPointPhoto>();

            return reading.Photos.Select(p => new MeasurementPointPhoto()
            {
                //Comment = p.Comment,
                Id = p.Id,
                //Photo = Convert.ToBase64String(p.Data),
                //Title = p.Title
            }).ToList();
        }

        private byte ConvertSideTextToByte(string side)
        {
            if (side == "Left")
                return 1;
            return 2;
        }
    }
}
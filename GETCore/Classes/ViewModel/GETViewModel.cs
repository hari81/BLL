using System;
using System.Collections.Generic;
using static BLL.GETInterfaces.Enum;

namespace BLL.GETCore.Classes.ViewModel
{
    public class GETViewModel
    {

    }

    public class GETInspectionSummaryDetailsVM
    {
        public string impserial { get; set; }
        public DateTime inspection_date { get; set; }
        public int eval { get; set; }
        public int ltd { get; set; }
        public string serialno { get; set; }
        public string unitno { get; set; }
        public string username { get; set; }
    }

    public class GETImplementCategoriesVM
    {
        public int implement_category_auto { get; set; }
        public string category_shortname { get; set; }
        public string category_name { get; set; }
        public string category_desc { get; set; }
    }

    public class GETComponentPositionsVM
    {
        public int inspection_auto { get; set; }
        public int comparttype_auto { get; set; }
        public int positionX { get; set; }
        public int positionY { get; set; }
    }

    public class GETInspectionDateEvalVM
    {
        public int inspection_auto { get; set; }
        public DateTime inspection_date { get; set; }
        public int eval { get; set; }
    }

    public class GETInterpretationCommentsVM
    {
        public string comment { get; set; }
        public string username { get; set; }
        public DateTime comment_date { get; set; }
    }

    public class GETEquipmentDetailsVM
    {
        public long id { get; set; }
        public string serialno { get; set; }
        public string unitno { get; set; }
        public string site_name { get; set; }
        public int meter_reading { get; set; }
        public string makedesc { get; set; }
        public string modeldesc { get; set; }
        public long ltd { get; set; }
        public string typedesc { get; set; }
        public string setup_date { get; set; }
    }

    public class GETComponentInfoVM
    {
        public long inspection_auto { get; set; }

        public decimal measurement { get; set; }

        public bool flag { get; set; }

        public bool replace { get; set; }

        public string comment { get; set; }

        public bool flag_ignored { get; set; }

        public int ltd { get; set; }

        public decimal condition { get; set; }

        public string comparttype { get; set; }

        public bool req_measure { get; set; }
    }

     public class GETGeneralQuestionsVM
    {
        public string inspOverallComments { get; set; }
        public string inspDirtyEnv { get; set; }
        public string inspCondition { get; set; }
        public string inspWorkArea { get; set; }
        public string inspMachine { get; set; }
        public string inspArea { get; set; }
    }

    public class GETImplementInspectionPhotosVM
    {
        public int image_auto { get; set; }
        public int parameter_type { get; set; }
    }

    public class GETObservationsVM
    {
        public int inspection_auto { get; set; }
        public int observations_auto { get; set; }
        public string observation { get; set; }
    }

    public class GETOPObservationsVM
    {
        public int op_inspection_auto { get; set; }
        public int observations_auto { get; set; }
        public string observation { get; set; }
    }

    public class GETComponentInspectionPhotosVM
    {
        public int component_inspection_auto { get; set; }
        public int component_auto { get; set; }
        public int image_auto { get; set; }
    }

    public class GETObservationPointInspectionPhotosVM
    {
        public int op_inspection_auto { get; set; }
        public int observation_point_auto { get; set; }
        public int image_auto { get; set; }
    }

    public class GETObservationPhotosVM
    {
        public int inspection_auto { get; set; }
        public int observations_auto { get; set; }
        public int image_auto { get; set; }
    }

    public class GETOPObservationPhotosVM
    {
        public int op_inspection_auto { get; set; }
        public int observations_auto { get; set; }
        public int image_auto { get; set; }
    }

    public class GETEquipmentListVM
    {
        public long equipmentid_auto { get; set; }
        public string serialno { get; set; }
        public string unitno { get; set; }
        public long customer_auto { get; set; }
        public string cust_name { get; set; }
        public int model_auto { get; set; }
        public string modeldesc { get; set; }
        public string makedesc { get; set; }
    }

    public class EquipmentListVM
    {
        public long equipmentId { get; set; }
        public string equipmentSerialNo { get; set; }
        public long equipmentSMU { get; set; }
    }

    public class GETObservationPoint
    {
        public int observation_point_auto { get; set; }
        public string name { get; set; }
        public string schematic_auto { get; set; }
        public string positionX { get; set; }
        public string positionY { get; set; }
    }

    public class GETObservationPointDetail
    {
        public int observation_point_auto { get; set; }
        public string name { get; set; }
        public string make { get; set; }
        public string observation_list { get; set; }
        public bool requires_measurement { get; set; }
        public string initial_length { get; set; }
        public string worn_length { get; set; }
        public string part_number { get; set; }
        public string price { get; set; }
        public string schematic_auto { get; set; }
        public string positionX { get; set; }
        public string positionY { get; set; }
    }

    public class GETObservationPointPositionsVM
    {
        public int inspection_auto { get; set; }

        public int observations_point_auto { get; set; }

        public int positionX { get; set; }

        public int positionY { get; set; }
    }

    public class GETObservationPointResultsSummaryVM
    {
        public long op_inspection_auto { get; set; }

        public string observation_name { get; set; }

        public decimal measurement { get; set; }

        public string comment { get; set; }

        public decimal condition { get; set; }

        public bool req_measure { get; set; }
    }

    public class GETImplementInventoryVM
    {
        public int get_auto { get; set; }

        public int condition { get; set; }

        public string customer { get; set; }

        public string jobsite { get; set; }

        public string make { get; set; }

        public string type { get; set; }

        public string serial_no { get; set; }

        public int ltd { get; set; }

        public string status { get; set; }

        public string equipment_serialno { get; set; }

        public string equipment_unitno { get; set; }

        public string repairer { get; set; }

        public string workshop { get; set; }
    }

    public class GETImplementTypeVM
    {
        public long implement_auto { get; set; }

        public string implement_description { get; set; }
    }

    public class MakeVM
    {
        public int make_auto { get; set; }

        public string make_desc { get; set; }
    }

    public class GETImplementDetailsVM
    {
        public int condition { get; set; }

        public string customer { get; set; }

        public string jobsite { get; set; }

        public long jobsiteId { get; set; }

        public string make { get; set; }

        public string model { get; set; }

        public int life { get; set; }

        public string implementType { get; set; }

        public string serialNo { get; set; }

        public string lastInspection { get; set; }

        public string status { get; set; }

        public string repairerName { get; set; }

        public string workshopName { get; set; }

        public override bool Equals(object obj)
        {
            bool result = false;
            var details = obj as GETImplementDetailsVM;
            if(details != null)
            {
                result = (condition == details.condition);
                result &= customer.Equals(details.customer);
                result &= jobsite.Equals(details.jobsite);
                result &= make.Equals(details.make);
                result &= model.Equals(details.model);
                result &= (life == details.life);
                result &= implementType.Equals(details.implementType);
                result &= serialNo.Equals(details.serialNo);
                result &= lastInspection.Equals(details.lastInspection);
                result &= status.Equals(details.status);
            }

            return result;
        }
    }

    public class GETInspectionDetailsVM
    {
        public int id { get; set; }

        public int condition { get; set; }

        public string inspectionDate { get; set; }

        public string inspector { get; set; }

        public int life { get; set; }
    }

    public class GETComponentDetailsVM
    {
        public long componentId { get; set; }

        public int condition { get; set; }

        public string component { get; set; }

        public string part_number { get; set; }

        public string make { get; set; }

        public int cost { get; set; }

        public int life { get; set; }
    }

    public class GETHistoryVMInternal
    {
        public long events_auto { get; set; }

        public string date { get; set; }

        public string implement_life { get; set; }

        public string component { get; set; }

        public string component_life { get; set; }

        public string action_taken { get; set; }

        public decimal cost { get; set; }

        public string comment { get; set; }

        public string component_part_no { get; set; }

        public int recordStatus { get; set; }
    }

    public class GETHistoryVM
    {
        public string date { get; set; }

        public string implement_life { get; set; }

        public string component { get; set; }

        public string component_life { get; set; }

        public string action_taken { get; set; }

        public decimal cost { get; set; }

        public string comment { get; set; }
    }

    public class GETInventoryStatusVM
    {
        public int statusId { get; set; }
        public string statusName { get; set; }
    }

    public class GETRepairerVM
    {
        public int repairerId { get; set; }
        public string repairerName { get; set; }
    }

    public class GETWorkshopVM
    {
        public int workshopId { get; set; }
        public string workshopName { get; set; }
    }

    public class GenericIdNameVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SchematicDataVM
    {
        public int Id { get; set; }
        public string Data { get; set; }
        public List<ComponentPointVM> GETComponents { get; set; }
        public List<ComponentPointVM> ObservationPoints { get; set; }
    }

    public class ComponentPointVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public int WornPct { get; set; }
        public string InspectionDate { get; set; }
    }

    public class ImplementDetails
    {
        public int Id { get; set; }
        public int Make { get; set; }
        public long ImplementType { get; set; }
        public string SerialNo { get; set; }
        public string SetupDate { get; set; }
        public long ImplementHoursAtSetup { get; set; }
        public long EquipmentSMUAtSetup { get; set; }
    }

    public class MeasurementPointDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Make { get; set; }
        public int ObservationListId { get; set; }
        public decimal InitialLength { get; set; }
        public decimal WornLength { get; set; }
        public string PartNo { get; set; } 
    }

    public class ObservationPointDetails
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ObservationListId { get; set; }
        public decimal InitialLength { get; set; }
        public decimal WornLength { get; set; }
    }

    public class SaveImplementImageParams
    {
        public int GETAuto { get; set; }
        public string ImageData { get; set; }
    }

    public class SaveImplementDetailParams
    {
        public ImplementDetails Details { get; set; }
        public int EquipmentId { get; set; }
        public int JobsiteId { get; set; }
    }

    public class SaveMeasurementPointParams
    {
        public int GetAuto { get; set; }
        public List<MeasurementPointDetails> MeasurementPoints { get; set; }
    }

    public class SaveObservationPointParams
    {
        public int GetAuto { get; set; }
        public List<ObservationPointDetails> ObservationPoints { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BLL.Core.ManageEquipment.ViewModels
{
    public class ManageEquipmentDetailsViewModel
    {
        public decimal PercentWorn { get; set; }
        public string CustomerName { get; set; }
        public string JobsiteName { get; set; }
        public string SerialNumber { get; set; }
        public string UnitNumber { get; set; }
        public string LastInspectionDate { get; set; }
        public string NextInspectionDate { get; set; }
        public int Smu { get; set; }
        public int Ltd { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public string Family { get; set; }
        public string EquipmentPhoto { get; set; }
        public long CustomerId { get; set; }
        public long JobsiteId { get; set; }


        public int InspectEvery { get; set; }
        public int InspectEveryUnitTypeId { get; set; }
     
    }

    public class ManageEquipmentSystemViewModel
    {
        public long Id { get; set; }
        public string SerialNumber { get; set; }
        public int LifeLived { get; set; }
        public decimal TotalComponentPurchaseCost { get; set; }
        public decimal TotalComponentRepairsCost { get; set; }
        public string DateInstalled { get; set; }
        public int EquipmentSmuAtInstall { get; set; }
        public string Side { get; set; }
        public string SystemType { get; set; }
        public List<ManageEquipmentComponentViewModel> Components { get; set; }
    }

    public class ManageEquipmentComponentViewModel
    {
        public long Id { get; set; }
        public string Photo { get; set; }
        public decimal PercentWorn { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public int Cmu { get; set; }
        public decimal RemainingLife100 { get; set; }
        public decimal RemainingLife120 { get; set; }
        public string InstallDate { get; set; }
        public int EquipmentSmuAtInstall { get; set; }
        public decimal PurchaseCost { get; set; }
        public decimal RepairsCost { get; set; }
        public int Position { get; set; }
        public int CompartTypeId { get; set; }
    }

    public class ManageEquipmentInspectionViewModel
    {
        public int Id { get; set; }
        public int QuoteId { get; set; }
        public string Eval { get; set; }
        public string Date { get; set; }
        public bool Interpreted { get; set; }
        public bool IsReportSaved { get; set; }
    }






    public class UpdateEquipmentSMUReadingViewModel
    {
        public long equipmentId { get; set; }
        public long currentReading { get; set; }
        public DateTime dateReadSMU { get; set; }
    }

    public class ChangeEquipmentMeterUnitViewModel
    {
        public long equipmentId { get; set; }
        public long newSMUReadingOnNewMeter { get; set; }
        public long oldMeterSMUReading { get; set; }
        public DateTime dateReplaced { get; set; }
    }



    public class UpdateInspectionForecastingInfoViewModel
    {
        public int EquipmentId { get; set; }
        public int InspectEvery { get; set; }
        public int InspectEveryUnitType { get; set; }
    }




}
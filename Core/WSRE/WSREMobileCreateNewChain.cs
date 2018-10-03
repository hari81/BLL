using BLL.Administration;
using BLL.Core.WSRE.Models;
using BLL.GETCore.Classes;
using DAL;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using BLL.Extensions;

namespace BLL.Core.Domain
{
    public class WSREMobileCreateNewChain
    {
        private UndercarriageContext _context;

        public WSREMobileCreateNewChain(UndercarriageContext context)
        {
            this._context = context;
        }

        private int GetGrouserId(String grouser)
        {
            switch (grouser)
            {
                case "Single Grouser":
                    return 1;
                case "Double Grouser":
                    return 2;
                case "Triple Grouser":
                    return 3;
                default:
                    return 0;
            }
        }

        public BLL.Core.ViewModel.SetupViewModel createNewChain(WSRENewChain newchain)
        {

            BLL.Core.ViewModel.SetupViewModel system = new Core.ViewModel.SetupViewModel();

            ///////////
            // User
            var userEntity = _context.USER_TABLE.Where(m => m.userid == newchain.UserId).FirstOrDefault();
            if (userEntity == null)
                return system;
            var _user = new BLL.Core.Domain.User
            {
                Id = userEntity.user_auto.LongNullableToInt(),
                userName = userEntity.username,
                userStrId = userEntity.userid
            };

            //////////
            // Make
            var makeEntity = _context.MAKE.Where(m => m.make_auto == newchain.MakeAuto).FirstOrDefault();
            if (makeEntity == null)
                return system;
            MakeForSelectionVwMdl makeVwMdl = new MakeForSelectionVwMdl();
            makeVwMdl.Id = makeEntity.make_auto;
            makeVwMdl.Title = makeEntity.makedesc;
            makeVwMdl.Symbol = makeEntity.makeid;
            //makeVwMdl.ExistingCount = 0;

            ////////////
            // Family
            var mmtaEntity = _context.LU_MMTA.Where(m => m.model_auto == newchain.ModelAuto).FirstOrDefault();
            if (mmtaEntity == null)
                return system;
            var familyEntity = _context.TYPEs.Where(m => m.type_auto == mmtaEntity.type_auto).FirstOrDefault();
            if (familyEntity == null)
                return system;
            FamilyForSelectionVwMdl familyVwMdl = new FamilyForSelectionVwMdl();
            familyVwMdl.Id = familyEntity.type_auto;
            familyVwMdl.Title = familyEntity.typedesc;
            familyVwMdl.Symbol = familyEntity.typeid;
            //familyVwMdl.ExistingCount = 0;

            ///////////
            // Model
            var modelEntity = _context.MODELs.Where(m => m.model_auto == newchain.ModelAuto).FirstOrDefault();
            if (modelEntity == null)
                return system;
            ModelForSelectionVwMdl modelVwMdl = new ModelForSelectionVwMdl();
            modelVwMdl.Id = newchain.ModelAuto;
            modelVwMdl.MakeId = newchain.MakeAuto;
            modelVwMdl.FamilyId = familyVwMdl.Id;
            modelVwMdl.Title = modelEntity.modeldesc;
            //makeVwMdl.ExistingCount = 0;

            BLL.Core.ViewModel.ComponentSetup linkComponent = null;
            BLL.Core.ViewModel.ComponentSetup bushingComponent = null;
            BLL.Core.ViewModel.ComponentSetup shoeComponent = null;

            /////////////////
            // Link Compart
            if (newchain.LinkComponent.compartid_auto > 0)
            {
                var linkCompartEntity = _context.LU_COMPART.Where(m => m.compartid_auto == newchain.LinkComponent.compartid_auto).FirstOrDefault();
                if (linkCompartEntity == null)
                    return system;
                var linkCompartExtEntity = _context.TRACK_COMPART_EXT
                    .Where(
                        m => m.compartid_auto == newchain.LinkComponent.compartid_auto).FirstOrDefault();
                if (linkCompartExtEntity == null)
                    return system;

                linkComponent = new Core.ViewModel.ComponentSetup();
                linkComponent.Id = 0;
                linkComponent.SystemId = 0;
                linkComponent.Note = "";
                linkComponent.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = true,
                    LastMessage = "",
                    ActionLog = ""
                };
                linkComponent.Validity = false;
                linkComponent.listPosition = 1;
                linkComponent.BudgetLife = newchain.LinkComponent.budget_life;
                linkComponent.HoursAtInstall = newchain.LinkComponent.hours_on_surface;
                linkComponent.InstallDate = DateTime.Now;
                linkComponent.EquipmentSMU = 0;
                linkComponent.InstallCost = newchain.LinkComponent.cost;
                linkComponent.Pos = 1;
                linkComponent.Points = 1;

                CompartV linkCompart = new CompartV();
                linkCompart.Id = newchain.LinkComponent.compartid_auto;
                linkCompart.CompartStr = linkCompartEntity.compartid;
                linkCompart.CompartTitle = linkCompartEntity.compart;
                linkCompart.MeasurementPointsNo = 1;
                linkCompart.DefaultBudgetLife = linkCompartExtEntity.budget_life.Value;

                CompartTypeV linkCompartType = new CompartTypeV();
                linkCompartType.Id = 230;
                linkCompartType.Title = "Link";
                linkCompartType.Order = 121;
                linkCompart.CompartType = linkCompartType;

                ModelForSelectionVwMdl linkModel = new ModelForSelectionVwMdl();
                linkModel.Id = newchain.ModelAuto;
                linkModel.Title = modelEntity.modeldesc;
                linkCompart.Model = linkModel;

                MakeForSelectionVwMdl linkDefaultMake = new MakeForSelectionVwMdl();
                linkDefaultMake.Id = newchain.MakeAuto;
                linkDefaultMake.Title = makeVwMdl.Title;
                linkDefaultMake.Symbol = makeVwMdl.Symbol;
                linkCompart.DefaultMake = linkDefaultMake;

                linkComponent.Compart = linkCompart;
                linkComponent.Grouser = new IdTitleV
                {
                    Id = 0,
                    Title = ""
                };

                var brandEntity = _context.MAKE.Where(m => m.make_auto == newchain.LinkComponent.brand_auto).FirstOrDefault();
                if (brandEntity == null)
                    return system;
                linkComponent.Brand = new MakeForSelectionVwMdl
                {
                    Id = newchain.LinkComponent.brand_auto,
                    Title = brandEntity.makedesc,
                    Symbol = brandEntity.makeid,
                    ExistingCount = 0
                };
                linkComponent.ShoeSize = new ShoeSizeV
                {
                    Id = 0,
                    Title = "",
                    Size = 0
                };
            }

            ////////////////////
            // Bushing Compart
            if (newchain.BushingComponent.compartid_auto > 0 )
            {
                var bushingCompartEntity = _context.LU_COMPART.Where(m => m.compartid_auto == newchain.BushingComponent.compartid_auto).FirstOrDefault();
                if (bushingCompartEntity == null)
                    return system;
                var bushingCompartExtEntity = _context.TRACK_COMPART_EXT
                    .Where(
                        m => m.compartid_auto == newchain.BushingComponent.compartid_auto).FirstOrDefault();
                if (bushingCompartExtEntity == null)
                    return system;

                bushingComponent = new Core.ViewModel.ComponentSetup();
                bushingComponent.Id = 0;
                bushingComponent.SystemId = 0;
                bushingComponent.Note = "";
                bushingComponent.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = true,
                    LastMessage = "",
                    ActionLog = ""
                };
                bushingComponent.Validity = false;
                bushingComponent.listPosition = 1;
                bushingComponent.BudgetLife = newchain.BushingComponent.budget_life;
                bushingComponent.HoursAtInstall = newchain.BushingComponent.hours_on_surface;
                bushingComponent.InstallDate = DateTime.Now;
                bushingComponent.EquipmentSMU = 0;
                bushingComponent.InstallCost = newchain.BushingComponent.cost;
                bushingComponent.Pos = 1;
                bushingComponent.Points = 1;

                CompartV bushingCompart = new CompartV();
                bushingCompart.Id = newchain.BushingComponent.compartid_auto;
                bushingCompart.CompartStr = bushingCompartEntity.compartid;
                bushingCompart.CompartTitle = bushingCompartEntity.compart;
                bushingCompart.MeasurementPointsNo = 1;
                bushingCompart.DefaultBudgetLife = bushingCompartExtEntity.budget_life.Value;

                CompartTypeV bushingCompartType = new CompartTypeV();
                bushingCompartType.Id = 231;
                bushingCompartType.Title = "Bushing";
                bushingCompartType.Order = 122;
                bushingCompart.CompartType = bushingCompartType;

                ModelForSelectionVwMdl bushingModel = new ModelForSelectionVwMdl();
                bushingModel.Id = newchain.ModelAuto;
                bushingModel.Title = modelEntity.modeldesc;
                bushingCompart.Model = bushingModel;

                MakeForSelectionVwMdl bushingDefaultMake = new MakeForSelectionVwMdl();
                bushingDefaultMake.Id = newchain.MakeAuto;
                bushingDefaultMake.Title = makeVwMdl.Title;
                bushingDefaultMake.Symbol = makeVwMdl.Symbol;
                bushingCompart.DefaultMake = bushingDefaultMake;

                bushingComponent.Compart = bushingCompart;
                bushingComponent.Grouser = new IdTitleV
                {
                    Id = 0,
                    Title = ""
                };

                var brandBushingEntity = _context.MAKE.Where(m => m.make_auto == newchain.BushingComponent.brand_auto).FirstOrDefault();
                if (brandBushingEntity == null)
                    return system;
                bushingComponent.Brand = new MakeForSelectionVwMdl
                {
                    Id = newchain.BushingComponent.brand_auto,
                    Title = brandBushingEntity.makedesc,
                    Symbol = brandBushingEntity.makeid,
                    ExistingCount = 0
                };
                bushingComponent.ShoeSize = new ShoeSizeV
                {
                    Id = 0,
                    Title = "",
                    Size = 0
                };
            }

            /////////////////
            // Shoe Compart
            if (newchain.ShoeComponent.compartid_auto > 0)
            {
                var shoeCompartEntity = _context.LU_COMPART.Where(m => m.compartid_auto == newchain.ShoeComponent.compartid_auto).FirstOrDefault();
                if (shoeCompartEntity == null)
                    return system;
                var shoeCompartExtEntity = _context.TRACK_COMPART_EXT
                    .Where(
                        m => m.compartid_auto == newchain.ShoeComponent.compartid_auto).FirstOrDefault();
                if (shoeCompartExtEntity == null)
                    return system;

                shoeComponent = new Core.ViewModel.ComponentSetup();
                shoeComponent.Id = 0;
                shoeComponent.SystemId = 0;
                shoeComponent.Note = "";
                shoeComponent.Result = new ResultMessage
                {
                    Id = 0,
                    OperationSucceed = true,
                    LastMessage = "",
                    ActionLog = ""
                };
                shoeComponent.Validity = false;
                shoeComponent.listPosition = 1;
                shoeComponent.BudgetLife = newchain.ShoeComponent.budget_life;
                shoeComponent.HoursAtInstall = newchain.ShoeComponent.hours_on_surface;
                shoeComponent.InstallDate = DateTime.Now;
                shoeComponent.EquipmentSMU = 0;
                shoeComponent.InstallCost = newchain.ShoeComponent.cost;
                shoeComponent.Pos = 1;
                shoeComponent.Points = 1;

                CompartV shoeCompart = new CompartV();
                shoeCompart.Id = newchain.ShoeComponent.compartid_auto;
                shoeCompart.CompartStr = shoeCompartEntity.compartid;
                shoeCompart.CompartTitle = shoeCompartEntity.compart;
                shoeCompart.MeasurementPointsNo = 1;
                shoeCompart.DefaultBudgetLife = shoeCompartExtEntity.budget_life.Value;

                CompartTypeV shoeCompartType = new CompartTypeV();
                shoeCompartType.Id = 232;
                shoeCompartType.Title = "Shoe";
                shoeCompartType.Order = 123;
                shoeCompart.CompartType = shoeCompartType;

                ModelForSelectionVwMdl shoeModel = new ModelForSelectionVwMdl();
                shoeModel.Id = newchain.ModelAuto;
                shoeModel.Title = modelEntity.modeldesc;
                shoeCompart.Model = shoeModel;

                MakeForSelectionVwMdl shoeDefaultMake = new MakeForSelectionVwMdl();
                shoeDefaultMake.Id = newchain.MakeAuto;
                shoeDefaultMake.Title = makeVwMdl.Title;
                shoeDefaultMake.Symbol = makeVwMdl.Symbol;
                shoeCompart.DefaultMake = shoeDefaultMake;

                shoeComponent.Compart = shoeCompart;
                shoeComponent.Grouser = new IdTitleV
                {
                    Id = GetGrouserId(newchain.ShoeComponent.grouser),
                    Title = newchain.ShoeComponent.grouser
                };

                var brandShoeEntity = _context.MAKE.Where(m => m.make_auto == newchain.ShoeComponent.brand_auto).FirstOrDefault();
                if (brandShoeEntity == null)
                    return system;
                shoeComponent.Brand = new MakeForSelectionVwMdl
                {
                    Id = newchain.ShoeComponent.brand_auto,
                    Title = brandShoeEntity.makedesc,
                    Symbol = brandShoeEntity.makeid,
                    ExistingCount = 0
                };

                var shoeSizeEntity = _context.SHOE_SIZE.Where(m => m.Id == newchain.ShoeComponent.shoe_size_id).FirstOrDefault();
                if (shoeSizeEntity == null)
                    return system;
                shoeComponent.ShoeSize = new ShoeSizeV
                {
                    Id = newchain.ShoeComponent.shoe_size_id,
                    Title = shoeSizeEntity.Title,
                    Size = shoeSizeEntity.Size
                };
            }

            ///////////////////
            // List Component
            List<BLL.Core.ViewModel.ComponentSetup> Components = new List<Core.ViewModel.ComponentSetup>();
            if (linkComponent != null)
                Components.Add(linkComponent);
            if (bushingComponent != null)
                Components.Add(bushingComponent);
            if (shoeComponent != null)
                Components.Add(shoeComponent);

            ///////////////////
            // Result
            ResultMessage result = new ResultMessage
            {
                Id = 0,
                OperationSucceed = true,
                LastMessage = "",
                ActionLog = ""
            };

            ///////////////////////
            // Create new chain
            system.Serial = newchain.Serial;
            system.JobsiteId = newchain.JobsiteId;
            system.Type = UCSystemType.Chain;
            system.EquipmentId = 0;
            system.SmuAtInstall = 0;
            system.HoursAtInstall = newchain.HoursAtInstall;
            system.UserId = unchecked((int)userEntity.user_auto);
            system.SetupDate = DateTime.Now;
            system.InstallationDate = DateTime.Now;
            // Cost, Comment, InstallOnEquipment, Side
            system.Make = makeVwMdl;
            system.Model = modelVwMdl;
            system.Family = familyVwMdl;
            system.Components = Components;
            system.Result = result;

            BLL.Core.ViewModel.SetupViewModel SystemSetup = 
                new UCSystem(new DAL.UndercarriageContext()).CreateAndUpdateSystemForInventory(system, _user);

            return SystemSetup;
        }
    }
}
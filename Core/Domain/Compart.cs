using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAL;
using BLL.Persistence.Repositories;
using System.Data.Entity;
using BLL.Interfaces;
using System.Threading.Tasks;
using BLL.Extensions;

namespace BLL.Core.Domain
{
    public class Compart : Component, ICompart
    {
        private int pvId;
        private LU_COMPART pvDALCompart;
        private LU_COMPART_TYPE pvCompartType;
        private int pvDefaultBudget = 0;
        public new int Id
        {
            get { return pvId; }
            set { pvId = value; }
        }
        private UndercarriageContext _context
        {
            get { return Context as UndercarriageContext; }
        }

        public LU_COMPART DALCompart
        {
            get
            { return pvDALCompart; }

            set
            { pvDALCompart = value; }
        }

        public LU_COMPART_TYPE DALType
        {
            get
            { return pvCompartType; }

            set
            { pvCompartType = value; }
        }

        public int DefaultBudgetLife
        {
            get
            { return pvDefaultBudget; }

            set
            { pvDefaultBudget = value; }
        }

        public Compart(IUndercarriageContext context) : base(context)
        {
            Id = 0;
            DefaultBudgetLife = 0;
        }
        public Compart(IUndercarriageContext context, int id) : base(context)
        {
            Id = 0;
            DefaultBudgetLife = 0;
            DALCompart = _context.LU_COMPART.Find(id);
            if (DALCompart != null)
            {
                Id = id;
                DALType = DALCompart.LU_COMPART_TYPE;
            }
        }
        public int getCompartDefaultBudgetLife()
        {
            if (Id == 0 || DefaultBudgetLife != 0)
                return DefaultBudgetLife;
            var k = _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == Id);
            if (k.Count() > 0)
                DefaultBudgetLife = k.First().budget_life == null ? 0 : (int)k.First().budget_life;
            return DefaultBudgetLife;
        }

        public MakeForSelectionVwMdl getCompartDefaultMake()
        {
            if (Id == 0)
                return new MakeForSelectionVwMdl { Id = 0, Symbol = "UN", Title = "Unknown" };
            var k = _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == Id);
            if (k.Count() == 0)
                return new MakeForSelectionVwMdl { Id = 0, Symbol = "UN", Title = "Unknown" };
            return new MakeForSelectionVwMdl { Id = k.First().make_auto ?? 0, Symbol = k.First().MAKE.makeid, Title = k.First().MAKE.makedesc };
        }

        public MakeForSelectionVwMdl getCompartDefaultMake(int Id)
        {
            if (Id == 0)
                return new MakeForSelectionVwMdl { Id = 0, Symbol = "UN", Title = "Unknown" };
            var k = _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == Id);
            if (k.Count() == 0)
                return new MakeForSelectionVwMdl { Id = 0, Symbol = "UN", Title = "Unknown" };
            return new MakeForSelectionVwMdl { Id = k.First().make_auto ?? 0, Symbol = k.First().MAKE.makeid, Title = k.First().MAKE.makedesc };
        }

        public int getCompartDefaultBudgetLifeExc(int Id)
        {
            int result = 0;
            if (Id <= 0)
                return 0;
            var k = _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == Id);
            if (k.Count() > 0)
                result = k.First().budget_life == null ? 0 : (int)k.First().budget_life;
            return result;
        }

        public string GetCompartSerialNumber()
        {
            if (Id == 0)
                return "";
            return DALCompart.compartid;
        }
        /*

         */
        public List<CompartToolImageViewModel> GetCompartMeasuringImages()
        {
            List<CompartToolImageViewModel> retList = new List<CompartToolImageViewModel>();
            if (Id == 0)
                return retList;
            var res = _context.COMPART_TOOL_IMAGE.Where(m => m.CompartId == Id).OrderBy(m => m.ToolId).ToList();
            foreach (var ctImg in res)
            {
                CompartToolImageViewModel ctImgVm = new CompartToolImageViewModel
                {
                    Id = ctImg.Id,
                    CompartId = ctImg.CompartId,
                    CreatedDate = ctImg.CreatedDate.ToString("dd-MMM-yyyy"),
                    ImageDataBase64 = Convert.ToBase64String(ctImg.ImageData),
                    ImageType = ctImg.ImageType,
                    Title = ctImg.Title,
                    ToolId = ctImg.ToolId,
                    ToolName = ctImg.Tool.tool_name
                };
                retList.Add(ctImgVm);
            }
            return retList;
        }

        public bool getReadingAsEvalStatus()
        {
            if (DALCompart == null)
                return false;
            return DALCompart.AcceptEvalAsReading;
        }

        public ServerReturnMessage saveCompartToolImage(CompartToolImageViewModel ImageRecord)
        {
            ServerReturnMessage result = new ServerReturnMessage
            {
                Id = 0,
                Message = "Compartment is not found!",
                Succeed = false
            };
            if (Id == 0)
                return result;
            result.Message = "Selected tool is invalid!";
            var tool = _context.TRACK_TOOL.Find(ImageRecord.ToolId);
            if (tool == null)
                return result;
            COMPART_TOOL_IMAGE ctImage = new COMPART_TOOL_IMAGE();
            ctImage.CompartId = Id;
            ctImage.ToolId = ImageRecord.ToolId;
            ctImage.Title = ImageRecord.Title;
            ctImage.ImageData = Convert.FromBase64String(ImageRecord.ImageDataBase64);
            ctImage.ImageType = ImageRecord.ImageType;
            ctImage.CreatedDate = DateTime.Now;
            var k = _context.COMPART_TOOL_IMAGE.Where(m => m.CompartId == ImageRecord.CompartId && m.ToolId == ImageRecord.ToolId);
            if (k.Count() > 0)
                _context.COMPART_TOOL_IMAGE.RemoveRange(k);
            _context.COMPART_TOOL_IMAGE.Add(ctImage);
            try
            {
                _context.SaveChanges();
                result.Succeed = true;
                result.Message = "Operation succeeded";
                return result;
            }
            catch (Exception e1)
            {
                result.Message = "Operation failed! please try again!";
                result.ExceptionMessage = e1.Message;
                if (e1.InnerException != null)
                    result.InnerExceptionMessage = e1.InnerException.Message;
                result.Succeed = false;
                return result;
            }
        }
        /// <summary>
        /// By using this method parent of the current compart will be set or updated
        /// This method will be used for mining shovel comparts 
        /// </summary>
        /// <param name="parentCompartId">Parent Compartment Id to be set for this compartment</param>
        /// <returns></returns>
        public ServerReturnMessage AssignParent(int parentCompartId)
        {
            ServerReturnMessage result = new ServerReturnMessage
            {
                Id = 0,
                Message = "Compartment is not found!",
                Succeed = false
            };
            if (Id == 0)
                return result;
            var thisCompartAschildList = _context.COMPART_PARENT_RELATION.Where(m => m.ChildCompartId == Id);
            LU_COMPART parentCompart = _context.LU_COMPART.Find(parentCompartId);
            if (thisCompartAschildList.Count() > 0) //Update or Remove
            {
                COMPART_PARENT_RELATION relationRecord = thisCompartAschildList.First();
                if (parentCompartId == 0) //Remove
                {
                    _context.COMPART_PARENT_RELATION.Remove(relationRecord);
                }
                else if (parentCompart != null) //Update
                {
                    relationRecord.ParentCompartId = parentCompart.compartid_auto;
                    _context.Entry(relationRecord).State = EntityState.Modified;
                }
                else //Not remove and not able to update because parent compart not found
                {
                    result.Succeed = true;
                    result.Message = "Operation partially successful! Parent compart not found to be updated";
                    return result;
                }
            }
            else if (parentCompart != null) //Add
            {
                _context.COMPART_PARENT_RELATION.Add(
                    new COMPART_PARENT_RELATION { ChildCompartId = Id, ParentCompartId = parentCompartId }
                    );
            } // Not a change or not an update. Just doing nothing about parent
            else
            {
                result.Succeed = true;
                result.Message = "Operation was successful! No parent assigned or updated for this component.";
                return result;
            }
            try
            {
                _context.SaveChanges();
                result.Succeed = true;
                result.Message = "Operation was successful!";
                return result;
            }
            catch (Exception e1)
            {
                result.Message = "Operation failed! please try again!";
                result.ExceptionMessage = e1.Message;
                if (e1.InnerException != null)
                    result.InnerExceptionMessage = e1.InnerException.Message;
                result.Succeed = false;
                return result;
            }
        }

        public LU_COMPART GetParentDALCompart()
        {
            if (Id == 0)
            {
                return null;
            }
            var compartParentRelation = _context.COMPART_PARENT_RELATION.Where(m => m.ChildCompartId == Id);
            if (compartParentRelation.Count() > 0)
            {
                int? ParentCompartId = compartParentRelation.First().ParentCompartId;
                return _context.LU_COMPART.Find(ParentCompartId);
            }
            return null;
        }

        public List<LU_COMPART> getChildComparts()
        {
            List<LU_COMPART> result = new List<LU_COMPART>();
            if (Id == 0)
                return result;

            var relation = _context.COMPART_PARENT_RELATION.Where(m => m.ParentCompartId == Id).Select(m => m.ChildCompartId);

            var k = _context.LU_COMPART.Where(m => relation.Any(p => p == m.compartid_auto));
            return k.ToList();
        }

        public new bool isAChildBasedOnCompart()
        {
            if (Id == 0 || DALCompart == null)
                return false;
            var k = _context.COMPART_PARENT_RELATION.Where(m => m.ChildCompartId == Id).Count();
            if (k == 0)
                return false;
            return true;
        }
        public WornCalculationMethod getWornCalcMethod(int toolId)
        {
            if (Id == 0)
                return WornCalculationMethod.None;
            var compart_ext = _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == Id && m.tools_auto == toolId);
            int method = 0;
            if (compart_ext.Count() > 0)
                method = compart_ext.First().track_compart_worn_calc_method_auto == null ? 0 : (int)compart_ext.First().track_compart_worn_calc_method_auto;
            try
            {
                return (WornCalculationMethod)method;
            }
            catch
            {
                return WornCalculationMethod.None;
            }
        }

        public List<TRACK_COMPART_EXT> getWornExtList()
        {
            List<TRACK_COMPART_EXT> result = new List<TRACK_COMPART_EXT>();
            if (Id == 0)
                return result;
            return _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == Id).ToList();
        }
        public TRACK_COMPART_WORN_LIMIT_ITM getWornExtData(int toolId)
        {
            if (Id == 0)
                return null;
            var ItmList = _context.TRACK_COMPART_WORN_LIMIT_ITM.Where(m => m.compartid_auto == Id && m.track_tools_auto == toolId);
            if (ItmList.Count() > 0)
                return ItmList.First();
            return null;
        }

        public List<TRACK_COMPART_WORN_LIMIT_ITM> getWornExtDataListITM()
        {
            return _context.TRACK_COMPART_WORN_LIMIT_ITM.Where(m => m.compartid_auto == Id).ToList();
        }

        public List<TRACK_COMPART_WORN_LIMIT_CAT> getWornExtDataListCAT()
        {
            return _context.TRACK_COMPART_WORN_LIMIT_CAT.Where(m => m.compartid_auto == Id).ToList();
        }

        public List<TRACK_COMPART_WORN_LIMIT_KOMATSU> getWornExtDataListKomatsu()
        {
            return _context.TRACK_COMPART_WORN_LIMIT_KOMATSU.Where(m => m.compartid_auto == Id).ToList();
        }
        public List<TRACK_COMPART_WORN_LIMIT_HITACHI> getWornExtDataListHitachi()
        {
            return _context.TRACK_COMPART_WORN_LIMIT_HITACHI.Where(m => m.compartid_auto == Id).ToList();
        }
        public List<TRACK_COMPART_WORN_LIMIT_LIEBHERR> getWornExtDataListLibherr()
        {
            return _context.TRACK_COMPART_WORN_LIMIT_LIEBHERR.Where(m => m.compartid_auto == Id).ToList();
        }

        public List<CompartWornExtViewModel> getCompartWornDataAllMethods()
        {
            List<CompartWornExtViewModel> result = new List<CompartWornExtViewModel>();
            if (Id == 0)
                return result;

            var compartExtList = _context.TRACK_COMPART_EXT.Where(m => m.compartid_auto == Id).ToList();
            foreach (var ext in compartExtList)
            {
                CompartWornExtViewModel res = new CompartWornExtViewModel { Id = Id };
                WornCalculationMethod method;
                try { method = (WornCalculationMethod)ext.track_compart_worn_calc_method_auto; } catch { method = WornCalculationMethod.None; }
                switch (method)
                {
                    case WornCalculationMethod.ITM:
                        res.method = method;
                        res.ITMExtList = getWornExtDataListITM();
                        break;
                    case WornCalculationMethod.CAT:
                        res.method = method;
                        res.CATExtList = getWornExtDataListCAT();
                        break;
                    case WornCalculationMethod.Komatsu:
                        res.method = method;
                        res.KomatsuExtList = getWornExtDataListKomatsu();
                        break;
                    case WornCalculationMethod.Hitachi:
                        res.method = method;
                        res.HitachiExtList = getWornExtDataListHitachi();
                        break;
                    case WornCalculationMethod.Liebherr:
                        res.method = method;
                        res.LiebherrExtList = getWornExtDataListLibherr();
                        break;
                }
                result.Add(res);
            };
            return result;
        }

        /// <summary>
        /// If this compartment is a child of any other compartments
        /// returns true otherwise false
        /// </summary>
        /// <returns></returns>
        public bool isAChild(int Id)
        {
            if (Id == 0)
                return false;

            var k = _context.COMPART_PARENT_RELATION.Where(m => m.ChildCompartId == Id);
            if (k.Count() == 0)
                return false;
            return true;
        }

        public IEnumerable<CompartV> GetCompartListForModel(int ModelId)
        {
            var k = _context.TRACK_COMPART_MODEL_MAPPING.Where(m => m.model_auto == ModelId && m.Compart.CHILD_RELATION_LIST.Count() == 0).Select(m => new CompartV { Id = m.compartid_auto, CompartStr = m.Compart.compartid, CompartTitle = m.Compart.compart, CompartType = new CompartTypeV { Id = m.Compart.comparttype_auto, Title = m.Compart.LU_COMPART_TYPE.comparttype, Order = m.Compart.LU_COMPART_TYPE.sorder ?? 0 }, MeasurementPointsNo = 1, Model = new ModelForSelectionVwMdl { Id = m.model_auto, Title = m.Model.modeldesc }, CompartNote = m.Compart.compart_note }).ToList();
            foreach (var compart in k)
            {
                compart.MeasurementPointsNo = Id.NumberOfChildPoints();
                compart.DefaultBudgetLife = getCompartDefaultBudgetLifeExc(compart.Id);
                compart.DefaultMake = getCompartDefaultMake(compart.Id);
            }
            return k;
        }

        public async Task<IEnumerable<CompartV>> GetCompartListForModelAsync(int ModelId)
        {
            return await Task.Run(() => GetCompartListForModel(ModelId));
        }
    }
}
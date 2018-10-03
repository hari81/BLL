using BLL.GETInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.GETCore.Classes.ViewModel;
using System.Threading.Tasks;
using DAL;
using static BLL.GETInterfaces.Enum;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace BLL.GETCore.Classes
{
    public class ImplementTemplateManager : IImplementTemplateManager
    {
        private GETContext _context;
        private UserManagement _userManager;

        public ImplementTemplateManager()
        {
            this._context = new GETContext();
            this._userManager = new UserManagement();
        }

        public async Task<Tuple<int, string>> CreateNewImplementTemplate(NewImplementTemplateViewModel newImplementTemplate)
        {
            List<LU_COMPART_TYPE> components = new List<LU_COMPART_TYPE>(); // New components that need to be added to the database

            // Check if a template with this name exists for the same category and customer
            bool existingTemplate = _context.LU_IMPLEMENT.Where(i => i.implementdescription.ToLower() == newImplementTemplate.TemplateName.ToLower()
                                                                        && i.CustomerId == newImplementTemplate.CustomerId
                                                                        && i.implement_category_auto == (int) newImplementTemplate.ImplementCategory).Any();
            if (existingTemplate)
                return Tuple.Create(-1, "A template with this name already exists. ");

            // Create new implement
            LU_IMPLEMENT template = new LU_IMPLEMENT
            {
                CustomerId = newImplementTemplate.CustomerId,
                implementdescription = newImplementTemplate.TemplateName,
                implement_category_auto = (int) newImplementTemplate.ImplementCategory,
                parentID = 0
            };
            _context.LU_IMPLEMENT.Add(template);

            // Create any new components that don't exist yet, and then add all new and existing components to list
            foreach(ImplementComponentTypeViewModel componentType in newImplementTemplate.ComponentTypes)
            {
                if(componentType.Id == 0) // If ID = 0 then it is a new component we need to create
                {
                    // Generate a stupid second ID we don't need.
                    string id = componentType.Name;
                    if (id.Length > 10)
                        id = id.Substring(0, 10);

                    // GET-68
                    if (containsReservedName(id))
                    {
                        return Tuple.Create(-1, "Unable to create component as it contains a reserved keyword!");
                    }

                    // Work out the component type category for GET or Dump Body
                    short componentCategory;
                    if(newImplementTemplate.ImplementCategory == ImplementCategory.GET)
                    {
                        componentCategory = (int) ComponentTypeCategory.GET;
                    } else
                    {
                        componentCategory = (int)ComponentTypeCategory.DumpBody;
                    }

                    LU_COMPART_TYPE newComponent = new LU_COMPART_TYPE()
                    {
                        comparttypeid = id,
                        comparttype = componentType.Name,
                        _protected = false,
                        system_auto = componentCategory
                    };

                    components.Add(newComponent);
                    _context.LU_COMPART_TYPE.Add(newComponent);
                }
                else // Add existing components to list, so we can map them to this implement template
                {
                    components.Add(_context.LU_COMPART_TYPE.Where(c => c.comparttype_auto == componentType.Id).First());
                }
            }

            try
            {
                _context.SaveChanges();
            } catch (Exception e)
            {
                return Tuple.Create(-1, "Failed to create template and new component types. Exception: " + e.Message);
            }

            // If everything saved successfully, map all the components to the new template
            foreach(LU_COMPART_TYPE comp in components)
            {
                _context.GET_IMPLEMENT_COMPARTTYPE.Add(new GET_IMPLEMENT_COMPARTTYPE()
                {
                    comparttype_auto = comp.comparttype_auto,
                    implement_auto = template.implement_auto
                });
            }

            try
            {
                _context.SaveChanges();
            } catch (Exception e)
            {
                return Tuple.Create(-1, "Failed to map new component types to the new template. Exception: " + e.Message);
            }

            // Map the models to the template
            foreach(int modelId in newImplementTemplate.EquipmentModels)
            {
                _context.GET_IMPLEMENT_MAKE_MODEL.Add(new GET_IMPLEMENT_MAKE_MODEL()
                {
                    implement_auto = template.implement_auto,
                    model_auto = modelId
                });
            }

            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return Tuple.Create(-1, "Failed to map the new template to it's models. Exception: " + e.Message);
            }

            return Tuple.Create((int) template.implement_auto, "Template created successfully. ");
        }

        public async Task<Tuple<bool, List<ImplementTemplateExtendedViewModel>>> GetImplementTemplatesForUser(long userId)
        {
            var customers = _userManager.getCustomerAndDealershipList(userId).Where(l => l.type == "customer").Select(c => c.id).ToList();
            var implements = await _context.LU_IMPLEMENT.Where(i => i.CustomerId == null || customers.Contains((long)i.CustomerId))
                .Where(i => i.implement_category_auto == (int)ImplementCategory.DumpBody || i.implement_category_auto == (int)ImplementCategory.GET)
                .Select(i => new ImplementTemplateExtendedViewModel() {
                    CustomerName = i.Customer.cust_name == null ? "GLOBAL" : i.Customer.cust_name,
                    TemplateId = (int) i.implement_auto,
                    TemplateName = i.implementdescription,
                    ImplementsUsing = 0,
                    ImplementCategory = (ImplementCategory)i.implement_category_auto
                }).ToListAsync();

            return Tuple.Create(true, implements);
        }

        public async Task<Byte[]> getSchematicImageSmall(int schematicId)
        {
            var schematicRecord = await _context.GET_SCHEMATIC_IMAGE.Where(s => s.schematic_auto == schematicId).FirstAsync();
            Image schematic;
            Image resizedSchematic;
            using (var ms = new MemoryStream(schematicRecord.attachment))
            {
                schematic = Image.FromStream(ms);
            }
            resizedSchematic = new Bitmap(schematic, new Size(60, 60));

            using(var ms2 = new MemoryStream())
            {
                resizedSchematic.Save(ms2, ImageFormat.Png);
                return ms2.ToArray();
            }
        }

        public async Task<Tuple<bool, NewImplementTemplateViewModel>> ReturnTemplateById(long templateId)
        {
            bool status = false;
            NewImplementTemplateViewModel result = new NewImplementTemplateViewModel();

            using (var dataEntities = new DAL.GETContext())
            {
                var template = dataEntities.LU_IMPLEMENT.Find(templateId);
                if(template != null)
                {
                    result.CustomerId = template.CustomerId;
                    result.ImplementCategory = (ImplementCategory) template.implement_category_auto;
                    result.TemplateName = template.implementdescription;
                    result.TemplateId = template.implement_auto;

                    if(template.CustomerId != null)
                    {
                        result.TemplateAccess = TemplateAccess.Customer;
                    }
                    else
                    {
                        result.TemplateAccess = TemplateAccess.Global;
                    }

                    var implementModels = await dataEntities.GET_IMPLEMENT_MAKE_MODEL
                        .Where(i => i.implement_auto == templateId)
                        .Select(s => (int) s.model_auto)
                        .ToArrayAsync();
                    if(implementModels != null)
                    {
                        result.EquipmentModels = implementModels;
                    }

                    var implementComponents = await (from ic in dataEntities.GET_IMPLEMENT_COMPARTTYPE
                                                     join lct in dataEntities.LU_COMPART_TYPE
                                                        on ic.comparttype_auto equals lct.comparttype_auto
                                                     where ic.implement_auto == templateId
                                                     select new ImplementComponentTypeViewModel
                                                     {
                                                         Id = ic.comparttype_auto,
                                                         Name = lct.comparttype
                                                     }).ToListAsync();

                    if(implementComponents != null)
                    {
                        result.ComponentTypes = implementComponents;
                    }

                    status = true;
                }
            }

            return Tuple.Create(status, result);
        }

        public async Task<Tuple<int, string>> UpdateExistingImplementTemplate(NewImplementTemplateViewModel existingImplementTemplate)
        {
            List<LU_COMPART_TYPE> components = new List<LU_COMPART_TYPE>(); // New components that need to be added to the database

            // Check if the template exists.
            var template = _context.LU_IMPLEMENT.Find(existingImplementTemplate.TemplateId);

            // An existing template does not exist, so an update action cannot be performed.
            if (template == null)
                return Tuple.Create(-1, "Could not update this template. Please refresh the page and try again. ");

            template.CustomerId = existingImplementTemplate.CustomerId;
            template.implementdescription = existingImplementTemplate.TemplateName;
            template.implement_category_auto = (int) existingImplementTemplate.ImplementCategory;
            template.parentID = 0;

            // Save any changes made to the template.
            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex1)
            {
                return Tuple.Create(-1, "Unable to save changes to the template. Exception: " + ex1.Message);
            }


            // Create any new components that don't exist yet, and then add all new and existing components to list
            foreach (ImplementComponentTypeViewModel componentType in existingImplementTemplate.ComponentTypes)
            {
                if (componentType.Id == 0) // If ID = 0 then it is a new component we need to create
                {
                    // Generate a stupid second ID we don't need.
                    string id = componentType.Name;
                    if (id.Length > 10)
                        id = id.Substring(0, 10);

                    // GET-68
                    if(containsReservedName(id))
                    {
                        return Tuple.Create(-1, "Unable to create component as it contains a reserved keyword!");
                    }

                    // Work out the component type category for GET or Dump Body
                    short componentCategory;
                    if (existingImplementTemplate.ImplementCategory == ImplementCategory.GET)
                    {
                        componentCategory = (int)ComponentTypeCategory.GET;
                    }
                    else
                    {
                        componentCategory = (int)ComponentTypeCategory.DumpBody;
                    }

                    LU_COMPART_TYPE newComponent = new LU_COMPART_TYPE()
                    {
                        comparttypeid = id,
                        comparttype = componentType.Name,
                        _protected = false,
                        system_auto = componentCategory
                    };

                    components.Add(newComponent);
                    _context.LU_COMPART_TYPE.Add(newComponent);
                }
                else // Add existing components to list, so we can map them to this implement template
                {
                    components.Add(_context.LU_COMPART_TYPE.Where(c => c.comparttype_auto == componentType.Id).First());
                }
            }

            // Save any changes made to the list of component types.
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return Tuple.Create(-1, "Failed to update component types. Exception: " + e.Message);
            }


            // If everything saved successfully, map all the components to the new template
            foreach (LU_COMPART_TYPE comp in components)
            {
                // Check whether the mapping already exists.
                var implement_comparttype = await _context.GET_IMPLEMENT_COMPARTTYPE
                    .Where(ic => ic.comparttype_auto == comp.comparttype_auto && ic.implement_auto == template.implement_auto)
                    .FirstOrDefaultAsync();

                // Create the mapping if it doesn't exist.
                if (implement_comparttype == null)
                {
                    _context.GET_IMPLEMENT_COMPARTTYPE.Add(new GET_IMPLEMENT_COMPARTTYPE()
                    {
                        comparttype_auto = comp.comparttype_auto,
                        implement_auto = template.implement_auto
                    });
                }
            }

            // Save the mapping of component types to the implement template.
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return Tuple.Create(-1, "Failed to map new component types to the new template. Exception: " + e.Message);
            }


            // Map the models to the template
            foreach (int modelId in existingImplementTemplate.EquipmentModels)
            {
                // Check whether the mapping already exists.
                var implement_make_model = await _context.GET_IMPLEMENT_MAKE_MODEL
                    .Where(imm => imm.implement_auto == template.implement_auto && imm.model_auto == modelId)
                    .FirstOrDefaultAsync();

                // Create the mapping if it doesn't already exist.
                if (implement_make_model == null)
                {
                    _context.GET_IMPLEMENT_MAKE_MODEL.Add(new GET_IMPLEMENT_MAKE_MODEL()
                    {
                        implement_auto = template.implement_auto,
                        model_auto = modelId
                    });
                }
            }

            // Save the mapping of models to the implement template.
            try
            {
                _context.SaveChanges();
            }
            catch (Exception e)
            {
                return Tuple.Create(-1, "Failed to map the new template to it's models. Exception: " + e.Message);
            }

            return Tuple.Create((int)template.implement_auto, "Template updated successfully. ");
        }

        /// <summary>
        /// Returns true if the componentName or part of it contains a reserved keyword.
        /// </summary>
        /// <param name="componentName"></param>
        /// <returns></returns>
        private bool containsReservedName(string componentName)
        {
            bool result = false;

            string[] reservedKeywords =
            {
                "Link",
                "Bushing",
                "Shoe",
                "Idler",
                "Carrier Roller",
                "Track Roller",
                "Sprocket",
                "Guard",
                "Track Elongation"
            };

            for(int i=0; i<reservedKeywords.Length; i++)
            {
                if(componentName.ToLower().Contains(reservedKeywords[i].ToLower()))
                {
                    return true;
                }
            }

            return result;
        }
    }
}
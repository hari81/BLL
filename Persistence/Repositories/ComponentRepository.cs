using BLL.Core.Domain;
using BLL.Core.Repositories;
using DAL;
using AutoMapper;
using BLL.Interfaces;
using System.Data.Entity;

namespace BLL.Persistence.Repositories
{
    public class ComponentRepository:Repository<GeneralComponent>, IComponentRepository
    {
        public IEquipment _equipment;
        public IUCSystem _system;
        public GeneralComponent _component;
        public ComponentRepository(IUndercarriageContext context) : base(context)
        {
            _equipment = new Equipment(context);
            _system = new UCSystem(context);
            _component = new GeneralComponent();
        }
        public ComponentRepository(IUndercarriageContext context, IEquipment equipment) : base(context)
        {
            _equipment = equipment;
            _system = new UCSystem(context);
            _component = new GeneralComponent();
        }
        public ComponentRepository(IUndercarriageContext context, IEquipment equipment, IUCSystem system) : base(context)
        {
            _equipment = equipment;
            _system = system;
            _component = new GeneralComponent();
        }
        public ComponentRepository(IUndercarriageContext context, IEquipment equipment, IUCSystem system, GeneralComponent component) : base(context)
        {
            _equipment = equipment;
            _system = system;
            _component = component;
        }
        public UndercarriageContext _context
        {
            get { return Context as UndercarriageContext; }
        }

        public GeneralComponent GetComponentWithId(int id)
        {
            return Mapper.Map<GeneralComponent>(_context.GENERAL_EQ_UNIT.Find(id));
        }
    }
}
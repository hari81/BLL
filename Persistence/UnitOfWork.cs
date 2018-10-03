using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using BLL.Core.Repositories;
using BLL.Persistence.Repositories;
using BLL.Core;
using DAL;

namespace BLL.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly UndercarriageContext _context;


        public UnitOfWork(UndercarriageContext context)
        {
            _context = context;
            Components = new ComponentRepository(_context);
            Users = new UserRepository(_context);
            Equipments = new EquipmentRepository(_context);
        }

        public IComponentRepository Components { get; private set; }
        public IUserRepository Users { get; private set; }
        public IEquipmentRepository Equipments { get; private set; }
        public int Complete()
        {
            return _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
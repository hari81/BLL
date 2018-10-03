using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Data.Entity;
using System.Web;
using BLL.Persistence.Repositories;
using BLL.Core.Repositories;

namespace BLL.Persistence.Repositories
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        protected readonly DAL.IUndercarriageContext Context;

        public Repository(DAL.IUndercarriageContext context)
        {

            Context = context;
        }

        public int longNullableToint(long? number)
        {
            if (number == null)
                return 0;
            if (number > Int32.MaxValue) //:) So Stupid
                return Int32.MaxValue;
            if (number < Int32.MinValue) // :))
                return Int32.MinValue;
            try{return (int)number;}catch { return 0; }
        }

        //public TEntity Get(int id)
        //{
        //    return Context.Set<TEntity>().Find(id);
        //}

        //public IEnumerable<TEntity> GetAll()
        //{
        //    return Context.Set<TEntity>().ToList();
        //}

        //public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        //{
        //    return Context.Set<TEntity>().Where(predicate);
        //}

        //public TEntity SingleOrDefault(Expression<Func<TEntity, bool>> predicate)
        //{
        //    return Context.Set<TEntity>().SingleOrDefault(predicate);
        //}

        //public void Add(TEntity entity)
        //{
        //    Context.Set<TEntity>().Add(entity);
        //}

        //public void AddRange(IEnumerable<TEntity> entities)
        //{
        //    Context.Set<TEntity>().AddRange(entities);
        //}

        //public void Remove(TEntity entity)
        //{
        //    Context.Set<TEntity>().Remove(entity);
        //}

        //public void RemoveRange(IEnumerable<TEntity> entities)
        //{
        //    Context.Set<TEntity>().RemoveRange(entities);
        //}
    }
}
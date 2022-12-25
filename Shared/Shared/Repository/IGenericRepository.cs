using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Repository
{
    public  interface IGenericRepository<T> where T : class
    {
        Task<bool> Delete(T entityToDelete);
        Task<bool> Delete(object id);
        Task<ICollection<T>> Get();
        Task<T> Get(object id);
        Task<T> Insert(T entity);
        Task<T> Update(T entityToUpdate);
    }
}

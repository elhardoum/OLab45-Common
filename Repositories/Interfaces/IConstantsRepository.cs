using OLabWebAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  public interface IConstantsRepository
  {
    bool Exists(uint id);
    IList<SystemConstants> GetAsync(uint id);
    IList<SystemConstants> GetByScopeAsync(uint parentId, string scopeType);
    uint UpsertAsync(SystemConstants phys, bool save = true);
    void Delete(SystemConstants phys);
    void SaveChanges();
  }
}

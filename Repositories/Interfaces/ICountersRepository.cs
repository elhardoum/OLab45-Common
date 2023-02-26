using OLabWebAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  public interface ICountersRepository
  {
    bool Exists(uint id);
    IList<SystemCounters> GetAsync(uint id);
    IList<SystemCounters> GetByScopeAsync(uint parentId, string scopeType);
    uint UpsertAsync(SystemCounters phys, bool save = true);
    void Delete(SystemCounters phys);
    void SaveChanges();
  }
}

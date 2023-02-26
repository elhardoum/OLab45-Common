using OLabWebAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  public interface ICounterActionsRepository
  {
    bool Exists(uint id);
    IList<SystemCounterActions> GetAsync(uint id);
    IList<SystemCounterActions> GetByMapIdAsync(uint mapId);
    IList<SystemCounterActions> GetByScopeAsync(uint parentId, string scopeType, string operationType);
    uint UpsertAsync(SystemCounterActions phys, bool save = true);
    void Delete(SystemCounterActions phys);

    void SaveChanges();
  }
}

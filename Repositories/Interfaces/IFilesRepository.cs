using OLabWebAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  public interface IFilesRepository
  {
    bool Exists(uint id);
    IList<SystemFiles> GetAsync(uint id);
    IList<SystemFiles> GetByScopeAsync(uint parentId, string scopeType);
    uint UpsertAsync(SystemFiles phys, bool save = true);
    void Delete(SystemFiles phys);
    void SaveChanges();
  }
}

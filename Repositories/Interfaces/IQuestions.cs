using OLabWebAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  public interface IQuestions
  {
    bool Exists(uint id);
    IList<SystemQuestions> GetAsync(uint id);
    IList<SystemQuestions> GetWithResponsesAsync(uint id);
    IList<SystemQuestions> GetByScopeAsync(uint parentId, string scopeType);
    IList<SystemQuestions> GetByScopeWithResponsesAsync(uint parentId, string scopeType);
    uint UpsertAsync(SystemQuestions phys, bool save = true);
    void Delete(SystemQuestions phys);
    void SaveChanges();
  }
}

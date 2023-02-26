using OLabWebAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  public interface IQuestionResponseRepository
  {
    bool Exists(uint id);
    IList<SystemQuestionResponses> GetAsync(uint id);

    IList<SystemQuestionResponses> GetWithResponsesAsync(uint id);
    IList<SystemQuestionResponses> GetByScopeAsync(uint parentId, string scopeType);
    IList<SystemQuestionResponses> GetByScopeWithResponsesAsync(uint parentId, string scopeType);
    uint UpsertAsync(SystemQuestionResponses phys, bool save = true);
    void Delete(SystemQuestionResponses phys);
    void SaveChanges();
  }
}

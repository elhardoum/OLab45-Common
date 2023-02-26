using OLabWebAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  internal interface IUserRepository
  {
    bool Exists(uint id);
    IList<Users> GetAsync(uint id);
    IList<Users> GetAsync(string name);
    uint UpsertAsync(Users phys, bool save = true);
    void Delete(Users phys);
    void SaveChanges();
  }
}

using OLabWebAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  public interface IMapNodeLinkRepository
  {
    Task<bool> ExistsAsync(uint id);
    Task<IList<MapNodeLinks>> GetAsync(int skip = 0, int take = 0);
    Task<MapNodeLinks?> GetAsync(uint id);
    Task<IList<MapNodeLinks>> GetByMapIdAsync(uint id);
    Task<uint> UpsertAsync(MapNodeLinks phys, bool save = true);
    void Delete(MapNodeLinks phys);
    void SaveChanges();
  }
}

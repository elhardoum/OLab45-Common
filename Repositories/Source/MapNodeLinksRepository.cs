using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using OLabWebAPI.Model;
using OLabWebAPI.Utils;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Humanizer.On;

namespace Repositories.Interfaces
{
  public class MapNodeLinkRepository : IMapNodeLinkRepository
  {
    private readonly OLabDBContext _context;
    private readonly OLabLogger _logger;

    public MapNodeLinkRepository(OLabLogger logger, OLabDBContext context)
    {
      _context = context;
      _logger = logger;
    }

    public void Delete(MapNodeLinks phys)
    {
      throw new NotImplementedException();
    }

    public Task<bool> ExistsAsync(uint id)
    {
      throw new NotImplementedException();
    }

    public Task<IList<MapNodeLinks>> GetAsync(int skip = 0, int take = 0)
    {
      throw new NotImplementedException();
    }

    public Task<MapNodeLinks?> GetAsync(uint id)
    {
      throw new NotImplementedException();
    }

    public async Task<IList<MapNodeLinks>> GetByMapIdAsync(uint id)
    {
      var items = await _context.MapNodeLinks.AsNoTracking().Where(x => x.MapId == id).ToListAsync();
      return items;
    }

    public void SaveChanges()
    {
      throw new NotImplementedException();
    }

    public Task<uint> UpsertAsync(MapNodeLinks phys, bool save = true)
    {
      throw new NotImplementedException();
    }
  }
}

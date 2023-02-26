using Microsoft.EntityFrameworkCore;
using OLab.Repository;
using OLabWebAPI.Model;
using OLabWebAPI.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
  public class MapNodeRepository : IMapNodeRepository
  {
    private readonly OLabDBContext _context;
    private readonly OLabLogger _logger;

    public MapNodeRepository(OLabLogger logger, OLabDBContext context)
    {
      _context = context;
      _logger = logger;
    }

    public void Delete(MapNodes phys)
    {
      throw new NotImplementedException();
    }

    public bool Exists(uint id)
    {
      throw new NotImplementedException();
    }

    public async Task<MapNodes?> GetAsync(uint id)
    {
      throw new NotImplementedException();
    }

    public async Task<IList<MapNodes>> GetByLinkIdsAsync(IList<uint> linkIds)
    {
      throw new NotImplementedException();
    }

    public async Task<IList<MapNodes>> GetByMapIdAsync(uint mapId)
    {
      var items = await _context.MapNodes.AsNoTracking().Where(x => x.MapId == mapId).ToListAsync();
      return items;
    }

    public async Task<MapNodes?> GetRootByMapIdAsync(uint mapId)
    {
      throw new NotImplementedException();
    }

    public void SaveChanges()
    {
      throw new NotImplementedException();
    }

    public async Task<uint> UpsertAsync(MapNodes phys, bool save = true)
    {
      throw new NotImplementedException();
    }
  }
}

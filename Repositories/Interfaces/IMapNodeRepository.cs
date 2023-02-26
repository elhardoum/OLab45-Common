using OLabWebAPI.Model;

namespace OLab.Repository
{
  public interface IMapNodeRepository
  {
    bool Exists(uint id);
    Task<IList<MapNodes>> GetByLinkIdsAsync(IList<uint> linkIds);
    Task<IList<MapNodes>> GetByMapIdAsync(uint mapId);
    Task<MapNodes?> GetAsync(uint id);
    Task<MapNodes?> GetRootByMapIdAsync(uint mapId);
    Task<uint> UpsertAsync(MapNodes phys, bool save = true);
    void Delete(MapNodes phys);
    void SaveChanges();
  }

}
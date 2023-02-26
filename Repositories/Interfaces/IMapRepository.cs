using OLabWebAPI.Model;

namespace OLab.Repository
{
  public interface IMapRepository
  {
    Task<bool> ExistsAsync(uint id);
    Task<IList<Maps>> GetAsync(int skip = 0, int take = 0);
    Task<IList<Maps>> GetTemplates();
    Task<Maps> AppendFromTemplateAsync(Maps target, Maps template);
    Task<Maps> CloneFromTemplateAsync(Maps target, Maps template);
    Task<Maps> CreateFromTemplateAsync(Maps map, Maps template);
    Task<Maps?> GetAsync(uint id);
    Task<Maps?> GetWithNodesAsync(uint id);
    Task<uint> UpsertAsync(Maps phys, bool save = true);
    void Delete(Maps phys);
    void SaveChanges();
  }

}
using OLab.Repository;
using OLabWebAPI.Model;
using Microsoft.Extensions.Logging;
using OLabWebAPI.Model.ReaderWriter;
using Microsoft.EntityFrameworkCore;
using OLabWebAPI.Utils;
using static Humanizer.On;
using System.Drawing;
using System.Collections;

/// <summary>
/// Summary description for Class1
/// </summary>
public class MapRepository : IMapRepository
{
  private readonly OLabDBContext _context;
  private readonly OLabLogger _logger;

  public MapRepository(OLabLogger logger, OLabDBContext context)
  {
    _context = context;
    _logger = logger;
  }

  public static MapRepository Instance(OLabLogger logger, OLabDBContext context)
  {
    return new MapRepository(logger, context);
  }

  public async Task<Maps> AppendFromTemplateAsync(Maps target, Maps template)
  {
    using var transaction = _context.Database.BeginTransaction();

    try
    {
      target = await CloneFromTemplateAsync(target, template);
      await _context.SaveChangesAsync();
      await transaction.CommitAsync();
    }
    catch (Exception)
    {
      await transaction.RollbackAsync();
      throw;
    }

    return target;
  }

  public async Task<Maps> CreateFromTemplateAsync(Maps target, Maps template)
  {
    using var transaction = _context.Database.BeginTransaction();

    try
    {
      target = await CloneFromTemplateAsync(target, template);
      await _context.SaveChangesAsync();
      await transaction.CommitAsync();
    }
    catch (Exception)
    {
      await transaction.RollbackAsync();
      throw;
    }

    return target;
  }

  public async Task<Maps> CloneFromTemplateAsync(Maps target, Maps template)
  {
    var oldMapId = template.Id;

    var reverseNodeIdMap = new Dictionary<uint, uint>();
    var mapBoundingBox = new MapNodeBoundingBox();
    var templateBoundingBox = new MapNodeBoundingBox();
    var points = new List<PointF>();

    // if no target passed in, then create a new one
    // which will be added to the database.  otherwise
    // try and copy the template nodes to the target
    if (target == null)
    {
      target = Maps.CreateDefault(template);
      target.Id = 0;
      _context.Entry(target).State = EntityState.Added;
      await _context.SaveChangesAsync();

      _logger.LogError($"  New Map {target.Id}");
    }
    else
    {
      // calculate a box containing all the target nodes
      mapBoundingBox.Load(target.MapNodes.ToList());
      points.Clear();
    }

    // calculate a box containing all the template nodes
    templateBoundingBox.Load(template.MapNodes.ToList());

    // calculate the positional differences between the template
    // and the original target
    var transformVector = templateBoundingBox.CalculateTransformTo(mapBoundingBox);

    _logger.LogDebug($"target BB: {mapBoundingBox.Rect}");
    _logger.LogDebug($"template BB: {templateBoundingBox.Rect}");
    _logger.LogDebug($"transform vector: {transformVector}");

    // reassign nodes to target target and add to target target
    foreach (var node in template.MapNodes)
    {
      if (node == null)
        continue;

      var oldNodeId = node.Id;
      MapNodes.Reassign(target, node);

      // if template node is a root node AND we are adding to an existing
      // target, then clear the root node flag for this node
      if (!mapBoundingBox.IsEmpty() && (node.TypeId == 1))
        node.TypeId = 2;

      if (!node.X.HasValue || !node.Y.HasValue)
      {
        _logger.LogError($"  Node {node.Id} has no X/Y coordinated");
        continue;
      }

      // transform position of node using the transform vector
      var nodeCoord = new PointF((float)node.X.Value, (float)node.Y.Value);
      var newCoord = nodeCoord + transformVector;

      _logger.LogDebug($"transforming: {nodeCoord} -> {newCoord}");

      node.X = newCoord.X;
      node.Y = newCoord.Y;

      _context.Entry(node).State = EntityState.Added;
      await _context.SaveChangesAsync();

      _logger.LogDebug($"  Node {oldNodeId} -> {node.Id}");
      reverseNodeIdMap[oldNodeId] = node.Id;
    }

    target.AppendMapNodes(template);

    _logger.LogDebug($"{reverseNodeIdMap.Count} node ids to be remapped");

    var templateLinks = _context.MapNodeLinks.AsNoTracking().Where(x => x.MapId == template.Id).ToList();

    foreach (var templateLink in templateLinks)
    {
      var oldNodeLinkId = templateLink.Id;
      MapNodeLinks.Reassign(reverseNodeIdMap, target.Id, templateLink);

      _context.Entry(templateLink).State = EntityState.Added;
      await _context.SaveChangesAsync();

      _logger.LogDebug($"  Link {oldNodeLinkId} -> {templateLink.Id}");
    }

    return target;
  }

  public void Delete(Maps phys)
  {
    _context.Maps.Remove(phys);
    _context.SaveChanges();
  }

  public async Task<bool> ExistsAsync(uint id)
  {
    return await _context.Maps.AnyAsync(x => x.Id == id);
  }

  public async Task<IList<Maps>> GetAsync(int skip = 0, int take = 0)
  {
    IList<Maps> items;

    if ((skip == 0) && (take == 0))
      items = await _context.Maps
        .OrderBy(x => x.Name)
        .ToListAsync();
    else
      items = await _context.Maps
      .Skip(skip)
      .Take(take)
      .OrderBy(x => x.Name)
      .ToListAsync();
    return items;
  }

  public async Task<Maps?> GetAsync(uint id)
  {
    var phys = await _context.Maps
      .FirstOrDefaultAsync(x => x.Id == id);
    if (phys != null && phys.Id == 0)
      return null;
    return phys;
  }

  public async Task<IList<Maps>> GetTemplates()
  {
    var items = await _context.Maps
      .Where(x => x.IsTemplate.HasValue && x.IsTemplate.Value == 1)
      .OrderBy(x => x.Name).ToListAsync();
    return items;
  }

  public async Task<Maps?> GetWithNodesAsync(uint id)
  {
    var phys = await GetAsync(id);
    if (phys != null)
      _context.Entry(phys).Collection(b => b.MapNodes).Load();

    return phys;
  }

  public void SaveChanges()
  {
    throw new NotImplementedException();
  }

  public async Task<uint> UpsertAsync(Maps phys, bool save = true)
  {
    if (phys.Id == 0)
      await _context.Maps.AddAsync(phys);
    else
      _context.Maps.Update(phys);

    if (save)
      await _context.SaveChangesAsync();

    return phys.Id;
  }
}

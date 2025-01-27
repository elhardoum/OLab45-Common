using Dawn;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OLabWebAPI.Data.Interface;
using OLabWebAPI.Dto;
using OLabWebAPI.Model;
using OLabWebAPI.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OLabWebAPI.Endpoints
{
  public class OlabEndpoint
  {
    protected readonly OLabDBContext dbContext;
    protected OLabLogger logger;
    protected string token;
    protected IUserContext _userContext;
    readonly AppSettings appSettings;

    public OlabEndpoint(OLabLogger logger, OLabDBContext context)
    {
      Guard.Argument(logger).NotNull(nameof(logger));
      Guard.Argument(context).NotNull(nameof(context));

      this.dbContext = context;
      this.logger = logger;
    }

    public OlabEndpoint(OLabLogger logger, IOptions<AppSettings> appSettings, OLabDBContext context)
    {
      Guard.Argument(logger).NotNull(nameof(logger));
      Guard.Argument(appSettings).NotNull(nameof(appSettings));
      Guard.Argument(context).NotNull(nameof(context));

      this.dbContext = context;
      this.appSettings = appSettings.Value;
      this.logger = logger;
    }

    public void SetUserContext(IUserContext userContext)
    {
      _userContext = userContext;
    }

    protected async ValueTask<Maps> GetMapAsync(uint id)
    {
      Maps phys = await dbContext.Maps.FirstOrDefaultAsync(x => x.Id == id);
      if ( phys != null)
        dbContext.Entry(phys).Collection(b => b.MapNodes).Load();

      return phys;
    }

    /// <summary>
    /// Attach parent information to scoped object
    /// </summary>
    /// <param name="dto"></param>
    [NonAction]
    protected void AttachParentObject(ScopedObjectDto dto)
    {
      if (dto.ImageableType == Constants.ScopeLevelServer)
      {
        Servers obj = dbContext.Servers.FirstOrDefault(x => x.Id == dto.ImageableId);
        dto.ParentInfo.Id = obj.Id;
        dto.ParentInfo.Name = obj.Name;
      }

      else if (dto.ImageableType == Constants.ScopeLevelMap)
      {
        Maps obj = dbContext.Maps.FirstOrDefault(x => x.Id == dto.ImageableId);
        dto.ParentInfo.Id = obj.Id;
        dto.ParentInfo.Name = obj.Name;
      }

      else if (dto.ImageableType == Constants.ScopeLevelNode)
      {
        MapNodes obj = dbContext.MapNodes.FirstOrDefault(x => x.Id == dto.ImageableId);
        dto.ParentInfo.Id = obj.Id;
        dto.ParentInfo.Name = obj.Title;
      }
    }

    [NonAction]
    protected async Task<MapNodes> GetMapRootNode(uint mapId, uint nodeId)
    {
      if (nodeId != 0)
        return await dbContext.MapNodes
          .Where(x => x.MapId == mapId && x.Id == nodeId)
          .FirstOrDefaultAsync(x => x.Id == nodeId);

      MapNodes item = await dbContext.MapNodes
          .Where(x => x.MapId == mapId && x.TypeId == 1)
          .FirstOrDefaultAsync(x => x.Id == nodeId);

      if (item == null)
        item = await dbContext.MapNodes
                  .Where(x => x.MapId == mapId)
                  .OrderBy(x => x.Id)
                  .FirstAsync();

      return item;
    }

    protected IList<IdName> GetMapIdNames()
    {
      return dbContext.Maps.Select(x => new IdName() { Id = x.Id, Name = x.Name }).ToList();
    }

    protected IList<IdName> GetNodeIdNames()
    {
      return dbContext.MapNodes.Select(x => new IdName() { Id = x.Id, Name = x.Title }).ToList();
    }

    protected IList<IdName> GetServerIdNames()
    {
      return dbContext.Servers.Select(x => new IdName() { Id = x.Id, Name = x.Name }).ToList();
    }

    protected IdName FindParentInfo(
      string scopeLevel,
      uint parentId,
      IList<IdName> maps,
      IList<IdName> nodes,
      IList<IdName> servers)
    {
      if (scopeLevel == Utils.Constants.ScopeLevelServer)
        return servers.FirstOrDefault(x => x.Id == parentId);
      if (scopeLevel == Utils.Constants.ScopeLevelMap)
        return maps.FirstOrDefault(x => x.Id == parentId);
      if (scopeLevel == Utils.Constants.ScopeLevelNode)
        return nodes.FirstOrDefault(x => x.Id == parentId);
      return null;
    }

    /// <summary>
    /// Get nodes for map
    /// </summary>
    /// <param name="map">Parent map to query for</param>
    /// <param name="enableWikiTanslation">PErform WikiTag translation</param>
    /// <returns>List of mapnode dto's</returns>
    [NonAction]
    protected async Task<IList<MapNodesFullDto>> GetNodesAsync(Maps map, bool enableWikiTanslation = true)
    {
      List<MapNodes> physList = await dbContext.MapNodes.Where(x => x.MapId == map.Id).ToListAsync();
      logger.LogDebug(string.Format("found {0} mapNodes", physList.Count));

      // patch up any malformed nodes that have 0/0 sizes, 
      // whic prevent them from being displayed
      var physNodes = physList.Where(x => (x.Height == 0) || (x.Width == 0)).ToList();
      foreach (MapNodes physNode in physNodes)
      {
        physNode.Height = 440;
        physNode.Width = 300;
      }

      IList<MapNodesFullDto> dtoList = new ObjectMapper.MapNodesFullMapper(logger, enableWikiTanslation).PhysicalToDto(physList);
      return dtoList;
    }

    /// <summary>
    /// Get node for map
    /// </summary>
    /// <param name="map">Map object</param>
    /// <param name="nodeId">Node id</param>
    /// <param name="hideHidden">flag to hide hidden links</param>
    /// <param name="enableWikiTanslation">PErform WikiTag translation</param>
    /// <returns>MapsNodesFullRelationsDto</returns>
    [NonAction]
    protected async Task<MapsNodesFullRelationsDto> GetNodeAsync(uint mapId, uint nodeId, bool hideHidden, bool enableWikiTanslation)
    {
      MapNodes phys = await dbContext.MapNodes
        .FirstOrDefaultAsync(x => x.MapId == mapId && x.Id == nodeId);

      if (phys == null)
        return new MapsNodesFullRelationsDto();

      // explicitly load the related objects.
      dbContext.Entry(phys).Collection(b => b.MapNodeLinksNodeId1Navigation).Load();

      var builder = new ObjectMapper.MapsNodesFullRelationsMapper(logger, enableWikiTanslation);
      MapsNodesFullRelationsDto dto = builder.PhysicalToDto(phys);

      var linkedIds = phys.MapNodeLinksNodeId1Navigation.Select(x => x.NodeId2).Distinct().ToList();
      var linkedNodes = dbContext.MapNodes.Where(x => linkedIds.Contains(x.Id)).ToList();

      // add destination node title to link information
      foreach (MapNodeLinksDto item in dto.MapNodeLinks)
      {
        MapNodes link = linkedNodes.Where(x => x.Id == item.DestinationId).FirstOrDefault();
        item.DestinationTitle = linkedNodes.Where(x => x.Id == item.DestinationId).Select(x => x.Title).FirstOrDefault();
        if (string.IsNullOrEmpty(item.LinkText))
          item.LinkText = item.DestinationTitle;
      }

      // if asked for, remove any hidden links
      if (hideHidden)
        dto.MapNodeLinks = dto.MapNodeLinks.Where(x => !x.IsHidden).ToList();

      return dto;
    }

    /// <summary>
    /// Get a mapnode
    /// </summary>
    /// <param name="nodeId">Node id</param>
    /// <returns></returns>
    [NonAction]
    public async ValueTask<MapNodes> GetMapNodeAsync(uint nodeId)
    {
      MapNodes item = await dbContext.MapNodes
          .FirstOrDefaultAsync(x => x.Id == nodeId);

      // explicitly load the related objects.
      dbContext.Entry(item).Collection(b => b.MapNodeLinksNodeId1Navigation).Load();

      return item;
    }

    /// <summary>
    /// Get question response
    /// </summary>
    /// <param name="id">id</param>
    /// <returns></returns>
    [NonAction]
    protected async ValueTask<SystemQuestionResponses> GetQuestionResponseAsync(uint id)
    {
      SystemQuestionResponses item = await dbContext.SystemQuestionResponses.FirstOrDefaultAsync(x => x.Id == id);
      return item;
    }

    /// <summary>
    /// Get constant
    /// </summary>
    /// <param name="id">question id</param>
    /// <returns></returns>
    [NonAction]
    protected async ValueTask<SystemConstants> GetConstantAsync(uint id)
    {
      SystemConstants item = await dbContext.SystemConstants
    .FirstOrDefaultAsync(x => x.Id == id);
      return item;
    }

    /// <summary>
    /// Get file
    /// </summary>
    /// <param name="id">file id</param>
    /// <returns></returns>
    [NonAction]
    protected async ValueTask<SystemFiles> GetFileAsync(uint id)
    {
      SystemFiles item = await dbContext.SystemFiles
    .FirstOrDefaultAsync(x => x.Id == id);
      return item;
    }

    /// <summary>
    /// Get question
    /// </summary>
    /// <param name="id">question id</param>
    /// <returns></returns>
    [NonAction]
    protected async ValueTask<SystemQuestions> GetQuestionSimpleAsync(uint id)
    {
      SystemQuestions item = await dbContext.SystemQuestions
    .FirstOrDefaultAsync(x => x.Id == id);
      return item;
    }

    /// <summary>
    /// Get question with responses
    /// </summary>
    /// <param name="id">question id</param>
    /// <returns></returns>
    [NonAction]
    protected async ValueTask<SystemQuestions> GetQuestionAsync(uint id)
    {
      SystemQuestions item = await dbContext.SystemQuestions
    .Include(x => x.SystemQuestionResponses)
    .FirstOrDefaultAsync(x => x.Id == id);
      return item;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="sinceTime"></param>
    /// <param name="scopeLevel"></param>
    /// <returns></returns>
    [NonAction]
    protected async ValueTask<ScopedObjects> GetScopedObjectsDynamicAsync(
      uint parentId,
      uint sinceTime,
      string scopeLevel)
    {
      var phys = new ScopedObjects
      {
        Counters = await GetScopedCountersAsync(scopeLevel, parentId, sinceTime)
      };

      return phys;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="scopeLevel"></param>
    /// <returns></returns>
    [NonAction]
    protected async ValueTask<ScopedObjects> GetScopedObjectsAllAsync(
      uint parentId,
      string scopeLevel)
    {
      var phys = new ScopedObjects
      {
        Constants = await GetScopedConstantsAsync(parentId, scopeLevel),
        Questions = await GetScopedQuestionsAsync(parentId, scopeLevel),
        Files = await GetScopedFilesAsync(parentId, scopeLevel),
        Scripts = await GetScopedScriptsAsync(parentId, scopeLevel),
        Themes = await GetScopedThemesAsync(parentId, scopeLevel),
        Counters = await GetScopedCountersAsync(scopeLevel, parentId, 0)
      };

      if (scopeLevel == Constants.ScopeLevelMap)
      {
        var items = new List<SystemCounterActions>();
        items.AddRange(await dbContext.SystemCounterActions.Where(x =>
            x.MapId == parentId).ToListAsync());

        phys.CounterActions.AddRange(items);
      }

      return phys;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="scopeLevel"></param>
    /// <returns></returns>
    [NonAction]
    protected async Task<List<SystemConstants>> GetScopedConstantsAsync(uint parentId, string scopeLevel)
    {
      var items = new List<SystemConstants>();

      items.AddRange(await dbContext.SystemConstants.Where(x =>
        x.ImageableType == scopeLevel && x.ImageableId == parentId).ToListAsync());

      return items;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="scopeLevel"></param>
    /// <returns></returns>
    [NonAction]
    protected async Task<List<SystemFiles>> GetScopedFilesAsync(uint parentId, string scopeLevel)
    {
      var items = new List<SystemFiles>();

      items.AddRange(await dbContext.SystemFiles.Where(x =>
        x.ImageableType == scopeLevel && x.ImageableId == parentId).ToListAsync());

      foreach (SystemFiles item in items)
      {
        var subPath = $"{scopeLevel}/{parentId}/{item.Path}";
        var physicalPath = Path.Combine(appSettings.WebsitePublicFilesDirectory, subPath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(physicalPath))
          item.OriginUrl = $"/{Path.GetFileName(appSettings.WebsitePublicFilesDirectory)}/{subPath}";
        else
          item.OriginUrl = null;
      }

      return items;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="scopeLevel"></param>
    /// <returns></returns>
    [NonAction]
    protected async Task<List<SystemQuestions>> GetScopedQuestionsAsync(uint parentId, string scopeLevel)
    {
      var items = new List<SystemQuestions>();

      items.AddRange(await dbContext.SystemQuestions
        .Where(x => x.ImageableType == scopeLevel && x.ImageableId == parentId)
        .Include("SystemQuestionResponses")
        .ToListAsync());

      // order the responses by Order field
      foreach (SystemQuestions item in items)
        item.SystemQuestionResponses = item.SystemQuestionResponses.OrderBy(x => x.Order).ToList();

      return items;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="scopeLevel"></param>
    /// <returns></returns>
    [NonAction]
    protected async Task<List<SystemThemes>> GetScopedThemesAsync(uint parentId, string scopeLevel)
    {
      var items = new List<SystemThemes>();

      items.AddRange(await dbContext.SystemThemes.Where(x =>
        x.ImageableType == scopeLevel && x.ImageableId == parentId).ToListAsync());

      return items;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="scopeLevel"></param>
    /// <returns></returns>
    [NonAction]
    protected async Task<List<SystemScripts>> GetScopedScriptsAsync(uint parentId, string scopeLevel)
    {
      var items = new List<SystemScripts>();

      items.AddRange(await dbContext.SystemScripts.Where(x =>
        x.ImageableType == scopeLevel && x.ImageableId == parentId).ToListAsync());

      return items;
    }

    /// <summary>
    /// Get counter 
    /// </summary>
    /// <param name="id">counter id</param>
    /// <returns>Counter</returns>
    [NonAction]
    protected async Task<SystemCounters> GetCounterAsync(uint id)
    {
      SystemCounters phys = await dbContext.SystemCounters.SingleOrDefaultAsync(x => x.Id == id);
      if (phys.Value == null)
        phys.Value = new List<byte>().ToArray();
      if (phys.StartValue == null)
        phys.StartValue = new List<byte>().ToArray();
      return phys;
    }

    /// <summary>
    /// Get counters associated with a 'parent' object 
    /// </summary>
    /// <param name="scopeLevel">Scope level of parent (Maps, MapNodes, etc)</param>
    /// <param name="parentId">Id of parent object</param>
    /// <param name="sinceTime">(optional) looks for values changed since a (unix) time</param>
    /// <returns>List of counters</returns>
    [NonAction]
    protected async Task<List<SystemCounters>> GetScopedCountersAsync(string scopeLevel, uint parentId, uint sinceTime = 0)
    {
      var items = new List<SystemCounters>();

      if (sinceTime != 0)
      {
        // generate DateTime from sinceTime
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(sinceTime).ToLocalTime();
        items.AddRange(await dbContext.SystemCounters.Where(x =>
          x.ImageableType == scopeLevel && x.ImageableId == parentId && x.UpdatedAt >= dateTime).ToListAsync());
      }
      else
      {
        items.AddRange(await dbContext.SystemCounters.Where(x =>
          x.ImageableType == scopeLevel && x.ImageableId == parentId).ToListAsync());
      }

      return items;
    }
  }
}
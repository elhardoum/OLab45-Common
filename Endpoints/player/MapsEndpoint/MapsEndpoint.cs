using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OLab.Repository;
using OLabWebAPI.Common;
using OLabWebAPI.Common.Exceptions;
using OLabWebAPI.Data.Exceptions;
using OLabWebAPI.Data.Interface;
using OLabWebAPI.Dto;
using OLabWebAPI.Model;
using OLabWebAPI.Model.ReaderWriter;
using OLabWebAPI.ObjectMapper;
using OLabWebAPI.Utils;
using Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OLabWebAPI.Endpoints.Player
{
  public partial class MapsEndpoint : OlabEndpoint
  {

    private readonly IMapRepository _mapRepo;
    private readonly IMapNodeLinkRepository _linkRepo;
    private readonly IMapNodeRepository _nodeRepo;

    public MapsEndpoint(
      OLabLogger logger,
      IOptions<AppSettings> appSettings,
      OLabDBContext context) : base(logger, appSettings, context)
    {
      _mapRepo = new MapRepository(logger, context);
      _linkRepo = new MapNodeLinkRepository(logger, context);
      _nodeRepo = new MapNodeRepository(logger, context);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    //public Model.Maps GetSimple(OLabDBContext context, uint id)
    //{
    //  Maps phys = context.Maps.Include(x => x.SystemCounterActions).FirstOrDefault(x => x.Id == id);
    //  return phys;
    //}

    /// <summary>
    /// Get a list of maps
    /// </summary>
    /// <param name="take">Max number of records to return</param>
    /// <param name="skip">SKip over a number of records</param>
    /// <returns>IActionResult</returns>
    public async Task<OLabAPIPagedResponse<MapsDto>> GetAsync(
      IOLabAuthentication auth,
      int? take,
      int? skip)
    {
      logger.LogDebug($"GetAsync([FromQuery] int? take={take}, [FromQuery] int? skip={skip})");

      IList<Maps> items;
      var total = 0;
      var remaining = 0;

      if (!skip.HasValue)
        skip = 0;

      if (take.HasValue && skip.HasValue)
      {
        items = await _mapRepo.GetAsync(skip.Value, take.Value);
        remaining = total - take.Value - skip.Value;
      }
      else
        items = await _mapRepo.GetAsync();

      total = items.Count;

      logger.LogDebug(string.Format("found {0} maps", items.Count));

      IList<MapsDto> dtoList = new MapsMapper(logger).PhysicalToDto(items);

      // filter out any maps user does not have access to.
      dtoList = dtoList.Where(x => auth.HasAccess("R", Utils.Constants.ScopeLevelMap, x.Id)).ToList();
      return new OLabAPIPagedResponse<MapsDto> { Data = dtoList, Remaining = remaining, Count = total };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<MapsFullDto> GetAsync(
      IOLabAuthentication auth,
      uint id)
    {
      logger.LogDebug($"GetAsync(uint id={id})");

      // test if user has access to map.
      if (!auth.HasAccess("R", Utils.Constants.ScopeLevelMap, id))
        throw new OLabUnauthorizedException(Utils.Constants.ScopeLevelMap, id);

      var map = await _mapRepo.GetAsync(id);
      if (map == null)
        throw new OLabObjectNotFoundException(Utils.Constants.ScopeLevelNode, id);

      Maps phys = await GetMapAsync(id);
      MapsFullDto dto = new MapsFullMapper(logger).PhysicalToDto(phys);

      return dto;
    }

    /// <summary>
    /// Append template to an existing map
    /// </summary>
    /// <param name="mapId">Map to add template to</param>
    /// <param name="CreateMapRequest.templateId">Template to add to map</param>
    /// <returns>IActionResult</returns>
    public async Task<ExtendMapResponse> PostExtendMapAsync(
      IOLabAuthentication auth,
      uint mapId,
      ExtendMapRequest body)
    {
      logger.LogDebug($"MapsController.PostExtendMapAsync(mapId = {mapId}, templateId = {body.TemplateId})");

      // test if user has access to map.
      if (!auth.HasAccess("W", Utils.Constants.ScopeLevelMap, mapId))
        throw new OLabUnauthorizedException(Utils.Constants.ScopeLevelMap, mapId);

      //Maps map = await dbContext.Maps
      //  .AsNoTracking()
      //  .Include(x => x.MapNodes)
      //  .FirstOrDefaultAsync(x => x.Id == mapId);
      var map = await _mapRepo.GetWithNodesAsync(mapId);
      if (map == null)
        throw new OLabObjectNotFoundException(Utils.Constants.ScopeLevelMap, mapId);

      //Maps template = await dbContext.Maps
      //  .AsNoTracking()
      //  .Include(x => x.MapNodes)
      //  .FirstOrDefaultAsync(x => x.Id == body.TemplateId);
      var template = await _mapRepo.GetWithNodesAsync(body.TemplateId);
      if (template == null)
        throw new OLabObjectNotFoundException(Utils.Constants.ScopeLevelMap, body.TemplateId);

      //map = await MapsReaderWriter.Instance(logger.GetLogger(), dbContext)
      //  .CreateMapWithTemplateAsync(map, template);
      map = await _mapRepo.CreateFromTemplateAsync(map, template);

      //var mapLinks = dbContext.MapNodeLinks.AsNoTracking().Where(x => x.MapId == map.Id).ToList();
      var mapLinks = await _linkRepo.GetByMapIdAsync(map.Id);
      IList<MapNodeLinksDto> linksDto = new MapNodeLinksMapper(logger).PhysicalToDto(mapLinks);

      //var mapNodes = dbContext.MapNodes.AsNoTracking().Where(x => x.MapId == map.Id).ToList();
      var mapNodes = await _nodeRepo.GetByMapIdAsync(map.Id);
      IList<MapNodesFullDto> nodesDto = new MapNodesFullMapper(logger).PhysicalToDto(mapNodes);

      var dto = new ExtendMapResponse
      {
        Nodes = nodesDto,
        Links = linksDto
      };

      return dto;
    }

    /// <summary>
    /// Create new map (using optional template)
    /// </summary>
    /// <param name="body">Create map request body</param>
    /// <returns>IActionResult</returns>
    public async Task<MapsFullRelationsDto> PostCreateMapAsync(
      IOLabAuthentication auth,
      CreateMapRequest body)
    {
      logger.LogDebug($"MapsController.PostCreateMapAsync(templateId = {body.TemplateId})");

      // test if user has access to map.
      if (!auth.HasAccess("W", Utils.Constants.ScopeLevelMap, 0))
        throw new OLabUnauthorizedException(Utils.Constants.ScopeLevelMap, 0);

      Maps map = null;

      // if no templateId passed in, create default map
      if (!body.TemplateId.HasValue)
      {
        map = Maps.CreateDefault();
        await _mapRepo.UpsertAsync(map);
        //dbContext.Maps.Add(map);
        //await dbContext.SaveChangesAsync();
      }
      else
      {
        //Maps templateMap = await dbContext.Maps
        //  .AsNoTracking()
        //  .Include(x => x.MapNodes)
        //  .FirstOrDefaultAsync(x => x.Id == body.TemplateId);
        var templateMap = await _mapRepo.GetWithNodesAsync(body.TemplateId.Value);

        if (templateMap == null)
          throw new OLabObjectNotFoundException(Utils.Constants.ScopeLevelMap, body.TemplateId.Value);

        //map = await MapsReaderWriter.Instance(logger.GetLogger(), dbContext)
        //  .CreateMapWithTemplateAsync(map, templateMap);
        map = await _mapRepo.CreateFromTemplateAsync(map, templateMap);
      }

      MapsFullRelationsDto dto = new MapsFullRelationsMapper(logger).PhysicalToDto(map);
      return dto;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <param name="mapdto"></param>
    /// <returns></returns>
    public async Task PutAsync(
      IOLabAuthentication auth,
      uint id,
      MapsFullDto mapdto)
    {
      logger.LogDebug($"MapsController.PutAsync(id = {id})");

      Maps map = new MapsFullMapper(logger).DtoToPhysical(mapdto);

      // test if user has access to map.
      if (!auth.HasAccess("W", Utils.Constants.ScopeLevelMap, map.Id))
        throw new OLabUnauthorizedException(Utils.Constants.ScopeLevelMap, map.Id);

      if (id != map.Id)
        throw new OLabObjectNotFoundException(Utils.Constants.ScopeLevelMap, map.Id);

      //dbContext.Entry(map).State = EntityState.Modified;

      try
      {
        //await dbContext.SaveChangesAsync();
        await _mapRepo.UpsertAsync(map);
      }
      catch (DbUpdateConcurrencyException)
      {
        var existingMap = await _mapRepo.GetAsync(id);
        if (existingMap == null)
          throw new OLabObjectNotFoundException(Utils.Constants.ScopeLevelMap, id);
      }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteAsync(
      IOLabAuthentication auth,
      uint id)
    {
      logger.LogDebug($"DeleteAsync(uint mapId={id})");

      // test if user has access to map.
      if (!auth.HasAccess("D", Utils.Constants.ScopeLevelMap, id))
        throw new OLabUnauthorizedException(Utils.Constants.ScopeLevelMap, id);

      var map = await _mapRepo.GetAsync(id);
      if (map == null)
        throw new OLabObjectNotFoundException(Utils.Constants.ScopeLevelMap, id);

      //dbContext.Maps.Remove(map);
      //await dbContext.SaveChangesAsync();
      _mapRepo.Delete(map);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mapId"></param>
    /// <returns></returns>
    public async Task<IList<MapNodeLinksFullDto>> GetLinksAsync(
      IOLabAuthentication auth,
      uint mapId)
    {
      logger.LogDebug($"GetLinksAsync(uint mapId={mapId})");

      // test if user has access to map.
      if (!auth.HasAccess("R", Utils.Constants.ScopeLevelMap, mapId))
        throw new OLabUnauthorizedException(Utils.Constants.ScopeLevelMap, mapId);

      Maps map = await _mapRepo.GetAsync(mapId);
      if (map == null)
        throw new OLabObjectNotFoundException(Utils.Constants.ScopeLevelMap, mapId);

      //List<MapNodeLinks> items = await dbContext.MapNodeLinks.Where(x => x.MapId == mapId).ToListAsync();
      var items = await _linkRepo.GetByMapIdAsync(mapId);
      logger.LogDebug(string.Format("found {0} MapNodeLinks", items.Count));

      IList<MapNodeLinksFullDto> dtoList = new MapNodeLinksFullMapper(logger).PhysicalToDto(items);
      return dtoList;
    }

  }
}

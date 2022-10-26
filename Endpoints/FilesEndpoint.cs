using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OLabWebAPI.Model;
using OLabWebAPI.Dto;
using OLabWebAPI.ObjectMapper;
using OLabWebAPI.Common;
using OLabWebAPI.Interface;
using OLabWebAPI.Utils;
using System.IO;
using Microsoft.Extensions.Options;
using OLabWebAPI.Common.Exceptions;

namespace OLabWebAPI.Endpoints
{
  public partial class FilesEndpoint : OlabEndpoint
  {

    private readonly AppSettings _appSettings;

    public FilesEndpoint(
      OLabLogger logger,
      IOptions<AppSettings> appSettings,
      OLabDBContext context,
      IOlabAuthentication auth) : base(logger, context, auth)
    {
      _appSettings = appSettings.Value;
    }

    private bool Exists(uint id)
    {
      return context.SystemFiles.Any(e => e.Id == id);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="take"></param>
    /// <param name="skip"></param>
    /// <returns></returns>
    public async Task<OLabAPIPagedResponse<FilesDto>> GetAsync(int? take, int? skip)
    {
      logger.LogDebug($"FilesController.GetAsync([FromQuery] int? take={take}, [FromQuery] int? skip={skip})");

      var Files = new List<SystemFiles>();
      var total = 0;
      var remaining = 0;

      if (!skip.HasValue)
        skip = 0;

      Files = await context.SystemFiles.OrderBy(x => x.Name).ToListAsync();
      total = Files.Count;

      if (take.HasValue && skip.HasValue)
      {
        Files = Files.Skip(skip.Value).Take(take.Value).ToList();
        remaining = total - take.Value - skip.Value;
      }

      logger.LogDebug(string.Format("found {0} Files", Files.Count));

      var dtoList = new ObjectMapper.Files(logger).PhysicalToDto(Files);
      return new OLabAPIPagedResponse<FilesDto> { Data = dtoList, Remaining = remaining, Count = total };

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<FilesFullDto> GetAsync(uint id)
    {

      logger.LogDebug($"FilesController.GetAsync(uint id={id})");

      if (!Exists(id))
        throw new OLabObjectNotFoundException("Files", id);

      var phys = await context.SystemFiles.FirstAsync(x => x.Id == id);
      var dto = new ObjectMapper.FilesFull(logger).PhysicalToDto(phys);

      // test if user has access to object
      var accessResult = auth.HasAccess("R", dto);
      if (accessResult is UnauthorizedResult)
        throw new OLabUnauthorizedException("Files", id);

      AttachParentObject(dto);
      return dto;

    }

    /// <summary>
    /// Saves a file edit
    /// </summary>
    /// <param name="id">file id</param>
    /// <returns>IActionResult</returns>
    public async Task PutAsync(uint id, FilesFullDto dto)
    {

      logger.LogDebug($"PutAsync(uint id={id})");

      dto.ImageableId = dto.ParentObj.Id;

      // test if user has access to object
      var accessResult = auth.HasAccess("W", dto);
      if (accessResult is UnauthorizedResult)
        throw new OLabUnauthorizedException("Files", id);

      try
      {
        var builder = new FilesFull(logger);
        var phys = builder.DtoToPhysical(dto);

        phys.UpdatedAt = DateTime.Now;

        context.Entry(phys).State = EntityState.Modified;
        await context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        var existingObject = await GetConstantAsync(id);
        if (existingObject == null)
          throw new OLabObjectNotFoundException("Files", id);
      }

    }

    /// <summary>
    /// Create new file
    /// </summary>
    /// <param name="phys">Physical object to save</param>
    /// <returns>FilesFullDto</returns>
    public async Task<FilesFullDto> PostAsync(SystemFiles phys)
    {
      logger.LogDebug($"FilesController.PostAsync()");
      var builder = new FilesFull(logger);
      var dto = builder.PhysicalToDto(phys);

      // test if user has access to object
      var accessResult = auth.HasAccess("W", dto);
      if (accessResult is UnauthorizedResult)
        throw new OLabUnauthorizedException("Files", 0);

      phys.CreatedAt = DateTime.Now;

      context.SystemFiles.Add(phys);
      await context.SaveChangesAsync();

      dto = builder.PhysicalToDto(phys);
      return dto;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task DeleteAsync(uint id)
    {

      logger.LogDebug($"ConstantsEndpoint.DeleteAsync(uint id={id})");

      if (!Exists(id))
          throw new OLabObjectNotFoundException("Files", id);

      try
      {
        var phys = await GetFileAsync(id);
        var dto = new FilesFull(logger).PhysicalToDto(phys);

        // test if user has access to object
        var accessResult = auth.HasAccess("W", dto);
        if (accessResult is UnauthorizedResult)
          throw new OLabUnauthorizedException("Constants", id);

        context.SystemFiles.Remove(phys);
        await context.SaveChangesAsync();

      }
      catch (DbUpdateConcurrencyException)
      {
        var existingObject = await GetFileAsync(id);
        if (existingObject == null)
          throw new OLabObjectNotFoundException("Files", id);
      }

    }

  }

}

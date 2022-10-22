using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OLabWebAPI.Model;
using OLabWebAPI.Dto;
using OLabWebAPI.ObjectMapper;
using OLabWebAPI.Common;
using OLabWebAPI.Interface;
using OLabWebAPI.Utils;

namespace OLabWebAPI.Endpoints
{
  public partial class QuestionResponsesEndpoint : OlabEndpoint
  {

    public QuestionResponsesEndpoint( 
      OLabLogger logger, 
      OLabDBContext context, 
      IOlabAuthentication auth) : base(logger, context, auth)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    private bool Exists(uint id)
    {
      return context.SystemQuestionResponses.Any(e => e.Id == id);
    }

    /// <summary>
    /// Saves a object edit
    /// </summary>
    /// <param name="id">question id</param>
    /// <returns>IActionResult</returns>
    public async Task<IActionResult> PutAsync(uint id, QuestionResponsesDto dto)
    {
      logger.LogDebug($"PutAsync(uint id={id})");

      try
      {
        var physQuestionTemp = await GetQuestionSimpleAsync(dto.QuestionId);
        var builder = new QuestionsFull(logger);
        var dtoQuestionTemp = builder.PhysicalToDto(physQuestionTemp);

        // test if user has access to object
        var accessResult = auth.HasAccess("W", dtoQuestionTemp);
        if (accessResult is UnauthorizedResult)
          return accessResult;

        var responsebuilder = new QuestionResponses(logger, dtoQuestionTemp);
        var physResponse = responsebuilder.DtoToPhysical(dto);

        physResponse.UpdatedAt = DateTime.Now;

        context.SystemQuestionResponses.Update(physResponse);
        await context.SaveChangesAsync();

        return OLabObjectResult<QuestionResponsesDto>.Result(dto);

      }
      catch (Exception ex)
      {
        return OLabServerErrorResult.Result(ex.Message);
      }
    }

    /// <summary>
    /// Create new object
    /// </summary>
    /// <param name="dto">object data</param>
    /// <returns>IActionResult</returns>
    public async Task<IActionResult> PostAsync(QuestionResponsesDto dto)
    {
      logger.LogDebug($"QuestionResponsesController.PostAsync({dto.Response})");

      try
      {
        var physQuestionTemp = await GetQuestionSimpleAsync(dto.QuestionId);
        var questionBuilder = new QuestionsFull(logger);
        var dtoQuestionTemp = questionBuilder.PhysicalToDto(physQuestionTemp);

        // test if user has access to object
        var accessResult = auth.HasAccess("W", dtoQuestionTemp);
        if (accessResult is UnauthorizedResult)
          return accessResult;

        var responsebuilder = new QuestionResponses(logger, dtoQuestionTemp);
        var physResponse = responsebuilder.DtoToPhysical(dto);

        context.SystemQuestionResponses.Add(physResponse);
        await context.SaveChangesAsync();

        var returnDto = responsebuilder.PhysicalToDto(physResponse);
        return OLabObjectResult<QuestionResponsesDto>.Result(returnDto);

      }
      catch (Exception ex)
      {
        return OLabServerErrorResult.Result(ex.Message);
      }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<IActionResult> DeleteAsync(uint id)
    {
      logger.LogDebug($"QuestionResponsesController.DeleteAsync(uint id={id})");

      if (!Exists(id))
        return OLabNotFoundResult<uint>.Result(id);

      try
      {
        var physResponse = await GetQuestionResponseAsync(id);
        var physQuestion = await GetQuestionAsync(physResponse.QuestionId.Value);
        var questionBuilder = new QuestionsFull(logger);
        var dtoQuestion = questionBuilder.PhysicalToDto(physQuestion);

        // test if user has access to objectdtoQuestion
        var accessResult = auth.HasAccess("W", dtoQuestion);
        if (accessResult is UnauthorizedResult)
          return accessResult;

        context.SystemQuestionResponses.Remove(physResponse);
        await context.SaveChangesAsync();
        return null;
      }
      catch (Exception ex)
      {
        return OLabServerErrorResult.Result(ex.Message);
      }

    }

  }

}

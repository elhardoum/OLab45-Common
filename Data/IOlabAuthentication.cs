using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using OLabWebAPI.Dto;

namespace OLabWebAPI.Interface
{
    public interface IOlabAuthentication
    {
      IActionResult HasAccess(string acl, ScopedObjectDto dto);
      bool HasAccess(string acl, string objectType, uint? objectId);

    }
}
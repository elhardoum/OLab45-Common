using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OLabWebAPI.Dto
{
  public class DynamicScopedObject
  {
    [JsonProperty("counters")]
    public List<CountersDto> Counters { get; set; }

    public DynamicScopedObject()
    {
      Counters = new List<CountersDto>();
    }
  }

  public class ServerDynamicScopedObjects : DynamicScopedObject
  {
    public ServerDynamicScopedObjects()
    {
    }
  }

  public class MapDynamicScopedObjects : DynamicScopedObject
  {
    public MapDynamicScopedObjects()
    {
    }
  }

  public class NodeDynamicScopedObjects : DynamicScopedObject
  {
    public NodeDynamicScopedObjects()
    {
    }
  }

  public class DynamicScopedObjectsDto
  {
    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; private set; }
    [JsonProperty("server")]
    public ServerDynamicScopedObjects Server { get; set; }
    [JsonProperty("map")]
    public MapDynamicScopedObjects Map { get; set; }
    [JsonProperty("node")]
    public NodeDynamicScopedObjects Node { get; set; }

    public DynamicScopedObjectsDto()
    {
      UpdatedAt = DateTime.Now;
      Server = new ServerDynamicScopedObjects();
      Map = new MapDynamicScopedObjects();
      Node = new NodeDynamicScopedObjects();
    }

    public bool IsEmpty()
    {
      return ( Map == null ) && ( Node == null ) && ( Server == null );
    }

    /// <summary>
    /// Get counter from scoped object lists
    /// </summary>
    /// <param name="id"></param>
    /// <param name="scopeType">(optional) desired scope level</param>
    /// <returns></returns>
    public CountersDto GetCounter(uint id, string scopeType = "")
    {
      CountersDto dto = null;

      if (string.IsNullOrEmpty(scopeType))
      {
        dto = Server.Counters.FirstOrDefault(x => x.Id == id);
        if (dto == null)
          dto = Map.Counters.FirstOrDefault(x => x.Id == id);
        if (dto == null)
          dto = Node.Counters.FirstOrDefault(x => x.Id == id);

      }

      return dto;
    }
  }
}

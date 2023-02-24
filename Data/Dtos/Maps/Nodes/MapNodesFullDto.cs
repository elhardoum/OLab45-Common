using Newtonsoft.Json;
using System;

namespace OLabWebAPI.Dto
{
  public class MapNodesFullDto : MapNodesDto
  {
    [JsonProperty("isEnd")]
    public bool? End { get; set; }
    public bool? Kfp { get; set; }
    public bool? Probability { get; set; }
    public bool? Undo { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public int IsPrivate { get; set; }
    public int? Collapsed { get; set; }
    public int? ForceReload { get; set; }
    public int? Height { get; set; }
    public uint? LinkTypeId { get; set; }
    public int? Locked { get; set; }
    public int? PriorityId { get; set; }
    public uint? TypeId { get; set; }
    public int? VisitOnce { get; set; }
    public int? Width { get; set; }
    public sbyte ShowInfo { get; set; }
    public string Annotation { get; set; }
    public string Conditional { get; set; }
    public string ConditionalMessage { get; set; }
    public string Info { get; set; }
    [JsonProperty("color")]
    public string Rgb { get; set; }
    public string Text { get; set; }
    public uint? LinkStyleId { get; set; }
    public uint? MapId { get; set; }

    [JsonProperty("data")]
    public MapNodesFullDto Data { get; set; }
  }
}

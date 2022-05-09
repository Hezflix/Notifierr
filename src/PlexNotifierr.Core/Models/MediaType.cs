using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace PlexNotifierr.Core.Models
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MediaType
    {
        [EnumMember(Value = "Show")]
        Show = 0,

        [EnumMember(Value = "Season")]
        Season = 1,

        [EnumMember(Value = "Episode")]
        Episode = 2,
    }
}
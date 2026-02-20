using Newtonsoft.Json;
using SilverDasher.ACT.Storages;

namespace SilverDasher.ACT.Models.Messages;

internal abstract class Message {
    [JsonProperty("t")] internal string type;

    [JsonProperty("v")] internal int version = DataStorage.Version;

    [JsonProperty("id")] internal int id;

    [JsonProperty("i")] internal uint instance;

    [JsonProperty("w")] internal uint world;

    [JsonProperty("ts")] internal string timestamp = Utils.CurrentUTCTimeStamp();

    [JsonProperty("m")] internal uint map;

    [JsonProperty("c")] internal Coordinate coordinate;
}
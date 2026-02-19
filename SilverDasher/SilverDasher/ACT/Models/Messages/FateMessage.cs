using Newtonsoft.Json;

namespace SilverDasher.ACT.Models.Messages;

internal class FateMessage : Message {
    [JsonProperty("p")] internal int Progress;

    [JsonProperty("lt")] internal string LeftTime;

    internal FateMessage() {
        type = "fate";
    }
}
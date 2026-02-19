using Newtonsoft.Json;

namespace SilverDasher.ACT.Models.Messages;

internal class HuntMessage : Message {
    [JsonProperty("hp")] internal int health;

    internal HuntMessage() {
        type = "hunt";
    }
}
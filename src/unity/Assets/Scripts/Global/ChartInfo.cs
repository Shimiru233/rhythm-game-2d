using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Assets.Scripts.Global
{
    [Serializable]
    public class ChartInfo
    {

        [JsonProperty("songName")]
        public string SongName { get; set; }

        [JsonProperty("artist")]
        public string Artist { get; set; }

        [JsonProperty("songBpm")]
        public string SongBpm { get; set; }

        [JsonProperty("notes")]
        public List<Note> Notes { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using TShockAPI;
using Newtonsoft.Json;
using Auxiliary.Configuration;
using System.Text.Json.Serialization;

namespace Banker
{
	public class BankerSettings : ISettings
	{
		[JsonPropertyName("CurrencyNameSingular")]
		public string CurrencyNameSingular { get; set; } = "dollar";
        
		[JsonPropertyName("CurrencyNamePlural")]
        public string CurrencyNamePlural { get; set; } = "dollars";

        [JsonPropertyName("ExcludedMobs")]
        public List<int> ExcludedMobs { get; set; } = new List<int>() { 211, 210 };

        [JsonPropertyName("EnableMobDrops")]
        public bool EnableMobDrops { get; set; } = false;
        
		[JsonPropertyName("AnnounceMobDrops")]
        public bool AnnounceMobDrops { get; set; } = true;

        [JsonPropertyName("RewardsForPlaying")]
        public bool RewardsForPlaying { get; set; } = false;

        [JsonPropertyName("RewardTimer")]
        public int RewardTimer { get; set; } = 5;

        [JsonPropertyName("PercentageDroppedOnDeath")]
        public double PercentageDroppedOnDeath { get; set; } = 0;

		
		}

	}

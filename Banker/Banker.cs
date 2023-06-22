using Auxiliary.Configuration;
using Banker.Api;
using Banker.Models;
using CSF.TShock;
using Microsoft.Xna.Framework;
using System.Timers;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using Timer = System.Timers.Timer;

namespace Banker
{
	[ApiVersion(2, 1)]
	public class Banker : TerrariaPlugin
	{
		public static BankerApi api = new();

		private Timer _rewardTimer;
		private readonly TSCommandFramework _fx;

		#region Plugin metadata
		public override string Name => "Banker";

		public override Version Version => new(1, 3);

		public override string Author => "Average";

		public override string Description => "A modern and robust economy plugin, designed to replace SEConomy.";
		#endregion

		public Banker(Main game) : base(game)
		{
			_fx = new(new()
			{
				DefaultLogLevel = CSF.LogLevel.Warning,
			});
		}
		public async override void Initialize()
		{
			Configuration<BankerSettings>.Load("Banker");

			// Reload Event
			GeneralHooks.ReloadEvent += (x) =>
			{
				Configuration<BankerSettings>.Load("Banker");
				x.Player.SendSuccessMessage("Successfully reloaded Banker!");
			};

			TShockAPI.GetDataHandlers.KillMe += PlayerDead;
			ServerApi.Hooks.NetSendData.Register(this, OnNpcStrike);

			// Reward Timer initialization
			if (Configuration<BankerSettings>.Settings.RewardsForPlaying)
			{
				_rewardTimer = new Timer(Configuration<BankerSettings>.Settings.RewardTimer)
				{
					AutoReset = true
				};
				_rewardTimer.Elapsed += async (_, x) => await Rewards(x);
				_rewardTimer.Start();
			}

			await _fx.BuildModulesAsync(typeof(Banker).Assembly);
		}

		// On player death event method
		public async void PlayerDead(object sender, GetDataHandlers.KillMeEventArgs args)
		{
			if (args.Player.IsLoggedIn && Configuration<BankerSettings>.Settings.PercentageDroppedOnDeath > 0)
			{
				BankerSettings settings = Configuration<BankerSettings>.Settings;

				var player = await api.RetrieveOrCreateBankAccount(args.Player.Account.Name);
				var toLose = (float)(player.Currency * settings.PercentageDroppedOnDeath);
				player.Currency -= toLose;

				if (settings.AnnounceMobDrops)
				{
					args.Player.SendMessage($"You lost {toLose} {((toLose == 1) ? settings.CurrencyNameSingular : settings.CurrencyNamePlural)} from dying!", Color.Orange);
					return;
				}
			}
		}

		// On mob death event method
		public async void OnNpcStrike(SendDataEventArgs args)
		{
			BankerSettings settings = Configuration<BankerSettings>.Settings;

			if (args.MsgId != PacketTypes.NpcStrike || !settings.EnableMobDrops)
				return;

			var npc = Main.npc[args.number];

			if (args.ignoreClient == -1 || !(npc.life <= 0) || npc.type == NPCID.TargetDummy || npc.SpawnedFromStatue)
				return;

			var player = TSPlayer.FindByNameOrID(args.ignoreClient.ToString())[0];
			Color color = Color.Gold;

			if (settings.ExcludedMobs.Contains(npc.netID))
				return;

			NpcCustomAmount customAmount = api.npcCustomAmounts.FirstOrDefault(x => x.npcID == npc.netID);

			int totalGiven = customAmount != null ? customAmount.reward : 1;
			color = customAmount?.color ?? color;

			var playerAccount = await api.RetrieveOrCreateBankAccount(player.Account.Name);
			playerAccount.Currency += totalGiven;

			if (settings.AnnounceMobDrops)
				player.SendMessage($"+ {totalGiven} {(totalGiven == 1 ? settings.CurrencyNameSingular : settings.CurrencyNamePlural)} from killing {npc.FullName}", color);
		}

		// timer callback event method
		private static async Task Rewards(ElapsedEventArgs _)
		{
			foreach (TSPlayer plr in TShock.Players)
			{
				if (plr == null || !plr.Active || !plr.IsLoggedIn || plr.Account == null)
					continue;

				var player = await api.RetrieveOrCreateBankAccount(plr.Account.Name);
				player.Currency++;
			}
		}

	}
}

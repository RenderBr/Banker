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

namespace Banker
{
	[ApiVersion(2, 1)]
	public class Banker : TerrariaPlugin
	{
		public static BankerApi api = new();

		private Timer _rewardTimer;
		private readonly TSCommandFramework _fx;

		#region Plugin metadata
		public override string Name
			=> "Banker";

		public override Version Version
			=> new(1, 2);

		public override string Author
			=> "Average";

		public override string Description
			=> "A modern and simple economy plugin, designed to replace SEConomy.";
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

			//Reload Event
			GeneralHooks.ReloadEvent += (x) =>
			{
				Configuration<BankerSettings>.Load("Banker");
				x.Player.SendSuccessMessage("Successfully reloaded Banker!");
			};

			TShockAPI.GetDataHandlers.KillMe += PlayerDead;
			ServerApi.Hooks.NetSendData.Register(this, OnNpcStrike);

			#region Reward Timer initialization
			if (Configuration<BankerSettings>.Settings.RewardsForPlaying)
			{
				_rewardTimer = new(Configuration<BankerSettings>.Settings.RewardTimer)
				{
					AutoReset = true
				};
				_rewardTimer.Elapsed += async (_, x)
					=> await Rewards(x);
				_rewardTimer.Start();
			}


			#endregion

			await _fx.BuildModulesAsync(typeof(Banker).Assembly);
		}

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

		public async void OnNpcStrike(SendDataEventArgs args)
		{
			BankerSettings settings = Configuration<BankerSettings>.Settings;

			if (args.MsgId != PacketTypes.NpcStrike)
				return;

			if (settings.EnableMobDrops == false)
				return;

			var npc = Main.npc[args.number];

			if (args.ignoreClient == -1)
				return;

			var player = TSPlayer.FindByNameOrID(args.ignoreClient.ToString())[0];
			Color color;

			if (!(npc.life <= 0))
				return;

			color = Color.Gold;

			if (npc.type != NPCID.TargetDummy && !npc.SpawnedFromStatue)
			{

				int totalGiven = 1;


				if (settings.ExcludedMobs.Count > 0)
				{
					foreach (var mob in settings.ExcludedMobs)
					{
						if (npc.netID == mob)
							return;
					}
				}

				NpcCustomAmount customAmount = api.npcCustomAmounts.FirstOrDefault(x => x.npcID == npc.netID, null);

				if (customAmount != null)
				{
					totalGiven = customAmount.reward;
					color = customAmount.color;
				}

				var Player = await api.RetrieveOrCreateBankAccount(player.Account.Name);
				Player.Currency += totalGiven;

				if (settings.AnnounceMobDrops == true)
					player.SendMessage($"+ {totalGiven} {((totalGiven == 1) ? settings.CurrencyNameSingular : settings.CurrencyNamePlural)} from killing {npc.FullName}", color);

				return;
			}
		}

		private static async Task Rewards(ElapsedEventArgs _)
		{
			foreach (TSPlayer plr in TShock.Players)
			{
				if (plr is null || !(plr.Active && plr.IsLoggedIn))
					continue;

				if (plr.Account is null)
					continue;

				var player = await api.RetrieveOrCreateBankAccount(plr.Account.Name);
				player.Currency++;
			}
		}
	}
}

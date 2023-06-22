using Auxiliary.Configuration;
using Banker.Models;
using CSF;
using CSF.TShock;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using TShockAPI;

namespace Banker.Modules
{
	[RequirePermission("tbc.user")]
	internal class UserCommands : TSModuleBase<TSCommandContext>
	{
		private readonly BankerSettings _settings = Configuration<BankerSettings>.Settings;

		[Command("balance", "bank", "eco", "bal")]
		[Description("Displays the user's balance.")]
		public async Task<IResult> CheckBalance(string user = "")
		{
			if (string.IsNullOrEmpty(user))
			{
				float balance = await Banker.api.GetCurrency(Context.Player);
				return Respond($"You currently have {Math.Round(balance)} {(balance == 1 ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}", Color.LightGoldenrodYellow);
			}
			else
			{
				float balance = await Banker.api.GetCurrency(user);

				if (balance == -1)
					return Error("Invalid player name! Try using their user account name.");

				return Respond($"{user} currently has {Math.Round(balance)} {(balance == 1 ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}", Color.LightGoldenrodYellow);
			}
		}

		[Command("jointbank", "joint", "jointaccount", "jointacc")]
		[Description("Joint bank account manager. Create a joint account with another player.")]
		public async Task<IResult> JointBank(string sub = "", string args1 = "", string args2 = "")
		{
			switch (sub)
			{
				default:
				case "":
				case "help":
					Info("Joint banking? What is it!");
					Respond("You can now make joint bank accounts with your friends. Each of you can take and add money into it at any time.");
					Respond("Once somebody has been added to it, they cannot be removed unless they voluntarily leave. The commands are:");
					Respond("/joint create <name>");
					Respond("/joint invite <name>");
					Respond("/joint take <amount>");
					Respond("/joint bal");
					Respond("/joint deposit <amount>");
					Respond("/joint leave");
					return Success("Enjoy joint banking!");
				case "balance":
				case "bal":
				case "money":
					{
						var joint = await Banker.api.GetJointAccountOfPlayer(Context.Player);
						if (joint == null)
							return Error("You are not in a joint account!");

						return Respond($"The joint account currently has {Math.Round(joint.Currency)} {(joint.Currency == 1 ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}", Color.LightGoldenrodYellow);
					}
				case "take":
					{
						if (args1 == "")
							return Error("Please enter an amount to take!");

						if (!float.TryParse(args1, out float amount) || amount <= 0)
							return Error("Please enter a valid amount!");

						var joint = await Banker.api.GetJointAccountOfPlayer(Context.Player);
						if (joint == null)
							return Error("You are not in a joint account!");

						var bank = await Banker.api.RetrieveOrCreateBankAccount(Context.Player);

						if (joint.Currency < amount)
							return Error("The joint account doesn't have enough money!");

						joint.Currency -= amount;
						bank.Currency += amount;
						return Success("You took " + amount + " from the joint account!");
					}
				case "deposit":
					{
						if (string.IsNullOrWhiteSpace(args1))
							return Error("Please enter an amount to deposit!");

						if (!float.TryParse(args1, out float amount) || amount <= 0)
							return Error("Please enter a valid amount!");

						var joint = await Banker.api.GetJointAccountOfPlayer(Context.Player);
						if (joint is null)
							return Error("You are not in a joint account!");

						var bank = await Banker.api.RetrieveOrCreateBankAccount(Context.Player);

						if (bank.Currency < amount)
							return Error("You don't have enough money to do that!");

						joint.Currency += amount;
						bank.Currency -= amount;
						return Success("You deposited " + amount + " into the joint account!");
					}
				case "accept":
					{
						if (string.IsNullOrEmpty(Context.Player.GetData<string>("jointinvite")) || Context.Player.GetData<string>("jointinvite") == "N.A")
							return Error("You have not been invited to any joint accounts");

						var invite = Context.Player.GetData<string>("jointinvite");

						var acc = await Banker.api.RetrieveOrCreateBankAccount(Context.Player);

						var success = await Banker.api.AddUserToJointAccount(Context.Player, invite);

						if (success)
						{
							Context.Player.RemoveData("jointinvite");
							return Success("You've joined " + invite);
						}
						else
						{
							return Error("Something went wrong!");
						}
					}
				case "deny":
					if (string.IsNullOrEmpty(Context.Player.GetData<string>("jointinvite")) || Context.Player.GetData<string>("jointinvite") == "N.A")
						return Error("You have not been invited to any joint accounts");

					Context.Player.RemoveData("jointinvite");
					return Success("You rejected the invite!");
				case "make":
				case "create":
					{
						if (string.IsNullOrWhiteSpace(args1))
							return Error("Please enter a name for your joint account!");

						JointAccount acc = await Banker.api.CreateJointAccount(Context.Player, args1);

						if (acc is null)
							return Error("Either you are already in a joint account or there is one by that name!");

						return Success("Congratz! You now have a joint account.");
					}
				case "invite":
					{
						if (string.IsNullOrWhiteSpace(args1))
							return Error("Please enter the name of a player you want to invite!");

						var joint = await Banker.api.GetJointAccountOfPlayer(Context.Player);
						if (joint is null)
							return Error("You are not in a joint account!");

						var acc = TShock.UserAccounts.GetUserAccountByName(args1);
						if (acc is null)
							return Error("The player name you entered was invalid!");

						var success = await Banker.api.InviteUserToJointAccount(acc.Name, joint.Name);

						if (success)
							return Success("You have invited " + acc.Name + " to your joint account!");
						else
							return Error("That player is already a part of a joint account!");
					}
			}
		}

		[Command("baltop", "topbal", "topbalance", "leaderboard")]
		[Description("Shows the top ten users with the highest balance.")]
		public IResult TopBalance()
		{
			var topList = Banker.api.TopBalances(8).ToList();

			Respond($"Top Users by Balance (/baltop)", Color.LightGoldenrodYellow);

			foreach (IBankAccount account in topList)
			{
				Respond($"{topList.IndexOf(account) + 1}. {account.AccountName} - {Math.Round(account.Currency)} {(account.Currency == 1 ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}", Color.LightGreen);
			}
			return ExecuteResult.FromSuccess();
		}

		[Command("pay", "transfer", "etransfer")]
		[RequirePermission("transfer")]
		[Description("Transfers user currency from your account to another.")]
		public async Task<IResult> Transfer(string user = "", float? pay = null)
		{
			// no args
			if (string.IsNullOrWhiteSpace(user))
				return Error("Please enter a username! Ex. /pay Ollie <quantity>");

			if (pay is null)
				return Error($"Please enter a quantity to pay the user! Ex. /pay {user} 1000");

			if (pay <= 0)
				return Error($"Please enter a valid quantity, must be a positive number! (You entered: {pay})");

			if (user == Context.Player.Name)
				return Error($"You cannot pay yourself money!");

			var paidUser = await Banker.api.RetrieveBankAccount(user);
			var payingUser = await Banker.api.RetrieveBankAccount(Context.Player.Account.Name);

			if (paidUser is null)
				return Error("Invalid player name!");

			if (payingUser is null)
				return Error("Something went wrong!");

			if (!(payingUser.Currency >= pay))
				return Error($"You do not have enough money to make this payment! You need: {pay - payingUser.Currency} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}");

			payingUser.Currency -= (float)pay;
			paidUser.Currency += (float)pay;

			if (TSPlayer.FindByNameOrID(user).Count > 0)
			{
				var player = TSPlayer.FindByNameOrID(user).FirstOrDefault();
				player.SendInfoMessage($"{Context.Player.Name} has paid you {pay} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}! Your new balance is: {paidUser.Currency}.");
			}
			return Success($"You have successfully given {user} {pay} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}! Your new balance is: {paidUser.Currency}.");
		}
	}
}

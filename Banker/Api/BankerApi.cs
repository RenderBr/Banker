using Auxiliary;
using Auxiliary.Configuration;
using Banker.Models;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using Terraria.ID;
using TShockAPI;
using static Banker.Models.LinkedBankAccount;

namespace Banker.Api
{
	public class BankerApi
	{
		/// <summary>
		/// Represents a list of custom NPC amounts for currency rewards.
		/// </summary>
		public List<NpcCustomAmount> npcCustomAmounts = new()
		{
			new NpcCustomAmount(NPCID.EyeofCthulhu, 100, Color.Red),
			new NpcCustomAmount(NPCID.EaterofWorldsHead, 100, Color.MediumPurple),
			new NpcCustomAmount(NPCID.BrainofCthulhu, 100, Color.Red),
			new NpcCustomAmount(NPCID.SkeletronHead, 100, Color.White),
			new NpcCustomAmount(NPCID.Skeleton, 3, Color.Gray),
			new NpcCustomAmount(NPCID.Pinky, 1000, Color.Pink),
			new NpcCustomAmount(NPCID.DemonEye, 2, Color.DarkRed),
			new NpcCustomAmount(NPCID.Zombie, 2, Color.DarkGreen),
			new NpcCustomAmount(NPCID.BlueSlime, 1, Color.Blue),
			new NpcCustomAmount(NPCID.GreenSlime, 1, Color.Green),
			new NpcCustomAmount(NPCID.RedSlime, 2, Color.Red)
		};

		/// <summary>
		/// Creates a new joint account.
		/// </summary>
		/// <param name="player">The player requesting the joint account.</param>
		/// <param name="name">The name of the joint account.</param>
		/// <returns>The created joint account if successful, or null if the player is already in a joint account or the joint account name is already taken.</returns>
		public async Task<JointAccount> CreateJointAccount(TSPlayer player, string name)
		{
			JointAccount acc = new();

			if (await IsAlreadyInJointAccount(player) == true)
				return null;

			if (await IModel.GetAsync(GetRequest.Bson<JointAccount>(x => x.Name == name)) is null)
			{
				acc = await IModel.GetAsync(GetRequest.Bson<JointAccount>(x => x.Name == name), x =>
				{
					x.Currency = 0;
					x.Accounts = new List<string>() { player.Account.Name };
					x.Name = name;
				});

			}

			return acc;
		}

		/// <summary>
		/// Gets the balance of a joint account.
		/// </summary>
		/// <param name="jointAccount">The name of the joint account.</param>
		/// <returns>The currency balance of the joint account.</returns>
		public float GetBalanceOfJointAccount(string jointAccount)
			=> RetrieveJointAccount(jointAccount).GetAwaiter().GetResult().GetCurrency();

		/// <summary>
		/// Updates the balance of a joint account.
		/// </summary>
		/// <param name="jointAccount">The name of the joint account.</param>
		/// <param name="newAmount">The new currency amount.</param>
		/// <returns>True if the balance was successfully updated, false otherwise.</returns>
		public async Task<bool> UpdateJointBalance(string jointAccount, int newAmount)
		{
			var acc = await RetrieveJointAccount(jointAccount);

			if (acc == null)
				return false;

			acc.UpdateCurrency(newAmount);
			return true;
		}

		/// <summary>
		/// Retrieves a joint account by its name.
		/// </summary>
		/// <param name="jointAccount">The name of the joint account.</param>
		/// <returns>The retrieved joint account if found, or null otherwise.</returns>
		public async Task<JointAccount> RetrieveJointAccount(string jointAccount)
			=> await IModel.GetAsync(GetRequest.Bson<JointAccount>(x => x.Name == jointAccount));

		/// <summary>
		/// Adds a player to a joint account.
		/// </summary>
		/// <param name="player">The TSplayer object to add.</param>
		/// <param name="jointAccount">The name of the joint account.</param>
		/// <returns>True if the player was successfully added to the joint account, false otherwise.</returns>
		public async Task<bool> AddUserToJointAccount(TSPlayer player, string jointAccount)
			=> await AddUserToJointAccount(player.Account.Name, jointAccount);

		/// <summary>
		/// Adds a player to a joint account.
		/// </summary>
		/// <param name="player">The name of the player to add.</param>
		/// <param name="jointAccount">The name of the joint account.</param>
		/// <returns>True if the player was successfully added to the joint account, false otherwise.</returns>
		public async Task<bool> AddUserToJointAccount(string player, string jointAccount)
		{
			var acc = await RetrieveOrCreateBankAccount(player);
			bool cannotJoin = await IsAlreadyInJointAccount(player);

			if (cannotJoin == true)
				return false;

			acc.JointAccount = jointAccount;
			var joint = await RetrieveJointAccount(jointAccount);

			var temp = joint.Accounts;

			temp.Add(player);

			return true;
		}

		/// <summary>
		/// Invites a player to join a joint account.
		/// </summary>
		/// <param name="player">The name of the player to invite.</param>
		/// <param name="jointAccount">The name of the joint account.</param>
		/// <returns>True if the player was successfully invited, false otherwise.</returns>
		public async Task<bool> InviteUserToJointAccount(string player, string jointAccount)
		{
			JointAccount acc = await GetJointAccountOfPlayer(player);

			if (acc is not null) return false;

			TSPlayer p = TSPlayer.FindByNameOrID(player).First();
			p.SetData("jointinvite", jointAccount);

			p.SendMessage(
				$"You have been invited to join a joint bank account: {jointAccount}\n" +
				"Accept the request with /joint accept, or deny it with /joint deny.",
				Color.Yellow);

			return true;
		}

		/// <summary>
		/// Removes a player from a joint account.
		/// </summary>
		/// <param name="player">The name of the player to remove.</param>
		/// <param name="jointAccount">The name of the joint account.</param>
		/// <returns>True if the player was successfully removed from the joint account, false otherwise.</returns>
		public async Task<bool> RemoveUserFromJointAccount(string player, string jointAccount)
		{
			var acc = await RetrieveOrCreateBankAccount(player);

			if (!await IsAlreadyInJointAccount(player))
				return false;

			var joint = await RetrieveJointAccount(jointAccount);

			joint.Accounts.Remove(player);
			acc.JointAccount = string.Empty;

			return true;
		}

		/// <summary>
		/// Retrieves the joint account associated with a player.
		/// </summary>
		/// <param name="player">The name of the player.</param>
		/// <returns>The joint account associated with the player if found, null otherwise.</returns>
		public async Task<JointAccount> GetJointAccountOfPlayer(string player)
		{
			var p = await RetrieveBankAccount(player);

			if (string.IsNullOrWhiteSpace(p.JointAccount))
				return null;

			return await RetrieveJointAccount(p.JointAccount);
		}

		/// <summary>
		/// Retrieves the joint account associated with a player.
		/// </summary>
		/// <param name="player">The player whose joint account is to be retrieved.</param>
		/// <returns>The joint account associated with the player if found, null otherwise.</returns>
		public async Task<JointAccount> GetJointAccountOfPlayer(TSPlayer player)
			=> await GetJointAccountOfPlayer(player.Account.Name);

		/// <summary>
		/// Checks if a player is already in a joint account.
		/// </summary>
		/// <param name="player">The player to check.</param>
		/// <returns>True if the player is already in a joint account, false otherwise.</returns>
		public async Task<bool> IsAlreadyInJointAccount(TSPlayer player) =>
				await IsAlreadyInJointAccount(player.Account.Name);

		/// <summary>
		/// Checks if a player is already in a joint account.
		/// </summary>
		/// <param name="player">The name of the player to check.</param>
		/// <returns>True if the player is already in a joint account, false otherwise.</returns>
		public async Task<bool> IsAlreadyInJointAccount(string player)
		{
			var acc = await RetrieveBankAccount(player);
			return acc.IsInJointAccount();
		}

		/// <summary>
		/// Gets the currency of a player.
		/// </summary>
		/// <param name="player">The player whose currency is to be retrieved.</param>
		/// <returns>The currency amount of the player.</returns>
		public async Task<float> GetCurrency(TSPlayer Player) => await GetCurrency(Player.Account.Name);

		/// <summary>
		/// Gets the currency of a player.
		/// </summary>
		/// <param name="player">The name of the player whose currency is to be retrieved.</param>
		/// <returns>The currency amount of the player.</returns>
		public async Task<float> GetCurrency(string player)
		{
			var p = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == player), x => x.AccountName = player);
			return p?.Currency ?? -1;
		}


		/// <summary>
		/// Resets the currency of a player to 0.
		/// </summary>
		/// <param name="player">The name of the player.</param>
		public async void ResetCurrency(string Player) => await UpdateCurrency(Player, 0);

		/// <summary>
		/// Updates the currency of a player.
		/// </summary>
		/// <param name="player">The player whose currency is to be updated.</param>
		/// <param name="amount">The new currency amount.</param>
		/// <returns>True if the currency was successfully updated, false otherwise.</returns>
		public async Task<bool> UpdateCurrency(TSPlayer player, float amount)
			=> await UpdateCurrency(player.Account.Name, amount);

		/// <summary>
		/// Updates the currency of a player.
		/// </summary>
		/// <param name="player">The name of the player whose currency is to be updated.</param>
		/// <param name="amount">The new currency amount.</param>
		/// <returns>True if the currency was successfully updated, false otherwise.</returns>
		public async Task<bool> UpdateCurrency(string player, float amount)
		{
			var Player = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == player), x => x.AccountName = player);
			if (Player is null)
				return false;

			Player.Currency = amount;
			return true;
		}

		/// <summary>
		/// Retrieves the top bank account balances.
		/// </summary>
		/// <param name="limit">The maximum number of accounts to retrieve (default is 10).</param>
		/// <returns>An IEnumerable of IBankAccount representing the top account balances.</returns>
		public IEnumerable<IBankAccount> TopBalances(int limit = 10)
		{
			return Configuration<BankerSettings>.Settings.LinkedMode
				? StorageProvider.GetLinkedCollection<LinkedBankAccount>("LinkedBankAccounts")
					.Find(x => true)
					.SortByDescending(x => x.Currency)
					.Limit(limit)
					.ToList()
				: StorageProvider.GetMongoCollection<BankAccount>("BankAccounts")
					.Find(x => true)
					.SortByDescending(x => x.Currency)
					.Limit(limit)
					.ToList();
		}

		/// <summary>
		/// Retrieves the bank account associated with a player.
		/// </summary>
		/// <param name="player">The player whose bank account is to be retrieved.</param>
		/// <returns>The bank account associated with the player if found, null otherwise.</returns>
		public async Task<IBankAccount> RetrieveBankAccount(TSPlayer player) => await RetrieveBankAccount(player.Account.Name);

		/// <summary>
		/// Retrieves or creates the bank account associated with a player.
		/// </summary>
		/// <param name="player">The player whose bank account is to be retrieved or created.</param>
		/// <returns>The bank account associated with the player.</returns>
		public async Task<IBankAccount> RetrieveOrCreateBankAccount(TSPlayer player) => await RetrieveOrCreateBankAccount(player.Account.Name);

		/// <summary>
		/// Retrieves the bank account associated with a player.
		/// </summary>
		/// <param name="name">The name of the player whose bank account is to be retrieved.</param>
		/// <returns>The bank account associated with the player if found, null otherwise.</returns>
		public async Task<IBankAccount> RetrieveBankAccount(string name)
		{
			return Configuration<BankerSettings>.Settings.LinkedMode == true
				? await IModel.GetAsync(GetRequest.Linked<LinkedBankAccount>(x => x.AccountName == name), x => x.AccountName = name)
				: await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == name), x => x.AccountName = name);
		}

		/// <summary>
		/// Retrieves or creates the bank account associated with a player.
		/// </summary>
		/// <param name="name">The name of the player whose bank account is to be retrieved or created.</param>
		/// <returns>The bank account associated with the player.</returns>
		public async Task<IBankAccount> RetrieveOrCreateBankAccount(string name)
		{
			return Configuration<BankerSettings>.Settings.LinkedMode == true
				? await IModel.GetAsync(GetRequest.Linked<LinkedBankAccount>(x => x.AccountName == name), x => x.AccountName = name)
				: await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == name), x => x.AccountName = name);
		}

	}
}

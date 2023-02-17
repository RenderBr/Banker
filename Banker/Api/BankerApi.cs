using Auxiliary;
using Banker.Models;
using IL.Terraria.GameContent.Bestiary;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Terraria.ID;
using TShockAPI;

namespace Banker.Api
{
    public class BankerApi
    {
        public List<NpcCustomAmount> npcCustomAmounts = new List<NpcCustomAmount>()
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

        public async Task<JointAccount> CreateJointAccount(TSPlayer player, string name)
        {
            JointAccount acc = null;

            if(await IsAlreadyInJointAccount(player) == true)
                return acc;

            if(await IModel.GetAsync(GetRequest.Bson<JointAccount>(x => x.Name == name)) == null)
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

        public async Task GetBalanceOfJointAccount(string jointAccount)
            => await RetrieveJointAccount(jointAccount).GetAwaiter().GetResult().GetCurrency();

        public async Task<bool> UpdateJointBalance(string jointAccount, int newAmount)
        {
            var acc = await RetrieveJointAccount(jointAccount);

            if (acc == null)
                return false;

            acc.UpdateCurrency(newAmount);
            return true;
        }

        public async Task<JointAccount> RetrieveJointAccount(string jointAccount)
            => await IModel.GetAsync(GetRequest.Bson<JointAccount>(x => x.Name == jointAccount));

        public async Task<bool> AddUserToJointAccount(TSPlayer player, string jointAccount)
            => await AddUserToJointAccount(player.Account.Name, jointAccount);

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

        public async Task<bool> InviteUserToJointAccount(string player, string jointAccount)
        {
            var acc = await GetJointAccountOfPlayer(player);

            if (acc != null) {
                return false;
            }

            var p = TSPlayer.FindByNameOrID(player).First();
            p.SetData<string>("jointinvite", jointAccount);
            
            p.SendMessage("You have been invited to join a joint bank account: " + jointAccount, Color.Yellow);
            p.SendMessage("Accept the request with /joint accept, or deny it with /joint deny. ", Color.Yellow);
            return true;
        }

        public async Task<bool> RemoveUserFromJointAccount(string player, string jointAccount)
        {
            var acc = await RetrieveOrCreateBankAccount(player);

            if (await IsAlreadyInJointAccount(player) == false)
                return false;

            var joint = await RetrieveJointAccount(jointAccount);

            var temp = joint.Accounts;

            temp.Remove(player);
            acc.JointAccount = string.Empty;

            return true;
        }

        public async Task<JointAccount> GetJointAccountOfPlayer(string player)
        {
            var p = await RetrieveBankAccount(player);

            if (string.IsNullOrEmpty(p.JointAccount))
                return null;
            
            return await RetrieveJointAccount(p.JointAccount);
        }

        public async Task<JointAccount> GetJointAccountOfPlayer(TSPlayer player)
            => await GetJointAccountOfPlayer(player.Account.Name);

        public async Task<bool> IsAlreadyInJointAccount(TSPlayer player) =>
                await IsAlreadyInJointAccount(player.Account.Name);

        public async Task<bool> IsAlreadyInJointAccount(string player)
        {
            var acc = await RetrieveBankAccount(player);

            #if DEBUG
            Console.WriteLine(string.IsNullOrEmpty(acc.JointAccount));
            Console.WriteLine(acc.JointAccount);
            Console.WriteLine(acc.IsInJointAccount());
            #endif

            return acc.IsInJointAccount();
        }

        public async Task<float> GetCurrency(TSPlayer Player)
        {
            return await GetCurrency(Player.Account.Name);
        }

        public async Task<float> GetCurrency(string Player)
        {
            var player = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == Player), x => x.AccountName = Player);
            if (player == null)
                return -1;

            return player.Currency;
        }

        /// <summary>
        /// Resets the currency of a player to 0
        /// </summary>
        /// <param name="Player"></param>
        /// <returns></returns>
        public async void ResetCurrency(string Player)
        {
            await UpdateCurrency(Player, 0);
        }

        public async Task<bool> UpdateCurrency(TSPlayer player, float amount)
            => await UpdateCurrency(player.Account.Name, amount);

        public async Task<bool> UpdateCurrency(string Player, float amount)
        {
            var player = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == Player), x => x.AccountName = Player);
            if (player == null)
                return false;

            player.Currency += amount;
            return true;
        }

        public List<BankAccount> TopBalances(int limit = 10)
        {
            int e = (int)StorageProvider.GetMongoCollection<BankAccount>("BankAccounts").Find(x => true).SortByDescending(x => x.Currency).CountDocuments();
            if (e < limit)
                limit = e;
            
            
            return StorageProvider.GetMongoCollection<BankAccount>("BankAccounts").Find(x => true).SortByDescending(x => x.Currency).Limit(limit).ToList();
        }

        public async Task<BankAccount> RetrieveBankAccount(TSPlayer player)
            => await RetrieveBankAccount(player.Account.Name);

        public async Task<BankAccount> RetrieveOrCreateBankAccount(TSPlayer player)
            => await RetrieveOrCreateBankAccount(player.Account.Name);

        public async Task<BankAccount> RetrieveBankAccount(string name)
            => await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName.ToLower() == name.ToLower()));

        public async Task<BankAccount> RetrieveOrCreateBankAccount(string name) 
            => await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == name), x => x.AccountName = name);

    }
}

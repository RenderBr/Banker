using Auxiliary;
using Auxiliary.Configuration;
using Banker.Models;
using CSF;
using CSF.TShock;
using Microsoft.Xna.Framework;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TShockAPI;

namespace Banker.Modules
{
    [RequirePermission("tbc.user")]
    internal class UserCommands : TSModuleBase<TSCommandContext>
    {
        readonly BankerSettings _settings = Configuration<BankerSettings>.Settings;

        [Command("balance", "bank", "eco", "bal")]
        [Description("Displays the user's balance.")]
        public async Task<IResult> CheckBalance(string user = "")
        {
            // no args
            if (user == "")
            {
                var player = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == Context.Player.Account.Name), x => x.AccountName = Context.Player.Account.Name);

                float balance = player.Currency;

                return Respond($"You currently have {Math.Round(balance)} {(balance == 1 ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}", Color.LightGoldenrodYellow);
            }
            else
            {
                var userBank = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName.ToLower() == user.ToLower()));

                if (userBank == null)
                {
                    return Error("Invalid player name! Try using their user account name.");
                }
                float balance = userBank.Currency;
                return Respond($"{userBank.AccountName} currently has {Math.Round(balance)} {(balance == 1 ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}", Color.LightGoldenrodYellow);

            }
        }

        [Command("baltop", "topbal", "topbalance", "leaderboard")]
        [Description("Shows the top ten users with the highest balance.")]
        public async Task<IResult> TopBalance()
        {
            var player = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == Context.Player.Account.Name), x => x.AccountName = Context.Player.Account.Name);

            int limit = 10;
            int e = (int)StorageProvider.GetMongoCollection<BankAccount>("BankAccounts").Find(x => true).SortByDescending(x => x.Currency).CountDocuments();
            if (e < limit)
            {
                limit = e;
            }
            List<BankAccount> topList = StorageProvider.GetMongoCollection<BankAccount>("BankAccounts").Find(x => true).SortByDescending(x => x.Currency).Limit(limit).ToList();

            Respond($"Top Users by Balance (/baltop)", Color.LightGoldenrodYellow);

            foreach (BankAccount account in topList)
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
            if (user == "")
            {
                return Error("Please enter a username! Ex. /pay Ollie <quantity>");
            }
            if (pay == null)
            {
                return Error($"Please enter a quantity to pay the user! Ex. /pay {user} 1000");
            }
            if (pay <= 0)
            {
                return Error($"Please enter a valid quantity, must be a positive number! (You entered: {pay})");
            }
            if (user == Context.Player.Name)
            {
                return Error($"You cannot pay yourself money!");
            }

            var paidUser = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName.ToLower() == user.ToLower()));
            var payingUser = await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == Context.Player.Name));

            if (paidUser == null)
            {
                return Error("Invalid player name!");
            }
            if (payingUser == null)
            {
                return Error("Something went wrong!");
            }

            if (!(payingUser.Currency >= pay))
            {
                return Error($"You do not have enough money to make this payment! You need: {pay - payingUser.Currency} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}");
            }

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

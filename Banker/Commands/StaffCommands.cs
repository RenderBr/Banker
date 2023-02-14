using Auxiliary;
using Auxiliary.Configuration;
using Banker.Models;
using CSF;
using CSF.TShock;
using System.Linq;
using System.Threading.Tasks;
using TShockAPI;

namespace Banker.Modules
{
    [RequirePermission("tbc.staff")]
    internal class StaffCommands : TSModuleBase<TSCommandContext>
    {
        readonly BankerSettings _settings = Configuration<BankerSettings>.Settings;

        [Command("givebal", "gb")]
        [Description("Gives the user currency forcefully.")]
        public async Task<IResult> CheckBalance(string user = "", float? pay = null)
        {
            // no args
            if (user == "")
                return Error("Please enter a username! Ex. /gb Markeyex <quantity>");

            if (pay == null)
                return Error($"Please enter a quantity to give to the user! Ex. /gb {user} 1000");

            if (pay <= 0)
                return Error($"Please enter a valid quantity, must be a positive number! (You entered: {pay})");

            var paidUser = await Banker.api.RetrieveBankAccount(user);

            if (paidUser == null)
                return Error($"Invalid player name!");

            paidUser.Currency += (float)pay;
            if (TSPlayer.FindByNameOrID(user).Count > 0)
            {
                var player = TSPlayer.FindByNameOrID(user).FirstOrDefault();
                player.SendSuccessMessage($"{Context.Player.Name} has added {pay} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)} to your account! Your new balance is: {paidUser.Currency}!");
            }
            return Success($"You have successfully added {pay} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)} to {user}'s account!");

        }

        [Command("takebal", "tb", "take")]
        [Description("Takes user currency forcefully.")]
        public async Task<IResult> TakeBalance(string user = "", float? pay = null)
        {
            // no args
            if (user == "")
                return Error("Please enter a username! Ex. /tb Razyer <quantity>");

            if (pay == null)
                return Error($"Please enter a quantity to take from the user! Ex. /tb {user} 1000");

            if (pay <= 0)
                return Error($"Please enter a valid quantity, must be a positive number! (You entered: {pay})");

            var paidUser = await Banker.api.RetrieveBankAccount(user);

            if (paidUser == null)
                return Error($"Invalid player name!");

            paidUser.Currency -= (float)pay;
            if (TSPlayer.FindByNameOrID(user).Count > 0)
            {
                var player = TSPlayer.FindByNameOrID(user).FirstOrDefault();
                player.SendInfoMessage($"{Context.Player.Name} has taken away {pay} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)} from your account! Your new balance is: {paidUser.Currency}.");
            }
            return Success($"You have successfully removed {pay} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)} from {user}'s account!");
        }

        [Command("setbal", "sb", "ecoset", "seteco")]
        [Description("Sets user currency forcefully.")]
        public async Task<IResult> SetBalance(string user = "", float? pay = null)
        {
            // no args
            if (user == "")
                return Error("Please enter a username! Ex. /sb Trifle 10000");  

            if (pay == null)
                return Error($"Please enter a quantity to set this user's balance to! Ex. /sb {user} 1000");

            if (pay <= 0)
                return Error($"Please enter a valid quantity, must be a positive number! (You entered: {pay})");

            var paidUser = await Banker.api.RetrieveBankAccount(user);

            if (paidUser == null)
                return Error($"Invalid player name!");

            paidUser.Currency = (float)pay;
            if (TSPlayer.FindByNameOrID(user).Count > 0)
            {
                var player = TSPlayer.FindByNameOrID(user).FirstOrDefault();
                player.SendInfoMessage($"{Context.Player.Name} has set your bank account to: {pay} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}.");
            }
            return Success($"You have successfully set {user}'s bank account to {pay} {((pay == 1) ? _settings.CurrencyNameSingular : _settings.CurrencyNamePlural)}");
        }


        [Command("resetbal", "rbal", "rb")]
        [Description("Resets a user's bank account.")]
        public async Task<IResult> ResetBalance(string user = "")
        {
            // no args
            if (user == "")
                return Error("Please enter a username! Ex. /rb rozen");

            var paidUser = await Banker.api.RetrieveBankAccount(user);

            if (paidUser == null)
                return Error($"Invalid player name!");

            paidUser.Currency = 0;
            if (TSPlayer.FindByNameOrID(user).Count > 0)
            {
                var player = TSPlayer.FindByNameOrID(user).FirstOrDefault();
                player.SendInfoMessage($"{Context.Player.Name} has reset your bank account! Your new balance is: {paidUser.Currency}.");
            }
            return Success($"You have successfully reset {user}'s bank account!");
        }
    }
}

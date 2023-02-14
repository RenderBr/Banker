using Auxiliary;
using Banker.Models;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using TShockAPI;

namespace Banker.Api
{
    public class BankerApi
    {
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

        public List<BankAccount> TopBalances(int limit = 10)
        {
            int e = (int)StorageProvider.GetMongoCollection<BankAccount>("BankAccounts").Find(x => true).SortByDescending(x => x.Currency).CountDocuments();
            if (e < limit)
                limit = e;
            
            
            return StorageProvider.GetMongoCollection<BankAccount>("BankAccounts").Find(x => true).SortByDescending(x => x.Currency).Limit(limit).ToList();
        }

        public async Task<BankAccount> RetrieveBankAccount(string name)
            => await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName.ToLower() == name.ToLower()));

        public async Task<BankAccount> RetrieveOrCreateBankAccount(string name) 
            => await IModel.GetAsync(GetRequest.Bson<BankAccount>(x => x.AccountName == name), x => x.AccountName = name);

    }
}

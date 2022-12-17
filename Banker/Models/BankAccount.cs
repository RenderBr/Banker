using Auxiliary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Banker.Models
{
    public class BankAccount : BsonModel
    {
        private string _accountName = string.Empty;
        public string AccountName { get => _accountName; set { _ = this.SaveAsync(x => x.AccountName, value); _accountName = value; } }

        private float _currency;
        public float Currency { get => _currency; set { _ = this.SaveAsync(x => x.Currency, value); _currency = value; } }
    }
}

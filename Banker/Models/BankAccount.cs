using Auxiliary;

namespace Banker.Models
{
    public class BankAccount : BsonModel
    {
        private string _accountName = string.Empty;
        public string AccountName { get => _accountName; set { _ = this.SaveAsync(x => x.AccountName, value); _accountName = value; } }

        private float _currency;
        public float Currency { get => _currency; set { _ = this.SaveAsync(x => x.Currency, value); _currency = value; } }

        public string _jointAccount = string.Empty;

        public string JointAccount { get => _jointAccount; set { _ = this.SaveAsync(x => x.JointAccount, value); _jointAccount = value; } }

        public bool IsInJointAccount() => !string.IsNullOrEmpty(JointAccount);
    }
}

using Auxiliary;

namespace Banker.Models
{
	public interface IBankAccount
	{
		string AccountName { get; set; }

		float Currency { get; set; }

		string JointAccount { get; set; }

		bool IsInJointAccount() => !string.IsNullOrWhiteSpace(JointAccount);
	}


	public class LinkedBankAccount : LinkedModel, IBankAccount
	{
		private string _accountName = string.Empty;
		public string AccountName { get => _accountName; set { _ = this.SaveAsync(x => x.AccountName, value); _accountName = value; } }

		private float _currency;
		public float Currency { get => _currency; set { _ = this.SaveAsync(x => x.Currency, value); _currency = value; } }

		public string _jointAccount = string.Empty;

		public string JointAccount { get => _jointAccount; set { _ = this.SaveAsync(x => x.JointAccount, value); _jointAccount = value; } }

	}

	public class BankAccount : BsonModel, IBankAccount
	{
		private string _accountName = string.Empty;
		public string AccountName { get => _accountName; set { _ = this.SaveAsync(x => x.AccountName, value); _accountName = value; } }

		private float _currency;
		public float Currency { get => _currency; set { _ = this.SaveAsync(x => x.Currency, value); _currency = value; } }

		public string _jointAccount = string.Empty;

		public string JointAccount { get => _jointAccount; set { _ = this.SaveAsync(x => x.JointAccount, value); _jointAccount = value; } }

	}
}

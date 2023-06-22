using Auxiliary;

namespace Banker.Models
{
	public class JointAccount : BsonModel
	{
		private List<string> _accounts = new();
		public List<string> Accounts { get => _accounts; set { _ = this.SaveAsync(x => x.Accounts, value); _accounts = value; } }

		private float _currency;
		public float Currency { get => _currency; set { _ = this.SaveAsync(x => x.Currency, value); _currency = value; } }

		private string _name;
		public string Name { get => _name; set { _ = this.SaveAsync(x => x.Name, value); _name = value; } }
		public float GetCurrency() => _currency;
		public void UpdateCurrency(int newAmount) => Currency = newAmount;
	}
}
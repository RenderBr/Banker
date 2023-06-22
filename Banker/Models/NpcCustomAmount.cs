using Microsoft.Xna.Framework;

namespace Banker.Models
{
	public class NpcCustomAmount
	{
		public short npcID;
		public int reward;
		public Color color;

		public NpcCustomAmount(short npcID, int reward, Color color)
		{
			this.npcID = npcID;
			this.reward = reward;
			this.color = color;
		}

	}
}

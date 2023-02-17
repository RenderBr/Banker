using Terraria.ID;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Banker.Models
{
    public class NpcCustomAmount
    {
        public short npcID;
        public int reward;
        public Color color;

        public NpcCustomAmount(NPCID npcID, int reward, Color color)
        {
            this.npcID = npcID;
            this.reward = reward;
            this.color = color;
        }

    }
}

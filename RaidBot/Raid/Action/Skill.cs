using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot
{
    public partial class Raid
    {
        public class Skill : Action
        {
            public static Skill[] Skills = { };


            public Skill(string name, string description, string emote) :base(name,description,Type.Skill)
            {
                ForSale = true;
                Emote = emote;
                ActTerm = "use";
            }
        }

    }
}

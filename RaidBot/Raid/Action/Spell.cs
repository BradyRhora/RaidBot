using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot
{
    public partial class Raid 
    {
        public class Spell : Action
        {
            public static Spell Lightning_Bolt = new Spell("lightning_bolt", "⚡", "Fires a jolt of lightning that can zap through multiple enemies.", 9, new ActionDetails(2, Direction.Cardinal, 3, new ActionEffect("Zap", "⚡", 2, true)));
            public static Spell Magic_missile = new Spell("magic_missile", "☄", "Multiple beams of light launch at various enemies.", 8, new ActionDetails(1.5, Direction.Cardinal, 10));
            public static Spell Fire_Bolt = new Spell("fire_bolt", "🔥", "Launches a burning flame at the enemy, possibly setting it aflame.", 7, new ActionDetails(2, Direction.Cardinal, 7, new ActionEffect("Burn", "🔥", 1)));
            public static Spell Tornado = new Spell("tornado", "🌪", "Creates a powerful cyclone of wind that sucks in enemies and deals damage over time.", 8, new ActionDetails(0, Direction.All, 1, new ActionEffect("Tornado", "🌪", 3, lifespan: 3)));
            public static Spell Summon_Familiar = new Spell("summon_familiar", "🐺", "Summons a creature to aid you in battle!", 8, new ActionDetails(0, Direction.All, 1, new ActionEffect("Wolf", "🐺", 0, false, 5, Monster.Wolf)));
            public static Spell Heal = new Spell("heal", "❤️", "Restores an ally's heath.", 7, new ActionDetails(-2, Direction.Cardinal, 5));
            public static Spell Flame_Wall = new Spell("flame_wall", "🔥", "Creates a wall of fire across the room that lasts for several turns.", 9, new ActionDetails(1, Direction.Cardinal, 5, new ActionEffect("Fire", "🔥", 2, continuous: true, lifespan: 4)));
            
            public static Spell[] Spells = { Lightning_Bolt, Magic_missile, Fire_Bolt, Tornado, Summon_Familiar, Heal, Flame_Wall };

            public Spell(string name, string emote, string description, int requiredlvl, ActionDetails actDet = null, bool reqDir = true) : base(name, description, Type.Spell, actDet, requiredlvl, reqDir)
            {
                ForSale = true;
                Emote = "📖";
                EffectEmote = emote;
                Price = 10 * requiredlvl;
                ActTerm = "cast";
            }

            public static Spell GetSpell(string spellName)
            {
                foreach (Spell s in Spells)
                    if (s.Name == spellName) return s;
                return null;
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot
{
    public partial class Raid
    {
        public class Class
        {
            readonly static Class Archer = new Class("Archer", "The sharpshooting master, attacking from a distance, never missing their target.", "🏹", power: 8);
            readonly static Class Cleric = new Class("Cleric", "The magical healer, protecting and buffing their allies.", "💉", magic_power: 6, baseActions: new Action[] { Spell.Heal });
            readonly static Class Mage = new Class("Mage", "The amazing spellcaster, blasting their enemies with elements and more.", "📘", magic_power: 8, power: 4, baseActions: new Action[] { Spell.Fire_Bolt });
            readonly static Class Paladin = new Class("Paladin", "The religious warrior, smiting their enemies and blessing their allies.", "🔨", magic_power: 7, power: 7);
            readonly static Class Rogue = new Class("Rogue", "The stealthy thief, moving quickly and quietly, their enemies won't see them coming.", "🗡", speed: 8, power: 6);
            readonly static Class Warrior = new Class("Warrior", "The mighty fighter, using a variety of tools and weapons to surpass their foes.", "⚔", power: 9, speed: 4);
            readonly static Class[] classes = { Archer, Cleric, Mage, Paladin, Rogue, Warrior };

            public static Class[] Classes() { return classes; }
            public static string StartMessage()
            {
                string msg = ":man_mage: Welcome... To The Dungeon of Efrüg! A mysterious dungeon that shifts its rooms with each entry, full of deadly monsters and fearsome foes!\n" +
                             "First you must choose your class, then you may enter the dungeon and duel various beasts, before taking on... ***The Boss!***\n" +
                             "To start, use the command `>choose [class]`! You may choose between the following classes:\n\n";

                foreach (Class c in classes)
                {
                    msg += $"{c.Emote} The {c.Name}, {c.Description}\n";
                }



                return msg;
            }

            public string Name;
            public string Description;
            public string Emote;
            public int BaseSpeed, BasePower, BaseMagic_Power;
            public Action[] BaseActions;
            public Class(string Name, string Description, string Emote, int speed = 5, int power = 5, int magic_power = 5, Action[] baseActions = null)
            {
                this.Name = Name;
                this.Description = Description;
                this.Emote = Emote;
                BaseSpeed = speed;
                BasePower = power;
                BaseMagic_Power = magic_power;

                if (baseActions != null)
                    BaseActions = baseActions;
                else
                    BaseActions = new Action[0];

                //BaseActions = Spell.Spells; //gives all spells! only for debugging
            }
            public static Class GetClass(string className)
            {
                return classes.Where(x => x.Name.ToLower() == className.ToLower()).FirstOrDefault();
            }
        }

    }
}

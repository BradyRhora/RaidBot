using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Collections.Specialized;

namespace RaidBot
{
    public partial class Raid
    {
        public class Class
        {
            /*readonly static Class Archer = new Class("Archer", "The sharpshooting master, attacking from a distance, never missing their target.", "🏹", power: 8);
            readonly static Class Cleric = new Class("Cleric", "The magical healer, protecting and buffing their allies.", "💉", magic_power: 6, baseActions: new Action[] { Spell.Heal });
            readonly static Class Mage = new Class("Mage", "The amazing spellcaster, blasting their enemies with elements and more.", "📘", magic_power: 8, power: 4, baseActions: new Action[] { Spell.Fire_Bolt });
            readonly static Class Paladin = new Class("Paladin", "The religious warrior, smiting their enemies and blessing their allies.", "🔨", magic_power: 7, power: 7);
            readonly static Class Rogue = new Class("Rogue", "The stealthy thief, moving quickly and quietly, their enemies won't see them coming.", "🗡", speed: 8, power: 6);
            readonly static Class Warrior = new Class("Warrior", "The mighty fighter, using a variety of tools and weapons to surpass their foes.", "⚔", power: 9, speed: 4);
            readonly static Class[] classes = { Archer, Cleric, Mage, Paladin, Rogue, Warrior };*/

            public static Class[] GetClasses()
            {
                List<Class> classes = new List<Class>();
                using (var sql = new SQLiteConnection(Constants.Strings.DB_CONNECTION_STRING))
                {
                    sql.Open();
                    var stm = "SELECT * FROM CLASSES";
                    using (var cmd = new SQLiteCommand(stm, sql))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string name = reader.GetString(0);
                                string desc = reader.GetString(1);
                                string emote = reader.GetString(2);
                                int pow = reader.GetInt32(3);
                                int mag_pow = reader.GetInt32(4);
                                int spd = reader.GetInt32(5);
                                var a = reader.GetValue(6);
                                List<Action> acts = new List<Action>();
                                Action[] actions;
                                if (a.GetType() == typeof(DBNull)) actions = null;
                                else
                                {
                                    var acs = ((string)a).Split(',');
                                    foreach (var act in acs) acts.Add(Action.GetActionByName(act));
                                    actions = acts.ToArray();
                                }
                                classes.Add(new Class(name, desc, emote, spd, pow, mag_pow, actions));
                            }
                        }
                    }
                }
                return classes.ToArray();
            }

            public static string StartMessage()
            {
                string msg = "🧙‍♂️ Welcome... To The Dungeon of Efrüg! A mysterious dungeon that shifts its rooms with each entry, full of deadly monsters and fearsome foes!\n" +
                             "First you must choose your class, then you may enter the dungeon and duel various beasts, before taking on... *The Boss!*\n" +
                             "To start, use the command `>choose [class]`! You may choose between the following classes:\n\n";

                foreach (Class c in GetClasses())
                {
                    msg += $"{c.Emote} The {c.Name}, {c.Description}\n";
                }



                return msg;
            }

            public string Name { get; }
            public string Description { get; }
            public string Emote { get; }
            public int BaseSpeed, BasePower, BaseMagic_Power;
            public Action[] BaseActions { get; }

            
            public Class(string Name, string Description, string Emote, int speed = 5, int power = 5, int magic_power = 5, Action[] baseActions = null)
            {
                this.Name = Name;
                this.Description = Description;
                this.Emote = Emote;
                BaseSpeed = speed;
                BasePower = power;
                BaseMagic_Power = magic_power;

                if (baseActions != null)
                    BaseActions = baseActions.Concat(Action.Actions).ToArray();
                else
                    BaseActions = Action.Actions;


                //BaseActions = Spell.Spells; //gives all spells! only for debugging
            }
            public static Class GetClass(string className)
            {
                using (var sql = new SQLiteConnection(Constants.Strings.DB_CONNECTION_STRING))
                {
                    sql.Open();
                    var stm = "SELECT * FROM CLASSES WHERE NAME LIKE @name";
                    using (var cmd = new SQLiteCommand(stm, sql))
                    {
                        cmd.Parameters.AddWithValue("@name", className);
                        using (var reader = cmd.ExecuteReader())
                        {
                            reader.Read();

                            string name = reader.GetString(0);
                            string desc = reader.GetString(1);
                            string emote = reader.GetString(2);
                            int pow = reader.GetInt32(3);
                            int mag_pow = reader.GetInt32(4);
                            int spd = reader.GetInt32(5);
                            var a = reader.GetValue(6);
                            List<Action> acts = new List<Action>();
                            Action[] actions;
                            if (a.GetType() == typeof(DBNull)) actions = null;
                            else
                            {
                                var acs = ((string)a).Split(',');
                                foreach (var act in acs) acts.Add(Action.GetActionByName(act));
                                actions = acts.ToArray();
                            }
                            return new Class(name, desc, emote, spd, pow, mag_pow,actions);

                        }
                    }
                }
            }
        }

    }
}

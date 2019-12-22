using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot
{
    public partial class Raid
    {
        public class Action : IShoppable
        {
            public static Action Attack = new Action("attack", "Use your fists or currently equipped weapon to attack an enemy in range.", Type.Attack, reqDir: true);
            public static Action Move = new Action("move", "Move somewhere else on the board.", Type.Movement, reqDir: true);
            public static Action Pass = new Action("pass", "End your turn without taking an action.", Type.Pass);
            public static Action Equip = new Action("equip", "Equip an item from your inventory to use as a weapon.", Type.Equip);
            public static Action Info = new Action("info", "Give information on the specified action and how to use it.", Type.Info);
            public static Action[] Actions = new Action[] { Attack, Move, Pass, Equip, Info };
            public string Name { get; set; }
            public string Description { get; set; }
            public Type type;
            public int Required_Level; //the minimum level the user must be
            public ActionDetails Details; //contains information about the action such as damage, direction, etc
            public bool RequiresDirection; //whether or not a direction must be specified when using the action
            public string EffectEmote; //visual emote for on board or descriptions
            public string ActTerm; //the "verb" for using the action for use in descriptions. i.e. cast, or use
            //shop vars
            public int Price { get; set; }
            public bool ForSale { get; set; }
            public string Emote { get; set; }


            public Action(string name, string description, Type type, ActionDetails actDet = null, int reqLvl = 1, bool reqDir = false)
            {
                Name = name;
                Description = description;
                this.type = type;
                if (type == Type.Spell)
                    Types = new ItemType[] { ItemType.Magic };
                else if (type == Type.Skill)
                    Types = new ItemType[] { ItemType.Skill };
                Required_Level = reqLvl;
                Details = actDet;
                RequiresDirection = reqDir;
            }

            public static Action GetActionByName(string name)
            {
                var AllActions = Actions.Concat(Spell.Spells).Concat(Skill.Skills);
                foreach (Action a in AllActions)
                    if (a.Name == name)
                        return a;
                return null;
            }

            public ActionDetails UseAction()
            {
                return Details;
            }
            public override bool Equals(object obj)
            {
                return (obj is Action) && ((Action)(obj)).Name == Name;
            }

            public override int GetHashCode()
            {
                var hashCode = -176021468;
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Description);
                hashCode = hashCode * -1521134295 + type.GetHashCode();
                return hashCode;
            }

            public enum Type
            {
                Attack,
                Movement,
                Spell,
                Pass,
                Equip,
                Skill,
                Info
            }

            public ItemType[] Types { get; set; }
        }

        public class ActionDetails
        {
            public double Power;
            public int Range;
            public Direction PossibleDirections;
            public ActionEffect Effect;

            public ActionDetails(double power = 0, Direction posDir = Direction.All, int range = 1, ActionEffect effect = null)
            {
                Power = power;
                Range = range;

                PossibleDirections = posDir;
                Effect = effect;
            }
            public bool HasEffect() { return Effect != null; }

        }

        public class ActionEffect : Placeable
        {
            public Monster Monster = null;
            public int Lifespan; //in rounds
            public bool Continuous;

            readonly string Name;
            readonly string Emote;
            readonly int Power;

            public ActionEffect(string name, string emote, int power, bool continuous = false, int lifespan = 1, Monster summon = null)
            {
                Name = name;
                Emote = emote;
                Power = power;
                Continuous = continuous;
                Lifespan = lifespan + 1;
                Monster = summon;
                Dead = true;
            }

            public override string GetEmote()
            {
                return Emote;
            }

            public override int GetMoveDistance()
            {
                return Power;
            }

            public override string GetName()
            {
                return Name;
            }

            public override int RollAttackDamage()
            {
                return rdm.Next(Power) + Power / 2;
            }

            public Placeable CreateInstance(int x, int y, Game game = null, Player owner = null)
            {
                Placeable instance;
                if (Monster != null)
                {
                    Monster mon;
                    mon = Monster.Clone(game);
                    int monLevel = -1;
                    if (owner != null) monLevel = owner.GetLevel() - 1;
                    if (monLevel == 0) monLevel = 1;
                    if (monLevel != -1) mon.Level = monLevel;
                    mon.Owner = owner;
                    instance = mon;
                }
                else instance = new ActionEffect(Name, Emote, Power, Continuous, Lifespan, Monster);
                instance.X = x;
                instance.Y = y;

                return instance;
            }
        }

        public interface IShoppable
        {
            bool ForSale { get; set; }
            int Price { get; set; }
            string Description { get; set; }
            string Name { get; set; }

            string Emote { get; set; }


            ItemType[] Types { get; set; }


        }

        public enum ItemType
        {
            Magic,
            Food,
            Weapon,
            General,
            Skill
        }
    }
}

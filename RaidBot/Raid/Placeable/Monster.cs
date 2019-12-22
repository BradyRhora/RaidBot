using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot
{
    public partial class Raid
    {
        public class Monster : Placeable
        {
            #region Monsters
            public static Monster Imp = new Monster("imp", "👿", 2);
            public static Monster Ghost = new Monster("ghost", "👻", 5);
            public static Monster Skeleton = new Monster("skeleton", "💀", 3);
            public static Monster Alien = new Monster("alien", "👽", 6);
            public static Monster Robot = new Monster("robot", "🤖", 5);
            public static Monster Spider = new Monster("Giant Spider", "🕷", 3);
            public static Monster Dragon = new Monster("dragon", "🐉", 10);
            public static Monster Mind_Flayer = new Monster("mind flayer", "🦑", 7);
            public static Monster Snake = new Monster("snake", "🐍", 1);
            public static Monster Giant_Snake = new Monster("giant snake", "🐍", 4);
            public static Monster Bat = new Monster("bat", "🦇", 1);
            public static Monster Strange_Creature = new Monster("strange creature", "🦠", 6);
            public static Monster Wolf = new Monster("wolf", "🐺", 1, evil: false);
            #endregion
            public readonly static Monster[] Monsters = { Imp, Ghost, Skeleton, Alien, Robot, Spider, Dragon, Mind_Flayer, Snake, Bat, Giant_Snake, Strange_Creature };

            readonly string Name;
            readonly string Emote;
            public readonly bool Evil;
            public Player Owner;
            public int Level { get; set; }
            public Monster(string name, string emote, int level, Game game = null, bool evil = true, Player owner = null)
            {
                Name = name;
                Emote = emote;
                Level = level;
                Health = (Level * 10) + rdm.Next(Level);
                Game = game;
                Evil = evil;
                Owner = owner;
            }

            public override string GetEmote()
            {
                return Emote;
            }
            public override string GetName()
            {
                return Name;
            }

            public Monster Clone(Game game = null)
            {
                Monster monster = new Monster(Name, Emote, Level, game, Evil);
                return monster;
            }

            Placeable FindClosestEnemy()
            {
                var room = Game.GetCurrentRoom();
                int closestIndex = 0;
                int closestDist = 100;
                Placeable[] enemies;
                if (Evil) enemies = room.Players.Concat(room.Initiative.Where(x => x.Key.GetType() == typeof(Monster) && !(x.Key as Monster).Evil).Select(x => x.Key)).ToArray();
                else if (Game.GetType() == typeof(Duel)) enemies = room.Players.Where(x => x.ID != Owner.ID).ToArray();
                else enemies = room.Enemies.Where(x => x.Evil).ToArray();

                for (int i = 0; i < enemies.Count(); i++)
                {
                    int dist = GetDistance(enemies[i]);
                    if (dist < closestDist && !enemies[i].Dead)
                    {
                        closestDist = dist;
                        closestIndex = i;
                    }
                }
                return enemies[closestIndex];
            }

            int GetDistance(int x, int y)
            {
                return Math.Abs(X - x) + Math.Abs(Y - y);
            }
            int[] GetClosestSide(int x, int y)
            {
                int closest = GetDistance(x, y);
                int[] closestCoord = { x, y };
                for (int x2 = x - 1; x2 <= x + 1; x2++)
                {
                    for (int y2 = y - 1; y2 <= y + 1; y2++)
                    {
                        if (x == x2 ^ y == y2)
                        {
                            if ((Game.GetCurrentRoom().IsSpaceEmpty(x2, y2, false) || x2 == X && y2 == Y) && x2 >= 0 && y2 >= 0 && x2 < Game.GetCurrentRoom().GetSize() && y2 < Game.GetCurrentRoom().GetSize())
                            {
                                int distance = (GetDistance(x2, y2));
                                if (distance < closest) //make sure its checking right spots and not moving onto player
                                {
                                    closest = distance;
                                    closestCoord = new int[] { x2, y2 };
                                }
                            }
                        }
                    }
                }
                if (closestCoord[0] == x && closestCoord[1] == y) closestCoord = new int[] { -1, -1 };
                return closestCoord;
            }

            public override int GetMoveDistance()
            {
                return (Level / 2) + 1;
            }
            public override int RollAttackDamage()
            {
                return rdm.Next(Level * 5) + Level;
            }


            public string ChooseAction(Room room)
            {

                var target = FindClosestEnemy();
                string msg = "";
                var coords = GetClosestSide(target.X, target.Y);
                if (Dead) msg = "";
                else if (GetDistance(target.X, target.Y) - 1 <= GetMoveDistance() && coords[0] != -1) //if target is within move distance
                {
                    if (!(coords[0] == X && coords[1] == Y))
                    {
                        SetLocation(coords[0], coords[1]);
                        msg += $"{GetEmote()} {GetName()} moves towards {target.GetName()}.";
                    }

                    int attackDMG = RollAttackDamage();
                    msg += $"\n{GetEmote()} {GetName()} attacks {target.GetEmote()} {target.GetName()} for {attackDMG} damage!";
                    var dead = target.TakeDamage(attackDMG);
                    if (dead != null)
                    {
                        if (!Evil && Owner != null)
                        {
                            if (target.GetType() == typeof(Monster)) //temporary. Users should still give xp!
                            {
                                int xp = (target as Monster).GetDeathEXP();
                                msg += $"\n{Owner.GetName()} gained {xp} experience.";
                                Owner.GiveEXP(xp);
                            }

                            /*
                             * if is duel
                             *      get player xp
                             *      give player xp
                            */
                        }
                        msg += "\n" + dead;

                    }
                }
                else
                {
                    var path = GetPathTo(target);
                    if (path.Count() == 0) msg += $"{GetEmote()} {GetName()} stares at the party.";
                    else
                    {
                        var newLocation = path[GetMoveDistance()];
                        SetLocation(newLocation.X, newLocation.Y);
                        msg += $"{GetEmote()} {GetName()} moves towards {target.GetName()}.";
                    }
                }
                return msg;
            }

            public int GetDeathEXP()
            {
                return Level * 5;
            }

        }

    }
}

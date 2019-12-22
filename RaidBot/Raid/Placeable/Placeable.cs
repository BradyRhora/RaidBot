using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;
using System.Drawing;

namespace RaidBot
{
    public partial class Raid
    {
        public abstract class Placeable
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Health { get; set; }
            public int MaxHealth { get; set; }

            public Item Equipped { get; set; }
            public Game Game { get; set; }
            public Action[] Actions;
            public int StepsLeft { get; set; }
            public bool Moved { get; set; } = false;
            public bool Acted { get; set; } = false;
            public bool Dead { get; set; } = false;
            public bool Attackable { get; set; } = true;
            public Placeable()
            {
                //Actions = new Action[] { Action.Pass, Action.Move, Action.Attack, Action.Equip };
            }

            public int GetDistance(Placeable p)
            {
                var path = GetPathTo(p);
                return path.Count();
            }
            public Point[] GetPathTo(Placeable p)
            {
                return new Astar(Game.GetCurrentRoom().BuildAStarBoard(p), new int[] { X, Y }, new int[] { p.X, p.Y }, "DiagonalFree").result.ToArray();
            }

            public void SetLocation(int x, int y)
            {
                X = x;
                Y = y;
            }
           
            public abstract string GetName();
            public abstract string GetEmote();
            public abstract int GetMoveDistance();
            public abstract int RollAttackDamage();
            public string TakeDamage(int attackDamage)
            {
                Health -= attackDamage;
                if (Health <= 0)
                {
                    Health = 0;
                    Dead = true;
                    if (GetType() != typeof(Item))
                        return $"{GetEmote()} {GetName()} falls to the ground, dead. ☠️";
                    else
                        return $"The {GetEmote()} {GetName()} is smashed to pieces.";

                }
                else if (Health > MaxHealth) Health = MaxHealth;
                return null;
            }

            public override int GetHashCode()
            {
                int hash = 13;
                hash = (hash * 7) + X.GetHashCode();
                hash = (hash * 7) + Y.GetHashCode();
                hash = (hash * 7) + GetName().GetHashCode();
                hash = (hash * 7) + GetEmote().GetHashCode();
                return hash;
            }
        }
       
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;
using Discord.Commands;

namespace RaidBot
{
    public partial class Raid
    {
        readonly static Random rdm = new Random();
        public static bool ChannelHasRaid(IMessageChannel channel)
        {
            return Games.Concat(Duels).Where(x => x.GetChannel().Id == channel.Id).Count() > 0;
        }
        public static Game GetChannelRaid(IMessageChannel channel)
        {
            return Games.Concat(Duels).Where(x => x.GetChannel().Id == channel.Id).FirstOrDefault();
        }
        
        public static List<Game> Games = new List<Game>();
        
        public static List<Duel> Duels = new List<Duel>();
        
        public enum Direction
        {
            None,
            Left,
            Right,
            Up,
            Down,
            UpRight,
            DownRight,
            UpLeft,
            DownLeft,
            All,
            Cardinal,
            Diagonal,
        }
        public static bool DirectionEquals(Direction a, Direction b)
        {
            switch (a)
            {
                case Direction.All:
                    return b != Direction.None;
                case Direction.Cardinal:
                    switch (b)
                    {
                        case Direction.Left:
                        case Direction.Right:
                        case Direction.Up:
                        case Direction.Down:
                            return true;
                        default:
                            return false;
                    }
                case Direction.Diagonal:
                    switch (b)
                    {
                        case Direction.UpLeft:
                        case Direction.UpRight:
                        case Direction.DownLeft:
                        case Direction.DownRight:
                            return true;
                        default:
                            return false;
                    }
                case Direction.None:
                    return b == Direction.None;
                case Direction.Up:
                case Direction.Down:
                case Direction.Left:
                case Direction.Right:
                    switch (b)
                    {
                        case Direction.All:
                        case Direction.Cardinal: 
                            return true;
                        default:
                            if (a == b) return true;
                            return false;
                    }
                case Direction.UpLeft:
                case Direction.UpRight:
                case Direction.DownLeft:
                case Direction.DownRight:
                    switch (b)
                    {

                        case Direction.All:
                        case Direction.Diagonal: 
                            return true;
                        default:
                            if (a == b) return true;
                            return false;
                    }
                default:
                    return false;
            }
        }
    }
}

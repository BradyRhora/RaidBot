using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace RaidBot
{
    public partial class Raid
    {
        public class Game
        {
            public Profile Host { get; set; }
            readonly List<Player> Players = new List<Player>();
            public readonly IMessageChannel Channel;
            public bool Started { get; set; }
            public Room[] Dungeon;
            int currentRoom = 0;
            public static int DUNGEON_SIZE = 10;
            public Game(Profile host, IMessageChannel chan)
            {
                Host = host;
                Join(host);
                Channel = chan;
                Started = false;
                Dungeon = new Room[DUNGEON_SIZE];
            }

            public void Join(Profile user)
            {
                Players.Add(new Player(user, this));
            }

            public IMessageChannel GetChannel()
            {
                return Channel;
            }

            public Player GetPlayer(Profile profile)
            {
                foreach (Player p in GetPlayers())
                {
                    if (p.ID == profile.ID) return p;
                }
                return null;
            }
            public Player[] GetPlayers()
            {
                return Players.ToArray();
            }

            public void Kick(Profile user)
            {
                Players.Remove(user as Player);
            }

            public async Task Start(int level = 1)
            {
                Started = true;
                await Channel.SendMessageAsync("You journey into the mysterious dungeon of Efrüg, knowing not what awaits you and your party...");
                Dungeon[0] = new Room(level, this);
                string action = StateCurrentAction();
                await Channel.SendMessageAsync(action);
            }

            public Room GetCurrentRoom()
            {
                return Dungeon[currentRoom];
            }
            public string ShowCurrentRoom(bool describe = true)
            {
                string msg = "";
                var room = GetCurrentRoom();
                msg += room.BuildBoard();
                if (describe) msg += '\n' + room.DescribeRoom();
                return msg;
            }

            public Placeable GetCurrentTurn()
            {
                var room = GetCurrentRoom();
                var turn = room.Initiative.ElementAt(room.Counter).Key;
                return turn;
            }

            public string StateCurrentAction()
            {
                var room = GetCurrentRoom();
                var turn = GetCurrentTurn();

                string msg = "";


                if (turn.Dead && turn.GetType() == typeof(Player))
                {
                    msg = $"☠️ {turn.GetEmote()} {turn.GetName()} lies there, dead. ☠️";
                    room.NextInitiative();
                }
                else if (turn.GetType() == typeof(Player))
                {
                    var player = (Player)turn;
                    if (!turn.Moved)
                    {
                        msg += room.BuildBoard();
                        if (room.FirstAction)
                        {
                            msg += room.DescribeRoom() + "\n";
                            room.FirstAction = false;
                        }
                        msg += $"It's {player.GetEmote()} <@{player.ID}>'s turn.\nChoose one of the following actions with `>r [action] (direction)`.\n" +
                               $"You can move `{player.StepsLeft}` more spaces. `❤️ {player.Health}/{player.MaxHealth}` \n```\n";
                        var actions = player.GetActions();
                        for (int i = 0; i < actions.Count(); i++)
                        {
                            msg += $"{actions[i].Name}";
                            if (actions[i].RequiresDirection) msg += " [direction]\n";
                            else msg += "\n";
                        }
                        msg += "```\nGet more info on an action with `>r info [action]`";
                    }
                    else msg += $"You can move `{player.StepsLeft}` more spaces.";
                }
                else if (turn.GetType() == typeof(Monster))
                {
                    var monster = (Monster)turn;
                    msg = monster.ChooseAction(room);
                    room.NextInitiative();
                }
                else if (turn.GetType() == typeof(ActionEffect))
                {
                    var effect = turn as ActionEffect;
                    effect.Lifespan--;
                    if (effect.Lifespan < 0)
                    {
                        msg = $"The {effect.GetName()} fades out.";
                        room.RemoveFromInitiative(effect);
                        effect.Dead = true;
                    }
                    room.NextInitiative();
                }


                if (GetType() == typeof(Duel))
                {
                    var alive = Players.Where(x => !x.Dead);
                    if (alive.Count() == 1)
                    {
                        msg = $"The duel has ended.\n{alive.First().GetName()} is the winner!";
                        Duels.Remove(this as Duel);
                        return msg;
                    }
                }
                else if (Players.Where(x => x.Dead).Count() == Players.Count())
                {
                    msg += "All players have died. Game over.";
                    Games.Remove(this);
                    return msg;
                }
                else
                {
                    var monsters = room.Initiative.Where(x => x.Key.GetType() == typeof(Monster)).Select(x => x.Key as Monster);
                    var evilMonsters = monsters.Where(x => x.Evil);
                    if (evilMonsters.Where(x => x.Dead).Count() == evilMonsters.Count())
                    {
                        msg = "All enemies have died. Moving on the next room.\n";
                        currentRoom++;
                        Dungeon[currentRoom] = new Room(currentRoom + 1, this);
                        msg += StateCurrentAction();
                        return msg;
                    }
                }

                string add = "";
                if (turn.GetType() == typeof(Monster) || turn.GetType() == typeof(ActionEffect) || turn.Dead) add = StateCurrentAction();
                if (msg != "") add = msg + '\n' + add;
                return add;
            }
        }

        public class Duel : Game
        {
            public Profile Opponent;
            public Duel(Profile host, Profile opponent, IMessageChannel channel) : base(host, channel)
            {
                Opponent = opponent;
                Duels.Add(this);
            }

            public async Task Start()
            {
                Started = true;
                Dungeon[0] = new Room(0, this);
                string action = StateCurrentAction();
                await Channel.SendMessageAsync(action);
            }
        }
    }
}

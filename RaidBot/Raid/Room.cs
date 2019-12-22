using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot
{
    public partial class Raid
    {
        public class Room
        {
            public Monster[] Enemies;
            public int Number;
            public List<Item> Loot = new List<Item>();
            int Size;
            readonly Game Game;
            public Player[] Players { get; private set; }

            public IOrderedEnumerable<KeyValuePair<Placeable, int>> Initiative;
            public int Counter { get; private set; } = 0;
            public bool FirstAction = true;

            public Room(int num, Game game)
            {
                Number = num;
                Game = game;
                Players = Game.GetPlayers();
                Size = rdm.Next(5, 7 + Number / 2);
                if (Size > 16) Size = 16;
                Enemies = new Monster[0];
                if (num > 0) GenerateEnemies();
                PlacePlayers(num > 0);
                RollInitiative();
                if (num > 0) GenerateLoot();
            }

            int[][] CreateBoardArray()
            {
                int[][] board = new int[Size][];
                for (int i = 0; i < Size; i++) board[i] = new int[Size];

                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        board[x][y] = -1;
                    }
                }
                return board;
            }

            void GenerateEnemies()
            {
                int enemyLVL = Convert.ToInt32((Decimal.Divide(Number, Game.DUNGEON_SIZE)) * 10);
                int enemyLimit = Players.Count() * 3;
                int enemyCount = Size / 2;
                if (enemyCount > enemyLimit) enemyCount = enemyLimit;

                var possibleEnemies = Monster.Monsters.Where(x => x.Level <= enemyLVL).ToArray();
                Enemies = new Monster[enemyCount];
                for (int i = 0; i < enemyCount; i++)
                {
                    Enemies[i] = possibleEnemies[rdm.Next(possibleEnemies.Count())].Clone(Game);

                    int posX = -1, posY = -1;
                    do
                    {
                        posY = rdm.Next(Size);
                        posX = rdm.Next(Size / 2, Size);
                    } while (Enemies.Where(e => e != null && e.X == posX && e.Y == posY).Count() > 0);
                    Enemies[i].SetLocation(posX, posY);
                }

            }

            void PlacePlayers(bool team = true)
            {
                Players = Players.Where(x => !x.Dead).ToArray();
                int playerCount = Players.Count();
                int midY = Size / 2 - playerCount / 2;
                if (team)
                {
                    for (int i = 0; i < playerCount; i++)
                        Players[i].SetLocation(0, midY + i);
                }
                else
                {
                    Players[0].SetLocation(0, midY);
                    Players[1].SetLocation(Size - 1, midY + 1);
                }

            }

            void GenerateLoot()
            {
                for (int i = 0; i < Size / 3; i++)
                {
                    int index = rdm.Next(Item.Items.Count());
                    Loot.Add(Item.Items[index].Clone());
                    int x, y;
                    do
                    {
                        x = rdm.Next(Size);
                        y = rdm.Next(Size);
                    }
                    while (!IsSpaceEmpty(x, y, includeItems: true));
                    Loot[i].SetLocation(x, y);
                }
            }

            public void AddToInitative(Placeable p, int initiative)
            {
                var currentInit = Initiative.ElementAt(Counter).Key;
                var init = Initiative.ToList();
                init.Add(new KeyValuePair<Placeable, int>(p, initiative));
                Initiative = init.OrderByDescending(x => x.Value);
                for (int i = 0; i < Initiative.Count(); i++)
                {
                    if (Initiative.ElementAt(i).Key.GetHashCode() == currentInit.GetHashCode())
                    {
                        Counter = i;
                        return;
                    }
                }
            }

            public void RemoveFromInitiative(Placeable p)
            {
                Initiative = Initiative.Where(x => x.Key != p).OrderByDescending(x => x.Value);
            }
            void RollInitiative()
            {
                var rolls = new Dictionary<Placeable, int>();
                foreach (Player p in Players)
                {
                    p.StepsLeft = p.GetMoveDistance();
                    int roll = rdm.Next(10) + 1 + p.Speed;
                    rolls.Add(p, roll);
                }

                foreach (Monster m in Enemies)
                {
                    int roll = rdm.Next(10) + 1 + m.Level;
                    rolls.Add(m, roll);
                }

                Initiative = rolls.OrderByDescending(x => x.Value);
            }
            public void NextInitiative()
            {
                Counter++;

                if (Counter >= Initiative.Count()) Counter = 0;
                var turn = Game.GetCurrentTurn();
                turn.Acted = false;
                turn.Moved = false;
                turn.StepsLeft = turn.GetMoveDistance();
            }

            public string BuildBoard()
            {
                var b2 = CreateBoardArray();
                // remember in array its [Y,X] not [X,Y]

                var placeables = Initiative.Select(x => x.Key).Concat(Loot).ToArray();
                for (int i = 0; i < placeables.Count(); i++)
                {
                    var obj = placeables[i];
                    if ((obj.Dead && IsSpaceEmpty(obj.X, obj.Y, false, includeEffects: false)) || !obj.Dead) b2[obj.Y][obj.X] = i;
                }

                string board = "";
                for (int x = 0; x < Size; x++)
                {
                    for (int y = 0; y < Size; y++)
                    {
                        if (b2[x][y] == -1) board += "⬛";
                        else
                        {
                            int index = b2[x][y];
                            var obj = placeables[index];
                            if (obj.Dead && (obj.GetType() == typeof(Player) || obj.GetType() == typeof(Monster))) //if dead and monster or player
                            {
                                if (obj.GetType() == typeof(Player) || obj.GetType() == typeof(Monster)) board += "☠"; // (skull and crossbones)
                                else board += "⬛";
                            }
                            else board += obj.GetEmote();
                        }
                    }
                    board += "\n";
                }
                return board;
            }
            public int[][] BuildAStarBoard(params Placeable[] exclude)
            {
                var b2 = CreateBoardArray();
                for (int x = 0; x < Size; x++) for (int y = 0; y < Size; y++) b2[x][y] = 0;
                var placeables = Initiative.Select(x => x.Key).Concat(Loot).ToArray();
                for (int i = 0; i < placeables.Count(); i++)
                {
                    var obj = placeables[i];
                    if (!exclude.Contains(obj))
                        if ((obj.Dead && IsSpaceEmpty(obj.X, obj.Y, false, includeEffects: false)) || !obj.Dead)
                            if (obj.GetType() == typeof(Player) || obj.GetType() == typeof(Monster))
                                b2[obj.X][obj.Y] = 1;
                }
                return b2;
            }

            public string DescribeRoom()
            {
                string msg = "";
                if (Size < 7) msg += "You enter a small room. ";
                else if (Size < 10) msg += "You enter a medium sized room. ";
                else msg += "You enter a large room. ";

                msg += "Inside the room is ";

                Dictionary<string, int> mons = new Dictionary<string, int>();
                foreach (Monster mon in Enemies)
                {
                    if (mons.ContainsKey(mon.GetName()))
                    {
                        mons[mon.GetName()]++;
                    }
                    else mons.Add(mon.GetName(), 1);
                }

                char[] vowels = { 'a', 'e', 'i', 'o', 'u' };

                for (int i = 0; i < mons.Count(); i++)
                {
                    bool isVowel = vowels.Contains(Char.ToLower(mons.ElementAt(i).Key[0]));
                    string vowelN = "";
                    if (isVowel) vowelN = "n";
                    if (i > 0 && i == mons.Count() - 1) msg += "and ";
                    if (mons.ElementAt(i).Value > 1) msg += $"a group of {mons.ElementAt(i).Key.ToTitleCase()}s";
                    else msg += $"a{vowelN} {mons.ElementAt(i).Key.ToTitleCase()}";

                    if (mons.Count() >= 3) msg += ",";
                    msg += " ";
                }
                msg = msg.Trim(',', ' ');
                msg += ".";

                return msg;
            }

            public Placeable GetPlaceableAt(int x, int y, bool alive = true, params Type[] types)
            {
                if (types.Contains(typeof(Item))) alive = false;
                Placeable[] placeables = Initiative.Select(o => o.Key).Concat(Loot).ToArray();
                foreach (var p in placeables)
                {
                    if (p.X == x && p.Y == y) //check item coords
                        if (alive && !p.Dead || !alive)
                            if (types.Count() == 0) return p;
                            else if (types.Contains(p.GetType())) return p;
                }
                return null;
            }
            public int[] GetProjectileContact(int x, int y, int dirX, int dirY, int range, bool returnAllSpaces = false, params Type[] types)
            {
                int currentX = x, currentY = y;
                List<int> coords = new List<int>(); // {x1, y1, x2, y2, x3, y3 ... etc}
                for (int i = 0; i < range; i++)
                {
                    currentX += dirX;
                    currentY += dirY;
                    if (currentX < 0 || currentX >= Size || currentY < 0 || currentY >= Size) //if hits wall (dont include the wall!)
                    {
                        currentX -= dirX;
                        currentY -= dirY;
                        break;
                    }

                    if (returnAllSpaces) coords.AddRange(new int[] { currentX, currentY }); //if the function caller wants every space that the projectile makes contact with

                    var p = GetPlaceableAt(currentX, currentY, true, types);
                    if (p != null) //if hits placeable (include the placeable!)
                        break;
                }

                if (returnAllSpaces) return coords.ToArray();
                else return new int[] { currentX, currentY };
            }
            public bool IsSpaceEmpty(int x, int y, bool includeDead = true, bool includeItems = false, bool includeEffects = false)
            {
                var onboard = x >= 0 && y >= 0 && x < Size && y < Size;

                var inits = Initiative.Where(o => o.Key.X == x && o.Key.Y == y);
                if (!includeDead) inits = inits.Where(o => !o.Key.Dead);
                if (!includeEffects) inits = inits.Where(o => o.Key.GetType() != typeof(ActionEffect));
                var init = inits.Count() <= 0;

                bool items = true;
                if (includeItems)
                    items = Loot.Where(o => o != null && o.X == x && o.Y == y).Count() <= 0;


                var empty = onboard && init && items;
                return empty;
            }
            public int GetSize() { return Size; }
        }

    }
}

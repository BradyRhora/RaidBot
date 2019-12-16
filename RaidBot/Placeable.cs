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
                var room = Game.GetCurrentRoom();
                var path = GetPathTo(p);
                return path.Count();
            }
            public Point[] GetPathTo(Placeable p)
            {
                return new Astar(Game.GetCurrentRoom().BuildAStarBoard(), new int[] { X, Y }, new int[] { p.X, p.Y }, "DiagonalFree").result.ToArray();
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
                        return $"☠️ {GetEmote()} {GetName()} falls to the ground, dead. ☠️";
                    else
                        return $"The {GetEmote()} {GetName()} is smashed to pieces.";
                    
                }
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
            public static Monster Wolf = new Monster("wolf", "🐺", 1, evil:false);
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
                if (Evil) enemies = room.Players.Concat(room.Initiative.Where(x=>x.Key.GetType() == typeof(Monster) && !(x.Key as Monster).Evil).Select(x=>x.Key)).ToArray();
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
                            if ((Game.GetCurrentRoom().IsSpaceEmpty(x2, y2,false) || x2 == X && y2==Y) && x2 >= 0 && y2 >= 0 && x2 < Game.GetCurrentRoom().GetSize() && y2 < Game.GetCurrentRoom().GetSize())
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
                if (closestCoord[0] == x && closestCoord[1] == y) closestCoord = new int[] { -1,-1};
                return closestCoord;
            }

            public override int GetMoveDistance()
            {
                return (Level / 2) + 1;
            }
            public override int RollAttackDamage()
            {
                return rdm.Next(Level*5) + Level;
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
                            int xp = (target as Monster).GetDeathEXP();
                            msg += $"\n{Owner.GetName()} gained {xp} experience.";
                            Owner.GiveEXP(xp);
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
        public class Profile : Placeable
        {
            public ulong ID;
            readonly string Name;
            string[] profileData;
            public IUser DiscordUser;
            public int Power { get; }
            public int Speed { get; }
            public int Magic_Power { get; }

            public Item[] Inventory;
            public Profile(IUser user)
            {
                Name = Functions.GetName(user as IGuildUser);
                ID = user.Id;
                DiscordUser = user;
                if (!Directory.Exists("Raid")) Directory.CreateDirectory("Raid");
                if (File.Exists($"Raid/{ID}.raid")) profileData = File.ReadAllLines($"Raid/{ID}.raid");
                else
                {
                    profileData = new string[] { };
                    Save();
                }

                if (GetClass() != null)
                {
                    Power = Convert.ToInt32(GetData("power"));
                    Magic_Power = Convert.ToInt32(GetData("magic_Power"));
                    Speed = Convert.ToInt32(GetData("speed"));
                }

                Inventory = GetItems();
                Actions = GetActions();
            }


            public string GetData(string data)
            {
                foreach (string s in profileData)
                {
                    if (s.ToLower().StartsWith(data.ToLower() + ":")) return s.Split(':')[1];
                }

                string[] newData = { data.ToLower() + ":0" };
                profileData = profileData.Concat(newData).ToArray();
                Save();
                return "0";
            }
            public void SetData(string dataName, string data)
            {
                GetData(dataName);
                for (int i = 0; i < profileData.Count(); i++)
                {
                    if (profileData[i].StartsWith(dataName + ":"))
                    {
                        profileData[i] = $"{dataName}:{data}";
                        break;
                    }
                }

                Save();
            }
            public void SetMultipleData(string[,] data)
            {
                for (int i = 0; i < data.GetLength(0); i++)
                {
                    GetData(data[i, 0]);
                    for (int j = 0; j < profileData.Count(); j++)
                    {
                        if (profileData[j].StartsWith(data[i, 0] + ":"))
                        {
                            profileData[j] = $"{data[i, 0]}:{data[i, 1]}";
                            break;
                        }
                    }
                }
                Save();
            }

            public string[] GetDataA(string data)
            {
                var uData = profileData;
                List<string> results = new List<string>();
                bool adding = false;
                foreach (string d in uData)
                {
                    if (d.StartsWith(data)) adding = true;
                    else if (adding && d.Contains("}")) break;
                    else if (adding) results.Add(d.Replace("\t", ""));
                }

                if (!adding)
                {
                    var list = uData.ToList();
                    list.Add($"{data}{{");
                    list.Add("}");
                    profileData = list.ToArray();
                    Save();
                }

                return results.ToArray();
            }
            public void AddDataA(string dataA, string data) => AddDataA(dataA, new string[] { data });

            public void AddDataA(string dataA, string[] data)
            {
                GetDataA(dataA); //ensure data array exists
                string[] newData = new string[profileData.Count() + data.Count()];
                for (int i = 0; i < profileData.Count(); i++)
                {
                    if (profileData[i].Contains($"{dataA}{{"))
                    {
                        for (int o = 0; o <= i; o++) newData[o] = profileData[o];

                        for(int a = 0; a < data.Count(); a++)
                            newData[i + a + 1] = "\t" + data[a];

                        for (int o = data.Count() + i + 1; o < newData.Count(); o++) newData[o] = profileData[o - data.Count()];
                        break;
                    }

                }
                profileData = newData;
                Save();
            }
            public void RemoveDataA(string dataA, string data)
            {
                var newData = new string[profileData.Count() - 1];
                bool removing = false;
                for (int i = 0; i < profileData.Count(); i++)
                {
                    if (profileData[i].Contains($"{dataA}{{")) removing = true;
                    else if (removing && profileData[i].Contains(data))
                    {
                        for (int o = 0; o < i; o++) newData[o] = profileData[o];
                        for (int o = i + 1; o <= newData.Count(); o++) newData[o - 1] = profileData[o];
                        break;
                    }
                }
                profileData = newData;
                Save();
            }

            public Embed BuildProfileEmbed()
            {
                Class rClass = GetClass();
                JEmbed emb = new JEmbed
                {
                    ColorStripe = Constants.Colours.DEFAULT,
                    Author = new JEmbedAuthor($"{Functions.GetName(DiscordUser as IGuildUser)} the {rClass.Name}")
                };
                emb.Fields.Add(new JEmbedField(x =>
                {
                    x.Header = "Class";
                    x.Text = rClass.Emote + " " + rClass.Name;
                    x.Inline = false;
                }));
                emb.Fields.Add(new JEmbedField(x =>
                {
                    x.Header = "Level";
                    x.Text = GetLevel().ToString();
                    x.Inline = true;
                }));
                emb.Fields.Add(new JEmbedField(x =>
                {
                    x.Header = "EXP";
                    x.Text = GetEXP().ToString() + "/" + EXPToNextLevel();
                    x.Inline = true;
                }));
                emb.Fields.Add(new JEmbedField(x =>
                {
                    x.Header = "Emote";
                    x.Text = GetEmote();
                    x.Inline = true;
                }));

                var items = Inventory;
                Dictionary<Item, int> inv = new Dictionary<Item, int>();
                for (int i = 0; i < items.Count(); i++)
                {
                    if (inv.ContainsKey(items[i])) inv[items[i]]++;
                    else inv.Add(items[i], 1);
                }
                List<string> fields = new List<string>();
                string txt = "";
                foreach (KeyValuePair<Item, int> item in inv)
                {
                    string itemListing = $"{item.Key.Emote} {item.Key.GetTagsString()} {item.Key.Name}";
                    if (item.Value > 1) itemListing += $" x{item.Value} ";
                    if (txt.Count() + itemListing.Count() > 1024)
                    {
                        fields.Add(txt);
                        txt = itemListing;
                    }
                    else txt += itemListing + "\n";
                }
                fields.Add(txt);
                string title = "Inventory";
                foreach (string f in fields)
                {
                    emb.Fields.Add(new JEmbedField(x =>
                    {
                        x.Header = title + ":";
                        x.Text = f;
                    }));
                    title += " (cont.)";
                }

                var spells = Actions.Where(x => x.GetType() == typeof(Spell));
                var skills = Actions.Where(x => x.GetType() == typeof(Skill));

                if (spells.Count() > 0)
                {
                    emb.Fields.Add(new JEmbedField(x =>
                    {
                        x.Header = "Known Spells";
                        string spellTxt = "";
                        foreach(Spell s in spells)
                        {
                            spellTxt += $"{s.EffectEmote} {s.Name}\n";
                        }
                        x.Text = spellTxt;
                        x.Inline = true;
                    }));
                }

                if (skills.Count() > 0)
                {
                    emb.Fields.Add(new JEmbedField(x =>
                    {
                        x.Header = "Known Skills";
                        string skillTxt = "";
                        foreach (Skill s in skills)
                        {
                            skillTxt += $"{s.Emote} {s.Name}\n";
                        }
                        x.Text = skillTxt;
                        x.Inline = true;
                    }));
                }

                return emb.Build();
            }
            private void Save()
            {
                File.WriteAllLines($"Raid/{ID}.raid", profileData);
            }

            public void GiveItem(Item item)
            {
                AddDataA("inventory", item.ItemToString());
                Inventory = GetItems();
            }
            public Item[] GetItems()
            {
                var strItems = GetDataA("inventory");
                List<Item> items = new List<Item>();
                foreach(var i in strItems)
                {
                    Item newItem = Item.StringToItem(i);
                    items.Add(newItem);
                }
                return items.ToArray();
            }

            public void LearnAction(Action action)
            {
                AddDataA("actions", action.Name);
                Actions = GetActions();
            }
            public bool KnowsActions(Action action)
            {
                return Actions.Contains(action);
            }
            public Action[] GetActions()
            {
                var strActions = GetDataA("actions");
                List<Action> actions = new List<Action>();
                foreach (var i in strActions)
                {
                    Action newAction = Action.GetActionByName(i);
                    actions.Add(newAction);
                }
                return Action.Actions.Concat(actions).ToArray();
            }

            public Item GetInventoryItemByName(string name)
            {
                var items = Inventory;
                foreach(Item i in items)
                    if (i.Name.ToLower() == name.ToLower()) return i;
                return null;
            }

            

            public Class GetClass()
            {
                return Class.GetClass(GetData("class"));
            }
            public int GetGold()
            {
                return Convert.ToInt32(GetData("gold"));
            }
            public int GetLevel()
            {
                return Convert.ToInt32(GetData("level"));
            }
            public int GetEXP()
            {
                return Convert.ToInt32(GetData("exp"));
            }
            public void GiveEXP(int amount)
            {
                int current = GetEXP();
                int newAmt = current + amount;

                while (newAmt >= EXPToNextLevel())
                {
                    newAmt -= EXPToNextLevel();
                    LevelUp();
                }
                
                SetData("exp", newAmt.ToString());
            }

            void LevelUp()
            {
                SetData("level", (Convert.ToInt32(GetLevel()) + 1).ToString());
                //give skill points and stuff
            }

            public override string GetName()
            {
                return Name;
            }
            public override string GetEmote()
            {
                return GetData("emote");
            }
            public override int GetMoveDistance()
            {
                return (Speed / 2) + 1;
            }
            public override int RollAttackDamage()
            {
                var dmg = rdm.Next(Power) + (Power / 2); //add weapon damage
                if (Equipped != null) dmg += Equipped.Strength;
                return dmg;
            }

            public int EXPToNextLevel()
            {
                return (int)Math.Pow(5, GetLevel());
            }
        }
        public class Player : Profile
        {
            public Player(IUser user, Game game) : base(user) => Initialize(game);
            public Player(Profile user, Game game) : base(user.DiscordUser) => Initialize(game);

            void Initialize(Game game)
            {
                MaxHealth = Power * 10;
                if (MaxHealth < 1) MaxHealth = 1;
                Health = MaxHealth;
                Game = game;
            }

            string Attack(Placeable target, int damage)
            {
                var resultMSG = target.TakeDamage(damage);
                string xtraMSG = "";
                if (resultMSG != null)
                {
                    xtraMSG = "\n" + resultMSG;
                    if (target.GetType() == typeof(Monster))
                    {
                        int exp = ((Monster)target).GetDeathEXP();
                        GiveEXP(exp);
                        xtraMSG += " You gained " + exp + " experience.";
                    }
                }
                else if (target.Health <= (target.MaxHealth / 2)) xtraMSG = " It looks pretty hurt!";
                return xtraMSG;
            }

            string Move(Direction dir, int steps = 1)
            {
                string itemMsg = "";
                bool moveFailed = false;
                for (int i = 0; i < steps; i++)
                {
                    switch (dir)
                    {
                        case Direction.Left:
                            if (Game.GetCurrentRoom().IsSpaceEmpty(X - 1, Y, includeDead: false))
                                X--;
                            else
                                moveFailed = true;

                            break;
                        case Direction.Right:
                            if (Game.GetCurrentRoom().IsSpaceEmpty(X + 1, Y, includeDead: false))
                                X++;
                            else
                                moveFailed = true;
                            break;
                        case Direction.Down:
                            if (Game.GetCurrentRoom().IsSpaceEmpty(X, Y + 1, includeDead: false))
                                Y++;
                            else
                                moveFailed = true;
                            break;
                        case Direction.Up:
                            if (Game.GetCurrentRoom().IsSpaceEmpty(X, Y - 1, includeDead: false))
                                Y--;
                            else
                                moveFailed = true;
                            break;
                        default:
                            moveFailed = true;
                            break;
                    }

                    if (moveFailed)
                    {
                        return "You are stopped by something in your way.\n" + Game.ShowCurrentRoom(describe: false);
                    }

                    var item = Game.GetCurrentRoom().GetPlaceableAt(X, Y, types: typeof(Item));

                    if (item != null)
                    {
                        GiveItem((Item)item);
                        Game.GetCurrentRoom().Loot.Remove((Item)item);
                        itemMsg = $"You picked up a(n) {item.GetName()} {item.GetEmote()}.\n";
                    }
                    StepsLeft--;
                    Moved = true;
                }
                return itemMsg + Game.ShowCurrentRoom(describe: false);
            }
            public string Act(string[] commands)
            {
                string actionS = commands[0];
                Action action = Action.GetActionByName(actionS);
                Direction dir = Direction.None;
                var currentRoom = Game.GetCurrentRoom();
                if (commands.Count() > 1)
                {
                    string direction = commands[1];
                    switch (direction)
                    {
                        case "l":
                        case "left":
                            dir = Direction.Left;
                            break;
                        case "r":
                        case "right":
                            dir = Direction.Right;
                            break;
                        case "u":
                        case "up":
                            dir = Direction.Up;
                            break;
                        case "d":
                        case "down":
                            dir = Direction.Down;
                            break;
                    }
                }

                if (action == null) return null;
                if (action.Equals(Action.Move))
                {
                    int steps = 1;
                    if (commands.Count() > 2)
                        steps = Convert.ToInt32(commands[2]);

                    if (steps < 0 || steps > StepsLeft) return null;

                    return Move(dir, steps);
                }
                else if (action == Action.Attack)
                {
                    int[] attackCoords = new int[] { X, Y };
                    switch (dir)
                    {
                        case Direction.Left:
                            attackCoords[0]--;
                            break;
                        case Direction.Right:
                            attackCoords[0]++;
                            break;
                        case Direction.Down:
                            attackCoords[1]++;
                            break;
                        case Direction.Up:
                            attackCoords[1]--;
                            break;
                        default:
                            return null;
                    }
                    Placeable target = Game.GetCurrentRoom().GetPlaceableAt(attackCoords[0], attackCoords[1], true, typeof(Monster), typeof(Player));
                    Acted = true;
                    Game.GetCurrentRoom().NextInitiative();
                    if (target == null) return $"{GetEmote()} {GetName()} attacks the air to their {dir.ToString().ToLower()}.";
                    else
                    {
                        var damage = RollAttackDamage();
                        var deathMSG = Attack(target, damage);
                        
                        var weapon = Equipped;
                        string weaponName, weaponEmote;
                        if (weapon == null)
                        {
                            weaponName = "Fists";
                            weaponEmote = "✊";
                        }
                        else
                        {
                            weaponName = weapon.Name;
                            weaponEmote = weapon.Emote;
                        }
                        var equippedDmg = 0;
                        if (Equipped != null) equippedDmg = Equipped.Strength;
                        return $"{GetName()} attacks {target.GetName()} for (*roll: {damage - (Power / 2) - equippedDmg}* + {Power / 2 + equippedDmg}) = **{damage}** damage using their {weaponEmote} {weaponName}." + deathMSG;
                    }
                }
                else if (action == Action.Equip)
                {
                    if (commands.Count() < 2) return "You must specify an item in your inventory to equip.";
                    string itemStr = commands[1];
                    Item item = GetInventoryItemByName(itemStr);
                    if (item != null)
                    {
                        Equipped = item;
                        return $"{GetName()} prepares their {item.GetEmote()} {item.GetName()} for combat.";
                    }
                    else return $"Item not found. Are you sure you have a(n) '{itemStr}'?";
                }
                else if (action == Action.Pass)
                {
                    Acted = true;
                }
                else if (action.GetType() == typeof(Spell) || action.GetType() == typeof(Skill))
                {
                    var details = action.UseAction();
                    
                    if (action.RequiresDirection && DirectionEquals(dir, details.PossibleDirections)) //checks if spell requires direction, and if the inputted direction is correct
                    {

                        int dirX = 0, dirY = 0;
                        switch (dir)
                        {
                            case Direction.Left:
                                dirX = -1;
                                break;
                            case Direction.Right:
                                dirX = 1;
                                break;
                            case Direction.Down:
                                dirY = 1;
                                break;
                            case Direction.Up:
                                dirY = -1;
                                break;
                            case Direction.UpLeft:
                                dirX = -1;
                                dirY = -1;
                                break;
                            case Direction.UpRight:
                                dirX = 1;
                                dirY = -1;
                                break;
                            case Direction.DownLeft:
                                dirX = -1;
                                dirY = 1;
                                break;
                            case Direction.DownRight:
                                dirX = 1;
                                dirY = 1;
                                break;
                        }

                        var contactCoords = Game.GetCurrentRoom().GetProjectileContact(X, Y, dirX, dirY, details.Range, returnAllSpaces: details.Effect.Continuous, typeof(Monster),typeof(Player));
                        var target = Game.GetCurrentRoom().GetPlaceableAt(contactCoords[contactCoords.Length-2], contactCoords[contactCoords.Length - 1]);

                        if (details.Effect.Continuous)
                        {
                            for (int i = 0; i < contactCoords.Count(); i += 2)
                            {
                                int x = contactCoords[i];
                                int y = contactCoords[i + 1];

                                currentRoom.AddToInitative(details.Effect.CreateInstance(x,y), currentRoom.Counter + 1);
                            }
                        }
                        else if (details.Effect.Monster != null)
                        {
                            currentRoom.AddToInitative(details.Effect.CreateInstance(contactCoords[0], contactCoords[1], Game, owner:this), currentRoom.Initiative.ElementAt(currentRoom.Counter).Value + 1);
                        }

                        Acted = true;
                        currentRoom.NextInitiative();
                        if (target == null) return $"{GetEmote()} {GetName()} {action.ActTerm}s {action.Name} to the {dir.ToString().ToLower()}.";
                        else
                        {
                            int roll = rdm.Next((int)(Magic_Power * details.Power));
                            var damage = roll + Magic_Power/2;
                            var deathMSG = Attack(target, damage);

                            return $"{GetName()} uses {action.EffectEmote} {action.Name} on {target.GetName()} for (*roll: {roll}* + {Magic_Power/2}) = **{damage}** damage." + deathMSG;
                        }
                        
                    }
                    else return "direction WRONK";
                }

                if (Acted) currentRoom.NextInitiative();
                return "nomsg";
            }
        }
        public class Item : Placeable, IShoppable
        {
            

            public static Item[] Items =
            {
                new Item("dagger", "🗡", 50, 5, "A short, deadly blade that can be coated in poison.", types: new ItemType[] { ItemType.Weapon }),
                new Item("key", "🔑", -1, 1, "An item found in dungeons. Used to open doors and chests.", purchaseable:false),
                new Item("ring", "💍", 150, 1, "A valuable item that can sold in shops or enchanted.", types: new ItemType[] { ItemType.General, ItemType.Magic }),
                new Item("bow and arrow", "🏹", 50, 2, "A well crafted piece of wood with a string attached, used to launch arrows at enemies to damage them from a distance.", types: new ItemType[] { ItemType.Weapon }),
                new Item("pill", "💊", 25, 0, "A drug with various effects.", purchaseable:false),
                new Item("syringe", "💉", 65, 1, "A needle filled with healing liquids to regain health."),
                new Item("shield", "🛡", 45, 3, "A sturdy piece of metal that can be used to block incoming attacks.", types: new ItemType[] { ItemType.Weapon }),
                new Item("gem", "💎", 200, 0, "A large valuable gem that can be sold at a high price or used as an arcane focus to increase a spells power.", types: new ItemType[] { ItemType.General, ItemType.Magic }),
                new Item("apple", "🍎", 10, 0, "A red fruit that provides minor healing.", types: new ItemType[] { ItemType.Food }),
                new Item("banana", "🍌", 12, 0, "A long yellow fruit that provides minor healing.", types: new ItemType[] { ItemType.Food }),
                new Item("potato", "🥔", 15, 0, "A vegetable that can be cooked in various ways and provides minor healing.", types: new ItemType[] { ItemType.Food }),
                new Item("meat", "🍖", 20, 0, "Meat from some sort of animal that can be cooked and provides more than minor healing.", types: new ItemType[] { ItemType.Food }),
                new Item("cake", "🍰", 25, 0,"A baked good, that's usually eaten during celebrations. Provides minor healing for all party members.", types: new ItemType[] { ItemType.Food }),
                new Item("ale", "🍺", 10, 1, "A cheap drink that provides minor healing, but may have unwanted side effects.", types: new ItemType[] { ItemType.Food }),
                new Item("guitar", "🎸", 50, 3, "A musical instrument, usually with six strings that play different notes."),
                new Item("saxophone", "🎷", 50, 2, "A brass musical instrument."),
                new Item("drum", "🥁", 50, 2, "A musical instrument that usually requires sticks to play beats."),
                new Item("candle", "🕯", 50, 0, "A chunk of wax with a wick in the middle that slowly burns to create minor light."),
                new Item("hammer", "🔨", 60, 7, "A heavy but strong weapon to crush your enemies.", types: new ItemType[] { ItemType.Weapon }),
                new Item("axe", "🪓", 65, 7, "A heavy but strong weapon to crush your enemies.", types: new ItemType[] { ItemType.Weapon }),
                new Item("sword", "⚔️", 70, 80, "A sharp blade to strike down your opponents.", types: new ItemType[] { ItemType.Weapon })
            };



            public static Item GetItem(string name, params string[] tags)
            {
                Item i = Items.Where(x => x.Name == name).First().Clone();
                List<ItemTag> tagList = new List<ItemTag>();
                foreach(var t in tags) tagList.Add(ItemTag.GetTagByName(t));
                
                i.Tags = tagList.ToArray();
                return i;
            }

            public string Name { get; set; }
            public string Description { get; set; }

            public int Strength { get; }
            public string Emote { get; set; }
            public ItemTag[] Tags;
            public ItemType[] Types { get; set; }

            //shop vars
            public int Price { get; set; }
            public bool ForSale { get; set; }

            public override string GetEmote()
            {
                return Emote;
            }
            public override int GetMoveDistance()
            {
                throw new NotImplementedException("Item does not implement the method RollAttackDamage()");
            }
            public override int RollAttackDamage()
            {
                throw new NotImplementedException("Item does not implement the method RollAttackDamage()");
            }
            public override string GetName()
            {
                return Name;
            }

            public string[] GetTagNames()
            {
                List<string> names = new List<string>();
                foreach(ItemTag t in Tags) names.Add(t.Name);
                return names.ToArray();
            }
            public string GetTagsString()
            {
                return string.Join(", ", GetTagNames());
            }

            public string ItemToString()
            {
                return Name + "|" + GetTagsString();
            }
            public static Item StringToItem(string str)
            {
                var data = str.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var name = data[0];
                var tagStrings = new string[0];
                if (data.Count() > 1)
                    tagStrings = data[1].Split(',');
                var item = Item.GetItem(name, tagStrings);
                return item;
            }
            public Item(string name, string emote, int value, int strength, string description, ItemTag[] tags = null, bool purchaseable = true, ItemType[] types = null)
            {
                Name = name;
                Emote = emote;
                Price = value; 
                Description = description;
                if (tags == null)
                    Tags = new ItemTag[0];
                else
                    Tags = tags;

                if (types == null)
                    Types = new ItemType[] { ItemType.General };
                else
                    Types = types;

                Strength = strength;
                ForSale = purchaseable;
                Dead = true;
            }
            
            public Item Clone()
            {
                return new Item(Name, Emote, Price, Strength, Description, Tags);
            }

            public static bool operator ==(Item a, Item b) 
            {
                object objA = a, objB = b;
                if (objA == null && objB == null) return true;
                if (objA == null || objB == null) return false;
                return a.Name == b.Name && a.Tags.OrderBy(x=>x).SequenceEqual(b.Tags.OrderBy(x=>x));
            }
            public static bool operator !=(Item a, Item b)
            {
                object objA = a, objB = b;
                if (objA == null && objB == null) return false;
                if (objA == null || objB == null) return true;
                return a.Name != b.Name || !a.Tags.OrderBy(x => x).SequenceEqual(b.Tags.OrderBy(x => x));
            }

            public override bool Equals(object i)
            {
                if (i == null) return false;
                Item b = i as Item;
                return Name == b.Name && Tags.OrderBy(x => x).SequenceEqual(b.Tags.OrderBy(x => x));
            }
            public override int GetHashCode()
            {
                int hash = 13;
                hash = (hash * 7) + Name.GetHashCode();
                foreach (ItemTag tag in Tags)
                {
                    hash = (hash * 7) + tag.GetHashCode();
                }
                
                return hash;
            }

            public override string ToString()
            {
                return Name;
            }

        }

        public class ItemTag
        {
            public static ItemTag[] Tags = new ItemTag[]
            {
                new ItemTag("golden",5),
                new ItemTag("sharp",5),
                new ItemTag("powerful",5)
            };

            public static ItemTag GetTagByName(string name)
            {
                return Tags.Where(x => x.Name.ToLower() == name.ToLower()).FirstOrDefault();
            }

            public string Name;
            public int Chance;

            public ItemTag(string name, int chance)
            {
                Name = name;
                Chance = chance;
            }

            public ItemTag Clone()
            {
                return new ItemTag(Name, Chance);
            }

            public override bool Equals(Object t)
            {
                return Name == (t as ItemTag).Name;
            }

            public override int GetHashCode()
            {
                return Name.GetHashCode();
            }
        }

        
    }
}

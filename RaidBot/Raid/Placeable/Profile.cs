using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
//using System.IO;

namespace RaidBot
{
    public partial class Raid
    {
        public class Profile : Placeable
        {
            public ulong ID;
            readonly string Name;
            public IUser DiscordUser;
            public string Emote { get; private set; }
            public int Power { get; private set; }
            public int Speed { get; private set; }
            public int Magic_Power { get; private set; }

            public Class Class { get; private set; }
            public int Gold { get; private set; }
            public int Level { get; private set; }
            public int EXP { get; private set; }
            public Item[] Inventory;
            public Profile(IUser user)
            {
                Name = Functions.GetName(user as IGuildUser);
                ID = user.Id;
                DiscordUser = user;
                var dbClass = GetData<string>("Class");
                if (dbClass == null) Class = null;
                else Class = Class.GetClass(dbClass);

                if (Class != null)
                {
                    Power = GetData<int>("Power");
                    Magic_Power = GetData<int>("Magic_Power");
                    Speed = GetData<int>("Speed");
                    Gold = GetData<int>("Gold");
                    EXP = GetData<int>("XP");
                    Emote = GetData<string>("Emote");
                    Level = GetData<int>("Level");
                    Inventory = LoadItems();
                    if (Inventory == null) Inventory = new Item[0];
                    var actionNames = GetData<string>("Known_Actions").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    List<Action> acts = new List<Action>();
                    foreach(var action in actionNames)
                    {
                        acts.Add(Action.GetActionByName(action));
                    }
                    Actions = acts.ToArray();
                }

            }

            public void Create(Class choice)
            {
                Class = choice;
                Level = 1;
                EXP = 0;
                Emote = "👨";
                Gold = 100;
                Power = choice.BasePower;
                Speed = choice.BaseSpeed;
                Magic_Power = choice.BaseMagic_Power;
                Actions = choice.BaseActions;
                CreateDBEntry();
                //Save();
            }

            public T GetData<T>(string data)
            {

                using (var sql = new SQLiteConnection(Constants.Strings.DB_CONNECTION_STRING))
                {
                    sql.Open();
                    var query = $"SELECT {data} FROM PROFILES WHERE ID = {ID}";
                    using (var cmd = new SQLiteCommand(query, sql))
                    {
                        var val = cmd.ExecuteScalar();
                        if (val == null) return default(T);
                        else return (T)Convert.ChangeType(val, typeof(T));
                    }
                }

                /*
                foreach (string s in profileData)
                {
                    if (s.ToLower().StartsWith(data.ToLower() + ":")) return s.Split(':')[1];
                }

                string[] newData = { data.ToLower() + ":0" };
                profileData = profileData.Concat(newData).ToArray();
                Save();
                return "0";*/
            }
            

            public Embed BuildProfileEmbed()
            {
                Class rClass = Class;
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
                    x.Text = Level.ToString();
                    x.Inline = true;
                }));
                emb.Fields.Add(new JEmbedField(x =>
                {
                    x.Header = "EXP";
                    x.Text = EXP.ToString() + "/" + EXPToNextLevel();
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
                        foreach (Spell s in spells)
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
            
            private void CreateDBEntry()
            {

                using (var sql = new SQLiteConnection(Constants.Strings.DB_CONNECTION_STRING))
                {
                    sql.Open();
                    var query = "INSERT INTO PROFILES VALUES(@ID,@class,@power,@speed,@mag_pow,@lvl,@xp,@gold,@emote,@actions)";
                    using (var cmd = new SQLiteCommand(query, sql))
                    {
                        cmd.Parameters.AddWithValue("@ID", ID);
                        cmd.Parameters.AddWithValue("@class", Class.Name);
                        cmd.Parameters.AddWithValue("@power", Power);
                        cmd.Parameters.AddWithValue("@speed", Speed);
                        cmd.Parameters.AddWithValue("@mag_pow", Magic_Power);
                        cmd.Parameters.AddWithValue("@lvl", Level);
                        cmd.Parameters.AddWithValue("@xp", EXP);
                        cmd.Parameters.AddWithValue("@gold", Gold);
                        cmd.Parameters.AddWithValue("@emote", Emote);
                        var actions = string.Join(",", Actions.Select(x => x.Name));
                        cmd.Parameters.AddWithValue("@actions", actions);
                        cmd.ExecuteNonQuery();
                    }
                }

            }
            
            private void Save()
            {

                using (var sql = new SQLiteConnection(Constants.Strings.DB_CONNECTION_STRING))
                {
                    sql.Open();
                    //basic stats/gold
                    var query = $"UPDATE PROFILES SET Class = @class, Power = @power, Speed = @speed,  Magic_Power = @mag_pow, Level = @lvl, XP = @xp, Gold = @gold, Emote = @emote, Known_Actions = @actions WHERE ID = {ID}";
                    using (var cmd = new SQLiteCommand(query, sql))
                    {
                        cmd.Parameters.AddWithValue("@class", Class.Name);
                        cmd.Parameters.AddWithValue("@power",Power);
                        cmd.Parameters.AddWithValue("@speed",Speed);
                        cmd.Parameters.AddWithValue("@mag_pow",Magic_Power);
                        cmd.Parameters.AddWithValue("@lvl", Level);
                        cmd.Parameters.AddWithValue("@xp",EXP);
                        cmd.Parameters.AddWithValue("@gold",Gold);
                        cmd.Parameters.AddWithValue("@emote",Emote);
                        var actions = string.Join(",", Actions.Select(x => x.Name));
                        cmd.Parameters.AddWithValue("@actions", actions);
                        cmd.ExecuteNonQuery();
                    }

                    //items
                    query = $"DELETE FROM INVENTORY WHERE ID = {ID}";
                    using (var cmd = new SQLiteCommand(query, sql)) cmd.ExecuteNonQuery();
                    if (Inventory != null)
                    {
                        Dictionary<Item, int> itemCounts = new Dictionary<Item, int>();
                        foreach (var item in Inventory)
                        {
                            if (itemCounts.ContainsKey(item))
                            {
                                itemCounts[item]++;
                            }
                            else itemCounts.Add(item, 1);
                        }
                        query = $"INSERT INTO INVENTORY VALUES";
                        foreach (var item in itemCounts)
                            query += $"({ID},{item.Key.Name},{item.Key.GetTagsString()},{item.Value}),";
                        query = query.Trim(',');
                        using (var cmd = new SQLiteCommand(query, sql)) cmd.ExecuteNonQuery();
                    }
                }

            }

            private Item[] LoadItems()
            {

                using (var sql = new SQLiteConnection(Constants.Strings.DB_CONNECTION_STRING))
                {
                    sql.Open();
                    var query = "SELECT * FROM INVENTORY WHERE ID = " + ID;
                    using (var cmd = new SQLiteCommand(query, sql))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            List<Item> inv = new List<Item>();
                            while (reader.Read())
                            {
                                int count = reader.GetInt32(3);
                                for (int i = 0; i < count; i++)
                                {
                                    inv.Add(Item.GetItem(reader.GetString(1), reader.GetString(2).Split(',')));
                                }
                            }
                            return inv.ToArray();
                        }
                    }
                }

            }
            public void GiveItem(params Item[] item)
            {
                Inventory.Concat(item);
            }
            
            public void LearnAction(params Action[] action)
            {
                Actions.Concat(action);
            }
            public bool KnowsActions(Action action)
            {
                return Actions.Contains(action);
            }
            public void GiveAction(params Action[] actions)
            {
                Actions.Concat(actions);
            }
            
            public Item GetInventoryItemByName(string name)
            {
                var items = Inventory;
                foreach (Item i in items)
                    if (i.Name.ToLower() == name.ToLower()) return i;
                return null;
            }

            public string GiveEXP(int amount)
            {
                int current = EXP;
                int newAmt = current + amount;
                string lvlUpMsg = "";
                while (newAmt >= EXPToNextLevel())
                {
                    newAmt -= EXPToNextLevel();
                    int newLvl = LevelUp();
                    lvlUpMsg = $"✨Level up! {newLvl - 1} -> {newLvl}✨"; //probably say stat gains or w/e too
                }

                EXP = newAmt;
                return lvlUpMsg;
            }

            int LevelUp()
            {
                int newLevel = Level + 1;
                Level = newLevel;
                return newLevel;
                //give skill points and stuff
            }

            public override string GetName()
            {
                return Name;
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
                return (int)Math.Pow(5, Level);
            }

            public void SetEmote(string emote)
            {
                Emote = emote;
            }
            public override string GetEmote()
            {
                return Emote;
            }
        }
    }
}

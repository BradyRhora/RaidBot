using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.IO;

namespace RaidBot
{
    public partial class Raid
    {
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

                        for (int a = 0; a < data.Count(); a++)
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
                foreach (var i in strItems)
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
                foreach (Item i in items)
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
            public string GiveEXP(int amount)
            {
                int current = GetEXP();
                int newAmt = current + amount;
                string lvlUpMsg = "";
                while (newAmt >= EXPToNextLevel())
                {
                    newAmt -= EXPToNextLevel();
                    int newLvl = LevelUp();
                    lvlUpMsg = $"✨Level up! {newLvl - 1} -> {newLvl}✨"; //probably say stat gains or w/e too
                }

                SetData("exp", newAmt.ToString());
                return lvlUpMsg;
            }

            int LevelUp()
            {
                int newLevel = GetLevel() + 1;
                SetData("level", newLevel.ToString());
                return newLevel;
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
    }
}

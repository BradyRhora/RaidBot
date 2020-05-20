using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace RaidBot
{
    public partial class Raid
    {
        public class Item : Placeable, IShoppable
        {
            public static Item[] GetAllItems()
            {
                using (var sql = new SQLiteConnection(Constants.Strings.DB_CONNECTION_STRING))
                {
                    sql.Open();
                    var query = "SELECT * FROM ITEMS";
                    using (var cmd = new SQLiteCommand(query, sql))
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            List<Item> itms = new List<Item>();
                            while (r.Read())
                            {
                                var name = r.GetString(0);
                                var emote = r.GetString(1);
                                var value = r.GetInt32(2);
                                var str = r.GetInt32(3);
                                var desc = r.GetString(4);
                                
                                var buy = r.GetBoolean(5);
                                var t = r.GetValue(6);
                                ItemType[] types;
                                if (t.GetType() == typeof(DBNull)) types = null;
                                else types = StringToTypes((string)t);
                                itms.Add(new Item(name, emote, value, str, desc, null, buy, types));
                            }
                            return itms.ToArray();
                        }
                    }
                }

            }

            public static Item GetItem(string name, params string[] tags)
            {

                using (var sql = new SQLiteConnection(Constants.Strings.DB_CONNECTION_STRING))
                {
                    sql.Open();
                    var query = "SELECT * FROM ITEMS WHERE NAME = @name";
                    using (var cmd = new SQLiteCommand(query, sql))
                    {
                        cmd.Parameters.AddWithValue("@name", name.ToLower());
                        using (var r = cmd.ExecuteReader())
                        {
                            r.Read();
                            return new Item(r.GetString(0), r.GetString(1), r.GetInt32(2), r.GetInt32(3), r.GetString(4), ItemTag.StringToTags(tags), r.GetBoolean(5), StringToTypes(r.GetString(6)));
                        }
                    }
                }
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
                throw new NotImplementedException("Item does not implement the method GetMoveDamage()");
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
                foreach (ItemTag t in Tags) names.Add(t.Name);
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
                return a.Name == b.Name && a.Tags.OrderBy(x => x).SequenceEqual(b.Tags.OrderBy(x => x));
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

            public static ItemType[] StringToTypes(string str)
            {
                var strTypes = str.Split(',');
                List<ItemType> types = new List<ItemType>();
                foreach (var s in strTypes)
                {
                    types.Add((ItemType)Enum.Parse(typeof(ItemType), s));
                }
                return types.ToArray();
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

            public static ItemTag[] StringToTags(string str)
            {
                var strTags = str.Split(',');
                List<ItemTag> tags = new List<ItemTag>();
                foreach(var s in strTags)
                {
                    tags.Add(GetTagByName(s));
                }
                return tags.ToArray();
            }

            public static ItemTag[] StringToTags(string[] str)
            {
                return StringToTags(string.Join(",", str));
            }
        }
    }
}

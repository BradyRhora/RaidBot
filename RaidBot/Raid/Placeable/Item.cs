using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot
{
    public partial class Raid
    {
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
                new Item("sword", "⚔️", 70, 8, "A sharp blade to strike down your opponents.", types: new ItemType[] { ItemType.Weapon })
            };



            public static Item GetItem(string name, params string[] tags)
            {
                Item i = Items.Where(x => x.Name == name).First().Clone();
                List<ItemTag> tagList = new List<ItemTag>();
                foreach (var t in tags) tagList.Add(ItemTag.GetTagByName(t));

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

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
        public class Shop
        {
            static Shop CurrentShop;
            readonly Dictionary<IShoppable, int> Stock = new Dictionary<IShoppable, int>();
            string Name;
            string Title;
            string Emote;
            ItemType Type;
            string Description;
            DateTime StockTime;

            public Shop()
            {
                StockTime = DateTime.Now;
                SetInfo();
                SetStock();
            }
            void SetStock()
            {
                List<IShoppable> items = new List<IShoppable>();
                foreach (IShoppable i in Item.GetAllItems()) if (i.ForSale) items.Add(i);
                foreach (IShoppable i in Spell.Spells) if (i.ForSale) items.Add(i);
                foreach (IShoppable i in Skill.Skills) if (i.ForSale) items.Add(i);

                List<int> inShop = new List<int>();
                for (int i = 0; i < 5; i++)
                {
                    var index = -1;
                    while (index == -1 || inShop.Contains(index) || !items[index].Types.Contains(Type)) index = rdm.Next(items.Count());
                    Stock.Add(items[index], rdm.Next(5) + 1);
                    inShop.Add(index);
                }
            }
            void SetInfo()
            {
                string[] emotes = { "🧙", "🧙‍♂️", "🧙‍♀️", "👨‍🌾", "👵", "🧝‍♀️", "🧝", "🧝‍♂️" };
                string[] names = { "Brady", "Gartilda", "Garnkle", "Velsha", "Marlo", "Peter", "Vecna", "Fro", "Karmle" };
                string[] titles = { "Shop", "Shoppe", "Store", "Market", "Booth" };

                Emote = emotes[rdm.Next(emotes.Count())];
                Name = names[rdm.Next(names.Count())];
                Title = titles[rdm.Next(titles.Count())];
                Type = (ItemType)(rdm.Next(Enum.GetNames(typeof(ItemType)).Count()));


                string[] descriptions = new string[0];

                if (Type == ItemType.Weapon)
                    descriptions = new string[] { $"Welcome, traveller! Fancy a new sword? Maybe a bow? {Name} here's got it all! ...While supplies last.", $"O.. hullo.. {Name} forge new weapon today... You buy, yes?", $"Ah, welcome! My {Title} has the finest selection of tools for slaying those awful dungeon creatures." };
                else if (Type == ItemType.General)
                    descriptions = new string[] { $"Hey there! Welcome to my {Title}. Take a look around!", "Howdy, fancy some supplies?", "Welcome.. No dilly dallyin'.", "Hiya! Please keep all weapons and spellbooks tucked away.", $"Hey there, the name's {Name}. Welcome to my {Title}!" };
                else if (Type == ItemType.Magic)
                    descriptions = new string[] { "Welcome dearie... Looking for some new spells?", "Ohoho! Welcome adventurer! In the market for some spellbooks?", "Buy somethin' or leave.", $"Welcome! If I, {Name} the wizard, got any spells you don't know yet, I'd be happy to sell you them!", $"Make sure you tell people! {Name}'s {Title} has the best prices!" };
                else if (Type == ItemType.Food)
                    descriptions = new string[] { "Just got a new batch, fresh and ready!", "Sellin' food ain't always easy... But it's honest work.", "Hey there! I got somethin' that'll fill ya right up.", $"Welcome to {Name}'s {Title}, where our stock is as delicious as our.. wait, what's the catchphrase again?" };
                else if (Type == ItemType.Skill)
                    descriptions = new string[] { "Welcome, traveller. I can teach you moves that will aid you in combat.", "Hello. I can show you powerful skills... for some gold, of course.", "Well? What skill should I demonstrate?" };

                Description = descriptions[rdm.Next(descriptions.Count())];
            }
            public IShoppable[] GetStock() { return Stock.Select(x => x.Key).ToArray(); }

            public Embed BuildShopEmbed(Profile user = null)
            {
                var emb = new JEmbed
                {
                    Title = $"{Emote} {Name}'s {Type} {Title}",
                    ColorStripe = new Color(165, 42, 42),
                    Description = $"*\"{Description}\"*"
                };
                foreach (KeyValuePair<IShoppable, int> i in Stock)
                {
                    emb.Fields.Add(new JEmbedField(x =>
                    {
                        x.Header = $"[{i.Key.GetType().Name}] {i.Key.Emote} {i.Key.Name.ToTitleCase()} - {i.Key.Price} gold [{i.Value} left in stock]";
                        x.Text = i.Key.Description;
                    }));
                }
                if (user != null) emb.Footer.Text = $"You currently have: 💰 {user.Gold} gold.";
                return emb.Build();
            }

            public static Shop GetCurrentShop()
            {
                if (CurrentShop == null) CurrentShop = new Shop();
                return CurrentShop;
            }
        }

    }
}

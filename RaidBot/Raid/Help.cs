using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RaidBot
{
    public partial class Raid
    {
        public class Help
        {
            readonly static Help[] helps =
            {
                //the &&& is used to avoid bolding in command examples. It is removed in the actual embed.
                new Help("This action is used to move around the board while in game. The syntax is `;r mo&&&ve [dire&&&ction] (amount)`.\n"+
                         "The [direction] parameter is mandatory and specifies which direction to move in. Valid directions include `up`,`down`,`left`,`right`."+
                         "You may also use just the first letter of the direction, which includes `u`,`d`,`l`,`r`.\n"+
                         "The `(amount)` parameter is optional and used when you want to move one direction multiple times. If something blocks your path before "+
                         "you reach your destination, the movement will be stopped.",
                         "move","walk","run"),
                new Help("This action is used to attack other creatures on the board with your currently equipped weapon. The syntax is `;r att&&&ack [dire&&&ction]`"+
                         "The [direction] parameter is mandatory and specifies which direction to attack in. Valid directions include `up`,`down`,`left`,`right`."+
                         "You may also use just the first letter of the direction, which includes `u`,`d`,`l`,`r`.\n",
                         "attack"),
                new Help("","spell"),//
                new Help("The bad guys in the dungeon. Defeat all of them to move onto the next room of the dungeon. As you progress through the dungeon, the monsters "+
                         "will become increasingly powerful. Monsters occasionally drop loot, and sometimes stronger monsters can drop better loot. Stronger "+
                         "monsters can move more spaces in their turn and deal more damage with their attacks. Sometimes certain monsters will have special "+
                         "abilities as well.",
                         "monster","enemy"),
                new Help("","party","host","join","kick","close","start"),//
                new Help("","item","weapon","armour","equip"),//
                new Help("","direction"),//
                new Help("","dungeon"),//
                new Help("","emote","emoji","icon"),//
                new Help("","profile","player","exp","experience","level","class"),//
                new Help("","shop","store","buy")
            };

            readonly string[] KeyWords;
            readonly string HelpMsg;

            public Help(string msg, params string[] words)
            {
                HelpMsg = msg;
                KeyWords = words;
            }

            public static Help GetHelp(string keyWord)
            {
                foreach (Help h in helps)
                {
                    if (h.KeyWords.Contains(keyWord)) return h;
                }

                foreach (CommandInfo c in Bot.commands.Commands)
                {
                    if (c.Aliases.Contains(keyWord))
                    {
                        return new Help(c.Summary, c.Aliases.ToArray());
                    }
                }
                return null;
            }

            public Embed BuildHelpEmbed()
            {
                List<string> allKeyWords = new List<string>();
                foreach (Help h in helps) foreach (string kw in h.KeyWords) allKeyWords.Add(kw);
                string words = "";
                foreach (string kw in KeyWords.OrderBy(x => x.ToString()))
                {
                    words += $"`{kw}` ";
                }

                JEmbed emb = new JEmbed
                {
                    Title = KeyWords[0].ToTitleCase(),
                    Description = words.Trim()
                };


                string msg = HelpMsg;
                foreach (string kw in allKeyWords)
                {
                    msg = msg.Replace(kw, $"**{kw}**");
                }
                msg = msg.Replace("&&&", "");
                emb.Fields.Add(new JEmbedField(x =>
                {
                    x.Header = "Description";
                    x.Text = msg;
                }));
                emb.ColorStripe = Constants.Colours.DEFAULT;
                emb.Footer.Text = "Bolded words have their own help pages. Use `>help [word]` for more information on them.";
                return emb.Build();
            }

            public static Embed ShowAllHelps()
            {
                IOrderedEnumerable<Help> orderedHelps = helps.OrderBy(x => x.KeyWords[0]);
                JEmbed emb = new JEmbed
                {
                    Title = "Raid Help",
                    Description = "Use `>help [topic]` to get more information on the inputted topic.",
                    ColorStripe = Constants.Colours.DEFAULT
                };
                string topics = "";
                foreach (Help h in orderedHelps)
                {
                    topics += $"`{h.KeyWords[0]}`, ";
                }
                topics = topics.Trim(',', ' ');
                emb.Fields.Add(new JEmbedField(x =>
                {
                    x.Header = "Topics";
                    x.Text = topics;
                }));

                string commands = "";
                var alphabeticalCommands = Bot.commands.Commands.OrderBy(x => x.Name);
                foreach (CommandInfo command in alphabeticalCommands)
                {
                    commands += $"`{command.Name}`, ";
                }
                commands = commands.Trim(',', ' ');
                emb.Fields.Add(new JEmbedField(x =>
                {
                    x.Header = "Commands";
                    x.Text = commands;
                }));

                return emb.Build();
            }
        }


    }
}

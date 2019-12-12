using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace SpotifyBot
{
    public class Commands : ModuleBase
    {
        Random rdm = new Random();
        readonly Exception NotBradyException = new Exception("This command can only be used by Brady.");

        [Command("help"), Summary("Displays commands and descriptions.")]
        public async Task Help()
        {
            JEmbed emb = new JEmbed();
            emb.Author.Name = "Commands";
            emb.ColorStripe = Constants.Colours.SPOTIFY_GREEN;

            foreach (CommandInfo command in Bot.commands.Commands)
            {
                emb.Fields.Add(new JEmbedField(x =>
                {
                    string header = "+" + command.Name;
                    foreach (ParameterInfo parameter in command.Parameters)
                    {
                        header += " [" + parameter.Name + "]";
                    }
                    x.Header = header;
                    x.Text = command.Summary;
                }));
            }
            await Context.Channel.SendMessageAsync("", embed: emb.Build());
        }
    }
}
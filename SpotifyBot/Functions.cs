using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace RaidBot
{
    static class Functions
    {
        public static string GetName(IGuildUser user)
        {
            if (user.Nickname == null)
                return user.Username;
            return user.Nickname;
        }
        public static string ToTitleCase(this string s)
        {
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
        }
        public static async Task<bool> isDM(IMessage message)
        {
            return message.Channel.Name == (await message.Author.GetOrCreateDMChannelAsync()).Name;
        }
    }
}

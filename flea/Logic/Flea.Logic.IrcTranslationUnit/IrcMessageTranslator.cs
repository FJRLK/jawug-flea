using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Flea.Data.DataObjects.WugMs;
using Flea.Data.WebserviceCommunicator.Responses;
using Flea.Logic.CommandUnit;
using Flea.Logic.IrcConnectionUnit;
using Flea.Logic.Utils;

namespace Flea.Logic.IrcTranslationUnit
{
    public static class IrcMessageTranslator
    {
        #region Members

        /// <summary>
        /// Translates the specified member check response.
        /// </summary>
        /// <param name="memberCheckResponse">The member check response.</param>
        /// <returns></returns>
        public static IEnumerable<IrcMessage> Translate(MemberCheckResponse memberCheckResponse)
        {
            List<IrcMessage> response = new List<IrcMessage>
            {
                new IrcMessage($"Total Members: {memberCheckResponse.TotalPaidUpMembers}", true),
                new IrcMessage($"Total Paid Up Members: {memberCheckResponse.TotalPaidUpMembers}", false)
            };

            response.AddRange(memberCheckResponse.PaidUpMembers.Select(mem =>
                mem.last_payment != null
                    ? new IrcMessage($"{mem.member} paid up until {mem.last_payment.Value.AddYears(1)}", false)
                    : new IrcMessage($"{mem.member} paid up until (unspecified date)", false))
                );

            List<WugMsMember> recentlyLapsed =
                memberCheckResponse.LapsedMembers.Where(x => x.last_payment > DateTime.Now.AddMonths(-18)).ToList();
            response.Add(new IrcMessage($"Total Recently Lapsed Members (6 months): {recentlyLapsed.Count}", false));
            response.AddRange(recentlyLapsed.Select(mem =>
                mem.last_payment != null
                    ? new IrcMessage($"{mem.member} lapsed at {mem.last_payment.Value.AddYears(1)}", false)
                    : new IrcMessage($"{mem.member} lapsed at (unspecified date)", false))
                );
            return response;
        }

        /// <summary>
        /// Parses the raw message.
        /// </summary>
        /// <param name="ircCommand">The irc command.</param>
        /// <param name="currentIrcNick">The current irc nick.</param>
        /// <returns></returns>
        public static IrcMessage ParseRawMessage(string ircCommand, string currentIrcNick)
        {
            IrcMessage message = new IrcMessage();
            message.SourceUser = ircCommand.Split(' ')[0].Split('!')[0].Replace(":", "");
            message.SourceChannel = "none";

            if (ircCommand.Split(' ').Length > 2)
                message.SourceChannel = ircCommand.Split(' ')[2];
            message.IsPrivateMessage = false;
            if (message.SourceChannel == currentIrcNick)
            {
                message.SourceChannel = message.SourceUser; // PM the user back, instead of the channel
                message.IsPrivateMessage = true;
            }

            message.SourceType = ircCommand.Split(' ')[1];

            ircCommand = ircCommand.Replace("  ", " ").Trim();


            if (ircCommand.Split(' ').Length <= 3) message.ExclamationCommand = "!NONE";
            else
            {
                message.ExclamationCommand = ircCommand.Split(' ')[3].ToUpperInvariant().Substring(1);

                // support flea tell xxx style commands
                if (string.Equals(message.ExclamationCommand, currentIrcNick,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    List<string> words = ircCommand.Split(' ').ToList();
                    if (words.Count > 4)
                    {
                        words.RemoveAt(3);
                        words[3] = $":!{words[3].ToUpperInvariant()}";
                        message.ExclamationCommand = words[3].ToUpperInvariant().Replace(":", "");
                        ircCommand = string.Join(" ", words);
                    }
                }
            }
            message.RawMessage = ircCommand;
            message.UserPart = ircCommand.RestOfString(3);
            message.Url = GetUrls(message.UserPart);
            if (message.UserPart.Length > 0) message.UserPart = message.UserPart.Substring(1);
            return message;
        }

        /// <summary>
        ///     Gets the urls.
        /// </summary>
        /// <param name="userPart">The user part.</param>
        /// <returns></returns>
        private static string GetUrls(string userPart)
        {
            Regex myRegEx = new Regex("(\\w*://)?(\\w+[\\.?/=])+\\w+", RegexOptions.IgnoreCase);
            MatchCollection matches = myRegEx.Matches(userPart + " ");
            return (from Match match in matches select match.Value).FirstOrDefault();
        }

        public static string Translate(TellMessageStatus tellMessageStatus)
        {
            return
                $"I currently have {tellMessageStatus.MessageCount} messages waiting for {tellMessageStatus.UserCount} users";
        }

        #endregion
    }
}
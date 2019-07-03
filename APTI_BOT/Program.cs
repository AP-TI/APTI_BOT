﻿using Discord;
using Discord.WebSocket;
using Discord.API;
using Discord.Rest;
using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace APTI_BOT
{
    class Program
    {
        private DiscordSocketClient _client;
        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        Config config;

        public async Task MainAsync()
        {
            try
            {
                string result = System.IO.File.ReadAllText(@"config_apti.json");
                config = JsonConvert.DeserializeObject<Config>(result);
                if (config.Jaar1RolId == 0 || config.StudentRolId == 0)
                {
                    Console.WriteLine("Je configuratiebestand is verouderd. Om verder te gaan moet je nog extra gegevens invoeren.");
                    JaarRolInvoer(out ulong jaar1RolId, out ulong jaar2RolId, out ulong jaar3RolId, out ulong studentRolId);
                    config = new Config(config.DiscordToken, config.ServerId, config.PinLogId, jaar1RolId, jaar2RolId, jaar3RolId, studentRolId);
                    string json = JsonConvert.SerializeObject(config);
                    using (StreamWriter sw = File.CreateText(@"config_apti.json"))
                    {
                        sw.WriteLine(json);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                string discordToken;
                ulong serverId, pinLogId;
                Console.Write("Geef bot token: ");
                discordToken = Console.ReadLine();
                Console.Write("Geef server id: ");
                serverId = ulong.Parse(Console.ReadLine());
                Console.Write("Geef pin-log kanaal id: ");
                pinLogId = ulong.Parse(Console.ReadLine());
                JaarRolInvoer(out ulong jaar1RolId, out ulong jaar2RolId, out ulong jaar3RolId, out ulong studentRolId);
                config = new Config(discordToken, serverId, pinLogId, jaar1RolId, jaar2RolId, jaar3RolId, studentRolId);
                string json = JsonConvert.SerializeObject(config);
                using (StreamWriter sw = File.CreateText(@"config_apti.json"))
                {
                    sw.WriteLine(json);
                }
                Console.WriteLine("Configuratie compleet! De bot gaat nu starten.");
            }
            Environment.SetEnvironmentVariable("DiscordToken", config.DiscordToken);
            _client = new DiscordSocketClient();
            _client.MessageReceived += MessageReceived;
            _client.ReactionAdded += ReactionAdded;
            _client.ReactionRemoved += ReactionRemoved;
            _client.Log += Log;

            await _client.LoginAsync(TokenType.Bot,
                Environment.GetEnvironmentVariable("DiscordToken"));
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private static void JaarRolInvoer(out ulong jaar1RolId, out ulong jaar2RolId, out ulong jaar3RolId, out ulong studentRolId)
        {
            Console.Write("Geef id van rol 'Jaar 1': ");
            jaar1RolId = ulong.Parse(Console.ReadLine());
            Console.Write("Geef id van rol 'Jaar 2': ");
            jaar2RolId = ulong.Parse(Console.ReadLine());
            Console.Write("Geef id van rol 'Jaar 3': ");
            jaar3RolId = ulong.Parse(Console.ReadLine());
            Console.Write("Geef id van rol 'Student': ");
            studentRolId = ulong.Parse(Console.ReadLine());
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage message)//Nog deftige command handlers maken
        {
            Emoji[] emoji = new Emoji[3];
            emoji[0] = new Emoji("🥇");
            emoji[1] = new Emoji("🥈");
            emoji[2] = new Emoji("🥉");
            if (message.Content == "!AP")
            {
                await message.Channel.SendMessageAsync("TI!");
            }
            else if (message.Content == "!date")
            {
                await message.Channel.SendMessageAsync(DateTime.Today.ToLongDateString());
            }
            else if (message.Content == "!time")
            {
                await message.Channel.SendMessageAsync(DateTime.Now.ToLongTimeString());
            }
            else if (message.Content == "!datetime")
            {
                await message.Channel.SendMessageAsync($"{DateTime.Now.ToLongDateString()} {DateTime.Now.ToLongTimeString()}");
            }
            else if (message.Source == MessageSource.System || message.Content == "!start")
            {
                if (message.Author.Username == "APTI")
                    await message.DeleteAsync();
                else
                {
                    await message.Author.SendMessageAsync("Hey, welkom in onze server! Ik ben de APTI-bot en mijn doel is om het toetreden tot de server eenvoudiger te maken. We zullen beginnen met je naam op de server in te stellen. Om dit te doen type je je naam en klas in het volgende formaat: {Naam} - {Jaar}TI{Groep} voorafgegeaan door `!naam`.\nBijvoorbeeld: `!naam Maxim - 1TIC`.");
                }
            }
            else if (message.Channel is IPrivateChannel && message.Source == MessageSource.User && message.Content.Contains("!naam") && message.Content.Substring(0, 5) == "!naam")
            {
                string naam = message.Content.Substring(5);
                var guild = _client.GetGuild(config.ServerId);
                var user = guild.GetUser(message.Author.Id);
                Console.Write(message.Content);
                try
                {
                    await user.ModifyAsync(x =>
                    {
                        x.Nickname = naam;
                    });
                    var sent = await message.Author.SendMessageAsync($"Je nickname is ingesteld op {naam}. De volgende stap is je jaar kiezen door te klikken op één (of meerdere) emoji onder dit bericht. Als je vakken moet meenemen, dan kan je ook het vorige jaar kiezen. Als je geen kanalen meer wilt zien van een jaar dan kan je gewoon opnieuw op de emoji ervan klikken. Als je jaar niet verandert dan is de sessie van deze chat verlopen en moet je de sessie terug activeren door `!jaar` te typen.");
                    await sent.AddReactionsAsync(emoji);
                }
                catch (Discord.Net.HttpException e)
                {
                    if (e.HttpCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        var sent_error = await message.Author.SendMessageAsync("Ik heb niet de machtigingen om jouw naam te veranderen, je zal dit zelf moeten doen. Als troost mag je wel kiezen in welk jaar je zit :)");
                        await sent_error.AddReactionsAsync(emoji);
                    }
                    else
                    {
                        var sent_error_unknown = await message.Author.SendMessageAsync("Het instellen van je nickname is niet gelukt. Ik weet zelf niet wat er is fout gegaan. Stuur een berichtje naar @mixxamm met een screenshot van dit bericht.\nFoutcode: " + e.HttpCode + "\n\nJe kan voorlopig al wel je jaar kiezen door te klikken op één (of meerdere) emoji onder dit bericht. Als je vakken moet meenemen, dan kan je ook het vorige jaar kiezen. Als je geen kanalen meer wilt zien van een jaar dan kan je gewoon opnieuw op de emoji ervan klikken.");
                        await sent_error_unknown.AddReactionsAsync(emoji);
                    }
                }
            }
            else if (message.Channel is IPrivateChannel && message.Source == MessageSource.User && message.Content == "!jaar")
            {
                var sent = await message.Author.SendMessageAsync("Kies je jaar door op één of meer van de emoji onder dit bericht te klikken.");
                await sent.AddReactionsAsync(emoji);
            }
            else if (message.Content == "!site")
            {
                await message.Channel.SendMessageAsync("https://apti.ml");
            }
            else if (message.Content == "!github" || message.Content == "!gh")
            {
                await message.Channel.SendMessageAsync("https://apti.ml/github");
            }
            else if (message.Content == "!youtube" || message.Content == "!yt")
            {
                await message.Channel.SendMessageAsync("https://apti.ml/youtube");
            }
            else if (message.Content == "!discord" || message.Content == "!dc")
            {
                await message.Channel.SendMessageAsync("https://apti.ml/discord");
            }
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel is IPrivateChannel && !reaction.User.Value.IsBot)
            {
                var user = _client.GetUser(reaction.UserId);
                Console.WriteLine(user.ToString());
                var guild = _client.GetGuild(config.ServerId);
                if (reaction.Emote.ToString() == "🥇")
                {
                    var role = guild.GetRole(config.Jaar1RolId);
                    Console.WriteLine(role.ToString());
                    await guild.GetUser(reaction.UserId).AddRoleAsync(role);
                }
                else if (reaction.Emote.ToString() == "🥈")
                {
                    var role = guild.GetRole(config.Jaar2RolId);
                    Console.WriteLine(role.ToString());
                    await guild.GetUser(reaction.UserId).AddRoleAsync(role);
                }
                else if (reaction.Emote.ToString() == "🥉")
                {
                    var role = guild.GetRole(config.Jaar3RolId);
                    Console.WriteLine(role.ToString());
                    await guild.GetUser(reaction.UserId).AddRoleAsync(role);
                }
                var studentRole = guild.GetRole(config.StudentRolId);
                await guild.GetUser(reaction.UserId).AddRoleAsync(studentRole);
            }
            else if (reaction.Emote.ToString() == "📌")
            {
                IUserMessage messageToPin = (IUserMessage)await channel.GetMessageAsync(message.Id);
                if (!messageToPin.IsPinned)
                {
                    await messageToPin.PinAsync();
                    var embedBuilder = new EmbedBuilder()
                        .WithTitle("Pinned");
                    try
                    {
                        embedBuilder = embedBuilder.AddField("Bericht", messageToPin.Content, false);
                    }
                    catch (System.ArgumentException)
                    {
                        foreach(IAttachment attachment in messageToPin.Attachments)
                        {
                            if(attachment.IsSpoiler())
                                embedBuilder = embedBuilder.AddField("Afbeelding", $"||{attachment.Url}||", false);
                            else
                                embedBuilder = embedBuilder.WithImageUrl(attachment.Url);
                        }
                    }
                    var embed = embedBuilder.AddField("Kanaal", $"<#{messageToPin.Channel.Id}>", true)
                    .AddField("Door", reaction.User.Value.Mention, true)
                    .WithAuthor(messageToPin.Author.ToString(), messageToPin.Author.GetAvatarUrl(), messageToPin.GetJumpUrl())
                    .Build();
                    await ((ISocketMessageChannel)_client.GetChannel(config.PinLogId)).SendMessageAsync("", false, embed);
                }
            }
        }

        private async Task ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel is IPrivateChannel)
            {
                var user = _client.GetUser(reaction.UserId);
                Console.WriteLine(user.ToString());
                var guild = _client.GetGuild(config.ServerId);
                if (reaction.Emote.ToString() == "🥇")
                {
                    var role = guild.GetRole(config.Jaar1RolId);
                    Console.WriteLine(role.ToString());
                    await guild.GetUser(reaction.UserId).RemoveRoleAsync(role);
                }
                else if (reaction.Emote.ToString() == "🥈")
                {
                    var role = guild.GetRole(config.Jaar2RolId);
                    Console.WriteLine(role.ToString());
                    await guild.GetUser(reaction.UserId).RemoveRoleAsync(role);
                }
                else if (reaction.Emote.ToString() == "🥉")
                {
                    var role = guild.GetRole(config.Jaar3RolId);
                    Console.WriteLine(role.ToString());
                    await guild.GetUser(reaction.UserId).RemoveRoleAsync(role);
                }
            }
            //eventueel unpinnen maken
        }


    }
}

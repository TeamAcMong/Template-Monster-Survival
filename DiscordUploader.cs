using Discord;
using Discord.WebSocket;
using System;
using System.IO;
using System.Threading.Tasks;

public class DiscordUploader 
{
    private DiscordSocketClient _client;
    private string _botToken;
    private ulong _channelId;

    public DiscordUploader(string botToken, ulong channelId)
    {
        _botToken = botToken;
        _channelId = channelId;
        _client = new DiscordSocketClient(new DiscordSocketConfig { 
            LogLevel = LogSeverity.Info,
            DefaultRetryMode = RetryMode.AlwaysRetry
        });
        
        _client.Log += LogAsync;
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    public async Task UploadFileAsync(string filePath, string message)
    {
        try 
        {
            await _client.LoginAsync(TokenType.Bot, _botToken);
            await _client.StartAsync();

            await Task.Delay(5000);  // Wait for connection to establish

            var channel = await _client.GetChannelAsync(_channelId) as ITextChannel;
            if (channel != null)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                string fileSize = (fileInfo.Length / (1024 * 1024)).ToString() + " MB";
                
                // Build a rich message with game details
                var embed = new EmbedBuilder()
                    .WithTitle($"New {Environment.GetEnvironmentVariable("GAME_NAME")} Build Available")
                    .WithDescription($"A new build has been generated with the following details:")
                    .WithColor(Color.Blue)
                    .AddField("Platform", Environment.GetEnvironmentVariable("TARGET_PLATFORM"), true)
                    .AddField("Build Type", Environment.GetEnvironmentVariable("BUILD_TYPE"), true)
                    .AddField("Version", Environment.GetEnvironmentVariable("BUNDLE_VERSION"), true)
                    .AddField("Branch", Environment.GetEnvironmentVariable("SELECTED_GIT_BRANCH"), true)
                    .AddField("Build Number", Environment.GetEnvironmentVariable("BUILD_NUMBER"), true)
                    .AddField("File Size", fileSize, true)
                    .WithFooter(footer => footer.Text = $"Built on {DateTime.Now}")
                    .WithCurrentTimestamp()
                    .Build();

                using (var fileStream = File.OpenRead(filePath))
                {
                    // Send file with an embed
                    await channel.SendFileAsync(fileStream, Path.GetFileName(filePath), text: "Download the latest build directly:", embed: embed);
                    Console.WriteLine($"File uploaded successfully: {filePath}");
                }
            }
            else 
            {
                Console.WriteLine($"Channel with ID {_channelId} not found or is not a text channel.");
            }

            await _client.StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Upload error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    public static async Task RunUpload(string botToken, ulong channelId, string filePath, string message)
    {
        var uploader = new DiscordUploader(botToken, channelId);
        await uploader.UploadFileAsync(filePath, message);
    }
}

public class Program 
{
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: DiscordUploader.exe <filePath> <message>");
            return;
        }

        string botToken = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        string channelIdStr = Environment.GetEnvironmentVariable("DISCORD_CHANNEL_ID");
        
        if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(channelIdStr))
        {
            Console.WriteLine("Error: DISCORD_BOT_TOKEN or DISCORD_CHANNEL_ID environment variables not set.");
            return;
        }

        if (!ulong.TryParse(channelIdStr, out ulong channelId))
        {
            Console.WriteLine($"Error: Invalid channel ID format: {channelIdStr}");
            return;
        }

        string filePath = args[0];
        string message = args[1];

        Console.WriteLine($"Uploading file: {filePath}");
        Console.WriteLine($"Channel ID: {channelId}");
        
        await DiscordUploader.RunUpload(botToken, channelId, filePath, message);
    }
}
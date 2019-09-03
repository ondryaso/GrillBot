# GrillBot

## Requirements
- MSSQL server.
- Microsoft Visual Studio (2017 and later) (or another IDE)
- .NET Core 2.2 (with ASP.NET Core 2.2)

## Continuous integration
[![Build Status](https://dev.azure.com/mhalabica/GrillBot/_apis/build/status/GrillBot-CI?branchName=master)](https://dev.azure.com/mhalabica/GrillBot/_build/latest?definitionId=4&branchName=master)

## Used NuGet packages

### GrillBot
- Discord .NET (v2.1.1)
- Microsoft.AspNetCore.App (v2.2.0)
- Microsoft.AspNetCore.Razor.Design (v2.2.0)
- Microsoft.EntityFrameworkCore.SqlServer (v2.2.6)
- Microsoft.VisualStudio.CodeGeneration.Design

### GrillBotMath
- Newtonsoft.JSON
- MathParser.org-mXParser

## GrillBot config
- Format: **JSON**
- Config parts
  - AllowedHosts (string): Semicollon delimited list of allowed hostnames without port numbers.
  - CommandPrefix (string): Message content, that must contain to invoke command.
  - IsDevelopment (bool): Runtime mode **(Deprecated)**.
  - Leaderboards: Channelboard config
    - ChannelStatsTakeTop (int): Count of top channels **(Deprecated)**
    - SyncWithDbStats (int): Time in miliseconds to sync data in memory with database. **(Deprecated)**
  - Database (string): Connection string to database.
  - Discord: Bot configuration
    - Activity (string): Activity message
    - Token (string): Bot login token
    - UserJoinedMessage (string): Message, that will be sent, when user joined to guild.
    - Administrators (string[]): List of bot administrators. Can use bot independently of roles.
    - LoggerRoomID (string): ID of channel to send logging data.
  - Log: Bot logging configuration **(Deprecated)**
  - MethodsConfig: Bot features configuration.
    - *In common*:
      - RequireRoles (string[]): List of required roles. User must have at least one of these roles.
      - IsDisabled (bool): Deactivated command.
    - Greeting (grillhi command):
      - Message (string): Bots response.
      - AppendEmoji (string): Local server emoji, that will be appended to Message.
      - OutputMode (string): Default output mode. Supported is 'bin', 'text', 'hexa'
    - GrillStatus: Grillbot diagnostics.
    - Help
    - Channelboard: Channelboard commands configuration.
      - Web: REST API and client configuration.
        - TokenValidMins (int): Time in minutes, to remove token from memory.
        - TokenLength (int): Length of token string. **(Deprecated)**
        - Url (string): Url to client.
    - Images
      - NudesDataPath (string): Path to directory of images. Bot take one of these images on nudes command.
      - NotNudesDataPath (string): Path to directory of images. Bot take one of these images on notnudes command.
      - AllowedDataPath (string[]): List of image extensions.
      - NoImage (string): Message to send, when directory is empty **(Deprecated)**
    - RoleManager
    - Math:
      - ComputingTime (int): Time in miliseconds to compute. When time is up, computing will be killed.
      - ProcessPath (string): Path to executable dll to computing engine.
  - AutoReply (array of objects):
    - In object:
      - IsInMessage:
        - Regex
        - OptionsFlags: Regex configuration ([RegexOptions](https://docs.microsoft.com/cs-cz/dotnet/api/system.text.regularexpressions.regexoptions?view=netframework-4.8))
      - Reply (string): Reply message when user sent message with regex match.
  - EmoteChain:
    - CheckLastN (int): Count of same emotes before bot send emote.
  - EmoteStats:
    - SyncWithDBSecs (int): Time in secondsto sync data in memory with database. **(Deprecated)**
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;

namespace DotEnv
{
  public static class Program
  {
    public static Task<int> Main(string[] args)
    {
      var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false)
        .AddUserSecrets(typeof(Program).Assembly, optional: true)
        .Build();

      var loggerFactory = LoggerFactory.Create(configure => configure.AddConfiguration(configuration));

      var builder = new CommandLineBuilder(new RootCommand());

      builder.ConfigureRootCommand()
        .AddMiddleware(
          (context, next) =>
          {
            context.BindingContext.AddService(typeof(IConfiguration), (services) => configuration);
            context.BindingContext.AddService(typeof(ILoggerFactory), (services) => loggerFactory);
            return next(context);
          }
        );
      var parser = builder.Build();
      return parser.InvokeAsync(args);
    }

    internal static Dictionary<string, string> Read(string path)
    {
      IEnumerable<KeyValuePair<string, string>> values = DotNetEnv.Env.LoadMulti(new[] { path });
      return new Dictionary<string, string>(values);
    }

    private static CommandLineBuilder ConfigureRootCommand(this CommandLineBuilder builder)
    {
      var verboseOption = new Option<bool>(
        name: "--verbose",
        description: "provide verbose output to stderr"
      );
      verboseOption.AddAlias("-v");
      builder.Command.AddGlobalOption(verboseOption);

      var fileOption = new Option<string>(
        name: "--file",
        description: "Location of the input .env file. Defaults to .env file in current working directory.",
        getDefaultValue: () => Path.GetFileName("./.env")

      );
      fileOption.AddAlias("--file");
      builder.Command.AddOption(fileOption);

      var targetOption = new Option<EnvironmentVariableTarget>(
        name: "--target",
        description: "Indicates whether the environment variable should be applied to the current process (default), the current user, or the machine. Valid values 'Process', 'Machine', or 'User'."
      );
      targetOption.AddAlias("-t");
      builder.Command.AddOption(targetOption);

      var dryRunOption = new Option<bool>(
        name: "--dry-run",
        description: "Loads and parses the .env file, but does not apply environment variable changes."
      );
      builder.Command.AddOption(dryRunOption);

      builder.Command.Handler = new RootCommandHandler(verboseOption, fileOption, targetOption, dryRunOption);

      return builder;
    }
  }
}
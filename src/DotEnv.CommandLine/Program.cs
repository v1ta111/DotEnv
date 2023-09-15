using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace DotEnv
{
  public static class Program
  {
    public static Task<int> Main(string[] args)
    {
      var rootCommand = new RootCommand();
      return rootCommand.InvokeAsync(args);
    }

    internal static Dictionary<string, string> Read(string path)
    {
      IEnumerable<KeyValuePair<string, string>> values = DotNetEnv.Env.LoadMulti(new[] { path });
      return new Dictionary<string, string>(values);
    }

    private static RootCommand Configure(this RootCommand rootCommand)
    {
      var verboseOption = new Option<bool>(
        name: "--verbose",
        description: "provide verbose output to stderr"
      );
      verboseOption.AddAlias("-v");
      rootCommand.AddGlobalOption(verboseOption);

      var fileOption = new Option<string>(
        name: "--file",
        description: "Location of the input .env file. Defaults to .env file in current working directory.",
        getDefaultValue: () => Path.GetFileName("./.env")
      );
      fileOption.AddAlias("--file");
      rootCommand.AddOption(fileOption);

      var targetOption = new Option<EnvironmentVariableTarget>(
        name: "--target",
        description: "Indicates whether the environment variable should be applied to the current process (default), the current user, or the machine. Valid values 'Process', 'Machine', or 'User'."
      );
      targetOption.AddAlias("-t");
      rootCommand.AddOption(targetOption);

      var dryRunOption = new Option<bool>(
        name: "--dry-run",
        description: "Loads and parses the .env file, but does not apply environment variable changes."
      );
      rootCommand.AddOption(dryRunOption);

      rootCommand.SetHandler((ctx) => {
          var verbose = ctx.BindingContext.ParseResult.GetValueForOption(verboseOption);
          var file = ctx.BindingContext.ParseResult.GetValueForOption(fileOption);
          var target = ctx.BindingContext.ParseResult.GetValueForOption(targetOption);
          var dryRun = ctx.BindingContext.ParseResult.GetValueForOption(dryRunOption);
          ctx.ExitCode = Program.Run(new Options
            {
              Path = file,
              Target = target,
              Verbose = verbose,
              DryRun = dryRun
            }
          );
      });

      return rootCommand;
    }

    private static int Run(Options options)
    {
      string envFile = options.Path;
      if (!Path.IsPathRooted(options.Path))
      {
        envFile = Path.Combine(Directory.GetCurrentDirectory(), options.Path);
        envFile = Path.GetFullPath(envFile);
      }

      if (!File.Exists(envFile))
      {
        ConsoleLogger.LogError($"ERROR: Unable to find file '{envFile}'");
        return 1;
      }

      Dictionary<string, string> values = Program.Read(envFile);
      bool log = options.Verbose || options.DryRun;
      string prefix = options.Verbose ? "VERBOSE" : "DRY-RUN";

      if (values.Count > 0)
      {
        if (log)
        {
          ConsoleLogger.LogInfo($"{prefix}: Applying {values.Count} environment variables to current {options.Target}");
        }

        foreach (var (key, value) in values)
        {
          if (log)
          {
            ConsoleLogger.LogInfo($"{prefix}: {key}={value}");
          }

          if (!options.DryRun)
          {
            Environment.SetEnvironmentVariable(key, value, options.Target);
          }
        }
      }
      else if (log)
      {
        ConsoleLogger.LogInfo($"{prefix}: No environment variables found in file '{envFile}'");
      }

      return 0;
    }
  }
}

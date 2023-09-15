using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace DotEnv
{
  public sealed class Options
  {
    public string Path { get; init; }

    public EnvironmentVariableTarget Target { get; init; }

    public bool Verbose { get; init; }

    public bool DryRun { get; init; }
/*
    public static IEnumerable<Example> Examples
    {
      get
      {
        var settings = UnParserSettings.WithUseEqualTokenOnly();
        yield return new Example("Run using local .env file", settings, new Options());
        yield return new Example("Run using specified file", settings, new Options { Path = ".env.local" });
        yield return new Example("Apply variables to Machine level", settings,
          new Options { Target = EnvironmentVariableTarget.Machine });
        yield return new Example("Run without applying changes", settings, new Options { DryRun = true });
      }
    }
*/
  }

    internal class RootCommandHandler : ICommandHandler
    {
        private Option<bool> verboseOption;
        private Option<string> fileOption;
        private Option<EnvironmentVariableTarget> targetOption;
        private Option<bool> dryRunOption;

        public RootCommandHandler(Option<bool> verboseOption, Option<string> fileOption, Option<EnvironmentVariableTarget> targetOption, Option<bool> dryRunOption)
        {
            this.verboseOption = verboseOption;
            this.fileOption = fileOption;
            this.targetOption = targetOption;
            this.dryRunOption = dryRunOption;
        }

        public int Invoke(InvocationContext context)
      {
        var verbose = context.BindingContext.ParseResult.GetValueForOption(verboseOption);
        var file = context.BindingContext.ParseResult.GetValueForOption(fileOption);
        var target = context.BindingContext.ParseResult.GetValueForOption(targetOption);
        var dryRun = context.BindingContext.ParseResult.GetValueForOption(dryRunOption);
        return this.Run(context.Console, new Options
            {
              Path = file,
              Target = target,
              Verbose = verbose,
              DryRun = dryRun
            }
          );
      }

      public Task<int> InvokeAsync(InvocationContext context)
      {
        return Task.FromResult(this.Invoke(context));
      }

      private int Run(IConsole console, Options options)
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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
namespace DotEnv
{
    internal class RootCommandHandler : ICommandHandler
    {
        private readonly Option<bool> verboseOption;
        private readonly Option<string> fileOption;
        private readonly Option<EnvironmentVariableTarget> targetOption;
        private readonly Option<bool> dryRunOption;

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
          return this.Run(new Options
              {
              Path = file,
              Target = target,
              Verbose = verbose,
              DryRun = dryRun
              },
              context
              );
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
          return Task.FromResult(this.Invoke(context));
        }

        private int Run(Options options, InvocationContext context)
        {
          var logger = context.BindingContext.GetRequiredService<ILogger<RootCommandHandler>>();
          string envFile = options.Path;
          if (!Path.IsPathRooted(options.Path))
          {
            envFile = Path.Combine(Directory.GetCurrentDirectory(), options.Path);
            envFile = Path.GetFullPath(envFile);
          }

          if (!File.Exists(envFile))
          {
            logger.LogError("ERROR: Unable to find environment file '{path}'", envFile);
            return 1;
          }

          Dictionary<string, string> values = Program.Read(envFile);
          bool log = options.Verbose || options.DryRun;
          string prefix = options.Verbose ? "VERBOSE" : "DRY-RUN";

          if (values.Count > 0)
          {
            if (log)
            {
              logger.LogInformation("{prefix}: Applying {values.Count} environment variables to current {options.Target}", prefix, values.Count, options.Target);
            }

            foreach (var (key, value) in values)
            {
              if (log)
              {
                logger.LogInformation("{prefix}: {key}={value}", prefix, key, value);
              }

              if (!options.DryRun)
              {
                Environment.SetEnvironmentVariable(key, value, options.Target);
              }
            }
          }
          else if (log)
          {
            logger.LogInformation("{prefix}: No environment variables found in environment file '{path}'", prefix, envFile);
          }

          return 0;
        }
    }
}
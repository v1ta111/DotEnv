using System;

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

          new Options { Target = EnvironmentVariableTarget.Machine });
        yield return new Example("Run without applying changes", settings, new Options { DryRun = true });
      }
    }
*/
  }
}
using System.ComponentModel;
using System.Diagnostics;

namespace MatchDay.IntegrationTests;

/// <summary>The integration tests start real PostgreSQL, Redis and RabbitMQ containers, so they need Docker.</summary>
internal static class DockerAvailability
{
    public const string SkipReason =
        "Docker isn't available. These tests start PostgreSQL, Redis and RabbitMQ containers - start Docker and run them again.";

    public static bool IsAvailable { get; } = Check();

    private static bool Check()
    {
        try
        {
            using var docker = Process.Start(new ProcessStartInfo("docker", "info --format {{.ServerVersion}}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });

            if (docker is null)
            {
                return false;
            }

            if (!docker.WaitForExit(TimeSpan.FromSeconds(20)))
            {
                docker.Kill();
                return false;
            }

            return docker.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            // The docker CLI isn't installed
            return false;
        }
    }
}

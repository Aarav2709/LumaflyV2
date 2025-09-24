
using System.Threading.Tasks;
using Needlelight.Interfaces;

namespace Needlelight.Services;

/// <summary>
/// Stub app updater: updates were intentionally removed. This class implements the
/// <see cref="IAppUpdater"/> interface but performs no actions. Keeping this
/// non-destructive stub avoids changing DI registrations and callers across the
/// codebase.
/// </summary>
public class AppUpdater : IAppUpdater
{
    public AppUpdater(ISettings _)
    {
        // No-op
    }

    public Task CheckUpToDate(bool forced = false)
    {
        // Updates are disabled in this build.
        return Task.CompletedTask;
    }
}



using System.Threading.Tasks;

namespace Needlelight.Interfaces;

public interface IAppUpdater
{
    public Task CheckUpToDate(bool forced = false);
}

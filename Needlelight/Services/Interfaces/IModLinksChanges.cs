using System.Threading.Tasks;

namespace Needlelight.Interfaces;

public interface IModLinksChanges
{
    public Task LoadChanges();
    public bool? IsLoaded { get; }
}

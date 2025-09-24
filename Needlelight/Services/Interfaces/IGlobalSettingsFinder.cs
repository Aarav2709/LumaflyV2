using Needlelight.Models;

namespace Needlelight.Interfaces;

public interface IGlobalSettingsFinder
{
    public string? GetSettingsFileLocation(ModItem modItem);
}

using System;
using System.Linq;
using Needlelight.Services;

namespace Needlelight.Models;
/// <summary>
/// A record to represent a pack of mods
/// </summary>
[Serializable]
public record Pack(string Name, string Description, string Authors, InstalledMods InstalledMods)
{
    /// <summary>
    /// A list of the names of the mods in the profile.
    /// </summary>
    public InstalledMods InstalledMods { get; set; } = InstalledMods;
    
    /// <summary>
    /// The name of the pack
    /// </summary>
    public string Name { get; set; } = Name;
    
    /// <summary>
    /// The description of the pack
    /// </summary>
    public string Description { get; set; } = Description;
    
    /// <summary>
    /// The description of the pack
    /// </summary>
    public string Authors { get; set; } = Authors;

    public string? SharingCode { get; set; }
    
    public bool HasSharingCode => !string.IsNullOrEmpty(SharingCode);
    
    public string ModList => InstalledMods.Mods.Keys
        .Concat(InstalledMods.NotInModlinksMods.Keys.Select(x => $"{x} ({Resources.MVVM_NotInModlinks_Disclaimer})"))
        .Aggregate("", (x, y) => x + y + "\n");
    
    public Pack DeepCopy() => new(Name, Description, Authors, InstalledMods.DeepCopy());

    public void Copy(Pack pack)
    {
        Name = pack.Name;
        Description = pack.Description;
        Authors = pack.Authors;
        SharingCode = pack.SharingCode;
        InstalledMods = pack.InstalledMods;
    }
    
    public bool IsSame(Pack pack)
    {
        return
            Name == pack.Name &&
            Description == pack.Description &&
            Authors == pack.Authors &&
            (
                InstalledMods.Mods.All(x =>
                    pack.InstalledMods.Mods.ContainsKey(x.Key) && pack.InstalledMods.Mods[x.Key] == x.Value)
                &&
                InstalledMods.NotInModlinksMods.All(x =>
                    pack.InstalledMods.NotInModlinksMods.ContainsKey(x.Key) && pack.InstalledMods.NotInModlinksMods[x.Key] == x.Value)
            );
    }
}

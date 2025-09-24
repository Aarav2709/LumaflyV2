using System.Collections.Generic;
using Needlelight.Models;

namespace Needlelight.Interfaces
{
    public interface IModDatabase
    {
        List<ModItem> Items { get; }
        
        (string Url, int Version, string SHA256) Api { get; }
    }
}

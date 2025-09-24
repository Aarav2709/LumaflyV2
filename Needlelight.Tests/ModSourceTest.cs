using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using Needlelight.Interfaces;
using Needlelight.Models;
using Needlelight.Services;
using Xunit;

namespace Needlelight.Tests
{
    public class ModSourceTest
    {
        [Fact]
        public async Task Record()
        {
            var fs = new MockFileSystem();
            // Prepare config dir for legacy path (tests don't load real settings)
            fs.AddDirectory(Path.GetDirectoryName(InstalledMods.LegacyConfigPath));

            IModSource ms = new InstalledMods(fs);

            var orig_version = new Version("1.3.2.2");

            var state = new InstalledState(true, orig_version, true);

            var item = new ModItem
            (
                null,
                state,
                new Version("1.3.2.2"),
                Array.Empty<string>(),
                string.Empty,
                string.Empty,
                "test",
                "test",
                "repo",
                "repo",
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>()
            );

            await ms.RecordInstalledState(item);

            // Build the expected per-game path using a default settings (no Game -> HK key)
            var expectedPath = InstalledMods.GetConfigPathFor(new Settings());
            Assert.True(fs.FileExists(expectedPath));

            var manifest = new Manifest {
                Name = "test"
            };

            ModState up_to_date = ms.FromManifest
            (
                manifest with {
                    Version = orig_version
                }
            );

            Assert.Equal(up_to_date, item.State);

            var new_version = new Version("2.0.0.0");

            Assert.Equal
            (
                ms.FromManifest
                (
                    manifest with {
                        Version = new_version
                    }
                ),
                state with { Updated = false }
            );

            item.State = new NotInstalledState();

            await ms.RecordUninstall(item);

            Assert.Equal(ms.FromManifest(manifest), new NotInstalledState());
        }
    }
}


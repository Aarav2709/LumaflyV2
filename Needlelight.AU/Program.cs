using System.Diagnostics;
using System.Reflection;
using System.Web;

try
{
    // set defaults to allow AU.exe to be run manually just incase
    string NeedlelightExeName = "Needlelight.exe";
    string NeedlelightPath = Environment.CurrentDirectory;
    bool shouldLaunchUpdated = true; // default to launching the updated app

    string fullPath = string.Empty;

    // concat all cli args (except first which is the path of this app) to form the full path of Needlelight which is passed
    // in by the installer. This is done because the path may contain spaces which would be split into multiple args
    for (int i = 1; i < Environment.GetCommandLineArgs().Length; i++)
    {
        Console.WriteLine($"Arg passed {i}: '{Environment.GetCommandLineArgs()[i]}'");
        fullPath += Environment.GetCommandLineArgs()[i] + " ";
    }

    fullPath = fullPath.Trim(); // trim the last space

    // see if full path is a valid file
    if (File.Exists(fullPath))
    {
        NeedlelightExeName = Path.GetFileName(fullPath);
        NeedlelightPath = Path.GetDirectoryName(fullPath) ?? throw new Exception("Invalid path given in arguments.");
        shouldLaunchUpdated = false; // don't launch the updated app as it'll be handled by NetSparkle

        // do rename of exe if it has Scarab in its name
        if (NeedlelightExeName.Contains("Scarab"))
        {
            NeedlelightExeName = NeedlelightExeName.Replace("Scarab", "Needlelight");
            shouldLaunchUpdated = true; // netsparkle wont launch Needlelight if exe name had scarab
        }
    }

    var originalNeedlelightExe = Path.Combine(NeedlelightPath, NeedlelightExeName);

    var updatedNeedlelightExeBytes = GetNeedlelightExe() ?? throw new Exception("Unable to get updated Needlelight.");
    var updatedNeedlelightFile = Path.Combine(NeedlelightPath, "Needlelight-Update.exe");

    // these actions shouldn't fail as Needlelight is running as admin
    File.WriteAllBytes(updatedNeedlelightFile, updatedNeedlelightExeBytes); // create the file
    if (File.Exists(originalNeedlelightExe)) // delete the old file
    {
        File.Delete(originalNeedlelightExe);
    }
    else // it means original exe file has Scarab in it and "originalNeedlelightExe" doesnt exist anymore as we did the replace above
    {
        if (File.Exists(fullPath)) File.Delete(fullPath); // delete the actual old file
        shouldLaunchUpdated = true; // netsparkle doesn't expect this to happen so we have to do it now
    }
    File.Move(updatedNeedlelightFile, originalNeedlelightExe); // move the new file to the old file's location

    Console.WriteLine("Successfully updated Needlelight.");

    Task.Delay(500).Wait(); // wait a second to show message

    // only relaunch app if it was launched manually
    if (shouldLaunchUpdated)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = NeedlelightExeName,
            WorkingDirectory = Environment.CurrentDirectory,
            UseShellExecute = true,
        });
    }
}
catch (Exception e)
{
    Console.WriteLine($"Press any key to continue. Unable to complete autoupdate because {e.Message}");
    Console.ReadKey();
}

// gets Needlelight.exe from embedded resources
static byte[]? GetNeedlelightExe()
{
    var asm = Assembly.GetExecutingAssembly();
    foreach (string res in asm.GetManifestResourceNames())
    {
        if (res.EndsWith("Needlelight.exe"))
        {
            var s = asm.GetManifestResourceStream(res);
            if (s == null) continue;
            var buffer = new byte[s.Length];
            _ = s.Read(buffer, 0, buffer.Length);
            s.Dispose();
            return buffer;
        }
    }
    return null;
}


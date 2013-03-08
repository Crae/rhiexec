using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.Model
{
  /// <summary>
  /// The base class for all installable objects
  /// </summary>
  abstract class PackageInstallerBase
  {
    /// <summary>
    /// Unique identifier of the package.
    /// </summary>
    public abstract Guid ID
    {
      get;
    }

    /// <summary>
    /// Version string. Can be any format, so long as the newer
    /// versions sort to the top of the list when sorted
    /// in descending order.
    /// </summary>
    public abstract Version PackageVersion
    {
      get;
    }

    /// <summary>
    /// Human readable title of the Package. This name is displayed in the
    /// InstallerEngine user interface.
    /// </summary>
    public abstract string Title
    {
      get;
    }

    /// <summary>
    /// The content type of this installer package.
    /// </summary>
    public abstract PackageContentType ContentType
    {
      get;
    }

    /// <summary>
    /// Full path to the package.
    /// </summary>
    public abstract string PackagePath
    {
      get;
    }

    /// <summary>
    /// The default install root for a package.
    /// </summary>
    public abstract PackageInstallRoot InstallRoot
    {
      get;
    }

    /// <summary>
    /// The InstallerEngine calls the InstallFolder function so that the
    /// PackageInstaller can append appropriate directory information to the
    /// rootFolder prior to installation
    /// </summary>
    public abstract string InstallFolder(string rootFolder);

    /// <summary>
    /// Called by the InstallerEngine to determine if this Package
    /// is currently installed on the user's system, and if the
    /// installed version is older, the same as, or newer than
    /// the one in this Package.
    /// </summary>
    public abstract PackageInstallState GetInstallState(Package package);

    /// <summary>
    /// Check if this Package is compatible with an installed Rhino.
    /// </summary>
    /// <param name="info">
    /// A RhinoInfo class representing an installation of Rhino on the target machine.
    /// </param>
    /// <returns>
    /// true if this Package is compatible with Rhino represented by info.
    /// false otherwise.
    /// </returns>
    public abstract bool IsCompatible(RhinoInfo rhino);

    /// <summary>
    /// InstallerEngine will extract the contents of .rhi file into
    /// PackageFolder and then allow each Package-derived
    /// class to inspect the directory for recognized payload.
    /// 
    /// This scan should be fast, and should work on all platforms.
    /// 
    /// For example, scanning the direcotry tree for recognized file
    /// types is probably sufficient.
    /// </summary>
    /// <param name="PackageFolder"></param>
    /// <returns>true if PackageFolder contains payload that can be installed by this Package instance.</returns>
    public abstract bool ContainsRecognizedPayload(Package package);

    /// <summary>
    /// Called by the InstallerEngine to do the full initialization
    /// of the Package. This will only be called if ContainsRecognizedPayload()
    /// returned true.
    /// 
    /// All data needed by the Install() function should be computed and saved
    /// as __~~[filename]~~__.tmp files in PackageFolder.
    /// 
    /// Note that the PackageFolder is still a temporary location at this point,
    /// so no installation or registration should happen at this point.
    /// </summary>
    /// <param name="PackageFolder">Full path to the package to be initialized.</param>
    /// <returns>true if initalization succeeds; false otherwise</returns>
    public abstract bool Initialize(Package package);

    /// <summary>
    /// Called by the InstallerEngine to finalize the installation 
    /// </summary>
    /// <returns></returns>
    public virtual bool BeforeInstall(Package package, RhinoInfo[] RhinoList, InstallerUser installAsUser)
    {
      return true;
    }

    /// <summary>
    /// Called by the InstallerEngine to finalize the installation 
    /// </summary>
    /// <returns></returns>
    public virtual bool AfterInstall(Package package, RhinoInfo[] RhinoList, InstallerUser installAsUser)
    {
      return true;
    }

    public virtual bool ShouldReplaceFile(string DestinationFilePath)
    {
      return true;
    }

    /// <summary>
    /// Text description of the contents of this Package.
    /// Used by debug logging.
    /// </summary>
    /// <returns></returns>
    public abstract string Describe();

    /// <summary>
    /// Derived classes can (and should) report their progress throughout.
    /// This method provides thread-safe and thread-agnostic access to the
    /// main logging engine.
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="level">Log Level</param>
    public static void ReportProgress(string message, LogLevel level)
    {
      InstallerEngine.ReportProgress(level, message);
    }

    public static string RemoveInvalidPathChars(string fileName)
    {
      char[] invalid_chars = System.IO.Path.GetInvalidFileNameChars();
      string clean = fileName;
      int index = clean.IndexOfAny(invalid_chars);
      while (index >= 0)
      {
        clean = clean.Remove(index, 1);
        index = clean.IndexOfAny(invalid_chars);
      }
      return clean;
    }

    public static string PackageManifestName
    {
      get { return "package.xml"; }
    }

    protected static Version GetNewestVersionOfInstalledPackage(string folder)
    {
      ReportProgress("Getting newest version of installed package", LogLevel.Debug);
      if (!Directory.Exists(folder))
      {
        ReportProgress("Package folder not found: " + folder, LogLevel.Debug);
        return null;
      }

      string[] directories = GetAllInstalledVersions(folder);

      Version vNewest = new Version(1,0,0,0);
      foreach (string directory in directories)
      {
        string sVersion = Path.GetFileName(directory);
        Version v;
        if (Version.TryParse(sVersion, out v))
        {
          vNewest = v > vNewest ? v : vNewest;
        }
      }

      if (vNewest > new Version(1, 0, 0, 0))
        return vNewest;

      return null;
    }

    static int CompareDirectoriesByVersionNumber(string a, string b)
    {
      string sVa = Path.GetFileName(a);
      string sVb = Path.GetFileName(b);

      Version vA, vB;
      bool bHaveA = false, bHaveB = false;
      if (Version.TryParse(sVa, out vA))
      {
        bHaveA = true;
      }
      if (Version.TryParse(sVb, out vB))
      {
        bHaveB = true;
      }

      if (bHaveA && bHaveB)
        return vA.CompareTo(vB);
      if (bHaveA)
        return 1;
      if (bHaveB)
        return -1;

      return 0;
    }

    public static string[] GetAllInstalledVersions(string folder)
    {
      List<string> directories = new List<string>(Directory.GetDirectories(folder));

      // Filter out directories that are not version numbers
      Regex rxVersionNumbers = new Regex(@"\d+\.\d+\.\d+\.\d+", RegexOptions.Compiled);
      for (int i = directories.Count - 1; i >= 0; i--)
      {
        string folderName = Path.GetFileName(directories[i]);
        if (!rxVersionNumbers.IsMatch(folderName))
          directories.RemoveAt(i);
      }

      if (directories.Count > 1)
        directories.Sort(CompareDirectoriesByVersionNumber);

      return directories.ToArray();
    }

    public virtual PackageInstallState GetPackageInstallState(Version VersionNumber)
    {
      ReportProgress("Getting Package InstallState for " + this.PackagePath, LogLevel.Debug);

      // Is plug-in installed for all users?
      ReportProgress("Checking install state for all users", LogLevel.Debug);
      string folder = InstallFolder(InstallerEngine.AllUsersInstallRoot);

      // AllUsersInstallFolder returns ...\plug-in name\version
      // we want to look in just ...\plug-in name
      folder = Path.GetDirectoryName(folder);

      Version InstalledVersion = GetNewestVersionOfInstalledPackage(folder);
      PackageInstallState state = CompareVersions(VersionNumber, InstalledVersion);

      if (state > PackageInstallState.NotInstalled)
        return state;


      // Is plug-in installed for current user?
      ReportProgress("Checking install state for current user", LogLevel.Debug);
      if (this.InstallRoot == PackageInstallRoot.CurrentUserLocalProfile)
      {
        folder = InstallFolder(InstallerEngine.CurrentUserLocalProfileRoot);
      }
      else if (this.InstallRoot == PackageInstallRoot.CurrentUserRoamingProfile)
      {
        folder = InstallFolder(InstallerEngine.CurrentUserRoamingProfileRoot);
      }

      // CurrentUserInstallFolder returns ...\plug-in name\version
      // we want to look in just ...\plug-in name
      folder = Path.GetDirectoryName(folder);

      InstalledVersion = GetNewestVersionOfInstalledPackage(folder);
      return CompareVersions(VersionNumber, InstalledVersion);
    }

    protected static PackageInstallState CompareVersions(Version PackageVersion, Version InstalledVersion)
    {
      if (InstalledVersion == null)
      {
        ReportProgress("Not installed.", LogLevel.Debug);
        return PackageInstallState.NotInstalled;
      }

      int cf = InstalledVersion.CompareTo(PackageVersion);
      if (cf < 0)
      {
        ReportProgress("Older version installed.", LogLevel.Debug);
        return PackageInstallState.OlderVersionInstalledAllUsers;
      }
      if (cf == 0)
      {
        ReportProgress("Same version installed.", LogLevel.Debug);
        return PackageInstallState.SameVersionInstalledAllUsers;
      }
      if (cf > 0)
      {
        ReportProgress("Newer version installed.", LogLevel.Debug);
        return PackageInstallState.NewerVersionInstalledAllUsers;
      }

      return PackageInstallState.NotInstalled;
    }



  }

  public enum RhinoInstallState
  {
    Unknown,
    Found,
    NotFound,
  }

  public enum PackageContentType
  {
    Unknown,
    Plugin,
    Python,
    Tutorial,
    Localization,
    UserInterface,
    HelpMedia,
    Help,
  }

  public enum PackageInstallRoot
  {
    CurrentUserRoamingProfile,
    CurrentUserLocalProfile,
    AllUsers,
  }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using RMA.RhiExec.Engine;

namespace RMA.RhiExec.Model
{
  class PluginInfo : PackageManifest
  {
    private RhinoInstallState m_compatible_rhino_found = RhinoInstallState.Unknown;
    private string m_plugin_path = "";
    private string RhinoSDKVersion = "";
    private string RhinoSDKServiceRelease = "";
    private string DotNetSDKVersion = "";
    private string RhinoCommonSDKVersion = "";

    private PackageInstallState m_install_state = PackageInstallState.Unknown;

    public PluginInfo()
      : base(PackageContentType.Plugin)
    {
    }

    #region Package Members
    public PackageInstallState InstallState 
    {
      get { return m_install_state; }
      set { m_install_state = value; }
    }

    public string PluginPath
    {
      get { return m_plugin_path; }
      set { m_plugin_path = value; }
    }

    public bool IsCompatible(RhinoInfo rhino)
    {
      PluginInfo plugin = this;
      if (!rhino.IsValid())
      {
        Logger.Log(LogLevel.Debug, "RhinoInfo::IsPluginCompatible called with invalid PluginInfo: " + plugin.m_plugin_path);
        return false;
      }

      // Plug-in uses RhinoDotNet?
      if (!string.IsNullOrEmpty(plugin.DotNetSDKVersion))
      {
        // Get last section of Rhino DotNet version number
        Match m = Regex.Match(rhino.RhinoDotNetVersion, @"\.(\d+)$");
        string sRhinoRev = m.Groups[1].Value;
        int nRhinoRev = -1;
        if (!int.TryParse(sRhinoRev, out nRhinoRev))
          nRhinoRev = -1;

        // Get last section of Plug-in DotNet version number
        m = Regex.Match(plugin.DotNetSDKVersion, @"\.(\d+)$");
        string sPluginRev = m.Groups[1].Value;
        int nPluginRev = -1;

        if (!int.TryParse(sPluginRev, out nPluginRev))
          nPluginRev = -1;

        if (nRhinoRev < 0 || nPluginRev < 0)
        {
          Logger.Log(LogLevel.Debug, "RhinoDotNet version incompatibility: Rhino: " + rhino.RhinoDotNetVersion + ", Plug-in: " + plugin.DotNetSDKVersion);
          return false;
        }

        if (nRhinoRev < 100)
        {
          // Rhino is 4.0
          if (nPluginRev < 100)
          {
            // Plug-in is 4.0; go ahead and install
          }
          else
          {
            // Plug-in is newer than 4.0; do not install.
            Logger.Log(LogLevel.Debug, "RhinoDotNet version incompatibility: Rhino: " + rhino.RhinoDotNetVersion + ", Plug-in: " + plugin.DotNetSDKVersion);
            return false;
          }
        }
        else
        {
          // Rhino is 5.0
        }

        // Plug-in is dotNet, but Rhino has older version of dotNet
        int cf = string.Compare(rhino.RhinoDotNetVersion, plugin.DotNetSDKVersion, StringComparison.OrdinalIgnoreCase);
        if (cf < 0)
        {
          Logger.Log(LogLevel.Debug, "RhinoDotNet version incompatibility: Rhino: " + rhino.RhinoDotNetVersion + ", Plug-in: " + plugin.DotNetSDKVersion);
          return false;
        }
      }

      // Plug-in uses RhinoCommon?
      if (!string.IsNullOrEmpty(plugin.RhinoCommonSDKVersion))
      {
        // Plug-in uses RhinoCommon, but Rhino doesn't have it
        if (string.IsNullOrEmpty(rhino.RhinoCommonVersion))
        {
          Logger.Log(LogLevel.Debug, "RhinoCommon version incompatibility: Rhino: not found, Plug-in: " + plugin.RhinoCommonSDKVersion);
          return false;
        }

        // Plug-in uses RhinoCommon, but Rhino has older version of RhinoCommon
        int cf = string.Compare(rhino.RhinoCommonVersion, plugin.RhinoCommonSDKVersion, StringComparison.OrdinalIgnoreCase);
        if (cf < 0)
        {
          Logger.Log(LogLevel.Debug, "RhinoCommon version incompatibility: Rhino: " + rhino.RhinoCommonVersion + ", Plug-in: " + plugin.RhinoCommonSDKVersion);
          return false;
        }
      }

      // Plug-in uses C++?
      if (!string.IsNullOrEmpty(plugin.RhinoSDKVersion))
      {
        if (rhino.RhinoSdkVersion.EndsWith("0", StringComparison.OrdinalIgnoreCase))
        {
          // Rhino is 4.0
          if (plugin.RhinoSDKVersion.EndsWith("0", StringComparison.OrdinalIgnoreCase))
          {
            // Do nothing. Plug-in is compatible.
          }
          else
          {
            Logger.Log(LogLevel.Debug, "RhinoSdkVersion incompatibility: Rhino: " + rhino.RhinoSdkVersion + ", Plug-in: " + plugin.RhinoSDKVersion);
            return false; // Plug-in is not compatible
          }
        }
        else if (rhino.RhinoSdkVersion.EndsWith("5", StringComparison.OrdinalIgnoreCase))
        {
          // Rhino is 5.0
          if (plugin.RhinoSDKVersion.EndsWith("5", StringComparison.OrdinalIgnoreCase))
          {
            // Plugin is 4.0 or 5.0
            // do nothing; rhino is compatible
          }
          else
          {
            Logger.Log(LogLevel.Debug, "RhinoSdkVersion incompatibility: Rhino: " + rhino.RhinoSdkVersion + ", Plug-in: " + plugin.RhinoSDKVersion);
            return false;
          }
        }
        else
        {
          Logger.Log(LogLevel.Debug, "RhinoSdkVersion incompatibility: Rhino: " + rhino.RhinoSdkVersion + ", Plug-in: " + plugin.RhinoSDKVersion);
          return false;
        }
      }

      if (!string.IsNullOrEmpty(plugin.RhinoSDKServiceRelease))
      {
        if (rhino.RhinoSdkServiceRelease.EndsWith("0", StringComparison.OrdinalIgnoreCase))
        {
          // Rhino is 4.0
          if (plugin.RhinoSDKServiceRelease.EndsWith("0", StringComparison.OrdinalIgnoreCase))
          {
            // do nothing, rhino is compatible
          }
          else
          {
            Logger.Log(LogLevel.Debug, "RhinoSdkServiceRelease incompatibility: Rhino: " + rhino.RhinoSdkServiceRelease + ", Plug-in: " + plugin.RhinoSDKServiceRelease);
            return false;
          }
        }
        else if (rhino.RhinoSdkServiceRelease.EndsWith("5", StringComparison.OrdinalIgnoreCase))
        {
          if (plugin.RhinoSDKServiceRelease.EndsWith("0", StringComparison.OrdinalIgnoreCase)
              || plugin.RhinoSDKServiceRelease.EndsWith("5", StringComparison.OrdinalIgnoreCase))
          {
            // do nothing, rhino is compatible
          }
          else
          {
            Logger.Log(LogLevel.Debug, "RhinoSdkServiceRelease incompatibility: Rhino: " + rhino.RhinoSdkServiceRelease + ", Plug-in: " + plugin.RhinoSDKServiceRelease);
            return false;
          }
        }
        else
        {
          // unknown Rhino
          Logger.Log(LogLevel.Debug, "RhinoSdkServiceRelease incompatibility: Rhino: " + rhino.RhinoSdkServiceRelease + ", Plug-in: " + plugin.RhinoSDKServiceRelease);
          return false;
        }
      }

      Logger.Log(LogLevel.Info, "Compatible Rhino found: " + rhino.RhinoExePath);
      return true;
    }

    public RhinoInstallState CompatibleRhinoInstalled
    {
      set { m_compatible_rhino_found = value; }
    }

    public override bool IsValid()
    {
      for (; ; )
      {
        // If there's no path, this isn't valid
        if (string.IsNullOrEmpty(m_plugin_path))
          break;

        if (IsCPP() || IsDotNet() || IsRhinoCommon())
        {
          if (!base.IsValid())
            break;
        }
        else
        {
          // This is not a known plug-in type; return false
          Logger.Log(LogLevel.Debug, "PluginInfo::IsValid() returning false");
          return false;
        }

        Logger.Log(LogLevel.Debug, "PluginInfo::IsValid() returning true");
        return true;
      }

      Logger.Log(LogLevel.Debug, "PluginInfo::IsValid() returning false");
      return false;
    }

    public string Describe()
    {
      StringBuilder sb = new StringBuilder();
      
      sb.Append("Plug-in:").Append("\r\n");
      sb.Append(this.Title).Append("\r\n");
      sb.Append(this.VersionNumber).Append("\r\n");
      sb.Append(this.OS).Append("\r\n");
      sb.Append("InstallState: " + this.InstallState).Append("\r\n");
      sb.Append("Compatible Rhino: " + this.m_compatible_rhino_found).Append("\r\n");
      sb.Append("PluginPath: " + this.m_plugin_path).Append("\r\n");
      sb.Append("Rhino SDK Version: " + this.RhinoSDKVersion).Append("\r\n");
      sb.Append("Rhino SDK Service Release: " + this.RhinoSDKServiceRelease).Append("\r\n");
      sb.Append("Rhino Common SDK Version: " + this.RhinoCommonSDKVersion).Append("\r\n");
      sb.Append(".NET SDK Version: " + this.DotNetSDKVersion).Append("\r\n");
      
      return sb.ToString();
    }

    #endregion

    private bool IsCPP()
    {
      if (!string.IsNullOrEmpty(RhinoSDKVersion) && !string.IsNullOrEmpty(RhinoSDKServiceRelease))
        return true;

      return false;
    }

    private bool IsDotNet()
    {
      if (!string.IsNullOrEmpty(DotNetSDKVersion))
        return true;

      return false;
    }

    private bool IsRhinoCommon()
    {
      if (!string.IsNullOrEmpty(RhinoCommonSDKVersion))
        return true;

      return false;
    }

    public int CompareTo(PluginInfo cf)
    {
      if (cf == null)
        return 1;

      // See if version numbers differ
      int comp = this.VersionNumber.CompareTo(cf);
      if (comp != 0)
        return comp;

      /*
      this.RhinoSDKVersion;
      this.RhinoSDKServiceRelease;
      this.DotNetSDKVersion;
      this.RhinoCommonSDKVersion;
      */

      // See if RhinoSdkVersion numbers differ:
      if (!string.IsNullOrEmpty(this.RhinoSDKVersion) || !string.IsNullOrEmpty(cf.RhinoSDKVersion))
      {
        comp = string.Compare(this.RhinoSDKVersion, cf.RhinoSDKVersion, StringComparison.OrdinalIgnoreCase);
        if (comp != 0)
          return comp; // RhinoSdkVersion difference
      }
      else
      {
        // RhinoSdkVersion is empty on one or more objects; cannot compare
      }

      // See if RhinoSDKServiceRelease numbers differ:
      if (!string.IsNullOrEmpty(this.RhinoSDKServiceRelease) || !string.IsNullOrEmpty(cf.RhinoSDKServiceRelease))
      {
        comp = string.Compare(this.RhinoSDKServiceRelease, cf.RhinoSDKServiceRelease, StringComparison.OrdinalIgnoreCase);
        if (comp != 0)
          return comp; // RhinoSDKServiceRelease difference
      }
      else
      {
        // RhinoSDKServiceRelease is empty on one or more objects; cannot compare
      }

      // See if DotNetSDKVersion numbers differ:
      if (!string.IsNullOrEmpty(this.DotNetSDKVersion) || !string.IsNullOrEmpty(cf.DotNetSDKVersion))
      {
        comp = string.Compare(this.DotNetSDKVersion, cf.DotNetSDKVersion, StringComparison.OrdinalIgnoreCase);
        if (comp != 0)
          return comp; // DotNetSDKVersion difference
      }
      else
      {
        // DotNetSDKVersion is empty on one or more objects; cannot compare
      }

      // See if RhinoCommonSDKVersion numbers differ:
      if (!string.IsNullOrEmpty(this.RhinoCommonSDKVersion) || !string.IsNullOrEmpty(cf.RhinoCommonSDKVersion))
      {
        comp = string.Compare(this.RhinoCommonSDKVersion, cf.RhinoCommonSDKVersion, StringComparison.OrdinalIgnoreCase);
        if (comp != 0)
          return comp; // RhinoCommonSDKVersion difference
      }
      else
      {
        // RhinoCommonSDKVersion is empty on one or more objects; cannot compare
      }

      // Exact match
      return 0;
    }

    public bool InspectPlugin(string rhp_file)
    {
      // See if an XML file exists for this plug-in; if so, load it:
      if (XmlFileExists(rhp_file))
      {
        ReadXml(GetXmlFileName(rhp_file));
        return true;
      }

      AssemblyResolver.AddSearchPath(rhp_file, false);
      this.m_plugin_path = rhp_file;
      // See if we can load this module.
      if (!CpuMatchesCurrentProcess())
        return false;

      PopulateDotNetProperties();
      PopulateCppProperties();
      if (!GetPluginAttributes())
      {
        throw new PackageNotCompatibleException("This plug-in is not compatible with the Rhino Installer Engine.\nPlug-in: " + this.m_plugin_path + "\n\nFor details on the Rhino Installer Engine, please visit http://wiki.mcneel.com/developer/rhinoinstallerengine/overview");
      }

      return true;
    }

    // Writes the XML file to the same place where it was initialized from
    private static string GetXmlFileName(string rhp_file)
    {
      string xml = rhp_file + ".xml";
      return xml;
    }

    private static bool XmlFileExists(string rhp_file)
    {
      if (File.Exists(GetXmlFileName(rhp_file)))
        return true;

      return false;
    }

    #region Plug-in Inspection Functions
    private void PopulateDotNetProperties()
    {
      Assembly pluginAssembly = GetReflectionAssembly(this.m_plugin_path);
      if (pluginAssembly == null)
        return;

      AssemblyName[] references = pluginAssembly.GetReferencedAssemblies();

      bool usesRhinoDotNet = false;
      bool usesRhinoCommon = false;
      for (int i = 0; i < references.Length; i++)
      {
        AssemblyName reference = references[i];
        if (reference.Name.Equals("rhino_dotnet", StringComparison.OrdinalIgnoreCase))
        {
          DotNetSDKVersion = reference.Version.ToString();
          usesRhinoDotNet = true;
        }
        else if (reference.Name.Equals("rhinocommon", StringComparison.OrdinalIgnoreCase))
        {
          usesRhinoCommon = true;
          RhinoCommonSDKVersion = reference.Version.ToString();
        }
      }

      AssemblyName pluginMetadata = System.Reflection.AssemblyName.GetAssemblyName(this.m_plugin_path);
      if (usesRhinoCommon || usesRhinoDotNet)
      {
        // RhinoCommon is Rhino 5 only;
        switch (pluginMetadata.ProcessorArchitecture)
        {
          case ProcessorArchitecture.X86:
            this.SupportedPlatforms.Add(RhinoPlatform.Rhino5_win32);
            break;
          case ProcessorArchitecture.Amd64:
          case ProcessorArchitecture.IA64:
            this.SupportedPlatforms.Add(RhinoPlatform.Rhino5_win64);
            break;
          case ProcessorArchitecture.MSIL:
            this.SupportedPlatforms.Add(RhinoPlatform.Rhino5_win32);
            this.SupportedPlatforms.Add(RhinoPlatform.Rhino5_win64);
            break;
        }
      }
    }

    private void PopulateCppProperties()
    {
      IntPtr hPlugIn = UnsafeNativeMethods.LoadLibraryEx(this.m_plugin_path, IntPtr.Zero, UnsafeNativeMethods.DONT_RESOLVE_DLL_REFERENCES);
      if (IntPtr.Zero == hPlugIn)
        return;

      IntPtr funcPtr = UnsafeNativeMethods.GetProcAddress(hPlugIn, "RhinoPlugInSdkVersion");
      if (IntPtr.Zero == funcPtr)
        return;

      UnsafeNativeMethods.GetIntegerInvoke GetVersionInt = (UnsafeNativeMethods.GetIntegerInvoke)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(UnsafeNativeMethods.GetIntegerInvoke));
      int pluginSdkVersion = GetVersionInt();
      this.RhinoSDKVersion = pluginSdkVersion.ToString(CultureInfo.InvariantCulture);

      funcPtr = UnsafeNativeMethods.GetProcAddress(hPlugIn, "RhinoPlugInSdkServiceRelease");
      if (IntPtr.Zero == funcPtr)
        return;

      GetVersionInt = (UnsafeNativeMethods.GetIntegerInvoke)Marshal.GetDelegateForFunctionPointer(funcPtr, typeof(UnsafeNativeMethods.GetIntegerInvoke));
      int pluginSdkServiceRelease = GetVersionInt();
      this.RhinoSDKServiceRelease = pluginSdkServiceRelease.ToString(CultureInfo.InvariantCulture);

      if (this.RhinoSDKVersion.EndsWith("0", StringComparison.OrdinalIgnoreCase))
      {
        // Compiled with 4.0 SDK
        this.SupportedPlatforms.Add(RhinoPlatform.Rhino4_win32);
        this.SupportedPlatforms.Add(RhinoPlatform.Rhino5_win32);
      }
      if (this.RhinoSDKVersion.EndsWith("5", StringComparison.OrdinalIgnoreCase))
      {
        // Compiled with Rhino 5.0 SDK
        if (IntPtr.Size == 4)
          this.SupportedPlatforms.Add(RhinoPlatform.Rhino5_win32);
        else if (IntPtr.Size == 8)
          this.SupportedPlatforms.Add(RhinoPlatform.Rhino5_win64);
      }

      //TODO: Get plug-in version number, title, and GUID.
    }

    private bool GetPluginAttributes()
    {
      Assembly ass = GetReflectionAssembly(this.m_plugin_path);
      if (null != ass)
      {
        int informationalVersion = 0;
        string tempId = "";
        IList<CustomAttributeData> attrs = CustomAttributeData.GetCustomAttributes(ass);
        if (attrs != null)
        {
          // We require the AssemblyInformationalVersionAttribute to exist and be at least 2
          // If this is the case, then the GuidAttribute matches the plug-in Id.
          for (int i = 0; i < attrs.Count; i++)
          {
            CustomAttributeData attr = attrs[i];
            string name = attr.Constructor.ReflectedType.FullName;
            if (name != null && name.Equals("System.Reflection.AssemblyInformationalVersionAttribute", StringComparison.OrdinalIgnoreCase))
            {
              if (attr.ConstructorArguments.Count == 1)
              {
                string val = attr.ConstructorArguments[0].Value.ToString();
                if (!int.TryParse(val, out informationalVersion))
                  informationalVersion = 0;
              }
            }
            else if (name != null && name.Equals("System.Runtime.InteropServices.GuidAttribute", StringComparison.OrdinalIgnoreCase))
            {
              if (attr.ConstructorArguments.Count == 1)
              {
                tempId = attr.ConstructorArguments[0].Value.ToString();
              }
            }

            else if (name != null && name.Equals("System.Reflection.AssemblyTitleAttribute", StringComparison.OrdinalIgnoreCase))
            {
              if (attr.ConstructorArguments.Count == 1)
              {
                Title = attr.ConstructorArguments[0].Value.ToString();
              }
            }

          }
        }

        VersionNumber = ass.GetName().Version;
        if (informationalVersion >= 2)
          ID = new Guid(tempId);
      }
      else
      {
        this.OS = OSPlatform.Unknown;
        if (IntPtr.Size == 4)
          this.OS = OSPlatform.x86;
        else if (IntPtr.Size == 8)
          this.OS = OSPlatform.x64;

        // This is most likely an unmanaged C++ plug-in. Try to get id from an exported function
        IntPtr hPlugIn = UnsafeNativeMethods.LoadLibraryEx(this.m_plugin_path, IntPtr.Zero, UnsafeNativeMethods.DONT_RESOLVE_DLL_REFERENCES);
        if (IntPtr.Zero != hPlugIn)
        {
          // Plug-in GUID
          IntPtr funcPtrPlugInId = UnsafeNativeMethods.GetProcAddress(hPlugIn, "RhinoPlugInId");
          if (IntPtr.Zero != funcPtrPlugInId)
          {
            UnsafeNativeMethods.GetStringInvoke GetGuid;
            GetGuid = (UnsafeNativeMethods.GetStringInvoke)Marshal.GetDelegateForFunctionPointer(funcPtrPlugInId, typeof(UnsafeNativeMethods.GetStringInvoke));
            IntPtr pStr = GetGuid();
            string tmpGuid = Marshal.PtrToStringUni(pStr);
            ID = new Guid(tmpGuid);
          }

          // Plug-in Name
          IntPtr funcPtrPlugInName = UnsafeNativeMethods.GetProcAddress(hPlugIn, "RhinoPlugInName");
          if (IntPtr.Zero != funcPtrPlugInId)
          {
            UnsafeNativeMethods.GetStringInvoke GetName;
            GetName = (UnsafeNativeMethods.GetStringInvoke)Marshal.GetDelegateForFunctionPointer(funcPtrPlugInName, typeof(UnsafeNativeMethods.GetStringInvoke));
            IntPtr pStr = GetName();
            Title = Marshal.PtrToStringUni(pStr);
          }

          // Plug-in Version
          IntPtr funcPtrPlugInVersion = UnsafeNativeMethods.GetProcAddress(hPlugIn, "RhinoPlugInVersion");
          if (IntPtr.Zero != funcPtrPlugInId)
          {
            UnsafeNativeMethods.GetStringInvoke GetVersion;
            GetVersion = (UnsafeNativeMethods.GetStringInvoke)Marshal.GetDelegateForFunctionPointer(funcPtrPlugInVersion, typeof(UnsafeNativeMethods.GetStringInvoke));
            IntPtr pStr = GetVersion();
            string sVersion = Marshal.PtrToStringAnsi(pStr);

            // This version number could be a number of different things.
            // Likely, it's a date in text format.
            DateTime dVersion = new DateTime();
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("en-US", false);
            if (DateTime.TryParse(sVersion, culture, System.Globalization.DateTimeStyles.None, out dVersion))
            {
              // Coerce this into a Version object.
              TimeSpan TimeOfDay = dVersion.TimeOfDay;
              this.VersionNumber = new Version(dVersion.Year, dVersion.Month, dVersion.Day, (int)TimeOfDay.TotalMinutes);
            }
            else if (Regex.IsMatch(sVersion, @"\d+\.\d+\.\d+\.\d+"))
            {
              this.VersionNumber = new Version(sVersion);
            }
            else
            {
              throw new PackageNotCompatibleException("Version Number Not Recognized: " + sVersion);
            }
          }
        }
      }

      return (ID != Guid.Empty && !string.IsNullOrEmpty(Title) && VersionNumber != null);
    }

    private bool CpuMatchesCurrentProcess()
    {
      bool rc = false;

      Assembly ass = GetReflectionAssembly(this.m_plugin_path);
      if (null != ass)
      {
        PortableExecutableKinds pekind;
        ImageFileMachine machine;
        ass.ManifestModule.GetPEKind(out pekind, out machine);


        if ((pekind & PortableExecutableKinds.Required32Bit) == PortableExecutableKinds.Required32Bit)
          this.OS = OSPlatform.x86;
        else if ((pekind & PortableExecutableKinds.PE32Plus) == PortableExecutableKinds.PE32Plus)
          this.OS = OSPlatform.x64;
        else if (pekind == PortableExecutableKinds.ILOnly)
          this.OS = OSPlatform.Any;
        else
          this.OS = OSPlatform.Unknown;


        if (IsCompatiblePe(pekind))
        {
          // It is easy to "accidentally" compile a plug-in as Any CPU when one of
          // its referenced assemblies is actually targetting a specific processor.
          // We could walk through the referenced assemblies and attempt to make a
          // decision based on their target CPU, but it is pretty easy to mix/match
          // the referenced assemblies and dynamically load the correct ones with
          // a little programming effort.
          rc = true;
        }
      }
      else
      {
        // Try to load the plug-in as an unmanaged DLL
		    // Fixed http://dev.mcneel.com/bugtrack/?q=105122
        uint prevMode = UnsafeNativeMethods.SetErrorMode(UnsafeNativeMethods.SEM_FAILCRITICALERRORS);
        IntPtr hModule = UnsafeNativeMethods.LoadLibraryEx(this.m_plugin_path, IntPtr.Zero, UnsafeNativeMethods.DONT_RESOLVE_DLL_REFERENCES);
        UnsafeNativeMethods.SetErrorMode(prevMode);
        // if we were able to load this DLL, the CPU target must match what is currently running
        rc = (hModule != IntPtr.Zero);
      }

      return rc;
    }

    private static Assembly GetReflectionAssembly(string path)
    {
      Assembly rc = null;
      try
      {
        string file = path;
        rc = Assembly.ReflectionOnlyLoadFrom(file);
      }
// ReSharper disable EmptyGeneralCatchClause
      catch
// ReSharper restore EmptyGeneralCatchClause
      {
        // it is ok if we can't load the assembly. The plug-in may be
        // a native C++ DLL
      }
      return rc;
    }

    private static bool IsCompatiblePe(PortableExecutableKinds pekind)
    {
      if (PortableExecutableKinds.ILOnly == pekind)
        return true;
      Assembly thisAss = Assembly.GetExecutingAssembly();
      PortableExecutableKinds thisPeKind;
      ImageFileMachine thisMachine;
      thisAss.ManifestModule.GetPEKind(out thisPeKind, out thisMachine);
      return pekind == thisPeKind;
    }
    #endregion

    public void WriteXml()
    {
      this.WriteXml(GetXmlFileName(this.PluginPath));
    }

    public void ReadXml()
    {
      this.ReadXml(GetXmlFileName(this.PluginPath));
    }

    public override void WriteToDocument(XmlNode element)
    {
      XmlHelper.AppendElement(element, "RhinoSDKVersion", this.RhinoSDKVersion);
      XmlHelper.AppendElement(element, "RhinoSDKServiceRelease", this.RhinoSDKServiceRelease);
      XmlHelper.AppendElement(element, "DotNetSDKVersion", this.DotNetSDKVersion);
      XmlHelper.AppendElement(element, "RhinoCommonSDKVersion", this.RhinoCommonSDKVersion);
      XmlHelper.AppendElement(element, "InstallState", this.InstallState.ToString());
      XmlHelper.AppendElement(element, "Platform", this.OS.ToString());
    }

    public override void ReadFromDocument(XmlNode element)
    {
      RhinoSDKVersion = XmlHelper.SelectSingleNodeInnerText(element, "RhinoSDKVersion", this.m_plugin_path);
      RhinoSDKServiceRelease = XmlHelper.SelectSingleNodeInnerText(element, "RhinoSDKServiceRelease", this.m_plugin_path);
      DotNetSDKVersion = XmlHelper.SelectSingleNodeInnerText(element, "DotNetSDKVersion", this.m_plugin_path);
      RhinoCommonSDKVersion = XmlHelper.SelectSingleNodeInnerText(element, "RhinoCommonSDKVersion", this.m_plugin_path);

      try
      {
        string sInstallState = XmlHelper.SelectSingleNodeInnerText(element, "InstallState", this.m_plugin_path);
        InstallState = (PackageInstallState)Enum.Parse(InstallState.GetType(), sInstallState);
      }
      catch (System.ArgumentException)
      {
        InstallState = PackageInstallState.Unknown;
      }

      try
      {
        string sPlatform = XmlHelper.SelectSingleNodeInnerText(element, "Platform", this.m_plugin_path);
        OS = (OSPlatform)Enum.Parse(OS.GetType(), sPlatform);
      }
      catch
      {
        OS = OSPlatform.Unknown;
      }
    }
  }
}

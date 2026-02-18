using System;
using Elements;
using Protocols;
using Skyline.AppInstaller;
using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.AppPackages;

/// <summary>
/// DataMiner Script Class.
/// </summary>
internal class Script
{
    /// <summary>
    /// The script entry point.
    /// </summary>
    /// <param name="engine">Provides access to the Automation engine.</param>
    /// <param name="context">Provides access to the installation context.</param>
    [AutomationEntryPoint(AutomationEntryPointType.Types.InstallAppPackage)]
    public void Install(IEngine engine, AppInstallContext context)
    {
        try
        {
            engine.GenerateInformation("Starting installation");
            var installer = new AppInstaller(Engine.SLNetRaw, context);
            installer.InstallDefaultContent();

            string setupContentPath = installer.GetSetupContentDirectory();

            var protocolInstaller = new ProtocolInstaller(Engine.SLNetRaw, context, setupContentPath, engine.GenerateInformation);
            protocolInstaller.InstallDefaultContent();

            var elementInstaller = new ElementInstaller(engine);
            elementInstaller.InstallDefaultContent();
        }
        catch (Exception e)
        {
            engine.ExitFail($"Exception encountered during installation: {e}");
        }
    }
}
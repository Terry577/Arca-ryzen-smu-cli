using System.Security.Principal;

namespace ryzen_smu_cli;

internal sealed class WindowsPrivilegeChecker : IPrivilegeChecker
{
    public bool IsWindows => OperatingSystem.IsWindows();

    public bool IsAdministrator
    {
        get
        {
            if (!IsWindows)
            {
                return false;
            }

            try
            {
                using WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}

import java.io.File;

public class LinuxInfo {
    public static void main(String[] args) {
        String[] files = new String[]
                {
                        "tModLoader.exe",
                        "tModLoaderServer.exe",
                        "tModLoaderServer",
                        "Terraria",
                        "tModLoader",
                        "tModLoader-kick",
                        "tModLoader-mono",
                        "I18N.dll",
                        "I18N.West.dll"
                };
        String[] filesToDelete = new String[]
                {
                        "Terraria.exe.config",
                        "MP3Sharp.dll",
                        "Ionic.Zip.Reduced.dll",
                        "Mono.Cecil.dll"
                };
        Installer.tryInstall(files, filesToDelete, getInstallDir(), false);
    }

    private static File getInstallDir() {
        File installDir;

        String xdgHome = System.getenv("XDG_DATA_HOME");
        if (xdgHome != null) {
            installDir = new File(xdgHome + "/Steam/steamapps/common/Terraria");
            if (installDir.isDirectory()) {
                return installDir;
            }
        }

        String home = System.getenv("HOME");
        if (home != null) {
            installDir = new File(home + "/.local/share/Steam/steamapps/common/Terraria");
            if (installDir.isDirectory()) {
                return installDir;
            }

            installDir = new File(home + "/.steam/steam/steamapps/common/Terraria");
            if (installDir.isDirectory()) {
                return installDir;
            }
        }

        return null;
    }
}

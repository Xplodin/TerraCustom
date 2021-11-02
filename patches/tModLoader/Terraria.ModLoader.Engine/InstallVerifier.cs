﻿using ReLogic.OS;
using Steamworks;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using Terraria.Localization;
using Terraria.Social;

namespace Terraria.ModLoader.Engine
{
	internal static class InstallVerifier
	{
		const string ContentDirectory = "Content";

		private static bool? isValid;
		public static bool IsValid => isValid ?? (isValid = InstallCheck()).Value;
		public static bool IsGoG = false;
		public static bool IsSteam = false;

		private static string steamAPIPath;
		private static byte[] steamAPIHash;
		private static byte[] gogHash;
		private static byte[] steamHash;

		static InstallVerifier()
		{
			if (Platform.IsWindows) {
				steamAPIPath = "steam_api.dll";
				steamAPIHash = ToByteArray("7B857C897BC69313E4936DC3DCCE5193");
				gogHash = ToByteArray("a198ff662df47b20ef1d1436c5d80f67"); // Don't forget to update CheckExe in CheckGoG
				steamHash = ToByteArray("3f1fb331d23db8d6cb9584afa4e5e1a9");
			}
			else if (Platform.IsOSX) {
				steamAPIPath = "osx/libsteam_api.dylib";
				steamAPIHash = ToByteArray("4EECD26A0CDF89F90D4FF26ECAD37BE0");
				gogHash = ToByteArray("2d877e9d74d0a382e45d75ff008969d3");
				steamHash = ToByteArray("291b15544aff1e1226ebff119066537d");
			}
			else if (Platform.IsLinux) {
				steamAPIPath = "lib/libsteam_api.so";
				steamAPIHash = ToByteArray("7B74FD4C207D22DB91B4B649A44467F6");
				gogHash = ToByteArray("a37d05f005181bd1bacaa5b82d6fab0d");
				steamHash = ToByteArray("15eb9336a908769ffba388549020a083");
			}
			else {
				string message = Language.GetTextValue("tModLoader.UnknownVerificationOS");
				Logging.tML.Fatal(message);
				Exit(message, string.Empty);
			}
		}

		private static bool HashMatchesFile(string path, byte[] hash)
		{
			using (var md5 = MD5.Create())
			using (var stream = File.OpenRead(path))
				return hash.SequenceEqual(md5.ComputeHash(stream));
		}

		private static byte[] ToByteArray(string hexString)
		{
			byte[] retval = new byte[hexString.Length / 2];
			for (int i = 0; i < hexString.Length; i += 2)
				retval[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
			return retval;
		}
		private static void Exit(string errorMessage, string extraMessage)
		{
			errorMessage += $"\r\n\r\n{extraMessage}";
			Logging.tML.Fatal(errorMessage);
			UI.Interface.MessageBoxShow(errorMessage);
			Environment.Exit(1);
		}

		private static bool InstallCheck()
		{
#if CLIENT
			// Check if the content directory is present which is required
			if (!Directory.Exists(ContentDirectory)) {
				Exit(Language.GetTextValue("tModLoader.ContentFolderNotFoundInstallCheck", ContentDirectory), Language.GetTextValue("tModLoader.DefaultExtraMessage"));
				return false;
			}
#endif
			// Whether the steam_api file exists, indicating we'd have to check steam installation
			if (File.Exists(steamAPIPath))
				return CheckSteam();

			return CheckGoG();
		}

		// Check if steam installation is correct
		private static bool CheckSteam()
		{
			Logging.tML.Info("Checking Steam installation...");
			IsSteam = true;
#if CLIENT
			SocialAPI.LoadSteam();
			string terrariaInstallLocation = Steam.GetSteamTerrariaInstallDir();
			string terrariaContentLocation = Path.Combine(terrariaInstallLocation, ContentDirectory);

			if (!Directory.Exists(terrariaContentLocation)) {
				Exit(Language.GetTextValue("tModLoader.VanillaSteamInstallationNotFound"), Language.GetTextValue("tModLoader.DefaultExtraMessage"));
				return false;
			}
#endif
			if (!HashMatchesFile(steamAPIPath, steamAPIHash)) {
				Process.Start(@"https://terraria.org");
				Exit(Language.GetTextValue("tModLoader.SteamAPIHashMismatch"), string.Empty);
				return false;
			}

			Logging.tML.Info("Steam installation OK.");
			return true;
		}

		// Check if GOG install or manual install is correct
		private static bool CheckGoG()
		{
			Logging.tML.Info("Checking GOG or manual installation...");
			IsGoG = true;

			const string DefaultExe = "Terraria.exe";
			string CheckExe = $"Terraria_1.4.2.3.exe"; // This should match the hashes. {Main.versionNumber}
			string vanillaPath = File.Exists(CheckExe) ? CheckExe : DefaultExe;

			// If .exe not present, check Terraria directory (Side-by-Side Manual Install)
			if (!File.Exists(vanillaPath)) {
				vanillaPath = Path.Combine("..", "Terraria");
#if MAC
				// GOG installs to /Applications/Terraria.app, Steam installs to /Applications/Terraria/Terraria.app
				// working directory is /Applications/tModLoader/tModLoader.app/Contents/MacOS/ for steam manual installs
				// working directory is /Applications/tModLoader.app/Contents/MacOS/ for GOG installs
				// Vanilla .exe files are in /Contents/Resources/, not /Contents/MacOS/
				if (Directory.Exists("../../../../Terraria/Terraria.app/")) {
					vanillaPath = "../../../../Terraria/Terraria.app/Contents/Resources/";
					Logging.tML.Info($"Mac installation location found at {vanillaPath}, assuming Steam manual install");
				}
				else if (Directory.Exists("../../../Terraria.app/")) {
					vanillaPath = "../../../Terraria.app/Contents/Resources/";
					Logging.tML.Info($"Mac installation location found at {vanillaPath}, assuming GOG manual install");
				}
				else {
					Logging.tML.Info($"Mac installation location not found.");
				}
#endif
				string defaultExe = Path.Combine(vanillaPath, DefaultExe);
				string checkExe = Path.Combine(vanillaPath, CheckExe);
				vanillaPath = File.Exists(checkExe) ? checkExe : defaultExe;
			}
			// If .exe not present check parent directory (Nested Manual Install)
			if (!File.Exists(vanillaPath)) {
				string defaultExe = Path.Combine("..", DefaultExe);
				string checkExe = Path.Combine("..", CheckExe);
				vanillaPath = File.Exists(checkExe) ? checkExe : defaultExe;
			}

			if (!File.Exists(vanillaPath)) {
#if SERVER
				return false;
#else
				Exit(Language.GetTextValue("tModLoader.VanillaGOGNotFound", vanillaPath, CheckExe), string.Empty);
				return false;
#endif
			}

			if (!HashMatchesFile(vanillaPath, gogHash)) {
				Exit(Language.GetTextValue("tModLoader.GOGHashMismatch", vanillaPath), string.Empty);
				return false;
			}

			if (Path.GetFileName(vanillaPath) != CheckExe) {
				string pathToCheckExe = Path.Combine(Path.GetDirectoryName(vanillaPath), CheckExe);
				Logging.tML.Info($"Backing up {Path.GetFileName(vanillaPath)} to {CheckExe}");
				File.Copy(vanillaPath, pathToCheckExe);
			}

			Logging.tML.Info("GOG or manual installation OK.");
			return true;
		}
	}
}

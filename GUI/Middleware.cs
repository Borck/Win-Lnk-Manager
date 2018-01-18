using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Shell32;

namespace GUI {
    public static class Middleware {
        private const string EXT_SHORTCUT = ".lnk";
        private const string FILE_PATTERN_SHORTCUT = "*"+EXT_SHORTCUT;
        private const string FILE_PATTERN_APP = "*.exe";

        public const string ShortcutsSubfolder = "Portables";

        public static readonly string PathStartmenuMaschine = @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs";
        public static readonly  string PathStartmenuUser = Environment.ExpandEnvironmentVariables(@"%appdata%\Microsoft\Windows\Start Menu\Programs");

        public static IDictionary<string, ShortcutFileInfo> GetStartmenuShortcuts() {
            var shortcutsMaschine = Directory.GetFiles( PathStartmenuMaschine, FILE_PATTERN_SHORTCUT, SearchOption.AllDirectories );
            var shortcutsUser = Directory.GetFiles( PathStartmenuUser, FILE_PATTERN_SHORTCUT, SearchOption.AllDirectories );

            var aReturn = new Dictionary<string, ShortcutFileInfo>( shortcutsMaschine.Length + shortcutsUser.Length );
            foreach (var shortcut in shortcutsMaschine.Concat( shortcutsUser )) {
              try {
                var linkFileInfo = ShortcutFileInfo.Read(shortcut);
                aReturn.Add(shortcut, linkFileInfo);
              } catch (UnauthorizedAccessException e) {
                Console.Error.WriteLine($"Can read target path from lnk-file: {shortcut}", e);
              } catch (COMException e) {
                Console.Error.WriteLine( $"Can read target path from lnk-file: {shortcut}", e );
              }
            }
            return aReturn;
        }


        public static void DeleteFiles(IEnumerable<string> files) {
            foreach (var file in files)
                File.Delete( file );
        }


        public static IEnumerable<string> GetApplicationFiles(string rootDirectory) {
            if (Directory.Exists( rootDirectory )) {
                return Directory.GetFiles( rootDirectory, FILE_PATTERN_APP, SearchOption.AllDirectories );
            }
            throw new IOException( "Folder does not exist: " + rootDirectory );

        }


        public static void SaveShortcuts(IEnumerable<ShortcutFileInfo> shortcuts, string directory) {
            Directory.CreateDirectory( directory );
            foreach (var shortcut in shortcuts) {
                var filename = Path.Combine( directory, Path.GetFileNameWithoutExtension( shortcut.Target ) + EXT_SHORTCUT );
                shortcut.Save( filename );
            }
        }
    }
}
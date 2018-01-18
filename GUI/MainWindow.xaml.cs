using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CB.Tools.Properties;
using File = IWshRuntimeLibrary.File;

namespace GUI {
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow {
        private static Settings Settings => Settings.Default;

        private string ScanDirectory => ScanCombo.Text;

        public MainWindow() {
          InitializeComponent();
          ScanCombo.ItemsSource = Settings.ScanDirectories;
          ScanCombo.SelectedIndex = ScanCombo.Items.IsEmpty ? -1 : 0;
          ShowPage( Page.MAIN );
        }

        private static void SaveSettingsScanDirectories(string addedScanDirectory) {
            var scanDirectories = Settings.ScanDirectories;

            var indexOf = scanDirectories.IndexOf( addedScanDirectory );
            if (indexOf >= 0) // remove if exists
                scanDirectories.RemoveAt( indexOf );
            else {
                // remove last item if to much items
                var numScanDirectories = Settings.MaxNumScanDirectories;
                if (scanDirectories.Count > Settings.MaxNumScanDirectories)
                    scanDirectories.RemoveAt( numScanDirectories - 1 );
            }

            scanDirectories.Insert( 0, addedScanDirectory );
            Settings.ScanDirectories = scanDirectories;
            Settings.Save();
        }

        private void ScanBtn_Click(object sender, RoutedEventArgs e) {
            ScanAndUpdate();
        }

        private void ScanAndUpdate() {
          var targetRootDirectory = ScanDirectory;
          if (targetRootDirectory == "")
              return;

          IEnumerable<string> apps;
          try {
            apps = Middleware.GetApplicationFiles(targetRootDirectory);

          } catch (IOException e) {
            HandleException(e);
            return;
          } catch (UnauthorizedAccessException e) {
            HandleException(e);
            return;
          }

          var shortcutsForTargetRoot = GetShortcutsForTargetRoot( targetRootDirectory );

            var shortcutsTargets = GetTargets( shortcutsForTargetRoot.Values );
            var appsFiltered = FilterApps( apps, shortcutsTargets );

            var scanDirectory = ScanDirectory;

            ShortcutCandidatesBox.ItemsSource = appsFiltered
              .Select( app => new ShortcutCandidatesListBoxItem( app, scanDirectory ) )
              .ToList();
            ShortcutsBox.ItemsSource = ToListBoxItems( shortcutsForTargetRoot, scanDirectory );

            SaveSettingsScanDirectories( scanDirectory );
        }

      private void HandleException(Exception e) {
        MessageBox.Show(this, e.Message, "ERROR");
        Console.Error.WriteLine(e.ToString());
      }

    private void ScanCombo_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
      if (e.Key == Key.Enter) {
        ScanAndUpdate();
      }
    }
    private static ShortcutListBoxItem[] ToListBoxItems(
        IDictionary<string, ShortcutFileInfo> shortcutInfos,
        string scanDirectory) {
            return shortcutInfos
              .AsEnumerable()
              .Select( shortcutEntry => new ShortcutListBoxItem( shortcutEntry.Key, shortcutEntry.Value, scanDirectory ) )
              .OrderBy( shortcutItem => shortcutItem.Item2.Target )
              .ToArray();
        }

        private static ISet<string> GetTargets(IEnumerable<ShortcutFileInfo> linkInfos) {
            return new SortedSet<string>( linkInfos
              .Select( linkInfo => linkInfo.Target ) );
        }

        private static IEnumerable<string> FilterApps(IEnumerable<string> apps, ISet<string> shortcutsTargets) {
            return apps.Where( app => !shortcutsTargets.Contains( app ) );
        }

        private static IDictionary<string, ShortcutFileInfo> GetShortcutsForTargetRoot(string targetRootDirectory) {
            return Middleware
            .GetStartmenuShortcuts()
            .AsEnumerable()
            .Where( shortcutEntry => shortcutEntry.Value.Target.StartsWith( targetRootDirectory ) )
            .ToDictionary(
              shortcutEntry => shortcutEntry.Key,
              shortcutEntry => shortcutEntry.Value );
        }


        private void AddShortcutsToUser_Click(object sender, RoutedEventArgs e) {
          AddShortcutsToUserStartmenuAndBackToMain();
        }

        private void AddShortcutsToComputer_Click(object sender, RoutedEventArgs e) {
          AddShortcutsToMaschineStartmenuAndBackToMain();
        }
    private void ShortcutCandidatesBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Enter) {
        if (Keyboard.IsKeyDown(Key.LeftShift)|| Keyboard.IsKeyDown(Key.RightShift)) {
          AddShortcutsToUserStartmenuAndBackToMain();
        } else {
          AddShortcutsToMaschineStartmenuAndBackToMain();
        }
      }
    }
    private void AddShortcutsAndBackToMain(string startmenuPath) {
            SaveShortcuts( startmenuPath );
            ScanAndUpdate(); //TODO meanwhile scan path could be change  
            ShowPage( Page.MAIN );
        }


    private void AddShortcutsToUserStartmenuAndBackToMain()
      => AddShortcutsAndBackToMain( Middleware.PathStartmenuUser );
    private void AddShortcutsToMaschineStartmenuAndBackToMain()
      => AddShortcutsAndBackToMain( Middleware.PathStartmenuMaschine );

    private void ShortcutsBox_KeyUp(object sender, KeyEventArgs e) {
      if (e.Key == Key.Delete) {
        RemoveShortcuts();
      }
    }


    private void ShortcutRemoveBtn_Click(object sender, RoutedEventArgs e) {
      RemoveShortcuts();
        }

      private void RemoveShortcuts() {
        var selectedShortcutFiles = ShortcutsBox
        .SelectedItems
        .Cast<ShortcutListBoxItem>()
        .Select( shortcut => shortcut.Item1 );
        Middleware.DeleteFiles( selectedShortcutFiles );
        ScanAndUpdate();
    }


    private void SaveShortcuts(string startmenuDirectory) {
            var directory = Path.Combine(
              startmenuDirectory,
              Middleware.ShortcutsSubfolder );
            var selectedShortcuts = ShortcutCandidatesBox
            .SelectedItems
            .Cast<ShortcutCandidatesListBoxItem>()
            .Select( target => new ShortcutFileInfo( target.Target ) );
            Middleware.SaveShortcuts(
              selectedShortcuts,
              directory );
        }

        private void ShowPage(Page page) {
            PageControl.SelectedIndex = (int)page;
        }


        private static string ToRelativePath(string filePath, string referencePath) {
            if (filePath.StartsWith( referencePath )) {
                return filePath.Remove( 0, referencePath.Length );
            }
            throw new ArgumentException( $"filePath is not in referencePath: filepath=\"{filePath}\" referencePath=\"{referencePath}\"" );
        }


        private enum Page {
            MAIN
        }
        private class ShortcutCandidatesListBoxItem {
            public readonly string Target;
            private readonly string _scanDirectory;

            public ShortcutCandidatesListBoxItem(string target, string scanDirectory) {
                Target = target;
                _scanDirectory = scanDirectory;
            }

            public override string ToString() {
                return ToRelativePath( Target, _scanDirectory );
            }
        }


        private class ShortcutListBoxItem : Tuple<string, ShortcutFileInfo> {
            private readonly string _scanDirectory;

            public ShortcutListBoxItem(string shortcutFile, ShortcutFileInfo shortcutInfo, string scanDirectory)
            : base( shortcutFile, shortcutInfo ) {
                _scanDirectory = scanDirectory;
            }

            public override string ToString() {
                return ToRelativePath( Item2.Target, _scanDirectory );
            }
        }

  }
}

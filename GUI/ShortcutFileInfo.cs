using System;
using System.Diagnostics;
using System.IO;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace GUI {
  public class ShortcutFileInfo {
    public readonly string Target;

    public ShortcutFileInfo(string target) {
      Target = target;
    }


    public override string ToString() {
      return Target;
    }

    public static ShortcutFileInfo Read(string filename) {
      var shortcutTarget = GetLnkTarget( filename );
      return new ShortcutFileInfo( shortcutTarget );
    }


    public bool Save(string filename) {
      var wsh = new WshShell();
      var shortcut = wsh.CreateShortcut( filename ) as IWshShortcut;
      if (shortcut == null)
        return false;

      shortcut.Arguments = "";
      shortcut.TargetPath = Target;
      shortcut.Description = "";
      shortcut.WorkingDirectory = Path.GetDirectoryName( Target );
      shortcut.Save();
      return true;
    }

    private static string GetLnkTarget(string lnkPath) {
      var shl = new Shell32.Shell();         // Move this to class scope
      lnkPath = Path.GetFullPath( lnkPath );
      var dir = shl.NameSpace( Path.GetDirectoryName( lnkPath ) );
      var itm = dir.Items().Item( Path.GetFileName( lnkPath ) );
      var lnk = (Shell32.ShellLinkObject)itm.GetLink;
      return lnk.Target.Path;
    }

    private static string GetShortcutTarget(string filename) {
      try {
        var fileStream = File.Open( filename, FileMode.Open, FileAccess.Read );
        using (var fileReader = new BinaryReader( fileStream )) {
          fileStream.Seek( 0x14, SeekOrigin.Begin );     // Seek to flags
          var flags = fileReader.ReadUInt32();        // Read flags
          if (( flags & 1 ) == 1) {                      // Bit 1 set means we have to
                                                         // skip the shell item ID list
            fileStream.Seek( 0x4c, SeekOrigin.Begin ); // Seek to the end of the header
            uint offset = fileReader.ReadUInt16();   // Read the length of the Shell item ID list
            fileStream.Seek( offset, SeekOrigin.Current ); // Seek past it (to the file locator info)
          }

          var fileInfoStartsAt = fileStream.Position; // Store the offset where the file info
                                                      // structure begins
          var totalStructLength = fileReader.ReadUInt32(); // read the length of the whole struct
          fileStream.Seek( 0xc, SeekOrigin.Current ); // seek to offset to base pathname
          var fileOffset = fileReader.ReadUInt32(); // read offset to base pathname
                                                    // the offset is from the beginning of the file info struct (fileInfoStartsAt)
          fileStream.Seek( ( fileInfoStartsAt + fileOffset ), SeekOrigin.Begin ); // Seek to beginning of
                                                                                  // base pathname (target)
          var pathLength = ( totalStructLength + fileInfoStartsAt ) - fileStream.Position - 2; // read
                                                                                               // the base pathname. I don't need the 2 terminating nulls.
          if (pathLength < 0)
            return string.Empty;

          var linkTarget = fileReader.ReadChars( (int)pathLength ); // should be unicode safe
          var link = new string( linkTarget );

          var begin = link.IndexOf( "\0\0", StringComparison.Ordinal );
          if (begin <= -1)
            return link;

          var end = link.IndexOf( "\\\\", begin + 2, StringComparison.Ordinal ) + 2;
          end = link.IndexOf( '\0', end ) + 1;

          var firstPart = link.Substring( 0, begin );
          var secondPart = link.Substring( end );

          return firstPart + secondPart;
        }
      } catch (EndOfStreamException e) {
        Debug.WriteLine( "Could not read target from shortcut \"" + filename + "\".", e );
        return "";
      }
    }
  }
}

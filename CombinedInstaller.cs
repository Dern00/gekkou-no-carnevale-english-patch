using System;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;

[assembly: AssemblyTitle("Gekkou no Carnevale English Patch")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

internal static class CombinedInstaller
{
    internal const string GameExe = "GekkounoCarnevale.exe";
    private const string Backup = "Before Carnevale combined patch";
    private const string Receipt = "original-sha256.txt";
    private const string UpdaterName = "Carnevale101.EXE";
    private const string UpdaterUrl = "https://data.nitroplus.co.jp/dl/carnival/Carnevale101.EXE";
    private const string UpdaterHash = "c2513e1808511997097acef11934e49d616321b6bc853a0c20af89fa68ae0938";
    private const int CabinetOffset = 30752;
    private const int UpdaterSize = 7490544;
    private const string FirstEnglishOverlay = "09dbda16e25dc87319c89724f6d772c5d217add8a17896a5d06a0f1480636e0c";
    private const string TestedEnglishOverlay = "d7a77b77250be50a7063058fc542da6d7c7d36ecbc5632c4b25072ee15fb1db5";
    private const string SubtitleMovie = "e2fdb2ccbd0c10493ba59ff22381c4e4e9867077631b9594e4f2933b3fbb2a27";
    private const int OpeningIndex = 5;
    private static readonly string[] Files = {
        "0cg.pak", "script.pak", "system.pak", "0script.pak", "system.dll", "mpeg/OP.mpg"
    };
    private static readonly string[] Stock = {
        "-",
        "ad145835208e3f547537c27fb221fd6fb52a96d7d1f02a2a69a55057e73b8cfb",
        "50925f9ad4f00253c5b99bbded02076693b948159670d90a90fb4ddcab3cd072",
        "-",
        "ae6b5b463734f6eeefc1c61f05b44354ce52140578a7b151a962168dd354a158",
        "2a5ceb8c09622e2ad43f5e2e3a906831815641e6e9fcef0e928fea0d0e19170c"
    };
    private static readonly string[] OlderEngines = {
        "aba31f2fa285a0ec7dfbc7dcecbb9e2c4ba0032f83645ce97738ec97f073e951",
        "1648d339770e9675e01a2e781d33d6e0ee615da1f2ad8f4f5432d658537e414c",
        "fff97b3fea508da5ab7c407e13234e1a4cfdd7174ccffaa62120b92c6f1538a9"
    };

    private sealed class UpdaterRequiredException : IOException
    {
        internal UpdaterRequiredException(Exception cause) : base(
            "The official 1.01 download is unavailable. Download Carnevale101.EXE from Nitroplus, then select it here.", cause)
        { }
    }

    // Both the staged files and their destinations are on the selected game volume.
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileEx(string from, string to, int flags);

    private static void Replace(string from, string to)
    {
        if (!MoveFileEx(from, to, 1 | 8))
            throw new IOException("Could not replace " + to, new Win32Exception(Marshal.GetLastWin32Error()));
    }

    private static string Hash(string file)
    {
        if (!File.Exists(file)) return "-";
        using (var stream = File.OpenRead(file))
            return Hash(stream);
    }

    private static string Hash(Stream stream)
    {
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    private static void Check(string file, string expected)
    {
        if (Hash(file) != expected)
            throw new InvalidDataException("File verification failed for " + file);
    }

    private static void RejectLink(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) &&
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("This file or folder links to another location: " + path);
    }

    private static string[] ReadHashes(TextReader reader, bool allowMissing)
    {
        string[] values = new string[Files.Length];
        for (int i = 0; i < Files.Length; i++)
        {
            string value = reader.ReadLine();
            if (!(allowMissing && value == "-") &&
                (value == null || value.Length != 64 || !Array.TrueForAll(value.ToCharArray(),
                    c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'))))
                throw new InvalidDataException("Invalid checksum list.");
            values[i] = value;
        }
        if (reader.ReadLine() != null) throw new InvalidDataException("Unexpected checksum entries.");
        return values;
    }

    private static void Copy(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        using (var input = File.OpenRead(source))
        using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            input.CopyTo(output);
    }

    private static void PrepareCabinet(string stage, string updater)
    {
        if (updater == null)
        {
            string besideInstaller = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, UpdaterName);
            if (File.Exists(besideInstaller)) updater = besideInstaller;
        }
        if (updater == null)
        {
            updater = Path.Combine(stage, UpdaterName);
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var request = (HttpWebRequest)WebRequest.Create(UpdaterUrl);
                request.AllowAutoRedirect = false;
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;
                using (var response = (HttpWebResponse)request.GetResponse())
                using (var input = response.GetResponseStream())
                using (var output = File.Create(updater))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new IOException("Unexpected response from the Nitroplus download.");
                    byte[] buffer = new byte[65536];
                    int size = 0;
                    for (int count; (count = input.Read(buffer, 0, buffer.Length)) != 0; )
                    {
                        size += count;
                        if (size > UpdaterSize) throw new IOException("Unexpected Nitroplus download length.");
                        output.Write(buffer, 0, count);
                    }
                }
                Check(updater, UpdaterHash);
            }
            catch (WebException error) { throw new UpdaterRequiredException(error); }
            catch (IOException error) { throw new UpdaterRequiredException(error); }
        }
        using (var input = File.OpenRead(updater))
        {
            if (input.Length != UpdaterSize || Hash(input) != UpdaterHash)
                throw new InvalidDataException("This file does not match the official Carnevale 1.01 updater. Get it from Nitroplus.");
            input.Position = CabinetOffset;
            using (var cabinet = File.Create(Path.Combine(stage, "update.cab")))
                input.CopyTo(cabinet);
        }
    }

    private static void ExtractOfficialFile(string stage, string name, string updater, string destination)
    {
        string cabinet = Path.Combine(stage, "update.cab");
        if (!File.Exists(cabinet)) PrepareCabinet(stage, updater);
        // Extract each verified archive member using Windows' own CAB utility.
        // The updater executable itself is never run.
        using (var expand = Process.Start(new ProcessStartInfo {
            FileName = Path.Combine(Environment.SystemDirectory, "expand.exe"),
            WorkingDirectory = stage,
            Arguments = "update.cab -F:" + name + " .",
            CreateNoWindow = true,
            UseShellExecute = false
        }))
        {
            if (expand == null || !expand.WaitForExit(30000))
            {
                if (expand != null) expand.Kill();
                throw new IOException("Windows timed out extracting " + name + " from the official updater.");
            }
            if (expand.ExitCode != 0) throw new IOException("Windows could not extract " + name + " from the official updater.");
        }
        File.Move(Path.Combine(stage, name), destination);
    }

    private static void ValidateGame(string root)
    {
        if (!Directory.Exists(root) || !File.Exists(Path.Combine(root, GameExe)))
            throw new IOException("Select the game folder containing " + GameExe + ".");
        foreach (string name in new[] { "cg.pak", "data.pak", "voice.pak", "sound.pak",
                                         "script.pak", "system.pak", "system.dll" })
            if (!File.Exists(Path.Combine(root, name)))
                throw new IOException("The selected game folder is missing " + name + ".");
        for (int i = 0; i < OpeningIndex; i++)
        {
            string path = Path.Combine(root, Files[i]);
            RejectLink(path);
            if (Directory.Exists(path)) throw new IOException("A folder occupies " + path);
        }
    }

    private static void EnsureGameClosed(string root)
    {
        string executable = Path.Combine(root, GameExe);
        foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(GameExe)))
        using (process)
        {
            string running;
            try { running = process.MainModule.FileName; }
            catch (Win32Exception) { continue; }
            catch (InvalidOperationException) { continue; }
            if (string.Equals(running, executable, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Close Gekkou no Carnevale in the selected folder before continuing.");
        }
    }

    private static string[] GetOriginals(string root, string backup, string stage, bool restore)
    {
        RejectLink(backup);
        if (!Directory.Exists(backup))
        {
            if (restore) throw new IOException("This folder has no backup from this installer.");
            string pending = Path.Combine(stage, "originals");
            Directory.CreateDirectory(pending);
            var original = new string[Files.Length];
            for (int i = 0; i < Files.Length; i++)
            {
                // Reserve the sixth receipt line for local pre-release backups.
                if (i == OpeningIndex) { original[i] = "-"; continue; }
                string file = Path.Combine(root, Files[i]);
                original[i] = Hash(file);
                if (original[i] == "-") continue;
                string saved = Path.Combine(pending, Files[i]);
                Copy(file, saved);
                Check(saved, original[i]);
            }
            File.WriteAllLines(Path.Combine(pending, Receipt), original);
            return original;
        }
        RejectLink(Path.Combine(backup, Receipt));
        using (var reader = File.OpenText(Path.Combine(backup, Receipt)))
        {
            string[] original = ReadHashes(reader, true);
            for (int i = 0; i < Files.Length; i++)
            {
                if (i == OpeningIndex)
                {
                    if (original[i] == "-") continue;
                    RejectLink(Path.Combine(backup, "mpeg"));
                }
                string file = Path.Combine(backup, Files[i]);
                RejectLink(file);
                Check(file, original[i]);
            }
            return original;
        }
    }

    private static bool IsKnownSource(int i, string actual, string patched)
    {
        if (actual == Stock[i] || actual == patched) return true;
        if (i == 3) return actual == FirstEnglishOverlay || actual == TestedEnglishOverlay;
        if (i == 4) return Array.IndexOf(OlderEngines, actual) >= 0;
        if (i == OpeningIndex) return actual == SubtitleMovie;
        return false;
    }

    private static void StagePatch(string root, string backup, string stage, string updater,
                                   ZipArchive archive, int i, string next)
    {
        if (i < 3)
        {
            ExtractOfficialFile(stage, Files[i], updater, next);
            return;
        }
        if (i == 4)
        {
            string source = Path.Combine(root, Files[i]);
            string entryName = "engine/" + Hash(source) + ".delta";
            if (archive.GetEntry(entryName) == null)
            {
                source = Path.Combine(backup, Files[i]);
                entryName = "engine/" + Hash(source) + ".delta";
            }
            var entry = archive.GetEntry(entryName);
            if (entry == null) throw new IOException("The installer needs a supported original system.dll.");
            using (var delta = entry.Open()) BinaryDelta.Apply(source, delta, next);
            return;
        }
        using (var input = archive.GetEntry(Files[i]).Open())
        using (var output = File.Create(next)) input.CopyTo(output);
    }

    private static void Apply(string root, bool restore, string updater)
    {
        root = Path.GetFullPath(root);
        ValidateGame(root);
        EnsureGameClosed(root);
        string backup = Path.Combine(root, Backup);
        string stage = Path.Combine(root, ".carnevale-stage-" + Guid.NewGuid().ToString("N"));
        string lockPath = Path.Combine(root, ".carnevale-install.lock");
        RejectLink(lockPath);
        using (var guard = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.Write,
                   FileShare.None, 1, FileOptions.DeleteOnClose))
        using (var data = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
        using (var archive = new ZipArchive(data, ZipArchiveMode.Read))
        {
            string[] patched;
            using (var reader = new StreamReader(archive.GetEntry("sha256.txt").Open()))
                patched = ReadHashes(reader, true);
            Directory.CreateDirectory(stage);
            bool keepStage = false;
            try
            {
                bool firstInstall = !Directory.Exists(backup);
                string[] original = GetOriginals(root, backup, stage, restore);
                string[] desired = (string[])(restore ? original : patched).Clone();
                string[] before = new string[Files.Length];
                bool[] changed = new bool[Files.Length];
                int count = OpeningIndex;
                if (restore && original[OpeningIndex] != "-")
                {
                    RejectLink(Path.Combine(root, "mpeg"));
                    RejectLink(Path.Combine(root, Files[OpeningIndex]));
                    count = Files.Length;
                }
                for (int i = 0; i < count; i++)
                {
                    string file = Path.Combine(root, Files[i]);
                    before[i] = Hash(file);
                    if (before[i] != desired[i] && !IsKnownSource(i, before[i], patched[i]))
                        throw new IOException("The file has changed since it was backed up: " + file);
                }
                for (int i = 0; i < count; i++)
                {
                    if (before[i] == desired[i]) continue;
                    string file = Path.Combine(root, Files[i]);
                    if (before[i] != "-")
                    {
                        string undo = Path.Combine(stage, "undo" + i);
                        Copy(file, undo);
                        Check(undo, before[i]);
                    }
                    if (desired[i] == "-") continue;
                    string next = Path.Combine(stage, "next" + i);
                    if (restore) Copy(Path.Combine(backup, Files[i]), next);
                    else StagePatch(root, backup, stage, updater, archive, i, next);
                    Check(next, desired[i]);
                }
                try
                {
                    EnsureGameClosed(root);
                    for (int i = 0; i < count; i++)
                        Check(Path.Combine(root, Files[i]), before[i]);
                    if (firstInstall) Directory.Move(Path.Combine(stage, "originals"), backup);
                    for (int i = 0; i < count; i++)
                    {
                        if (before[i] == desired[i]) continue;
                        string file = Path.Combine(root, Files[i]);
                        Check(file, before[i]);
                        if (desired[i] == "-") File.Delete(file);
                        else Replace(Path.Combine(stage, "next" + i), file);
                        changed[i] = true;
                    }
                    for (int i = 0; i < count; i++) Check(Path.Combine(root, Files[i]), desired[i]);
                }
                catch (Exception failure)
                {
                    Exception rollbackError = null;
                    for (int i = count - 1; i >= 0; i--)
                    {
                        if (!changed[i]) continue;
                        string file = Path.Combine(root, Files[i]);
                        try
                        {
                            if (before[i] == "-") File.Delete(file);
                            else Replace(Path.Combine(stage, "undo" + i), file);
                            Check(file, before[i]);
                        }
                        catch (Exception error) { rollbackError = error; }
                    }
                    if (rollbackError != null)
                    {
                        keepStage = true;
                        throw new IOException("Recovery files were retained in " + stage + ". " +
                            failure.Message, rollbackError);
                    }
                    throw;
                }
            }
            finally { if (!keepStage) Directory.Delete(stage, true); }
        }
    }

    [STAThread]
    private static int Main(string[] args)
    {
        // The window and command-line paths invoke the same installation transaction.
        if (args.Length > 0)
        {
            bool install = args[0] == "/install", restore = args[0] == "/restore";
            if (!(install && (args.Length == 2 || args.Length == 3)) && !(restore && args.Length == 2)) return 2;
            try { Apply(args[1], restore, args.Length == 3 ? args[2] : null); return 0; }
            catch (Exception error) { Console.Error.WriteLine(error.GetBaseException().Message); return 1; }
        }
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new InstallerWindow());
        return 0;
    }

    private sealed class InstallerWindow : Form
    {
        private readonly TextBox folder = new TextBox();
        private readonly Label status = new Label();
        private readonly Button browse = new Button(), install = new Button(), restore = new Button();
        private bool busy;

        internal InstallerWindow()
        {
            Text = "Gekkou no Carnevale English Patch v1.0";
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(550, 182);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            var label = new Label { Text = "Game folder", AutoSize = true, Location = new Point(16, 18) };
            folder.SetBounds(16, 42, 420, 26);
            browse.Text = "Browse…"; browse.SetBounds(444, 40, 90, 27);
            browse.Click += delegate {
                using (var picker = new OpenFileDialog { Title = "Select the game executable",
                    Filter = "Gekkou no Carnevale|" + GameExe, FileName = GameExe })
                {
                    if (Directory.Exists(folder.Text)) picker.InitialDirectory = folder.Text;
                    if (picker.ShowDialog(this) == DialogResult.OK)
                        folder.Text = Path.GetDirectoryName(picker.FileName);
                }
            };
            string here = AppDomain.CurrentDomain.BaseDirectory;
            if (File.Exists(Path.Combine(here, GameExe))) folder.Text = here;
            status.SetBounds(16, 79, 520, 48);
            status.Text = "Installs the English patch and gets the official 1.01 update if needed. Close the game before installing.";
            install.Text = "Install patch"; install.SetBounds(422, 132, 112, 33);
            restore.Text = "Restore original files"; restore.SetBounds(258, 132, 155, 33);
            install.Click += delegate { Run(false); };
            restore.Click += delegate { Run(true); };
            AcceptButton = install;
            Controls.AddRange(new Control[] { label, folder, browse, status, restore, install });
            FormClosing += delegate(object sender, FormClosingEventArgs e) { e.Cancel = busy; };
        }

        private async void Run(bool undo)
        {
            string path = folder.Text.Trim().Trim('"');
            if (path.Length == 0) { browse.PerformClick(); path = folder.Text.Trim(); }
            if (path.Length == 0) return;
            if (undo && MessageBox.Show(this, "Restore the files saved before the first installation?",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            busy = true;
            folder.Enabled = browse.Enabled = install.Enabled = restore.Enabled = false;
            status.Text = undo ? "Restoring and verifying game files…" : "Backing up, installing and verifying game files…";
            try
            {
                string localUpdater = null;
                try
                {
                    await System.Threading.Tasks.Task.Run(() => Apply(path, undo, null));
                }
                catch (UpdaterRequiredException error)
                {
                    status.Text = error.Message;
                    using (var picker = new OpenFileDialog {
                        Title = "Select the official Nitroplus 1.01 update",
                        Filter = "Official Carnevale 1.01 update|Carnevale101.EXE|Executable files|*.exe",
                        FileName = UpdaterName
                    })
                    {
                        if (picker.ShowDialog(this) != DialogResult.OK) return;
                        localUpdater = picker.FileName;
                    }
                }
                if (localUpdater != null)
                {
                    status.Text = "Verifying the official update and installing the English patch…";
                    await System.Threading.Tasks.Task.Run(() => Apply(path, false, localUpdater));
                }
                status.Text = undo ? "Original game files restored." : "Installation complete. You can start the game.";
            }
            catch (Exception error)
            {
                status.Text = "The operation could not be completed.";
                MessageBox.Show(this, error.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { busy = false; folder.Enabled = browse.Enabled = install.Enabled = restore.Enabled = true; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MonkeModManager.Core;

namespace MonkeModManager
{
    public class FormMain : Form
    {
        private const string GuideUrl = "https://gorillatagmodding.burrito.software/";

        private readonly Settings settings = Settings.Load();
        private ModCatalog catalog;
        private string gameDir;
        private bool busy;
        private bool suppressCheckEvents;

        // UI
        private TextBox textBoxDirectory;
        private Button buttonBrowse;
        private TabControl tabs;
        private ListView listViewMods;
        private Label labelBepInEx;
        private CheckBox checkBoxAutoBepInEx;
        private Button buttonModInfo;
        private Button buttonInstall;
        private ToolStripStatusLabel statusLabel;
        private ToolStripProgressBar progressBar;
        private readonly List<Control> gameDependentControls = new List<Control>();

        public FormMain()
        {
            BuildUi();
            Shown += async (s, e) => await StartupAsync();
        }

        #region UI construction

        private void BuildUi()
        {
            Text = "Monke Mod Manager";
            Font = new Font("Segoe UI", 9f);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(720, 520);
            MinimumSize = new Size(600, 420);
            StartPosition = FormStartPosition.CenterScreen;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(8) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Game folder row
            var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, RowCount = 1 };
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathRow.Controls.Add(new Label { Text = "Gorilla Tag folder:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            textBoxDirectory = new TextBox { ReadOnly = true, Dock = DockStyle.Fill };
            pathRow.Controls.Add(textBoxDirectory, 1, 0);
            buttonBrowse = new Button { Text = "Browse…", AutoSize = true };
            buttonBrowse.Click += (s, e) => BrowseForGame();
            pathRow.Controls.Add(buttonBrowse, 2, 0);
            root.Controls.Add(pathRow, 0, 0);

            // Tabs
            tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildModsTab());
            tabs.TabPages.Add(BuildUtilitiesTab());
            root.Controls.Add(tabs, 0, 1);

            // BepInEx row
            var bepRow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            labelBepInEx = new Label { AutoSize = true, Text = "BepInEx: checking…", Margin = new Padding(3, 6, 12, 3) };
            checkBoxAutoBepInEx = new CheckBox
            {
                AutoSize = true,
                Text = "Automatically keep BepInEx up to date",
                Checked = settings.AutoUpdateBepInEx,
                Margin = new Padding(3, 4, 3, 3)
            };
            checkBoxAutoBepInEx.CheckedChanged += (s, e) =>
            {
                settings.AutoUpdateBepInEx = checkBoxAutoBepInEx.Checked;
                settings.Save();
            };
            bepRow.Controls.Add(labelBepInEx);
            bepRow.Controls.Add(checkBoxAutoBepInEx);
            root.Controls.Add(bepRow, 0, 2);

            // Bottom buttons
            var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, RowCount = 1 };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonModInfo = new Button { Text = "View Mod Info", AutoSize = true, Enabled = false };
            buttonModInfo.Click += (s, e) => OpenSelectedModPage();
            buttonInstall = new Button { Text = "Install / Update", AutoSize = true, Enabled = false, Font = new Font(Font, FontStyle.Bold) };
            buttonInstall.Click += async (s, e) => await InstallSelectedAsync();
            buttons.Controls.Add(buttonModInfo, 0, 0);
            buttons.Controls.Add(buttonInstall, 2, 0);
            root.Controls.Add(buttons, 0, 3);

            // Status bar
            var statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Starting…") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            progressBar = new ToolStripProgressBar { Visible = false, Style = ProgressBarStyle.Marquee };
            statusStrip.Items.Add(statusLabel);
            statusStrip.Items.Add(progressBar);
            statusStrip.Items.Add(new ToolStripStatusLabel("v" + AppInfo.VersionString));

            Controls.Add(root);
            Controls.Add(statusStrip);
            AcceptButton = buttonInstall;
        }

        private TabPage BuildModsTab()
        {
            var page = new TabPage("Mods") { Padding = new Padding(4) };

            listViewMods = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                ShowItemToolTips = true,
            };
            listViewMods.Columns.Add("Mod", 220);
            listViewMods.Columns.Add("Latest", 110);
            listViewMods.Columns.Add("Author", 130);
            listViewMods.Columns.Add("Status", 200);
            listViewMods.ItemCheck += ListViewMods_ItemCheck;
            listViewMods.ItemChecked += ListViewMods_ItemChecked;
            listViewMods.SelectedIndexChanged += (s, e) => buttonModInfo.Enabled = listViewMods.SelectedItems.Count > 0;
            listViewMods.DoubleClick += (s, e) => OpenSelectedModPage();

            var menu = new ContextMenuStrip();
            menu.Items.Add("View on GitHub", null, (s, e) => OpenSelectedModPage());
            listViewMods.ContextMenuStrip = menu;

            page.Controls.Add(listViewMods);
            return page;
        }

        private TabPage BuildUtilitiesTab()
        {
            var page = new TabPage("Utilities") { Padding = new Padding(4) };
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };

            flow.Controls.Add(MakeGroup("Open folder",
                MakeButton("Game folder", (s, e) => OpenFolder(gameDir), true),
                MakeButton("BepInEx folder", (s, e) => OpenFolder(Installer.BepInExDir(gameDir)), true),
                MakeButton("Plugins folder", (s, e) => OpenFolder(Installer.PluginsDir(gameDir)), true),
                MakeButton("Config folder", (s, e) => OpenFolder(Installer.ConfigDir(gameDir)), true),
                MakeButton("BepInEx log", (s, e) => OpenFile(Path.Combine(Installer.BepInExDir(gameDir), "LogOutput.log")), true)));

            flow.Controls.Add(MakeGroup("Backups",
                MakeButton("Back up mods…", (s, e) => BackupMods(), true),
                MakeButton("Restore mods…", (s, e) => RestoreMods(), true)));

            flow.Controls.Add(MakeGroup("Uninstall",
                MakeButton("Remove all mods", (s, e) => UninstallAllMods(), true),
                MakeButton("Remove BepInEx", (s, e) => UninstallBepInEx(), true)));

            flow.Controls.Add(MakeGroup("Links",
                MakeButton("Modding guide", (s, e) => OpenUrl(GuideUrl), false),
                MakeButton("Mod Manager GitHub", (s, e) => OpenUrl("https://github.com/" + AppInfo.RepoSlug), false),
                MakeButton("BepInEx GitHub", (s, e) => OpenUrl("https://github.com/BepInEx/BepInEx"), false)));

            var picture = new PictureBox { Size = new Size(150, 130), SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(12) };
            try
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MonkeModManager.monke.png"))
                    picture.Image = Image.FromStream(stream);
            }
            catch { }
            flow.Controls.Add(picture);

            page.Controls.Add(flow);
            return page;
        }

        private static GroupBox MakeGroup(string title, params Button[] buttons)
        {
            var group = new GroupBox { Text = title, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6) };
            var inner = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                WrapContents = false
            };
            inner.Controls.AddRange(buttons);
            group.Controls.Add(inner);
            return group;
        }

        private Button MakeButton(string text, EventHandler onClick, bool needsGame)
        {
            var button = new Button { Text = text, Width = 150, Height = 28 };
            button.Click += onClick;
            if (needsGame) gameDependentControls.Add(button);
            return button;
        }

        #endregion

        #region Startup

        private async Task StartupAsync()
        {
            SetBusy(true, "Looking for Gorilla Tag…");

            string dir = GameLocator.IsGameFolder(settings.GamePath) ? settings.GamePath : GameLocator.Find();
            if (dir == null)
            {
                MessageBox.Show(this,
                    "We couldn't find your Gorilla Tag install automatically.\n\nPress OK and select \"Gorilla Tag.exe\".",
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                dir = PromptForGameExe();
            }
            SetGameDir(dir);

            var updateCheck = CheckForAppUpdateAsync();

            SetStatus("Downloading the mod list…");
            catalog = await ModCatalog.LoadAsync();
            PopulateModList();

            SetStatus("Getting the latest versions from GitHub…");
            await ResolveAllAsync();

            await updateCheck;

            var failed = catalog.Mods.Where(m => !m.IsResolved).ToList();
            SetBusy(false, failed.Count == 0
                ? "Ready! Pick your mods and press Install / Update."
                : $"Ready, but {failed.Count} mod(s) couldn't be found on GitHub right now.");

            await AutoUpdateBepInExAsync();
        }

        private async Task CheckForAppUpdateAsync()
        {
            string newer = await UpdateChecker.GetNewerVersionAsync();
            if (newer == null) return;

            var answer = MessageBox.Show(this,
                $"A new version of Monke Mod Manager is available ({newer}).\n\nOpen the download page?",
                "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (answer == DialogResult.Yes) OpenUrl(AppInfo.ReleasesPage);
        }

        private async Task ResolveAllAsync()
        {
            using (var gate = new SemaphoreSlim(4))
            {
                var tasks = catalog.Mods.Select(async mod =>
                {
                    await gate.WaitAsync();
                    try
                    {
                        await ReleaseResolver.ResolveAsync(mod);
                    }
                    finally
                    {
                        gate.Release();
                    }
                    RefreshItem(mod);
                    if (mod.IsBepInEx) RefreshBepInExLabel();
                });
                await Task.WhenAll(tasks);
            }
        }

        private void PopulateModList()
        {
            suppressCheckEvents = true;
            listViewMods.BeginUpdate();
            listViewMods.Items.Clear();
            listViewMods.Groups.Clear();

            var groups = catalog.Groups.ToDictionary(g => g, g => listViewMods.Groups.Add(g, g));
            foreach (var mod in catalog.Mods)
            {
                var item = new ListViewItem(mod.Name) { Tag = mod, Group = groups[catalog.GroupOf(mod)] };
                item.SubItems.Add("checking…");
                item.SubItems.Add(mod.Author);
                item.SubItems.Add("");
                item.Checked = mod.Required;
                listViewMods.Items.Add(item);
                RefreshItem(mod);
            }

            listViewMods.EndUpdate();
            suppressCheckEvents = false;
            RefreshLocks();
        }

        #endregion

        #region BepInEx

        private Version LatestBepInExVersion =>
            catalog?.BepInEx?.IsResolved == true ? ReleaseResolver.ParseVersion(catalog.BepInEx.Version) : null;

        private bool BepInExNeedsUpdate()
        {
            var installed = Installer.GetInstalledBepInExVersion(gameDir);
            var latest = LatestBepInExVersion;
            return installed != null && latest != null && ReleaseResolver.CompareVersions(installed, latest) < 0;
        }

        private void RefreshBepInExLabel()
        {
            if (gameDir == null)
            {
                labelBepInEx.Text = "BepInEx: select your game folder first";
                return;
            }

            var installed = Installer.GetInstalledBepInExVersion(gameDir);
            var latest = LatestBepInExVersion;
            string latestText = latest != null ? latest.ToString() : "unknown";

            if (installed == null)
                labelBepInEx.Text = $"BepInEx: not installed (latest {latestText} will be installed)";
            else if (latest != null && ReleaseResolver.CompareVersions(installed, latest) < 0)
                labelBepInEx.Text = $"BepInEx: {installed} installed — update to {latestText} available";
            else
                labelBepInEx.Text = $"BepInEx: {installed} installed (up to date)";
        }

        private async Task AutoUpdateBepInExAsync()
        {
            if (!settings.AutoUpdateBepInEx || gameDir == null || !BepInExNeedsUpdate()) return;

            if (Installer.IsGameRunning())
            {
                SetStatus("A BepInEx update is available — close Gorilla Tag and press Install / Update.");
                return;
            }

            SetBusy(true, "Updating BepInEx automatically…");
            try
            {
                await DownloadAndInstallAsync(catalog.BepInEx);
                SetStatus($"BepInEx updated to {catalog.BepInEx.Version}!");
            }
            catch (Exception ex)
            {
                SetStatus("Automatic BepInEx update failed: " + ex.Message);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        #endregion

        #region Installing

        private async Task InstallSelectedAsync()
        {
            if (gameDir == null)
            {
                BrowseForGame();
                if (gameDir == null) return;
            }

            if (Installer.IsGameRunning())
            {
                MessageBox.Show(this, "Please close Gorilla Tag before installing mods.", Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selected = listViewMods.Items.Cast<ListViewItem>()
                .Where(i => i.Checked)
                .Select(i => (ModInfo)i.Tag)
                .ToList();

            // BepInEx always goes first; it is the mod loader everything else needs.
            var bep = catalog.BepInEx;
            if (bep != null && !selected.Contains(bep)) selected.Insert(0, bep);
            selected = selected.OrderBy(m => m.IsBepInEx ? 0 : 1).ThenBy(m => DependencyDepth(m)).ToList();

            // Skip BepInEx when it is already the newest version.
            var installedBep = Installer.GetInstalledBepInExVersion(gameDir);
            if (bep != null && installedBep != null && !BepInExNeedsUpdate() && LatestBepInExVersion != null)
                selected.Remove(bep);

            var failures = new List<string>();
            SetBusy(true, "Starting install…");
            try
            {
                foreach (var mod in selected)
                {
                    if (!mod.IsResolved)
                    {
                        failures.Add($"{mod.Name}: {mod.ResolveError ?? "no download found"}");
                        continue;
                    }

                    try
                    {
                        await DownloadAndInstallAsync(mod);
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{mod.Name}: {ex.Message}");
                    }
                }
            }
            finally
            {
                SetBusy(false, failures.Count == 0 ? "Install complete! Launch Gorilla Tag to play with mods." : "Install finished with errors.");
            }

            if (failures.Count > 0)
            {
                MessageBox.Show(this, "Some mods could not be installed:\n\n" + string.Join("\n", failures),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private async Task DownloadAndInstallAsync(ModInfo mod)
        {
            SetStatus($"Downloading {mod.Name} {mod.Version}…");
            byte[] data = await Http.GetBytesAsync(mod.DownloadUrl);

            SetStatus($"Installing {mod.Name}…");
            string dir = gameDir;
            await Task.Run(() => Installer.Install(dir, mod, data));

            RefreshItem(mod);
            if (mod.IsBepInEx) RefreshBepInExLabel();
            SetStatus($"Installed {mod.Name}!");
        }

        private int DependencyDepth(ModInfo mod, int guard = 0)
        {
            if (guard > 20 || mod.Dependencies.Count == 0) return 0;
            return 1 + mod.Dependencies
                .Select(d => catalog.Mods.FirstOrDefault(m => m.Name == d))
                .Where(m => m != null)
                .Select(m => DependencyDepth(m, guard + 1))
                .DefaultIfEmpty(0)
                .Max();
        }

        #endregion

        #region Mod list checking

        private ListViewItem ItemFor(string modName) =>
            listViewMods.Items.Cast<ListViewItem>().FirstOrDefault(i => ((ModInfo)i.Tag).Name == modName);

        private ListViewItem ItemFor(ModInfo mod) =>
            listViewMods.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == mod);

        private bool IsLocked(ModInfo mod) =>
            mod.Required || mod.Dependents.Any(d => ItemFor(d)?.Checked == true);

        private void ListViewMods_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (suppressCheckEvents) return;
            var mod = (ModInfo)listViewMods.Items[e.Index].Tag;

            // Don't allow unchecking BepInEx or something another checked mod needs.
            if (e.NewValue == CheckState.Unchecked && IsLocked(mod))
                e.NewValue = CheckState.Checked;
        }

        private void ListViewMods_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (suppressCheckEvents) return;
            suppressCheckEvents = true;
            try
            {
                if (e.Item.Checked) CheckDependencies((ModInfo)e.Item.Tag, 0);
            }
            finally
            {
                suppressCheckEvents = false;
            }
            RefreshLocks();
        }

        private void CheckDependencies(ModInfo mod, int depth)
        {
            if (depth > 20) return;
            foreach (var dep in mod.Dependencies)
            {
                var item = ItemFor(dep);
                if (item == null) continue;
                item.Checked = true;
                CheckDependencies((ModInfo)item.Tag, depth + 1);
            }
        }

        private void RefreshLocks()
        {
            foreach (ListViewItem item in listViewMods.Items)
            {
                var mod = (ModInfo)item.Tag;
                bool locked = IsLocked(mod);
                item.ForeColor = locked ? SystemColors.GrayText : SystemColors.WindowText;
                item.ToolTipText = mod.Required
                    ? "Required: this is the mod loader every other mod needs."
                    : locked ? "Needed by another mod you selected." : $"github.com/{mod.GitPath}";
            }
        }

        private void RefreshItem(ModInfo mod)
        {
            var item = ItemFor(mod);
            if (item == null) return;

            item.SubItems[1].Text = mod.IsResolved ? mod.Version : (mod.ResolveError != null ? "unavailable" : "checking…");
            item.SubItems[2].Text = mod.Author;

            string status;
            if (mod.ResolveError != null && !mod.IsResolved)
                status = "Couldn't reach release: " + mod.ResolveError;
            else if (gameDir == null)
                status = "";
            else if (mod.IsBepInEx)
            {
                var installed = Installer.GetInstalledBepInExVersion(gameDir);
                if (installed == null) status = "Not installed";
                else if (BepInExNeedsUpdate()) status = $"Update available ({installed} installed)";
                else status = $"Installed ({installed})";
            }
            else
                status = Installer.IsModInstalled(gameDir, mod) ? "Installed" : "Not installed";

            item.SubItems[3].Text = status;
        }

        private void RefreshAllItems()
        {
            if (catalog == null) return;
            foreach (var mod in catalog.Mods) RefreshItem(mod);
            RefreshBepInExLabel();
        }

        #endregion

        #region Game folder

        private void SetGameDir(string dir)
        {
            gameDir = GameLocator.IsGameFolder(dir) ? dir : null;
            textBoxDirectory.Text = gameDir ?? "(not selected)";
            if (gameDir != null)
            {
                settings.GamePath = gameDir;
                settings.Save();
            }
            foreach (var control in gameDependentControls) control.Enabled = gameDir != null;
            RefreshAllItems();
        }

        private string PromptForGameExe()
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "Select Gorilla Tag.exe",
                FileName = AppInfo.GameExeName,
                Filter = "Gorilla Tag|" + AppInfo.GameExeName + "|Programs (*.exe)|*.exe",
                CheckFileExists = true,
            })
            {
                while (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    if (string.Equals(Path.GetFileName(dialog.FileName), AppInfo.GameExeName, StringComparison.OrdinalIgnoreCase))
                        return Path.GetDirectoryName(dialog.FileName);

                    MessageBox.Show(this, "That's not Gorilla Tag.exe! Please try again.", "Wrong file",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            return null;
        }

        private void BrowseForGame()
        {
            string dir = PromptForGameExe();
            if (dir != null) SetGameDir(dir);
        }

        #endregion

        #region Utilities

        private void BackupMods()
        {
            if (!Directory.Exists(Installer.PluginsDir(gameDir)))
            {
                MessageBox.Show(this, "There is no plugins folder to back up yet.", Text);
                return;
            }

            using (var dialog = new SaveFileDialog
            {
                FileName = $"Gorilla Tag Mods {DateTime.Now:yyyy-MM-dd}.zip",
                Filter = "ZIP file (*.zip)|*.zip",
                Title = "Save mod backup"
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                RunUtility("Backing up mods…", "Mods backed up!", () => Installer.BackupMods(gameDir, dialog.FileName));
            }
        }

        private void RestoreMods()
        {
            using (var dialog = new OpenFileDialog { Filter = "ZIP file (*.zip)|*.zip", Title = "Choose a mod backup" })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                RunUtility("Restoring mods…", "Mods restored!", () => Installer.RestoreMods(gameDir, dialog.FileName));
            }
        }

        private void UninstallAllMods()
        {
            if (MessageBox.Show(this,
                    "This deletes everything in BepInEx\\plugins (all mods, hats and materials). This cannot be undone!\n\nContinue?",
                    "Remove all mods", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            RunUtility("Removing all mods…", "All mods removed.", () => Installer.UninstallAllMods(gameDir));
        }

        private void UninstallBepInEx()
        {
            if (MessageBox.Show(this,
                    "This removes BepInEx, all mods and all mod configs, returning Gorilla Tag to vanilla.\n\nContinue?",
                    "Remove BepInEx", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            RunUtility("Removing BepInEx…", "BepInEx removed. The game is now unmodded.", () => Installer.UninstallBepInEx(gameDir));
        }

        private void RunUtility(string working, string done, Action action)
        {
            if (Installer.IsGameRunning())
            {
                MessageBox.Show(this, "Please close Gorilla Tag first.", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetStatus(working);
            try
            {
                action();
                SetStatus(done);
            }
            catch (Exception ex)
            {
                SetStatus("Failed: " + ex.Message);
                MessageBox.Show(this, ex.Message, "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            RefreshAllItems();
        }

        private void OpenSelectedModPage()
        {
            if (listViewMods.SelectedItems.Count == 0) return;
            var mod = (ModInfo)listViewMods.SelectedItems[0].Tag;
            OpenUrl("https://github.com/" + mod.GitPath);
        }

        private void OpenFolder(string path)
        {
            if (Directory.Exists(path)) Process.Start("explorer.exe", "\"" + path + "\"");
            else SetStatus("That folder doesn't exist yet. Install BepInEx and run the game once.");
        }

        private void OpenFile(string path)
        {
            if (File.Exists(path)) Process.Start(path);
            else SetStatus("That file doesn't exist yet. Run the game once with BepInEx installed.");
        }

        private static void OpenUrl(string url)
        {
            try { Process.Start(url); } catch { }
        }

        #endregion

        #region Status

        private void SetStatus(string text)
        {
            if (text != null) statusLabel.Text = text;
        }

        private void SetBusy(bool isBusy, string text)
        {
            busy = isBusy;
            progressBar.Visible = isBusy;
            buttonInstall.Enabled = !isBusy && catalog != null;
            buttonBrowse.Enabled = !isBusy;
            tabs.Enabled = !isBusy || catalog == null;
            checkBoxAutoBepInEx.Enabled = !isBusy;
            SetStatus(text);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (busy && e.CloseReason == CloseReason.UserClosing &&
                MessageBox.Show(this, "An install is in progress. Quit anyway?", Text,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        #endregion
    }
}

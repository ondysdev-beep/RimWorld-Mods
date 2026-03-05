using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using RimVerse.Client.Compat;
using RimVerse.Client.Core;
using UnityEngine;
using Verse;

namespace RimVerse.Client.UI
{
    public class ReportWindow : Window
    {
        private string _description = "";
        private string _category = "Desync";
        private string _statusMessage = "";
        private Vector2 _scrollPos;

        private static readonly string[] Categories = { "Desync", "Crash", "Bug", "Suggestion", "Other" };
        private int _categoryIndex = 0;

        public override Vector2 InitialSize => new Vector2(500f, 450f);

        public ReportWindow()
        {
            doCloseButton = true;
            draggable = true;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("Report MP Issue");
            Text.Font = GameFont.Small;
            listing.Gap(8f);

            listing.Label("Category:");
            if (listing.ButtonText(_category))
            {
                _categoryIndex = (_categoryIndex + 1) % Categories.Length;
                _category = Categories[_categoryIndex];
            }

            listing.Gap(8f);
            listing.Label("Description:");
            _description = listing.TextEntry(_description, 3);

            listing.Gap(12f);
            listing.Label("Auto-collected data:");
            listing.Label($"  Game Version: {VersionControl.CurrentVersionString}");
            listing.Label($"  Active Mods: {ModHasher.GetActiveModPackageIds().Length}");
            listing.Label($"  Modpack Hash: {ModHasher.ComputeCurrentModpackHash().Substring(0, 16)}...");
            listing.Label($"  Player: {RimVerseMod.LocalPlayerName ?? "N/A"}");
            listing.Label($"  Connected: {RimVerseMod.IsConnected}");

            listing.Gap(12f);
            if (listing.ButtonText("Export ZIP Report"))
            {
                ExportReport();
            }

            listing.Gap(4f);
            if (listing.ButtonText("Copy to Clipboard"))
            {
                CopyToClipboard();
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                listing.Gap(8f);
                GUI.color = Color.green;
                listing.Label(_statusMessage);
                GUI.color = Color.white;
            }

            listing.End();
        }

        private void ExportReport()
        {
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var zipPath = Path.Combine(desktopPath, $"RimVerse_Report_{timestamp}.zip");

                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    var infoEntry = zip.CreateEntry("report_info.txt");
                    using (var writer = new StreamWriter(infoEntry.Open()))
                    {
                        writer.WriteLine("=== RimVerse Bug Report ===");
                        writer.WriteLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        writer.WriteLine($"Category: {_category}");
                        writer.WriteLine($"Description: {_description}");
                        writer.WriteLine($"Game Version: {VersionControl.CurrentVersionString}");
                        writer.WriteLine($"Player: {RimVerseMod.LocalPlayerName ?? "N/A"}");
                        writer.WriteLine($"Server: {RimVerseMod.Settings.ServerUrl}");
                        writer.WriteLine($"Connected: {RimVerseMod.IsConnected}");
                        writer.WriteLine($"Modpack Hash: {ModHasher.ComputeCurrentModpackHash()}");
                    }

                    var modsEntry = zip.CreateEntry("modlist.txt");
                    using (var writer = new StreamWriter(modsEntry.Open()))
                    {
                        foreach (var modId in ModHasher.GetActiveModPackageIds())
                        {
                            writer.WriteLine(modId);
                        }
                    }

                    var logPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
                        "Ludeon Studios", "RimWorld by Ludeon Studios", "Player.log");
                    if (File.Exists(logPath))
                    {
                        zip.CreateEntryFromFile(logPath, "Player.log");
                    }
                }

                _statusMessage = $"Report exported to: {zipPath}";
                Log.Message($"[RimVerse] Report exported: {zipPath}");
            }
            catch (Exception ex)
            {
                _statusMessage = $"Export failed: {ex.Message}";
                Log.Error($"[RimVerse] Report export failed: {ex.Message}");
            }
        }

        private void CopyToClipboard()
        {
            var sb = new StringBuilder();
            sb.AppendLine("## RimVerse Bug Report");
            sb.AppendLine($"**Category:** {_category}");
            sb.AppendLine($"**Description:** {_description}");
            sb.AppendLine($"**Game Version:** {VersionControl.CurrentVersionString}");
            sb.AppendLine($"**Mods:** {ModHasher.GetActiveModPackageIds().Length} active");
            sb.AppendLine($"**Modpack Hash:** {ModHasher.ComputeCurrentModpackHash()}");
            sb.AppendLine($"**Connected:** {RimVerseMod.IsConnected}");

            GUIUtility.systemCopyBuffer = sb.ToString();
            _statusMessage = "Report copied to clipboard!";
        }
    }
}

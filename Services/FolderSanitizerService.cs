using PlexSanitizer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PlexSanitizer.Services
{
    public class FolderSanitizerService : IFolderSanitizerService
    {
        private readonly List<FolderSanitizationRule> _rules;

        public FolderSanitizerService()
        {
            _rules = GetAbsoluteSeriesScannerRules();
        }

        public async Task<List<FolderItem>> GetFoldersAsync(string rootPath)
        {
            var result = new List<FolderItem>();

            try
            {
                Debug.WriteLine($"GetFoldersAsync called with path: {rootPath}");

                // Check if this is a mapped drive before doing anything else
                if (NetworkAccessHelper.IsMappedDrive(rootPath))
                {
                    Debug.WriteLine($"Detected mapped drive: {rootPath}");
                    throw new NotSupportedException($"Mapped drives are not supported. Please use the full network path (e.g., \\\\server\\share) instead of '{rootPath}'.");
                }

                // Normalize the path to handle network paths better
                string normalizedPath = NormalizePath(rootPath);
                Debug.WriteLine($"Normalized path: {normalizedPath}");

                // Check if path exists
                bool pathExists = await PathExistsAsync(normalizedPath);
                Debug.WriteLine($"Path exists check result: {pathExists}");

                if (!pathExists)
                {
                    Debug.WriteLine($"Directory does not exist or is not accessible: {normalizedPath}. Using example data instead.");

                    // Add example folders for demonstration
                    string[] exampleFolders = new string[]
                    {
                        "Adobe CC Mega Pack For Mac - [CrackzSoft]",
                        "By Dawn's Early Light (1990)",
                        "Les Chevaliers du ciel",
                        "Naruto Shippuden",
                        "Zot van A. (2010). DVDR(xvid). NL Gespr. DMT",
                        "[Nep_Blanc][ToshY] Death Note"
                    };

                    foreach (var folderName in exampleFolders)
                    {
                        result.Add(new FolderItem
                        {
                            FullPath = Path.Combine(normalizedPath, folderName),
                            Name = folderName,
                            LastModified = DateTime.Now,
                            ParentPath = normalizedPath,
                            IsSelected = true
                        });
                    }
                }
                else
                {
                    // Normal operation - get actual folders from the directory
                    Debug.WriteLine($"Directory exists: {normalizedPath}. Scanning for subdirectories...");

                    string[] directories;
                    try
                    {
                        directories = Directory.GetDirectories(normalizedPath);
                        Debug.WriteLine($"Successfully got directories. Found: {directories.Length}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error getting directories: {ex.Message}. Trying alternative method.");

                        // Try DirectoryInfo as an alternative
                        try
                        {
                            var dirInfo = new DirectoryInfo(normalizedPath);
                            var dirInfos = dirInfo.GetDirectories();
                            directories = dirInfos.Select(di => di.FullName).ToArray();
                            Debug.WriteLine($"Got directories using DirectoryInfo. Found: {directories.Length}");
                        }
                        catch (Exception ex2)
                        {
                            Debug.WriteLine($"Both methods failed to get directories: {ex2.Message}");
                            throw;
                        }
                    }

                    if (directories != null)
                    {
                        Debug.WriteLine($"Processing {directories.Length} directories");
                        foreach (var dir in directories)
                        {
                            try
                            {
                                var dirInfo = new DirectoryInfo(dir);
                                result.Add(new FolderItem
                                {
                                    FullPath = dir,
                                    Name = dirInfo.Name,
                                    LastModified = dirInfo.LastWriteTime,
                                    ParentPath = dirInfo.Parent?.FullName,
                                    IsSelected = true
                                });
                                Debug.WriteLine($"Added folder: {dirInfo.Name}");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error processing directory {dir}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Directories array is null");
                    }
                }

                return await Task.FromResult(result);
            }
            catch (NotSupportedException)
            {
                // Re-throw mapped drive exceptions as-is
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting folders: {ex.Message}");

                // If we hit an exception, still return example data for demo purposes
                if (result.Count == 0)
                {
                    Debug.WriteLine("Using example data due to error.");

                    string[] exampleFolders = new string[]
                    {
                        "Adobe CC Mega Pack For Mac - [CrackzSoft]",
                        "By Dawn's Early Light (1990)",
                        "Les Chevaliers du ciel",
                        "Naruto Shippuden",
                        "Zot van A. (2010). DVDR(xvid). NL Gespr. DMT",
                        "[Nep_Blanc][ToshY] Death Note"
                    };

                    foreach (var folderName in exampleFolders)
                    {
                        result.Add(new FolderItem
                        {
                            FullPath = Path.Combine(rootPath, folderName),
                            Name = folderName,
                            LastModified = DateTime.Now,
                            ParentPath = rootPath,
                            IsSelected = true
                        });
                    }
                }

                return result;
            }
        }

        public async Task<List<FolderItem>> PreviewSanitizationAsync(List<FolderItem> folders)
        {
            foreach (var folder in folders)
            {
                string sanitizedName = folder.Name;

                // Apply all active rules in sequence
                foreach (var rule in _rules.Where(r => r.IsActive))
                {
                    sanitizedName = rule.Apply(sanitizedName);
                }

                // Only set NewName if it's different from the original
                if (sanitizedName != folder.Name && !string.IsNullOrWhiteSpace(sanitizedName))
                {
                    folder.NewName = sanitizedName;
                }
                else
                {
                    folder.NewName = folder.Name;
                }
            }

            return await Task.FromResult(folders);
        }

        public async Task<bool> ApplySanitizationAsync(List<FolderItem> folders)
        {
            bool allSuccessful = true;
            bool demoMode = false;

            // Check if any of the folders actually exist
            if (folders.Any())
            {
                var firstPath = folders.First().FullPath;
                demoMode = !(await PathExistsAsync(firstPath));

                if (demoMode)
                {
                    Debug.WriteLine("Running in demo mode - no actual file system changes will be made");
                }
                else
                {
                    Debug.WriteLine("Running in real mode - actual file system changes will be made");
                }
            }

            foreach (var folder in folders.FindAll(f => f.IsSelected && f.HasChanges))
            {
                try
                {
                    if (demoMode)
                    {
                        // In demo mode, just update the name in memory
                        folder.Name = folder.NewName;
                        Debug.WriteLine($"[DEMO] Would rename: {folder.Name} -> {folder.NewName}");
                        continue;
                    }

                    string parentDir = folder.ParentPath;
                    string newPath = Path.Combine(parentDir, folder.NewName);

                    // Check if directory already exists
                    if (await PathExistsAsync(newPath))
                    {
                        Debug.WriteLine($"Target directory already exists: {newPath}");
                        allSuccessful = false;
                        continue;
                    }

                    Debug.WriteLine($"Attempting to rename: {folder.FullPath} -> {newPath}");

                    try
                    {
                        // Rename directory
                        Directory.Move(folder.FullPath, newPath);
                        folder.FullPath = newPath;
                        folder.Name = folder.NewName;
                        Debug.WriteLine($"Successfully renamed folder: {folder.FullPath} -> {newPath}");
                    }
                    catch (UnauthorizedAccessException uaEx)
                    {
                        Debug.WriteLine($"Access denied while renaming folder: {uaEx.Message}");
                        allSuccessful = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error renaming folder: {ex.Message}");
                    allSuccessful = false;
                }
            }

            return await Task.FromResult(allSuccessful);
        }

        public List<FolderSanitizationRule> GetRules()
        {
            return _rules;
        }

        private List<FolderSanitizationRule> GetAbsoluteSeriesScannerRules()
        {
            return new List<FolderSanitizationRule>
            {
                // Rule 1: Remove common release prefixes
                new FolderSanitizationRule
                {
                    Name = "Remove Release Prefixes",
                    Description = "Removes common release prefixes while preserving content",
                    Pattern = @"^(?:DVDR?|NL\s*Gespr|DMT|DutchReleaseTeam|Xvid|DivX|BluRay|BRRip|DVDRip|HDRip|WEBRip|HDTV|PDTV|BDRip)(?:\s*[\.\-_]+\s*)?",
                    Replacement = ""
                },

                // Rule 2: Preserve and clean forced IDs (CRITICAL for ASS)
                new FolderSanitizationRule
                {
                    Name = "Preserve Forced IDs",
                    Description = "Preserves {anidb-123}, {tvdb-456} style IDs while cleaning around them",
                    Pattern = @"^(.*?)(\s*\{(?:anidb|tvdb|anidb2|anidb3|anidb4|tvdb2|tvdb3|tvdb4|tvdb5|tmdb|tsdb|imdb|youtube|youtube2)-\d+\})\s*(.*)$",
                    Replacement = "$1 $2"
                },

                // Rule 3: Handle FC2 PPV content specifically
                new FolderSanitizationRule
                {
                    Name = "Clean FC2 PPV Content",
                    Description = "Extracts FC2 PPV identifier while removing extra text",
                    Pattern = @"^.*?(FC2[\s\-]*PPV[\s\-]*\d+).*$",
                    Replacement = "$1"
                },

                // Rule 4: Convert bracketed years to parentheses and handle year ranges
                new FolderSanitizationRule
                {
                    Name = "Fix Year Brackets and Handle Year Ranges",
                    Description = "Converts [YYYY] to (YYYY), takes start year from ranges like (2004-2007) -> (2004), and removes technical specs in brackets",
                    Pattern = @"\[((19|20)\d{2})\]|\(((19|20)\d{2})-((19|20)\d{2})\)|\[(?![A-Za-z_\s]*$)[^\]]*\]",
                    Replacement = "($1$3)"
                },

                // Rule 5: Remove technical specifications in parentheses (except years)
                new FolderSanitizationRule
                {
                    Name = "Remove Technical Parentheses",
                    Description = "Removes technical specs in parentheses while preserving years",
                    Pattern = @"\((?!(?:19|20)\d{2}\))[^)]*\)",
                    Replacement = ""
                },

                // Rule 6: Remove codec and quality specifications (enhanced)
                new FolderSanitizationRule
                {
                    Name = "Remove Codec/Quality Specs",
                    Description = "Removes video/audio codec and quality specifications",
                    Pattern = @"\b(?:UNCENx264|x264|x265|H\.?264|H\.?265|HEVC|AVC|10bit|8bit|AAC|AC3|EAC3|DDP5\.?1?|DD5\.?1?|DTS|Atmos|5\.1|2\.0|1080p|720p|480p|2160p|4K|WEB-?DL|NF|HULU|BluRay|BRRip|WEBRip|HDRip|DVDRip|COMPLETE|Mixed|DSNP|(?:H|x)\.?264|ITA|SWE|AMZN|REPACK|HMAX|WEBDL|NHTFS)\b",
                    Replacement = ""
                },

                // Rule 7: Remove release group tags (enhanced with even more groups)
                new FolderSanitizationRule
                {
                    Name = "Remove Release Groups",
                    Description = "Removes common release group tags in brackets and at end of names",
                    Pattern = @"[\[\(](?:TMD-Group|OceanVeil|CrackzSoft|YTS\.(?:MX|AM|LT)|RARBG|YIFY|Silence|Kappa|NTb|Atmos|Deadmauvlad|RCVR|SuccessfulCrab|NVEnc|TGx|rartv|XEBEC|PECULATE|Vyndros|KONTRAST|PHDTeam|MoviesMod|HANDJOB|SMURF|StereotypedGazelleOfWondrousPassion|TÅoE|HxD|ImE|MeM|GP)[\]\)]|[-\.\s]+(?:TMD-Group|OceanVeil|CrackzSoft|YTS\.(?:MX|AM|LT)|RARBG|YIFY|Silence|Kappa|NTb|Atmos|Deadmauvlad|RCVR|SuccessfulCrab|NVEnc|TGx|rartv|XEBEC|PECULATE|Vyndros|KONTRAST|PHDTeam|MoviesMod|HANDJOB|SMURF|StereotypedGazelleOfWondrousPassion|TÅoE|HxD|ImE|MeM|GP)$",
                    Replacement = ""
                },

                // Rule 7b: Remove additional release group patterns (enhanced)
                new FolderSanitizationRule
                {
                    Name = "Remove Additional Release Groups",
                    Description = "Removes release groups that appear at the end without brackets",
                    Pattern = @"\s*[-\.\s]*(?:SuccessfulCrab|XEBEC|PECULATE|NTb|TGx|rartv|Vyndros|English\s*Subs?|KONTRAST|PHDTeam|MoviesMod|HANDJOB|Swedish\s*Msubs|SMURF|StereotypedGazelleOfWondrousPassion|TÅoE|HxD|ImE|MeM|GP)\s*$",
                    Replacement = ""
                },

                // Rule 8: Clean season/series information and release tags for top-level folders (enhanced)
                new FolderSanitizationRule
                {
                    Name = "Remove Top-Level Season Info and Tags",
                    Description = "Removes season info and trailing tags from top-level folders",
                    Pattern = @"\b(?:Complete\s+)?(?:Series|Collection|(?:Season\s*\d+(?:\s*\-?\s*\d+)?|S\d+(?:-S\d+)?(?:E\d+)?|\d+\s*seasons?|Season\s*\d+\-\d+))(?:\s*\+?\s*Extras?)?\b|[-\.\s]+(?:Complete\s+Collection|The\s+Complete\s+Collection)$|\s*\[complete\]|\s*Season\s*\d+\s*\[complete\]",
                    Replacement = ""
                },

                // Rule 9: Remove language and subtitle indicators (final enhancement)
                new FolderSanitizationRule
                {
                    Name = "Remove Language Indicators",
                    Description = "Removes language and subtitle indicators",
                    Pattern = @"\b(?:UNCENSORED\s*)?(?:English|Eng|Dutch|NL|Swedish|Danish|Subs?|Subtitles|Hardsub|Msubs|optional\s*Eng\s*subs?|SweSub|EngSub)(?:\s+(?:Hardsub|Msubs|subs?))?\b",
                    Replacement = ""
                },

                // Rule 10: Remove Asian characters (disabled by default)
                new FolderSanitizationRule
                {
                    Name = "Remove Asian Characters",
                    Description = "Removes Japanese/Chinese/Korean characters (use carefully for anime)",
                    Pattern = @"[\p{IsHiragana}\p{IsKatakana}\p{IsCJKUnifiedIdeographs}\p{IsHangulSyllables}]+",
                    Replacement = "",
                    IsActive = false
                },

                // Rule 11: Remove file size and edition indicators
                new FolderSanitizationRule
                {
                    Name = "Remove Size/Edition Info",
                    Description = "Removes file size and edition indicators",
                    Pattern = @"\b(?:\d+(?:\.\d+)?(?:GB|MB)|Edition|Remastered|Director.?s?\s*Cut|Extended|Uncut|Unrated)\b",
                    Replacement = ""
                },

                // Rule 12: Handle special characters
                new FolderSanitizationRule
                {
                    Name = "Clean Special Characters",
                    Description = "Cleans special characters while preserving essential ones",
                    Pattern = @"[ːø]",
                    Replacement = ""
                },

                // Rule 13: Replace separators with spaces
                new FolderSanitizationRule
                {
                    Name = "Replace Separators",
                    Description = "Replaces dots, underscores with spaces",
                    Pattern = @"[\._]+",
                    Replacement = " "
                },

                // Rule 14: Clean up multiple spaces
                new FolderSanitizationRule
                {
                    Name = "Clean Multiple Spaces",
                    Description = "Replaces multiple spaces with single space",
                    Pattern = @"\s{2,}",
                    Replacement = " "
                },

                // Rule 15: Remove leading/trailing spaces and separators
                new FolderSanitizationRule
                {
                    Name = "Trim Spaces and Separators",
                    Description = "Removes leading/trailing spaces and separators",
                    Pattern = @"^[\s\-_,\.]+|[\s\-_,\.]+$",
                    Replacement = ""
                },

                // Rule 16: Extract known series patterns (more conservative)
                new FolderSanitizationRule
                {
                    Name = "Extract Known Series",
                    Description = "Preserves known series names (disabled by default - add your own patterns)",
                    Pattern = @".*(Death Note|Naruto Shippuden|Dragon Ball|One Piece|Bleach|Attack on Titan|Sword Art Online|My Hero Academia|Demon Slayer|Jujutsu Kaisen).*",
                    Replacement = "$1",
                    IsActive = false // Disabled by default - can be too aggressive
                },

                // Rule 17: Fix title spacing (more conservative)
                new FolderSanitizationRule
                {
                    Name = "Fix Title Spacing",
                    Description = "Fixes spacing issues in compound words (disabled by default - can be aggressive)",
                    Pattern = @"\b([a-z])([A-Z])",
                    Replacement = "$1 $2",
                    IsActive = false // Disabled by default as it can split words incorrectly
                },

                // Rule 18: Don't force year parentheses (they should already be handled)
                new FolderSanitizationRule
                {
                    Name = "Clean Standalone Years",
                    Description = "Removes standalone years that aren't in parentheses",
                    Pattern = @"^\s*(19|20)\d{2}\s*$",
                    Replacement = "",
                    IsActive = false // Usually we want to keep years
                },

                // Rule 20: Final cleanup - remove empty brackets/parentheses
                new FolderSanitizationRule
                {
                    Name = "Remove Empty Brackets",
                    Description = "Removes empty brackets and parentheses left after cleaning",
                    Pattern = @"\[\s*\]|\(\s*\)",
                    Replacement = ""
                },

                // Rule 21: Clean trailing technical remnants (final enhancement)
                new FolderSanitizationRule
                {
                    Name = "Clean Trailing Technical Remnants",
                    Description = "Removes technical remnants and incomplete words at the end",
                    Pattern = @"\s*[-\.\s]*(?:WEB|NTb|TGx|SuccessfulCrab|XEBEC|PECULATE|KONTRAST|PHDTeam|MoviesMod|HANDJOB|Animated\s*TV|Complete\s*Animated\s*TV\s*Series|SMURF|StereotypedGazelleOfWondrousPassion|TÅoE|HxD|ImE|MeM|GP|ITA|SWE|LITTLEBLUEMAN|NHTFS|HazMatt|HIQVE|EDITH|DANISH|EDGE2020|TheSickle|RAV1NE|EnlaHD|GalaxyTV|Justiso|NTG|RVKD|RICK|BadRips|\-\d{2}|SweSub|EngSub|AV1|Opus)\s*$",
                    Replacement = ""
                },

                // Rule 22: Clean grouping indicators (disabled by default)
                new FolderSanitizationRule
                {
                    Name = "Clean Grouping Indicators",
                    Description = "Cleans grouping folder indicators while preserving structure",
                    Pattern = @"\b(?:Part|Pt|Arc|Saga|Story)\s*\d+\b",
                    Replacement = "",
                    IsActive = false
                }
            };
        }

        public void ToggleRule(int index, bool isActive)
        {
            if (index >= 0 && index < _rules.Count)
            {
                _rules[index].IsActive = isActive;
            }
        }

        public async Task<bool> PathExistsAsync(string path)
        {
            try
            {
                // Normalize the path
                string normalizedPath = NormalizePath(path);
                Debug.WriteLine($"Checking if path exists: {normalizedPath}");

                // For network paths, use the network helper
                if (IsNetworkPath(normalizedPath))
                {
                    Debug.WriteLine($"Detected network path: {normalizedPath}");
                    bool isAccessible = NetworkAccessHelper.IsNetworkPathAccessible(normalizedPath);
                    Debug.WriteLine($"Network path accessibility check result: {isAccessible}");

                    if (!isAccessible && normalizedPath.StartsWith("\\\\"))
                    {
                        Debug.WriteLine($"Attempting to connect to UNC path: {normalizedPath}");
                        bool connected = NetworkAccessHelper.ConnectToNetworkShare(normalizedPath);
                        Debug.WriteLine($"Connection attempt result: {connected}");

                        if (connected)
                        {
                            isAccessible = NetworkAccessHelper.IsNetworkPathAccessible(normalizedPath);
                        }
                    }

                    return isAccessible;
                }

                // Standard approach for local paths
                if (Directory.Exists(normalizedPath))
                {
                    return true;
                }

                // Try additional methods for non-network paths
                try
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(normalizedPath);
                    if (dirInfo.Exists)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error checking path with DirectoryInfo: {ex.Message}");
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking path: {ex.Message}");
                return false;
            }
        }

        private bool IsNetworkPath(string path)
        {
            // Check if this is a network path (UNC or mapped drive)
            return path.StartsWith("\\\\") || NetworkAccessHelper.IsMappedDrive(path);
        }

        private string NormalizePath(string path)
        {
            Debug.WriteLine($"Normalizing path: {path}");

            if (string.IsNullOrEmpty(path))
                return path;

            // Trim whitespace
            path = path.Trim();

            // Try to resolve mapped drives using the NetworkAccessHelper
            if (path.Length >= 2 && path[1] == ':')
            {
                string uncPath = NetworkAccessHelper.GetUNCPath(path);
                if (!string.IsNullOrEmpty(uncPath))
                {
                    Debug.WriteLine($"Resolved drive letter to UNC path: {path} -> {uncPath}");
                    return uncPath;
                }
            }

            // Handle UNC paths (already normalized)
            if (path.StartsWith("\\\\"))
            {
                Debug.WriteLine($"Path is already in UNC format: {path}");
                return path;
            }

            // Handle local drive paths
            if (path.Length >= 2 && path[1] == ':')
            {
                // Ensure proper formatting
                if (path.Length == 2)
                {
                    return path + "\\";
                }

                if (path.Length > 2 && path[2] != '\\')
                {
                    return path.Substring(0, 2) + "\\" + path.Substring(2);
                }

                Debug.WriteLine($"Local drive path: {path}");
                return path;
            }

            // Handle forward slashes
            if (path.Contains("/"))
            {
                Debug.WriteLine($"Converting forward slashes to backslashes: {path}");
                return path.Replace("/", "\\");
            }

            return path;
        }
    }
}
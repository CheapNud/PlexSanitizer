using System.Text.RegularExpressions;

namespace PlexSanitizer.Models
{
    public class FolderSanitizationRule
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Pattern { get; set; }
        public string Replacement { get; set; }
        public bool IsActive { get; set; } = true;

        public string Apply(string input)
        {
            if (!IsActive) return input;
            return Regex.Replace(input, Pattern, Replacement, RegexOptions.IgnoreCase);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using MahApps.Metro.IconPacks;

namespace Sleipnir.App.Utils
{
    public class IconItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public static class EmojiHelper
    {
        private static readonly List<IconItem> _allIcons = new();

        static EmojiHelper()
        {
            try
            {
                // Use reflection to get all available Material icons automatically
                var kindType = typeof(PackIconMaterialKind);
                var names = Enum.GetNames(kindType);

                foreach (var name in names.OrderBy(n => n))
                {
                    // Skip names that look like internal or obscure markers if any
                    // and keep it to a manageable list if it's too huge, but usually it's fine.
                    _allIcons.Add(new IconItem { Id = name, Name = name });
                }
            }
            catch
            {
                // Fallback if reflection fails
                _allIcons.Add(new IconItem { Id = "Account", Name = "Account" });
                _allIcons.Add(new IconItem { Id = "Airplane", Name = "Airplane" });
                _allIcons.Add(new IconItem { Id = "Heart", Name = "Heart" });
                _allIcons.Add(new IconItem { Id = "Star", Name = "Star" });
            }
        }

        public static List<IconItem> GetAllIcons()
        {
            return _allIcons;
        }

        public static List<string> GetPopularIcons()
        {
            // Give a decent selection of popular ones first
            return _allIcons.Take(200).Select(i => i.Id).ToList();
        }

        // Compatibility
        public static List<string> GetPopularEmojis() => GetPopularIcons();
        public static List<IconItem> GetAllEmojis() => GetAllIcons();
    }
}

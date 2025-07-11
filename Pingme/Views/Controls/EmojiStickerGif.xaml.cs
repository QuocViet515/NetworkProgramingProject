using Newtonsoft.Json;
using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Pingme.Models;
using EmojiTextBlock = Emoji.Wpf.TextBlock;


namespace Pingme.Views.Controls
{
    public partial class EmojiStickerGif : UserControl
    {
        public event EventHandler<string> EmojiSelected;

        private List<EmojiData> allEmojis = new List<EmojiData>();
        private List<string> recentEmojis = new List<string>(); // Gần đây

        public EmojiStickerGif()
        {
            InitializeComponent();
            LoadEmojisFromJson();
        }

        private void LoadEmojisFromJson()
        {
            try
            {
                string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "emoji.json");

                if (!System.IO.File.Exists(jsonPath))
                {
                    MessageBox.Show($"Không tìm thấy file emoji.json tại {jsonPath}");
                    return;
                }

                string json = System.IO.File.ReadAllText(jsonPath);
                allEmojis = JsonConvert.DeserializeObject<List<EmojiData>>(json);
                ShowEmojis("all"); // Hiển thị mặc định
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi load emoji: " + ex.Message);
            }
        }

        private void ShowEmojis(string category)
        {
            EmojiPanel.Children.Clear();

            List<EmojiData> filtered;

            if (category == "recent")
            {
                filtered = allEmojis
                    .Where(e => recentEmojis.Contains(e.emoji))
                    .OrderByDescending(e => recentEmojis.IndexOf(e.emoji))
                    .ToList();
            }
            else if (category == "all")
            {
                filtered = allEmojis;
            }
            else
            {
                filtered = allEmojis
                    .Where(e => string.Equals(e.category, category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            foreach (var item in filtered)
            {
                var emojiBlock = new EmojiTextBlock
                {
                    Text = item.emoji,
                    FontSize = 28,
                    FontFamily = new FontFamily("Segoe UI Emoji"),
                    Margin = new Thickness(5),
                    Cursor = Cursors.Hand,
                    Foreground = Brushes.Black
                };

                emojiBlock.MouseLeftButtonUp += Emoji_Click;
                EmojiPanel.Children.Add(emojiBlock);
            }
        }


        private void Category_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                ShowEmojis(tag);
            }
        }

        private void Emoji_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is EmojiTextBlock tb && !string.IsNullOrEmpty(tb.Text))
            {
                AddToRecent(tb.Text);
                EmojiSelected?.Invoke(this, tb.Text);
            }
        }

        private void AddToRecent(string emoji)
        {
            if (recentEmojis.Contains(emoji))
            {
                recentEmojis.Remove(emoji);
            }

            recentEmojis.Insert(0, emoji);

            if (recentEmojis.Count > 20)
            {
                recentEmojis = recentEmojis.Take(20).ToList();
            }
        }


        private void CategoryTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryTabControl.SelectedItem is TabItem tab && tab.Tag is string category)
            {
                ShowEmojis(category);
            }
        }

    }
}

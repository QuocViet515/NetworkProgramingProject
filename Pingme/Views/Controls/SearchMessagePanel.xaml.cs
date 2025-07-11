using Firebase.Database;
using Pingme.Helpers;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Firebase.Database.Query;

namespace Pingme.Views.Controls
{
    public partial class SearchMessagePanel : UserControl
    {
        private ObservableCollection<Message> allMessages = new ObservableCollection<Message>();
        public event Action<Message> MessageClicked;

        public ObservableCollection<Message> AllMessages
        {
            get => allMessages;
            set
            {
                allMessages = value;

                // Cập nhật SenderName cho mỗi tin nhắn
                _ = Task.Run(async () =>
                {
                    var firebase = new FirebaseClient("https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
                        new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken) });

                    foreach (var msg in allMessages)
                    {
                        if (string.IsNullOrEmpty(msg.SenderName))
                        {
                            if (msg.SenderId == SessionManager.UID)
                            {
                                msg.SenderName = "Bạn";
                            }
                            else if (!userNameCache.ContainsKey(msg.SenderId))
                            {
                                try
                                {
                                    var user = await firebase.Child("users").Child(msg.SenderId).OnceSingleAsync<User>();
                                    userNameCache[msg.SenderId] = user.FullName ?? "Không rõ";
                                }
                                catch
                                {
                                    userNameCache[msg.SenderId] = "Không rõ";
                                }

                                msg.SenderName = userNameCache[msg.SenderId];
                            }
                            else
                            {
                                msg.SenderName = userNameCache[msg.SenderId];
                            }
                        }
                    }

                    // Gọi lại search UI
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SearchBox_TextChanged(null, null);
                    });
                });
            }
        }
        private Dictionary<string, string> userNameCache = new Dictionary<string, string>();


        public SearchMessagePanel()
        {
            InitializeComponent();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = SearchBox.Text.ToLower().Trim();
            SearchResultPanel.Children.Clear();

            if (string.IsNullOrWhiteSpace(keyword))
                return;

            var results = AllMessages
                .Where(m => m.Content?.ToLower().Contains(keyword) == true)
                .ToList();

            if (results.Count == 0)
            {
                SearchResultPanel.Children.Add(new TextBlock
                {
                    Text = "Không tìm thấy tin nhắn nào.",
                    Foreground = System.Windows.Media.Brushes.Gray
                });
                return;
            }

            foreach (var msg in results)
            {
                var stack = new StackPanel
                {
                    Margin = new Thickness(0, 5, 0, 10)
                };

                string senderName = msg.SenderName ?? (msg.SenderId == SessionManager.UID ? "Bạn" : msg.SenderId);

                stack.Children.Add(new TextBlock
                {
                    Text = $"{senderName} - {msg.SentAt.ToLocalTime():dd/MM/yyyy HH:mm}",
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Gray,
                    FontSize = 12
                });

                var contentBlock = new TextBlock
                {
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                string original = msg.Content ?? "";
                string lowerContent = original.ToLower();
                int index = 0;

                while (index < lowerContent.Length)
                {
                    int matchIndex = lowerContent.IndexOf(keyword, index);
                    if (matchIndex == -1)
                    {
                        // Không còn keyword -> thêm phần còn lại
                        contentBlock.Inlines.Add(new Run(original.Substring(index)));
                        break;
                    }

                    // Thêm đoạn trước keyword
                    if (matchIndex > index)
                    {
                        contentBlock.Inlines.Add(new Run(original.Substring(index, matchIndex - index)));
                    }

                    // Thêm phần keyword được highlight
                    contentBlock.Inlines.Add(new Run(original.Substring(matchIndex, keyword.Length))
                    {
                        Foreground = Brushes.DodgerBlue,
                        FontWeight = FontWeights.SemiBold
                    });

                    index = matchIndex + keyword.Length;
                }

                stack.Children.Add(contentBlock);

                // Thêm border + hiệu ứng hover
                var border = new Border
                {
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(10),
                    Background = Brushes.Transparent,
                    Child = stack
                };
                border.MouseEnter += (s, e1) => border.Background = new SolidColorBrush(Color.FromRgb(230, 240, 255));
                border.MouseLeave += (s, e2) => border.Background = Brushes.Transparent;
                border.MouseLeftButtonUp += (s, e3) =>
                {
                    MessageClicked?.Invoke(msg);
                };

                SearchResultPanel.Children.Add(border);
            }
        }
    }
}


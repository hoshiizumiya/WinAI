using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.UI;

namespace AIAppT
{
    /// <summary>
    /// Ollama聊天应用的主窗口
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private OllamaService _ollamaService;
        private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();
        private string _currentModel = "llama2";

        public MainWindow()
        {
            this.InitializeComponent();

            // 设置转换器
            InitializeConverters();

            // 初始化Ollama服务
            _ollamaService = new OllamaService(modelName: _currentModel);

            // 设置聊天列表数据源
            ChatList.ItemsSource = _chatMessages;

            // 检查Ollama服务可用性
            CheckOllamaAvailability();

            // 加载可用模型
            LoadAvailableModels();
        }

        private void InitializeConverters()
        {
            // 这些转换器需要在App.xaml中定义
            // 或在此处代码中创建
        }

        private async 
        Task
CheckOllamaAvailability()
        {
            StatusText.Text = "正在连接Ollama服务...";

            bool isAvailable = await _ollamaService.IsServerAvailableAsync();

            if (isAvailable)
            {
                StatusText.Text = "已连接到Ollama服务";
            }
            else
            {
                StatusText.Text = "无法连接到Ollama服务";
                ContentDialog dialog = new ContentDialog
                {
                    Title = "连接错误",
                    Content = "无法连接到Ollama服务。请确保Ollama已安装并运行在localhost:11434。",
                    CloseButtonText = "确定",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }

        private async void LoadAvailableModels()
        {
            try
            {
                ModelSelector.IsEnabled = false;
                var models = await _ollamaService.GetAvailableModelsAsync();

                if (models.Length > 0)
                {
                    ModelSelector.ItemsSource = models;
                    ModelSelector.SelectedItem = _currentModel;
                }
                else
                {
                    ModelSelector.ItemsSource = new[] { "llama2" };
                    ModelSelector.SelectedItem = "llama2";
                }
            }
            catch (Exception ex)
            {
                // 处理错误
            }
            finally
            {
                ModelSelector.IsEnabled = true;
            }
        }

        private void ModelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ModelSelector.SelectedItem is string selectedModel)
            {
                _currentModel = selectedModel;
                _ollamaService = new OllamaService(modelName: _currentModel);
            }
        }

        private async void RefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            await CheckOllamaAvailability();
            LoadAvailableModels();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string prompt = PromptInput.Text?.Trim();

            if (string.IsNullOrEmpty(prompt))
                return;

            // 添加用户消息
            _chatMessages.Add(new ChatMessage { Content = prompt, IsUser = true });

            // 清空输入框
            PromptInput.Text = string.Empty;

            // 禁用发送按钮，显示加载状态
            SendButton.IsEnabled = false;
            LoadingRing.IsActive = true;

            try
            {
                // 调用Ollama API获取响应
                string response = await _ollamaService.GenerateResponseAsync(prompt);

                // 添加AI响应
                _chatMessages.Add(new ChatMessage { Content = response, IsUser = false });
            }
            catch (Exception ex)
            {
                // 处理错误
                _chatMessages.Add(new ChatMessage
                {
                    Content = $"发生错误: {ex.Message}",
                    IsUser = false
                });
            }
            finally
            {
                // 恢复UI状态
                SendButton.IsEnabled = true;
                LoadingRing.IsActive = false;
            }
        }
    }

    public class ChatMessage
    {
        public string Content { get; set; } = string.Empty;
        public bool IsUser { get; set; }
    }

    public class BoolToColorConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isUser = (bool)value;
            return isUser
                ? new SolidColorBrush(Microsoft.UI.Colors.LightBlue)
                : new SolidColorBrush(Microsoft.UI.Colors.LightGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToAlignmentConverter : Microsoft.UI.Xaml.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isUser = (bool)value;
            return isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
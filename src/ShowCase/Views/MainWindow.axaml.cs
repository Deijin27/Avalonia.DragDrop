using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ShowCase.ViewModels;

namespace ShowCase.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            //SetValue(DragDrop.AllowDropProperty, true);
            //AddHandler(DragDrop.DragOverEvent, DragOver);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void DragOver(object? sender, DragEventArgs e)
        {
            e.DragEffects = DragDropEffects.None;
        }
    }
}
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Media;
using System.Diagnostics;
using System;
using System.Linq;
using Avalonia.Data;
using System.Collections;
using Avalonia.Controls.Templates;

namespace Avalonia.DragDrop
{
    public record DragStartResult(IDataObject Data, DragDropEffects AllowedEffects, string DataFormat = IDragSource.DefaultDataFormat);

    public interface IDragSource
    {
        public const string DefaultDataFormat = "Avalonia.DragDrop";
        // TODO: make our own wrapper for the data
        DragStartResult? StartDrag(PointerEventArgs dragInfo);
    }

    public interface IDropTarget
    {
        // TODO: make our own wrapper for the data
        DragDropEffects DragOver(DragEventArgs dropInfo);

        void Drop(DragEventArgs dropInfo);
    }

    /// <summary>
    /// Container class for attached properties. Must inherit from <see cref="AvaloniaObject"/>.
    /// </summary>
    public partial class DragDrop : AvaloniaObject
    {

        public static readonly AttachedProperty<bool> IsDragSourceProperty = 
            AvaloniaProperty.RegisterAttached<DragDrop, InputElement, bool>("IsDragSource");

        public static readonly AttachedProperty<bool> IsDropTargetProperty =
            AvaloniaProperty.RegisterAttached<DragDrop, Interactive, bool>("IsDropTarget");

        public static readonly AttachedProperty<IDragSource?> DragHandlerProperty =
            AvaloniaProperty.RegisterAttached<DragDrop, InputElement, IDragSource?>("DragHandler");

        public static readonly AttachedProperty<IDropTarget?> DropHandlerProperty =
            AvaloniaProperty.RegisterAttached<DragDrop, Interactive, IDropTarget?>("DropHandler");

        public static readonly AttachedProperty<IDataTemplate?> DragAdornerTemplateProperty =
            AvaloniaProperty.RegisterAttached<DragDrop, InputElement, IDataTemplate?>("DragAdornerTemplate");

        public static readonly AttachedProperty<Point> DragAdornerTranslationProperty =
            AvaloniaProperty.RegisterAttached<DragDrop, InputElement, Point>("DragAdornerTranslation");

        public static bool GetIsDragSource(InputElement e) => e.GetValue(IsDragSourceProperty);
        public static void SetIsDragSource(InputElement e, bool value) => e.SetValue(IsDragSourceProperty, value);

        public static bool GetIsDropTarget(Interactive e) => e.GetValue(IsDropTargetProperty);
        public static void SetIsDropTarget(Interactive e, bool value) => e.SetValue(IsDropTargetProperty, value);

        public static IDragSource? GetDragHandler(InputElement e) => e.GetValue(DragHandlerProperty);
        public static void SetDragHandler(InputElement e, IDragSource? value) => e.SetValue(DragHandlerProperty, value);

        public static IDropTarget? GetDropHandler(InputElement e) => e.GetValue(DropHandlerProperty);
        public static void SetDropHandler(InputElement e, IDropTarget? value) => e.SetValue(DropHandlerProperty, value);

        public static IDataTemplate? GetDragAdornerTemplate(InputElement e) => e.GetValue(DragAdornerTemplateProperty);
        public static void SetDragAdornerTemplate(InputElement e, IDataTemplate? value) => e.SetValue(DragAdornerTemplateProperty, value);

        public static Point GetDragAdornerTranslation(InputElement e) => e.GetValue(DragAdornerTranslationProperty);
        public static void SetDragAdornerTranslation(InputElement e, Point value) => e.SetValue(DragAdornerTranslationProperty, value);

        static DragDrop()
        {
            IsDragSourceProperty.Changed.AddClassHandler<InputElement>(OnIsDragSourcePropertyChanged);
            IsDropTargetProperty.Changed.AddClassHandler<Interactive>(OnIsDropTargetPropertyChanged);
        }

        private static void OnIsDragSourcePropertyChanged(InputElement sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.GetNewValue<bool>())
            {
                sender.PointerMoved += DragSource_PointerMoved;
            }
            else
            {
                sender.PointerMoved -= DragSource_PointerMoved;
            }
        }

        private static async void DragSource_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (sender is not InputElement inputElement)
            {
                return;
            }

            var properties = e.GetCurrentPoint(inputElement).Properties;
            if (!properties.IsLeftButtonPressed)
            {
                return;
            }

            var handler = GetDragHandler(inputElement);
            if (handler == null)
            {
                return;
            }

            var result = handler.StartDrag(e);
            if (result == null)
            {
                return;
            }

            // supports adorners
            var adornerInfo = StartupAdorner(inputElement, e, result);


            await Input.DragDrop.DoDragDrop(e, result.Data, result.AllowedEffects);

            if (adornerInfo != null)
            {
                StopAdorner(adornerInfo);
            }
        }

        

        private static AdornerInfo? StartupAdorner(InputElement sender, PointerEventArgs e, DragStartResult dragInfo)
        {
            IDataTemplate? adornerTemplate = GetDragAdornerTemplate(sender);
            if (adornerTemplate == null)
            {
                return null;
            }
            var window = (sender as Window) ?? sender.FindAncestorOfType<Window>();
            if (sender == null || window == null)
            {
                return null;
            }
            var adornerLayer = AdornerLayer.GetAdornerLayer(sender);
            if (adornerLayer == null)
            {
                return null;
            }

            var thingToBeDrawn = GetThingToBeDrawn(dragInfo, adornerTemplate);
            
            adornerLayer.Children.Add(thingToBeDrawn);
            AdornerLayer.SetAdorner(window, thingToBeDrawn);

            var adornerOffset = GetDragAdornerTranslation(sender);
            var adornerPosition = e.GetPosition(window) + adornerOffset;
            thingToBeDrawn.RenderTransform = new TranslateTransform(adornerPosition.X, adornerPosition.Y);
            var originalWindowAllowDrop = window.GetValue(Input.DragDrop.AllowDropProperty);
            _info = new AdornerInfo(adornerLayer, window, thingToBeDrawn, adornerPosition, originalWindowAllowDrop, adornerOffset);

            
            window.SetValue(Input.DragDrop.AllowDropProperty, true);
            // set the DragOver handler on both to prevent flickering
            window.AddHandler(Input.DragDrop.DragOverEvent, Window_DragOver);
            window.AddHandler(Input.DragDrop.DragEnterEvent, Window_DragOver);
            return _info;
        }

        private static Control GetThingToBeDrawn(DragStartResult dragInfo, IDataTemplate adornerTemplate)
        {
            Control result;
            var customData = dragInfo.Data.Get(dragInfo.DataFormat);
            if (customData is IEnumerable enumerable and not string)
            {
                var items = enumerable.Cast<object>().ToList();
                //var maxItemsCount = DragDrop.TryGetDragPreviewMaxItemsCount(dragInfo, sender);

                result = new ItemsControl
                {
                    ItemsSource = items,
                    ItemTemplate = adornerTemplate,
                };

                // The ItemsControl doesn't display unless we create a grid to contain it.
                //var grid = new Grid();
                //grid.Children.Add(itemsControl);
                //adornment = grid;
            }
            else
            {
                result = new ContentPresenter
                {
                    ContentTemplate = adornerTemplate,
                    Content = customData
                };
            }
            result.IsHitTestVisible = false;
            result.Tag = dragInfo;
            return result;
        }

        private static AdornerInfo? _info;

        private record AdornerInfo(
            AdornerLayer AdornerLayer, 
            Interactive Window, 
            Control ThingToBeDrawn,
            Point Start,
            bool WindowOrignalAllowDrop,
            Point AdornerOffset);

        private static void Window_DragOver(object? sender, DragEventArgs e)
        {
            OnDragMove(e);
            e.DragEffects = DragDropEffects.None;
        }

        private static void OnDragMove(DragEventArgs e)
        {
            if (_info == null)
            {
                return;
            }

            var adornerPosition = e.GetPosition(_info.Window) + _info.AdornerOffset;

            _info.ThingToBeDrawn.RenderTransform = new TranslateTransform(adornerPosition.X, adornerPosition.Y);

        }

        private static void StopAdorner(AdornerInfo info)
        {
            _info = null;
            AdornerLayer.SetAdorner(info.Window, null);
            info.AdornerLayer.Children.Clear();

            if (!info.WindowOrignalAllowDrop)
            {
                info.Window.SetValue(Input.DragDrop.AllowDropProperty, false);
            }
            info.Window.RemoveHandler(Input.DragDrop.DragOverEvent, Window_DragOver);
            info.Window.RemoveHandler(Input.DragDrop.DragEnterEvent, Window_DragOver);
        }

        private static void OnIsDropTargetPropertyChanged(Interactive sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.GetNewValue<bool>())
            {
                sender.SetValue(Input.DragDrop.AllowDropProperty, true);
                sender.AddHandler(Input.DragDrop.DragOverEvent, DragOver);
                sender.AddHandler(Input.DragDrop.DragEnterEvent, DragOver);
                sender.AddHandler(Input.DragDrop.DropEvent, Drop);
            }
            else
            {
                sender.SetValue(Input.DragDrop.AllowDropProperty, false);
                // set the DragOver handler on both to prevent flickering
                sender.RemoveHandler(Input.DragDrop.DragOverEvent, DragOver);
                sender.RemoveHandler(Input.DragDrop.DragEnterEvent, DragOver);
                sender.RemoveHandler(Input.DragDrop.DropEvent, Drop);
            }
        }

        private static void DragOver(object? sender, DragEventArgs e)
        {
            if (sender is not InputElement inputElement)
            {
                return;
            }

            var handler = GetDropHandler(inputElement);
            if (handler == null)
            {
                return;
            }

            handler.DragOver(e);
            
            OnDragMove(e);
            e.Handled = true;
        }

        private static void Drop(object? sender, DragEventArgs e)
        {
            if (sender is not InputElement inputElement)
            {
                return;
            }

            var handler = GetDropHandler(inputElement);
            if (handler == null)
            {
                return;
            }
            handler.Drop(e);
        }
    }
}
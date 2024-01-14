using Avalonia.DragDrop;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShowCase.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string Greeting => "Welcome to Avalonia.DragDrop!";

    public MainWindowViewModel()
    {
        FileDropHandler = new FileDropHandler();
        FileDropHandler.FilesDropped += FileDropHandler_FilesDropped;
        TextDropHandler = new TextDropHandler();
        TextDropHandler.TextDropped += (s, t) => TextSourceC = t;
    }

    private string _droppedFile = "Drop file here";
    public string DroppedFile
    {
        get => _droppedFile;
        set => RaiseAndSetIfChanged(ref _droppedFile, value);
    }

    private void FileDropHandler_FilesDropped(object? sender, IEnumerable<IStorageItem> e)
    {
        DroppedFile = e.FirstOrDefault()?.TryGetLocalPath() ?? "We're on web / mobile?";
    }

    public FileDropHandler FileDropHandler { get; }


    public string TextSourceA => "A";
    public TextDragHandler TextDragHandlerA { get; } = new TextDragHandler("A");
    public string TextSourceB => "B";
    public TextDragHandler TextDragHandlerB { get; } = new TextDragHandler("B");

    private string _textSourceC = "C";
    public string TextSourceC
    {
        get => _textSourceC;
        set => RaiseAndSetIfChanged(ref _textSourceC, value);
    }

    public TextDropHandler TextDropHandler { get; }
}


public class FileDropHandler : IDropTarget
{
    public DragDropEffects DragOver(DragEventArgs dropInfo)
    {
        var files = dropInfo.Data.GetFiles();
        if (files == null)
        {
            return DragDropEffects.None;
        }
        return DragDropEffects.Copy;
    }

    public void Drop(DragEventArgs dropInfo)
    {
        var files = dropInfo.Data.GetFiles();
        if (files == null) 
        { 
            return; 
        }
        FilesDropped?.Invoke(this, files);
    }

    public event EventHandler<IEnumerable<IStorageItem>>? FilesDropped;
}

public class TextDragHandler(string text) : IDragSource
{
    public DragStartResult? StartDrag(PointerEventArgs dragInfo)
    {
        var data = new DataObject();
        data.Set(DataFormats.Text, text);
        return new DragStartResult(data, DragDropEffects.Copy);
    }
}

public class TextDropHandler : IDropTarget
{
    public EventHandler<string>? TextDropped;

    public DragDropEffects DragOver(DragEventArgs dropInfo)
    {
        var text = dropInfo.Data.GetText();
        if (text == null)
        {
            return DragDropEffects.None;
        }
        return DragDropEffects.Copy;
    }

    public void Drop(DragEventArgs dropInfo)
    {
        var text = dropInfo.Data.GetText();
        if (text == null)
        {
            return;
        }
        TextDropped?.Invoke(this, text);
    }
}
using Avalonia.DragDrop;
using Avalonia.Input;
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
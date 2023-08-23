using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShowCase.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    protected void RaiseAllPropertiesChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    protected bool RaiseAndSetIfChanged<T>(ref T property, T newValue, [CallerMemberName] string? name = null)
    {
        if (!EqualityComparer<T>.Default.Equals(property, newValue))
        {
            property = newValue;
            RaisePropertyChanged(name);
            return true;
        }
        return false;
    }
}

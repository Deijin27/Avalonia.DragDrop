﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ShowCase.ViewModels;

public class RelayCommand : ICommand
{
    public event EventHandler? CanExecuteChanged;

    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = async () => await execute();
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    public void Execute(object? parameter)
    {
        _execute();
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

}

public class RelayCommand<T> : ICommand
{
    public event EventHandler? CanExecuteChanged;

    private readonly Action<T?> _execute;
    private readonly Predicate<T?>? _canExecute;

    public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public RelayCommand(Func<T?, Task> execute, Predicate<T?>? canExecute = null)
    {
        _execute = async x => await execute(x);
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter)
    {
        if (parameter != null && parameter is not T)
        {
            throw new InvalidCastException($"In {nameof(RelayCommand)} unable to cast parameter of type {parameter.GetType().Name} to {typeof(T).Name}");
        }
        return _canExecute == null || _canExecute((T?)parameter);
    }

    public void Execute(object? parameter)
    {
        if (parameter != null && parameter is not T)
        {
            throw new InvalidCastException($"In {nameof(RelayCommand)} unable to cast parameter of type {parameter.GetType().Name} to {typeof(T).Name}");
        }
        _execute((T?)parameter);
    }

    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

}

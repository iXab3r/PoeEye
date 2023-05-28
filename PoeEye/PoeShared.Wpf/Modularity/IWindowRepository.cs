using System;
using System.Threading.Tasks;
using System.Windows;
using PoeShared.Dialogs.ViewModels;
using PoeShared.Native;

namespace PoeShared.Modularity;

public interface IWindowRepository
{
    /// <summary>
    /// Shows content in window that is handled by a separate dispatcher
    /// </summary>
    /// <param name="contentFactory"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> Show<T>(Func<T> contentFactory) where T : IWindowViewModel;
    
    Task<T> ShowWindow<T>(Func<T> windowFactory) where T : Window;
    
    /// <summary>
    /// Shows content in modal dialog window that is handled by a separate dispatcher
    /// </summary>
    /// <param name="contentFactory"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> ShowDialog<T>(Func<T> contentFactory) where T : IWindowViewModel;
}
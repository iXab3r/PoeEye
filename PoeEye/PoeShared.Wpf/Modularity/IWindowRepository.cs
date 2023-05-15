using System;
using System.Threading.Tasks;
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
    Task<IWindowViewController> Show<T>(Func<T> contentFactory) where T : IWindowViewModel;
    
    /// <summary>
    /// Shows content in modal dialog window that is handled by a separate dispatcher
    /// </summary>
    /// <param name="contentFactory"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task ShowDialog<T>(Func<T> contentFactory) where T : IWindowViewModel;
}
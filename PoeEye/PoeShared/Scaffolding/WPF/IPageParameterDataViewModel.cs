using System.Windows.Input;
using DynamicData.Operators;
using JetBrains.Annotations;

namespace PoeShared.Scaffolding.WPF
{
    public interface IPageParameterDataViewModel
    {
        ICommand NextPageCommand { [NotNull] get; }
        ICommand PreviousPageCommand { [NotNull] get; }
        int TotalCount { get; }
        int PageCount { get; }
        int CurrentPage { get; }
        int PageSize { get; set; }

        void Update([NotNull] IPageResponse response);
    }
}
using System.Windows.Input;
using DynamicData.Operators;
using Guards;
using Prism.Commands;
using ReactiveUI;

namespace PoeShared.Scaffolding.WPF
{
    public sealed class PageParameterDataViewModel : DisposableReactiveObject, IPageParameterDataViewModel
    {
        private readonly DelegateCommand nextPageCommand;
        private readonly DelegateCommand previousPageCommand;
        private int currentPage;
        private int pageCount;
        private int pageSize;
        private int totalCount;

        public PageParameterDataViewModel(int currentPage, int pageSize)
        {
            this.currentPage = currentPage;
            this.pageSize = pageSize;

            nextPageCommand = new DelegateCommand(() => CurrentPage = CurrentPage + 1, () => CurrentPage < PageCount);
            previousPageCommand = new DelegateCommand(() => CurrentPage = CurrentPage - 1, () => CurrentPage > 1);
        }

        public ICommand NextPageCommand => nextPageCommand;

        public ICommand PreviousPageCommand => previousPageCommand;

        public int TotalCount
        {
            get => totalCount;
            set => this.RaiseAndSetIfChanged(ref totalCount, value);
        }

        public int PageCount
        {
            get => pageCount;
            set => this.RaiseAndSetIfChanged(ref pageCount, value);
        }

        public int CurrentPage
        {
            get => currentPage;
            set => this.RaiseAndSetIfChanged(ref currentPage, value);
        }

        public int PageSize
        {
            get => pageSize;
            set => this.RaiseAndSetIfChanged(ref pageSize, value);
        }

        public void Update(IPageResponse response)
        {
            Guard.ArgumentNotNull(response, nameof(response));
            
            CurrentPage = response.Page;
            PageSize = response.PageSize;
            PageCount = response.Pages;
            TotalCount = response.TotalSize;
            nextPageCommand.RaiseCanExecuteChanged();
            previousPageCommand.RaiseCanExecuteChanged();
        }
    }
}
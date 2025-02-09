using System.Reactive;
using System.Reactive.Linq;
using HotReloadDemo.Models;
using HotReloadDemo.Services;
using ReactiveUI;

namespace HotReloadDemo.ViewModels;

public class MainViewModel : ViewModelBase
{
    private object _content;

    public MainViewModel()
        : this(new FakeToDoItemProvider())
    {
    }

    public MainViewModel(IToDoItemProvider provider)
    {
        ToDoList = new(provider.GetItems());
        _content = ToDoList;
    }

    public ToDoListViewModel ToDoList { get; }

    public object Content
    {
        get => _content;
        private set => this.RaiseAndSetIfChanged(ref _content, value);
    }

    public void AddItem()
    {
        AddItemViewModel addItemViewModel = new();

        Observable.Merge(
            addItemViewModel.OkCommand,
            addItemViewModel.CancelCommand.Select(_ => default(ToDoItem)))
            .Take(1)
            .Subscribe(Observer.Create<ToDoItem?>(newItem =>
            {
                if (newItem is not null)
                    ToDoList.ToDoItems.Add(newItem);

                Content = ToDoList;
            }));

        Content = addItemViewModel;
    }
}

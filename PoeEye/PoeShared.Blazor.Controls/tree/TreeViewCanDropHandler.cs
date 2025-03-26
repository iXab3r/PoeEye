namespace PoeShared.Blazor.Controls;

public delegate bool TreeViewCanDropHandler<TItem>(TreeViewNode<TItem>? dragItem, TreeViewNode<TItem> targetItem);
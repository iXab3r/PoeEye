using System;
using System.Collections.Generic;
using System.Linq;
using PoeShared.Scaffolding;

namespace PoeShared.UI;

public static class TreeViewExtensions
{
    private static readonly Predicate<ITreeViewItemViewModel> TRUE = model => true;

    public static ITreeViewItemViewModel FindRoot(this ITreeViewItemViewModel node)
    {
        var result = node;
        while (result.Parent != null)
        {
            result = result.Parent;
        }
        return result;
    }

    public static IEnumerable<T> FindChildrenOfType<T>(this ITreeViewItemViewModel instance) where T : ITreeViewItemViewModel
    {
        var children = instance is IDirectoryTreeViewItemViewModel dir
            ? (IEnumerable<ITreeViewItemViewModel>)dir.Children
            : instance.Parent?.Children;
        return children == null ? ArraySegment<T>.Empty : FindChildren(x => x is T, children).OfType<T>();
    }

    public static IEnumerable<T> FindParentsOfType<T>(this ITreeViewItemViewModel instance) where T : ITreeViewItemViewModel
    {
        return FindParents(instance, x => x is T, instance).OfType<T>();
    }

    public static void ExpandParentDirectories(this ITreeViewItemViewModel instance)
    {
        instance.FindParentsOfType<IDirectoryTreeViewItemViewModel>().Where(x => x.IsExpanded == false).ForEach(x => x.IsExpanded = true);
    }

    public static IEnumerable<ITreeViewItemViewModel> FindParents(this ITreeViewItemViewModel instance, Predicate<ITreeViewItemViewModel> predicate, ITreeViewItemViewModel node)
    {
        var root = node;
        while (root != null)
        {
            if (predicate(root))
            {
                yield return root;
            }

            root = root.Parent;
        }
    }

    public static IEnumerable<ITreeViewItemViewModel> FindChildren(this ITreeViewItemViewModel instance, Predicate<ITreeViewItemViewModel> predicate)
    {
        return FindChildren(predicate, instance.Children);
    }

    private static IEnumerable<ITreeViewItemViewModel> FindChildren(Predicate<ITreeViewItemViewModel> predicate,
        IEnumerable<ITreeViewItemViewModel> items)
    {
        foreach (var node in items.EmptyIfNull())
        {
            if (node is IDirectoryTreeViewItemViewModel directoryNode)
            {
                var matchingChildNodes = FindChildren(predicate, directoryNode.Children);
                foreach (var treeViewItemViewModel in matchingChildNodes)
                {
                    yield return treeViewItemViewModel;
                }
            }

            if (predicate(node))
            {
                yield return node;
            }
        }
    }
}
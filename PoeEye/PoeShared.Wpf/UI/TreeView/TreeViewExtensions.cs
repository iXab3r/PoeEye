using System;
using System.Collections.Generic;
using System.Linq;

namespace PoeShared.UI.TreeView
{
    public static class TreeViewExtensions
    {
        private static readonly Predicate<ITreeViewItemViewModel> TRUE = model => true;

        public static IEnumerable<T> FindChildrenOfType<T>(this ITreeViewItemViewModel instance) where T : ITreeViewItemViewModel
        {
            return FindChildren(instance, x => x is T, instance.Children).OfType<T>();
        }

        public static IEnumerable<T> FindParentsOfType<T>(this ITreeViewItemViewModel instance) where T : ITreeViewItemViewModel
        {
            return FindParents(instance, x => x is T, instance).OfType<T>();
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
            return FindChildren(instance, predicate, instance.Children);
        }

        public static IEnumerable<ITreeViewItemViewModel> FindParents(this ITreeViewItemViewModel instance)
        {
            return instance.FindParents(TRUE, instance);
        }

        public static IEnumerable<ITreeViewItemViewModel> FindChildren(this ITreeViewItemViewModel instance,
            Predicate<ITreeViewItemViewModel> predicate,
            IEnumerable<ITreeViewItemViewModel> items)
        {
            foreach (var node in items)
            {
                if (node is IDirectoryTreeViewItemViewModel directoryNode)
                {
                    var matchingChildNodes = FindChildren(instance, predicate, directoryNode.Children);
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
}
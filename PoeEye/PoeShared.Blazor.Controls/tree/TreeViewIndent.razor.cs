// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Components;

namespace PoeShared.Blazor.Controls;

public partial class TreeViewIndent<TItem> : BlazorReactiveComponent
{
    [CascadingParameter(Name = "Tree")]
    public TreeView<TItem> TreeComponent { get; set; }

    [CascadingParameter(Name = "SelfNode")]
    public TreeViewNode<TItem> SelfNode { get; set; }

    [Parameter] public int TreeLevel { get; set; }
    
    private static TreeViewNode<TItem> GetParentNode(TreeViewNode<TItem> node, int level)
    {
        if (level > 0 && node.ParentNode != null)
        {
            return GetParentNode(node.ParentNode, level - 1);
        }

        return node;
    }
}
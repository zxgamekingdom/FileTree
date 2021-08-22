using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

namespace FileTree.Library
{
    public static class FileSystemNodeExtensions
    {
        /// <summary>
        /// 获取所有子节点(包括子节点的子节点)
        /// </summary>
        public static ReadOnlyCollection<FileSystemNode>? GetAllChildren(
            this FileSystemNode node)
        {
            if (node.Info is not DirectoryInfo) return default;
            var stack = new Stack<FileSystemNode>();
            var list = new List<FileSystemNode>();
            stack.Push(node);
            while (stack.Count != 0)
            {
                FileSystemNode pop = stack.Pop();
                if (pop.Children == null) continue;
                ReadOnlyCollection<FileSystemNode> children = pop.Children;
                list.AddRange(children);
                foreach (FileSystemNode child in children)
                {
                    stack.Push(child);
                }
            }

            return list.AsReadOnly();
        }

        public static bool IsRoot(this FileSystemNode node)
        {
            return node == null ?
                throw new ArgumentNullException(nameof(node)) :
                node.Tree?.Root == node;
        }

        ///  <summary>
        ///  获取父节点
        ///  </summary>
        ///  <param name="node"></param>
        ///  <param name="hierarchy">
        /// 必须大于等于零,如果为零则返回自己
        ///  </param>
        ///  <exception cref="ArgumentOutOfRangeException"><see cref="hierarchy"/>必须大于零</exception>
        public static FileSystemNode? GetParent(this FileSystemNode node, int hierarchy)
        {
            switch (hierarchy)
            {
                case 0:
                    return node;
                case < 0:
                    throw new ArgumentOutOfRangeException(nameof(hierarchy));
            }

            FileSystemNode? buff = node.Parent;
            for (int i = 1; i < hierarchy; i++)
            {
                buff = buff?.Parent;
            }

            return buff;
        }

        /// <summary>
        /// 获取顶级父节点
        /// </summary>
        /// <param name="node"></param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
        public static FileSystemNode? GetTopParent(this FileSystemNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            FileSystemNode? buff = node.Parent;
            switch (buff)
            {
                case null:
                    return default;
                default:
                    while (buff.Parent is not null)
                    {
                        buff = buff.Parent;
                    }

                    return buff;
            }
        }

        /// <summary>
        /// 获取所有父节点
        /// </summary>
        /// <param name="node"></param>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is <c>null</c>.</exception>
        public static ReadOnlyCollection<FileSystemNode>? GetAllParent(
            this FileSystemNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            FileSystemNode? buff = node.Parent;
            switch (buff)
            {
                case null:
                    return default;
                default:
                    var nodes = new List<FileSystemNode>();
                    while (buff.Parent is not null)
                    {
                        nodes.Add(buff);
                        buff = buff.Parent;
                    }

                    nodes.Add(buff);
                    return nodes.AsReadOnly();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace FileTree.Library
{
    public partial class FileSystemTree
    {
        private readonly Dictionary<int, List<Node?>?> _dictionary = new();

        public FileSystemTree(DirectoryInfo baseDirectoryInfo,
            CancellationToken token = default)
        {
            BaseDirectoryInfo = baseDirectoryInfo;
            UpdateTree(token);
            UpdateNodeIndex(token);
        }

        /// <summary>
        /// 文件树中的所有节点
        /// </summary>
        public ReadOnlyCollection<FileSystemNode> All
        {
            get
            {
                var fileNodes = new List<FileSystemNode>();
                foreach ((int _, List<Node?>? value) in _dictionary)
                {
                    IEnumerable<FileSystemNode> enumerable =
                        from node in value
                        where node != null
                        let a = node.GetFileNode()
                        select a;
                    fileNodes.AddRange(enumerable);
                }

                return fileNodes.AsReadOnly();
            }
        }

        public DirectoryInfo BaseDirectoryInfo { get; }

        /// <summary>
        /// 文件树的层级数量
        /// </summary>
        public int HierarchyCount => _dictionary.Keys.Count;

        public FileSystemNode? this[int hierarchy, int index] =>
            GetNode(hierarchy, index)?.GetFileNode();

        public ReadOnlyCollection<FileSystemNode>? this[int hierarchy]
        {
            get
            {
                return hierarchy switch
                {
                    < 0 => throw new ArgumentOutOfRangeException(nameof(hierarchy)),
                    _ => GetNodes(hierarchy)
                        ?.Select(node => node!.GetFileNode())
                        .ToList()
                        .AsReadOnly()
                };
            }
        }

        /// <summary>
        /// 获取指定节点的直接子节点
        /// </summary>
        /// <param name="systemNode"></param>
        /// <exception cref="InvalidOperationException">不能获取非本树节点的子节点</exception>
        public ReadOnlyCollection<FileSystemNode>? GetChildren(
            FileSystemNode systemNode)
        {
            return systemNode.Tree != this ?
                throw new InvalidOperationException("不能获取非本树节点的子节点") :
                _dictionary.TryGetValue(systemNode.Hierarchy!.Value,
                    out List<Node?>? list) ?
                    list!.Single(node1 => node1!.Info == systemNode.Info)!.Children
                        .ConvertAll(node1 => node1.GetFileNode())
                        .AsReadOnly() : default;
        }

        /// <summary>
        /// 删除指定的节点
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="token"></param>
        /// <exception cref="InvalidOperationException">不能移除非本树的节点</exception>
        /// <remarks>如果删除的节点是文件夹节点,则该节点的所有子节点均会被移除</remarks>
        public ReadOnlyCollection<FileSystemNode> RemoveNodes(
            IEnumerable<FileSystemNode> nodes,
            CancellationToken token = default)
        {
            List<FileSystemNode> buff = new();
            IEnumerable<FileSystemNode> buffNodes =
                nodes as FileSystemNode[] ?? nodes.ToArray();
            if (buffNodes.Any(node => node.Tree != this))
                throw new InvalidOperationException("不能移除非本树的节点");
            foreach (FileSystemNode node in buffNodes)
            {
                switch (node.Info)
                {
                    case DirectoryInfo:
                    {
                        List<Node>? children = GetNodeAllChildrenAndSelf(node);
                        if (children != null)
                        {
                            foreach (Node child in children)
                            {
                                _ = child.Parent?.Children.Remove(child);
                                _dictionary[child.Hierarchy!.Value]!
                                    [child.Index!.Value] = default;
                                child.RemoveTreeInfo();
                                buff.Add(child.GetFileNode());
                            }
                        }
                    }
                        break;
                    case FileInfo:
                    {
                        int hierarchy = node.Hierarchy!.Value;
                        int index = node.Index!.Value;
                        FileSystemNode? fileNode = this[hierarchy, index];
                        if (fileNode != null)
                        {
                            Node item = _dictionary[hierarchy]![index] ??
                                throw new InvalidOperationException();
                            if (item.Info == node.Info)
                            {
                                _ = item.Parent?.Children.Remove(item);
                                _dictionary[hierarchy]![index] = default;
                                item.RemoveTreeInfo();
                                buff.Add(item.GetFileNode());
                            }
                        }
                    }
                        break;
                }
            }

            RemoveNullNode(token);
            UpdateNodeIndex(token);
            return buff.AsReadOnly();
        }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            foreach ((int key, List<Node?>? value) in _dictionary)
            {
                if (value != null)
                {
                    _ = stringBuilder.AppendLine(key.ToString());
                    foreach (Node? node in value)
                    {
                        _ = stringBuilder.AppendLine(node != null ?
                            $"\t{node.GetFileNode()}" :
                            "\tnull");
                    }
                }
                else
                {
                    _ = stringBuilder.Append(key).AppendLine(" null");
                }
            }

            return stringBuilder.ToString();
        }

        private Node? GetNode(int hierarchy, int index)
        {
            if (hierarchy < 0) throw new ArgumentOutOfRangeException(nameof(hierarchy));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            List<Node?>? nodes = GetNodes(hierarchy);
            return nodes != null && index < nodes.Count ? nodes[index] : default;
        }

        private List<Node>? GetNodeAllChildrenAndSelf(FileSystemNode fileSystemNode)
        {
            if (fileSystemNode.Info is DirectoryInfo)
            {
                int hierarchy = fileSystemNode.Hierarchy!.Value;
                int index = fileSystemNode.Index!.Value;
                Node? node = GetNode(hierarchy, index);
                if (node != null && node.Info == fileSystemNode.Info)
                {
                    var stack = new Stack<Node>();
                    var list = new List<Node>() { node };
                    stack.Push(node);
                    while (stack.Count != 0)
                    {
                        Node pop = stack.Pop();
                        List<Node> children = pop.Children;
                        list.AddRange(children);
                        foreach (Node dirNode in children.Where(item =>
                            item.Info is DirectoryInfo))
                        {
                            stack.Push(dirNode);
                        }
                    }

                    return list;
                }
            }

            return default;
        }

        private List<Node?>? GetNodes(int hierarchy)
        {
            return _dictionary.TryGetValue(hierarchy, out List<Node?>? list) ?
                list :
                default;
        }

        private void RemoveNullNode(CancellationToken token)
        {
            foreach (List<Node?>? value in _dictionary.Values)
            {
                token.ThrowIfCancellationRequested();
                _ = value?.RemoveAll(node => node is null);
            }
        }

        private void UpdateNode(Node node)
        {
            switch (node.Info)
            {
                case DirectoryInfo directoryInfo:
                    try
                    {
                        DirectoryInfo[] directoryInfos = directoryInfo.GetDirectories();
                        FileInfo[] fileInfos = directoryInfo.GetFiles();
                        int length = directoryInfos.Length + fileInfos.Length;
                        var objects = new List<object>(length);
                        objects.AddRange(directoryInfos);
                        objects.AddRange(fileInfos);
                        int hierarchy = node.Hierarchy!.Value + 1;
                        Node[] array = objects
                            .Select(o => new Node(o, this, node, hierarchy))
                            .ToArray();
                        node.Children.AddRange(array);
                        if (_dictionary.TryGetValue(hierarchy, out List<Node?>? list))
                            list!.AddRange(array);
                        else
                            _dictionary.Add(hierarchy, new List<Node?>(array));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        node.IsExistsUnauthorizedAccessChildren = true;
                    }

                    break;
            }
        }

        private void UpdateNodeIndex(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            foreach ((_, List<Node?>? value) in _dictionary)
            {
                if (value != null)
                {
                    for (var index = 0; index < value.Count; index++)
                    {
                        token.ThrowIfCancellationRequested();
                        Node? node = value[index];
                        if (node != null) node.Index = index;
                    }
                }
            }

            foreach ((int key, List<Node?>? value) in _dictionary)
            {
                if (value is null || value.Count == 0)
                {
                    _ = _dictionary.Remove(key);
                }
            }
        }

        private void UpdateTree(CancellationToken token)
        {
            var rootNode = new Node(BaseDirectoryInfo, this);
            _dictionary.Add(0, new List<Node?> { rootNode });
            var count = 0;
            while (true)
            {
                token.ThrowIfCancellationRequested();
                List<Node?>? nodes = _dictionary[count];
                if (nodes!.All(node => node!.Type == FileSystemNodeType.FileInfo))
                    return;
                foreach (Node? node in nodes!) UpdateNode(node!);
                count++;
                token.ThrowIfCancellationRequested();
            }
        }
    }
}

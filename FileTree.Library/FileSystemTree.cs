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
                        let a = node.GetFileNode(this)
                        select a;
                    fileNodes.AddRange(enumerable);
                }

                return fileNodes.AsReadOnly();
            }
        }

        public DirectoryInfo BaseDirectoryInfo { get; }
        public int HierarchyCount => _dictionary.Keys.Count;

        public FileSystemNode? this[int hierarchy, int index] =>
            GetNode(hierarchy, index)?.GetFileNode(this);

        public ReadOnlyCollection<FileSystemNode>? this[int hierarchy]
        {
            get
            {
                if (hierarchy < 0)
                    throw new ArgumentOutOfRangeException(nameof(hierarchy));
                return GetNodes(hierarchy)
                    ?.Select(node => node!.GetFileNode(this))
                    .ToList()
                    .AsReadOnly();
            }
        }

        public ReadOnlyCollection<FileSystemNode>? GetChildren(
            FileSystemNode systemNode)
        {
            return
                _dictionary.TryGetValue(systemNode.Hierarchy, out List<Node?>? list) ?
                    list!.Single(node1 => node1!.Info == systemNode.Info)!.Children
                        .Select(node1 => node1.GetFileNode(this))
                        .ToList()
                        .AsReadOnly() :
                    default;
        }

        public ReadOnlyCollection<FileSystemNode> RemoveNode(
            IEnumerable<FileSystemNode> nodes,
            CancellationToken token = default)
        {
            List<FileSystemNode> buff = new();
            foreach (FileSystemNode node in nodes)
                switch (node.Info)
                {
                    case DirectoryInfo:
                        {
                            List<Node>? children = GetNodeAllChildrenAndSelf(node);
                            if (children != null)
                                foreach (Node child in children)
                                {
                                    _ = child.Parent?.Children.Remove(child);
                                    _dictionary[child.Hierarchy]![child.Index] = default;
                                    buff.Add(child.GetFileNode(this));
                                }
                        }
                        break;
                    case FileInfo:
                        {
                            int hierarchy = node.Hierarchy;
                            int index = node.Index;
                            FileSystemNode? fileNode = this[hierarchy, index];
                            if (fileNode != null)
                            {
                                Node item = _dictionary[hierarchy]![index] ??
                                    throw new InvalidOperationException();
                                if (item.Info == node.Info)
                                {
                                    _ = item.Parent?.Children.Remove(item);
                                    _dictionary[hierarchy]![index] = default;
                                    buff.Add(fileNode);
                                }
                            }
                        }
                        break;
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
                    stringBuilder.AppendLine(key.ToString());
                    foreach (Node? node in value)
                    {
                        stringBuilder.AppendLine(node != null ?
                            $"\t{node.GetFileNode(this)}" :
                            "\tnull");
                    }
                }
                else
                {
                    stringBuilder.AppendLine($"{key} null");
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
                int hierarchy = fileSystemNode.Hierarchy;
                int index = fileSystemNode.Index;
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
                        int hierarchy = node.Hierarchy + 1;
                        Node[] array = objects.Select(o => new Node(o, node, hierarchy))
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
                if (value != null)
                    for (var index = 0; index < value.Count; index++)
                    {
                        token.ThrowIfCancellationRequested();
                        Node? node = value[index];
                        if (node != null) node.Index = index;
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
            var rootNode = new Node(BaseDirectoryInfo);
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
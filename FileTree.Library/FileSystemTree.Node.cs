using System;
using System.Collections.Generic;
using System.IO;

namespace FileTree.Library
{
    public partial class FileSystemTree
    {
        private class Node
        {
            public FileSystemNode GetFileNode(FileSystemTree systemTree)
            {
                return new FileSystemNode(Info,
                    IsExistsUnauthorizedAccessChildren,
                    systemTree,
                    Parent?.GetFileNode(systemTree),
                    Hierarchy,
                    Index);
            }

            public bool IsExistsUnauthorizedAccessChildren { get; set; }

            public Node(object info, Node? parent = default, int hierarchy = 0)
            {
                if (hierarchy < 0)
                    throw new ArgumentOutOfRangeException(nameof(hierarchy));
                if (hierarchy > 0 && parent == null)
                    throw new ArgumentNullException(nameof(parent));
                Type type = info.GetType();
                if ((type == typeof(DirectoryInfo) ||
                    type == typeof(FileInfo)) is false)
                    throw new ArgumentException($@"n{nameof(info)}的类型必须是{
                        typeof(DirectoryInfo)}或者{typeof(FileInfo)}");
                Info = info ?? throw new ArgumentNullException(nameof(info));
                Parent = parent;
                Hierarchy = hierarchy;
            }

            public List<Node> Children { get; } = new();

            public string FullName =>
                Info switch
                {
                    FileInfo fileInfo => fileInfo.FullName,
                    DirectoryInfo directoryInfo => directoryInfo.FullName,
                    _ => throw new ArgumentOutOfRangeException()
                };

            public int Hierarchy { get; }
            public object Info { get; }

            public string Name =>
                Info switch
                {
                    FileInfo fileInfo => fileInfo.Name,
                    DirectoryInfo directoryInfo => directoryInfo.Name,
                    _ => throw new ArgumentOutOfRangeException()
                };

            public Node? Parent { get; }

            public FileSystemNodeType Type =>
                Info switch
                {
                    DirectoryInfo => FileSystemNodeType.DirectoryInfo,
                    FileInfo => FileSystemNodeType.FileInfo,
                    _ => throw new ArgumentOutOfRangeException()
                };

            public int Index { get; set; }
        }
    }
}

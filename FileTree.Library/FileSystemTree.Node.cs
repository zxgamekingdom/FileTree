using System;
using System.Collections.Generic;
using System.IO;

namespace FileTree.Library
{
    public partial class FileSystemTree
    {
        private class Node
        {
            public Node(object info,
                FileSystemTree tree,
                Node? parent = default,
                int hierarchy = 0)
            {
                if (hierarchy < 0)
                    throw new ArgumentOutOfRangeException(nameof(hierarchy));
                if (hierarchy > 0 && parent == null)
                    throw new ArgumentNullException(nameof(parent));
                Type type = info.GetType();
                if ((type == typeof(DirectoryInfo) ||
                    type == typeof(FileInfo)) is false)
                {
                    throw new ArgumentException($@"n{nameof(info)}的类型必须是{
                        typeof(DirectoryInfo)}或者{typeof(FileInfo)}");
                }

                Info = info ?? throw new ArgumentNullException(nameof(info));
                Tree = tree;
                Parent = parent;
                Hierarchy = hierarchy;
            }

            public List<Node> Children { get; } = new();

            public int? Hierarchy { get; private set; }
            public int? Index { get; set; }
            public object Info { get; }
            public bool IsExistsUnauthorizedAccessChildren { get; set; }
            public Node? Parent { get; private set; }

            public FileSystemNodeType Type =>
                Info switch
                {
                    DirectoryInfo => FileSystemNodeType.DirectoryInfo,
                    FileInfo => FileSystemNodeType.FileInfo,
                    _ => throw new ArgumentOutOfRangeException()
                };

            public FileSystemNode GetFileNode()
            {
                return new FileSystemNode(Info,
                    IsExistsUnauthorizedAccessChildren,
                    Tree,
                    Parent?.GetFileNode(),
                    Hierarchy,
                    Index);
            }

            public FileSystemTree? Tree { get; private set; }

            public void RemoveTreeInfo()
            {
                Tree = default;
                Parent = default;
                Hierarchy = default;
                Index = default;
            }
        }
    }
}

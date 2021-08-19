using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;
using System.Collections.ObjectModel;
using System.IO;

namespace FileTree.Library
{
    public class FileSystemNode
    {
        public FileSystemNode(object info,
            bool isExistsUnauthorizedAccessChildren,
            FileSystemTree? tree = null,
            FileSystemNode? parent = default,
            int hierarchy = 0,
            int index = 0)
        {
            switch (hierarchy)
            {
                case < 0:
                    throw new ArgumentOutOfRangeException(nameof(hierarchy));
                case > 0 when parent == null:
                    throw new ArgumentNullException(nameof(parent));
            }

            Type type = info.GetType();
            if ((type == typeof(DirectoryInfo) || type == typeof(FileInfo)) is false)
                throw new ArgumentException($@"n{nameof(info)}的类型必须是{
                    typeof(DirectoryInfo)}或者{typeof(FileInfo)}");
            Tree = tree;
            Info = info ?? throw new ArgumentNullException(nameof(info));
            IsExistsUnauthorizedAccessChildren = isExistsUnauthorizedAccessChildren;
            Parent = parent;
            Hierarchy = hierarchy;
            Index = index;
        }
        public ReadOnlyCollection<FileSystemNode>? Children => Tree?.GetChildren(this);
        public string FullName =>
            Info switch
            {
                FileInfo fileInfo => fileInfo.FullName,
                DirectoryInfo directoryInfo => directoryInfo.FullName,
                _ => throw new ArgumentOutOfRangeException()
            };
        public int Hierarchy { get; }
        public int Index { get; }
        [JsonIgnore]
        public object Info { get; }
        public bool IsExistsUnauthorizedAccessChildren { get; }
        public string Name =>
            Info switch
            {
                FileInfo fileInfo => fileInfo.Name,
                DirectoryInfo directoryInfo => directoryInfo.Name,
                _ => throw new ArgumentOutOfRangeException()
            };

        [JsonIgnore]
        public FileSystemNode? Parent { get; }

        [JsonIgnore]
        public FileSystemTree? Tree { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FileSystemNodeType Type =>
            Info switch
            {
                DirectoryInfo => FileSystemNodeType.DirectoryInfo,
                FileInfo => FileSystemNodeType.FileInfo,
                _ => throw new ArgumentOutOfRangeException()
            };

        public override string ToString() =>
            $"{nameof(Name)}: {Name}, {nameof(Type)}: {Type}, {nameof(Hierarchy)}: {Hierarchy}, {nameof(Index)}: {Index}, {nameof(FullName)}: {FullName}, {nameof(IsExistsUnauthorizedAccessChildren)}: {IsExistsUnauthorizedAccessChildren}";
    }
}
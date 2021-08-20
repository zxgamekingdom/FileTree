using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileTree.Library;

namespace Test
{
    internal static class Program
    {
        private static void Main()
        {
            var tree = new FileSystemTree(new DirectoryInfo(
                @"C:\Users\Taurus Zhou\.nuget\packages\fody\6.5.1\netstandardtask"));
            tree.HierarchyCount.WriteLine();
            IEnumerable<FileSystemNode> remove = tree.All.Where(node =>
                node.Hierarchy == tree.HierarchyCount - 1 && node.Index == 1);
            FileSystemNode[] buff = remove as FileSystemNode[] ?? remove.ToArray();
            foreach (FileSystemNode fileSystemNode in buff)
            {
                fileSystemNode.WriteLine(ConsoleColor.Red);
            }

            foreach (FileSystemNode fileSystemNode in tree.RemoveNodes(buff))
            {
                fileSystemNode.WriteLine(ConsoleColor.Cyan);
                $"GetTopParent {fileSystemNode.GetTopParent()}".WriteLine(ConsoleColor
                    .Yellow);
                $"GetParent    {fileSystemNode.GetParent(1)}".WriteLine(ConsoleColor
                    .Yellow);
            }

            tree.ToString().WriteLine();
            $"GetTopParent {tree[1, 0]?.GetTopParent()}"
                .WriteLine(ConsoleColor.Yellow);
            $"GetParent    {tree[1, 0]?.GetParent(1)}".WriteLine(ConsoleColor.Yellow);
            _ = Console.ReadKey();
        }
    }
}

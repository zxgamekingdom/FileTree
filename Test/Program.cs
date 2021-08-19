using FileTree.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Test
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var tree = new FileSystemTree(
                new DirectoryInfo(
                    $@"C:\Users\Taurus Zhou\.nuget\packages\fody\6.5.1\build"));
            FileSystemNode single = tree.All.Single(node =>
                node.Name is "Fody.targets");
            tree[0, 0].Name.WriteLine();
            single.WriteLine();
            List<FileSystemNode> fileSystemNodes = single.GetAllParent();
            if (fileSystemNodes != null)
                foreach (FileSystemNode node in fileSystemNodes)
                {
                    node.Name.WriteLine();
                }

            Console.ReadKey();
        }
    }
}

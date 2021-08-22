using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;
using static FileTree.Library.TestProject.FileSystemTreeUnitTestCollectionFixture;
using static Xunit.Assert;

namespace FileTree.Library.TestProject
{
    [Collection(nameof(FileSystemTreeUnitTestCollectionFixture))]
    public class FileSystemNodeExtensionsTest
    {
        private FileSystemTree _tree;
        private FileSystemTreeUnitTestClassFixture _fixture;

        public FileSystemNodeExtensionsTest(FileSystemTreeUnitTestClassFixture fixture)
        {
            _fixture = fixture;
            _tree = fixture.CreateTree();
        }

        [Fact]
        public void Test_GetAllChildren()
        {
            ReadOnlyCollection<FileSystemNode>? allChildren =
                (_tree[0, 0] ?? throw new InvalidOperationException()).GetAllChildren();
            ReadOnlyCollection<FileSystemNode> treeAll = _tree.All;
            Equal(treeAll.Count - 1, allChildren!.Count);
            FileSystemNode[] union = allChildren.Union(treeAll).ToArray();
            Equal(union.Length, treeAll.Count);
            FileSystemNode[] except = union.Except(treeAll).ToArray();
            Empty(except);
        }

        [Fact]
        public void Test_GetParent()
        {
            FileSystemNode? root = _tree[0, 0];
            FileSystemNode node = root!.Children![0];
            Equal(node.GetParent(0), node);
            Equal(node.GetParent(1), root);
            _ = Throws<ArgumentOutOfRangeException>(() => node.GetParent(-1));
            Equal(_tree[2]![0]!.GetParent(2), root);
        }

        [Fact]
        public void Test_GetTopParent()
        {
            {
                List<FileSystemNode> all = _tree.All.ToList();
                _ = all.Remove(_tree[0, 0]!);
                FileSystemNode root = _tree[0, 0]!;
                foreach (FileSystemNode node in all)
                {
                    Equal(node.GetTopParent(), root);
                }
            }
            {
                var node = new FileSystemNode(new DirectoryInfo(_fixture.BaseDirPath),
                    false);
                Null(node.GetTopParent());
            }
            {
                FileSystemNode node = _tree[0, 0]!;
                Null(node.GetTopParent());
            }
        }

        [Fact]
        public void Test_GetAllParent()
        {
            ReadOnlyCollection<FileSystemNode> all = _tree.All;
            FileSystemNode single = all.Single(node =>
                node.Name == "FileSystemTreeUnitTest.0.1.2.2.txt");
            List<FileSystemNode> allParent = single.GetAllParent()!.ToList();
            True(allParent.Exists(node => node.Name == "FileSystemTreeUnitTest"));
            True(allParent.Exists(node => node.Name == "FileSystemTreeUnitTest.0"));
            True(allParent.Exists(node => node.Name == "FileSystemTreeUnitTest.0.1"));
            True(allParent.Exists(node => node.Name == "FileSystemTreeUnitTest.0.1.2"));
            False(allParent.Exists(node =>
                node.Name == "FileSystemTreeUnitTest.0.1.2.2.txt"));
            Null(_tree[0, 0]!.GetAllParent());
        }

        [Fact]
        public void Test_IsRoot()
        {
            List<FileSystemNode> list = _tree.All.ToList();
            _ = list.Remove(_tree[0, 0]!);
            True(_tree[0, 0]!.IsRoot());
            foreach (FileSystemNode fileSystemNode in list)
                False(fileSystemNode.IsRoot());
        }
    }
}

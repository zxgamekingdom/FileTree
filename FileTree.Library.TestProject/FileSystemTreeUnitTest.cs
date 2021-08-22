using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;
using static Xunit.Assert;

namespace FileTree.Library.TestProject
{
    [Collection(nameof(FileSystemTreeUnitTestCollectionFixture))]
    public class FileSystemTreeUnitTest
    {
        private readonly
            FileSystemTreeUnitTestCollectionFixture.FileSystemTreeUnitTestClassFixture
            _fixture;

        private readonly FileSystemTree _tree;

        public FileSystemTreeUnitTest(
            FileSystemTreeUnitTestCollectionFixture.FileSystemTreeUnitTestClassFixture
                fixture)
        {
            _fixture = fixture;
            _tree = fixture.CreateTree();
        }

        [Fact]
        public void Test_All()
        {
            ReadOnlyCollection<FileSystemNode> all = _tree.All;
            int fileCount = all.Count(node => node.Info is FileInfo);
            int dirCount = all.Count(node => node.Info is DirectoryInfo);
            Equal(121, all.Count);
            Equal(81, fileCount);
            Equal(40, dirCount);
        }

        [Theory]
        [InlineData("FileSystemTreeUnitTest.1.1.1.1.txt")]
        [InlineData("FileSystemTreeUnitTest.1.2.0.0.txt")]
        [InlineData("FileSystemTreeUnitTest.1.1.2.1.txt")]
        [InlineData("FileSystemTreeUnitTest.0.1.1.1.txt")]
        public void Test_FileInfoNode(string fileName)
        {
            ReadOnlyCollection<FileSystemNode> all = _tree.All;
            FileSystemNode node = Single(all.Where(node => node.Name == fileName));
            Equal(node.Tree, _tree);
            NotNull(node.Parent);
            Equal(4, node.Hierarchy);
            Equal(FileSystemNodeType.FileInfo, node.Type);
        }

        [Theory]
        [InlineData(-1, default, typeof(ArgumentOutOfRangeException))]
        [InlineData(0, -1, typeof(ArgumentOutOfRangeException))]
        [InlineData(1, 1, typeof(ArgumentNullException))]
        public void Test_FileSystemNodeCtor1(int? hierarchy, int? index, Type exception)
        {
            _ = Throws(exception,
                () => new FileSystemNode(new DirectoryInfo(_fixture.BaseDirPath),
                    false,
                    default,
                    default,
                    hierarchy,
                    index));
        }

        [Fact]
        public void Test_FileSystemNodeCtor2()
        {
            _ = Throws<ArgumentException>(() => new FileSystemNode("1", false));
        }

        [Fact]
        public void Test_HierarchyCount()
        {
            Equal(5, _tree.HierarchyCount);
        }

        [Fact]
        public void Test_Index()
        {
            {
                ReadOnlyCollection<FileSystemNode> collection =
                    _tree[0] ?? throw new InvalidOperationException();
                _ = Single(collection);
                FileSystemNode node = collection[0];
                True(node.Hierarchy == 0 && node.Index == 0);
                True(_tree[0, 0] == node);
            }
            {
                ReadOnlyCollection<FileSystemNode> collection =
                    _tree[1] ?? throw new InvalidOperationException();
                Equal(3, collection.Count);
            }
        }

        [Fact]
        public void Test_RemoveAllFileNodes()
        {
            ReadOnlyCollection<FileSystemNode> removeNodes =
                _tree.RemoveNodes(_tree.All.Where(node => node.Info is FileInfo));
            True(removeNodes.Count == 81);
            True(removeNodes.All(node => node.Parent is null));
            True(removeNodes.All(node => node.Index is null));
            True(removeNodes.All(node => node.Hierarchy is null));
            True(removeNodes.All(node => node.Tree is null));
            True(removeNodes.All(node => node.Type is FileSystemNodeType.FileInfo));
            True(_tree.All.Count == 40);
            True(_tree.All.All(node => node.Info is DirectoryInfo));
        }

        [Fact]
        public void Test_RemoveDirNodes()
        {
            IEnumerable<FileSystemNode> needRemoveNodes = _tree.All.Where(node =>
                    node.Name == "FileSystemTreeUnitTest.0")
                .ToList();
            ReadOnlyCollection<FileSystemNode> removeNodes =
                _tree.RemoveNodes(needRemoveNodes);
            Equal(27, removeNodes.Count(node => node.Info is FileInfo));
            Equal(13, removeNodes.Count(node => node.Info is DirectoryInfo));
        }

        [Fact]
        public void Test_RemoveNotThisNodes()
        {
            _ = Throws<InvalidOperationException>(() => _tree.RemoveNodes(new[]
            {
                new FileSystemNode(new DirectoryInfo(_fixture.BaseDirPath), false)
            }));
        }

        [Fact]
        public void Test_RemoveOneFileNodes()
        {
            ReadOnlyCollection<FileSystemNode> removeNodes =
                _tree.RemoveNodes(new[]
                {
                    _tree.All.First(node => node.Info is FileInfo)
                });
            _ = Single(removeNodes);
            True(removeNodes.All(node => node.Parent is null));
            True(removeNodes.All(node => node.Index is null));
            True(removeNodes.All(node => node.Hierarchy is null));
            True(removeNodes.All(node => node.Tree is null));
            True(removeNodes.All(node => node.Type is FileSystemNodeType.FileInfo));
            Equal(120, _tree.All.Count);
            True(_tree.All.All(node => node.Name != removeNodes[0].Name));
        }

        [Fact]
        public void Test_TopNode()
        {
            FileSystemNode? node = _tree[0, 0];
            NotNull(node);
            var baseDirectoryInfo = new DirectoryInfo(_fixture.BaseDirPath);
            Equal(node!.Name, baseDirectoryInfo.Name);
            Equal(3, node.Children!.Count);
            Equal(node.Tree, _tree);
            Equal(typeof(DirectoryInfo), node.Info.GetType());
            Equal(node.FullName, baseDirectoryInfo.FullName);
            Null(node.Parent);
            Equal(0, node.Hierarchy);
            Equal(0, node.Index);
            False(node.IsExistsUnauthorizedAccessChildren);
        }
    }
}

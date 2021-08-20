using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;
using static Xunit.Assert;

namespace FileTree.Library.TestProject
{
    public partial class FileSystemTreeUnitTest : IClassFixture<
        FileSystemTreeUnitTest.FileSystemTreeUnitTestClassFixture>
    {
        private readonly FileSystemTreeUnitTestClassFixture _fixture;
        private readonly ReadOnlyCollection<DirectoryInfo> _directoryInfos;
        private readonly ReadOnlyCollection<FileInfo> _fileInfos;
        private readonly FileSystemTree _tree;

        public FileSystemTreeUnitTest(FileSystemTreeUnitTestClassFixture fixture)
        {
            _fixture = fixture;
            (ReadOnlyCollection<DirectoryInfo> directoryInfos,
                ReadOnlyCollection<FileInfo> fileInfos) = _fixture.GetInfos();
            _directoryInfos = directoryInfos;
            _fileInfos = fileInfos;
            _tree = CreateTree();
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

        [Fact]
        public void Test_HierarchyCount()
        {
            Equal(5, _tree.HierarchyCount);
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
            Equal(node.GetTopParent(), _tree[0, 0]);
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
        public void Test_RemoveAllFileNodes()
        {
            ReadOnlyCollection<FileSystemNode> removeNodes =
                _tree.RemoveNodes(_tree.All.Where(node => node.Info is FileInfo));
            Equal(81, removeNodes.Count);
            Equal(true, removeNodes.All(node => node.Parent is null));
            Equal(true, removeNodes.All(node => node.Index is null));
            Equal(true, removeNodes.All(node => node.Hierarchy is null));
            Equal(true, removeNodes.All(node => node.Tree is null));
            Equal(true, removeNodes.All(node => node.Info is not null));
            Equal(true,
                removeNodes.All(node => node.Type is FileSystemNodeType.FileInfo));
            Equal(40, _tree.All.Count);
            Equal(true, _tree.All.All(node => node.Info is DirectoryInfo));
        }

        [Fact]
        public void Test_RemoveOneFileNodes()
        {
            ReadOnlyCollection<FileSystemNode> removeNodes =
                _tree.RemoveNodes(new[]
                {
                    _tree.All.First(node => node.Info is FileInfo)
                });
            Equal(1, removeNodes.Count);
            Equal(true, removeNodes.All(node => node.Parent is null));
            Equal(true, removeNodes.All(node => node.Index is null));
            Equal(true, removeNodes.All(node => node.Hierarchy is null));
            Equal(true, removeNodes.All(node => node.Tree is null));
            Equal(true, removeNodes.All(node => node.Info is not null));
            Equal(true,
                removeNodes.All(node => node.Type is FileSystemNodeType.FileInfo));
            Equal(120, _tree.All.Count);
            Equal(true, _tree.All.All(node => node.Name != removeNodes[0].Name));
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
            Equal(false, node.IsExistsUnauthorizedAccessChildren);
        }

        private FileSystemTree CreateTree()
        {
            var tree = new FileSystemTree(new DirectoryInfo(_fixture.BaseDirPath));
            foreach (FileSystemNode fileSystemNode in tree.All.Where(node =>
            {
                return node.Info is FileInfo fileInfo &&
                    _fileInfos.Any(info => info.FullName == fileInfo.FullName) is false;
            }))
            {
                ((FileInfo)fileSystemNode.Info).Delete();
            }

            return tree;
        }
    }
}

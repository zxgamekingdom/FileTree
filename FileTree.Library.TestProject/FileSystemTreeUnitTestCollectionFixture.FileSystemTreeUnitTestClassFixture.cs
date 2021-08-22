using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Xunit;

namespace FileTree.Library.TestProject
{
    [CollectionDefinition(nameof(FileSystemTreeUnitTestCollectionFixture))]
    public class FileSystemTreeUnitTestCollectionFixture : ICollectionFixture<
        FileSystemTreeUnitTestCollectionFixture.FileSystemTreeUnitTestClassFixture>
    {
        public class FileSystemTreeUnitTestClassFixture
        {
            private List<DirectoryInfo> _directoryInfos = null!;
            private List<FileInfo> _fileInfos = null!;

            public string BaseDirPath { get; private set; } = null!;

            public FileSystemTree CreateTree()
            {
                var tree = new FileSystemTree(new DirectoryInfo(BaseDirPath));
                foreach (FileSystemNode fileSystemNode in tree.All.Where(node =>
                    node.Info is FileInfo fileInfo &&
                    _fileInfos.Any(info =>
                        info.FullName == fileInfo.FullName) is false))
                {
                    ((FileInfo)fileSystemNode.Info).Delete();
                }

                foreach (FileSystemNode fileSystemNode in tree.All.Where(node =>
                    node.Info is DirectoryInfo directoryInfo &&
                    _directoryInfos.Any(info =>
                        info.FullName == directoryInfo.FullName) is false))
                {
                    ((DirectoryInfo)fileSystemNode.Info).Delete(true);
                }

                return tree;
            }

            public (ReadOnlyCollection<DirectoryInfo> directoryInfos,
                ReadOnlyCollection<FileInfo> fileInfos) GetInfos()
            {
                return (_directoryInfos.AsReadOnly(), _fileInfos.AsReadOnly());
            }

            public FileSystemTreeUnitTestClassFixture()
            {
                InitInfos();
                CreateInfos();
            }

            private void CreateInfos()
            {
                foreach (DirectoryInfo directoryInfo in _directoryInfos.Where(
                    directoryInfo => directoryInfo.Exists is false))
                {
                    directoryInfo.Create();
                }

                foreach (FileInfo fileInfo in _fileInfos)
                {
                    File.WriteAllText(fileInfo.FullName, fileInfo.FullName);
                }
            }

            private void InitInfos()
            {
                _directoryInfos = new List<DirectoryInfo>();
                _fileInfos = new List<FileInfo>();
                BaseDirPath = Path.Combine(Path.GetTempPath(),
                    $"{nameof(FileSystemTree)}UnitTest");
                // _ = Process.Start("explorer", BaseDirPath);
                var dir0 = new DirectoryInfo(BaseDirPath);
                _directoryInfos.Add(dir0);
                for (int i = 0; i < 3; i++)
                {
                    var dir1 =
                        new DirectoryInfo(Path.Combine(dir0.FullName,
                            $"{dir0.Name}.{i}"));
                    _directoryInfos.Add(dir1);
                    for (int j = 0; j < 3; j++)
                    {
                        var dir2 = new DirectoryInfo(Path.Combine(dir1.FullName,
                            $"{dir1.Name}.{j}"));
                        _directoryInfos.Add(dir2);
                        for (int k = 0; k < 3; k++)
                        {
                            var dir3 = new DirectoryInfo(Path.Combine(dir2.FullName,
                                $"{dir2.Name}.{k}"));
                            _directoryInfos.Add(dir3);
                            for (int l = 0; l < 3; l++)
                            {
                                var file4 = new FileInfo(Path.Combine(dir3.FullName,
                                    $"{dir3.Name}.{l}.txt"));
                                _fileInfos.Add(file4);
                            }
                        }
                    }
                }
            }
        }
    }
}

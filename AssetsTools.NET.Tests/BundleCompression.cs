using AssetsTools.NET.Extra;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetsTools.NET.Tests
{
    public class BundleCompressionTests
    {
        [TestCase(AssetBundleCompressionType.None)]
        [TestCase(AssetBundleCompressionType.LZMA)]
        [TestCase(AssetBundleCompressionType.LZ4)]
        [TestCase(AssetBundleCompressionType.LZ4Fast)]
        public void BundleInstanceRemembersCompressionAndDirectoryPosition(AssetBundleCompressionType compression)
        {
            byte[] bundleBytes = CreatePackedBundle(compression, blockDirAtEnd: false);

            using var stream = new MemoryStream(bundleBytes);
            var instance = new BundleFileInstance(stream, "test.bundle");

            Assert.AreEqual(compression, instance.originalCompression);
            Assert.IsFalse(instance.originalBlockAndDirAtEnd);
        }

        [TestCase(AssetBundleCompressionType.None, false)]
        [TestCase(AssetBundleCompressionType.LZMA, false)]
        [TestCase(AssetBundleCompressionType.LZ4, false)]
        [TestCase(AssetBundleCompressionType.LZ4Fast, false)]
        [TestCase(AssetBundleCompressionType.None, true)]
        [TestCase(AssetBundleCompressionType.LZMA, true)]
        [TestCase(AssetBundleCompressionType.LZ4, true)]
        [TestCase(AssetBundleCompressionType.LZ4Fast, true)]
        public void PackWritesRequestedCompressionAndDirectoryPosition(
            AssetBundleCompressionType compression,
            bool blockDirAtEnd)
        {
            byte[] bundleBytes = CreatePackedBundle(compression, blockDirAtEnd);

            using var stream = new MemoryStream(bundleBytes);
            var bundle = new AssetBundleFile();
            bundle.Read(new AssetsFileReader(stream));

            Assert.AreEqual(compression, bundle.GetCompressionType());
            Assert.AreEqual(
                blockDirAtEnd,
                (bundle.Header.FileStreamHeader.Flags & AssetBundleFSHeaderFlags.BlockAndDirAtEnd) != 0);

            if (bundle.DataIsCompressed)
            {
                bundle = BundleHelper.UnpackBundle(bundle);
            }

            CollectionAssert.AreEqual(CreatePayload(), BundleHelper.LoadAssetDataFromBundle(bundle, 0));
        }

        private static byte[] CreatePackedBundle(
            AssetBundleCompressionType compression,
            bool blockDirAtEnd)
        {
            byte[] payload = CreatePayload();
            using var dataStream = new MemoryStream(payload);
            using var readerStream = new MemoryStream();

            var source = new AssetBundleFile
            {
                Header = new AssetBundleHeader
                {
                    Signature = "UnityFS",
                    Version = 6,
                    GenerationVersion = "5.x.x",
                    EngineVersion = "2020.3.0f1",
                    FileStreamHeader = new AssetBundleFSHeader
                    {
                        Flags = AssetBundleFSHeaderFlags.HasDirectoryInfo
                    }
                },
                BlockAndDirInfo = new AssetBundleBlockAndDirInfo
                {
                    BlockInfos = new[]
                    {
                        new AssetBundleBlockInfo
                        {
                            CompressedSize = (uint)payload.Length,
                            DecompressedSize = (uint)payload.Length,
                            Flags = 0
                        }
                    },
                    DirectoryInfos = new List<AssetBundleDirectoryInfo>
                    {
                        new AssetBundleDirectoryInfo
                        {
                            Offset = 0,
                            DecompressedSize = payload.Length,
                            Flags = 0,
                            Name = "payload.bin"
                        }
                    }
                },
                Reader = new AssetsFileReader(readerStream),
                DataReader = new AssetsFileReader(dataStream),
                DataIsCompressed = false
            };

            using var output = new MemoryStream();
            source.Pack(new AssetsFileWriter(output), compression, blockDirAtEnd);
            return output.ToArray();
        }

        private static byte[] CreatePayload()
        {
            return Enumerable.Repeat((byte)0x2a, 0x40000).ToArray();
        }
    }
}

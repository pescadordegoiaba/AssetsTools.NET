using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

namespace AssetsTools.NET.Tests
{
    public class LegacyAssetsFileTests
    {
        [Test]
        public void Version9MetadataOmitsScriptTypes()
        {
            AssetsFileMetadata metadata = CreateMetadata(9);
            metadata.ScriptTypes.Add(new AssetPPtr(7, 1234));
            metadata.Externals.Add(new AssetsFileExternal
            {
                VirtualAssetPathName = string.Empty,
                Guid = new GUID128(),
                Type = AssetsFileExternalType.Normal,
                PathName = "sharedassets0.assets",
                OriginalPathName = "sharedassets0.assets"
            });

            byte[] bytes = WriteMetadata(metadata, 9);
            AssetsFileMetadata rewritten = ReadMetadata(bytes, 9);

            Assert.IsEmpty(rewritten.ScriptTypes);
            Assert.AreEqual(1, rewritten.Externals.Count);
            Assert.AreEqual("sharedassets0.assets", rewritten.Externals[0].PathName);
            Assert.AreEqual(bytes.Length, metadata.GetSize(9));
        }

        [Test]
        public void Version9MetadataPreservesBigPathIds()
        {
            const long pathId = 0x0000000200000001;
            AssetsFileMetadata metadata = CreateMetadata(9);
            metadata.BigIdEnabled = 1;
            metadata.AssetInfos.Add(new AssetFileInfo
            {
                PathId = pathId,
                ByteOffset = 0x20,
                ByteSize = 0x40,
                TypeIdOrIndex = 49,
                OldTypeId = 49,
                ScriptTypeIndex = 0
            });

            byte[] bytes = WriteMetadata(metadata, 9);
            AssetsFileMetadata rewritten = ReadMetadata(bytes, 9);

            Assert.AreEqual(1, rewritten.BigIdEnabled);
            Assert.AreEqual(pathId, rewritten.AssetInfos[0].PathId);
            Assert.AreEqual(bytes.Length, metadata.GetSize(9));
        }

        [Test]
        public void Version11ScriptTypesUse32BitPathIds()
        {
            AssetsFileMetadata metadata = CreateMetadata(11);
            metadata.ScriptTypes.Add(new AssetPPtr(3, 0x12345678));

            byte[] bytes = WriteMetadata(metadata, 11);
            AssetsFileMetadata rewritten = ReadMetadata(bytes, 11);

            Assert.AreEqual(1, rewritten.ScriptTypes.Count);
            Assert.AreEqual(3, rewritten.ScriptTypes[0].FileId);
            Assert.AreEqual(0x12345678, rewritten.ScriptTypes[0].PathId);
            Assert.AreEqual(bytes.Length, metadata.GetSize(11));
        }

        private static AssetsFileMetadata CreateMetadata(uint version)
        {
            return new AssetsFileMetadata
            {
                UnityVersion = version == 9 ? "4.7.2f1" : "5.0.0f1",
                TargetPlatform = 13,
                TypeTreeEnabled = true,
                TypeTreeTypes = new List<TypeTreeType>(),
                AssetInfos = new List<AssetFileInfo>(),
                ScriptTypes = new List<AssetPPtr>(),
                Externals = new List<AssetsFileExternal>(),
                RefTypes = new List<TypeTreeType>(),
                UserInformation = string.Empty
            };
        }

        private static byte[] WriteMetadata(AssetsFileMetadata metadata, uint version)
        {
            using MemoryStream stream = new MemoryStream();
            using AssetsFileWriter writer = new AssetsFileWriter(stream);
            writer.BigEndian = false;
            metadata.Write(writer, version);
            return stream.ToArray();
        }

        private static AssetsFileMetadata ReadMetadata(byte[] bytes, uint version)
        {
            using MemoryStream stream = new MemoryStream(bytes);
            using AssetsFileReader reader = new AssetsFileReader(stream);
            reader.BigEndian = false;
            AssetsFileMetadata metadata = new AssetsFileMetadata();
            metadata.Read(reader, version);
            return metadata;
        }
    }
}

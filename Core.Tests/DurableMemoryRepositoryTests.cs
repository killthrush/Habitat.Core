using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Habitat.Core.Tests
{
    [TestClass]
    public class DurableMemoryRepositoryTests
    {
        private const string MockDataPath = "fakepath";

        private static MockFileSystemProvider _mockProvider;

        private class EntityTest
        {
            public int Number { get; set; }
            public string Name { get; set; }
            public string GetJson()
            {
                return JsonConvert.SerializeObject(this, Formatting.None);
            }
        }

        [TestInitialize]
        public void SetUp()
        {
            _mockProvider = new MockFileSystemProvider();
            CreateStandardMockDataFiles();
        }

        [TestCleanup]
        public void TearDown()
        {
            _mockProvider = null;
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RespositoryCreationWithNullPath()
        {
            new DurableMemoryRepository<EntityTest>(null, new FileSystemFacade());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RespositoryCreationWithNullFileFacade()
        {
            new DurableMemoryRepository<EntityTest>(MockDataPath, null);
        }

        [TestMethod]
        public void RespositoryCreationWithExtraneousFilesInStorageLocation()
        {
            // Instead of using the default mock files, create a bunch of mock files to simulate a case where the data storage location has other things in it
            _mockProvider.Reset();
            CreateMockDataFilesWithExtraStuff();

            var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object);
            var entity = testRepository.Create();
            Assert.IsNotNull(entity);
            Assert.AreEqual(5, entity.Id);
            Assert.AreEqual(MockDataPath, testRepository.Path);
        }

        [TestMethod]
        public void CreateNewResource()
        {
            var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object);
            var entity = testRepository.Create();
            Assert.IsNotNull(entity);
            Assert.IsNull(entity.JsonData);
            Assert.AreEqual(MockDataPath, testRepository.Path);
        }

        [TestMethod]
        public void CreateTwoNewResourcesWithUniqueIDs()
        {
            var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object);
            var firstEntity = testRepository.Create();
            Assert.IsNotNull(firstEntity);
            Assert.AreEqual(5, firstEntity.Id);

            var secondEntity = testRepository.Create();
            Assert.IsNotNull(secondEntity);
            Assert.AreEqual(6, secondEntity.Id);

            Assert.AreEqual(MockDataPath, testRepository.Path);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNullEntity()
        {
            var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object);
            testRepository.Add(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeleteNullEntity()
        {
            var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object);
            testRepository.Delete(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void UpdateNullEntity()
        {
            var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object);
            testRepository.Update(null);
        }

        [TestMethod]
        public void AddEntityAndRetrieveFromMemory()
        {
            IJsonEntity<EntityTest> entity;
            IJsonEntity<EntityTest> entityFromRepo;
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                entity = testRepository.Create();
                entity.Contents = new EntityTest {Name = "foo", Number = 1};
                testRepository.Add(entity);
                entityFromRepo = testRepository.Entities.FirstOrDefault(x => x.Contents.Name == "foo");
            }

            Assert.IsNotNull(entityFromRepo);
            Assert.AreEqual(entity.Id, entityFromRepo.Id);
            Assert.AreEqual(entity.Contents.Name, entityFromRepo.Contents.Name);
            Assert.AreEqual(entity.Contents.Number, entityFromRepo.Contents.Number);
        }

        [TestMethod]
        public void AddEntityAndRetrieveFromDisk()
        {
            // Create test entity and write it to the mock filesystem
            IJsonEntity<EntityTest> entity;
            IJsonEntity<EntityTest> entityFromRepo;
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                entity = testRepository.Create();
                entity.Contents = new EntityTest {Name = "foo", Number = 1};
                testRepository.Add(entity);
                testRepository.Save();
            }

            // Now re-create the repo, which should read the persisted value for the new entity from "disk"
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                entityFromRepo = testRepository.Entities.FirstOrDefault(x => x.Contents.Name == "foo");
            }

            Assert.IsNotNull(entityFromRepo);
            Assert.AreEqual(entity.Id, entityFromRepo.Id);
            Assert.AreEqual(entity.Contents.Name, entityFromRepo.Contents.Name);
            Assert.AreEqual(entity.Contents.Number, entityFromRepo.Contents.Number);
        }

        [TestMethod]
        public void AddEntityThenDeleteFromMemory()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                IJsonEntity<EntityTest> entity = testRepository.Create();
                entity.Contents = new EntityTest { Name = "foo", Number = 1 };
                testRepository.Add(entity);

                IJsonEntity<EntityTest> entityFromRepo = testRepository.Entities.FirstOrDefault(x => x.Contents.Name == "foo");
                Assert.IsNotNull(entityFromRepo);

                testRepository.Delete(entity);

                entityFromRepo = testRepository.Entities.FirstOrDefault(x => x.Contents.Name == "foo");
                Assert.IsNull(entityFromRepo);
            }
        }

        [TestMethod]
        public void AddEntityThenDeleteFromDisk()
        {
            // Create test entity
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entity = testRepository.Create();
                entity.Contents = new EntityTest { Name = "foo", Number = 1 };
                testRepository.Add(entity);
                testRepository.Save();
            }
            
            // Read the new entity from disk, then delete it and save
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entityFromRepo = testRepository.Entities.FirstOrDefault(x => x.Contents.Name == "foo");
                Assert.IsNotNull(entityFromRepo);
                testRepository.Delete(entityFromRepo);
                testRepository.Save();
            }

            // After re-initializing the repo, the deleted item should be gone, but everything else should still be present
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entityFromRepo = testRepository.Entities.FirstOrDefault(x => x.Contents.Name == "foo");
                Assert.IsNull(entityFromRepo);
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }
        }

        [TestMethod]
        public void CallSaveWithNoPendingChanges()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                testRepository.Save();
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }
        }

        [TestMethod]
        public void AddSameItemMoreThanOnce()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entity = testRepository.Create();
                testRepository.Add(entity);
                testRepository.Add(entity);
                testRepository.Save();
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4, 5 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4, 5 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }
        }

        [TestMethod]
        public void DeleteSameItemMoreThanOnce()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entity = testRepository.Entities.First();
                testRepository.Delete(entity);
                testRepository.Delete(entity);
                testRepository.Save();
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }
        }

        [TestMethod]
        public void AddAndDeleteTheSameItemMultipleTimesInASession()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entity = testRepository.Create();
                testRepository.Add(entity);
                testRepository.Delete(entity);
                testRepository.Add(entity);
                testRepository.Delete(entity);
                testRepository.Add(entity);
                testRepository.Delete(entity);
                testRepository.Add(entity);
                testRepository.Add(entity);
                testRepository.Delete(entity);
                testRepository.Delete(entity);
                testRepository.Save();
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }
        }

        [TestMethod]
        public void UpdatingItemThatDoesNotExistActsLikeAdd()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entity = testRepository.Create();
                testRepository.Update(entity);
                testRepository.Save();
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4, 5 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }
        }

        [TestMethod]
        public void DeleteItemThatDoesNotExist()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entity = testRepository.Create();
                testRepository.Delete(entity);
                testRepository.Save();
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }
        }

        [TestMethod]
        public void UpdateTheSameItemMultipleTimes()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var entity = testRepository.Entities.First(x => x.Id == 1);
                entity.Contents.Name = "newname1";
                entity.Contents.Number = -1;
                testRepository.Update(entity);
                entity.Contents.Name = "newname2";
                entity.Contents.Number = -2;
                testRepository.Update(entity);
                testRepository.Save();
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, testRepository.Entities.Select(x => x.Id).ToArray());
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, testRepository.Entities.Select(x => x.Id).ToArray());
                var entity = testRepository.Entities.First(x => x.Id == 1);
                Assert.AreEqual("newname2", entity.Contents.Name);
                Assert.AreEqual(-2, entity.Contents.Number);
            }
        }

        [TestMethod]
        public void DataReadFromRepoIsDisconnectedFromSession()
        {
            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var collection1 = testRepository.Entities.Where(x => new[]{1, 2}.Contains(x.Id)).ToArray();
                testRepository.Delete(collection1[0]);
                testRepository.Delete(collection1[1]);
                var collection2 = testRepository.Entities.Where(x => new[] { 1, 2 }.Contains(x.Id)).ToArray();
                CollectionAssert.AreEquivalent(new[] { 1, 2 }, collection1.Select(x => x.Id).ToArray());
                CollectionAssert.AreEquivalent(new int[] {}, collection2.Select(x => x.Id).ToArray());
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var collection1 = testRepository.Entities.ToArray();
                var entity1 = testRepository.Create();
                var entity2 = testRepository.Create();
                testRepository.Add(entity1);
                testRepository.Add(entity2);
                var collection2 = testRepository.Entities.ToArray();
                
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4 }, collection1.Select(x => x.Id).ToArray());
                CollectionAssert.AreEquivalent(new[] { 1, 2, 4, 5, 6 }, collection2.Select(x => x.Id).ToArray());
            }

            using (var testRepository = new DurableMemoryRepository<EntityTest>(MockDataPath, _mockProvider.MockFileSystem.Object))
            {
                var collection1 = testRepository.Entities.ToArray();
                var entity = collection1[0];
                string originalName = entity.Contents.Name;
                int originalNumber = entity.Contents.Number;
                entity.Contents.Name = "foo";
                entity.Contents.Number = -1;
                testRepository.Update(entity);
                var collection2 = testRepository.Entities.ToArray();

                Assert.AreNotEqual(originalName, collection2[0].Contents.Name);
                Assert.AreNotEqual(originalNumber, collection2[0].Contents.Number);
            }
        }

        private static string CreateMockFileName(int id)
        {
            var mockFileName = string.Format("{0:d10}_{1}.json", id, typeof (EntityTest));
            return Path.Combine(MockDataPath, mockFileName);
        }

        private static void CreateStandardMockDataFiles()
        {
            _mockProvider.CreateOrUpdateMockFile(CreateMockFileName(1), new EntityTest { Name = "1", Number = 1 }.GetJson());
            _mockProvider.CreateOrUpdateMockFile(CreateMockFileName(2), new EntityTest { Name = "2", Number = 2 }.GetJson());
            _mockProvider.CreateOrUpdateMockFile(CreateMockFileName(4), new EntityTest { Name = "4", Number = 4 }.GetJson());
        }

        private static void CreateMockDataFilesWithExtraStuff()
        {
            CreateStandardMockDataFiles();
            _mockProvider.CreateOrUpdateMockFile("blahblah" + typeof(EntityTest) + ".json", "blahblah");
            _mockProvider.CreateOrUpdateMockFile("monkey.txt", "I like monkeys");
            _mockProvider.CreateOrUpdateMockFile("..", "umm, this isn't a file");
        }
    }
}

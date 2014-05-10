using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Habitat.Core.Tests
{
    [TestClass]
    public class JsonEntityTests
    {
        [TestMethod]
        public void EmptyEntityReturnsNullData()
        {
            var entity = new JsonEntity<string>(1);
            Assert.IsNull(entity.Contents);
            Assert.IsNull(entity.JsonData);
        }

        [TestMethod]
        public void EntityReturnsJsonDataAfterContentsAssignment()
        {
            var entity = new JsonEntity<string>(1);
            entity.Contents = "1";
            Assert.IsNotNull(entity.Contents);
            Assert.IsNotNull(entity.JsonData);
        }

        [TestMethod]
        public void EntityReturnsContentsDataAfterJsonAssignment()
        {
            var entity = new JsonEntity<string>(1);
            entity.JsonData = "1";
            Assert.IsNotNull(entity.JsonData);
            Assert.IsNotNull(entity.Contents);
        }

        [TestMethod]
        public void EntityReturnsNullJsonDataAfterNullContentsAssignment()
        {
            var entity = new JsonEntity<string>(1);
            entity.Contents = null;
            Assert.IsNull(entity.Contents);
            Assert.IsNull(entity.JsonData);
        }

        [TestMethod]
        public void EntityReturnsNullContentsDataAfterNullJsonAssignment()
        {
            var entity = new JsonEntity<string>(1);
            entity.JsonData = null;
            Assert.IsNull(entity.Contents);
            Assert.IsNull(entity.JsonData);
        }

        [TestMethod]
        public void EntityReturnsNullsAfterInvalidContentsAssignment()
        {
            var entity = new JsonEntity<PathologicalClass>(1);
            entity.Contents = new PathologicalClass();
            Assert.IsNull(entity.JsonData);
            Assert.IsNull(entity.Contents);
        }

        [TestMethod]
        public void EntityReturnsNullsAfterInvalidJsonAssignment()
        {
            var entity = new JsonEntity<string>(1);
            entity.JsonData = "$&*&D[]:";
            Assert.IsNull(entity.JsonData);
            Assert.IsNull(entity.Contents);
        }

        [TestMethod]
        public void EntityReturnsNewJsonDataWhenRequestedMultipleTimes()
        {
            var entity = new JsonEntity<string>(1);
            entity.Contents = "1";
            var jsonData = entity.JsonData;
            Assert.IsNotNull(jsonData);
            Assert.AreNotSame(entity.JsonData, jsonData);
        }

        [TestMethod]
        public void EntityReturnsTheSameContentsDataWhenRequestedMultipleTimes()
        {
            var entity = new JsonEntity<string>(1);
            entity.JsonData = "1";
            var contents = entity.Contents;
            Assert.IsNotNull(contents);
            Assert.AreSame(entity.Contents, contents);
        }

        [TestMethod]
        public void EntityReturnsDifferentDataAfterJsonAssignment()
        {
            var entity = new JsonEntity<string>(1);
            entity.JsonData = "1";
            var jsonData = entity.JsonData;
            var contents = entity.Contents;
            entity.JsonData = "2";
            Assert.AreNotEqual((object) jsonData, entity.JsonData);
            Assert.AreNotEqual((object) contents, entity.Contents);
        }

        [TestMethod]
        public void EntityReturnsDifferentDataAfterContentsAssignment()
        {
            var entity = new JsonEntity<string>(1);
            entity.Contents = "1";
            var jsonData = entity.JsonData;
            var contents = entity.Contents;
            entity.Contents = "2";
            Assert.AreNotEqual((object) jsonData, entity.JsonData);
            Assert.AreNotEqual((object) contents, entity.Contents);
        }

        [TestMethod]
        public void RoundTripValueAssignmentWorksProperlyForSameType()
        {
            var entity1 = new JsonEntity<RegularClass>(1);
            entity1.Contents = new RegularClass {Value = 1};
            var entity2 = new JsonEntity<RegularClass>(2);
            entity2.JsonData = entity1.JsonData;
            Assert.AreEqual((object) entity1.Contents.Value, entity2.Contents.Value);
        }

        [TestMethod]
        public void RoundTripValueAssignmentWorksProperlyForConvertableType()
        {
            var entity1 = new JsonEntity<RegularClass>(1);
            entity1.Contents = new RegularClass { Value = 1 };
            var entity2 = new JsonEntity<AnotherRegularClass>(2);
            entity2.JsonData = entity1.JsonData;
            Assert.AreEqual((object) entity1.Contents.Value.ToString(), entity2.Contents.Value);
        }

        [TestMethod]
        public void RoundTripValueAssignmentWorksProperlyWhenChangesAreMadeDirectlyToContents()
        {
            var entity1 = new JsonEntity<RegularClass>(1);
            entity1.Contents = new RegularClass();
            entity1.Contents.Value = 42;
            var entity2 = new JsonEntity<RegularClass>(2);
            entity2.JsonData = entity1.JsonData;
            Assert.AreEqual((object) entity1.Contents.Value, entity2.Contents.Value);
        }

        internal class PathologicalClass
        {
            public string Breakage
            {
                get
                {
                    throw new Exception();
                }
            }
        }

        internal class RegularClass
        {
            public int Value { get; set; }
        }

        internal class AnotherRegularClass
        {
            public string Value { get; set; }
        }
    }
}

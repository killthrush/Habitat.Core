using System;
using System.Collections.Generic;
using KellermanSoftware.CompareNetObjects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using ProTeck.Config.Dto.V1;

namespace ProTeck.Config.Dto.Tests
{
    [TestClass]
    public class ConfigDtoTests
    {
        /// <summary>
        /// Object used for deep compare operations on object graphs
        /// </summary>
        private readonly CompareObjects _objectComparer = new CompareObjects();

        [TestMethod]
        public void Should_be_able_to_serialize_empty_config_node()
        {
            var originalNode = new ConfigNode();
            string json = JsonConvert.SerializeObject(originalNode);
            var deserializedNode = JsonConvert.DeserializeObject<ConfigNode>(json);
            Assert.IsTrue(_objectComparer.Compare(originalNode, deserializedNode));
        }

        [TestMethod]
        public void Should_be_able_to_serialize_node_with_multiple_value_children()
        {
            var originalNode = new ConfigNode();
            originalNode.Name = "N1";
            originalNode.Children = new List<ConfigNode>
                                        {
                                            CreateValueNode(1),
                                            CreateValueNode(2),
                                            CreateValueNode(3),
                                            CreateValueNode(4)
                                        };

            string json = JsonConvert.SerializeObject(originalNode);
            var deserializedNode = JsonConvert.DeserializeObject<ConfigNode>(json);
            Assert.IsTrue(_objectComparer.Compare(originalNode, deserializedNode));
        }

        [TestMethod]
        public void Should_be_able_to_serialize_node_with_complex_object_graph()
        {
            var originalNode = new ConfigNode();
            originalNode.Name = "N1";
            originalNode.Children = new List<ConfigNode>
                                        {
                                            CreateValueNode(1),
                                            CreateValueNode(2),
                                            new ConfigNode
                                                {
                                                    Name = "N2",
                                                    Children = new List<ConfigNode>
                                                                   {
                                                                       CreateValueNode(5),
                                                                   }
                                                },
                                            CreateValueNode(3),
                                            CreateValueNode(4),
                                            new ConfigNode
                                                {
                                                    Name = "N3",
                                                    Children = new List<ConfigNode>
                                                                   {
                                                                       CreateValueNode(6),
                                                                       new ConfigNode
                                                                           {
                                                                               Name = "N6",
                                                                               Children = new List<ConfigNode>
                                                                                              {
                                                                                                  CreateValueNode(8),
                                                                                              }
                                                                           }
                                                                   }
                                                }
                                        };

            string json = JsonConvert.SerializeObject(originalNode);
            var deserializedNode = JsonConvert.DeserializeObject<ConfigNode>(json);
            Assert.IsTrue(_objectComparer.Compare(originalNode, deserializedNode));
        }

        [TestMethod]
        public void Should_be_able_to_serialize_config_node_with_null_values()
        {
            var originalNode = new ConfigNode();
            originalNode.Name = "N1";
            originalNode.Children = new List<ConfigNode>
                                        {
                                            null,
                                            null,
                                            null,
                                            new ConfigNode
                                                {
                                                    Value = "foo"
                                                }
                                        };

            string json = JsonConvert.SerializeObject(originalNode);
            var deserializedNode = JsonConvert.DeserializeObject<ConfigNode>(json);
            Assert.IsTrue(_objectComparer.Compare(originalNode, deserializedNode));
        }

        [TestMethod]
        public void Convert_root_to_dictionary_when_there_are_no_duplicate_names()
        {
            var node = new ConfigNode();
            node.Name = "N1";
            node.Children = new List<ConfigNode>
                                        {
                                            CreateValueNode(1),
                                            CreateValueNode(2),
                                            CreateValueNode(3),
                                            CreateValueNode(4)
                                        };

            var expectedDictionary = new Dictionary<string, string>();
            expectedDictionary["N1.N1"] = "V1";
            expectedDictionary["N1.N2"] = "V2";
            expectedDictionary["N1.N3"] = "V3";
            expectedDictionary["N1.N4"] = "V4";

            Assert.IsTrue(_objectComparer.Compare(expectedDictionary, node.ToDictionary()));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Convert_root_to_dictionary_when_there_are_duplicate_names()
        {
            var node = new ConfigNode();
            node.Name = "N1";
            node.Children = new List<ConfigNode>
                                        {
                                            CreateValueNode(1),
                                            CreateValueNode(2),
                                            CreateValueNode(3),
                                            CreateValueNode(4)
                                        };
            node.Children[1].Name = "foo";
            node.Children[2].Name = "foo";
            node.ToDictionary();
        }

        [TestMethod]
        public void Convert_root_to_dictionary_when_there_are_nulls_in_some_of_the_names()
        {
            var node = new ConfigNode();
            node.Children = new List<ConfigNode>
                                        {
                                            CreateValueNode(1),
                                            CreateValueNode(2),
                                            CreateValueNode(3),
                                            CreateValueNode(4)
                                        };
            node.Children[1].Name = null; // Nulls alone should not cause this to break

            var expectedDictionary = new Dictionary<string, string>();
            expectedDictionary[".N1"] = "V1";
            expectedDictionary["."] = "V2";
            expectedDictionary[".N3"] = "V3";
            expectedDictionary[".N4"] = "V4";

            Assert.IsTrue(_objectComparer.Compare(expectedDictionary, node.ToDictionary()));
        }

        [TestMethod]
        public void Convert_root_to_dictionary_when_there_is_leading_or_trailing_whitespace_in_the_contents()
        {
            var node = new ConfigNode();
            node.Children = new List<ConfigNode>
                                        {
                                            CreateValueNodeWithWhitespace(1),
                                            CreateValueNodeWithWhitespace(2),
                                            CreateValueNodeWithWhitespace(3),
                                            CreateValueNodeWithWhitespace(4)
                                        };

            var expectedDictionary = new Dictionary<string, string>();
            expectedDictionary[".N1"] = "V1";
            expectedDictionary[".N2"] = "V2";
            expectedDictionary[".N3"] = "V3";
            expectedDictionary[".N4"] = "V4";

            Assert.IsTrue(_objectComparer.Compare(expectedDictionary, node.ToDictionary()));
        }

        [TestMethod]
        public void Convert_root_to_dictionary_when_there_is_a_complex_object_graph()
        {
            var originalNode = new ConfigNode();
            originalNode.Name = "N1";
            originalNode.Children = new List<ConfigNode>
                                        {
                                            CreateValueNode(1),
                                            CreateValueNode(2),
                                            new ConfigNode
                                                {
                                                    Name = "N2",
                                                    Children = new List<ConfigNode>
                                                                   {
                                                                       CreateValueNode(5),
                                                                   }
                                                },
                                            CreateValueNode(3),
                                            CreateValueNode(4),
                                            new ConfigNode
                                                {
                                                    Name = "N3",
                                                    Children = new List<ConfigNode>
                                                                   {
                                                                       CreateValueNode(6),
                                                                       new ConfigNode
                                                                           {
                                                                               Name = "N6",
                                                                               Children = new List<ConfigNode>
                                                                                              {
                                                                                                  CreateValueNode(8),
                                                                                              }
                                                                           }
                                                                   }
                                                }
                                        };

            var expectedDictionary = new Dictionary<string, string>();
            expectedDictionary["N1.N1"] = "V1";
            expectedDictionary["N1.N2"] = "V2";
            expectedDictionary["N1.N2.N5"] = "V5";
            expectedDictionary["N1.N3"] = "V3";
            expectedDictionary["N1.N4"] = "V4";
            expectedDictionary["N1.N3.N6"] = "V6";
            expectedDictionary["N1.N3.N6.N8"] = "V8";

            Assert.IsTrue(_objectComparer.Compare(expectedDictionary, originalNode.ToDictionary()));
        }

        [TestMethod]
        public void Should_be_able_to_serialize_empty_config_root()
        {
            var originalRoot = new ConfigRoot();
            string json = JsonConvert.SerializeObject(originalRoot);
            var deserializedRoot = JsonConvert.DeserializeObject<ConfigRoot>(json);
            Assert.IsTrue(_objectComparer.Compare(originalRoot, deserializedRoot));
        }

        [TestMethod]
        public void Should_be_able_to_serialize_root_with_multiple_value_children()
        {
            var originalRoot = new ConfigRoot();
            originalRoot.ComponentName = "foo";
            originalRoot.LastModified = new DateTime(2009, 11, 14); // JSON.NET loses the last 4 digits for Ticks when doing a round-trip conversion.  Therefore, something like DateTime.Now would cause this test to fail.
            var node = new ConfigNode();
            node.Name = "N1";
            node.Children = new List<ConfigNode>
                                        {
                                            CreateValueNode(1),
                                            CreateValueNode(2),
                                            CreateValueNode(3),
                                            CreateValueNode(4)
                                        };
            originalRoot.Data = node;

            string json = JsonConvert.SerializeObject(originalRoot);
            var deserializedRoot = JsonConvert.DeserializeObject<ConfigRoot>(json);
            Assert.IsTrue(_objectComparer.Compare(originalRoot, deserializedRoot));
        }

        private static ConfigNode CreateValueNode(int suffix)
        {
            var node = new ConfigNode {Name = string.Format("N{0}", suffix), Value = string.Format("V{0}", suffix)};
            return node;
        }

        private ConfigNode CreateValueNodeWithWhitespace(int suffix)
        {
            var node = CreateValueNode(suffix);
            node.Name = string.Format(" \t \r   {0} \n\n\t   ", node.Name);
            node.Value = string.Format(" \t \r   {0} \n\n\t   ", node.Value);
            return node;
        }
    }
}
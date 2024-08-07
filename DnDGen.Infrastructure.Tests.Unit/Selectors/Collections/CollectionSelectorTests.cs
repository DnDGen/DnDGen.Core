﻿using DnDGen.Infrastructure.Mappers.Collections;
using DnDGen.Infrastructure.Selectors.Collections;
using DnDGen.RollGen;
using Moq;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DnDGen.Infrastructure.Tests.Unit.Selectors.Collections
{
    [TestFixture]
    public class CollectionSelectorTests
    {
        private const string TableName = "table name";
        private const string AssemblyName = "my assembly";

        private ICollectionSelector selector;
        private Mock<CollectionMapper> mockMapper;
        private Mock<Dice> mockDice;
        private Dictionary<string, IEnumerable<string>> allCollections;

        [SetUp]
        public void Setup()
        {
            mockMapper = new Mock<CollectionMapper>();
            mockDice = new Mock<Dice>();
            selector = new CollectionSelector(mockMapper.Object, mockDice.Object);
            allCollections = new Dictionary<string, IEnumerable<string>>();

            mockMapper.Setup(m => m.Map(AssemblyName, TableName)).Returns(allCollections);
        }

        [Test]
        public void SelectFrom_SelectCollection()
        {
            allCollections["entry"] = Enumerable.Empty<string>();
            var collection = selector.SelectFrom(AssemblyName, TableName, "entry");
            Assert.That(collection, Is.EqualTo(allCollections["entry"]));
        }

        [Test]
        public void SelectAllFrom_SelectAllCollections()
        {
            var collections = selector.SelectAllFrom(AssemblyName, TableName);
            Assert.That(collections, Is.EqualTo(allCollections));
        }

        [Test]
        public void SelectFrom_IfEntryNotPresentInTable_ThrowException()
        {
            Assert.That(() => selector.SelectFrom(AssemblyName, TableName, "entry"),
                Throws.Exception.With.Message.EqualTo("entry is not a valid collection in the table table name"));
        }

        [Test]
        public void SelectRandomFrom_SelectRandomItemFromCollection()
        {
            var collection = new[] { "item 1", "item 2", "item 3" };
            mockDice.Setup(d => d.Roll(1).d(3).AsSum<int>()).Returns(2);

            var item = selector.SelectRandomFrom(collection);
            Assert.That(item, Is.EqualTo("item 2"));
        }

        [Test]
        public void SelectRandomFrom_SelectRandomItemFromTable()
        {
            allCollections["entry"] = new[] { "item 1", "item 2", "item 3" };
            mockDice.Setup(d => d.Roll(1).d(3).AsSum<int>()).Returns(2);

            var item = selector.SelectRandomFrom(AssemblyName, TableName, "entry");
            Assert.That(item, Is.EqualTo("item 2"));
        }

        [Test]
        public void SelectRandomFrom_CannotSelectRandomFromEmptyCollection()
        {
            var collection = Enumerable.Empty<string>();
            Assert.That(() => selector.SelectRandomFrom(collection), Throws.ArgumentException.With.Message.EqualTo("Cannot select random from an empty collection"));
        }

        [Test]
        public void SelectRandomFrom_CannotSelectRandomFromEmptyTable()
        {
            allCollections["entry"] = Enumerable.Empty<string>();
            Assert.That(() => selector.SelectRandomFrom(AssemblyName, TableName, "entry"),
                Throws.ArgumentException.With.Message.EqualTo("Cannot select random from an empty collection"));
        }

        [Test]
        public void SelectRandomFrom_CannotSelectRandomFromInvalidEntry()
        {
            Assert.That(() => selector.SelectRandomFrom(AssemblyName, TableName, "entry"),
                Throws.Exception.With.Message.EqualTo("entry is not a valid collection in the table table name"));
        }

        [Test]
        public void SelectRandomFrom_SelectRandomFromNonStringCollection()
        {
            var collection = new[] { 9266, 90210, 42, 600, 1337 };

            mockDice.Setup(d => d.Roll(1).d(5).AsSum<int>()).Returns(2);

            var entry = selector.SelectRandomFrom(collection);
            Assert.That(entry, Is.EqualTo(90210));
        }

        [Test]
        public void FindCollectionOf_FindCollectionContainingEntry()
        {
            allCollections["entry"] = new[] { "first", "fourth" };
            allCollections["other entry"] = new[] { "third", "fourth" };
            allCollections["wrong entry"] = new[] { "fifth", "second" };

            var collectionName = selector.FindCollectionOf(AssemblyName, TableName, "fourth");
            Assert.That(collectionName, Is.EqualTo("entry"));
        }

        [Test]
        public void FindCollectionOf_DoNotFindCollectionContainingEntry()
        {
            allCollections["entry"] = new[] { "first", "fourth" };
            allCollections["other entry"] = new[] { "third", "fourth" };
            allCollections["wrong entry"] = new[] { "fifth", "second" };

            Assert.That(() => selector.FindCollectionOf(AssemblyName, TableName, "sixth"),
                Throws.ArgumentException.With.Message.EqualTo("No collection in table name contains sixth"));
        }

        [Test]
        public void FindCollectionOf_FindCollectionContainingEntryWithFilteredCollectionNames()
        {
            allCollections["entry"] = new[] { "first", "second" };
            allCollections["other entry"] = new[] { "third", "fourth" };
            allCollections["wrong entry"] = new[] { "fifth", "fourth" };

            var group = selector.FindCollectionOf(AssemblyName, TableName, "fourth", "entry", "other entry");
            Assert.That(group, Is.EqualTo("other entry"));
        }

        [Test]
        public void FindCollectionOf_FindCollectionContainingEntryThrowsExceptionIfNotInFilteredCollectionNames()
        {
            allCollections["entry"] = new[] { "first", "second" };
            allCollections["other entry"] = new[] { "third", "fifth" };
            allCollections["wrong entry"] = new[] { "third", "fourth" };

            Assert.That(() => selector.FindCollectionOf(AssemblyName, TableName, "fourth", "entry", "other entry"),
                Throws.ArgumentException.With.Message.EqualTo("No collection from the 2 filters in table name contains fourth"));
        }

        [Test]
        public void IsCollection_IsCollection()
        {
            allCollections["entry"] = new[] { "first", "second" };
            var isCollection = selector.IsCollection(AssemblyName, TableName, "entry");
            Assert.That(isCollection, Is.True);
        }

        [Test]
        public void IsCollection_IsNotEntry()
        {
            allCollections["entry"] = new[] { "first", "second" };
            var isCollection = selector.IsCollection(AssemblyName, TableName, "other entry");
            Assert.That(isCollection, Is.False);
        }

        [Test]
        public void IsCollection_EntryIsNotCollection()
        {
            allCollections["entry"] = new[] { "first", "second" };
            var isCollection = selector.IsCollection(AssemblyName, TableName, "first");
            Assert.That(isCollection, Is.False);
        }

        [Test]
        public void IsCollection_EntryIsCollection()
        {
            allCollections["entry"] = new[] { "first", "second" };
            allCollections["first"] = new[] { "first", "third" };

            var isCollection = selector.IsCollection(AssemblyName, TableName, "first");
            Assert.That(isCollection, Is.True);
        }

        [Test]
        public void Explode_CollectionWithoutSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };

            var explodedCollection = selector.Explode(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Is.EquivalentTo(allCollections["entry"]));
        }

        [Test]
        public void Explode_CollectionWithSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };

            var explodedCollection = selector.Explode(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("third"));
            Assert.That(explodedCollection.Count, Is.EqualTo(4));
        }

        [Test]
        public void Explode_ExplodedCollectionsAreDistinctBetweenSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };
            allCollections["third"] = new[] { "sub 1", "sub 3" };

            var explodedCollection = selector.Explode(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Is.Unique);
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("sub 3"));
            Assert.That(explodedCollection, Does.Not.Contain("third"));
            Assert.That(explodedCollection.Count, Is.EqualTo(4));
        }

        [Test]
        public void Explode_ExplodedCollectionsAreDistinctBetweenCollectionAndSubCollection()
        {
            allCollections["entry"] = new[] { "first", "second", "third", "sub 1", "sub 3" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };
            allCollections["third"] = new[] { "second", "sub 3" };

            var explodedCollection = selector.Explode(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Is.Unique);
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("sub 3"));
            Assert.That(explodedCollection, Does.Not.Contain("third"));
            Assert.That(explodedCollection.Count, Is.EqualTo(4));
        }

        [Test]
        public void Explode_ExplodeCollectionWithSubCollectionsAndSelfAsSubCollection()
        {
            allCollections["entry"] = new[] { "first", "second", "entry" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };

            var explodedCollection = selector.Explode(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("entry"));
            Assert.That(explodedCollection.Count, Is.EqualTo(4));
        }

        [Test]
        public void BUG_Explode_LargeCollection()
        {
            var counts = Enumerable.Range(1, 100);
            allCollections["entry"] = counts.Select(c => $"entry {c}");

            foreach (var entry in allCollections["entry"].Take(10))
            {
                allCollections[entry] = counts.Take(10).Select(c => $"{entry}.{c}");

                foreach (var subentry in allCollections[entry])
                {
                    allCollections[subentry] = counts.Take(10).Select(c => $"{subentry}.{c}");

                    foreach (var subsubentry in allCollections[subentry])
                    {
                        allCollections[subsubentry] = counts.Take(10).Select(c => $"{subsubentry}.{c}");
                    }
                }
            }

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var explodedCollection = selector.Explode(AssemblyName, TableName, "entry");
            stopwatch.Stop();

            Assert.That(explodedCollection.Count, Is.EqualTo(10_090));

            var array = explodedCollection.ToArray();
            var i = 0;

            foreach (var c1 in counts)
            {
                if (c1 > 10)
                {
                    Assert.That(array[i++], Is.EqualTo($"entry {c1}"));
                    continue;
                }

                foreach (var c2 in counts.Take(10))
                {
                    foreach (var c3 in counts.Take(10))
                    {
                        foreach (var c4 in counts.Take(10))
                        {
                            Assert.That(array[i++], Is.EqualTo($"entry {c1}.{c2}.{c3}.{c4}"));
                        }
                    }
                }
            }

            Assert.That(i, Is.EqualTo(10_090));
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(1.009));
        }

        [Test]
        public void ExplodeAndPreserveDuplicates_ExplodeCollectionPreservingDuplicatesWithoutSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };

            var explodedCollection = selector.ExplodeAndPreserveDuplicates(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Is.EquivalentTo(allCollections["entry"]));
        }

        [Test]
        public void ExplodeAndPreserveDuplicates_ExplodeCollectionPreservingDuplicatesWithSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };

            var explodedCollection = selector.ExplodeAndPreserveDuplicates(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("third"));
            Assert.That(explodedCollection.Count, Is.EqualTo(4));
        }

        [Test]
        public void ExplodeAndPreserveDuplicates_ExplodedCollectionsPreservingDuplicatesAreNotDistinctBetweenSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };
            allCollections["third"] = new[] { "sub 1", "sub 3" };

            var explodedCollection = selector.ExplodeAndPreserveDuplicates(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Is.Not.Unique);
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection.Count(e => e == "sub 1"), Is.EqualTo(2));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("sub 3"));
            Assert.That(explodedCollection, Does.Not.Contain("third"));
            Assert.That(explodedCollection.Count, Is.EqualTo(5));
        }

        [Test]
        public void ExplodeAndPreserveDuplicates_ExplodedCollectionsPreservingDuplicatesAreNotDistinctBetweenCollectionAndSubCollection()
        {
            allCollections["entry"] = new[] { "first", "second", "third", "sub 1", "sub 3" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };
            allCollections["third"] = new[] { "second", "sub 3" };

            var explodedCollection = selector.ExplodeAndPreserveDuplicates(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Is.Not.Unique);
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection.Count(e => e == "sub 1"), Is.EqualTo(3));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection.Count(e => e == "sub 2"), Is.EqualTo(2));
            Assert.That(explodedCollection, Contains.Item("sub 3"));
            Assert.That(explodedCollection.Count(e => e == "sub 3"), Is.EqualTo(2));
            Assert.That(explodedCollection, Does.Not.Contain("third"));
            Assert.That(explodedCollection.Count, Is.EqualTo(8));
        }

        [Test]
        public void ExplodeAndPreserveDuplicates_ExplodeCollectionPreservingDuplicatesWithSubCollectionsAndSelfAsSubCollection()
        {
            allCollections["entry"] = new[] { "first", "second", "entry" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };

            var explodedCollection = selector.ExplodeAndPreserveDuplicates(AssemblyName, TableName, "entry");
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("entry"));
            Assert.That(explodedCollection.Count, Is.EqualTo(4));
        }

        [Test]
        public void BUG_ExplodeAndPreserveDuplicates_LargeCollection()
        {
            var counts = Enumerable.Range(1, 100);
            allCollections["entry"] = counts.SelectMany(c => new[] { $"entry {c}" });

            foreach (var entry in allCollections["entry"].Take(10))
            {
                allCollections[entry] = counts.Take(10).SelectMany(c => new[] { $"{entry}.{c}" });

                foreach (var subentry in allCollections[entry])
                {
                    allCollections[subentry] = counts.Take(10).SelectMany(c => new[] { $"{subentry}.{c}" });

                    foreach (var subsubentry in allCollections[subentry])
                    {
                        allCollections[subsubentry] = counts.Take(10).SelectMany(c => new[] { $"{subsubentry}.{c}", $"{subsubentry}.{c}" });
                    }
                }
            }

            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var explodedCollection = selector.ExplodeAndPreserveDuplicates(AssemblyName, TableName, "entry");
            stopwatch.Stop();

            Assert.That(explodedCollection.Count, Is.EqualTo(20_090));

            var array = explodedCollection.ToArray();
            var i = 0;

            foreach (var c1 in counts)
            {
                if (c1 > 10)
                {
                    Assert.That(array[i++], Is.EqualTo($"entry {c1}"));
                    continue;
                }

                foreach (var c2 in counts.Take(10))
                {
                    foreach (var c3 in counts.Take(10))
                    {
                        foreach (var c4 in counts.Take(10))
                        {
                            Assert.That(array[i++], Is.EqualTo($"entry {c1}.{c2}.{c3}.{c4}"));
                            Assert.That(array[i++], Is.EqualTo($"entry {c1}.{c2}.{c3}.{c4}"));
                        }
                    }
                }
            }

            Assert.That(i, Is.EqualTo(20_090));
            Assert.That(stopwatch.Elapsed.TotalSeconds, Is.LessThan(2.009));
        }

        [Test]
        public void FlattenCollection()
        {
            var otherCollections = new Dictionary<string, IEnumerable<string>>();
            otherCollections["first"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["sub 1"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["sub 2"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["fourth"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["third"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

            var keys = new[]
            {
                "first",
                "second",
                "sub 1",
                "sub 2",
                "third",
            };

            var flattenedCollection = selector.Flatten(otherCollections, keys);
            Assert.That(flattenedCollection, Is.SupersetOf(otherCollections["first"]), "first");
            Assert.That(flattenedCollection, Is.SupersetOf(otherCollections["sub 1"]), "sub 1");
            Assert.That(flattenedCollection, Is.SupersetOf(otherCollections["sub 2"]), "sub 2");
            Assert.That(flattenedCollection, Is.SupersetOf(otherCollections["third"]), "third");
            Assert.That(flattenedCollection, Is.Not.SupersetOf(otherCollections["fourth"]), "fourth");
            Assert.That(flattenedCollection, Does.Not.Contain("first"));
            Assert.That(flattenedCollection, Does.Not.Contain("second"));
            Assert.That(flattenedCollection, Does.Not.Contain("sub 1"));
            Assert.That(flattenedCollection, Does.Not.Contain("sub 2"));
            Assert.That(flattenedCollection, Does.Not.Contain("third"));
            Assert.That(flattenedCollection, Does.Not.Contain("fourth"));
        }

        [Test]
        public void FlattenCollectionDistinctly()
        {
            var otherCollections = new Dictionary<string, IEnumerable<string>>();
            otherCollections["first"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["sub 1"] = new[] { Guid.NewGuid().ToString(), "repeat" };
            otherCollections["sub 2"] = new[] { Guid.NewGuid().ToString(), "repeat" };
            otherCollections["fourth"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["third"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

            var keys = new[]
            {
                "first",
                "second",
                "sub 1",
                "sub 2",
                "third",
            };

            var flattenedCollection = selector.Flatten(otherCollections, keys);
            Assert.That(flattenedCollection, Is.SupersetOf(otherCollections["first"]), "first");
            Assert.That(flattenedCollection, Is.SupersetOf(otherCollections["sub 1"]), "sub 1");
            Assert.That(flattenedCollection, Is.SupersetOf(otherCollections["sub 2"]), "sub 2");
            Assert.That(flattenedCollection, Is.SupersetOf(otherCollections["third"]), "third");
            Assert.That(flattenedCollection, Is.Not.SupersetOf(otherCollections["fourth"]), "fourth");
            Assert.That(flattenedCollection, Does.Not.Contain("first"));
            Assert.That(flattenedCollection, Does.Not.Contain("second"));
            Assert.That(flattenedCollection, Does.Not.Contain("sub 1"));
            Assert.That(flattenedCollection, Does.Not.Contain("sub 2"));
            Assert.That(flattenedCollection, Does.Not.Contain("third"));
            Assert.That(flattenedCollection, Does.Not.Contain("fourth"));
            Assert.That(flattenedCollection.Count(i => i == "repeat"), Is.EqualTo(1));
        }

        [Test]
        public void CreateWeighted_CreateEmptyWeightedCollectionWithDefault()
        {
            var weightedCollection = selector.CreateWeighted<string>();
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Empty);
        }

        [Test]
        public void CreateWeighted_CreateEmptyNonStringWeightedCollectionWithDefault()
        {
            var weightedCollection = selector.CreateWeighted<int>();
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Empty);
        }

        [Test]
        public void CreateWeighted_CreateEmptyWeightedCollection()
        {
            var common = new List<string>();
            var uncommon = new List<string>();
            var rare = new List<string>();
            var veryRare = new List<string>();

            var weightedCollection = selector.CreateWeighted(common, uncommon, rare, veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Empty);
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollection()
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(common, uncommon, rare, veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(30));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(60));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateNonStringWeightedCollection()
        {
            var common = new[] { 9266 };
            var uncommon = new[] { 90210 };
            var rare = new[] { 42 };
            var veryRare = new[] { 600 };

            var weightedCollection = selector.CreateWeighted(common, uncommon, rare, veryRare);
            Assert.That(weightedCollection, Is.Not.Null.And.Not.Empty
                .And.Contains(9266)
                .And.Contains(90210)
                .And.Contains(42)
                .And.Contains(600));
            Assert.That(weightedCollection.Count(i => i == veryRare.Single()), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == rare.Single()), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(i => i == uncommon.Single()), Is.EqualTo(30));
            Assert.That(weightedCollection.Count(i => i == common.Single()), Is.EqualTo(60));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionWithNullCommon()
        {
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(uncommon: uncommon, rare: rare, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(90));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionWithNullUncommon()
        {
            var common = new[] { "common" };
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(common, rare: rare, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(90));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionWithNullRare()
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(common, uncommon, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(33));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(66));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionWithNullVeryRare()
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };

            var weightedCollection = selector.CreateWeighted(common, uncommon, rare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(3));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(6));
            Assert.That(weightedCollection.Count(), Is.EqualTo(10));
        }

        [TestCaseSource(typeof(WeightTestData), nameof(WeightTestData.Two))]
        public void CreateWeighted_CreateWeightedCollectionWithDuplicateRareAgainstVeryRare(int rareQuantity, int veryRareQuantity)
        {
            var rare = Enumerable.Range(1, rareQuantity).Select(i => $"rare {i}");
            var veryRare = Enumerable.Range(1, veryRareQuantity).Select(i => $"very rare {i}");

            var weightedCollection = selector.CreateWeighted(rare: rare, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(veryRare, Is.SubsetOf(weightedCollection));
            Assert.That(rare, Is.SubsetOf(weightedCollection));

            foreach (var veryRareEntry in veryRare)
                Assert.That(weightedCollection.Count(i => i == veryRareEntry), Is.EqualTo(1), veryRareEntry);

            var rareDuplicationCount = Convert.ToInt32(Math.Ceiling(veryRareQuantity / (double)rareQuantity * 9));

            foreach (var rareEntry in rare)
                Assert.That(weightedCollection.Count(i => i == rareEntry), Is.EqualTo(rareDuplicationCount), rareEntry);

            var expectedCount = rare.Count() * rareDuplicationCount + veryRareQuantity;
            Assert.That(weightedCollection.Count(), Is.EqualTo(expectedCount));

            var rareWeight = weightedCollection.Count(i => rare.Contains(i)) / (double)weightedCollection.Count();
            Assert.That(rareWeight, Is.AtLeast(.9));
        }

        private class WeightTestData
        {
            private static IEnumerable<int> quantities = new[]
            {
                1, 2, 9, 10, 11, 42, 96
            };

            public static IEnumerable Two
            {
                get
                {
                    foreach (var value1 in quantities)
                    {
                        foreach (var value2 in quantities)
                        {
                            yield return new TestCaseData(value1, value2);
                        }
                    }
                }
            }

            public static IEnumerable Three
            {
                get
                {
                    foreach (var value1 in quantities)
                    {
                        foreach (var value2 in quantities)
                        {
                            foreach (var value3 in quantities)
                            {
                                yield return new TestCaseData(value1, value2, value3);
                            }
                        }
                    }
                }
            }

            public static IEnumerable Four
            {
                get
                {
                    foreach (var value1 in quantities)
                    {
                        foreach (var value2 in quantities)
                        {
                            foreach (var value3 in quantities)
                            {
                                foreach (var value4 in quantities)
                                {
                                    yield return new TestCaseData(value1, value2, value3, value4);
                                }
                            }
                        }
                    }
                }
            }
        }

        [TestCaseSource(typeof(WeightTestData), nameof(WeightTestData.Two))]
        public void CreateWeighted_CreateWeightedCollectionWithDuplicateUncommonAgainstRare(int uncommonQuantity, int rareQuantity)
        {
            var uncommon = Enumerable.Range(1, uncommonQuantity).Select(i => $"uncommon {i}");
            var rare = Enumerable.Range(1, rareQuantity).Select(i => $"rare {i}");

            var weightedCollection = selector.CreateWeighted(uncommon: uncommon, rare: rare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(rare, Is.SubsetOf(weightedCollection));
            Assert.That(uncommon, Is.SubsetOf(weightedCollection));

            foreach (var rareEntry in rare)
                Assert.That(weightedCollection.Count(i => i == rareEntry), Is.EqualTo(1), rareEntry);

            var uncommonDuplicationCount = Convert.ToInt32(Math.Ceiling(rareQuantity / (double)uncommonQuantity * 9));

            foreach (var uncommonEntry in uncommon)
                Assert.That(weightedCollection.Count(i => i == uncommonEntry), Is.EqualTo(uncommonDuplicationCount), uncommonEntry);

            var expectedCount = uncommon.Count() * uncommonDuplicationCount + rareQuantity;
            Assert.That(weightedCollection.Count(), Is.EqualTo(expectedCount));

            var uncommonWeight = weightedCollection.Count(i => uncommon.Contains(i)) / (double)weightedCollection.Count();
            Assert.That(uncommonWeight, Is.AtLeast(.9));
        }

        [TestCaseSource(typeof(WeightTestData), nameof(WeightTestData.Two))]
        public void CreateWeighted_CreateWeightedCollectionWithDuplicateUncommonAgainstVeryRare(int uncommonQuantity, int veryRareQuantity)
        {
            var uncommon = Enumerable.Range(1, uncommonQuantity).Select(i => $"uncommon {i}");
            var veryRare = Enumerable.Range(1, veryRareQuantity).Select(i => $"very rare {i}");

            var weightedCollection = selector.CreateWeighted(uncommon, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(veryRare, Is.SubsetOf(weightedCollection));
            Assert.That(uncommon, Is.SubsetOf(weightedCollection));

            foreach (var veryRareEntry in veryRare)
                Assert.That(weightedCollection.Count(i => i == veryRareEntry), Is.EqualTo(1), veryRareEntry);

            var uncommonDuplicationCount = Convert.ToInt32(Math.Ceiling(Math.Round(veryRareQuantity / (double)uncommonQuantity * 99, 3)));

            foreach (var uncommonEntry in uncommon)
                Assert.That(weightedCollection.Count(i => i == uncommonEntry), Is.EqualTo(uncommonDuplicationCount), uncommonEntry);

            var expectedCount = uncommon.Count() * uncommonDuplicationCount + veryRareQuantity;
            Assert.That(weightedCollection.Count(), Is.EqualTo(expectedCount));

            var uncommonWeight = weightedCollection.Count(i => uncommon.Contains(i)) / (double)weightedCollection.Count();
            Assert.That(uncommonWeight, Is.AtLeast(.99));
        }

        [TestCaseSource(typeof(WeightTestData), nameof(WeightTestData.Three))]
        public void CreateWeighted_CreateWeightedCollectionWithDuplicateUncommonAgainstRareAndVeryRare(int uncommonQuantity, int rareQuantity, int veryRareQuantity)
        {
            var uncommon = Enumerable.Range(1, uncommonQuantity).Select(i => $"uncommon {i}");
            var rare = Enumerable.Range(1, rareQuantity).Select(i => $"rare {i}");
            var veryRare = Enumerable.Range(1, veryRareQuantity).Select(i => $"very rare {i}");

            var weightedCollection = selector.CreateWeighted(uncommon: uncommon, rare: rare, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(veryRare, Is.SubsetOf(weightedCollection));
            Assert.That(uncommon, Is.SubsetOf(weightedCollection));

            foreach (var veryRareEntry in veryRare)
                Assert.That(weightedCollection.Count(i => i == veryRareEntry), Is.EqualTo(1), veryRareEntry);

            var rareDuplicationCount = Convert.ToInt32(Math.Ceiling(veryRareQuantity / (double)rareQuantity * 9));

            foreach (var rareEntry in rare)
                Assert.That(weightedCollection.Count(i => i == rareEntry), Is.EqualTo(rareDuplicationCount), rareEntry);

            var uncommonDuplicationCount = Convert.ToInt32(Math.Ceiling((veryRareQuantity + rareQuantity * rareDuplicationCount) / (double)uncommonQuantity * 9));

            foreach (var uncommonEntry in uncommon)
                Assert.That(weightedCollection.Count(i => i == uncommonEntry), Is.EqualTo(uncommonDuplicationCount), uncommonEntry);

            var expectedCount = uncommon.Count() * uncommonDuplicationCount + rare.Count() * rareDuplicationCount + veryRareQuantity;
            Assert.That(weightedCollection.Count(), Is.EqualTo(expectedCount));

            var uncommonWeight = weightedCollection.Count(i => uncommon.Contains(i)) / (double)weightedCollection.Count();
            Assert.That(uncommonWeight, Is.AtLeast(.9));
        }

        [TestCaseSource(typeof(WeightTestData), nameof(WeightTestData.Two))]
        public void CreateWeighted_CreateWeightedCollectionWithDuplicateCommonAgainstRare(int commonQuantity, int rareQuantity)
        {
            var common = Enumerable.Range(1, commonQuantity).Select(i => $"common {i}");
            var rare = Enumerable.Range(1, rareQuantity).Select(i => $"rare {i}");

            var weightedCollection = selector.CreateWeighted(common, rare: rare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(rare, Is.SubsetOf(weightedCollection));
            Assert.That(common, Is.SubsetOf(weightedCollection));

            foreach (var rareEntry in rare)
                Assert.That(weightedCollection.Count(i => i == rareEntry), Is.EqualTo(1), rareEntry);

            var commonDuplicationCount = Convert.ToInt32(Math.Ceiling(rareQuantity / (double)commonQuantity * 9));

            foreach (var commonEntry in common)
                Assert.That(weightedCollection.Count(i => i == commonEntry), Is.EqualTo(commonDuplicationCount), commonEntry);

            var expectedCount = common.Count() * commonDuplicationCount + rareQuantity;
            Assert.That(weightedCollection.Count(), Is.EqualTo(expectedCount));

            var commonWeight = weightedCollection.Count(i => common.Contains(i)) / (double)weightedCollection.Count();
            Assert.That(commonWeight, Is.AtLeast(.9));
        }

        [TestCaseSource(typeof(WeightTestData), nameof(WeightTestData.Two))]
        public void CreateWeighted_CreateWeightedCollectionWithDuplicateCommonAgainstVeryRare(int commonQuantity, int veryRareQuantity)
        {
            var common = Enumerable.Range(1, commonQuantity).Select(i => $"common {i}");
            var veryRare = Enumerable.Range(1, veryRareQuantity).Select(i => $"very rare {i}");

            var weightedCollection = selector.CreateWeighted(common, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(veryRare, Is.SubsetOf(weightedCollection));
            Assert.That(common, Is.SubsetOf(weightedCollection));

            foreach (var veryRareEntry in veryRare)
                Assert.That(weightedCollection.Count(i => i == veryRareEntry), Is.EqualTo(1), veryRareEntry);

            var commonDuplicationCount = Convert.ToInt32(Math.Ceiling(Math.Round(veryRareQuantity / (double)commonQuantity * 99, 3)));

            foreach (var commonEntry in common)
                Assert.That(weightedCollection.Count(i => i == commonEntry), Is.EqualTo(commonDuplicationCount), commonEntry);

            var expectedCount = common.Count() * commonDuplicationCount + veryRareQuantity;
            Assert.That(weightedCollection.Count(), Is.EqualTo(expectedCount));

            var commonWeight = weightedCollection.Count(i => common.Contains(i)) / (double)weightedCollection.Count();
            Assert.That(commonWeight, Is.AtLeast(.99));
        }

        [TestCaseSource(typeof(WeightTestData), nameof(WeightTestData.Two))]
        public void CreateWeighted_CreateWeightedCollectionWithDuplicateCommonAgainstUncommon(int commonQuantity, int uncommonQuantity)
        {
            var common = Enumerable.Range(1, commonQuantity).Select(i => $"common {i}");
            var uncommon = Enumerable.Range(1, uncommonQuantity).Select(i => $"uncommon {i}");

            var weightedCollection = selector.CreateWeighted(common, uncommon);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(uncommon, Is.SubsetOf(weightedCollection));
            Assert.That(common, Is.SubsetOf(weightedCollection));

            foreach (var uncommonEntry in uncommon)
                Assert.That(weightedCollection.Count(i => i == uncommonEntry), Is.EqualTo(1), uncommonEntry);

            var commonDuplicationCount = Convert.ToInt32(Math.Ceiling(uncommonQuantity / (double)commonQuantity * 2));

            foreach (var commonEntry in common)
                Assert.That(weightedCollection.Count(i => i == commonEntry), Is.EqualTo(commonDuplicationCount), commonEntry);

            var expectedCount = common.Count() * commonDuplicationCount + uncommonQuantity;
            Assert.That(weightedCollection.Count(), Is.EqualTo(expectedCount));

            var commonWeight = weightedCollection.Count(i => common.Contains(i)) / (double)weightedCollection.Count();
            Assert.That(commonWeight, Is.AtLeast(2 / 3d));
        }

        [TestCaseSource(typeof(WeightTestData), nameof(WeightTestData.Three))]
        public void CreateWeighted_CreateWeightedCollectionWithDuplicateCommonAgainstRareAndVeryRare(int commonQuantity, int rareQuantity, int veryRareQuantity)
        {
            var common = Enumerable.Range(1, commonQuantity).Select(i => $"common {i}");
            var rare = Enumerable.Range(1, rareQuantity).Select(i => $"rare {i}");
            var veryRare = Enumerable.Range(1, veryRareQuantity).Select(i => $"very rare {i}");

            var weightedCollection = selector.CreateWeighted(common, rare: rare, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(veryRare, Is.SubsetOf(weightedCollection));
            Assert.That(common, Is.SubsetOf(weightedCollection));

            foreach (var veryRareEntry in veryRare)
                Assert.That(weightedCollection.Count(i => i == veryRareEntry), Is.EqualTo(1), veryRareEntry);

            var rareDuplicationCount = Convert.ToInt32(Math.Ceiling(veryRareQuantity / (double)rareQuantity * 9));

            foreach (var rareEntry in rare)
                Assert.That(weightedCollection.Count(i => i == rareEntry), Is.EqualTo(rareDuplicationCount), rareEntry);

            var commonDuplicationCount = Convert.ToInt32(Math.Ceiling((veryRareQuantity + rareQuantity * rareDuplicationCount) / (double)commonQuantity * 9));

            foreach (var commonEntry in common)
                Assert.That(weightedCollection.Count(i => i == commonEntry), Is.EqualTo(commonDuplicationCount), commonEntry);

            var expectedCount = common.Count() * commonDuplicationCount + rare.Count() * rareDuplicationCount + veryRareQuantity;
            Assert.That(weightedCollection.Count(), Is.EqualTo(expectedCount));

            var commonWeight = weightedCollection.Count(i => common.Contains(i)) / (double)weightedCollection.Count();
            Assert.That(commonWeight, Is.AtLeast(.9));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfJustCommon()
        {
            var common = new[] { "common" };

            var weightedCollection = selector.CreateWeighted(common);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfJustUncommon()
        {
            var uncommon = new[] { "uncommon" };

            var weightedCollection = selector.CreateWeighted(uncommon);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfJustRare()
        {
            var rare = new[] { "rare" };

            var weightedCollection = selector.CreateWeighted(rare: rare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfJustVeryRare()
        {
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(), Is.EqualTo(1));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfCommonAndUncommon()
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };

            var weightedCollection = selector.CreateWeighted(common, uncommon);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(2));
            Assert.That(weightedCollection.Count(), Is.EqualTo(3));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfCommonAndRare()
        {
            var common = new[] { "common" };
            var rare = new[] { "rare" };

            var weightedCollection = selector.CreateWeighted(common, rare: rare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(), Is.EqualTo(10));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfCommonAndVeryRare()
        {
            var common = new[] { "common" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(common, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(99));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfUncommonAndRare()
        {
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };

            var weightedCollection = selector.CreateWeighted(uncommon: uncommon, rare: rare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(), Is.EqualTo(10));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfUncommonAndVeryRare()
        {
            var uncommon = new[] { "uncommon" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(uncommon: uncommon, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(99));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfRareAndVeryRare()
        {
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(rare: rare, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(), Is.EqualTo(10));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfCommonAndUncommonAndRare()
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };

            var weightedCollection = selector.CreateWeighted(common, uncommon, rare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(3));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(6));
            Assert.That(weightedCollection.Count(), Is.EqualTo(10));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfCommonAndUncommonAndVeryRare()
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(common, uncommon, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(33));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(66));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfCommonAndRareAndVeryRare()
        {
            var common = new[] { "common" };
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(common, rare: rare, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("common"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(i => i == "common"), Is.EqualTo(90));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void CreateWeighted_CreateWeightedCollectionOfUncommonAndRareAndVeryRare()
        {
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            var weightedCollection = selector.CreateWeighted(uncommon, rare: rare, veryRare: veryRare);
            Assert.That(weightedCollection, Is.Not.Null);
            Assert.That(weightedCollection, Is.Not.Empty);
            Assert.That(weightedCollection, Contains.Item("very rare"));
            Assert.That(weightedCollection, Contains.Item("rare"));
            Assert.That(weightedCollection, Contains.Item("uncommon"));
            Assert.That(weightedCollection.Count(i => i == "very rare"), Is.EqualTo(1));
            Assert.That(weightedCollection.Count(i => i == "rare"), Is.EqualTo(9));
            Assert.That(weightedCollection.Count(i => i == "uncommon"), Is.EqualTo(90));
            Assert.That(weightedCollection.Count(), Is.EqualTo(100));
        }

        [Test]
        public void SelectRandomFrom_SelectRandomFromEmptyWeightedCollectionWithDefault()
        {
            Assert.That(() => selector.SelectRandomFrom<string>(), Throws.Exception);
        }

        [Test]
        public void SelectRandomFrom_SelectRandomFromEmptyWeightedCollection()
        {
            var common = new List<string>();
            var uncommon = new List<string>();
            var rare = new List<string>();
            var veryRare = new List<string>();

            Assert.That(() => selector.SelectRandomFrom(common, uncommon, rare, veryRare), Throws.Exception);
        }

        [TestCase("very rare", 100, 100)]
        [TestCase("rare", 91, 99)]
        [TestCase("uncommon", 61, 90)]
        [TestCase("common", 1, 60)]
        public void SelectRandomFrom_SelectRandomFromWeightedCollection(string result, int lower, int upper)
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            mockDice.Setup(d => d.Roll(1).d(1).AsSum<int>()).Returns(1);

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(common, uncommon, rare, veryRare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Roll {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Roll {i}");
                }
            }
        }

        //Because common is null, we build the weighted collection
        [TestCase("very rare", 1, 1)]
        [TestCase("rare", 2, 10)]
        [TestCase("uncommon", 11, 100)]
        public void SelectRandomFrom_SelectRandomFromWeightedCollectionWithNullCommon(string result, int lower, int upper)
        {
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(uncommon: uncommon, rare: rare, veryRare: veryRare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Roll {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Roll {i}");
                }
            }
        }

        [TestCase("very rare", 100, 100)]
        [TestCase("rare", 91, 99)]
        [TestCase("common", 1, 90)]
        public void SelectRandomFrom_SelectRandomFromWeightedCollectionWithNullUncommon(string result, int lower, int upper)
        {
            var common = new[] { "common" };
            var rare = new[] { "rare" };
            var veryRare = new[] { "very rare" };

            mockDice.Setup(d => d.Roll(1).d(1).AsSum<int>()).Returns(1);

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(common, rare: rare, veryRare: veryRare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Roll {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Roll {i}");
                }
            }
        }

        [TestCase("very rare", 100, 100)]
        [TestCase("uncommon", 61, 99)]
        [TestCase("common", 1, 60)]
        public void SelectRandomFrom_SelectRandomFromWeightedCollectionWithNullRare(string result, int lower, int upper)
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };
            var veryRare = new[] { "very rare" };

            mockDice.Setup(d => d.Roll(1).d(1).AsSum<int>()).Returns(1);

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(common, uncommon, veryRare: veryRare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Roll {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Roll {i}");
                }
            }
        }

        [TestCase("rare", 91, 100)]
        [TestCase("uncommon", 61, 90)]
        [TestCase("common", 1, 60)]
        public void SelectRandomFrom_SelectRandomFromWeightedCollectionWithNullVeryRare(string result, int lower, int upper)
        {
            var common = new[] { "common" };
            var uncommon = new[] { "uncommon" };
            var rare = new[] { "rare" };

            mockDice.Setup(d => d.Roll(1).d(1).AsSum<int>()).Returns(1);

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(common, uncommon, rare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Roll {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Roll {i}");
                }
            }
        }

        [Test]
        public void SelectRandomFrom_SelectRandomNonStringFromEmptyWeightedCollectionWithDefault()
        {
            Assert.That(() => selector.SelectRandomFrom<int>(), Throws.Exception);
        }

        [Test]
        public void SelectRandomFrom_SelectRandomNonStringFromEmptyWeightedCollection()
        {
            var common = new List<int>();
            var uncommon = new List<int>();
            var rare = new List<int>();
            var veryRare = new List<int>();

            Assert.That(() => selector.SelectRandomFrom(common, uncommon, rare, veryRare), Throws.Exception);
        }

        [TestCase(9266, 100, 100)]
        [TestCase(90210, 91, 99)]
        [TestCase(42, 61, 90)]
        [TestCase(600, 1, 60)]
        public void SelectRandomFrom_SelectRandomNonStringFromWeightedCollection(int result, int lower, int upper)
        {
            var common = new[] { 600 };
            var uncommon = new[] { 42 };
            var rare = new[] { 90210 };
            var veryRare = new[] { 9266 };

            mockDice.Setup(d => d.Roll(1).d(1).AsSum<int>()).Returns(1);

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(common, uncommon, rare, veryRare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Roll {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Roll {i}");
                }
            }
        }

        //Because common is null, we build the weighted collection
        [TestCase(9266, 1, 1)]
        [TestCase(90210, 2, 10)]
        [TestCase(42, 11, 100)]
        public void SelectRandomFrom_SelectRandomNonStringFromWeightedCollectionWithNullCommon(int result, int lower, int upper)
        {
            var uncommon = new[] { 42 };
            var rare = new[] { 90210 };
            var veryRare = new[] { 9266 };

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(uncommon: uncommon, rare: rare, veryRare: veryRare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Index {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Index {i}");
                }
            }
        }

        [TestCase(9266, 100, 100)]
        [TestCase(90210, 91, 99)]
        [TestCase(42, 1, 90)]
        public void SelectRandomFrom_SelectRandomNonStringFromWeightedCollectionWithNullUncommon(int result, int lower, int upper)
        {
            var common = new[] { 42 };
            var rare = new[] { 90210 };
            var veryRare = new[] { 9266 };

            mockDice.Setup(d => d.Roll(1).d(1).AsSum<int>()).Returns(1);

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(common, rare: rare, veryRare: veryRare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Index {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Index {i}");
                }
            }
        }

        [TestCase(9266, 100, 100)]
        [TestCase(90210, 61, 99)]
        [TestCase(42, 1, 60)]
        public void SelectRandomFrom_SelectRandomNonStringFromWeightedCollectionWithNullRare(int result, int lower, int upper)
        {
            var common = new[] { 42 };
            var uncommon = new[] { 90210 };
            var veryRare = new[] { 9266 };

            mockDice.Setup(d => d.Roll(1).d(1).AsSum<int>()).Returns(1);

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(common, uncommon, veryRare: veryRare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Index {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Index {i}");
                }
            }
        }

        [TestCase(9266, 91, 100)]
        [TestCase(90210, 61, 90)]
        [TestCase(42, 1, 60)]
        public void SelectRandomFrom_SelectRandomNonStringFromWeightedCollectionWithNullVeryRare(int result, int lower, int upper)
        {
            var common = new[] { 42 };
            var uncommon = new[] { 90210 };
            var rare = new[] { 9266 };

            mockDice.Setup(d => d.Roll(1).d(1).AsSum<int>()).Returns(1);

            for (var i = 1; i <= 100; i++)
            {
                mockDice.Setup(d => d.Roll(1).d(100).AsSum<int>()).Returns(i);

                var random = selector.SelectRandomFrom(common, uncommon, rare);

                if (lower <= i && i <= upper)
                {
                    Assert.That(random, Is.EqualTo(result), $"Index {i}");
                }
                else
                {
                    Assert.That(random, Is.Not.EqualTo(result), $"Index {i}");
                }
            }
        }
    }
}
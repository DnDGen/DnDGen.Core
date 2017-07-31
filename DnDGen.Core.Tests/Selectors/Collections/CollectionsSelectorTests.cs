﻿using DnDGen.Core.Mappers.Collections;
using DnDGen.Core.Selectors.Collections;
using Moq;
using NUnit.Framework;
using RollGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDGen.Core.Tests.Selectors.Collections
{
    [TestFixture]
    public class CollectionsSelectorTests
    {
        private const string TableName = "table name";

        private ICollectionsSelector selector;
        private Mock<CollectionsMapper> mockMapper;
        private Mock<Dice> mockDice;
        private Dictionary<string, IEnumerable<string>> allCollections;

        [SetUp]
        public void Setup()
        {
            mockMapper = new Mock<CollectionsMapper>();
            mockDice = new Mock<Dice>();
            selector = new CollectionsSelector(mockMapper.Object, mockDice.Object);
            allCollections = new Dictionary<string, IEnumerable<string>>();

            mockMapper.Setup(m => m.Map(TableName)).Returns(allCollections);
        }

        [Test]
        public void SelectCollection()
        {
            allCollections["entry"] = Enumerable.Empty<string>();
            var collection = selector.SelectFrom(TableName, "entry");
            Assert.That(collection, Is.EqualTo(allCollections["entry"]));
        }

        [Test]
        public void SelectAllCollections()
        {
            var collections = selector.SelectAllFrom(TableName);
            Assert.That(collections, Is.EqualTo(allCollections));
        }

        [Test]
        public void IfEntryNotPresentInTable_ThrowException()
        {
            Assert.That(() => selector.SelectFrom(TableName, "entry"), Throws.Exception.With.Message.EqualTo("entry is not a valid collection in the table table name"));
        }

        [Test]
        public void SelectRandomItemFromCollection()
        {
            var collection = new[] { "item 1", "item 2", "item 3" };
            mockDice.Setup(d => d.Roll(1).d(3).AsSum()).Returns(2);

            var item = selector.SelectRandomFrom(collection);
            Assert.That(item, Is.EqualTo("item 2"));
        }

        [Test]
        public void SelectRandomItemFromTable()
        {
            allCollections["entry"] = new[] { "item 1", "item 2", "item 3" };
            mockDice.Setup(d => d.Roll(1).d(3).AsSum()).Returns(2);

            var item = selector.SelectRandomFrom(TableName, "entry");
            Assert.That(item, Is.EqualTo("item 2"));
        }

        [Test]
        public void CannotSelectRandomFromEmptyCollection()
        {
            var collection = Enumerable.Empty<string>();
            Assert.That(() => selector.SelectRandomFrom(collection), Throws.ArgumentException.With.Message.EqualTo("Cannot select random from an empty collection"));
        }

        [Test]
        public void CannotSelectRandomFromEmptyTable()
        {
            allCollections["entry"] = Enumerable.Empty<string>();
            Assert.That(() => selector.SelectRandomFrom(TableName, "entry"), Throws.ArgumentException.With.Message.EqualTo("Cannot select random from an empty collection"));
        }

        [Test]
        public void CannotSelectRandomFromInvalidEntry()
        {
            Assert.That(() => selector.SelectRandomFrom(TableName, "entry"), Throws.Exception.With.Message.EqualTo("entry is not a valid collection in the table table name"));
        }

        [Test]
        public void SelectRandomFromNonStringCollection()
        {
            var collection = new[] { 9266, 90210, 42, 600, 1337 };

            mockDice.Setup(d => d.Roll(1).d(5).AsSum()).Returns(2);

            var entry = selector.SelectRandomFrom(collection);
            Assert.That(entry, Is.EqualTo(90210));
        }

        [Test]
        public void FindCollectionContainingEntry()
        {
            allCollections["entry"] = new[] { "first", "fourth" };
            allCollections["other entry"] = new[] { "third", "fourth" };
            allCollections["wrong entry"] = new[] { "fifth", "second" };

            var collectionName = selector.FindCollectionOf(TableName, "fourth");
            Assert.That(collectionName, Is.EqualTo("entry"));
        }

        [Test]
        public void DoNotFindCollectionContainingEntry()
        {
            allCollections["entry"] = new[] { "first", "fourth" };
            allCollections["other entry"] = new[] { "third", "fourth" };
            allCollections["wrong entry"] = new[] { "fifth", "second" };

            Assert.That(() => selector.FindCollectionOf(TableName, "sixth"), Throws.ArgumentException.With.Message.EqualTo("No collection in table name contains sixth"));
        }

        [Test]
        public void FindCollectionContainingEntryWithFilteredCollectionNames()
        {
            allCollections["entry"] = new[] { "first", "second" };
            allCollections["other entry"] = new[] { "third", "fourth" };
            allCollections["wrong entry"] = new[] { "fifth", "fourth" };

            var group = selector.FindCollectionOf(TableName, "fourth", "entry", "other entry");
            Assert.That(group, Is.EqualTo("other entry"));
        }

        [Test]
        public void FindCollectionContainingEntryThrowsExceptionIfNotInFilteredCollectionNames()
        {
            allCollections["entry"] = new[] { "first", "second" };
            allCollections["other entry"] = new[] { "third", "fifth" };
            allCollections["wrong entry"] = new[] { "third", "fourth" };

            Assert.That(() => selector.FindCollectionOf(TableName, "fourth", "entry", "other entry"), Throws.ArgumentException.With.Message.EqualTo("No collection from the 2 filters in table name contains fourth"));
        }

        [Test]
        public void IsCollection()
        {
            allCollections["entry"] = new[] { "first", "second" };
            var isCollection = selector.IsCollection(TableName, "entry");
            Assert.That(isCollection, Is.True);
        }

        [Test]
        public void IsNotCollection()
        {
            allCollections["entry"] = new[] { "first", "second" };
            var isCollection = selector.IsCollection(TableName, "other entry");
            Assert.That(isCollection, Is.False);
        }

        [Test]
        public void EntryIsNotCollection()
        {
            allCollections["entry"] = new[] { "first", "second" };
            var isCollection = selector.IsCollection(TableName, "first");
            Assert.That(isCollection, Is.False);
        }

        [Test]
        public void EntryIsCollection()
        {
            allCollections["entry"] = new[] { "first", "second" };
            allCollections["first"] = new[] { "first", "third" };

            var isCollection = selector.IsCollection(TableName, "first");
            Assert.That(isCollection, Is.True);
        }

        [Test]
        public void ExplodeCollectionWithoutSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };

            var explodedCollection = selector.Explode(TableName, "entry");
            Assert.That(explodedCollection, Is.EquivalentTo(allCollections["entry"]));
        }

        [Test]
        public void ExplodeCollectionWithSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };

            var explodedCollection = selector.Explode(TableName, "entry");
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("third"));
            Assert.That(explodedCollection.Count, Is.EqualTo(4));
        }

        [Test]
        public void ExplodedCollectionsAreDistinctBetweenSubCollections()
        {
            allCollections["entry"] = new[] { "first", "second", "third" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };
            allCollections["third"] = new[] { "sub 1", "sub 3" };

            var explodedCollection = selector.Explode(TableName, "entry");
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
        public void ExplodedCollectionsAreDistinctBetweenCollectionAndSubCollection()
        {
            allCollections["entry"] = new[] { "first", "second", "third", "sub 1", "sub 3" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };
            allCollections["third"] = new[] { "second", "sub 3" };

            var explodedCollection = selector.Explode(TableName, "entry");
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
        public void ExplodeCollectionWithSubCollectionsAndSelfAsSubCollection()
        {
            allCollections["entry"] = new[] { "first", "second", "entry" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };

            var explodedCollection = selector.Explode(TableName, "entry");
            Assert.That(explodedCollection, Contains.Item("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Contains.Item("sub 1"));
            Assert.That(explodedCollection, Contains.Item("sub 2"));
            Assert.That(explodedCollection, Contains.Item("entry"));
            Assert.That(explodedCollection.Count, Is.EqualTo(4));
        }

        [Test]
        public void ExplodeCollectionWithoutSubCollectionsIntoOtherTable()
        {
            var otherCollections = new Dictionary<string, IEnumerable<string>>();
            mockMapper.Setup(m => m.Map("other table name")).Returns(otherCollections);

            otherCollections["first"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["second"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["sub 1"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["sub 2"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["third"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["fourth"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

            allCollections["entry"] = new[] { "first", "second", "third" };

            var explodedCollection = selector.ExplodeInto(TableName, "entry", "other table name");
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["first"]));
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["second"]));
            Assert.That(explodedCollection, Is.Not.SupersetOf(otherCollections["sub 1"]));
            Assert.That(explodedCollection, Is.Not.SupersetOf(otherCollections["sub 2"]));
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["third"]));
            Assert.That(explodedCollection, Is.Not.SupersetOf(otherCollections["fourth"]));
            Assert.That(explodedCollection, Does.Not.Contain("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Does.Not.Contain("sub 1"));
            Assert.That(explodedCollection, Does.Not.Contain("sub 2"));
            Assert.That(explodedCollection, Does.Not.Contain("third"));
            Assert.That(explodedCollection, Does.Not.Contain("fourth"));
        }

        [Test]
        public void ExplodeCollectionWithSubCollectionsIntoOtherTable()
        {
            var otherCollections = new Dictionary<string, IEnumerable<string>>();
            mockMapper.Setup(m => m.Map("other table name")).Returns(otherCollections);

            otherCollections["first"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["sub 1"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["sub 2"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["fourth"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["third"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

            allCollections["entry"] = new[] { "first", "second", "third" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };

            var explodedCollection = selector.ExplodeInto(TableName, "entry", "other table name");
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["first"]));
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["sub 1"]));
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["sub 2"]));
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["third"]));
            Assert.That(explodedCollection, Is.Not.SupersetOf(otherCollections["fourth"]));
            Assert.That(explodedCollection, Does.Not.Contain("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Does.Not.Contain("sub 1"));
            Assert.That(explodedCollection, Does.Not.Contain("sub 2"));
            Assert.That(explodedCollection, Does.Not.Contain("third"));
            Assert.That(explodedCollection, Does.Not.Contain("fourth"));
        }

        [Test]
        public void ExplodeCollectionWithSubCollectionsIntoOtherTableDistinctly()
        {
            var otherCollections = new Dictionary<string, IEnumerable<string>>();
            mockMapper.Setup(m => m.Map("other table name")).Returns(otherCollections);

            otherCollections["first"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["sub 1"] = new[] { Guid.NewGuid().ToString(), "repeat" };
            otherCollections["sub 2"] = new[] { Guid.NewGuid().ToString(), "repeat" };
            otherCollections["fourth"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };
            otherCollections["third"] = new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() };

            allCollections["entry"] = new[] { "first", "second", "third" };
            allCollections["second"] = new[] { "sub 1", "sub 2" };

            var explodedCollection = selector.ExplodeInto(TableName, "entry", "other table name");
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["first"]));
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["sub 1"]));
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["sub 2"]));
            Assert.That(explodedCollection, Is.SupersetOf(otherCollections["third"]));
            Assert.That(explodedCollection, Is.Not.SupersetOf(otherCollections["fourth"]));
            Assert.That(explodedCollection, Does.Not.Contain("first"));
            Assert.That(explodedCollection, Does.Not.Contain("second"));
            Assert.That(explodedCollection, Does.Not.Contain("sub 1"));
            Assert.That(explodedCollection, Does.Not.Contain("sub 2"));
            Assert.That(explodedCollection, Does.Not.Contain("third"));
            Assert.That(explodedCollection, Does.Not.Contain("fourth"));
            Assert.That(explodedCollection.Count(i => i == "repeat"), Is.EqualTo(1));
        }
    }
}
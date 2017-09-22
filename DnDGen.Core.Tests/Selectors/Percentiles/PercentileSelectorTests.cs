﻿using DnDGen.Core.Mappers.Percentiles;
using DnDGen.Core.Selectors.Percentiles;
using Moq;
using NUnit.Framework;
using RollGen;
using System.Collections.Generic;
using System.Linq;

namespace DnDGen.Core.Tests.Selectors.Percentiles
{
    [TestFixture]
    public class PercentileSelectorTests
    {
        private const string tableName = "table name";

        private IPercentileSelector percentileSelector;
        private Dictionary<int, string> table;
        private Mock<Dice> mockDice;
        private Mock<PercentileMapper> mockPercentileMapper;

        [SetUp]
        public void Setup()
        {
            table = new Dictionary<int, string>();
            for (var i = 1; i <= 5; i++)
                table.Add(i, "content");
            for (var i = 6; i <= 10; i++)
                table.Add(i, i.ToString());

            mockPercentileMapper = new Mock<PercentileMapper>();
            mockPercentileMapper.Setup(p => p.Map(tableName)).Returns(table);

            mockDice = new Mock<Dice>();
            mockDice.Setup(d => d.Roll(1).d(100).AsSum()).Returns(1);
            percentileSelector = new PercentileSelector(mockPercentileMapper.Object, mockDice.Object);
        }

        [TestCase(1, "content")]
        [TestCase(2, "content")]
        [TestCase(3, "content")]
        [TestCase(4, "content")]
        [TestCase(5, "content")]
        [TestCase(6, "6")]
        [TestCase(7, "7")]
        [TestCase(8, "8")]
        [TestCase(9, "9")]
        [TestCase(10, "10")]
        public void GetPercentile(int roll, string content)
        {
            mockDice.Setup(d => d.Roll(1).d(100).AsSum()).Returns(roll);
            var result = percentileSelector.SelectFrom(tableName);
            Assert.That(result, Is.EqualTo(content));
        }

        [Test]
        public void GetAllResultsReturnsAllContentValues()
        {
            var results = percentileSelector.SelectAllFrom(tableName);
            var distinctContent = table.Values.Distinct();

            foreach (var content in distinctContent)
                Assert.That(results, Contains.Item(content));

            var extras = distinctContent.Except(results);
            Assert.That(extras, Is.Empty);
        }

        [Test]
        public void IfRollNotPresentInTable_ThrowException()
        {
            mockDice.Setup(d => d.Roll(1).d(100).AsSum()).Returns(11);
            Assert.That(() => percentileSelector.SelectFrom(tableName), Throws.Exception.With.Message.EqualTo("11 is not a valid entry in the table table name"));
        }

        [Test]
        public void CanConvertPercentileResult()
        {
            mockDice.Setup(d => d.Roll(1).d(100).AsSum()).Returns(6);
            var result = percentileSelector.SelectFrom<int>(tableName);
            Assert.That(result, Is.EqualTo(6));
        }

        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, false)]
        [TestCase(4, false)]
        [TestCase(5, false)]
        [TestCase(6, true)]
        [TestCase(7, true)]
        [TestCase(8, true)]
        [TestCase(9, true)]
        [TestCase(10, true)]
        public void CanConvertPercentileResult(int roll, bool isTrue)
        {
            table.Clear();

            for (var i = 1; i <= 5; i++)
                table.Add(i, false.ToString());
            for (var i = 6; i <= 10; i++)
                table.Add(i, true.ToString());

            mockDice.Setup(d => d.Roll(1).d(100).AsSum()).Returns(roll);

            var result = percentileSelector.SelectFrom<bool>(tableName);
            Assert.That(result, Is.EqualTo(isTrue));
        }

        [TestCase(.5, true)]
        [TestCase(.5, false)]
        [TestCase(.9266, true)]
        [TestCase(.9266, false)]
        [TestCase(.90210, true)]
        [TestCase(.90210, false)]
        [TestCase(.42, true)]
        [TestCase(.42, false)]
        [TestCase(.600, true)]
        [TestCase(.600, false)]
        [TestCase(.1337, true)]
        [TestCase(.1337, false)]
        public void ReturnsPercentileRollAsTrueOrFalse(double chance, bool isTrue)
        {
            mockDice.Setup(d => d.Roll(1).d(100).AsTrueOrFalse(chance)).Returns(isTrue);

            var result = percentileSelector.SelectFrom(chance);
            Assert.That(result, Is.EqualTo(isTrue));
        }

        [TestCase(1)]
        [TestCase(1.0000001)]
        [TestCase(1.5)]
        [TestCase(2)]
        public void DoNotRollIfChanceGreaterThanOrEqualTo100Percent(double chance)
        {
            var result = percentileSelector.SelectFrom(chance);
            Assert.That(result, Is.True);
            mockDice.Verify(d => d.Roll(It.IsAny<int>()), Times.Never);
        }

        [TestCase(0)]
        [TestCase(-0.000000001)]
        [TestCase(-.5)]
        [TestCase(-1)]
        public void DoNotRollIfChanceLessThanOrEqualTo0Percent(double chance)
        {
            var result = percentileSelector.SelectFrom(chance);
            Assert.That(result, Is.False);
            mockDice.Verify(d => d.Roll(It.IsAny<int>()), Times.Never);
        }
    }
}
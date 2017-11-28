﻿using DnDGen.Core.Generators;
using EventGen;
using Ninject;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;

namespace Tests.Integration.DnDGen.Core.Generators
{
    [TestFixture]
    public class IterativeGeneratorTests : IntegrationTests
    {
        [Inject]
        public Generator Generator { get; set; }
        [Inject]
        public Stopwatch Stopwatch { get; set; }
        [Inject]
        public GenEventQueue EventQueue { get; set; }
        [Inject]
        public ClientIDManager ClientIdManager { get; set; }
        [Inject]
        public Random Random { get; set; }

        [SetUp]
        public void Setup()
        {
            Stopwatch.Reset();

            var clientID = Guid.NewGuid();
            ClientIdManager.SetClientID(clientID);
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        public void GeneratorIsEfficient(int iterations)
        {
            var count = 1;
            Stopwatch.Start();

            var result = Generator.Generate(
                () => count++,
                c => c == iterations,
                () => 9266,
                c => $"{c} is not equal to {iterations}",
                "default 9266");

            Stopwatch.Stop();

            Assert.That(result, Is.EqualTo(iterations));
            Assert.That(count, Is.EqualTo(iterations + 1));

            var events = EventQueue.DequeueAllForCurrentThread();
            var expectedCount = GetExpectedEventCount(iterations, 0, false);
            Assert.That(events.Count, Is.EqualTo(expectedCount));

            Assert.That(Stopwatch.Elapsed.TotalSeconds, Is.LessThan(2));
        }

        [TestCase(1, 1)]
        [TestCase(1, 10)]
        [TestCase(1, 100)]
        [TestCase(1, 1000)]
        [TestCase(10, 1)]
        [TestCase(10, 10)]
        [TestCase(10, 100)]
        [TestCase(10, 1000)]
        [TestCase(100, 1)]
        [TestCase(100, 10)]
        [TestCase(100, 100)]
        [TestCase(100, 1000)]
        [TestCase(1000, 1)]
        [TestCase(1000, 10)]
        [TestCase(1000, 100)]
        [TestCase(1000, 1000)]
        public void GeneratorInceptionIsEfficient(int iterations, int subIterations)
        {
            var count = 1;
            Stopwatch.Start();

            var result = Generator.Generate(
                () => BuildInGenerator(count++, subIterations),
                c => c == iterations,
                () => 9266,
                c => $"{c} is not equal to {iterations}",
                "default 9266");

            Stopwatch.Stop();

            Assert.That(result, Is.EqualTo(iterations));
            Assert.That(count, Is.EqualTo(iterations + 1));

            var events = EventQueue.DequeueAllForCurrentThread();
            var expectedCount = GetExpectedEventCount(iterations, subIterations, false);
            Assert.That(events.Count, Is.EqualTo(expectedCount));

            Assert.That(Stopwatch.Elapsed.TotalSeconds, Is.LessThan(7));
        }

        private int BuildInGenerator(int count, int subIterations)
        {
            var subcount = 1;

            var result = Generator.Generate(
                () => subcount++,
                c => c == subIterations,
                () => 90210,
                c => $"{c} is not equal to {subIterations}",
                "default 90210");

            return count;
        }

        [Test]
        public void GeneratorIsEfficientWithDefaults()
        {
            var count = 1;
            Stopwatch.Start();

            var result = Generator.Generate(
                () => count++,
                c => c == 9266,
                () => 90210,
                c => $"{c} is not equal to {9266}",
                "default 90210");

            Stopwatch.Stop();

            Assert.That(result, Is.EqualTo(90210));
            Assert.That(count, Is.EqualTo(Generator.MaxAttempts + 1));

            var events = EventQueue.DequeueAllForCurrentThread();
            var expectedCount = GetExpectedEventCount(Generator.MaxAttempts, 0, true);
            Assert.That(events.Count, Is.EqualTo(expectedCount));

            Assert.That(Stopwatch.Elapsed.TotalSeconds, Is.LessThan(2));
        }

        [Test]
        public void GeneratorInceptionIsEfficientWithDefaults()
        {
            var count = 1;
            Stopwatch.Start();

            var result = Generator.Generate(
                () => BuildInGenerator(count++, 1337),
                c => c == 9266,
                () => 42,
                c => $"{c} is not equal to {9266}",
                "default 42");

            Stopwatch.Stop();

            Assert.That(result, Is.EqualTo(42));
            Assert.That(count, Is.EqualTo(Generator.MaxAttempts + 1));

            var events = EventQueue.DequeueAllForCurrentThread();
            var expectedCount = GetExpectedEventCount(Generator.MaxAttempts, Generator.MaxAttempts, true);
            Assert.That(events.Count, Is.EqualTo(expectedCount));

            Assert.That(Stopwatch.Elapsed.TotalSeconds, Is.LessThan(7));
        }

        private int GetExpectedEventCount(int outerIterations, int innerIterations, bool isDefault)
        {
            if (outerIterations == 0)
                return 0;

            var beginningEvents = 1;
            var endEvents = 1;
            var failureEvents = outerIterations - 1 + Convert.ToInt32(isDefault);
            var totalEvents = beginningEvents + failureEvents + endEvents;

            return totalEvents + outerIterations * GetExpectedEventCount(innerIterations, 0, isDefault);
        }

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(100)]
        public void RareThingOccurs(int chanceDivisor)
        {
            var chance = 1d / chanceDivisor;

            Stopwatch.Start();

            var result = Generator.Generate(
                () => Random.NextDouble(),
                c => c <= chance,
                () => 666,
                c => $"{c} is not less than {chance}",
                "FAILED TO GENERATE");

            Stopwatch.Stop();

            Assert.That(result, Is.LessThan(chance));

            var events = EventQueue.DequeueAllForCurrentThread();
            Assert.That(events.Count, Is.LessThan(Generator.MaxAttempts));

            Assert.That(Stopwatch.Elapsed.TotalSeconds, Is.LessThan(1));
        }

        [TestCase(1, 1)]
        [TestCase(10, 1)]
        [TestCase(100, 1)]
        [TestCase(1, 10)]
        [TestCase(10, 10)]
        [TestCase(100, 10)]
        [TestCase(1, 100)]
        [TestCase(10, 100)]
        [TestCase(100, 100)]
        public void RareThingWithGeneratorInceptionOccurs(int chanceDivisor, int subChanceDivisor)
        {
            var chance = 1d / chanceDivisor;

            Stopwatch.Start();

            var result = Generator.Generate(
                () => BuildRandomWithGenerator(subChanceDivisor),
                c => c <= chance,
                () => 666,
                c => $"{c} is not less than {chance}",
                "FAILED TO GENERATE");

            Stopwatch.Stop();

            Assert.That(result, Is.LessThan(chance));

            var events = EventQueue.DequeueAllForCurrentThread();
            Assert.That(events.Count, Is.LessThan(Generator.MaxAttempts * Generator.MaxAttempts));

            Assert.That(Stopwatch.Elapsed.TotalSeconds, Is.LessThan(1));
        }

        private double BuildRandomWithGenerator(int subChanceDivisor)
        {
            var subchance = 1d / subChanceDivisor;

            var result = Generator.Generate(
                () => Random.NextDouble(),
                c => c <= subchance,
                () => 666,
                c => $"sub {c} is not less than {subchance}",
                "FAILED TO GENERATE SUB");

            return Random.NextDouble();
        }
    }
}

using NUnit.Framework;
using AutoFixture;
using System;
using Shouldly;

namespace PoeShared.Tests
{
    public abstract class FixtureBase
    {
        public Fixture Container { get; private set; }

        [SetUp]
        public void SetUpTest()
        {
            Container = new Fixture();
            Container.OmitAutoProperties = true;
            SetUp();
        }

        protected virtual void SetUp(){}
    }
}
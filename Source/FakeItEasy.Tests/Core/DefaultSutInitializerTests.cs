﻿namespace FakeItEasy.Tests.Core
{
    using System;
    using System.Collections.Generic;
    using FakeItEasy.Core;
    using FakeItEasy.Creation;
    using FakeItEasy.Tests.TestHelpers;
    using FluentAssertions;
    using NUnit.Framework;

    [TestFixture]
    public class DefaultSutInitializerTests
    {
#pragma warning disable 649
        [Fake]
        private IFakeAndDummyManager fakeManager;
#pragma warning restore 649

        private DefaultSutInitializer sutInitializer;

        [SetUp]
        public void Setup()
        {
            Fake.InitializeFixture(this);

            this.sutInitializer = new DefaultSutInitializer(this.fakeManager);
        }

        [Test]
        public void Should_produce_sut_with_fakes_for_all_dependencies()
        {
            // Arrange
            this.StubFakeManagerWithFake<IFoo>();
            this.StubFakeManagerWithFake<object>();

            // Act
            var sut = (TypeWithFakeableDependencies)this.sutInitializer.CreateSut(typeof(TypeWithFakeableDependencies), (x, y) => { });

            // Assert
            sut.FooDependency.Should().BeAFake();
            sut.FooDependency2.Should().BeAFake();
            sut.Dependency.Should().BeAFake();
        }

        [Test]
        public void Should_use_same_instance_when_multiple_arguments_are_of_same_type()
        {
            // Arrange
            this.StubFakeManagerWithFake<IFoo>();
            this.StubFakeManagerWithFake<object>();

            // Act
            var sut = (TypeWithFakeableDependencies)this.sutInitializer.CreateSut(typeof(TypeWithFakeableDependencies), (x, y) => { });

            // Assert
            sut.FooDependency.Should().BeSameAs(sut.FooDependency2);
        }

        [Test]
        public void Should_pass_empty_options_builder_to_fake_manager_for_each_constructor_argument()
        {
            // Arrange
            this.StubFakeManagerWithFake<IFoo>();
            this.StubFakeManagerWithFake<object>();
            var optionsBuilders = new List<Action<IFakeOptions>>();
            A.CallTo(() => this.fakeManager.CreateFake(A<Type>._, A<Action<IFakeOptions>>._))
                .Invokes((Type type, Action<IFakeOptions> optionBuilder) => optionsBuilders.Add(optionBuilder));

            // Act
            this.sutInitializer.CreateSut(typeof(TypeWithFakeableDependencies), (x, y) => { });

            // Assert
            Assert.That(optionsBuilders, Has.Count.EqualTo(2), "not all constructor arguments were configured");
            var fakeOptions = A.Fake<IFakeOptions>();
            foreach (var optionsBuilder in optionsBuilders)
            {
                optionsBuilder.Invoke(fakeOptions);
            }

            A.CallTo(fakeOptions).MustNotHaveHappened();
        }

        [Test]
        public void Should_call_callback_with_each_created_fake()
        {
            // Arrange
            var createdFakes = new Dictionary<Type, object>();

            this.StubFakeManagerWithFake<IFoo>();
            this.StubFakeManagerWithFake<object>();

            // Act
            var sut = (TypeWithFakeableDependencies)this.sutInitializer.CreateSut(typeof(TypeWithFakeableDependencies), createdFakes.Add);

            // Assert
            createdFakes[typeof(IFoo)].Should().BeSameAs(sut.FooDependency);
            createdFakes[typeof(object)].Should().BeSameAs(sut.Dependency);
        }

        private void StubFakeManagerWithFake<T>()
        {
            A.CallTo(() => this.fakeManager.CreateFake(typeof(T), A<Action<IFakeOptions>>._))
                .ReturnsLazily(() => A.Fake<T>());
        }

        public class TypeWithFakeableDependencies
        {
            public TypeWithFakeableDependencies()
            {
            }

            public TypeWithFakeableDependencies(
                IFoo fooDependency,
                IFoo fooDependency2,
                object dependency)
            {
                this.FooDependency = fooDependency;
                this.FooDependency2 = fooDependency2;
                this.Dependency = dependency;
            }

            public IFoo FooDependency { get; private set; }

            public IFoo FooDependency2 { get; private set; }

            public object Dependency { get; private set; }
        }
    }
}
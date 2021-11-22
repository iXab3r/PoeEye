using NUnit.Framework;
using AutoFixture;
using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using PoeShared.Logging;
using PoeShared.Scaffolding;
using PoeShared.Services;
using PoeShared.Tests.Scaffolding;
using Shouldly;

namespace PoeShared.Tests.Services
{
    [TestFixture]
    public class SharedResourceBaseTests : FixtureBase
    {
        private static readonly IFluentLog Log = typeof(SharedResourceBaseTests).PrepareLogger();

        [Test]
        public void ShouldCreate()
        {
            // Given
            // When 
            Action action = () => CreateInstance();

            // Then
            action.ShouldNotThrow();
        }

        [Test]
        public void ShouldDisposeResourceIfNoUsagesLeft()
        {
            //Given
            var instance = CreateInstance();

            //When
            instance.Dispose();

            //Then
            instance.Anchors.IsDisposed.ShouldBe(true);
        }

        [Test]
        public void ShouldNotDisposeResourceIfHasReadUsages()
        {
            //Given
            var instance = CreateInstance();
            using var read = instance.RentReadLock();

            //When
            Action action = () => instance.Dispose();

            //Then
            action.ShouldThrow<LockRecursionException>();
        }
        
        [Test]
        public void ShouldNotDisposeResourceIfHasWriteUsages()
        {
            //Given
            var instance = CreateInstance();
            using var read = instance.RentWriteLock();

            //When
            Action action = () =>  instance.Dispose();

            //Then
            action.ShouldThrow<LockRecursionException>();
        }

        [Test]
        public void ShouldNotDisposeIfRented()
        {
            //Given
            var instance = CreateInstance();
            instance.TryRent().ShouldBe(true);

            //When
            instance.Dispose();

            //Then
            instance.Anchors.IsDisposed.ShouldBe(false);
        }

        [Test]
        public void ShouldDisposeIfReleasedRent()
        {
            //Given
            var instance = CreateInstance();
            instance.TryRent().ShouldBe(true);
            instance.Dispose(); // release rent

            //When
            instance.Dispose(); 

            //Then
            instance.Anchors.IsDisposed.ShouldBe(true);
        }

        [Test]
        public void ShouldDisposeAdjacentResource()
        {
            //Given
            var instance = CreateInstance();
            var resource = new Mock<IDisposable>();
            instance.AddResource(resource.Object);

            //When
            instance.Dispose();

            //Then
            resource.Verify(x => x.Dispose());
        }

        [Test]
        [Timeout(1000)]
        public void ShouldProcessRecursiveReadRent()
        {
            //Given
            var instance = CreateInstance();

            //When
            using var read1 = instance.RentWriteLock();
            Func<IDisposable> rent2 = () => instance.RentWriteLock();

            //Then
            rent2.ShouldNotThrow();
        }

        [Test]
        [Timeout(1000)]
        public void ShouldNotBlockReads()
        {
            //Given
            var instance = CreateInstance();

            //When
            var task1 = Task.Run(() =>
            {
                using var readLock = instance.RentReadLock();
            });
            var task2 = Task.Run(() =>
            {
                using var readLock = instance.RentReadLock();
            });

            //Then
            Task.WaitAll(task1, task2);
        }
        
        [Test]
        [Timeout(1000)]
        public void ShouldBlockWrites()
        {
            //Given
            var instance = CreateInstance();

            //When
            var task1 = Task.Run(() =>
            {
                Log.Debug("Rent #1");
                using var readLock = instance.RentReadLock();
                Log.Debug("Rent #1 completed");
            });
            var task2 = Task.Run(() =>
            {
                Log.Debug("Rent-write #1");
                using var readLock = instance.RentWriteLock();
                Log.Debug("Rent-write #1 completed");
            });

            //Then
            Log.Debug("Awaiting for tasks");
            Task.WaitAll(task1, task2);
            Log.Debug("Tasks have completed");
        }

        [Test]
        [Repeat(1000)]
        public void ShouldNotAllowToRentDisposedResource()
        {
            //Given
            var instance = CreateInstance();

            var expected = 0;
            var gate = new object();
            
            //When
            var task1 = Task.Run(() =>
            {
                Log.Debug($"Disposing instance {instance}");
                instance.Dispose();
                lock (gate)
                {
                    Interlocked.Increment(ref expected);
                    Log.Debug($"Disposed instance {instance}");
                }
            });
            var task2 = Task.Run(() =>
            {
                Log.Debug($"Renting instance {instance}");
                var rented = instance.TryRent();
                lock (gate)
                {
                    Log.Debug($"Rent result: {rented} for instance {instance}");
                    if (Interlocked.Increment(ref expected) == 1)
                    {
                        rented.ShouldBe(true);
                    }
                    else
                    {
                        rented.ShouldBe(false);
                    }
                }
            });


            //Then
            Task.WaitAll(task1, task2);
        }

        private SharedResource CreateInstance()
        {
            return new SharedResource();
        }

        private sealed class SharedResource : SharedResourceBase
        {
        }
    }
}
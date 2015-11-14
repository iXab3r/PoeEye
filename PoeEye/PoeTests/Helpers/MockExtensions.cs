﻿namespace PoeEye.Tests.Helpers
{
    using System;
    using System.ComponentModel;
    using System.Linq.Expressions;

    using Moq;

    using NUnit.Framework;

    internal static class MockExtensions
    {
        public static void SetPropertyAndNotify<T,TProperty>(this Mock<T> mock, Expression<Func<T,TProperty>> expr, TProperty value)
            where T : class, INotifyPropertyChanged
        {
            Assert.IsInstanceOf<MemberExpression>(expr.Body);

            var memberExpression = (MemberExpression)expr.Body;
            var propertyName = memberExpression.Member.Name;
            mock.SetupGet(expr).Returns(value);
            mock.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(propertyName));
        }

        public static void RaisePropertyChanged<T, TProperty>(this Mock<T> mock, Expression<Func<T, TProperty>> expr)
            where T : class, INotifyPropertyChanged
        {
            Assert.IsInstanceOf<MemberExpression>(expr.Body);

            var memberExpression = (MemberExpression)expr.Body;
            var propertyName = memberExpression.Member.Name;
            mock.Raise(x => x.PropertyChanged += null, new PropertyChangedEventArgs(propertyName));
        }
    }
}
using System;
using FluentAssertions;
using NUnit.Framework;
using TheBorg.Common.Extensions;

namespace TheBorg.Tests.UnitTests.Common.Extensions
{
    public class UriExtensionsTests
    {
        [TestCase("https://github.com/rasmus/EventFlow/archive/v0.27.1765.zip", "v0.27.1765.zip")]
        public void GetFilename(string url, string expectedFilename)
        {
            // Arrange
            var uri = new Uri(url, UriKind.Absolute);

            // Act
            var filename = uri.GetFilename();

            // Assert
            filename.Should().Be(expectedFilename);
        }
    }
}

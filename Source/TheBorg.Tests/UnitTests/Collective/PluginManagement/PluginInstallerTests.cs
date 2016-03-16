// The MIT License(MIT)
// 
// Copyright(c) 2016 Rasmus Mikkelsen
// https://github.com/rasmus/TheBorg
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using FluentAssertions;
using Moq;
using NUnit.Framework;
using TheBorg.Collective.PluginManagement;
using TheBorg.Common;
using TheBorg.Common.Clients;
using TheBorg.Interface.ValueObjects.Plugins;

namespace TheBorg.Tests.UnitTests.Collective.PluginManagement
{
    public class PluginInstallerTests : BaseTestFor<PluginInstaller>
    {
        private Mock<IFileSystemClient> _fileSystemClientMock;
        private Mock<IConfiguration> _configurationMock;

        [SetUp]
        public void SetUp()
        {
            _fileSystemClientMock = Mock<IFileSystemClient>();
            _configurationMock = Mock<IConfiguration>();

            _configurationMock
                .Setup(c => c.PluginInstallPath)
                .Returns(@"c:\plugins");
        }

        [Test]
        public void GetInstalledPluginsAsync()
        {
            // Arrange
            _fileSystemClientMock
                .Setup(f => f.GetDirectories(@"c:\plugins"))
                .Returns(new[] { @"c:\plugins\plugin.fancy", @"c:\plugins\plugin.magic"});
            _fileSystemClientMock
                .Setup(f => f.FileExists(It.IsAny<string>()))
                .Returns(true);

            // Act
            var pluginPaths = Sut.GetInstalledPluginsAsync(C).Result;

            // Assert
            pluginPaths.ShouldAllBeEquivalentTo(new []
                {
                    PluginPath.With(@"c:\plugins", "plugin.fancy", "plugin.fancy.dll"),
                    PluginPath.With(@"c:\plugins", "plugin.magic", "plugin.magic.dll")
                }); 
        }
    }
}